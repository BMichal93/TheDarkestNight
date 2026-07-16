// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleSettlements.cs
//
// Scopes the Temple kingdom down to its two seats: Ocs Hall (town_V2) and
// Pravend (town_V3). Any Vlandia clan that does not hold one of those two
// towns (or a castle bound to them) is ejected from the kingdom — it keeps
// whatever it holds, but that ground falls out of the Temple's scope, exactly
// the "ownerless" state Phase 8 will later fold into a proper city-state.
// Mirrors HiveSettlements.cs / BloodboundSettlements.cs / TowerSettlements.cs
// / WolfBrothersSettlements.cs exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    internal static class TempleSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == "vlandia" && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && TempleMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Temple town" if
        // the Vlandia/Temple kingdom currently owns it — the two named seats
        // when things are healthy, but this stays correct even if the Temple
        // later takes (or loses) ground beyond them.
        internal static bool IsTempleSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == "vlandia"; }
            catch { return false; }
        }
    }
}
