using System.Collections.Generic;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── TowerMath tests (Phase 7, Faction B) ──────────────────────────────

        [Test]
        public void TowerMath_IsStartingTownId_MatchesOnlyIyakis()
        {
            Assert.IsTrue(TowerMath.IsStartingTownId("town_A3"));   // Iyakis
            Assert.IsFalse(TowerMath.IsStartingTownId("town_A1"));  // Quyaz
            Assert.IsFalse(TowerMath.IsStartingTownId("town_A2"));  // Husn Fulq
            Assert.IsFalse(TowerMath.IsStartingTownId(null));
            Assert.IsFalse(TowerMath.IsStartingTownId(""));
            Assert.AreEqual(1, TowerMath.StartingTownIds.Length, "The Tower should keep exactly one starting town.");
        }


        [Test]
        public void TowerMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(TowerMath.IsStartingTownId("TOWN_a3"));
        }


        [Test]
        public void TowerMath_SpellInfluenceCost_ScalesWithFormulaLength()
        {
            int short5  = TowerMath.SpellInfluenceCost(5);
            int mid12   = TowerMath.SpellInfluenceCost(12);
            int long20  = TowerMath.SpellInfluenceCost(20);
            Assert.Greater(mid12, short5);
            Assert.Greater(long20, mid12);
            Assert.Greater(short5, 0);
        }


        [Test]
        public void TowerMath_SpellInfluenceCost_ClampsToFormulaBounds()
        {
            // Below the minimum and above the maximum both clamp rather than
            // running away — a malformed length can never price a lesson at 0
            // or unboundedly high.
            Assert.AreEqual(TowerMath.SpellInfluenceCost(SpellbookCatalog.MinFormulaLength), TowerMath.SpellInfluenceCost(1));
            Assert.AreEqual(TowerMath.SpellInfluenceCost(SpellbookCatalog.MaxFormulaLength), TowerMath.SpellInfluenceCost(999));
        }


        [Test]
        public void TowerMath_OfferSeedForDay_ChangesOncePerWeek()
        {
            Assert.AreEqual(TowerMath.OfferSeedForDay(0), TowerMath.OfferSeedForDay(6));
            Assert.AreNotEqual(TowerMath.OfferSeedForDay(0), TowerMath.OfferSeedForDay(7));
        }


        [Test]
        public void TowerMath_PickRandomSubset_IsDeterministicForSameSeed()
        {
            var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var a = TowerMath.PickRandomSubset(source, 6, 42);
            var b = TowerMath.PickRandomSubset(source, 6, 42);
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(6, a.Count);
        }


        [Test]
        public void TowerMath_PickRandomSubset_NeverExceedsSourceCount()
        {
            var source = new List<int> { 1, 2, 3 };
            var picked = TowerMath.PickRandomSubset(source, 6, 1);
            Assert.AreEqual(3, picked.Count);
        }


        [Test]
        public void TowerMath_PickRandomSubset_NeverDuplicatesEntries()
        {
            var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var picked = TowerMath.PickRandomSubset(source, 6, 7);
            var distinct = new HashSet<int>(picked);
            Assert.AreEqual(picked.Count, distinct.Count);
        }


        [Test]
        public void TowerMath_PickRandomSubset_HandlesEmptyOrNullSource()
        {
            Assert.AreEqual(0, TowerMath.PickRandomSubset<int>(null, 6, 1).Count);
            Assert.AreEqual(0, TowerMath.PickRandomSubset(new List<int>(), 6, 1).Count);
            Assert.AreEqual(0, TowerMath.PickRandomSubset(new List<int> { 1, 2 }, 0, 1).Count);
        }


        [Test]
        public void TowerMath_HollowChoirRankForTier_MapsAndClampsToOneThroughFive()
        {
            Assert.AreEqual(1, TowerMath.HollowChoirRankForTier(0));
            Assert.AreEqual(1, TowerMath.HollowChoirRankForTier(2));
            Assert.AreEqual(2, TowerMath.HollowChoirRankForTier(3));
            Assert.AreEqual(3, TowerMath.HollowChoirRankForTier(4));
            Assert.AreEqual(4, TowerMath.HollowChoirRankForTier(5));
            Assert.AreEqual(5, TowerMath.HollowChoirRankForTier(6));
            Assert.AreEqual(5, TowerMath.HollowChoirRankForTier(9), "Above tier 6 should still clamp to rank 5.");
        }


        [Test]
        public void TowerMath_TransmuteInfluenceCost_ScalesWithRankAndClamps()
        {
            int rank1 = TowerMath.TransmuteInfluenceCost(1);
            int rank5 = TowerMath.TransmuteInfluenceCost(5);
            Assert.Greater(rank5, rank1);
            Assert.AreEqual(TowerMath.TransmuteInfluenceCost(1), TowerMath.TransmuteInfluenceCost(0));
            Assert.AreEqual(TowerMath.TransmuteInfluenceCost(5), TowerMath.TransmuteInfluenceCost(9));
        }


        [Test]
        public void TowerMath_Tunables_ArePositiveAndSane()
        {
            Assert.Greater(TowerMath.SpellOffersPerVisit, 0);
            Assert.Greater(TowerMath.TeachInfluenceBase, 0f);
            Assert.Greater(TowerMath.TeachInfluencePerFormulaMark, 0f);
            Assert.Greater(TowerMath.TransmuteInfluenceBase, 0f);
            Assert.Greater(TowerMath.TransmuteInfluencePerRank, 0f);
            Assert.GreaterOrEqual(TowerMath.MinTransmuteTier, 2, "Requirement: tier-2+ troops only.");
            Assert.Greater(TowerMath.LordGrantedSpellCount, 0);
        }
    }
}
