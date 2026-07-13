// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundSettlements.cs
//
// Scopes the Bloodbound kingdom down to its two seats: Akkalat (town_K2) and
// Chaikand (town_K5). Any Khuzait clan that does not hold one of those two
// towns (or a castle bound to them) is ejected from the kingdom — it keeps
// whatever it holds, but that ground falls out of the Bloodbound's scope,
// exactly the "ownerless" state Phase 8 will later fold into a proper
// city-state. Mirrors HiveSettlements.cs / TowerSettlements.cs /
// WolfBrothersSettlements.cs exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AshAndEmber
{
    internal static class BloodboundSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == BloodboundCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && BloodboundMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Bloodbound town"
        // if the Khuzait/Bloodbound kingdom currently owns it — the two named
        // seats when things are healthy, but this stays correct even if the
        // Bloodbound later take (or lose) ground beyond them.
        internal static bool IsBloodboundSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == BloodboundCulture.CultureId; }
            catch { return false; }
        }
    }
}
