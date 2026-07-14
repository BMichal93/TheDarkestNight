// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntQuestLog.cs
// Journal entry for "The Great Hunt" (Faction A — Wolf Brothers, Phase 12).
// Registered in SaveDefiner.cs (id 15) so a save taken mid-quest can write.
// Mirrors GreatAwakeningQuestLog.cs's shape exactly.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed class WolfHuntQuestLog : QuestBase
    {
        private static WolfHuntQuestLog _questLog;
        internal static WolfHuntQuestLog Current => _questLog;

        public WolfHuntQuestLog()
            : base("wlfhunt_the_great_hunt", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Great Hunt");
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

        private JournalLog _objBeasts;

        private void RebindObjective()
        {
            try
            {
                if (_objBeasts == null && JournalEntries != null && JournalEntries.Count >= 2)
                    _objBeasts = JournalEntries[1];
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new WolfHuntQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Packmaster has set you the Great Hunt: three of the largest things still drawing " +
                    "breath on Wolf Brothers ground, hunted one after another, each colder than the last. " +
                    "What is done with the last kill's flesh is yours alone to decide."));

                log._objBeasts = log.AddDiscreteLog(
                    new TextObject("Hunt the named beasts, one after another."),
                    new TextObject("Beasts Hunted"), 0, WolfHuntMath.StageCount, null, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogBeastSlain(int stageIndexJustFinished)
        {
            try
            {
                RebindObjective();
                AddLog(new TextObject(
                    $"{WolfHuntCatalog.BeastName(stageIndexJustFinished)} is dead. " +
                    $"[{stageIndexJustFinished + 1}/{WolfHuntMath.StageCount}]"));
                _objBeasts?.UpdateCurrentProgress(stageIndexJustFinished + 1);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogAllBeastsSlain()
        {
            try
            {
                AddLog(new TextObject(
                    "All three are dead. Return to the Packmaster — the pack is waiting on what you do " +
                    "with the last of the flesh."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogTransformation()
        {
            try
            {
                AddLog(new TextObject(
                    "You ate what you killed. The pack has no more questions about what you are."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogBurning()
        {
            try
            {
                AddLog(new TextObject(
                    "You burned it. You are still a Kinsman — and, for the first time, the pack is not " +
                    "entirely sure what that means either."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
