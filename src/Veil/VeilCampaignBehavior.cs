// =============================================================================
// THE DARKEST NIGHT — Veil/VeilCampaignBehavior.cs
//
// The runtime clock for THE VEIL (see VeilMath.cs for the design and the pure
// rhythm/multipliers). This behavior does two things:
//
//   1. CurrentPhase() — the single shared entry point every other system reads
//      to learn which turn of the Veil it is right now. Demon spawning, the
//      elemental cast choke, the mage-lord AI, and the night-fear caution all
//      call it, so the rhythm is decided in exactly one place. It derives the
//      phase from the campaign clock (never persisted state), so it is naturally
//      reload-safe.
//
//   2. A daily announcement when the turn changes, so the player can see the
//      Veil turning and plan around it. Only the last-announced phase is
//      persisted (VEIL_LastAnnouncedPhase) so a save/load mid-season does not
//      re-announce a turn the player already saw.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public class VeilCampaignBehavior : CampaignBehaviorBase
    {
        private static int _lastAnnouncedPhase = -1;

        public static void ResetForNewGame()
        {
            _lastAnnouncedPhase = -1;
        }

        // The one shared read of "which turn of the Veil is it now." Safe from any
        // layer (campaign or mission) — it only reads the clock. Falls back to the
        // neutral Steady turn if the clock is somehow unreachable.
        public static VeilPhase CurrentPhase()
        {
            try
            {
                if (Campaign.Current == null) return VeilPhase.Steady;
                int day = (int)CampaignTime.Now.ToDays;
                return VeilMath.PhaseForDay(day);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return VeilPhase.Steady; }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("VEIL_LastAnnouncedPhase", ref _lastAnnouncedPhase);
        }

        private void OnDailyTick()
        {
            try
            {
                if (Campaign.Current == null) return;
                int day = (int)CampaignTime.Now.ToDays;
                VeilPhase phase = VeilMath.PhaseForDay(day);
                if ((int)phase == _lastAnnouncedPhase) return;

                _lastAnnouncedPhase = (int)phase;
                Announce(phase, VeilMath.DaysUntilNextPhase(day));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Announce(VeilPhase phase, int daysUntilNext)
        {
            try
            {
                // A cold silver for the Warding, a bruised red for the Thinning, a
                // plain grey for the even Steady — the same convention the Night
                // Tide's own announcements use.
                Color color;
                switch (phase)
                {
                    case VeilPhase.Thinning: color = new Color(0.70f, 0.18f, 0.15f); break;
                    case VeilPhase.Warding:  color = new Color(0.55f, 0.62f, 0.72f); break;
                    default:                 color = new Color(0.60f, 0.60f, 0.60f); break;
                }

                string text = VeilMath.PhaseDescription(phase)
                            + $" ({VeilMath.PhaseName(phase)} holds for {daysUntilNext} more day{(daysUntilNext == 1 ? "" : "s")}.)";
                InformationManager.DisplayMessage(new InformationMessage(text, color));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
