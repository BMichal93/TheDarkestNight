using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── BloodboundMath tests (Phase 7, Faction D) ──────────────────────────

        [Test]
        public void BloodboundMath_IsStartingTownId_MatchesOnlyAkkalatAndChaikand()
        {
            Assert.IsTrue(BloodboundMath.IsStartingTownId("town_K2"));  // Akkalat
            Assert.IsTrue(BloodboundMath.IsStartingTownId("town_K5"));  // Chaikand
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K1"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K3"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K4"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId(null));
            Assert.IsFalse(BloodboundMath.IsStartingTownId(""));
            Assert.AreEqual(2, BloodboundMath.StartingTownIds.Length, "The Bloodbound should keep exactly two starting towns.");
        }


        [Test]
        public void BloodboundMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(BloodboundMath.IsStartingTownId("TOWN_k2"));
            Assert.IsTrue(BloodboundMath.IsStartingTownId("Town_K5"));
        }


        [Test]
        public void BloodboundMath_RollDemonBloodYield_StaysWithinBounds()
        {
            for (double roll = 0.0; roll < 1.0; roll += 0.05)
            {
                int yield = BloodboundMath.RollDemonBloodYield(roll);
                Assert.GreaterOrEqual(yield, BloodboundMath.MinDemonBloodPerVictory);
                Assert.LessOrEqual(yield, BloodboundMath.MaxDemonBloodPerVictory);
            }
        }


        [Test]
        public void BloodboundMath_RollDemonBloodYield_NeverZero()
        {
            // Requirement: "every demon party defeated yields 1-3 Demon Blood" — never nothing.
            Assert.Greater(BloodboundMath.RollDemonBloodYield(0.0), 0);
            Assert.Greater(BloodboundMath.RollDemonBloodYield(0.999), 0);
        }


        [Test]
        public void BloodboundMath_RollIgnoreDurationDays_StaysWithinBounds()
        {
            for (double roll = 0.0; roll < 1.0; roll += 0.05)
            {
                float days = BloodboundMath.RollIgnoreDurationDays(roll);
                Assert.GreaterOrEqual(days, BloodboundMath.MinIgnoreDays);
                Assert.LessOrEqual(days, BloodboundMath.MaxIgnoreDays);
            }
        }


        [Test]
        public void BloodboundMath_IsIgnoreActive_TrueWithinWindowFalseOutside()
        {
            Assert.IsTrue(BloodboundMath.IsIgnoreActive(0f, 4f));
            Assert.IsTrue(BloodboundMath.IsIgnoreActive(3.99f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(4f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(5f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(-1f, 4f));
        }


        [Test]
        public void BloodboundMath_IsHpBuffActive_TrueWithinWeekFalseAfter()
        {
            Assert.IsTrue(BloodboundMath.IsHpBuffActive(0f));
            Assert.IsTrue(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays - 0.01f));
            Assert.IsFalse(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays));
            Assert.IsFalse(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays + 1f));
        }


        [Test]
        public void BloodboundMath_PickPairIndex_IsBinaryAndDeterministic()
        {
            Assert.AreEqual(0, BloodboundMath.PickPairIndex(0.0));
            Assert.AreEqual(0, BloodboundMath.PickPairIndex(0.49));
            Assert.AreEqual(1, BloodboundMath.PickPairIndex(0.5));
            Assert.AreEqual(1, BloodboundMath.PickPairIndex(0.99));
        }


        [Test]
        public void BloodboundMath_MeetsPhysicalThreshold_RespectsCombinedBoundary()
        {
            Assert.IsFalse(BloodboundMath.MeetsPhysicalThreshold(4, 4)); // 8 < 10
            Assert.IsTrue(BloodboundMath.MeetsPhysicalThreshold(5, 5));  // exactly 10
            Assert.IsTrue(BloodboundMath.MeetsPhysicalThreshold(8, 6));  // 14 >= 10
        }


        [Test]
        public void BloodboundMath_HasCombatFocus_RequiresAtLeastOnePoint()
        {
            Assert.IsFalse(BloodboundMath.HasCombatFocus(0));
            Assert.IsTrue(BloodboundMath.HasCombatFocus(1));
            Assert.IsTrue(BloodboundMath.HasCombatFocus(5));
        }


        [Test]
        public void BloodboundMath_QualifiesForBloodbound_RequiresBothConditions()
        {
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(3, 3, 0));  // fails both
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(8, 8, 0));  // strong but untrained
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(2, 2, 3));  // trained but frail
            Assert.IsTrue(BloodboundMath.QualifiesForBloodbound(6, 6, 2));   // meets both
        }


        [Test]
        public void BloodboundMath_CostTunables_ArePositive()
        {
            Assert.Greater(BloodboundMath.IgnoreCostBlood, 0);
            Assert.Greater(BloodboundMath.HpBuffCostBlood, 0);
            Assert.Greater(BloodboundMath.AttributeTradeCostBlood, 0);
            Assert.Greater(BloodboundMath.HpBuffAmount, 0f);
            Assert.Greater(BloodboundMath.HpBuffDurationDays, 0f);
        }


        // ── BloodAttunementMath (Bloodbound blood-attunement, mod-author-directed) ──

        [Test]
        public void BloodAttunementMath_AttunementCostBlood_EscalatesByOnePerKnownElement()
        {
            Assert.AreEqual(1, BloodAttunementMath.AttunementCostBlood(0));
            Assert.AreEqual(2, BloodAttunementMath.AttunementCostBlood(1));
            Assert.AreEqual(3, BloodAttunementMath.AttunementCostBlood(2));
            Assert.AreEqual(4, BloodAttunementMath.AttunementCostBlood(3));
        }


        [Test]
        public void BloodAttunementMath_AttunementCostBlood_NeverGoesBelowOne_EvenForNegativeInput()
        {
            Assert.AreEqual(1, BloodAttunementMath.AttunementCostBlood(-5));
        }


        [Test]
        public void BloodAttunementMath_IsUsableHour_BlocksOnlyTheDeepDaylightCore()
        {
            // Full daylight core — blocked.
            Assert.IsFalse(BloodAttunementMath.IsUsableHour(9f));
            Assert.IsFalse(BloodAttunementMath.IsUsableHour(12f));
            Assert.IsFalse(BloodAttunementMath.IsUsableHour(16.99f));

            // Dawn, dusk, and night — usable.
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(0f));
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(8.99f));
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(17f));
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(20f));
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(23.99f));
        }


        [Test]
        public void BloodAttunementMath_IsUsableHour_MatchesTheBroaderTwilightWindow_NotDemonMathsNightBand()
        {
            // 17:00-20:00 is dusk — blocked by DemonMath.IsNightHour (starts at 20)
            // but explicitly ALLOWED by the broader blood-attunement window.
            Assert.IsFalse(DemonMath.IsNightHour(18f));
            Assert.IsTrue(BloodAttunementMath.IsUsableHour(18f));
        }


        [Test]
        public void BloodAttunementMath_RollPenalty_CoversAllFourKindsAcrossTheRollRange()
        {
            Assert.AreEqual(BloodAttunementMath.PenaltyKind.SocialDown, BloodAttunementMath.RollPenalty(0.0));
            Assert.AreEqual(BloodAttunementMath.PenaltyKind.IntellectDown, BloodAttunementMath.RollPenalty(0.26));
            Assert.AreEqual(BloodAttunementMath.PenaltyKind.DaytimeMorale, BloodAttunementMath.RollPenalty(0.51));
            Assert.AreEqual(BloodAttunementMath.PenaltyKind.DaytimeSpeed, BloodAttunementMath.RollPenalty(0.99));
        }


        [Test]
        public void BloodAttunementMath_PickElementCount_StaysWithinOneToFour()
        {
            Assert.AreEqual(1, BloodAttunementMath.PickElementCount(0.0));
            Assert.AreEqual(4, BloodAttunementMath.PickElementCount(0.99));
            for (double r = 0.0; r < 1.0; r += 0.05)
            {
                int n = BloodAttunementMath.PickElementCount(r);
                Assert.GreaterOrEqual(n, 1);
                Assert.LessOrEqual(n, 4);
            }
        }


        [Test]
        public void BloodAttunementMath_RelationPenalties_TempleIsHarsherThanEveryoneElse()
        {
            Assert.Less(BloodAttunementMath.RelationPenaltyTemple, BloodAttunementMath.RelationPenaltyOther);
            Assert.AreEqual(-15, BloodAttunementMath.RelationPenaltyTemple);
            Assert.AreEqual(-5, BloodAttunementMath.RelationPenaltyOther);
        }


        [Test]
        public void BloodAttunementMath_DaytimePenaltyMagnitudes_ArePositiveDrainAndNegativeSpeedFactor()
        {
            Assert.Greater(BloodAttunementMath.DaytimeMoraleDrainPerDay, 0f);
            Assert.Less(BloodAttunementMath.DaytimeSpeedPenaltyFactor, 0f);
        }
    }
}
