// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Chosen/ChosenQuestLog.cs
// Journal entry for "The Promise" (Faction H — the Chosen, Phase 12).
// Registered in SaveDefiner.cs (id 17) so a save taken mid-quest can write.
// Mirrors TowerRiteQuestLog.cs's shape exactly.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed class ChosenQuestLog : QuestBase
    {
        private static ChosenQuestLog _questLog;
        internal static ChosenQuestLog Current => _questLog;

        public ChosenQuestLog()
            : base("chosen_the_promise", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Promise");
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

        private JournalLog _objConquest;

        private void RebindObjective()
        {
            try
            {
                if (_objConquest == null && JournalEntries != null && JournalEntries.Count >= 2)
                    _objConquest = JournalEntries[1];
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new ChosenQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The PriestKing has spoken the vision plain at last: the creature of light promised salvation " +
                    "from the demons, but only once the Chosen reclaim enough of Calradia in Heaven's name. Every " +
                    $"town and castle won for the Chosen's crown is a stone toward that promise — {ChosenQuestMath.ConquestFiefThreshold} " +
                    "held at once, and Heaven's word comes due."));

                log._objConquest = log.AddDiscreteLog(
                    new TextObject("Grow the Chosen's holdings toward the PriestKing's promised threshold."),
                    new TextObject("Calradia Reclaimed"), 0, ChosenQuestMath.ConquestFiefThreshold, null, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogProgress(int currentFiefs)
        {
            try
            {
                RebindObjective();
                _objConquest?.UpdateCurrentProgress(ChosenQuestMath.ClampedProgress(currentFiefs));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogPromiseIsHollow()
        {
            try
            {
                AddLog(new TextObject(
                    "The threshold is met. The Chosen hold more of Calradia than they ever have — and nothing " +
                    "answers. No angel descends. No demon falls silent. The PriestKing prays over ground his own " +
                    "armies bled for and hears only the wind that was always there."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogSplit(string splitSummary)
        {
            try
            {
                AddLog(new TextObject(
                    "The certainty that held the Chosen together did not survive the silence. " +
                    (splitSummary ?? "The kingdom has broken apart.")));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Reliability closure — the Chosen were wiped out before the promise
        // ever came due (see ChosenQuestCampaignBehavior.TickConquestProgress).
        // Ends the quest instead of leaving it dangling open with no possible
        // resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "The promise never comes due. Phycaon and Lycaron fall before the wall of reclaimed ground " +
                    "was ever finished — the PriestKing's vision dies with the last of his court, still unproven."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
