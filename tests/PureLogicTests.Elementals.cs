using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        [Test]
        public void SacredSiteMath_FormationOdds_FloorsCapsAndGrowsWithSkill()
        {
            // Floor even at 0 Smithing, ceiling even at absurd Smithing, and
            // strictly non-decreasing in between.
            Assert.AreEqual(0.10f, SacredSiteMath.FormationOdds(0), 0.0001f);
            Assert.AreEqual(0.85f, SacredSiteMath.FormationOdds(1000), 0.0001f);
            Assert.Less(SacredSiteMath.FormationOdds(50), SacredSiteMath.FormationOdds(150));
        }


        // ── ElementalMath tests (The Kindled) ─────────────────────────────────
        [Test]
        public void ElementalMath_BeingResistsItsOwnElement()
        {
            // A flame being drinks fire; a stone being shrugs off earth magic.
            Assert.Less(ElementalMath.ElementDamageMultiplier(ElementalKind.Flame, MagicElement.Fire), 1f);
            Assert.Less(ElementalMath.ElementDamageMultiplier(ElementalKind.Stone, MagicElement.Earth), 1f);
            Assert.Less(ElementalMath.ElementDamageMultiplier(ElementalKind.Gale,  MagicElement.Wind),  1f);
        }


        [Test]
        public void ElementalMath_BeingBucklesToItsCounter()
        {
            // The wheel: fire drowns to water, water is drunk by earth, earth is
            // worn by wind, wind is burned by fire.
            Assert.Greater(ElementalMath.ElementDamageMultiplier(ElementalKind.Flame, MagicElement.Water), 1f);
            Assert.Greater(ElementalMath.ElementDamageMultiplier(ElementalKind.Tide,  MagicElement.Earth), 1f);
            Assert.Greater(ElementalMath.ElementDamageMultiplier(ElementalKind.Stone, MagicElement.Wind),  1f);
            Assert.Greater(ElementalMath.ElementDamageMultiplier(ElementalKind.Gale,  MagicElement.Fire),  1f);
        }


        [Test]
        public void ElementalMath_FrostMeltsToFireHardest()
        {
            // Ice fears fire above all, and shrugs off water.
            Assert.Greater(ElementalMath.ElementDamageMultiplier(ElementalKind.Frost, MagicElement.Fire), 1.5f);
            Assert.Less(ElementalMath.ElementDamageMultiplier(ElementalKind.Frost, MagicElement.Water), 1f);
        }


        [Test]
        public void ElementalMath_NeutralElementDoesNothing()
        {
            // Wind against a flame being is neither its element nor its counter.
            Assert.AreEqual(1f, ElementalMath.ElementDamageMultiplier(ElementalKind.Flame, MagicElement.Wind), 0.001f);
        }


        [Test]
        public void ElementalMath_StoneShattersToBluntTurnsBlades()
        {
            Assert.Greater(ElementalMath.PhysicalDamageMultiplier(ElementalKind.Stone, PhysicalHit.Blunt), 1f);
            Assert.Less   (ElementalMath.PhysicalDamageMultiplier(ElementalKind.Stone, PhysicalHit.Cut),   1f);
        }


        [Test]
        public void ElementalMath_FlameLetsSteelPassThrough()
        {
            // No physical weakness — flame is unmade by magic, not by the blade.
            Assert.Less(ElementalMath.PhysicalDamageMultiplier(ElementalKind.Flame, PhysicalHit.Cut),   1f);
            Assert.Less(ElementalMath.PhysicalDamageMultiplier(ElementalKind.Flame, PhysicalHit.Blunt), 1f);
        }


        [Test]
        public void ElementalMath_WildKindMatchesBiome()
        {
            Assert.AreEqual(ElementalKind.Frost, ElementalMath.WildKindForBiome("snowy tundra"));
            Assert.AreEqual(ElementalKind.Sand,  ElementalMath.WildKindForBiome("deep desert dunes"));
            Assert.AreEqual(ElementalKind.Tide,  ElementalMath.WildKindForBiome("old forest"));
            Assert.AreEqual(ElementalKind.Gale,  ElementalMath.WildKindForBiome("open steppe"));
            Assert.AreEqual(ElementalKind.Stone, ElementalMath.WildKindForBiome("mountain root"));
        }


        [Test]
        public void ElementalMath_AllKindsHavePositiveHealth()
        {
            foreach (ElementalKind k in Enum.GetValues(typeof(ElementalKind)))
                Assert.Greater(ElementalMath.Health(k), 0f);
        }


        // ── ElementalMath: The Great Other (Void kind) ──────────────────────────

        [Test]
        public void ElementalMath_Void_ResistsEveryMagicElement()
        {
            foreach (MagicElement el in new[] { MagicElement.Fire, MagicElement.Wind, MagicElement.Earth, MagicElement.Water, MagicElement.Spirit })
                Assert.AreEqual(ElementalMath.VoidResistAll, ElementalMath.ElementDamageMultiplier(ElementalKind.Void, el), 1e-6f);
        }


        [Test]
        public void ElementalMath_Void_ResistsEveryPhysicalHit()
        {
            foreach (PhysicalHit hit in new[] { PhysicalHit.Cut, PhysicalHit.Pierce, PhysicalHit.Blunt })
                Assert.AreEqual(ElementalMath.VoidResistAll, ElementalMath.PhysicalDamageMultiplier(ElementalKind.Void, hit), 1e-6f);
        }


        [Test]
        public void ElementalMath_Void_HasFarMoreHealthThanAnyOrdinaryKindled()
        {
            float voidHp = ElementalMath.Health(ElementalKind.Void);
            foreach (var kind in new[] { ElementalKind.Stone, ElementalKind.Frost, ElementalKind.Sand, ElementalKind.Flame, ElementalKind.Tide, ElementalKind.Gale })
                Assert.Greater(voidHp, ElementalMath.Health(kind) * 10f);
        }


        [Test]
        public void ElementalMath_AttackCooldownSecondsFor_VoidIsFasterThanOrdinaryKindled()
        {
            Assert.Less(ElementalMath.AttackCooldownSecondsFor(ElementalKind.Void), ElementalMath.AttackCooldownSeconds);
            Assert.AreEqual(ElementalMath.AttackCooldownSeconds, ElementalMath.AttackCooldownSecondsFor(ElementalKind.Stone), 1e-6f);
        }
    }
}
