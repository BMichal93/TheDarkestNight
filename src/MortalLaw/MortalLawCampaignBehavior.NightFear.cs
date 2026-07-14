// =============================================================================
// THE DARKEST NIGHT — MortalLaw/MortalLawCampaignBehavior.NightFear.cs
//
// Phase 10b — "fear the night." Checked hourly (see DemonSpawnCampaignBehavior
// for the same hour/day bookkeeping pattern, and DemonMath.IsNightHour/
// DuskHour/DawnHour for the shared dusk/dawn gate — this system deliberately
// rides the exact same clock the Night Tide does, so a party redirected here
// is fleeing the same dusk that wakes the demons).
//
// Any non-player, non-garrison, non-caravan/villager lord party whose
// effective strength (its own headcount, or its Army's combined headcount if
// it is riding with one) falls under MortalLawMath.NightSafeArmySize is
// redirected toward the nearest friendly settlement via
// SetMoveGoToSettlement — the confirmed 3-arg MobileParty signature (see
// behaviour.md's "Confirmed gotchas"). The redirect fires once per hour while
// it is still night and the party has not yet reached shelter; it is a direct
// order exactly like Legion's raid nudge and the Night Tide's settlement
// assaults, not a persistent lock — once dawn breaks the daily tick simply
// stops re-issuing it and vanilla AI resumes deciding for itself.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AshAndEmber
{
    public partial class MortalLawCampaignBehavior
    {
        private static float CurrentHourOfDay()
        {
            try { return (float)CampaignTime.Now.CurrentHourInDay; }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return 12f; }
        }

        private static void TickNightFear()
        {
            float hour = CurrentHourOfDay();
            if (!DemonMath.IsNightHour(hour)) return;

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive) continue;
                    if (party == MobileParty.MainParty) continue; // never override the player
                    if (!party.IsLordParty) continue;             // caravans/villagers/garrisons stay on their own routines
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    if (party.MapEvent != null) continue;              // already fighting
                    if (party.BesiegedSettlement != null) continue;    // already assaulting
                    if (party.CurrentSettlement != null) continue;     // already sheltered
                    if (party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.RaidSettlement
                     || party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement
                     || party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement) continue; // let a raid resolve, not abandon it mid-swing

                    int effectiveManCount = party.Army != null
                        ? party.Army.TotalManCount
                        : (party.MemberRoster?.TotalManCount ?? 0);
                    if (MortalLawMath.IsSafeFromNightFear(effectiveManCount)) continue;

                    if (party.TargetSettlement != null) continue; // already homing on shelter

                    Settlement shelter = FindNearestFriendlySettlement(party);
                    if (shelter == null) continue;

                    try { party.SetMoveGoToSettlement(shelter, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static Settlement FindNearestFriendlySettlement(MobileParty party)
        {
            try
            {
                return Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle || s.IsVillage)
                             && s.MapFaction != null
                             && !FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
            }
            catch { return null; }
        }
    }
}
