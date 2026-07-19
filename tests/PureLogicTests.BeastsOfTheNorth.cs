using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── BeastsOfTheNorthMath ─────────────────────────────────────────────
        [Test]
        public void BeastsOfTheNorthMath_GiantCost_WithinAskedRange()
        {
            Assert.GreaterOrEqual(BeastsOfTheNorthMath.GiantFishCost, 40);
            Assert.LessOrEqual(BeastsOfTheNorthMath.GiantFishCost, 60);
        }


        [Test]
        public void BeastsOfTheNorthMath_WolfRiderCost_WithinAskedRange()
        {
            Assert.GreaterOrEqual(BeastsOfTheNorthMath.WolfRiderFishCost, 25);
            Assert.LessOrEqual(BeastsOfTheNorthMath.WolfRiderFishCost, 40);
        }


        [Test]
        public void BeastsOfTheNorthMath_GiantCostsMoreThanWolfRider()
        {
            Assert.Greater(BeastsOfTheNorthMath.GiantFishCost, BeastsOfTheNorthMath.WolfRiderFishCost);
            Assert.Greater(BeastsOfTheNorthMath.GiantGoldCost, BeastsOfTheNorthMath.WolfRiderGoldCost);
        }


        [Test]
        public void BeastsOfTheNorthMath_HasCapRemaining_RespectsCap()
        {
            Assert.IsTrue(BeastsOfTheNorthMath.HasCapRemaining(0, BeastsOfTheNorthMath.GiantMonthlyCap));
            Assert.IsTrue(BeastsOfTheNorthMath.HasCapRemaining(BeastsOfTheNorthMath.GiantMonthlyCap - 1, BeastsOfTheNorthMath.GiantMonthlyCap));
            Assert.IsFalse(BeastsOfTheNorthMath.HasCapRemaining(BeastsOfTheNorthMath.GiantMonthlyCap, BeastsOfTheNorthMath.GiantMonthlyCap));

            Assert.IsTrue(BeastsOfTheNorthMath.HasCapRemaining(0, BeastsOfTheNorthMath.WolfRiderMonthlyCap));
            Assert.IsFalse(BeastsOfTheNorthMath.HasCapRemaining(BeastsOfTheNorthMath.WolfRiderMonthlyCap, BeastsOfTheNorthMath.WolfRiderMonthlyCap));
        }


        [Test]
        public void BeastsOfTheNorthMath_CanAffordFish_ExactBoundary()
        {
            Assert.IsTrue(BeastsOfTheNorthMath.CanAffordFish(BeastsOfTheNorthMath.GiantFishCost, BeastsOfTheNorthMath.GiantFishCost));
            Assert.IsFalse(BeastsOfTheNorthMath.CanAffordFish(BeastsOfTheNorthMath.GiantFishCost - 1, BeastsOfTheNorthMath.GiantFishCost));
        }


        [Test]
        public void BeastsOfTheNorthMath_CanAffordGold_ExactBoundary()
        {
            Assert.IsTrue(BeastsOfTheNorthMath.CanAffordGold(BeastsOfTheNorthMath.GiantGoldCost, BeastsOfTheNorthMath.GiantGoldCost));
            Assert.IsFalse(BeastsOfTheNorthMath.CanAffordGold(BeastsOfTheNorthMath.GiantGoldCost - 1, BeastsOfTheNorthMath.GiantGoldCost));
        }
    }
}
