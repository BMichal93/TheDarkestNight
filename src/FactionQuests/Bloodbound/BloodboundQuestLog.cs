// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Bloodbound/BloodboundQuestLog.cs
// Journal entry for "The Surpassing Rite" (Faction D — the Bloodbound, Phase 12).
// Registered in SaveDefiner.cs (id 19) so a save taken mid-quest can write.
// Mirrors ForestWidowsQuestLog.cs / ChosenQuestLog.cs's shape.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed class BloodboundQuestLog : QuestBase
    {
        private static BloodboundQuestLog _questLog;
        internal static BloodboundQuestLog Current => _questLog;

        public BloodboundQuestLog()
            : base("bloodbound_the_surpassing_rite", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("The Surpassing Rite");
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

        private JournalLog _objDonation;

        private void RebindObjective()
        {
            try
            {
                if (_objDonation == null && JournalEntries != null && JournalEntries.Count >= 2)
                    _objDonation = JournalEntries[1];
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void Start()
        {
            try
            {
                var log = new BloodboundQuestLog();
                log.StartQuest();
                _questLog = log;

                log.AddLog(new TextObject(
                    "The Huntmaster has spoken of a rite the small workings only ever hinted at: not a vial at a " +
                    "time, but an entire draught, drunk by every Bloodhunter together — enough Demon Blood, given " +
                    $"at once, to carry a whole hall past what the dark can reach. {BloodboundQuestMath.DonationTarget:N0} " +
                    "vials, poured into the shrine at Akkalat, and the Rite is ready."));

                log._objDonation = log.AddDiscreteLog(
                    new TextObject("Pour Demon Blood into the shrine at Akkalat toward the Surpassing Rite."),
                    new TextObject("Vials Poured Into The Draught"), 0, BloodboundQuestMath.DonationTarget, null, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogProgress(int bloodDonated)
        {
            try
            {
                RebindObjective();
                _objDonation?.UpdateCurrentProgress(BloodboundQuestMath.ClampedProgress(bloodDonated));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogEndingParticipated()
        {
            try
            {
                AddLog(new TextObject(
                    "The draught is complete, and you drink with them. Whatever the blood does, it does to you " +
                    "too — and it is not gentle about it. The Bloodbound's last hunt ends the same night it truly " +
                    "began: every hall from Akkalat to Chaikand left to children too young to lead them, and the " +
                    "dark already circling what's left."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal void LogEndingRan()
        {
            try
            {
                AddLog(new TextObject(
                    "The draught is complete. You are on the road before the shrine's basin runs dry, and you do " +
                    "not look back to watch the rest of them drink. The Bloodbound's last hunt ends without you — " +
                    "every hall from Akkalat to Chaikand left to children too young to lead them, and the dark " +
                    "already circling what's left."));
                CompleteQuestWithSuccess();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Reliability closure — the Bloodbound were wiped out before the
        // draught was ever filled (see BloodboundQuestCampaignBehavior.
        // CheckFactionGoneWeeklyTick). Ends the quest instead of leaving it
        // dangling open with no possible resolution.
        internal void LogFactionGone()
        {
            try
            {
                AddLog(new TextObject(
                    "Word never has to reach Akkalat's shrine at all. Rival swords, or the dark itself, empty " +
                    "every hall the Bloodbound ever held before the draught amounts to anything — the Surpassing " +
                    "Rite dies with the last Bloodhunter who might have drunk it."));
                CompleteQuestWithFail();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
