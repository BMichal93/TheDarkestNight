// =============================================================================
// ASH AND EMBER — GreatAwakeningCampaignBehavior.Trigger.cs
// The day-50+ discovery roll. Fires once, ever; after that the leader's
// dialogue line (see .Dialogue.cs) carries the story forward.
// =============================================================================

using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public partial class GreatAwakeningCampaignBehavior
    {
        private void TriggerWeeklyTick()
        {
            if (_phase != PhaseIdle) return;
            // Days elapsed since the campaign started — NOT CampaignTime.Now.ToDays,
            // which is absolute calendar days (~tens of thousands) and would pass
            // the start-day gate immediately on a new campaign.
            int day = (int)CampaignMapEvents.ElapsedCampaignDays();

            // The Tower's darker second act: gated behind the Unbinding Rite's
            // conclusion (see GreatAwakeningMath.TriggerAllowed) so the same
            // Archmagister is never trying to close the way and crown what
            // comes through it in the same season. The late fallback day keeps
            // this reachable in a campaign that never engaged the Rite.
            bool riteConcluded = false;
            try { riteConcluded = TowerRiteQuestCampaignBehavior.RiteConcluded; }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            if (!GreatAwakeningMath.TriggerAllowed(day, riteConcluded)) return;
            if (day < GreatAwakeningMath.TriggerStartDay) return;
            if (_rng.NextDouble() >= GreatAwakeningMath.TriggerChance(day)) return;

            _phase = PhaseDiscovered;
            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(riteConcluded
                    ? "The Great Rite failed, and the Tower did not stop reading. Word reaches you that " +
                      "the Archmagister has drawn a darker lesson from the ruin: if the way cannot be " +
                      "closed, it means to greet what comes through it — and crown it."
                    : "Dark forces gather in the deep desert. Word reaches you that the Tower has found " +
                      "something ancient down in the Sands — and means to bring it in."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
