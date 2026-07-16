// =============================================================================
// THE DARKEST NIGHT — CityStates/CityStateCampaignBehavior.cs
//
// Thin CampaignBehaviorBase wrapper around CityStateSystem (Phase 8,
// Requirements 11 & 24). All logic lives in the static system class, matching
// the AshenCitySystem split (a plain campaign behavior only wires events).
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace TheDarkestNight
{
    public class CityStateCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore store)
        {
            // Nothing persisted — city-state kingdoms are ordinary Kingdom game
            // objects (saved by the engine itself once created) and membership
            // is re-derived every tick from live Clan/Kingdom state (see
            // CityStateSystem.ReassertCityStateMembership), so there is no extra
            // bookkeeping of our own to save or restore.
        }

        private void OnDailyTick()
        {
            CityStateSystem.OnDailyTick();
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            CityStateSystem.OnClanChangedKingdom(clan, oldKingdom, newKingdom, detail, showNotification);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            CityStateSystem.OnSessionLaunched();
        }

        public static void ResetForNewGame()
        {
            CityStateSystem.ResetForNewGame();
        }
    }
}
