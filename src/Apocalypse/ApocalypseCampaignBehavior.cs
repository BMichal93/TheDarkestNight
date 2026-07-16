// =============================================================================
// THE DARKEST NIGHT — Apocalypse/ApocalypseCampaignBehavior.cs
//
// Phase 11 — "the clock of the apocalypse" (requirements 32, 33). Owns:
//   • The Night of the Hunt (requirement 32) — a periodic, far worse night.
//   • Stage 1 — day-300 rumours (requirement 33).
// Stage 2 (the Gathering, day 600) lives in the .Gathering.cs partial; stage 3
// (the Demon Lord) and victory/defeat live in the .Resolution.cs partial and
// DemonLordSystem.cs. Structure mirrors CampaignMapEvents.Portents.cs (portent
// flags fire once, persisted) and the Ashen resurgence daily-tick pattern in
// MagicCampaignBehavior ("roll an interval, then act").
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class ApocalypseCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Night of the Hunt (requirement 32) ──────────────────────────────────
        private static int  _nextHuntDay = -1;
        private static bool _huntPortentShown = false;

        // ── Stage 1 — rumours (requirement 33) ──────────────────────────────────
        private static bool _rumourPortentShown = false;

        // True from day 300 onward for the rest of the campaign — consulted by
        // TavernCampaignBehavior.Rumors.cs to add the apocalypse rumour bucket.
        public static bool RumoursActive { get; private set; } = false;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try { dataStore.SyncData("APOC_NextHuntDay",      ref _nextHuntDay); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { dataStore.SyncData("APOC_HuntPortentShown", ref _huntPortentShown); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { dataStore.SyncData("APOC_RumourPortentShown", ref _rumourPortentShown); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { dataStore.SyncData("APOC_RumoursActive", ref _rumoursActiveField); RumoursActive = _rumoursActiveField; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SyncGatheringData(dataStore); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SyncResolutionData(dataStore); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { DemonLordSystem.SyncData(dataStore); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Backing field synced alongside the public RumoursActive property (auto
        // properties can't be passed by ref to SyncData).
        private static bool _rumoursActiveField = false;

        public static void ResetForNewGame()
        {
            _nextHuntDay = -1;
            _huntPortentShown = false;
            _rumourPortentShown = false;
            RumoursActive = false;
            _rumoursActiveField = false;
            ResetGatheringForNewGame();
            ResetResolutionForNewGame();
            DemonLordSystem.ResetForNewGame();
        }

        private static int CurrentDay()
        {
            try { return (int)CampaignTime.Now.ToDays; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }

        private void OnDailyTick()
        {
            try { TickNightOfTheHunt(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickRumourStage(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { GatheringWeeklyTick(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { DemonLordSystem.WeeklyTick(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TryRollDemonLordAppearance(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickVictoryDefeatChecks(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Requirement 32 — Night of the Hunt ──────────────────────────────────
        private void TickNightOfTheHunt()
        {
            int day = CurrentDay();
            if (_nextHuntDay < 0)
            {
                // First-ever roll: give a fresh game its first interval starting now.
                _nextHuntDay = day + ApocalypseMath.RollNextHuntIntervalDays(_rng);
                return;
            }

            // Dread portent the day before it falls.
            if (!_huntPortentShown && day >= _nextHuntDay - 1 && day < _nextHuntDay)
            {
                _huntPortentShown = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    "The dogs will not settle tonight. Every hearth-fire in the village burns low and blue, " +
                    "and something out past the treeline is counting. Tomorrow night, do not be on the road.",
                    new Color(0.55f, 0.10f, 0.10f)));
            }

            if (day < _nextHuntDay) return;

            _huntPortentShown = false;
            _nextHuntDay = day + ApocalypseMath.RollNextHuntIntervalDays(_rng);
            try { FireNightOfTheHunt(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void FireNightOfTheHunt()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "The Night of the Hunt has come. The dark rises in numbers no watch can hold, " +
                "and it does not stop at the tree line.",
                new Color(0.65f, 0.05f, 0.05f)));

            for (int i = 0; i < ApocalypseMath.HuntExtraParties; i++)
            {
                try
                {
                    Vec2 anchor = DemonHuntAnchor();
                    DemonSpawnCampaignBehavior.SpawnAmbushNear(anchor);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }

            try { RaidVillagesForHunt(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static Vec2 DemonHuntAnchor()
        {
            try
            {
                var list = Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle || s.IsVillage))
                    .ToList();
                if (list.Count == 0) return default;
                return list[_rng.Next(list.Count)].GetPosition2D;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return default; }
        }

        private static void RaidVillagesForHunt()
        {
            var villages = Settlement.All
                .Where(s => s != null && s.IsVillage && s.Village != null && s.Village.Hearth > 15f)
                .OrderBy(_ => _rng.Next())
                .Take(ApocalypseMath.HuntVillagesRaided)
                .ToList();
            foreach (var v in villages)
            {
                try
                {
                    float before = v.Village.Hearth;
                    v.Village.Hearth = Math.Max(5f, before * ApocalypseMath.HuntVillageHearthFraction);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{v.Name} bore the worst of the Hunt. Hearth: {before:F0} -> {v.Village.Hearth:F0}.",
                        new Color(0.55f, 0.15f, 0.15f)));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Requirement 33, stage 1 — rumours from day 300 ──────────────────────
        private void TickRumourStage()
        {
            int day = CurrentDay();
            if (!ApocalypseMath.IsRumourStage(day)) return;

            RumoursActive = true;
            _rumoursActiveField = true;

            if (!_rumourPortentShown)
            {
                _rumourPortentShown = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    "Hunters speak of demon bands moving with purpose now — circling, not raiding. " +
                    "As if counting the walls. As if waiting for a signal.",
                    new Color(0.55f, 0.10f, 0.10f)));
            }
        }

        // ── Hero-killed hook — victory watches for the Demon Lord dying ─────────
        private void OnHeroKilled(Hero victim, Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            try { TryResolveVictory(victim); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
