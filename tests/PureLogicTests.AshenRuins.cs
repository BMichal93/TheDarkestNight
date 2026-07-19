using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── AshenRuinMath tests (ruins expansion) ──────────────────────────────

        [Test]
        public void AshenRuinMath_EmberWraithPassChance_HigherRenownIsHarder()
        {
            int lowFame  = AshenRuinMath.EmberWraithPassChance(0f);
            int highFame = AshenRuinMath.EmberWraithPassChance(4000f);
            Assert.Greater(lowFame, highFame);
            Assert.GreaterOrEqual(highFame, 20); // floor clamp
            Assert.LessOrEqual(lowFame, 75);     // ceiling clamp
        }


        [Test]
        public void AshenRuinMath_WardstoneGatePassChance_ScalesWithRoguery()
        {
            Assert.AreEqual(25, AshenRuinMath.WardstoneGatePassChance(0));
            Assert.Greater(AshenRuinMath.WardstoneGatePassChance(150), AshenRuinMath.WardstoneGatePassChance(0));
            Assert.AreEqual(90, AshenRuinMath.WardstoneGatePassChance(1000)); // ceiling clamp
        }


        [Test]
        public void AshenRuinMath_HollowChoirPassChance_AutoPassesAtTierTwo()
        {
            Assert.AreEqual(100, AshenRuinMath.HollowChoirPassChance(2));
            Assert.AreEqual(100, AshenRuinMath.HollowChoirPassChance(3));
            Assert.AreEqual(45, AshenRuinMath.HollowChoirPassChance(0));
            Assert.AreEqual(65, AshenRuinMath.HollowChoirPassChance(1));
        }


        [Test]
        public void AshenRuinMath_WeightOfAshCost_ScalesWithTier()
        {
            Assert.AreEqual(2,  AshenRuinMath.WeightOfAshCost(RuinTier.Easy));
            Assert.AreEqual(4,  AshenRuinMath.WeightOfAshCost(RuinTier.Standard));
            Assert.AreEqual(7,  AshenRuinMath.WeightOfAshCost(RuinTier.Brutal));
            Assert.AreEqual(10, AshenRuinMath.WeightOfAshCost(RuinTier.Legendary));
        }


        [Test]
        public void AshenRuinMath_TriuneReckoningFallbackPassChance_ScalesWithProficiency()
        {
            Assert.AreEqual(40, AshenRuinMath.TriuneReckoningFallbackPassChance(0));
            Assert.Greater(AshenRuinMath.TriuneReckoningFallbackPassChance(10), AshenRuinMath.TriuneReckoningFallbackPassChance(0));
            Assert.AreEqual(80, AshenRuinMath.TriuneReckoningFallbackPassChance(100)); // ceiling clamp
        }


        [Test]
        public void AshenRuinMath_ResolveShiftingHall_CoversAllThreeBands()
        {
            Assert.AreEqual(ShiftingHallOutcome.RenownGain,  AshenRuinMath.ResolveShiftingHall(0));
            Assert.AreEqual(ShiftingHallOutcome.RenownGain,  AshenRuinMath.ResolveShiftingHall(34));
            Assert.AreEqual(ShiftingHallOutcome.Desertion,   AshenRuinMath.ResolveShiftingHall(35));
            Assert.AreEqual(ShiftingHallOutcome.Desertion,   AshenRuinMath.ResolveShiftingHall(69));
            Assert.AreEqual(ShiftingHallOutcome.WhisperGain, AshenRuinMath.ResolveShiftingHall(70));
            Assert.AreEqual(ShiftingHallOutcome.WhisperGain, AshenRuinMath.ResolveShiftingHall(99));
        }


        [Test]
        public void AshenRuinMath_ShiftingHallDesertionLoss_HasAFloor()
        {
            Assert.AreEqual(4, AshenRuinMath.ShiftingHallDesertionLoss(0));
            Assert.AreEqual(4, AshenRuinMath.ShiftingHallDesertionLoss(20));
            Assert.AreEqual(10, AshenRuinMath.ShiftingHallDesertionLoss(70));
        }


        // ── AshenRuinMath.RecoveryCooldownDays (ruins recover over time) ───────

        [Test]
        public void AshenRuinMath_RecoveryCooldownDays_IsInclusiveBetween30And120()
        {
            Assert.AreEqual(30,  AshenRuinMath.RecoveryCooldownDays(0));
            Assert.AreEqual(120, AshenRuinMath.RecoveryCooldownDays(90));
            Assert.AreEqual(75,  AshenRuinMath.RecoveryCooldownDays(45));
        }


        [Test]
        public void AshenRuinMath_RecoveryCooldownDays_ClampsOutOfRangeRolls()
        {
            Assert.AreEqual(30,  AshenRuinMath.RecoveryCooldownDays(-50));
            Assert.AreEqual(120, AshenRuinMath.RecoveryCooldownDays(500));
        }


        [Test]
        public void AshenRuinDefs_AllRuinsHaveAtLeastOneChallengeAndBothRewards()
        {
            foreach (var def in AshenRuinDefs.All)
            {
                Assert.IsNotEmpty(def.VillageName, $"{def.RuinName} has no VillageName.");
                Assert.IsNotEmpty(def.Challenges, $"{def.RuinName} has no challenges.");
                Assert.IsNotNull(def.MainReward, $"{def.RuinName} has no MainReward.");
                Assert.IsNotNull(def.PartialReward, $"{def.RuinName} has no PartialReward.");
            }
        }


        [Test]
        public void AshenRuinDefs_VillageNamesAreUnique()
        {
            var names = AshenRuinDefs.All.Select(r => r.VillageName).ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count(), "Two RuinDefs share the same VillageName.");
        }
    }
}
