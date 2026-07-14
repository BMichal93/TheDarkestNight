// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsExplorationSystem.WaitMenu.cs
//
// The Scouting-scaled wait between chambers (Requirement 12) and the
// nightfall risk that rides along with it. Mirrors SettlementEncounters.
// WaitMenu.cs / Sea's voyage wait menu exactly: AddWaitGameMenu +
// StartWait/SetTargetedWaitingTimeAndInitialProgress + a per-tick hour
// counter, with the same "not persisted — a reload mid-wait simply loses the
// pending callback" convention used everywhere else a wait-menu appears in
// this codebase.
//
// Nightfall risk: every tick checks DemonMath.IsNightHour on the wait's
// running in-game clock; the moment it flips from day to night
// (RuinsMath.NightfallCrossed) a single aggressive demon party is spawned at
// the ruin's own position via DemonSpawnCampaignBehavior.SpawnAmbushNear —
// the small, targeted hook added to Phase 1's spawner specifically for this.
// The wait itself is not interrupted (the player is still inside searching);
// the risk is what's waiting outside when they eventually leave.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public static partial class RuinsExplorationSystem
    {
        private static float _waitHoursTotal;
        private static float _waitHoursElapsed;
        private static bool  _waitDone;
        private static bool  _waitWasNight;
        private static bool  _nightfallWarned;

        private static void StartChamberWait(float hours)
        {
            _waitHoursTotal   = Math.Max(1f, hours);
            _waitHoursElapsed = 0f;
            _waitDone         = false;
            _nightfallWarned  = false;
            try { _waitWasNight = DemonMath.IsNightHour((float)CampaignTime.Now.CurrentHourInDay); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); _waitWasNight = false; }

            try { GameMenu.SwitchToMenu("ruins_wait_menu"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void RegisterWaitMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddWaitGameMenu("ruins_wait_menu", "{RUINS_WAIT_TEXT}",
                    new OnInitDelegate(WaitOnInit),
                    new OnConditionDelegate(WaitOnCondition),
                    new OnConsequenceDelegate(WaitOnConsequence),
                    new OnTickDelegate(WaitOnTick),
                    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                    GameMenu.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void WaitOnInit(MenuCallbackArgs args)
        {
            try
            {
                UpdateWaitText();
                args.MenuContext.GameMenu.StartWait();
                args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(
                    Math.Max(1f, _waitHoursTotal), 0f);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool WaitOnCondition(MenuCallbackArgs args) => true;

        private static void WaitOnConsequence(MenuCallbackArgs args)
        {
            try
            {
                if (_waitDone) return;
                float remaining = _waitHoursTotal - _waitHoursElapsed;
                if (remaining <= 0.01f) { FinishWait(); return; }
                args.MenuContext.GameMenu.StartWait();
                args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(
                    Math.Max(1f, remaining), 0f);
            }
            catch { try { FinishWait(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } }
        }

        private static void WaitOnTick(MenuCallbackArgs args, CampaignTime dt)
        {
            try
            {
                if (_waitDone) return;
                _waitHoursElapsed += (float)dt.ToHours;

                try
                {
                    bool isNight = DemonMath.IsNightHour((float)CampaignTime.Now.CurrentHourInDay);
                    if (RuinsMath.NightfallCrossed(_waitWasNight, isNight) && !_nightfallWarned)
                    {
                        _nightfallWarned = true;
                        RaiseNightfallRisk();
                    }
                    _waitWasNight = isNight;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                UpdateWaitText();
                try
                {
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(
                        Math.Min(1f, _waitHoursElapsed / Math.Max(1f, _waitHoursTotal)));
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                if (_waitHoursElapsed >= _waitHoursTotal)
                    FinishWait();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Nightfall risk (Requirement 12) ─────────────────────────────────
        private static void RaiseNightfallRisk()
        {
            try
            {
                if (_activeRuin == null) return;
                string biome = "";
                try { biome = _activeRuin.Culture?.StringId ?? ""; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                DemonSpawnCampaignBehavior.SpawnAmbushNear(_activeRuin.GetPosition2D, biome);

                InformationManager.DisplayMessage(new InformationMessage(
                    "Night has fallen outside. Something has risen near the gate — whatever you found in here, you will have to leave past it.",
                    new Color(0.65f, 0.15f, 0.12f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void UpdateWaitText()
        {
            try
            {
                int left = Math.Max(0, (int)Math.Ceiling(_waitHoursTotal - _waitHoursElapsed));
                string place = _activeRuin?.Name?.ToString() ?? "the ruin";
                MBTextManager.SetTextVariable("RUINS_WAIT_TEXT",
                    $"You search deeper into {place}, room by room, hour by hour. About {left} hour(s) remain before the next chamber gives up its secrets.");
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void FinishWait()
        {
            if (_waitDone) return;
            _waitDone = true;
            try { AdvanceAfterWait(); }
            finally { try { GameMenu.ExitToLast(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } }
        }
    }
}
