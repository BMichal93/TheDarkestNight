using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── UnitsMath (Phase 3 — Requirements 4, 29, 30) ────────────────────────

        [Test]
        public void UnitsMath_RequiresPromotionToll_OnlyTiersFourAndFive()
        {
            Assert.IsFalse(UnitsMath.RequiresPromotionToll(1));
            Assert.IsFalse(UnitsMath.RequiresPromotionToll(3));
            Assert.IsTrue(UnitsMath.RequiresPromotionToll(4));
            Assert.IsTrue(UnitsMath.RequiresPromotionToll(5));
        }


        [Test]
        public void UnitsMath_IsGoodPriceWeapon_ThresholdAtMinValue()
        {
            Assert.IsFalse(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue - 1));
            Assert.IsTrue(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue));
            Assert.IsTrue(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue + 500));
        }


        [Test]
        public void UnitsMath_IsShabbyGearTier_OnlyThreeAndFour()
        {
            Assert.IsFalse(UnitsMath.IsShabbyGearTier(2));
            Assert.IsTrue(UnitsMath.IsShabbyGearTier(3));
            Assert.IsTrue(UnitsMath.IsShabbyGearTier(4));
            Assert.IsFalse(UnitsMath.IsShabbyGearTier(5));
        }


        [Test]
        public void UnitsMath_NeedsGearDowngrade_StrictlyAboveCap()
        {
            Assert.IsFalse(UnitsMath.NeedsGearDowngrade(300, 300));
            Assert.IsTrue(UnitsMath.NeedsGearDowngrade(301, 300));
            Assert.IsFalse(UnitsMath.NeedsGearDowngrade(0, 300));
        }


        [Test]
        public void UnitsMath_IsTier5Recruit_OnlyTierFivePlus()
        {
            Assert.IsFalse(UnitsMath.IsTier5Recruit(4));
            Assert.IsTrue(UnitsMath.IsTier5Recruit(5));
            Assert.IsTrue(UnitsMath.IsTier5Recruit(6));
        }


        [Test]
        public void UnitsMath_IsOrnateLordGear_ByValueOrModifier()
        {
            Assert.IsFalse(UnitsMath.IsOrnateLordGear(100, false));
            Assert.IsTrue(UnitsMath.IsOrnateLordGear(UnitsMath.WearyLordValueCap + 1, false));
            Assert.IsTrue(UnitsMath.IsOrnateLordGear(50, true)); // cheap but "fine"/masterwork — still ornate
            Assert.IsFalse(UnitsMath.IsOrnateLordGear(UnitsMath.WearyLordValueCap, false)); // exactly at cap is not "over"
        }
    }
}
