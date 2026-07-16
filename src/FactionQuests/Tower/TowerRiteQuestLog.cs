// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Tower/TowerRiteQuestLog.cs
// Journal entry for "The Unbinding Rite" (Faction B — the Tower, Phase 12).
// Registered in SaveDefiner.cs (id 16) so a save taken mid-quest can write.
// Mirrors WolfHuntQuestLog.cs's shape exactly.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed class TowerRiteQuestLog : QuestBase
    {
        private static TowerRiteQuestLog _questLog;
        internal static TowerRiteQuestLog Current => _questLog;

        public TowerRiteQuestLog()
            : base("towerrite_the_unbinding_rite", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Unbinding Rite");
        // Exempts the quest from the engine's cancel-on-load sweep (see GreatAwakeningQuestLog).
        public override string SpecialQuestType => "AshAndEmberQuest";
        public override bool IsRemainingTimeHidden => true;

        protected override void InitializeQuestOnGameLoad()
        {
            _questLog = this;
            RebindObjective();
        }
        protected override void RegisterEvents() { }
        protected override void SetDialogs() { }

        private JournalLog _objGather;

        private void RebindObjective()
        {
            try
            {
                if (_objGather == null && JournalEntries != null && JournalEntries.Count >= 2)
                    _objGather = JournalEntries[1];
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new TowerRiteQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Warlock means to banish the Night Tide outright. Bring the Tower three grave-goods " +
                    $"in quantity — {TowerRiteMath.GatherRelicsRequired} magical relics, " +
                    $"{TowerRiteMath.GatherDemonBloodRequired} vials of Demon Blood, " +
                    $"{TowerRiteMath.GatherHolySigilsRequired} Holy Sigils — and deliver them to Iyakis for the " +
                    "Great Rite."));

                log._objGather = log.AddDiscreteLog(
                    new TextObject("Gather the relics, the Demon Blood, and the Holy Sigils, then bring them to Iyakis."),
                    new TextObject("Grave-Goods Gathered"), 0, 3, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogTrackComplete(int tracksNowComplete)
        {
            try
            {
                RebindObjective();
                _objGather?.UpdateCurrentProgress(tracksNowComplete);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogRiteFails()
        {
            try
            {
                AddLog(new TextObject(
                    "The Great Rite is spoken. For a moment it seems to work — then the working turns inside " +
                    "out. What was meant to close a door has torn one open instead, and something vast is " +
                    "pouring through it."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogAftermath()
        {
            try
            {
                AddLog(new TextObject(
                    "The host that came through the Tower's failed rite has finally burned itself out — but " +
                    "not before it left its mark on Iyakis, and on you. The Tower does not forgive easily, and " +
                    "something of what came through still lingers near their ground."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Reliability closure — Iyakis, the Tower's only seat, falls before
        // the rite is ever performed (see TowerRiteQuestCampaignBehavior.
        // CheckFactionGoneDailyTick). Ends the quest instead of leaving it
        // dangling open with no possible resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "Iyakis falls before the altar is ever laid. The Warlock, and whatever the Tower still knew " +
                    "of the working, goes with it — the Great Rite is a question nobody is left alive to finish asking."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
