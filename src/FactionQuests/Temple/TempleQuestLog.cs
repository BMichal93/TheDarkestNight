// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestLog.cs
// Journal entry for "The Unbroken Vow" (Faction E — the Temple, Phase 12).
// Registered in SaveDefiner.cs (id 20) so a save taken mid-quest can write.
// Mirrors BloodboundQuestLog.cs/TowerRiteQuestLog.cs's shape, but tracks TWO
// discrete objectives in sequence (the five Vigils, then the kill tally)
// rather than one.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed class TempleQuestLog : QuestBase
    {
        private static TempleQuestLog _questLog;
        internal static TempleQuestLog Current => _questLog;

        public TempleQuestLog()
            : base("temple_the_unbroken_vow", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Unbroken Vow");
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

        private JournalLog _objVigils;
        private JournalLog _objKills;

        private void RebindObjectives()
        {
            try
            {
                if (JournalEntries == null) return;
                if (_objVigils == null && JournalEntries.Count >= 2) _objVigils = JournalEntries[1];
                if (_objKills == null && JournalEntries.Count >= 3) _objKills = JournalEntries[2];
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new TempleQuestLog();
                log.StartQuest();
                _questLog = log;

                int required = TempleQuestCampaignBehavior.RequiredArtifactCount();

                log.AddLog(new TextObject(
                    "The Grand-Master has spoken of the Five Vigils — relics scattered across ruined halls, " +
                    "which the old texts swear were never meant to be held apart. Find them all, and bring them " +
                    "to a Temple town."));

                log._objVigils = log.AddDiscreteLog(
                    new TextObject("Find the Five Vigils, hidden in the ruins of the old world, and carry them to a Temple town."),
                    new TextObject("Vigils Found"), 0, required > 0 ? required : TempleQuestMath.ArtifactCount, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogArtifactFound(int found, int required)
        {
            try
            {
                RebindObjectives();
                _objVigils?.UpdateCurrentProgress(found);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogAllFound()
        {
            try
            {
                AddLog(new TextObject(
                    "Every Vigil the trail promised is in your saddlebags. Bring them to Ocs Hall or Pravend, and " +
                    "the Grand-Master will perform the Rite of Union."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogDelivered()
        {
            try
            {
                AddLog(new TextObject(
                    "The Five Vigils are laid before the Grand-Master. The Rite of Union is spoken."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogBound()
        {
            try
            {
                AddLog(new TextObject(
                    "The Unbroken Vow is sealed. Every Templar sword and banner marches as one host now — a war " +
                    "of attrition against the Night Tide with no clever ending written into it. There is no " +
                    "salvation here, only the fight: the Order will not stop marching until the dark is gone, or " +
                    "it is. Reaching a full count of the dead — should it ever truly happen — would be a hollow, " +
                    "almost impossible thing to celebrate, not a victory to grind toward."));

                _objKills = AddDiscreteLog(
                    new TextObject("March with — or simply witness — the Unbroken Vow's war of attrition against the Night Tide."),
                    new TextObject("Demons Fallen Before the Host"), 0, TempleQuestMath.KillTarget, null, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogKillProgress(int demonsKilled)
        {
            try
            {
                RebindObjectives();
                _objKills?.UpdateCurrentProgress(demonsKilled);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal void LogDisbanded()
        {
            try
            {
                AddLog(new TextObject(
                    "The tally is somehow, impossibly, complete. It changes nothing about how much dark is left " +
                    "in the world — only how few Templars remain to keep counting it. The Unbroken Vow does not " +
                    "break so much as it simply runs out of people willing to keep swearing it. The Order " +
                    "disbands, hall by hall, and the Temple's name outlives the Temple itself."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Reliability closure — the Temple/Vlandia was wiped out before the
        // Vow could be kept or broken (see TempleQuestCampaignBehavior.
        // CheckFactionGoneDailyTick). Ends the quest instead of leaving it
        // dangling open, waiting on a host that no longer exists.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "There is no Order left to keep the Vow, broken or unbroken. Ocs Hall and Pravend both fall, " +
                    "and whatever the Five Vigils might have bound together goes unbound, and unfinished."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
