// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenCampaignBehavior.Rod.cs
//
// Distribution for the Rod of the Apostle — mirrors
// TempleCampaignBehavior.GrantHolySigil's on-join grant pattern (see
// Factions/Temple/TempleCampaignBehavior.cs), extended per the brief: every
// Chosen lord is one of "the Chosen", so the PriestKing himself and every
// other Chosen lord hero are swept for one too, not just the player.
//
//   • On genuinely joining the Chosen (OnClanChangedKingdomEvent, same gate
//     shape as TempleCampaignBehavior.OnClanChangedKingdom) — the player is
//     granted a Rod immediately.
//   • SweepGrantRodsToChosenLords (weekly, plus once at session launch) —
//     checks every living Chosen lord hero (PriestKing included) and grants
//     a Rod to anyone who does not already carry one in their party's item
//     roster.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class ChosenCampaignBehavior
    {
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || newKingdom == null) return;
                if (newKingdom.StringId != ChosenCulture.KingdomId) return;
                if (oldKingdom != null && oldKingdom.StringId == ChosenCulture.KingdomId) return; // already Chosen

                if (clan == Clan.PlayerClan)
                    try { GrantRodOfApostle(Hero.MainHero, announce: true); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static bool HasRodOfApostle(Hero hero)
        {
            if (hero == null) return false;
            try
            {
                var roster = hero.PartyBelongedTo?.ItemRoster ?? (hero == Hero.MainHero ? MobileParty.MainParty?.ItemRoster : null);
                if (roster == null) return false;
                return roster.Any(e => e.EquipmentElement.Item?.StringId == ChosenRodCatalog.RodOfApostleItemId);
            }
            catch { return false; }
        }

        internal static void GrantRodOfApostle(Hero hero, bool announce)
        {
            if (hero == null) return;
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(ChosenRodCatalog.RodOfApostleItemId);
                var roster = hero.PartyBelongedTo?.ItemRoster ?? (hero == Hero.MainHero ? MobileParty.MainParty?.ItemRoster : null);
                if (item == null || roster == null) return;
                roster.AddToCounts(item, 1);

                if (announce && hero == Hero.MainHero)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "A Rod of the Apostle is pressed into your hand — cold iron, warm with something that is not quite blessing. "
                      + "\"Carry it well. Heaven does not ask what it costs the men beside you.\"",
                        new Color(0.85f, 0.72f, 0.25f)));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Weekly sweep (plus once at session launch): every living Chosen lord
        // hero, the PriestKing included, gets a Rod if they don't already
        // carry one — "give to all PriestKing Chosen" per the brief.
        private static void SweepGrantRodsToChosenLords()
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;

                foreach (var hero in Hero.AllAliveHeroes.ToList())
                {
                    try
                    {
                        if (hero == null || !hero.IsLord) continue;
                        if (!ChosenCulture.IsChosenLord(hero)) continue;
                        if (HasRodOfApostle(hero)) continue;
                        GrantRodOfApostle(hero, announce: hero == Hero.MainHero);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
