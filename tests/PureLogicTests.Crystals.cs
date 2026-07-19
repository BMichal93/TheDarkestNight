using System;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── CrystalMath pure logic ────────────────────────────────────────────

        [Test]
        public void CrystalMath_FormationOdds_FloorAtSixPercent()
        {
            float odds = CrystalMath.FormationOdds(0, 0);
            Assert.AreEqual(0.06f, odds, 0.001f, "Floor should be 6 % with no skill.");
        }


        [Test]
        public void CrystalMath_FormationOdds_RisesWithCombinedSkill()
        {
            float low  = CrystalMath.FormationOdds(50, 50);   // combined 50
            float high = CrystalMath.FormationOdds(150, 150); // combined 150
            Assert.Greater(high, low, "More skill means better odds.");
        }


        [Test]
        public void CrystalMath_FormationOdds_CapsAtNinetyPercent()
        {
            float odds = CrystalMath.FormationOdds(300, 300);
            Assert.LessOrEqual(odds, 0.90f, "Odds must not exceed the 90 % ceiling.");
        }


        [Test]
        public void CrystalMath_FormationOddsWithPatience_AddsBonus()
        {
            float base_  = CrystalMath.FormationOdds(100, 100);
            float talent = CrystalMath.FormationOddsWithPatience(100, 100);
            Assert.AreEqual(Math.Min(0.90f, base_ + 0.20f), talent, 0.001f);
        }


        [Test]
        public void CrystalMath_IsDaylight_TrueOnlyInWindow()
        {
            Assert.IsTrue(CrystalMath.IsDaylight(12f),  "Noon should be daylight.");
            Assert.IsTrue(CrystalMath.IsDaylight(6f),   "06:00 is the dawn boundary.");
            Assert.IsTrue(CrystalMath.IsDaylight(19.9f),"Just before 20:00 is still day.");
            Assert.IsFalse(CrystalMath.IsDaylight(20f), "20:00 is outside the window.");
            Assert.IsFalse(CrystalMath.IsDaylight(3f),  "Pre-dawn should be dark.");
        }


        [Test]
        public void CrystalMath_IsDaylightExtended_BroaderWindow()
        {
            Assert.IsTrue(CrystalMath.IsDaylightExtended(4f),  "SolarFlare: 04:00 is active.");
            Assert.IsTrue(CrystalMath.IsDaylightExtended(21.9f),"SolarFlare: just before 22:00 is active.");
            Assert.IsFalse(CrystalMath.IsDaylightExtended(22f), "SolarFlare: 22:00 is outside window.");
            Assert.IsFalse(CrystalMath.IsDaylightExtended(3.9f),"SolarFlare: 03:59 is outside window.");
        }


        [Test]
        public void CrystalMath_BurndownChance_IsFivePercent()
        {
            Assert.AreEqual(0.05f, CrystalMath.BurndownChance, 0.0001f);
        }


        [Test]
        public void CrystalMath_SolarFlareRadius_IncreasesBaseByTwentyFivePercent()
        {
            float r  = 5f;
            float r2 = CrystalMath.SolarFlareRadius(r);
            Assert.AreEqual(r * 1.25f, r2, 0.001f);
        }


        // ── CrystalMath NPC situational use ───────────────────────────────────────

        [Test]
        public void CrystalMath_NpcShouldUse_HealStone_WantsBearerHurt()
        {
            // Sunstone: fires when meaningfully hurt, not at (near) full health.
            Assert.IsTrue (CrystalMath.NpcShouldUse(CrystalType.Sunstone, 0.30f, 0, 0.1f));
            Assert.IsFalse(CrystalMath.NpcShouldUse(CrystalType.Sunstone, 0.90f, 0, 0.1f),
                "a healthy bearer does not waste a heal-stone");
            Assert.IsTrue(CrystalMath.IsHealingCrystal(CrystalType.Sunstone));
            Assert.IsFalse(CrystalMath.IsHealingCrystal(CrystalType.Embershard));
        }


        [Test]
        public void CrystalMath_NpcShouldUse_OffensiveStone_NeedsEnemiesInReach()
        {
            // Offensive crystal on empty air: never.
            Assert.IsFalse(CrystalMath.NpcShouldUse(CrystalType.Embershard, 1f, 0, 0.01f));
            // Enemies present + eager roll: yes; a crowd raises the eagerness.
            Assert.IsTrue(CrystalMath.NpcShouldUse(CrystalType.Embershard, 1f, 4, 0.6f));
            Assert.IsFalse(CrystalMath.NpcShouldUse(CrystalType.Embershard, 1f, 1, 0.6f),
                "a lone target with a lukewarm roll holds the crystal");
            // The Veilstone reaches farther than the short-range AoE stones.
            Assert.Greater(CrystalMath.CrystalUseRange(CrystalType.Veilstone),
                           CrystalMath.CrystalUseRange(CrystalType.Embershard));
        }


        // ── CrystalCatalog — five new crystals ────────────────────────────────────

        [Test]
        public void CrystalCatalog_AllElevenCrystalsResolve()
        {
            Assert.AreEqual(11, CrystalCatalog.All.Count);
            foreach (CrystalType t in System.Enum.GetValues(typeof(CrystalType)))
            {
                var def = CrystalCatalog.Get(t);
                Assert.AreEqual(t, def.Type);
                Assert.IsTrue(CrystalCatalog.IsCrystalItemId(def.ItemId));
                Assert.IsTrue(CrystalCatalog.TryGetByItemId(def.ItemId, out var found));
                Assert.AreEqual(t, found.Type);
            }
        }


        [Test]
        public void CrystalMath_Thornveil_IsDeepestSlowButShortestDuration()
        {
            // The root is far deeper than any partial slow...
            Assert.Less(CrystalMath.ThornRootMult, CrystalMath.RimeSlowMult);
            Assert.Less(CrystalMath.ThornRootMult, CrystalMath.VeilSlowMult);
            Assert.Less(CrystalMath.ThornRootMult, CrystalMath.DuskSlowMult);
            // ...and its damage is the lowest of the single-target stones (the
            // control is the point, not the hit).
            Assert.Less(CrystalMath.ThornDamage, CrystalMath.VeilDamage);
        }


        [Test]
        public void CrystalMath_Willowisp_HitsHarderThanAoEMoraleDrains()
        {
            Assert.Greater(CrystalMath.WillowMoraleDrain, CrystalMath.StormMoraleDrain);
            Assert.Greater(CrystalMath.WillowMoraleDrain, CrystalMath.DuskMoraleDrain);
            // Reaches as far as Veilstone — both are reach-out-to-one stones.
            Assert.AreEqual(CrystalMath.VeilRange, CrystalMath.WillowRange, 0.0001f);
            Assert.AreEqual(CrystalMath.WillowRange, CrystalMath.CrystalUseRange(CrystalType.Willowisp), 0.0001f);
        }


        [Test]
        public void CrystalMath_Bloodstone_LifestealReturnsHalfDamageDealt()
        {
            float totalDealt = CrystalMath.BloodDamage * 3; // three enemies struck
            float healed = totalDealt * CrystalMath.BloodLifestealFrac;
            Assert.AreEqual(totalDealt * 0.5f, healed, 0.0001f);
        }


        [Test]
        public void CrystalMath_Zephyrglass_HastensAboveNormalSpeed()
        {
            Assert.Greater(CrystalMath.ZephyrHasteMult, 1f);
        }


        [Test]
        public void CrystalMath_Aegisstone_IsTreatedAsHealingForNpcUse()
        {
            Assert.IsTrue(CrystalMath.IsHealingCrystal(CrystalType.Aegisstone));
            Assert.IsTrue(CrystalMath.NpcShouldUse(CrystalType.Aegisstone, 0.30f, 0, 0.1f));
            Assert.IsFalse(CrystalMath.NpcShouldUse(CrystalType.Aegisstone, 0.90f, 0, 0.1f));
        }


        [Test]
        public void CrystalMath_MasteryScale_KeysOffMedicine_AndCaps()
        {
            Assert.AreEqual(1.0f,  CrystalMath.MasteryScale(0),    1e-6f);
            Assert.AreEqual(1.15f, CrystalMath.MasteryScale(150),  1e-5f); // +0.1%/point
            Assert.AreEqual(1.30f, CrystalMath.MasteryScale(300),  1e-5f); // cap at 300 Medicine
            Assert.AreEqual(1.30f, CrystalMath.MasteryScale(1000), 1e-5f);
        }
    }
}
