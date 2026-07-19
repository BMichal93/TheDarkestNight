using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── WolfBrothersMath tests (Phase 7, Faction A) ──────────────────────

        [Test]
        public void WolfBrothersMath_IsStartingTownId_MatchesOnlyTyalAndSibir()
        {
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("town_S5"));  // Tyal
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("town_S6"));  // Sibir
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId("town_S4")); // Varnovapol
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId("castle_S7"));
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId(null));
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId(""));
        }


        [Test]
        public void WolfBrothersMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("TOWN_s5"));
        }


        [Test]
        public void WolfBrothersMath_ClampTraitLevel_ClampsToVanillaBounds()
        {
            Assert.AreEqual(-2, WolfBrothersMath.ClampTraitLevel(-2, -1));
            Assert.AreEqual(2,  WolfBrothersMath.ClampTraitLevel(2, 1));
            Assert.AreEqual(-1, WolfBrothersMath.ClampTraitLevel(0, WolfBrothersMath.JoinMercyShift));
            Assert.AreEqual(1,  WolfBrothersMath.ClampTraitLevel(0, WolfBrothersMath.JoinValorShift));
        }


        [Test]
        public void WolfBrothersMath_ApplyReputationLoss_SubtractsAndFloorsAtZero()
        {
            Assert.AreEqual(85f, WolfBrothersMath.ApplyReputationLoss(100f), 0.001f);
            Assert.AreEqual(0f,  WolfBrothersMath.ApplyReputationLoss(5f), 0.001f);
            Assert.AreEqual(0f,  WolfBrothersMath.ApplyReputationLoss(0f), 0.001f);
        }


        [Test]
        public void WolfBrothersMath_MeatFromTroop_ScalesWithTierAndClampsHigh()
        {
            int tier0 = WolfBrothersMath.MeatFromTroop(0);
            int tier3 = WolfBrothersMath.MeatFromTroop(3);
            int tier6 = WolfBrothersMath.MeatFromTroop(6);
            int tier9 = WolfBrothersMath.MeatFromTroop(9); // above the clamp ceiling
            Assert.Greater(tier3, tier0);
            Assert.Greater(tier6, tier3);
            Assert.AreEqual(tier6, tier9, "Yield should clamp at the max tier, not keep climbing.");
        }


        [Test]
        public void WolfBrothersMath_MeatFromTroop_NegativeTierClampsToZero()
        {
            Assert.AreEqual(WolfBrothersMath.MeatFromTroop(0), WolfBrothersMath.MeatFromTroop(-3));
        }


        [Test]
        public void WolfBrothersMath_MeatFromPrisoner_AlwaysExceedsTroopAtSameTier()
        {
            for (int tier = 0; tier <= 6; tier++)
                Assert.Greater(WolfBrothersMath.MeatFromPrisoner(tier), WolfBrothersMath.MeatFromTroop(tier),
                    $"Prisoner yield should exceed troop yield at tier {tier}.");
        }


        [Test]
        public void WolfBrothersMath_Tunables_ArePositiveAndSane()
        {
            Assert.Greater(WolfBrothersMath.JoinReputationLoss, 0f);
            Assert.Greater(WolfBrothersMath.LordCannibalizeIntervalDays, 0);
            Assert.Greater(WolfBrothersMath.LordMaxPrisonersEatenPerTick, 0);
            Assert.AreEqual(2, WolfBrothersMath.StartingTownIds.Length, "Wolf Brothers should keep exactly two starting towns.");
        }
    }
}
