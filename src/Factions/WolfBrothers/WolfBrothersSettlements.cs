// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersSettlements.cs
//
// Scopes the Wolf Brothers kingdom down to its two seats: Tyal (town_S5) and
// Sibir (town_S6). Any Sturgia clan that does not hold one of those two towns
// (or a castle bound to them) is ejected from the kingdom — it keeps whatever
// it holds, but that ground falls out of the Wolf Brothers' scope, exactly the
// "ownerless" state Phase 8 will later fold into a proper city-state. This
// mirrors TempleCulture.TrimTempleClans (AI/TempleCulture.cs), keyed on
// settlement ownership instead of clan renown ranking.
//
// KNOWN INTERACTION (pre-existing, out of this task's scope): the legacy
// AshAndEmber "Ashen" fire-lord kingdom (AshenCitySystem) also targets Tyal
// and Sibir by StringId as two of its own core seats and actively reclaims
// them via its own daily maintenance. That system predates The Darkest Night
// conversion and is untouched here — if it holds Tyal/Sibir at session start,
// the Wolf Brothers will start landless until a later cleanup phase resolves
// the overlap. This method only ever ACTS on settlements the Sturgia kingdom
// itself still owns; it never fights the Ashen system for ownership.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    internal static class WolfBrothersSettlements
    {
        public static void ScopeToStartingTowns()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k =>
                    k.StringId == WolfBrothersCulture.CultureId && !k.IsEliminated);
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
                    (s.IsTown || s.IsCastle) && WolfBrothersMath.IsStartingTownId(s.StringId));
            }
            catch { return false; }
        }

        // Used by the town-menu gate: a settlement counts as a "Wolf Brothers
        // town" if the Sturgia/Wolf Brothers kingdom currently owns it — the two
        // named seats when things are healthy, but this stays correct even if
        // the pack later takes (or loses) ground beyond them.
        internal static bool IsWolfBrothersSettlement(TaleWorlds.CampaignSystem.Settlements.Settlement s)
        {
            if (s == null || !(s.IsTown || s.IsCastle)) return false;
            try { return s.MapFaction?.StringId == WolfBrothersCulture.CultureId; }
            catch { return false; }
        }
    }
}
