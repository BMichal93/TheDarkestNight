// =============================================================================
// THE DARKEST NIGHT — Startup/SandboxOnlyGate.cs
// The Long Night's fiction, economy, and demon tide are built for an open
// Sandbox campaign. StoryMode's scripted main quest assumes an intact Calradia
// and was never adapted to any of it, so if a player starts it anyway we catch
// the campaign the moment it spins up and send them back to the main menu with
// an explanation, rather than let a StoryMode quest collide with systems it was
// never built to see.
//
// Detection is reflection-only (no compile-time reference to StoryMode.dll):
// we look for a registered campaign behavior whose full type name matches
// StoryMode's main storyline behavior. If present, the running campaign is a
// StoryMode campaign. Everything is guarded so a future engine/StoryMode change
// degrades to a no-op (the player simply is not stopped) rather than a crash.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    internal static class SandboxOnlyGate
    {
        private const string StoryModeBehaviorFullName =
            "StoryMode.GameComponents.CampaignBehaviors.MainStorylineCampaignBehavior";

        private static bool _checked;
        private static bool _warned;

        public static void ResetForNewGame()
        {
            _checked = false;
            _warned  = false;
        }

        public static void Tick()
        {
            try
            {
                if (_checked) return;
                var campaign = Campaign.Current;
                if (campaign == null) return;
                _checked = true;

                if (!IsStoryModeCampaign(campaign) || _warned) return;
                _warned = true;
                WarnAndReturnToMenu();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static bool IsStoryModeCampaign(Campaign campaign)
        {
            try
            {
                var manager = campaign.CampaignBehaviorManager;
                if (manager == null) return false;
                foreach (var behavior in manager.GetBehaviors<CampaignBehaviorBase>())
                {
                    if (behavior != null && behavior.GetType().FullName == StoryModeBehaviorFullName)
                        return true;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            return false;
        }

        // A blocking ShowInquiry fired from a campaign tick can race a loading-screen
        // transition and wedge behind an unclickable modal (issue 2) — the player is
        // then stranded with no way back to the main menu. Push the state change
        // first (always reversible), then explain what happened with a plain,
        // non-blocking message rather than a modal the player might never see.
        private static void WarnAndReturnToMenu()
        {
            ReturnToMainMenu();
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "The Darkest Night is a Sandbox tale. The scripted campaign was never rebuilt for the " +
                    "Long Night — start a Sandbox game instead, and let the dark find you there.",
                    new Color(0.8f, 0.3f, 0.3f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ReturnToMainMenu()
        {
            try
            {
                var gsm = GameStateManager.Current;
                if (gsm == null) return;
                var initial = gsm.CreateState<InitialState>();
                gsm.CleanAndPushState(initial, 0);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
