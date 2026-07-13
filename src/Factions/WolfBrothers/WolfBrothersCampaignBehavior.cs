// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersCampaignBehavior.cs
//
// Faction A — the Wolf Brothers (formerly Sturgia). Owns:
//   • session renaming (kingdom/culture text, via WolfBrothersCulture)
//   • town-scoping to Tyal + Sibir (via WolfBrothersSettlements), daily
//   • the join ritual: eating with the pack costs reputation and hardens the
//     new Kinsman's temperament — applied to EVERY hero in a joining clan,
//     not only the player, so lords who defect to the Wolf Brothers pay the
//     same price
//   • the pack's lords quietly rendering spare prisoners into meat for their
//     own stores, on the same interval/rate math the player's city menu uses
//     (see WolfBrothersCampaignBehavior.Menus.cs for the player-facing side)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class WolfBrothersCampaignBehavior : CampaignBehaviorBase
    {
        private int _lordCannibalizeThrottle = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore store)
        {
            // Nothing persisted — every effect here is either idempotent (renaming,
            // town-scoping) or fires once off a genuine kingdom-change event that
            // does not need to survive a reload to stay correct.
        }

        public static void ResetForNewGame()
        {
            // Renaming/town-scoping are idempotent and re-derive themselves from
            // live campaign state; nothing to reset between sessions.
        }

        // The kingdom-name rename itself runs from the SAME hook every other
        // culture identity uses — AshenCitySystem.EnsureKingdomRenames (fired off
        // OnSessionLaunchedEvent), which now calls WolfBrothersCulture.RenameWolfBrothersKingdom()
        // in place of the retired RenameNorthmenKingdom(). This behavior only owns
        // what is unique to the Wolf Brothers: the town-scoping, the larder menu,
        // and the lords' own cannibalism.
        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterWolfBrothersMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { WolfBrothersSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickLordCannibalism(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The join ritual — reputation loss + a hardened temperament ─────────
        // Fires for every hero in a clan that genuinely joins the Wolf Brothers
        // (was not already Sturgia/Wolf Brothers before). Applies to the whole
        // clan — a lord defecting in brings his whole household to the table,
        // not just himself — so this covers NPC lords exactly as it covers the
        // player.
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || newKingdom == null) return;
                if (newKingdom.StringId != WolfBrothersCulture.CultureId) return;
                if (oldKingdom != null && oldKingdom.StringId == WolfBrothersCulture.CultureId) return; // already one of the pack

                foreach (Hero hero in clan.Heroes.Where(h => h != null && h.IsAlive).ToList())
                    try { ApplyPackConsequence(hero); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void ApplyPackConsequence(Hero hero)
        {
            if (hero == null) return;
            try
            {
                if (hero.Clan != null)
                    hero.Clan.Renown = WolfBrothersMath.ApplyReputationLoss(hero.Clan.Renown);

                int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
                hero.SetTraitLevel(DefaultTraits.Mercy, WolfBrothersMath.ClampTraitLevel(mercy, WolfBrothersMath.JoinMercyShift));

                int valor = hero.GetTraitLevel(DefaultTraits.Valor);
                hero.SetTraitLevel(DefaultTraits.Valor, WolfBrothersMath.ClampTraitLevel(valor, WolfBrothersMath.JoinValorShift));

                if (hero == Hero.MainHero)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You share the pack's meal. There is no unknowing what was in it. (Reputation lost; you feel harder, and less merciful.)",
                        new Color(0.55f, 0.15f, 0.12f)));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Lords render spare prisoners into meat too ──────────────────────────
        private void TickLordCannibalism()
        {
            if (--_lordCannibalizeThrottle > 0) return;
            _lordCannibalizeThrottle = WolfBrothersMath.LordCannibalizeIntervalDays;

            ItemObject meat = MBObjectManager.Instance?.GetObject<ItemObject>("meat");
            if (meat == null) return;

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || party.IsMainParty) continue;
                    var lord = party.LeaderHero;
                    if (lord == null || !WolfBrothersCulture.IsWolfBrotherLord(lord)) continue;
                    if (party.PrisonRoster == null) continue;

                    var captives = party.PrisonRoster.GetTroopRoster()
                        .Where(e => e.Character != null && !e.Character.IsHero)
                        .OrderBy(e => e.Character.Tier)
                        .ToList();
                    if (captives.Count == 0) continue;

                    int rendered = 0;
                    foreach (var entry in captives)
                    {
                        if (rendered >= WolfBrothersMath.LordMaxPrisonersEatenPerTick) break;
                        party.PrisonRoster.AddToCounts(entry.Character, -1);
                        party.ItemRoster.AddToCounts(meat, WolfBrothersMath.MeatFromPrisoner(entry.Character.Tier));
                        rendered++;
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }
    }
}
