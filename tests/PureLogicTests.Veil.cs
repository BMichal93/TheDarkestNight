using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── The Veil (seasonal magic/demon cycle) ─────────────────────────────
        [Test]
        public void VeilMath_PhaseForDay_ThreeEqualWindowsAcrossASeason()
        {
            // Days 0–6 Warding, 7–13 Steady, 14–20 Thinning.
            for (int d = 0; d <= 6; d++)
                Assert.AreEqual(VeilPhase.Warding, VeilMath.PhaseForDay(d), "day " + d);
            for (int d = 7; d <= 13; d++)
                Assert.AreEqual(VeilPhase.Steady, VeilMath.PhaseForDay(d), "day " + d);
            for (int d = 14; d <= 20; d++)
                Assert.AreEqual(VeilPhase.Thinning, VeilMath.PhaseForDay(d), "day " + d);
        }


        [Test]
        public void VeilMath_PhaseForDay_RepeatsIdenticallyEverySeason()
        {
            // The cycle must be perfectly predictable season after season.
            for (int d = 0; d < VeilMath.SeasonLengthDays * 5; d++)
                Assert.AreEqual(VeilMath.PhaseForDay(d),
                                VeilMath.PhaseForDay(d + VeilMath.SeasonLengthDays),
                                "day " + d);
        }


        [Test]
        public void VeilMath_Multipliers_StrongWhenThinWeakWhenThick()
        {
            Assert.Greater(VeilMath.MagicMultiplier(VeilPhase.Thinning), 1f);
            Assert.AreEqual(1f, VeilMath.MagicMultiplier(VeilPhase.Steady));
            Assert.Less(VeilMath.MagicMultiplier(VeilPhase.Warding), 1f);

            Assert.Greater(VeilMath.DemonSpawnMultiplier(VeilPhase.Thinning), 1f);
            Assert.Less(VeilMath.DemonSpawnMultiplier(VeilPhase.Warding), 1f);

            // Mages cast MORE when the Veil is thin (shorter cooldown = < 1).
            Assert.Less(VeilMath.MageLordCooldownMultiplier(VeilPhase.Thinning), 1f);
            Assert.Greater(VeilMath.MageLordCooldownMultiplier(VeilPhase.Warding), 1f);

            // Armies shelter MORE when the Veil is thin (higher safety bar).
            Assert.Greater(VeilMath.NightSafeSizeMultiplier(VeilPhase.Thinning), 1f);
            Assert.Less(VeilMath.NightSafeSizeMultiplier(VeilPhase.Warding), 1f);
        }


        [Test]
        public void VeilMath_ApplySpawnMultiplier_ScalesAndNeverNegative()
        {
            Assert.AreEqual(12, VeilMath.ApplySpawnMultiplier(10, VeilPhase.Thinning)); // 10×1.2
            Assert.AreEqual(6,  VeilMath.ApplySpawnMultiplier(10, VeilPhase.Warding));  // 10×0.6
            Assert.AreEqual(10, VeilMath.ApplySpawnMultiplier(10, VeilPhase.Steady));
            Assert.AreEqual(0,  VeilMath.ApplySpawnMultiplier(0,  VeilPhase.Thinning));
        }


        [Test]
        public void VeilMath_DaysUntilNextPhase_CountsDownWithinWindow()
        {
            Assert.AreEqual(7, VeilMath.DaysUntilNextPhase(0));  // full Warding window ahead
            Assert.AreEqual(1, VeilMath.DaysUntilNextPhase(6));  // last day of Warding
            Assert.AreEqual(4, VeilMath.DaysUntilNextPhase(10)); // mid Steady
            Assert.AreEqual(1, VeilMath.DaysUntilNextPhase(20)); // last day before the season rolls
        }


        [Test]
        public void VeilMath_NightFearThreshold_MovesWithPhase()
        {
            // A 100-man host: sheltered while the Veil thins (bar 180), free to
            // march while it holds thick (bar 60), on the fence at Steady (bar 120).
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(100, VeilMath.NightSafeSizeMultiplier(VeilPhase.Thinning)));
            Assert.IsTrue(MortalLawMath.IsSafeFromNightFear(100, VeilMath.NightSafeSizeMultiplier(VeilPhase.Warding)));
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(100, VeilMath.NightSafeSizeMultiplier(VeilPhase.Steady)));
        }
    }
}
