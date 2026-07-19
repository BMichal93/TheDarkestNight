using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── RelicMath tests (Phase 6) ────────────────────────────────────────

        [Test]
        public void RelicMath_CrystalRelicPowerMult_IsWeakerThanFull()
        {
            Assert.Greater(RelicMath.CrystalRelicPowerMult, 0f);
            Assert.Less(RelicMath.CrystalRelicPowerMult, 1f);
        }


        [Test]
        public void RelicMath_DarkGiftRelicPowerMult_IsWeakerThanFull()
        {
            Assert.Greater(RelicMath.DarkGiftRelicPowerMult, 0f);
            Assert.Less(RelicMath.DarkGiftRelicPowerMult, 1f);
        }


        [Test]
        public void RelicMath_DarkGiftRelicPowerMult_IsWeakerThanCrystalRelicPowerMult()
        {
            // A relic-bound Dark Gift is taxed harder than a relic-bound Crystal
            // (see the header comment in RelicMath.cs for why).
            Assert.Less(RelicMath.DarkGiftRelicPowerMult, RelicMath.CrystalRelicPowerMult);
        }


        [Test]
        public void RelicMath_DemonBaneMultiplier_IsABonusNotAPenalty()
        {
            Assert.Greater(RelicMath.DemonBaneMultiplier, 1f);
        }


        [Test]
        public void RelicMath_RollRelicDrop_RespectsChanceBoundary()
        {
            Assert.IsTrue(RelicMath.RollRelicDrop(RelicMath.RelicDropChancePerVictory - 0.0001));
            Assert.IsFalse(RelicMath.RollRelicDrop(RelicMath.RelicDropChancePerVictory + 0.0001));
        }


        [Test]
        public void RelicMath_RelicDropChancePerVictory_IsRare()
        {
            Assert.Less(RelicMath.RelicDropChancePerVictory, 0.10f);
            Assert.Greater(RelicMath.RelicDropChancePerVictory, 0f);
        }


        [Test]
        public void RelicMath_PickRelicIndex_StaysInBounds()
        {
            var rng = new Random(11);
            for (int i = 0; i < 500; i++)
            {
                int idx = RelicMath.PickRelicIndex(rng.NextDouble(), 10);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, 10);
            }
        }


        [Test]
        public void RelicMath_PickRelicIndex_ZeroCount_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, RelicMath.PickRelicIndex(0.5, 0));
        }


        [Test]
        public void RelicMath_RollRuinLoot_RespectsChanceBoundary()
        {
            Assert.IsTrue(RelicMath.RollRuinLoot(RelicMath.RuinBaseRelicChance - 0.0001));
            Assert.IsFalse(RelicMath.RollRuinLoot(RelicMath.RuinBaseRelicChance + 0.0001));
        }


        // ── RelicNaming tests (Phase 6) ──────────────────────────────────────

        [Test]
        public void RelicNaming_Generate_NeverCrashes_AcrossManySeeds()
        {
            for (int seed = -2000; seed < 3000; seed++)
            {
                string name = RelicNaming.Generate(seed);
                Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"Empty name for seed {seed}.");
            }
        }


        [Test]
        public void RelicNaming_Generate_IsDeterministicForSameSeed()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                string a = RelicNaming.Generate(seed);
                string b = RelicNaming.Generate(seed);
                Assert.AreEqual(a, b, $"Same seed {seed} produced different names.");
            }
        }


        [Test]
        public void RelicNaming_Generate_LowCollisionRateAcrossManySeeds()
        {
            var names = new HashSet<string>();
            const int sampleSize = 3000;
            for (int seed = 0; seed < sampleSize; seed++)
                names.Add(RelicNaming.Generate(seed));

            // Soft check: with ~5,000 possible combinations across the four
            // templates, 3,000 rolls will legitimately see a fair number of
            // repeats (birthday paradox) — this is not a hard uniqueness
            // guarantee, just a floor catching an accidentally degenerate
            // word bank (e.g. a bank that collapsed to one entry).
            double uniqueFraction = names.Count / (double)sampleSize;
            Assert.Greater(uniqueFraction, 0.45,
                $"Only {names.Count}/{sampleSize} unique relic names — word banks may be too small.");
        }


        // ── RelicCatalog tests (Phase 6) ─────────────────────────────────────

        [Test]
        public void RelicCatalog_AllEntries_HaveNonEmptyItemIdAndName()
        {
            foreach (var def in RelicCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.ItemId), $"Relic {def.Id} has no ItemId.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Name), $"Relic {def.Id} has no Name.");
                Assert.IsTrue(def.Effects != null && def.Effects.Count >= 1 && def.Effects.Count <= 2,
                    $"Relic {def.Id} should carry one or two effects.");
            }
        }


        [Test]
        public void RelicCatalog_ItemIds_AreUnique()
        {
            var ids = RelicCatalog.All.Select(d => d.ItemId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Duplicate relic ItemId found.");
        }


        [Test]
        public void RelicCatalog_TryGetByItemId_RoundTrips()
        {
            foreach (var def in RelicCatalog.All)
            {
                Assert.IsTrue(RelicCatalog.TryGetByItemId(def.ItemId, out var found));
                Assert.AreEqual(def.Id, found.Id);
            }
            Assert.IsFalse(RelicCatalog.TryGetByItemId("not_a_relic", out _));
            Assert.IsFalse(RelicCatalog.TryGetByItemId(null, out _));
        }


        [Test]
        public void RelicCatalog_IsRelicItemId_MatchesOnlyCatalogItems()
        {
            Assert.IsTrue(RelicCatalog.IsRelicItemId("aae_relic_cinderfang"));
            Assert.IsFalse(RelicCatalog.IsRelicItemId("aae_sunstone")); // a Crystal, not a relic
            Assert.IsFalse(RelicCatalog.IsRelicItemId("looter"));
        }


        [Test]
        public void RelicCatalog_ContainsBothCrystalAndDarkGiftSourcedRelics()
        {
            bool anyCrystal  = RelicCatalog.All.Any(d => d.Effects.Any(e => e.Source == RelicEffectSource.Crystal));
            bool anyDarkGift = RelicCatalog.All.Any(d => d.Effects.Any(e => e.Source == RelicEffectSource.DarkGift));
            Assert.IsTrue(anyCrystal, "No Crystal-sourced relic found.");
            Assert.IsTrue(anyDarkGift, "No Dark Gift-sourced relic found.");
        }
    }
}
