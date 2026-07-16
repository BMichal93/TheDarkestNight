// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionSettlements.cs
//
// Scopes the Legion kingdom (Western Empire, StringId "empire_w") down to its
// two seats: Lageta (town_EW1) and Ortysia (town_EW4). Any Legion clan that
// does not hold one of those two towns (or a castle bound to them) is ejected
// from the kingdom — it keeps whatever it holds, but that ground falls out of
// the Legion's scope, exactly the "ownerless" state Phase 8 will later fold
// into a proper city-state. Mirrors EmpireSettlements.cs / TempleSettlements.
// cs / HiveSettlements.cs / BloodboundSettlements.cs exactly, keyed on
// settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    internal static class LegionSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == LegionCulture.KingdomId && !k.IsEliminated);
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
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static bool HoldsAStartingTown(Clan clan)
        {
            try
            {
                return clan.Settlements.Any(s =>
                    (s.IsTown || s.IsCastle) && LegionMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate and the aggression nudge: a settlement
        // counts as a "Legion town" if the Legion kingdom currently owns it —
        // the two named seats when things are healthy, but this stays correct
        // even if the Legion later takes (or loses) ground beyond them.
        internal static bool IsLegionSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == LegionCulture.KingdomId; }
            catch { return false; }
        }
    }
}
