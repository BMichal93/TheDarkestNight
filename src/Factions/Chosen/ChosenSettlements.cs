// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenSettlements.cs
//
// Scopes the Chosen kingdom (Southern Empire, StringId "empire_s") down to
// its two seats: Lycaron (town_ES4) and Phycaon (town_ES6) — the same two
// seats the (deleted) Pale Widows held. Any Chosen clan that does not hold
// one of those two towns (or a castle bound to them) is ejected from the
// kingdom — it keeps whatever it holds, but that ground falls out of the
// Chosen's scope. Mirrors LegionSettlements.cs / EmpireSettlements.cs /
// BloodboundSettlements.cs exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AshAndEmber
{
    internal static class ChosenSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == ChosenCulture.KingdomId && !k.IsEliminated);
                if (kingdom == null) return;

                foreach (var clan in kingdom.Clans.ToList())
                {
                    try
                    {
                        if (clan == null || clan.IsEliminated) continue;
                        if (clan == Clan.PlayerClan) continue; // never evict the player's own clan by ownership alone
                        if (HoldsAStartingTown(clan)) continue;

                        ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool HoldsAStartingTown(Clan clan)
        {
            try
            {
                return clan.Settlements.Any(s =>
                    (s.IsTown || s.IsCastle) && ChosenMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Chosen town"
        // if the kingdom currently owns it — the two named seats when things
        // are healthy, but this stays correct even if the Chosen later take
        // (or lose) ground beyond them.
        internal static bool IsChosenSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == ChosenCulture.KingdomId; }
            catch { return false; }
        }
    }
}
