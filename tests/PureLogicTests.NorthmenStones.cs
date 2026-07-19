using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── NorthmenStonesMath: The Bonefire Circle ─────────────────────────────

        [Test]
        public void NorthmenStonesMath_TriggerChance_ZeroBeforeStartDay()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay - 1));
        }


        [Test]
        public void NorthmenStonesMath_TriggerChance_RampsAndCaps()
        {
            Assert.AreEqual(0.10f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay), 1e-6f);
            Assert.AreEqual(0.20f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay + NorthmenStonesMath.TriggerStepDays), 1e-6f);
            Assert.AreEqual(1f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay + 999), 1e-6f);
        }


        [Test]
        public void NorthmenStonesMath_ClampedRatio_ClampsToZeroAndOne()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.ClampedRatio(-5, 100), 1e-6f);
            Assert.AreEqual(1f, NorthmenStonesMath.ClampedRatio(500, 100), 1e-6f);
            Assert.AreEqual(0.5f, NorthmenStonesMath.ClampedRatio(50, 100), 1e-6f);
        }


        [Test]
        public void NorthmenStonesMath_BlendedProgress_ZeroWhenNothingGiven()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.BlendedProgress(0, 0, 0, 0, 0, 0), 1e-6f);
        }


        [Test]
        public void NorthmenStonesMath_BlendedProgress_OneWhenEverythingComplete()
        {
            float progress = NorthmenStonesMath.BlendedProgress(
                NorthmenStonesMath.IronTarget, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget);
            Assert.AreEqual(1f, progress, 1e-6f);
        }


        [Test]
        public void NorthmenStonesMath_IsMaterialsComplete_RequiresEveryTrackFull()
        {
            Assert.IsFalse(NorthmenStonesMath.IsMaterialsComplete(
                NorthmenStonesMath.IronTarget - 1, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget));

            Assert.IsTrue(NorthmenStonesMath.IsMaterialsComplete(
                NorthmenStonesMath.IronTarget, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget));
        }


        [Test]
        public void NorthmenStonesMath_ApplyWeeklyDecay_LosesTenPercent()
        {
            Assert.AreEqual(900, NorthmenStonesMath.ApplyWeeklyDecay(1000));
            Assert.AreEqual(0, NorthmenStonesMath.ApplyWeeklyDecay(0));
        }


        [Test]
        public void NorthmenStonesMath_InvasionThresholds_AreAscending()
        {
            Assert.Less(NorthmenStonesMath.InvasionThresholds[0], NorthmenStonesMath.InvasionThresholds[1]);
            Assert.Less(NorthmenStonesMath.InvasionThresholds[1], NorthmenStonesMath.InvasionThresholds[2]);
        }


        [Test]
        public void NorthmenStonesMath_InvasionScaling_EscalatesByTier()
        {
            for (int tier = 0; tier < 2; tier++)
            {
                Assert.LessOrEqual(NorthmenStonesMath.InvasionBandCount(tier), NorthmenStonesMath.InvasionBandCount(tier + 1));
                Assert.Less(NorthmenStonesMath.InvasionMinStrength(tier), NorthmenStonesMath.InvasionMinStrength(tier + 1));
            }
        }


        [Test]
        public void NorthmenStonesMath_NpcContributionAmount_StaysInRange()
        {
            var rng = new Random(1234);
            for (int i = 0; i < 200; i++)
            {
                int amount = NorthmenStonesMath.NpcContributionAmount(rng, 10, 40);
                Assert.GreaterOrEqual(amount, 10);
                Assert.LessOrEqual(amount, 40);
            }
        }


        [Test]
        public void NorthmenStonesMath_EmberfallFractions_AreWithinUnitRange()
        {
            Assert.Greater(NorthmenStonesMath.EmberfallGarrisonKillFrac, 0f);
            Assert.LessOrEqual(NorthmenStonesMath.EmberfallGarrisonKillFrac, 1f);
            Assert.Greater(NorthmenStonesMath.EmberfallStatRemainingFrac, 0f);
            Assert.Less(NorthmenStonesMath.EmberfallStatRemainingFrac, 1f);
        }
    }
}
