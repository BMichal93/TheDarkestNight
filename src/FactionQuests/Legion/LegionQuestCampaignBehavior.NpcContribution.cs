// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestCampaignBehavior.NpcContribution.cs
//
// Legion lords occasionally hand over materials of their own — a background
// trickle, never the primary driver (mirrors NorthmenStonesCampaignBehavior.
// NpcContribution.cs's own NpcContributionWeeklyTick exactly).
//
// ApplyDecayWeeklyTick is the theft mechanic the brief explicitly calls out:
// "Materials are persistent... unless the city is captured by a different
// faction, in which case they disappear by 10 percent per week." Fires every
// week Ortysia is not Legion's — whether it fell to a rival kingdom's siege
// OR to the Demon Lord's own late-game conquest (DemonLordSystem.WeeklyTick
// besieges towns exactly like castles, no distinction) — and resumes normal
// accumulation the moment Ortysia is retaken. Not softened: this is the same
// ApplyWeeklyDecay curve NorthmenStonesMath.cs already uses.
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    public sealed partial class LegionQuestCampaignBehavior
    {
        private void NpcContributionWeeklyTick()
        {
            if (_phase != PhaseGathering) return;
            if (!IsOrtysiaLegionOwned()) return;

            Kingdom legion = GetLegionKingdom();
            if (legion == null) return;

            try
            {
                foreach (Hero lord in legion.Heroes.Where(h =>
                             h != null && h.IsLord && h.IsAlive && !h.IsChild && h != Hero.MainHero
                             && h.Clan != null && h.Clan.Settlements != null
                             && h.Clan.Settlements.Any(s => s.IsTown || s.IsCastle)).ToList())
                {
                    if (_rng.NextDouble() >= LegionQuestMath.NpcWeeklyContributionChance) continue;

                    if (_rng.Next(2) == 0)
                        _hardwood += LegionQuestMath.NpcContributionAmount(_rng, 15, 60);
                    else
                        _iron += LegionQuestMath.NpcContributionAmount(_rng, 15, 60);
                }

                LegionQuestLog.Current?.LogStockProgress(_hardwood, _iron);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void ApplyDecayWeeklyTick()
        {
            if (_phase != PhaseGathering) return;
            if (IsOrtysiaLegionOwned()) return;

            try
            {
                _hardwood = LegionQuestMath.ApplyWeeklyDecay(_hardwood);
                _iron     = LegionQuestMath.ApplyWeeklyDecay(_iron);
                LegionQuestLog.Current?.LogStockProgress(_hardwood, _iron);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
