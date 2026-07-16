// =============================================================================
// THE DARKEST NIGHT — MortalLaw/MortalLawCampaignBehavior.Rosters.cs
//
// Phase 10d — "same rules as the player." Checked weekly.
//
// ── Scoping decision ─────────────────────────────────────────────────────────
// The requirement text asks for NPCs to "conceptually pay the same promotion
// costs" as the player (Phase 3's horse+armour+weapon toll — see
// Units/PromotionToll.cs / UnitsMath.cs). A full NPC-side simulation of that —
// tracking each AI party's saddlebags, spending real items on every upgrade
// decision the campaign AI makes internally — would mean hooking or
// reimplementing AI troop-upgrade logic that Bannerlord does not expose a
// clean seam for (the same "no GameModel for the decision, only the math"
// shape Phase 7G already ran into for raids). That is real, fragile,
// clever-but-risky engineering for a cosmetic payoff. The prompt's own
// acceptance criterion is "enemy armies look as ragged as yours" — so instead,
// this is a periodic TIER-RATIO ENFORCEMENT pass: any lord party belonging to
// one of the eight Phase 7 kingdoms that is carrying more tier-4/5 troops than
// MortalLawMath.MaxAllowedForTier permits has the excess trimmed straight out
// of the roster (not demoted to a lower tier — CharacterObject exposes
// UpgradeTargets forward but no clean "downgrade to" lookup, and trimming
// alone already delivers the required silhouette: mostly low-tier troops,
// only a few elites). This is a deliberately safer, coarser substitute for the
// full economy simulation — documented here rather than silently scoped down.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace TheDarkestNight
{
    public partial class MortalLawCampaignBehavior
    {
        private static void TickRosterTrim()
        {
            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || !party.IsLordParty) continue;
                    if (party == MobileParty.MainParty) continue; // never touch the player's own roster
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    if (!IsOneOfOurKingdoms(leaderClan.Kingdom)) continue;

                    TrimRosterTier(party.MemberRoster, 5);
                    TrimRosterTier(party.MemberRoster, 4);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static void TrimRosterTier(TroopRoster roster, int tier)
        {
            if (roster == null) return;
            try
            {
                int totalTroops = roster.TotalManCount;
                if (totalTroops <= 0) return;

                var elements = roster.GetTroopRoster();
                if (elements == null) return;

                // Only ever trim rank-and-file — the party leader and any other
                // hero riding along are never touched, no matter their tier.
                var tierElements = elements
                    .Where(e => e.Character != null && !e.Character.IsHero && e.Character.Tier == tier && e.Number > 0)
                    .ToList();
                if (tierElements.Count == 0) return;

                int currentCount = tierElements.Sum(e => e.Number);
                int excess = MortalLawMath.TrimExcessForTier(tier, currentCount, totalTroops);
                if (excess <= 0) return;

                foreach (var e in tierElements)
                {
                    if (excess <= 0) break;
                    int take = Math.Min(excess, e.Number);
                    if (take <= 0) continue;
                    roster.AddToCounts(e.Character, -take);
                    excess -= take;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
