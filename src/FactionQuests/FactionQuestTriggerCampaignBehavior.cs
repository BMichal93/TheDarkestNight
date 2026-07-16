// =============================================================================
// THE DARKEST NIGHT — FactionQuests/FactionQuestTriggerCampaignBehavior.cs
//
// The thin CampaignBehaviorBase wrapper around the static FactionQuestTrigger
// registry (see FactionQuestTrigger.cs for the shared, generic infrastructure
// itself). Registered once in MagicSystem.cs; every faction questline plugs
// into the static registry independently and never needs a behavior of its
// own for the trigger step.
// =============================================================================

using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TheDarkestNight
{
    public sealed class FactionQuestTriggerCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store)
        {
            List<string> notified = FactionQuestTrigger.NotifiedSnapshot();
            try { store.SyncData("FACQ_Notified", ref notified); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (store.IsLoading) FactionQuestTrigger.RestoreNotified(notified);
        }

        public static void ResetForNewGame()
        {
            FactionQuestTrigger.ResetForNewGame();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { FactionQuestTrigger.RegisterDialogue(starter); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { FactionQuestTrigger.WeeklyTick(); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
