// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestCampaignBehavior.War.cs
//
// The Empire's own aggression toward demons, once the war is declared
// (PhaseWar). Retargets LegionCampaignBehavior.TickRaidNudges' proven "point
// an idle lord party at a target" technique (src/Factions/Legion/
// LegionCampaignBehavior.cs) at the nearest DEMON party instead of a rival
// kingdom's settlements — demons are already hostile to everyone, so the
// Empire's own lords actively seeking them out (rather than only fighting
// when a demon party finds THEM first) is the actual, felt mechanical shape
// of "the Empire's aggression toward demons increases."
//
// Demon-party identification is campaign-map-scoped (DemonSpawnCampaignBehavior.
// IsDemonParty, keyed by MobileParty.StringId) — NOT the mission-scoped Agent
// registry DemonBattleBehavior.IsDemon uses, which has no meaning for a party
// that has not yet spawned into a mission (see research note in this
// questline's design pass). There is no pre-built "nearest demon party"
// query anywhere in this codebase, so this mirrors DemonSpawnCampaignBehavior.
// DirectDemonParties' own nearest-party search, just with the roles reversed
// (an Empire party searching for demon prey, rather than a demon party
// searching for human prey).
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace TheDarkestNight
{
    public sealed partial class EmpireQuestCampaignBehavior
    {
        private static void TickAggressionNudges()
        {
            if (_phase != PhaseWar) return;

            var empire = GetEmpireKingdom();
            if (empire == null) return;

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || !party.IsLordParty) continue;
                    if (party == MobileParty.MainParty) continue; // never override the player's own orders
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    if (leaderClan.Kingdom != empire) continue;
                    if (party.Army != null) continue;               // leave coordinated army moves alone
                    if (party.MapEvent != null) continue;           // already fighting
                    if (party.BesiegedSettlement != null) continue; // already assaulting

                    if (!EmpireQuestMath.ShouldNudgeToEngageDemons(_rng.NextDouble())) continue;

                    MobileParty prey = FindNearestDemonParty(party);
                    if (prey == null) continue;

                    try { party.SetMoveEngageParty(prey, MobileParty.NavigationType.Default); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static MobileParty FindNearestDemonParty(MobileParty huntingParty)
        {
            try
            {
                return MobileParty.All
                    .Where(p => p != null && p.IsActive && p != huntingParty
                             && DemonSpawnCampaignBehavior.IsDemonParty(p) && p.MapEvent == null
                             && (p.MemberRoster?.TotalManCount ?? 0) > 0)
                    .OrderBy(p => (p.GetPosition2D - huntingParty.GetPosition2D).LengthSquared)
                    .FirstOrDefault(p => (p.GetPosition2D - huntingParty.GetPosition2D).Length
                                          <= EmpireQuestMath.AggressionSearchRadius);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }
    }
}
