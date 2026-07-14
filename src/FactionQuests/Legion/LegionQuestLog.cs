// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestLog.cs
// Journal entry for "The Far Shore" (Faction G — Legion, Phase 12).
// Registered in SaveDefiner.cs (id 22) so a save taken mid-quest can write.
// Mirrors EmpireQuestLog.cs's shape — a single discrete objective, then a
// resolution log entry for whichever ending fires.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed class LegionQuestLog : QuestBase
    {
        private static LegionQuestLog _questLog;
        internal static LegionQuestLog Current => _questLog;

        public LegionQuestLog()
            : base("legion_the_far_shore", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Far Shore");
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

        private JournalLog _objStock;

        private void RebindObjectives()
        {
            try
            {
                if (JournalEntries == null) return;
                if (_objStock == null && JournalEntries.Count >= 2) _objStock = JournalEntries[1];
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new LegionQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Warlord has said, plainly and to your face, the thing every other Legion Comrade only " +
                    "mutters over the fire: this land cannot be held, only raided a little longer before it takes " +
                    "everything back. He means to build an ark at Ortysia and sail beyond the sea, past whatever " +
                    "is left to find there, before the Long Night finishes what it started. Bring hardwood and " +
                    "iron to Ortysia — a very significant stock of both — until the ark is truly ready."));

                log._objStock = log.AddDiscreteLog(
                    new TextObject("Stock the ark at Ortysia with hardwood and iron."),
                    new TextObject("The Ark's Stock (blended %)"), 0, 100, null, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogStockProgress(int hardwood, int iron)
        {
            try
            {
                RebindObjectives();
                int pct = (int)(LegionQuestMath.BlendedProgress(hardwood, iron) * 100f);
                _objStock?.UpdateCurrentProgress(pct);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogSailed()
        {
            try
            {
                AddLog(new TextObject(
                    "The ark stands ready at Ortysia's quay, stocked past any reasonable margin. You go aboard " +
                    "with everyone who chose to follow, and Ortysia's harbour falls away behind the wake. " +
                    "Whatever lies beyond the sea, it is no longer the Long Night's to claim."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogStayed()
        {
            try
            {
                AddLog(new TextObject(
                    "The ark stands at Ortysia's quay, and you do not board it. Legion needed a Warlord who would " +
                    "stay and hold the line more than it ever needed a ship — so you are that Warlord now. Not " +
                    "everyone who called the old Warlord Comrade is willing to call you the same; some columns " +
                    "march out from Ortysia and do not come back to it."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Reliability closure — Legion was wiped out before the ark was ever
        // finished (see LegionQuestCampaignBehavior.CheckFactionGoneWeeklyTick).
        // Ends the quest instead of leaving it dangling open, decaying toward
        // zero with no possible resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "The ark never leaves the yard. Lageta and Ortysia both fall before the hold is stocked — " +
                    "the Warlord's far shore stays exactly that: far, and now unreachable."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
