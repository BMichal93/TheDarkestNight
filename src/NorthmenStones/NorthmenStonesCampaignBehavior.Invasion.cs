// =============================================================================
// ASH AND EMBER — NorthmenStonesCampaignBehavior.Invasion.cs
//
// As the working nears completion the Ashen feel it and throw themselves at
// Varcheg to break it before it closes — one-shot waves at 50/75/90% blended
// progress, each bigger than the last.
// =============================================================================

using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public partial class NorthmenStonesCampaignBehavior
    {
        private void InvasionWeeklyTick()
        {
            if (_phase != PhaseActive) return;

            float progress = NorthmenStonesMath.BlendedProgress(
                _iron, _hardwood, _tools, _silver, _denars, KindledTotal());

            if (!_invasion90Fired && progress >= NorthmenStonesMath.InvasionThresholds[2])
            {
                _invasion90Fired = true;
                SpawnInvasionWave(2,
                    "The Night can feel the stones close to whole — and demons pour toward Varcheg in a flood, " +
                    "drawn to tear at the working the way beasts are drawn to a fire that burns them.");
            }
            else if (!_invasion75Fired && progress >= NorthmenStonesMath.InvasionThresholds[1])
            {
                _invasion75Fired = true;
                SpawnInvasionWave(1,
                    "Three-quarters raised, and the Night has noticed. Packs of demons break from the dark " +
                    "and turn north, toward Varcheg, hungry for whatever stands near the stones.");
            }
            else if (!_invasion50Fired && progress >= NorthmenStonesMath.InvasionThresholds[0])
            {
                _invasion50Fired = true;
                SpawnInvasionWave(0,
                    "The stones are half-raised. Something out of the Night has noticed the working take shape, and " +
                    "moves against Varcheg.");
            }
        }

        private static void SpawnInvasionWave(int tier, string narration)
        {
            try
            {
                var varcheg = VarchegSettlement();
                if (varcheg == null) return;
                Vec2 pos = varcheg.GetPosition2D;

                int bands = NorthmenStonesMath.InvasionBandCount(tier);
                float minStrength = NorthmenStonesMath.InvasionMinStrength(tier);
                for (int i = 0; i < bands; i++)
                    CampaignMapEvents.SpawnAshenAmbushNear(pos, 40, minStrength);

                MBInformationManager.AddQuickInformation(new TextObject(narration));
                NorthmenStonesQuestLog.LogInvasion(narration);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
