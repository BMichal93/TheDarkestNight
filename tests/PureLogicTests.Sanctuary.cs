using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── SanctuaryMath: the Long Vigil (raise a trait for gold or blood) ────

        [Test]
        public void SanctuaryMath_CanRaiseTrait_BelowCap_ReturnsTrue()
        {
            Assert.IsTrue(SanctuaryMath.CanRaiseTrait(-2));
            Assert.IsTrue(SanctuaryMath.CanRaiseTrait(1));
        }


        [Test]
        public void SanctuaryMath_CanRaiseTrait_AtCap_ReturnsFalse()
        {
            Assert.IsFalse(SanctuaryMath.CanRaiseTrait(SanctuaryMath.TraitCap));
        }


        [Test]
        public void SanctuaryMath_CommunionCosts_ScaleWithTargetLevel()
        {
            int lowGold  = SanctuaryMath.CommunionGoldCost(-1);
            int highGold = SanctuaryMath.CommunionGoldCost(2);
            Assert.Greater(highGold, lowGold, "Reaching a higher trait level should cost more gold.");

            int lowHp  = SanctuaryMath.CommunionHpCost(-1);
            int highHp = SanctuaryMath.CommunionHpCost(2);
            Assert.Greater(highHp, lowHp, "Reaching a higher trait level should cost more HP.");
        }


        [Test]
        public void SanctuaryMath_CommunionCosts_ArePositive()
        {
            for (int target = -1; target <= SanctuaryMath.TraitCap; target++)
            {
                Assert.Greater(SanctuaryMath.CommunionGoldCost(target), 0);
                Assert.Greater(SanctuaryMath.CommunionHpCost(target), 0);
            }
        }
    }
}
