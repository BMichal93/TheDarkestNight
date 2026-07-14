// =============================================================================
// THE DARKEST NIGHT — MortalLaw/MortalLawCampaignBehavior.Hunger.cs
//
// Phase 10c — "fight for food." Checked daily. Reflecting RaidModel again
// finds no clean GameModel for the raid TRIGGER decision (only in-progress
// loot/damage math once a raid is already under way — the exact finding
// LegionCampaignBehavior's header documents for Faction G). So hunger is
// wired through the same direct-order technique: any idle, non-player lord
// party across all eight Phase 7 kingdoms that is both AT WAR with someone
// and running a food deficit (MobileParty.Food <= 0, vanilla's own starvation
// signal) rolls MortalLawMath.HungryRaidNudgeChance once a day to be pointed
// at the nearest hostile village via SetMoveRaidSettlement.
//
// Scoping note: "attack caravans" from the requirement text is intentionally
// left to vanilla's own opportunistic caravan-raiding AI — forcing a specific
// caravan engagement has no equivalent direct-order seam (SetMoveEngageParty
// exists, but hunting down a moving caravan target every tick is a much
// heavier, more fragile system than nudging toward a fixed settlement). The
// village-raid nudge alone already satisfies "hungry parties raid... for
// supplies" and is the low-risk implementation.
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
        private static readonly Random _hungerRng = new Random();

        private static void TickHungerRaids()
        {
            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || !party.IsLordParty) continue;
                    if (party == MobileParty.MainParty) continue; // never override the player
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    var kingdom = leaderClan.Kingdom;
                    if (!IsOneOfOurKingdoms(kingdom)) continue;
                    if (party.Army != null) continue;              // leave coordinated army moves alone
                    if (party.MapEvent != null) continue;          // already fighting
                    if (party.BesiegedSettlement != null) continue; // already assaulting
                    if (party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.RaidSettlement
                     || party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement
                     || party.ShortTermBehavior == TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement) continue;

                    if (!MortalLawMath.IsPartyHungry(party.Food)) continue;
                    if (!IsAtWarWithAnyone(kingdom)) continue; // nothing hostile to raid
                    if (!MortalLawMath.ShouldNudgeHungryRaid(_hungerRng.NextDouble())) continue;

                    Settlement target = FindHungryRaidTarget(party);
                    if (target == null) continue;

                    try { party.SetMoveRaidSettlement(target, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsAtWarWithAnyone(Kingdom kingdom)
        {
            try { return Kingdom.All.Any(k => k != null && !k.IsEliminated && k != kingdom && kingdom.IsAtWarWith(k)); }
            catch { return false; }
        }

        private static Settlement FindHungryRaidTarget(MobileParty party)
        {
            try
            {
                // Prefer an undefended village — food and supplies live there.
                Settlement village = Settlement.All
                    .Where(s => s != null && s.IsVillage && s.Village != null
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
                if (village != null) return village;

                return Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle)
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
            }
            catch { return null; }
        }
    }
}
