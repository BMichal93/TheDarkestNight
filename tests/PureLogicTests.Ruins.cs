using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        [Test]
        public void RuinsMath_RollChamberLoot_TalismanIsRarerThanWand()
        {
            Assert.AreEqual(RuinsMath.LootKind.Talisman, RuinsMath.RollChamberLoot(0.97));
            Assert.AreEqual(RuinsMath.LootKind.Wand, RuinsMath.RollChamberLoot(0.92));
        }


        [Test]
        public void RuinsMath_RollChamberLoot_CoversTheWholeRollRangeIncludingTalisman()
        {
            var seen = new HashSet<RuinsMath.LootKind>();
            for (double r = 0.0; r < 1.0; r += 0.001)
                seen.Add(RuinsMath.RollChamberLoot(r));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Talisman));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.None));
        }


        // ── RuinsMath tests (Phase 9 — the ruins) ───────────────────────────────

        [Test]
        public void RuinsMath_StableHash_IsDeterministicAndNonNegative()
        {
            int h1 = RuinsMath.StableHash("castle_test_id");
            int h2 = RuinsMath.StableHash("castle_test_id");
            Assert.AreEqual(h1, h2, "The same settlement id must always hash to the same value (reload-stability depends on this).");
            Assert.GreaterOrEqual(h1, 0);
            Assert.AreEqual(0, RuinsMath.StableHash(null));
            Assert.AreEqual(0, RuinsMath.StableHash(""));
        }


        [Test]
        public void RuinsMath_ShouldBeRuin_IsStableAcrossRepeatedCalls()
        {
            bool first = RuinsMath.ShouldBeRuin("castle_urikskala");
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(first, RuinsMath.ShouldBeRuin("castle_urikskala"),
                    "The same castle id must resolve to the same ruin/not-ruin verdict every time — this is what makes conversion stable per save without persisting the chosen set.");
            Assert.IsFalse(RuinsMath.ShouldBeRuin(null));
            Assert.IsFalse(RuinsMath.ShouldBeRuin(""));
        }


        [Test]
        public void RuinsMath_ShouldBeRuin_ConvertsRoughlyEightyPercentOfCastles()
        {
            int total = 2000;
            int ruins = 0;
            for (int i = 0; i < total; i++)
                if (RuinsMath.ShouldBeRuin("castle_synthetic_" + i)) ruins++;

            double fraction = ruins / (double)total;
            Assert.Greater(fraction, 0.72, "Expected roughly 80% of castles to convert to ruins.");
            Assert.Less(fraction, 0.88, "Expected roughly 80% of castles to convert to ruins.");
        }


        [Test]
        public void RuinsMath_RuinNameFor_IncludesOriginalNameAndAPrefix()
        {
            string name = RuinsMath.RuinNameFor("castle_kaysar", "Kaysar");
            StringAssert.Contains("Kaysar", name);
            bool hasKnownPrefix = false;
            foreach (var prefix in new[] { "Ruined Manor", "Ruined Castle", "Ruined City", "Ruined Tower", "Ruined Hold", "Ruined Bastion", "Ruined Keep", "Ruined Rampart" })
                if (name.StartsWith(prefix)) hasKnownPrefix = true;
            Assert.IsTrue(hasKnownPrefix, $"'{name}' did not start with a recognised ruin prefix.");
        }


        [Test]
        public void RuinsMath_RuinNameFor_IsIdempotent_NoStackedPrefixes()
        {
            string once  = RuinsMath.RuinNameFor("castle_odrysa", "Odrysa Castle");
            string twice = RuinsMath.RuinNameFor("castle_odrysa", once);
            Assert.AreEqual(once, twice, "Re-applying the ruin name must not stack another prefix.");
        }


        [Test]
        public void RuinsMath_StripRuinPrefix_RemovesEveryStackedPrefixFromLegacySaves()
        {
            Assert.AreEqual("Odrysa Castle",
                RuinsMath.StripRuinPrefix("Ruined City of Ruined City of Odrysa Castle"));
            Assert.AreEqual("Odrysa Castle",
                RuinsMath.StripRuinPrefix("Ruined Tower of Ruined Keep of Odrysa Castle"));
            Assert.AreEqual("Odrysa Castle", RuinsMath.StripRuinPrefix("Odrysa Castle"));
        }


        [Test]
        public void RuinsMath_ChamberSequence_AlwaysEndsOnTheThroneIndexAndHasNoDuplicates()
        {
            const int poolSize = 10;
            const int throneIndex = 9;
            for (int i = 0; i < 30; i++)
            {
                var seq = RuinsMath.ChamberSequence("castle_seq_" + i, poolSize, throneIndex);
                Assert.GreaterOrEqual(seq.Length, RuinsMath.MinChambersExcludingThrone + 1);
                Assert.LessOrEqual(seq.Length, RuinsMath.MaxChambersExcludingThrone + 1);
                Assert.AreEqual(throneIndex, seq[seq.Length - 1], "The Throne of Dust must always be the last chamber.");
                Assert.AreEqual(seq.Length, seq.Distinct().Count(), "A ruin's chamber sequence must not repeat a chamber.");
            }
        }


        [Test]
        public void RuinsMath_ChamberSequence_IsStableForTheSameSettlement()
        {
            var a = RuinsMath.ChamberSequence("castle_stable", 10, 9);
            var b = RuinsMath.ChamberSequence("castle_stable", 10, 9);
            CollectionAssert.AreEqual(a, b);
        }


        [Test]
        public void RuinsMath_HoursForChamber_DecreasesWithScoutingAndRespectsFloor()
        {
            float noSkill   = RuinsMath.HoursForChamber(0);
            float midSkill  = RuinsMath.HoursForChamber(150);
            float maxSkill  = RuinsMath.HoursForChamber(300);
            float overCap   = RuinsMath.HoursForChamber(9999);

            Assert.AreEqual(RuinsMath.BaseChamberHours, noSkill);
            Assert.Less(midSkill, noSkill);
            Assert.AreEqual(RuinsMath.MinChamberHours, maxSkill);
            Assert.AreEqual(maxSkill, overCap, "Scouting above the cap must not reduce hours further.");
            Assert.GreaterOrEqual(RuinsMath.HoursForChamber(-50), RuinsMath.MinChamberHours);
        }


        [Test]
        public void RuinsMath_NightfallCrossed_OnlyTrueOnTheDayToNightEdge()
        {
            Assert.IsTrue(RuinsMath.NightfallCrossed(wasNight: false, isNight: true));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: true, isNight: true));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: false, isNight: false));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: true, isNight: false));
        }


        [Test]
        public void RuinsMath_RollHazard_RespectsItsOwnThreshold()
        {
            Assert.IsTrue(RuinsMath.RollHazard(0.0));
            Assert.IsFalse(RuinsMath.RollHazard(RuinsMath.ChamberHazardChance));
            Assert.IsFalse(RuinsMath.RollHazard(0.999));
        }


        [Test]
        public void RuinsMath_RollChamberLoot_CoversTheWholeRollRangeWithoutGaps()
        {
            var seen = new HashSet<RuinsMath.LootKind>();
            for (double r = 0.0; r < 1.0; r += 0.001)
                seen.Add(RuinsMath.RollChamberLoot(r));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Weapon));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Armor));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.TradeGoods));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Relic));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.SpellFormula));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Wand));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.None));
        }


        [Test]
        public void RuinsMath_RollChamberLoot_WandIsRarerThanSpellFormula()
        {
            Assert.AreEqual(RuinsMath.LootKind.Wand, RuinsMath.RollChamberLoot(0.92));
            Assert.AreEqual(RuinsMath.LootKind.SpellFormula, RuinsMath.RollChamberLoot(0.85));
        }


        [Test]
        public void RuinsMath_BiasedLootRoll_FavoursTheChambersBiasAboutHalfTheTimeOnAMiss()
        {
            // A roll that would otherwise be "None" (>= 0.99, after the Talisman
            // slice) should become the bias when the bias-roll succeeds, and
            // stay None when it doesn't.
            Assert.AreEqual(RuinsMath.LootKind.SpellFormula,
                RuinsMath.BiasedLootRoll(0.995, 0.0, RuinsMath.LootKind.SpellFormula));
            Assert.AreEqual(RuinsMath.LootKind.None,
                RuinsMath.BiasedLootRoll(0.995, 0.9, RuinsMath.LootKind.SpellFormula));
            // A roll that already hit something concrete is left alone.
            Assert.AreEqual(RuinsMath.LootKind.Weapon,
                RuinsMath.BiasedLootRoll(0.1, 0.0, RuinsMath.LootKind.SpellFormula));
        }


        [Test]
        public void RuinsMath_FinalChamberLootRoll_NeverReturnsNone()
        {
            for (double r = 0.0; r < 1.0; r += 0.001)
                Assert.AreNotEqual(RuinsMath.LootKind.None, RuinsMath.FinalChamberLootRoll(r),
                    "The Throne of Dust — the last chamber — must always yield something.");
        }


        [Test]
        public void RuinsMath_HazardLossHelpers_StayWithinDeclaredBounds()
        {
            var rng = new Random(12345);
            for (int i = 0; i < 200; i++)
            {
                int troopLoss = RuinsMath.HazardTroopLoss(rng);
                Assert.GreaterOrEqual(troopLoss, RuinsMath.MinHazardTroopLoss);
                Assert.LessOrEqual(troopLoss, RuinsMath.MaxHazardTroopLoss);

                int hpLoss = RuinsMath.HazardHpLoss(rng);
                Assert.GreaterOrEqual(hpLoss, RuinsMath.MinHazardHpLoss);
                Assert.LessOrEqual(hpLoss, RuinsMath.MaxHazardHpLoss);
            }
        }


        [Test]
        public void RuinsMath_Cooldowns_ClearedIsLongerThanRetreat()
        {
            Assert.Greater(RuinsMath.ClearedCooldownDays, RuinsMath.RetreatCooldownDays);
        }


        // ── RuinsCatalog tests (Phase 9 — the ruins) ────────────────────────────

        [Test]
        public void RuinsCatalog_AllChambers_HaveNameAndEntryLoreAndSearchLine()
        {
            foreach (var def in RuinsCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Name), $"{def.Id} is missing a Name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.EntryLore), $"{def.Id} is missing EntryLore.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.SearchLine), $"{def.Id} is missing a SearchLine.");
            }
        }


        [Test]
        public void RuinsCatalog_ThroneOfDust_ExistsExactlyOnce()
        {
            Assert.AreEqual(1, RuinsCatalog.All.Count(d => d.Id == ChamberType.ThroneOfDust));
        }


        [Test]
        public void RuinsCatalog_PoolSize_MatchesAllCount()
        {
            Assert.AreEqual(RuinsCatalog.All.Count, RuinsCatalog.PoolSize);
        }
    }
}
