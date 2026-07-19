using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── ApocalypseMath tests (Phase 11 — the clock of the apocalypse) ───────

        [Test]
        public void ApocalypseMath_RollNextHuntIntervalDays_WithinBounds()
        {
            var rng = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                int days = ApocalypseMath.RollNextHuntIntervalDays(rng);
                Assert.GreaterOrEqual(days, ApocalypseMath.MinHuntIntervalDays);
                Assert.LessOrEqual(days, ApocalypseMath.MaxHuntIntervalDays);
            }
        }


        [Test]
        public void ApocalypseMath_RollNextHuntIntervalDays_NullRngReturnsMin()
        {
            Assert.AreEqual(ApocalypseMath.MinHuntIntervalDays, ApocalypseMath.RollNextHuntIntervalDays(null));
        }


        [Test]
        public void ApocalypseMath_HuntPartyBodyCount_ScalesUp()
        {
            Assert.Greater(ApocalypseMath.HuntPartyBodyCount(10), 10);
            Assert.AreEqual(18, ApocalypseMath.HuntPartyBodyCount(10));
        }


        [Test]
        public void ApocalypseMath_EscalationStages_GatedByDay()
        {
            Assert.IsFalse(ApocalypseMath.IsRumourStage(299));
            Assert.IsTrue(ApocalypseMath.IsRumourStage(300));
            Assert.IsFalse(ApocalypseMath.IsGatheringStage(599));
            Assert.IsTrue(ApocalypseMath.IsGatheringStage(600));
            Assert.IsFalse(ApocalypseMath.IsLordEligible(999));
            Assert.IsTrue(ApocalypseMath.IsLordEligible(1000));
        }


        [Test]
        public void ApocalypseMath_GatheringSizeAfterWeeks_GrowsAndCaps()
        {
            Assert.AreEqual(ApocalypseMath.GatheringInitialSize, ApocalypseMath.GatheringSizeAfterWeeks(0));
            Assert.AreEqual(
                ApocalypseMath.GatheringInitialSize + ApocalypseMath.GatheringWeeklyGrowth * 3,
                ApocalypseMath.GatheringSizeAfterWeeks(3));
            Assert.AreEqual(ApocalypseMath.GatheringMaxSize, ApocalypseMath.GatheringSizeAfterWeeks(10000));
        }


        [Test]
        public void ApocalypseMath_RollDemonLordAppears_RespectsThreshold()
        {
            Assert.IsTrue(ApocalypseMath.RollDemonLordAppears(0.0));
            Assert.IsFalse(ApocalypseMath.RollDemonLordAppears(ApocalypseMath.DemonLordAppearChancePerWeek));
            Assert.IsFalse(ApocalypseMath.RollDemonLordAppears(0.9999));
        }


        [Test]
        public void ApocalypseMath_DemonLordHostSizeAfterWeeks_GrowsAndCaps()
        {
            Assert.AreEqual(ApocalypseMath.DemonLordHostInitialSize, ApocalypseMath.DemonLordHostSizeAfterWeeks(0));
            Assert.Greater(ApocalypseMath.DemonLordHostSizeAfterWeeks(5), ApocalypseMath.DemonLordHostInitialSize);
            Assert.AreEqual(ApocalypseMath.DemonLordHostMaxSize, ApocalypseMath.DemonLordHostSizeAfterWeeks(100000));
        }


        [Test]
        public void ApocalypseMath_DemonLordBaneMultiplier_LessThanOrdinaryDemonBane()
        {
            // He resists the demon-bane bonus — his multiplier must sit below the
            // ordinary demon's (RelicMath.DemonBaneMultiplier), but not below 1.0
            // (he should never take LESS than unenchanted damage).
            Assert.Less(ApocalypseMath.DemonLordBaneMultiplier, RelicMath.DemonBaneMultiplier);
            Assert.GreaterOrEqual(ApocalypseMath.DemonLordBaneMultiplier, 1.0f);
        }


        [Test]
        public void ApocalypseMath_IsDefeatBySettlements_TriggersAtFraction()
        {
            Assert.IsFalse(ApocalypseMath.IsDefeatBySettlements(49, 100));
            Assert.IsTrue(ApocalypseMath.IsDefeatBySettlements(50, 100));
            Assert.IsFalse(ApocalypseMath.IsDefeatBySettlements(0, 0));
        }


        [Test]
        public void ApocalypseMath_IsDefeatByElimination_TriggersAtZero()
        {
            Assert.IsFalse(ApocalypseMath.IsDefeatByElimination(1));
            Assert.IsTrue(ApocalypseMath.IsDefeatByElimination(0));
        }
    }
}
