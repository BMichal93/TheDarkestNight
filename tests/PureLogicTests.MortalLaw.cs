using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── MortalLawMath (Phase 10 — Mortal AI under the same law) ────────────

        [Test]
        public void MortalLawMath_IsOverFiefCap_BoundaryIsExclusive()
        {
            Assert.IsFalse(MortalLawMath.IsOverFiefCap(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.IsOverFiefCap(MortalLawMath.KingdomFiefCap + 1));
            Assert.IsFalse(MortalLawMath.IsOverFiefCap(0));
        }


        [Test]
        public void MortalLawMath_ShouldTurnAwayDefector_MirrorsFiefCap()
        {
            Assert.IsFalse(MortalLawMath.ShouldTurnAwayDefector(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.ShouldTurnAwayDefector(MortalLawMath.KingdomFiefCap + 1));
        }


        [Test]
        public void MortalLawMath_ShouldThrottleWarDeclaration_MirrorsFiefCap()
        {
            Assert.IsFalse(MortalLawMath.ShouldThrottleWarDeclaration(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.ShouldThrottleWarDeclaration(MortalLawMath.KingdomFiefCap + 1));
        }


        [Test]
        public void MortalLawMath_IsSafeFromNightFear_BoundaryIsInclusive()
        {
            Assert.IsTrue(MortalLawMath.IsSafeFromNightFear(MortalLawMath.NightSafeArmySize));
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(MortalLawMath.NightSafeArmySize - 1));
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(0));
        }


        [Test]
        public void MortalLawMath_IsPartyHungry_AtOrBelowThresholdIsHungry()
        {
            Assert.IsTrue(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold));
            Assert.IsTrue(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold - 1f));
            Assert.IsFalse(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold + 0.01f));
        }


        [Test]
        public void MortalLawMath_ShouldNudgeHungryRaid_RollBelowChanceOnly()
        {
            Assert.IsTrue(MortalLawMath.ShouldNudgeHungryRaid(0.0));
            Assert.IsFalse(MortalLawMath.ShouldNudgeHungryRaid(MortalLawMath.HungryRaidNudgeChance));
            Assert.IsFalse(MortalLawMath.ShouldNudgeHungryRaid(0.999));
        }


        [Test]
        public void MortalLawMath_MaxAllowedForTier_LowTiersUncapped()
        {
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(1, 100));
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(2, 100));
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(3, 100));
        }


        [Test]
        public void MortalLawMath_MaxAllowedForTier_HighTiersRatioCapped()
        {
            Assert.AreEqual(15, MortalLawMath.MaxAllowedForTier(4, 100));
            Assert.AreEqual(5,  MortalLawMath.MaxAllowedForTier(5, 100));
        }


        [Test]
        public void MortalLawMath_MaxAllowedForTier_ZeroTroopsIsZero()
        {
            Assert.AreEqual(0, MortalLawMath.MaxAllowedForTier(4, 0));
            Assert.AreEqual(0, MortalLawMath.MaxAllowedForTier(5, 0));
        }


        [Test]
        public void MortalLawMath_TrimExcessForTier_OnlyTrimsAboveCap()
        {
            // 100 troops, tier-5 cap is 5 (Tier5MaxRatio = 0.05f).
            Assert.AreEqual(0, MortalLawMath.TrimExcessForTier(5, 5, 100));
            Assert.AreEqual(3, MortalLawMath.TrimExcessForTier(5, 8, 100));
            Assert.AreEqual(0, MortalLawMath.TrimExcessForTier(5, 0, 100));
        }


        [Test]
        public void MortalLawMath_KingdomFiefCap_IsPositiveAndSmall()
        {
            Assert.Greater(MortalLawMath.KingdomFiefCap, 0);
            Assert.Less(MortalLawMath.KingdomFiefCap, 15); // "a small cap" per the requirement text
        }
    }
}
