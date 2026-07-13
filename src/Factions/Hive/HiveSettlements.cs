// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveSettlements.cs
//
// Scopes the Hive kingdom down to its two seats: Marunath (town_B1) and
// Car Banseth (town_B3). Any Battania clan that does not hold one of those
// two towns (or a castle bound to them) is ejected from the kingdom — it
// keeps whatever it holds, but that ground falls out of the Hive's scope,
// exactly the "ownerless" state Phase 8 will later fold into a proper
// city-state. Mirrors WolfBrothersSettlements.cs / TowerSettlements.cs
// exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AshAndEmber
{
    internal static class HiveSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == HiveCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && HiveMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Hive town" if
        // the Battania/Hive kingdom currently owns it — the two named seats
        // when things are healthy, but this stays correct even if the Hive
        // later takes (or loses) ground beyond them.
        internal static bool IsHiveSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == HiveCulture.CultureId; }
            catch { return false; }
        }
    }
}
