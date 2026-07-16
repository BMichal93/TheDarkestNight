// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestLog.cs
// Journal entry for "The Final Peace" (the Forest Widows, Phase 12).
// Registered in SaveDefiner.cs (id 18) so a save taken mid-quest can write.
// Mirrors GreatAwakeningQuestLog.cs / ChosenQuestLog.cs's shape.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed class ForestWidowsQuestLog : QuestBase
    {
        private static ForestWidowsQuestLog _questLog;
        internal static ForestWidowsQuestLog Current => _questLog;

        public ForestWidowsQuestLog()
            : base("forestwidows_the_final_peace", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Final Peace");
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

        private JournalLog _objSacrifice;

        private void RebindObjective()
        {
            try
            {
                if (_objSacrifice == null && JournalEntries != null && JournalEntries.Count >= 2)
                    _objSacrifice = JournalEntries[1];
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new ForestWidowsQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Grand Widow has spoken plainly at last: the dark takes a day of quiet for every soldier " +
                    "fed to the altar, but a day is all it has ever sold. She means to buy something larger — an " +
                    $"end to the ledger itself. {ForestWidowsQuestMath.SacrificeTarget:N0} men, offered up, and " +
                    "the Widows need never bargain with the dark again."));

                log._objSacrifice = log.AddDiscreteLog(
                    new TextObject("Feed the altar toward the Grand Widow's lasting peace."),
                    new TextObject("Men Given To The Dark"), 0, ForestWidowsQuestMath.SacrificeTarget, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogProgress(int menSacrificed)
        {
            try
            {
                RebindObjective();
                _objSacrifice?.UpdateCurrentProgress(ForestWidowsQuestMath.ClampedProgress(menSacrificed));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogEndingCastOut()
        {
            try
            {
                AddLog(new TextObject(
                    "The count is paid. Something in the wood goes quiet in a way it has never been quiet before — " +
                    "and then the women who bought that silence stop being anyone you knew. You are put outside the " +
                    "gate before whatever is left of them notices you are still yourself. Marunath and Car Banseth " +
                    "are not the Widows' any longer. They belong, now and completely, to the dark they finally " +
                    "finished paying."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogEndingStayed()
        {
            try
            {
                AddLog(new TextObject(
                    "The count is paid, and you do not step back from the altar with the rest. Whatever the Widows " +
                    "bought, you bought too. The last thing that is still recognizably you is the certainty that " +
                    "this was worth it. After that there is only the quiet, and the dark, and no more ledger left " +
                    "to keep."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Reliability closure — the Forest Widows were wiped out before the
        // count was ever met (see ForestWidowsQuestCampaignBehavior.
        // CheckFactionGoneWeeklyTick). Ends the quest instead of leaving it
        // dangling open with no possible resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "The ledger never gets settled. Marunath and Car Banseth fall to someone or something else " +
                    "first — the Grand Widow's bargain dies with the last of her court, unpaid and unpayable."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
