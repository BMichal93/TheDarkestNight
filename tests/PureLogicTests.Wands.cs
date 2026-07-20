using System;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── WandsMath / WandsCatalog tests (mod-author-directed addition) ─────

        [Test]
        public void WandsMath_DramaticPrice_IsHigherThanStandard()
        {
            Assert.Greater(WandsMath.DramaticWandPriceGold, WandsMath.StandardWandPriceGold);
        }


        [Test]
        public void WandsMath_BothPrices_FarExceedRodPurchaseCost()
        {
            // "Price each wand well above the Rod of the Apostle's 6000 denars."
            Assert.Greater(WandsMath.StandardWandPriceGold, ChosenMath.RodPurchaseCostGold);
            Assert.Greater(WandsMath.DramaticWandPriceGold, ChosenMath.RodPurchaseCostGold);
        }


        [Test]
        public void WandsMath_PriceForTier_MatchesConstants()
        {
            Assert.AreEqual(WandsMath.StandardWandPriceGold, WandsMath.PriceForTier(WandTier.Standard));
            Assert.AreEqual(WandsMath.DramaticWandPriceGold, WandsMath.PriceForTier(WandTier.Dramatic));
        }


        [Test]
        public void WandsMath_RollRuinWandLoot_RespectsChanceBoundary()
        {
            Assert.IsTrue(WandsMath.RollRuinWandLoot(WandsMath.RuinWandChance - 0.0001));
            Assert.IsFalse(WandsMath.RollRuinWandLoot(WandsMath.RuinWandChance + 0.0001));
        }


        [Test]
        public void WandsMath_RuinWandChance_IsRarerThanRelicDrop()
        {
            Assert.Less(WandsMath.RuinWandChance, RelicMath.RuinBaseRelicChance);
        }


        [Test]
        public void WandsMath_PickWandIndex_StaysInBounds()
        {
            var rng = new Random(13);
            for (int i = 0; i < 500; i++)
            {
                int idx = WandsMath.PickWandIndex(rng.NextDouble(), WandsCatalog.All.Count);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, WandsCatalog.All.Count);
            }
        }


        [Test]
        public void WandsMath_PickWandIndex_ZeroCount_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, WandsMath.PickWandIndex(0.5, 0));
        }


        [Test]
        public void WandsMath_LordWandChances_AreRareNotGuaranteed()
        {
            Assert.Greater(WandsMath.TowerLordWandChance, 0.0);
            Assert.Less(WandsMath.TowerLordWandChance, 0.5);
            Assert.Greater(WandsMath.ChosenLordWandChance, 0.0);
            Assert.Less(WandsMath.ChosenLordWandChance, 0.5);
        }


        [Test]
        public void WandsMath_ForestLordWandChance_IsMostlyNotUniversal()
        {
            Assert.Greater(WandsMath.ForestLordWandChance, WandsMath.TowerLordWandChance);
            Assert.Greater(WandsMath.ForestLordWandChance, WandsMath.ChosenLordWandChance);
            Assert.Less(WandsMath.ForestLordWandChance, 1.0);
        }


        [Test]
        public void WandsMath_ShouldGrantLordWand_RespectsChanceBoundary()
        {
            Assert.IsTrue(WandsMath.ShouldGrantLordWand(0.05, 0.15));
            Assert.IsFalse(WandsMath.ShouldGrantLordWand(0.25, 0.15));
        }


        [Test]
        public void WandsMath_PlayerMaxCharges_IsPositiveAndBounded()
        {
            Assert.Greater(WandsMath.PlayerMaxCharges, 0);
            Assert.LessOrEqual(WandsMath.PlayerMaxCharges, 20);
        }


        // ── WandsMath shop scarcity tests (Children of the Forest prompt) ──────

        [Test]
        public void WandsMath_ShopStockSize_IsSmallerThanWholeCatalog()
        {
            Assert.Less(WandsMath.ShopStockSize, WandsCatalog.All.Count);
            Assert.Less(WandsMath.ForestShopStockSize, WandsCatalog.All.Count);
        }


        [Test]
        public void WandsMath_ForestShopStockSize_IsLargerThanOrdinaryShop()
        {
            Assert.Greater(WandsMath.ForestShopStockSize, WandsMath.ShopStockSize);
        }


        [Test]
        public void WandsMath_ShouldRestock_RespectsCadence()
        {
            Assert.IsFalse(WandsMath.ShouldRestock(0, WandsMath.ShopRestockDays - 1));
            Assert.IsTrue(WandsMath.ShouldRestock(0, WandsMath.ShopRestockDays));
            Assert.IsTrue(WandsMath.ShouldRestock(0, WandsMath.ShopRestockDays + 5));
        }


        [Test]
        public void WandsMath_RestockSeed_IsDeterministicAndVaries()
        {
            int a = WandsMath.RestockSeed("town_A", 3);
            int b = WandsMath.RestockSeed("town_A", 3);
            int c = WandsMath.RestockSeed("town_A", 4);
            int d = WandsMath.RestockSeed("town_B", 3);
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(a, d);
        }


        [Test]
        public void WandsMath_PickShopStock_StaysInBoundsAndHasNoDuplicates()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var stock = WandsMath.PickShopStock(seed, WandsCatalog.All.Count, WandsMath.ForestShopStockSize);
                Assert.AreEqual(WandsMath.ForestShopStockSize, stock.Count);
                Assert.AreEqual(stock.Count, stock.Distinct().Count(), "shop stock must not contain duplicate wands");
                foreach (int idx in stock)
                {
                    Assert.GreaterOrEqual(idx, 0);
                    Assert.Less(idx, WandsCatalog.All.Count);
                }
            }
        }


        [Test]
        public void WandsMath_PickShopStock_IsDeterministicForSameSeed()
        {
            var a = WandsMath.PickShopStock(42, WandsCatalog.All.Count, WandsMath.ShopStockSize);
            var b = WandsMath.PickShopStock(42, WandsCatalog.All.Count, WandsMath.ShopStockSize);
            CollectionAssert.AreEqual(a, b);
        }


        [Test]
        public void WandsMath_PickShopStock_ZeroInputs_ReturnsEmpty()
        {
            Assert.AreEqual(0, WandsMath.PickShopStock(1, 0, 3).Count);
            Assert.AreEqual(0, WandsMath.PickShopStock(1, 16, 0).Count);
        }


        [Test]
        public void WandsMath_PickShopStock_CapsAtCatalogCount()
        {
            var stock = WandsMath.PickShopStock(7, 3, 10);
            Assert.AreEqual(3, stock.Count);
        }


        [Test]
        public void WandsCatalog_HasSixteenEntries()
        {
            Assert.AreEqual(16, WandsCatalog.All.Count);
        }


        [Test]
        public void WandsCatalog_AllItemIdsAreUniqueAndPrefixed()
        {
            var ids = WandsCatalog.AllItemIds();
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), "Wand item ids must be unique.");
            foreach (var id in ids)
                Assert.IsTrue(id.StartsWith("aae_wand_"), $"Unexpected wand item id: {id}");
        }


        [Test]
        public void WandsCatalog_AllNamesFollowWandOfConvention()
        {
            foreach (var def in WandsCatalog.All)
                Assert.IsTrue(def.Name.StartsWith("Wand of "), $"Unexpected wand name: {def.Name}");
        }


        [Test]
        public void WandsCatalog_TryGetBySpell_RoundTripsToItemId()
        {
            Assert.IsTrue(WandsCatalog.TryGetBySpell(SpellId.Fireball, out var def));
            Assert.AreEqual("aae_wand_fireball", def.ItemId);
        }


        [Test]
        public void WandsCatalog_TryGetByItemId_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(WandsCatalog.TryGetByItemId("not_a_wand", out _));
            Assert.IsFalse(WandsCatalog.TryGetByItemId(null, out _));
        }


        [Test]
        public void WandsCatalog_IsWandItemId_MatchesOnlyCatalogEntries()
        {
            Assert.IsTrue(WandsCatalog.IsWandItemId("aae_wand_summondemon"));
            Assert.IsFalse(WandsCatalog.IsWandItemId("aae_rod_of_apostle"));
        }


        [Test]
        public void WandsCatalog_UnbindingSpells_AreNeverWands()
        {
            // The 12-mark Unbindings are deliberately excluded — see
            // WandsCatalog.cs's header for why.
            var unbindings = new[]
            {
                SpellId.FirstFlameRemembered, SpellId.OnTheWingsOfTheGale, SpellId.MountainsWrath,
                SpellId.TheWeepingSky, SpellId.TheBentKnee,
            };
            foreach (var s in unbindings)
                Assert.IsFalse(WandsCatalog.TryGetBySpell(s, out _), $"{s} should not be wand-eligible.");
        }
    }
}
