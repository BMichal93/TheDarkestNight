using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        [Test]
        public void NatureMath_WindwallHurl_ThrowsFurtherThanTheSharedBounce()
        {
            // The Windwall's only bite is its throw — if it ever drops back to the
            // shared bounce margin it becomes strictly the worst wall (Mistwall
            // gives the same stop plus a slow, a bite, and the fire-quench).
            Assert.IsTrue(NatureMath.WindwallHurlMargin > NatureMath.BarrierBounceMargin,
                "Windwall must hurl well past the ordinary barrier bounce.");
        }


        // ── NatureMath ────────────────────────────────────────────────────────

        [Test]
        public void NatureMath_TerrainElements_Forest_ReturnsEarth()
        {
            var els = NatureMath.TerrainElements("Forest");
            Assert.AreEqual(1, els.Length);
            Assert.AreEqual(NatureElement.Earth, els[0]);
        }


        [Test]
        public void NatureMath_TerrainElements_PureTerrains_MapToSingleElement()
        {
            // Iconic terrains give exactly one element for the whole battle.
            Assert.AreEqual(new[] { NatureElement.Wind },  NatureMath.TerrainElements("Mountain"));
            Assert.AreEqual(new[] { NatureElement.Water }, NatureMath.TerrainElements("River"));
            Assert.AreEqual(new[] { NatureElement.Water }, NatureMath.TerrainElements("OpenSea"));
            Assert.AreEqual(new[] { NatureElement.Storm }, NatureMath.TerrainElements("Desert"));
            Assert.AreEqual(new[] { NatureElement.Earth }, NatureMath.TerrainElements("Forest"));
        }


        [Test]
        public void NatureMath_TerrainElements_BlendedTerrains_OfferTwoElements()
        {
            // Transitional terrains offer one of two fitting elements (rolled per charge).
            void AssertBlend(string terrain, NatureElement a, NatureElement b)
            {
                var els = NatureMath.TerrainElements(terrain);
                Assert.AreEqual(2, els.Length, $"{terrain} should offer two elements.");
                CollectionAssert.Contains(els, a, $"{terrain} should include {a}.");
                CollectionAssert.Contains(els, b, $"{terrain} should include {b}.");
            }
            AssertBlend("Steppe", NatureElement.Wind,  NatureElement.Storm);
            AssertBlend("Plain",  NatureElement.Earth, NatureElement.Storm);
            AssertBlend("Snow",   NatureElement.Water, NatureElement.Wind);
            AssertBlend("Swamp",  NatureElement.Water, NatureElement.Earth);
            AssertBlend("Canyon", NatureElement.Earth, NatureElement.Wind);
        }


        [Test]
        public void NatureMath_TerrainElements_Unknown_ReturnsAllFour()
        {
            var els = NatureMath.TerrainElements("SomeFictionalTerrain");
            Assert.AreEqual(4, els.Length);
            CollectionAssert.Contains(els, NatureElement.Wind);
            CollectionAssert.Contains(els, NatureElement.Earth);
            CollectionAssert.Contains(els, NatureElement.Water);
            CollectionAssert.Contains(els, NatureElement.Storm);
        }


        [Test]
        public void NatureMath_ElementOf_EachPower_RoundTrips()
        {
            var pairs = new[]
            {
                (NaturePower.Gale,        NatureElement.Wind),
                (NaturePower.Windwall,    NatureElement.Wind),
                (NaturePower.Entangle,    NatureElement.Earth),
                (NaturePower.Thornwall,   NatureElement.Earth),
                (NaturePower.Torrent,     NatureElement.Water),
                (NaturePower.Mistwall,    NatureElement.Water),
                (NaturePower.ThunderClap, NatureElement.Storm),
                (NaturePower.Stormwall,   NatureElement.Storm),
            };
            foreach (var (power, expected) in pairs)
                Assert.AreEqual(expected, NatureMath.ElementOf(power),
                    $"ElementOf({power}) should be {expected}.");
        }


        [Test]
        public void NatureMath_AttackAndSupport_MatchElement()
        {
            foreach (NatureElement el in new[]
                { NatureElement.Wind, NatureElement.Earth, NatureElement.Water, NatureElement.Storm })
            {
                var atk = NatureMath.AttackPower(el);
                var sup = NatureMath.SupportPower(el);
                Assert.AreEqual(el, NatureMath.ElementOf(atk), $"Attack power of {el} mismatched.");
                Assert.AreEqual(el, NatureMath.ElementOf(sup), $"Support power of {el} mismatched.");
                Assert.IsTrue(NatureMath.IsAttack(atk),  $"{atk} should be an attack.");
                Assert.IsFalse(NatureMath.IsAttack(sup), $"{sup} should be a support.");
            }
        }


        [Test]
        public void NatureMath_RandomPower_Earth_OnlyEarthPowers()
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 20; i++)
            {
                var p = NatureMath.RandomPower(NatureElement.Earth, rng);
                Assert.IsTrue(p == NaturePower.Entangle || p == NaturePower.Thornwall,
                    $"Earth random power must be Entangle or Thornwall, got {p}.");
            }
        }


        [Test]
        public void NatureMath_PowerName_AllPowers_NonEmpty()
        {
            foreach (NaturePower p in System.Enum.GetValues(typeof(NaturePower)))
            {
                if (p == NaturePower.None) continue;
                Assert.IsFalse(string.IsNullOrEmpty(NatureMath.PowerName(p)),
                    $"PowerName({p}) must not be empty.");
            }
        }


        [Test]
        public void NatureMath_ElementName_AllElements_NonEmpty()
        {
            foreach (NatureElement el in System.Enum.GetValues(typeof(NatureElement)))
            {
                if (el == NatureElement.None) continue;
                Assert.IsFalse(string.IsNullOrEmpty(NatureMath.ElementName(el)),
                    $"ElementName({el}) must not be empty.");
            }
        }


        // ── NatureMath · element selection (W=Wind, S=Earth, A=Water, D=Storm) ──
        [Test]
        public void NatureMath_ElementForKey_MapsDirectionsToElements()
        {
            Assert.AreEqual(NatureElement.Wind,  NatureMath.ElementForKey("W"));
            Assert.AreEqual(NatureElement.Earth, NatureMath.ElementForKey("S"));
            Assert.AreEqual(NatureElement.Water, NatureMath.ElementForKey("A"));
            Assert.AreEqual(NatureElement.Storm, NatureMath.ElementForKey("D"));
            Assert.AreEqual(NatureElement.None,  NatureMath.ElementForKey("Q"));
        }


        [Test]
        public void NatureMath_KeyForElement_RoundTripsWithElementForKey()
        {
            foreach (NatureElement el in new[]
                { NatureElement.Wind, NatureElement.Earth, NatureElement.Water, NatureElement.Storm })
                Assert.AreEqual(el, NatureMath.ElementForKey(NatureMath.KeyForElement(el)),
                    $"KeyForElement/ElementForKey must round-trip for {el}.");
        }


        [Test]
        public void NatureMath_KnockbackEase_MonotonicFromZeroToOne()
        {
            Assert.AreEqual(0f, NatureMath.KnockbackEase(0f), 0.0001f);
            Assert.AreEqual(0f, NatureMath.KnockbackEase(-1f), 0.0001f);
            Assert.AreEqual(1f, NatureMath.KnockbackEase(NatureMath.KnockbackPushDuration), 0.0001f);
            Assert.AreEqual(1f, NatureMath.KnockbackEase(NatureMath.KnockbackPushDuration + 1f), 0.0001f);

            // Ease-OUT: covers more ground in the first half of the push than the second.
            float half = NatureMath.KnockbackPushDuration * 0.5f;
            float atHalfTime = NatureMath.KnockbackEase(half);
            Assert.Greater(atHalfTime, 0.5f);

            float prev = 0f;
            for (int i = 1; i <= 10; i++)
            {
                float frac = NatureMath.KnockbackEase(NatureMath.KnockbackPushDuration * (i / 10f));
                Assert.GreaterOrEqual(frac, prev, "Knockback ease must never move an agent backward.");
                prev = frac;
            }
        }


        // ── LivingEnergyMath · capacity ────────────────────────────────────────
        [Test]
        public void LivingEnergyMath_AreaCapacity_ForestRichestDesertPoorest()
        {
            float forest = LivingEnergyMath.AreaCapacity("Forest");
            float desert = LivingEnergyMath.AreaCapacity("Desert");
            float plain  = LivingEnergyMath.AreaCapacity("Plain");
            Assert.Greater(forest, plain,  "Forest should hold more living energy than open plain.");
            Assert.Greater(plain,  desert, "Plain should hold more living energy than desert.");
            Assert.AreEqual(60f, LivingEnergyMath.AreaCapacity("SomethingUnknown"), 0.001f);
        }


        // ── LivingEnergyMath · match factor ────────────────────────────────────
        [Test]
        public void LivingEnergyMath_MatchFactor_FavouredCheapMismatchedDear()
        {
            // Wind on a mountain (favoured) is cheap; water on a mountain is dear.
            Assert.AreEqual(LivingEnergyMath.MatchedFactor,
                LivingEnergyMath.MatchFactor(NatureElement.Wind, "Mountain"), 0.001f);
            Assert.AreEqual(LivingEnergyMath.MismatchedFactor,
                LivingEnergyMath.MatchFactor(NatureElement.Water, "Desert"), 0.001f);
            // Steppe favours Wind OR Storm (blended) — both cheap.
            Assert.AreEqual(LivingEnergyMath.MatchedFactor,
                LivingEnergyMath.MatchFactor(NatureElement.Wind,  "Steppe"), 0.001f);
            Assert.AreEqual(LivingEnergyMath.MatchedFactor,
                LivingEnergyMath.MatchFactor(NatureElement.Storm, "Steppe"), 0.001f);
            // Unknown ground is neutral; fire (None) is always neutral.
            Assert.AreEqual(LivingEnergyMath.NeutralFactor,
                LivingEnergyMath.MatchFactor(NatureElement.Water, "MysteryLand"), 0.001f);
            Assert.AreEqual(LivingEnergyMath.NeutralFactor,
                LivingEnergyMath.MatchFactor(NatureElement.None, "Forest"), 0.001f);
        }


        [Test]
        public void LivingEnergyMath_NatureDrain_ScalesWithMatch()
        {
            float cheap = LivingEnergyMath.NatureDrain(NatureElement.Wind,  "Mountain");
            float dear  = LivingEnergyMath.NatureDrain(NatureElement.Water, "Mountain");
            Assert.Greater(dear, cheap, "Drawing against the land must cost more than a favoured draw.");
            Assert.AreEqual(LivingEnergyMath.NatureDrawBase * LivingEnergyMath.MatchedFactor, cheap, 0.001f);
        }


        [Test]
        public void LivingEnergyMath_FireDrain_ScalesWithStrokesAndFloorsAtOne()
        {
            float terrain = LivingEnergyMath.FireDrain(4, "Forest");   // neutral (element-blind)
            Assert.AreEqual(4 * LivingEnergyMath.FireDrawPerInput, terrain, 0.001f);
            // A zero/garbage stroke count still spends at least one stroke's worth.
            Assert.AreEqual(LivingEnergyMath.FireDrawPerInput, LivingEnergyMath.FireDrain(0, "Forest"), 0.001f);
        }


        // ── LivingEnergyMath · thresholds ──────────────────────────────────────
        [Test]
        public void LivingEnergyMath_LevelOf_BandsByFraction()
        {
            Assert.AreEqual(EnergyOmen.None,    LivingEnergyMath.LevelOf(0.80f));
            Assert.AreEqual(EnergyOmen.Half,    LivingEnergyMath.LevelOf(0.50f));
            Assert.AreEqual(EnergyOmen.Quarter, LivingEnergyMath.LevelOf(0.20f));
            Assert.AreEqual(EnergyOmen.Empty,   LivingEnergyMath.LevelOf(0.0f));
            Assert.AreEqual(EnergyOmen.Empty,   LivingEnergyMath.LevelOf(-0.3f));
        }


        [Test]
        public void LivingEnergyMath_OmenCrossed_AnnouncesEachBandOnceGoingDown()
        {
            // Crossing 0.6 → 0.4 enters the Half band for the first time.
            Assert.AreEqual(EnergyOmen.Half,
                LivingEnergyMath.OmenCrossed(0.6f, 0.4f, EnergyOmen.None));
            // Already announced Half: staying in the Half band says nothing more.
            Assert.AreEqual(EnergyOmen.None,
                LivingEnergyMath.OmenCrossed(0.45f, 0.30f, EnergyOmen.Half));
            // Dropping further into the Quarter band announces again.
            Assert.AreEqual(EnergyOmen.Quarter,
                LivingEnergyMath.OmenCrossed(0.30f, 0.20f, EnergyOmen.Half));
            // Energy recovering (afterFraction up) never announces.
            Assert.AreEqual(EnergyOmen.None,
                LivingEnergyMath.OmenCrossed(0.20f, 0.40f, EnergyOmen.Quarter));
        }


        [Test]
        public void LivingEnergyMath_DailyRegen_AtLeastOnePerDay()
        {
            Assert.GreaterOrEqual(LivingEnergyMath.DailyRegen(120f),
                LivingEnergyMath.DailyRegen(15f), "Richer land regrows at least as fast.");
            Assert.GreaterOrEqual(LivingEnergyMath.DailyRegen(5f), 1f, "Regen floors at one per day.");
        }


        [Test]
        public void LivingEnergyMath_Fraction_HandlesZeroCapacity()
        {
            Assert.AreEqual(0f, LivingEnergyMath.Fraction(10f, 0f), 0.001f);
            Assert.AreEqual(0.5f, LivingEnergyMath.Fraction(30f, 60f), 0.001f);
        }


        [Test]
        public void LivingEnergyMath_WeedFreeDrawChance_IsAProbability()
        {
            Assert.Greater(LivingEnergyMath.WeedFreeDrawChance, 0f);
            Assert.Less(LivingEnergyMath.WeedFreeDrawChance, 1f);
            Assert.AreEqual(0.30f, LivingEnergyMath.WeedFreeDrawChance, 0.001f);
        }
    }
}
