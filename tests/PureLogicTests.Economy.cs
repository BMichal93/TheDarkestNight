using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── EconomyMath tests (Phase 2 — the barter economy) ────────────────────

        [Test]
        public void EconomyMath_GoldReductionFactor_MatchesScarcityFactor()
        {
            // -0.90f => ExplainedNumber.AddFactor drops the total by 90%,
            // leaving roughly a tenth of the vanilla amount (Path B).
            Assert.AreEqual(EconomyMath.GoldScarcityFactor - 1f, EconomyMath.GoldReductionFactor, 1e-6f);
            Assert.AreEqual(-0.90f, EconomyMath.GoldReductionFactor, 1e-6f);
        }


        [Test]
        public void EconomyMath_ScaleGold_RoundsToTenthByDefaultFactor()
        {
            Assert.AreEqual(100, EconomyMath.ScaleGold(1000, EconomyMath.GoldScarcityFactor));
            Assert.AreEqual(0, EconomyMath.ScaleGold(0, EconomyMath.GoldScarcityFactor));
        }


        [Test]
        public void EconomyMath_ScaledPlunderGold_LeavesNonPositiveUntouched()
        {
            Assert.AreEqual(0, EconomyMath.ScaledPlunderGold(0));
            Assert.AreEqual(-5, EconomyMath.ScaledPlunderGold(-5));
            Assert.AreEqual(200, EconomyMath.ScaledPlunderGold(2000));
        }


        [Test]
        public void EconomyMath_ScaledRansom_IsFarBelowVanilla()
        {
            int scaled = EconomyMath.ScaledRansom(4000);
            Assert.AreEqual(200, scaled);
            Assert.Less(scaled, 4000 * EconomyMath.GoldScarcityFactor); // ransom hit harder than general scarcity
        }


        [Test]
        public void EconomyMath_ScaledReward_TrivialisesPositiveAmountsOnly()
        {
            Assert.AreEqual(-500, EconomyMath.ScaledReward(-500)); // costs untouched
            Assert.AreEqual(0, EconomyMath.ScaledReward(0));
            Assert.AreEqual(50, EconomyMath.ScaledReward(1000));
            Assert.AreEqual(EconomyMath.RewardGoldMinimum, EconomyMath.ScaledReward(1)); // never rounds to nothing
        }


        [Test]
        public void EconomyMath_ScaledGarrisonChange_HalvesPositiveAndNegativeAlike()
        {
            Assert.AreEqual(5f, EconomyMath.ScaledGarrisonChange(10f), 1e-6f);
            Assert.AreEqual(-5f, EconomyMath.ScaledGarrisonChange(-10f), 1e-6f);
        }


        [Test]
        public void EconomyMath_ScaledAutoRecruitmentCount_HalvesAndFloors()
        {
            Assert.AreEqual(2, EconomyMath.ScaledAutoRecruitmentCount(5));
            Assert.AreEqual(0, EconomyMath.ScaledAutoRecruitmentCount(0));
            Assert.AreEqual(0, EconomyMath.ScaledAutoRecruitmentCount(-3));
        }


        [Test]
        public void EconomyMath_MaxFoodStocks_ClampsToFloorForPoorTowns()
        {
            Assert.AreEqual(EconomyMath.TownFoodStockCapFloor, EconomyMath.MaxFoodStocks(0f), 1e-6f);
            Assert.AreEqual(30f, EconomyMath.MaxFoodStocks(100f), 1e-4f);
        }


        [Test]
        public void EconomyMath_TownFoodSaleQuantity_CutToATrickle()
        {
            Assert.AreEqual(5, EconomyMath.TownFoodSaleQuantity(100));
            Assert.AreEqual(0, EconomyMath.TownFoodSaleQuantity(0));
        }


        [Test]
        public void EconomyMath_TownHorseSaleQuantity_CappedNearZero()
        {
            Assert.AreEqual(1, EconomyMath.TownHorseSaleQuantity(50));
            Assert.AreEqual(0, EconomyMath.TownHorseSaleQuantity(0));
            Assert.AreEqual(1, EconomyMath.TownHorseSaleQuantity(1));
        }


        [Test]
        public void EconomyMath_TownWeaponSaleQuantity_RemovesFineGearKeepsFewCrude()
        {
            Assert.AreEqual(0, EconomyMath.TownWeaponSaleQuantity(20, EconomyMath.CrudeWeaponTierCap + 1));
            Assert.AreEqual(1, EconomyMath.TownWeaponSaleQuantity(10, EconomyMath.CrudeWeaponTierCap));
        }


        [Test]
        public void EconomyMath_ForestWeaponSaleQuantity_IsAlwaysZero()
        {
            Assert.AreEqual(0, EconomyMath.ForestWeaponSaleQuantity(100));
            Assert.AreEqual(0, EconomyMath.ForestWeaponSaleQuantity(1));
        }


        [Test]
        public void EconomyMath_TownArmorSaleQuantity_RemovesFineArmorThinsRest()
        {
            Assert.AreEqual(0, EconomyMath.TownArmorSaleQuantity(20, EconomyMath.ArmorTierCap + 1));
            Assert.AreEqual(3, EconomyMath.TownArmorSaleQuantity(10, EconomyMath.ArmorTierCap));
            Assert.AreEqual(0, EconomyMath.TownArmorSaleQuantity(0, 1));
        }


        [Test]
        public void EconomyMath_TownSellsHorsesToday_IsDeterministicAndVariesByDay()
        {
            bool a = EconomyMath.TownSellsHorsesToday("town_A", 10);
            bool b = EconomyMath.TownSellsHorsesToday("town_A", 10);
            Assert.AreEqual(a, b);

            // Roughly 1 in HorseAvailableTownFraction days/towns should sell horses.
            int trueCount = 0;
            for (int day = 0; day < 300; day++)
                if (EconomyMath.TownSellsHorsesToday("town_test", day)) trueCount++;
            Assert.Greater(trueCount, 0);
            Assert.Less(trueCount, 300);
        }


        [Test]
        public void EconomyMath_VillageFoodQuantity_BoostedButCapped()
        {
            Assert.AreEqual(160, EconomyMath.VillageFoodQuantity(100));
            Assert.AreEqual(EconomyMath.VillageFoodBoostCap, EconomyMath.VillageFoodQuantity(1000));
            Assert.AreEqual(0, EconomyMath.VillageFoodQuantity(0));
        }
    }
}
