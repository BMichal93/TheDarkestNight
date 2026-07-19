using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── LegionMath (Phase 7, Faction G — Legion, Western Empire) ────────────
        [Test]
        public void LegionMath_StartingTownIds_HasNineSeats()
        {
            // 2 home seats (Lageta/Ortysia) + 7 deliberate border grabs
            // (town_V6, castle_V2, castle_V7, Galend, Charas, Quyaz, Sanala)
            // — see LegionMath.cs.
            Assert.AreEqual(9, LegionMath.StartingTownIds.Length);
        }


        [Test]
        public void LegionMath_IsStartingTownId_RecognisesLagetaAndOrtysia()
        {
            Assert.IsTrue(LegionMath.IsStartingTownId("town_EW1"));  // Lageta
            Assert.IsTrue(LegionMath.IsStartingTownId("town_EW4"));  // Ortysia
            Assert.IsFalse(LegionMath.IsStartingTownId("town_EN2")); // Diathma (the Empire)
        }


        [Test]
        public void LegionMath_IsStartingTownId_IsCaseInsensitiveAndRejectsEmpty()
        {
            Assert.IsTrue(LegionMath.IsStartingTownId("TOWN_ew1"));
            Assert.IsFalse(LegionMath.IsStartingTownId(""));
            Assert.IsFalse(LegionMath.IsStartingTownId(null));
        }


        [Test]
        public void LegionMath_ShouldNudgeToRaid_RespectsChanceBoundary()
        {
            Assert.IsTrue(LegionMath.ShouldNudgeToRaid(0.0));
            Assert.IsTrue(LegionMath.ShouldNudgeToRaid(LegionMath.RaidNudgeChance - 0.001));
            Assert.IsFalse(LegionMath.ShouldNudgeToRaid(LegionMath.RaidNudgeChance));
            Assert.IsFalse(LegionMath.ShouldNudgeToRaid(0.999));
        }


        [Test]
        public void LegionMath_RaidNudgeChance_IsMarkedlyAggressive()
        {
            // "Markedly more aggressive" — the daily per-idle-party raid chance
            // must be a large, deliberate fraction, not a token nudge.
            Assert.GreaterOrEqual(LegionMath.RaidNudgeChance, 0.25);
            Assert.Less(LegionMath.RaidNudgeChance, 1.0);
        }


        [Test]
        public void LegionMath_ShouldForceWarDeclaration_RespectsToleranceBoundary()
        {
            Assert.IsFalse(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays - 1));
            Assert.IsTrue(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays));
            Assert.IsTrue(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays + 5));
        }


        [Test]
        public void LegionMath_TrainingFieldTunables_MatchTheDoubleYieldSpec()
        {
            Assert.AreEqual(1, LegionMath.TrainingFieldFocusCost);
            Assert.AreEqual(2, LegionMath.TrainingFieldSkillsGranted);
            Assert.AreEqual(1, LegionMath.TrainingFieldFocusPerSkill);
            Assert.AreEqual(9, LegionMath.TrainingSkillPoolSize);
        }


        [Test]
        public void LegionMath_PickTwoDistinctSkillIndices_AreAlwaysDistinctAndInRange()
        {
            var rng = new System.Random(12345);
            for (int i = 0; i < 500; i++)
            {
                double r1 = rng.NextDouble();
                double r2 = rng.NextDouble();
                LegionMath.PickTwoDistinctSkillIndices(r1, r2, out int first, out int second);

                Assert.GreaterOrEqual(first, 0);
                Assert.Less(first, LegionMath.TrainingSkillPoolSize);
                Assert.GreaterOrEqual(second, 0);
                Assert.Less(second, LegionMath.TrainingSkillPoolSize);
                Assert.AreNotEqual(first, second);
            }
        }


        [Test]
        public void LegionMath_PickTwoDistinctSkillIndices_HandlesEdgeRolls()
        {
            LegionMath.PickTwoDistinctSkillIndices(0.0, 0.0, out int first, out int second);
            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);

            LegionMath.PickTwoDistinctSkillIndices(0.999, 0.999, out int lastFirst, out int lastSecond);
            Assert.AreEqual(LegionMath.TrainingSkillPoolSize - 1, lastFirst);
            Assert.AreNotEqual(lastFirst, lastSecond);
        }
    }
}
