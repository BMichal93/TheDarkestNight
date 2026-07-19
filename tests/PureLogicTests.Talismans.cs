using System;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── TalismansMath / TalismansCatalog tests (mod-author-directed) ─────

        [Test]
        public void TalismansMath_PurchaseCost_IsSlightlyBelowStandardWandPrice()
        {
            Assert.Greater(TalismansMath.TalismanPurchaseCostGold, 0);
            Assert.Less(TalismansMath.TalismanPurchaseCostGold, WandsMath.StandardWandPriceGold);
        }


        [Test]
        public void TalismansMath_ReducedSpellburnChance_NeverBelowFloor()
        {
            float reduced = TalismansMath.ReducedSpellburnChance(SpellbookMath.MinSpellburnChance);
            Assert.AreEqual(SpellbookMath.MinSpellburnChance, reduced, 0.0001f);
        }


        [Test]
        public void TalismansMath_ReducedSpellburnChance_ShavesOffTheDocumentedAmount()
        {
            float baseChance = SpellbookMath.BaseSpellburnChance;
            float reduced = TalismansMath.ReducedSpellburnChance(baseChance);
            Assert.AreEqual(baseChance - TalismansMath.UnburntTongueSpellburnReduction, reduced, 0.0001f);
        }


        [Test]
        public void TalismansMath_RollRuinTalismanLoot_RespectsChanceBoundary()
        {
            Assert.IsTrue(TalismansMath.RollRuinTalismanLoot(TalismansMath.RuinTalismanChance - 0.0001));
            Assert.IsFalse(TalismansMath.RollRuinTalismanLoot(TalismansMath.RuinTalismanChance + 0.0001));
        }


        [Test]
        public void TalismansMath_RuinTalismanChance_IsRarerThanRuinWandChance()
        {
            Assert.Less(TalismansMath.RuinTalismanChance, WandsMath.RuinWandChance);
        }


        [Test]
        public void TalismansMath_PickTalismanIndex_StaysInBounds()
        {
            var rng = new Random(29);
            for (int i = 0; i < 500; i++)
            {
                int idx = TalismansMath.PickTalismanIndex(rng.NextDouble(), TalismansCatalog.All.Count);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, TalismansCatalog.All.Count);
            }
        }


        [Test]
        public void TalismansMath_PickTalismanIndex_ZeroCount_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, TalismansMath.PickTalismanIndex(0.5, 0));
        }


        [Test]
        public void TalismansCatalog_HasFiveEntries()
        {
            Assert.AreEqual(5, TalismansCatalog.All.Count);
        }


        [Test]
        public void TalismansCatalog_AllItemIdsAreUniqueAndPrefixed()
        {
            var ids = TalismansCatalog.AllItemIds();
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), "Talisman item ids must be unique.");
            foreach (var id in ids)
                Assert.IsTrue(id.StartsWith("aae_talisman_"), $"Unexpected talisman item id: {id}");
        }


        [Test]
        public void TalismansCatalog_TryGetByItemId_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(TalismansCatalog.TryGetByItemId("not_a_talisman", out _));
            Assert.IsFalse(TalismansCatalog.TryGetByItemId(null, out _));
        }


        [Test]
        public void TalismansCatalog_IsTalismanItemId_MatchesOnlyCatalogEntries()
        {
            Assert.IsTrue(TalismansCatalog.IsTalismanItemId("aae_talisman_last_ward"));
            Assert.IsFalse(TalismansCatalog.IsTalismanItemId("aae_wand_fireball"));
        }


        [Test]
        public void TalismansCatalog_TryGet_RoundTripsToItemId()
        {
            Assert.IsTrue(TalismansCatalog.TryGet(TalismanId.CleansingBrand, out var def));
            Assert.AreEqual("aae_talisman_cleansing_brand", def.ItemId);
        }
    }
}
