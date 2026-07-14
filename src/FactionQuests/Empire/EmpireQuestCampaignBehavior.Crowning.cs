// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestCampaignBehavior.Crowning.cs
//
// The crowning and the war declaration — both fire together, once, the
// moment EmpireQuestMath.ConquestTownThreshold is reached.
//
// ── The crowning ─────────────────────────────────────────────────────────
// The ruler title "Emperor" already exists (EmpireCulture.RulerTitle, Phase
// 7F) — there is no mechanical title to grant here, so "crowning" is the
// narrative/ceremonial moment (EmpireQuestLog.LogCrowned + the inquiry
// below). The one small, tunable mechanical flourish: a real renown jump for
// the ruling clan (EmpireQuestMath.CrownRenownBonus, applied through
// ClanRenown.Gain — the one correct way to move renown in this codebase, see
// ClanRenown.cs's own header) and a one-time coronation-feast supply grant
// to every living Empire lord's party, reusing EmpireCampaignBehavior.
// GrantGrain rather than inventing a morale/stat system this codebase has
// nowhere else.
//
// ── The war declaration ──────────────────────────────────────────────────
// Per the brief: demons are already hostile to everyone (DemonBattleBehavior/
// DemonSpawnCampaignBehavior fight the player and every NPC party on sight
// regardless of any Kingdom diplomacy state) — a "demons are permanently
// hostile to the Empire" flag would add nothing. The meaningful addition is
// the Empire's OWN aggression increasing, which is what .War.cs's daily
// nudge actually does. This method still performs a real, mechanical war
// DECLARATION wherever one is possible — DemonLordSystem's one-clan Kingdom
// (DemonLordSystem.KingdomId), when it exists (Phase 11's late-game boss),
// gets a genuine DeclareWarAction the same way ForestWidowsQuestCampaign
// Behavior.Resolution.cs's DeclareWarOnEveryone declares the Widows' dark
// pact war — and _hasDeclaredWarOnDemons is set unconditionally regardless
// of whether that Kingdom happens to exist yet this campaign, so
// AshenDiplomacyModel's permanent-war lock (IsEmpireDemonWar) and the
// aggression nudge in .War.cs both activate the instant the Empire's own
// declaration is made, and the Kingdom-level DeclareWarAction simply
// catches up automatically the moment the Demon Lord's Kingdom is founded
// (ReassertWarOnDemonLord, ticked daily below).
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class EmpireQuestCampaignBehavior
    {
        // Persisted: once true, the Empire's war on the demons is permanent —
        // consulted by AshenDiplomacyModel.IsEmpireDemonWar and by .War.cs's
        // daily aggression nudge.
        private static bool _hasDeclaredWarOnDemons = false;
        internal static bool HasDeclaredWarOnDemons => _hasDeclaredWarOnDemons;

        private static void SyncWarData(IDataStore store)
        {
            try { store.SyncData("EMPQ_WarDeclared", ref _hasDeclaredWarOnDemons); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ResetWarState()
        {
            _hasDeclaredWarOnDemons = false;
        }

        private static void CrownEmperorAndDeclareWar(Kingdom empire)
        {
            try
            {
                _phase = PhaseWar;
                _hasDeclaredWarOnDemons = true;

                Hero emperor = EmpireLeader();
                ClanRenown.Gain(emperor?.Clan ?? empire?.RulingClan, EmpireQuestMath.CrownRenownBonus);
                GrantCoronationSupplies(empire);
                ReassertWarOnDemonLord(empire);

                try { EmpireQuestLog.Current?.LogCrowned(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                InformationManager.ShowInquiry(new InquiryData(
                    "The Reunification",

                    "Saneopa's old hall has not held a coronation since before the Long Night, and it holds one " +
                    "now. Every Legate who could ride in time stands witness as the ruling clan's head takes the " +
                    "old crown — not a ceremonial one, not a courtesy title, but the real thing: Emperor of a " +
                    "Calradia that answers to one banner again.\n\n" +
                    "The new Emperor does not let the moment rest. Before the feast is even cleared, the order " +
                    "goes out to every Empire hall, every Legate's hearth: the reunified Empire does not merely " +
                    "hold its walls against the dark any longer. It marches out to meet it. War is declared — not " +
                    "a war that can be won by one battle, but the first war in longer than anyone living can " +
                    "remember that a whole, unified Calradia is fighting on its own terms.",

                    true, false, "Long live the Emperor.", "", null, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void GrantCoronationSupplies(Kingdom empire)
        {
            if (empire == null) return;
            try
            {
                foreach (Clan clan in empire.Clans.ToList())
                {
                    if (clan == null || clan.IsEliminated) continue;
                    foreach (Hero hero in (clan.Heroes ?? System.Linq.Enumerable.Empty<Hero>()).ToList())
                    {
                        try
                        {
                            if (hero == null || !hero.IsAlive || hero.PartyBelongedTo == null) continue;
                            if (hero.PartyBelongedTo.ItemRoster == null) continue;
                            var grain = TaleWorlds.ObjectSystem.MBObjectManager.Instance?
                                .GetObject<ItemObject>("grain");
                            if (grain == null) continue;
                            hero.PartyBelongedTo.ItemRoster.AddToCounts(grain, EmpireQuestMath.CoronationGrainAmount);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Belt-and-suspenders: catches the Demon Lord's Kingdom up on the
        // Empire's declared war the instant it exists, whether that is right
        // now (Phase 11 already past its rise stage) or months later (the
        // crowning happened before the Demon Lord ever appeared). Cheap to
        // call daily — a no-op once already at war or while the Kingdom does
        // not yet exist.
        private static void ReassertWarOnDemonLord(Kingdom empire)
        {
            if (!_hasDeclaredWarOnDemons) return;
            try
            {
                empire = empire ?? GetEmpireKingdom();
                if (empire == null) return;
                var demonLordKingdom = Kingdom.All.FirstOrDefault(k =>
                    k != null && k.StringId == DemonLordSystem.KingdomId && !k.IsEliminated);
                if (demonLordKingdom == null) return;
                if (empire.IsAtWarWith(demonLordKingdom)) return;
                DeclareWarAction.ApplyByDefault(empire, demonLordKingdom);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
