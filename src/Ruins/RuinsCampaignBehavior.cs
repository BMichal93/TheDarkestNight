// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsCampaignBehavior.cs
//
// Phase 9 (Requirement 12). Wires RuinsCastleSystem/RuinsMenus into the
// session lifecycle and RuinsCastleSystem's cooldown state into the daily
// tick + save. Minimal CampaignBehaviorBase, exactly like
// AshenRuinCampaignBehavior — all the actual logic lives in
// RuinsCastleSystem / RuinsExplorationSystem / RuinsMenus.
// =============================================================================

using TaleWorlds.CampaignSystem;

namespace TheDarkestNight
{
    public class RuinsCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RuinsCastleSystem.OnSessionLaunched(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { RuinsMenus.OnSessionLaunched(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { RuinsCastleSystem.ReapplyRuinNamesIfNeeded(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { RuinsCastleSystem.DailyTick(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        public override void SyncData(IDataStore dataStore)
        {
            try { RuinsCastleSystem.SyncData(dataStore); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
