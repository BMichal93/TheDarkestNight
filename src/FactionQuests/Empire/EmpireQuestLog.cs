// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestLog.cs
// Journal entry for "The Reunification" (Faction F — the Empire, Phase 12).
// Registered in SaveDefiner.cs (id 21) so a save taken mid-quest can write.
// Mirrors TempleQuestLog.cs's shape — two discrete objectives in sequence
// (towns conquered, then the demon kill tally) — but ends in a genuine
// victory rather than a disbandment.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed class EmpireQuestLog : QuestBase
    {
        private static EmpireQuestLog _questLog;
        internal static EmpireQuestLog Current => _questLog;

        public EmpireQuestLog()
            : base("empire_the_reunification", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Reunification");
        // Exempts the quest from the engine's cancel-on-load sweep (see GreatAwakeningQuestLog).
        public override string SpecialQuestType => "AshAndEmberQuest";
        public override bool IsRemainingTimeHidden => true;

        protected override void InitializeQuestOnGameLoad()
        {
            _questLog = this;
            RebindObjectives();
        }
        protected override void RegisterEvents() { }
        protected override void SetDialogs() { }

        private JournalLog _objTowns;
        private JournalLog _objKills;

        private void RebindObjectives()
        {
            try
            {
                if (JournalEntries == null) return;
                if (_objTowns == null && JournalEntries.Count >= 2) _objTowns = JournalEntries[1];
                if (_objKills == null && JournalEntries.Count >= 3) _objKills = JournalEntries[2];
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new EmpireQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Emperor has said the old, dangerous thing plainly: Calradia broke because it was " +
                    "divided, and the only real answer to the Long Night is to put it back together under one " +
                    "crown again. Conquer cities in the Empire's name — enough of them, and the old throne can " +
                    "be claimed for real."));

                log._objTowns = log.AddDiscreteLog(
                    new TextObject("Conquer cities for the Empire, until two-thirds of Calradia's cities answer to it again."),
                    new TextObject("Cities Reclaimed"), 0, EmpireQuestMath.ConquestTownThreshold, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogTownProgress(int currentTowns)
        {
            try
            {
                RebindObjectives();
                _objTowns?.UpdateCurrentProgress(EmpireQuestMath.ClampedTownProgress(currentTowns));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogCrowned()
        {
            try
            {
                AddLog(new TextObject(
                    "Two-thirds of Calradia's cities fly the Empire's banner. In Saneopa's old hall, before every " +
                    "Legate who could reach it in time, the ruling clan's head is crowned Emperor of a reunified " +
                    "Calradia — the first true coronation since the world broke. By nightfall the newly-crowned " +
                    "Emperor turns from the throne to the horizon, and declares what every Legate already knows " +
                    "is coming: war on the dark itself."));

                _objKills = AddDiscreteLog(
                    new TextObject("Stand with the reunified Empire's war on the demons."),
                    new TextObject("Demons Slain in the Emperor's War"), 0, EmpireQuestMath.KillTarget, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogKillProgress(int demonsKilled)
        {
            try
            {
                RebindObjectives();
                _objKills?.UpdateCurrentProgress(EmpireQuestMath.ClampedKillProgress(demonsKilled));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogVictory()
        {
            try
            {
                AddLog(new TextObject(
                    "The tally is read out in every Empire town at once, and for the first time in longer than " +
                    "anyone can remember, the news from the front is simply good. The reunified Empire has proven " +
                    "what the old texts always claimed and nobody living had seen: a whole Calradia can win. The " +
                    "dark is not gone — it will never truly be gone — but it is smaller tonight than it was, and " +
                    "an Emperor's banner did that. The Legates drink to it, and mean it."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Reliability closure — the Empire was wiped out before it ever
        // reunified Calradia (see EmpireQuestCampaignBehavior.
        // TickConquestProgress). Ends the quest instead of leaving it dangling
        // open with no possible resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "The old throne stays empty. The Empire falls to rival swords or the dark itself before " +
                    "two-thirds of Calradia was ever reclaimed — reunification dies an unfinished ambition, " +
                    "not a broken promise."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
