// =============================================================================
// THE DARKEST NIGHT — Factions/ForestWidows/ForestWidowsSettlements.cs
//
// Scopes the Forest Widows kingdom (Battania, StringId "battania") down to
// its two seats: Marunath (town_B1) and Car Banseth (town_B3). Any Forest
// Widows clan that does not hold one of those two towns (or a castle bound
// to them) is ejected from the kingdom — it keeps whatever it holds, but
// that ground falls out of the Widows' scope, exactly the "ownerless" state
// Phase 8 later folds into a proper city-state. Mirrors
// PaleWidowsSettlements.cs exactly, keyed on settlement ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    internal static class ForestWidowsSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == ForestWidowsCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && ForestWidowsMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Forest Widows
        // town" if the kingdom currently owns it — the two named seats when
        // things are healthy, but this stays correct even if the Widows
        // later take (or lose) ground beyond them.
        internal static bool IsForestWidowsSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == ForestWidowsCulture.CultureId; }
            catch { return false; }
        }
    }
}
