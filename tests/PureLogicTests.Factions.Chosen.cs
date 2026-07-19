using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── ChosenMath tests (Phase 7, Faction H) ───────────────────────────────

        [Test]
        public void ChosenMath_IsStartingTownId_MatchesOnlyPhycaonAndLycaron()
        {
            Assert.IsTrue(ChosenMath.IsStartingTownId("town_ES4"));  // Lycaron
            Assert.IsTrue(ChosenMath.IsStartingTownId("town_ES6"));  // Phycaon
            Assert.IsFalse(ChosenMath.IsStartingTownId("town_ES1"));
            Assert.IsFalse(ChosenMath.IsStartingTownId("town_ES2"));
            Assert.IsFalse(ChosenMath.IsStartingTownId("town_ES3"));
            Assert.IsFalse(ChosenMath.IsStartingTownId("town_ES5"));
            Assert.IsFalse(ChosenMath.IsStartingTownId(null));
            Assert.IsFalse(ChosenMath.IsStartingTownId(""));
            // 2 home seats (Lycaron/Phycaon) + 2 deliberate border grabs
            // (Razih, Qasira) — Akkalat is deliberately NOT included, see
            // ChosenMath.cs's note on the Bloodbound seat conflict.
            Assert.AreEqual(4, ChosenMath.StartingTownIds.Length, "The Chosen should keep exactly four starting towns.");
        }


        [Test]
        public void ChosenMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(ChosenMath.IsStartingTownId("TOWN_es4"));
            Assert.IsTrue(ChosenMath.IsStartingTownId("Town_Es6"));
        }


        [Test]
        public void ChosenMath_ShouldNudgeToRaid_RespectsChanceBoundary()
        {
            Assert.IsTrue(ChosenMath.ShouldNudgeToRaid(0.0));
            Assert.IsFalse(ChosenMath.ShouldNudgeToRaid(ChosenMath.RaidNudgeChance));
            Assert.IsFalse(ChosenMath.ShouldNudgeToRaid(0.999));
        }


        [Test]
        public void ChosenMath_ShouldNudgeToSiege_RespectsChanceBoundary()
        {
            Assert.IsTrue(ChosenMath.ShouldNudgeToSiege(0.0));
            Assert.IsFalse(ChosenMath.ShouldNudgeToSiege(ChosenMath.SiegeNudgeChance));
            Assert.IsFalse(ChosenMath.ShouldNudgeToSiege(0.999));
        }


        [Test]
        public void ChosenMath_IsMoreAggressiveThanLegion()
        {
            // "Very expansive" — noticeably more aggressive than Legion: a
            // higher raid-nudge chance and a shorter peace tolerance.
            Assert.Greater(ChosenMath.RaidNudgeChance, LegionMath.RaidNudgeChance);
            Assert.Less(ChosenMath.PeaceToleranceDays, LegionMath.PeaceToleranceDays);
        }


        [Test]
        public void ChosenMath_ShouldForceWarDeclaration_RespectsPeaceTolerance()
        {
            Assert.IsFalse(ChosenMath.ShouldForceWarDeclaration(ChosenMath.PeaceToleranceDays - 1));
            Assert.IsTrue(ChosenMath.ShouldForceWarDeclaration(ChosenMath.PeaceToleranceDays));
            Assert.IsTrue(ChosenMath.ShouldForceWarDeclaration(ChosenMath.PeaceToleranceDays + 5));
        }


        [Test]
        public void ChosenMath_RollWifeAge_StaysWithinAdultRange()
        {
            for (double roll = 0.0; roll <= 1.0; roll += 0.05)
            {
                int age = ChosenMath.RollWifeAge(roll);
                Assert.GreaterOrEqual(age, ChosenMath.PlayerWifeMinAge);
                Assert.Less(age, ChosenMath.PlayerWifeMinAge + ChosenMath.PlayerWifeMaxAgeSpan);
            }
        }


        [Test]
        public void ChosenMath_RodPurchaseCost_IsRelicTierAboveTheHolySigil()
        {
            Assert.Greater(ChosenMath.RodPurchaseCostGold, TempleMath.SigilPurchaseCostGold * 10);
        }


        [Test]
        public void ChosenMath_WifeCaps_ArePositiveAndReasonable()
        {
            Assert.Greater(ChosenMath.PriestKingWifeMax, 0);
            Assert.Greater(ChosenMath.PlayerWifeMax, 0);
            Assert.LessOrEqual(ChosenMath.PriestKingWifeMax, 20);
            Assert.LessOrEqual(ChosenMath.PlayerWifeMax, 20);
        }
    }
}
