// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerSettlements.cs
//
// Scopes the Tower kingdom down to its one seat: Iyakis (town_A3). Any Aserai
// clan that does not hold it (or a castle bound to it) is ejected from the
// kingdom — it keeps whatever it holds, but that ground falls out of the
// Tower's scope, exactly the "ownerless" state Phase 8 will later fold into
// a proper city-state. Mirrors WolfBrothersSettlements.cs exactly, keyed on
// settlement ownership instead of clan renown ranking.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    internal static class TowerSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == TowerCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && TowerMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Tower town" if
        // the Aserai/Tower kingdom currently owns it — Iyakis when things are
        // healthy, but this stays correct even if the Tower later takes (or
        // loses) ground beyond it.
        internal static bool IsTowerSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == TowerCulture.CultureId; }
            catch { return false; }
        }
    }
}
