using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── GreatAwakeningMath tests ────────────────────────────────────────────

        [Test]
        public void GreatAwakeningMath_TriggerChance_ZeroBeforeDay50()
        {
            Assert.AreEqual(0f, GreatAwakeningMath.TriggerChance(0));
            Assert.AreEqual(0f, GreatAwakeningMath.TriggerChance(49));
        }


        [Test]
        public void GreatAwakeningMath_TriggerChance_StepsEvery20Days()
        {
            Assert.AreEqual(0.10f, GreatAwakeningMath.TriggerChance(50),  1e-5f);
            Assert.AreEqual(0.10f, GreatAwakeningMath.TriggerChance(69),  1e-5f);
            Assert.AreEqual(0.20f, GreatAwakeningMath.TriggerChance(70),  1e-5f);
            Assert.AreEqual(0.30f, GreatAwakeningMath.TriggerChance(90),  1e-5f);
            Assert.AreEqual(0.50f, GreatAwakeningMath.TriggerChance(130), 1e-5f);
        }


        [Test]
        public void GreatAwakeningMath_TriggerChance_CapsAt100Percent()
        {
            Assert.AreEqual(1.0f, GreatAwakeningMath.TriggerChance(50 + 20 * 9),  1e-5f);
            Assert.AreEqual(1.0f, GreatAwakeningMath.TriggerChance(100_000), 1e-5f);
        }


        [Test]
        public void GreatAwakeningMath_TriggerAllowed_OnlyAfterRiteOrFallbackDay()
        {
            // The Tower's second act waits for its first act to fail...
            Assert.IsFalse(GreatAwakeningMath.TriggerAllowed(100, false));
            Assert.IsTrue(GreatAwakeningMath.TriggerAllowed(100, true));
            // ...but a campaign that never engages the Rite still gets there.
            Assert.IsFalse(GreatAwakeningMath.TriggerAllowed(GreatAwakeningMath.FallbackTriggerDay - 1, false));
            Assert.IsTrue(GreatAwakeningMath.TriggerAllowed(GreatAwakeningMath.FallbackTriggerDay, false));
        }


        [Test]
        public void GreatAwakeningMath_PrisonerTarget_IsTenThousand()
        {
            Assert.AreEqual(10_000, GreatAwakeningMath.PrisonerTarget);
        }


        [Test]
        public void GreatAwakeningMath_NpcContributionAmount_NeverExceedsHeldOrMax()
        {
            var rng = new Random(1234);
            for (int i = 0; i < 200; i++)
            {
                int held = i % 25; // sweep small rosters, including zero
                int amount = GreatAwakeningMath.NpcContributionAmount(rng, held);
                Assert.GreaterOrEqual(amount, 0);
                Assert.LessOrEqual(amount, held);
                Assert.LessOrEqual(amount, GreatAwakeningMath.NpcContributionMax);
            }
        }


        [Test]
        public void GreatAwakeningMath_NpcContributionAmount_ZeroWhenNoPrisonersHeld()
        {
            Assert.AreEqual(0, GreatAwakeningMath.NpcContributionAmount(new Random(1), 0));
        }


        [Test]
        public void GreatAwakeningMath_ResolutionIsControlled_RoughlyHalfAndHalf()
        {
            var rng = new Random(42);
            int controlled = 0;
            const int trials = 10_000;
            for (int i = 0; i < trials; i++)
                if (GreatAwakeningMath.ResolutionIsControlled(rng)) controlled++;
            double frac = (double)controlled / trials;
            Assert.Greater(frac, 0.45);
            Assert.Less(frac, 0.55);
        }
    }
}
