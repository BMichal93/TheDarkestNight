// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireSettlements.cs
//
// Scopes the Empire kingdom down to its three seats: Saneopa (town_EN3),
// Diathma (town_EN2) and Argoron (town_EN4). Any Northern Empire clan that
// does not hold one of those three towns (or a castle bound to them) is
// ejected from the kingdom — it keeps whatever it holds, but that ground
// falls out of the Empire's scope, exactly the "ownerless" state Phase 8
// will later fold into a proper city-state. Mirrors TempleSettlements.cs /
// HiveSettlements.cs / BloodboundSettlements.cs / TowerSettlements.cs /
// WolfBrothersSettlements.cs exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AshAndEmber
{
    internal static class EmpireSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == EmpireCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && EmpireMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as an "Empire town"
        // if the Northern Empire kingdom currently owns it — the three named
        // seats when things are healthy, but this stays correct even if the
        // Empire later takes (or loses) ground beyond them.
        internal static bool IsEmpireSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == EmpireCulture.CultureId; }
            catch { return false; }
        }
    }
}
