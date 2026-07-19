using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── TempleMath tests (Phase 7, Faction E) ───────────────────────────────

        [Test]
        public void TempleMath_IsStartingTownId_MatchesOnlyOcsHallAndPravend()
        {
            Assert.IsTrue(TempleMath.IsStartingTownId("town_V2"));  // Ocs Hall
            Assert.IsTrue(TempleMath.IsStartingTownId("town_V3"));  // Pravend
            Assert.IsFalse(TempleMath.IsStartingTownId("town_V1")); // Charas
            Assert.IsFalse(TempleMath.IsStartingTownId("town_V4"));
            Assert.IsFalse(TempleMath.IsStartingTownId("town_V6")); // Jaculan
            Assert.IsFalse(TempleMath.IsStartingTownId(null));
            Assert.IsFalse(TempleMath.IsStartingTownId(""));
            Assert.AreEqual(2, TempleMath.StartingTownIds.Length, "The Temple should keep exactly two starting towns.");
        }


        [Test]
        public void TempleMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(TempleMath.IsStartingTownId("TOWN_v2"));
            Assert.IsTrue(TempleMath.IsStartingTownId("Town_V3"));
        }


        [Test]
        public void TempleMath_JoinGate_RefusesDeviousCalculatingMinds()
        {
            Assert.IsFalse(TempleMath.QualifiesForTemple(2, 0));  // devious (Calculating >= 2)
            Assert.IsFalse(TempleMath.QualifiesForTemple(3, 2));  // devious even with high mercy
            Assert.IsTrue(TempleMath.QualifiesForTemple(1, 0));   // not devious enough
        }


        [Test]
        public void TempleMath_JoinGate_RefusesCruelHearts()
        {
            Assert.IsFalse(TempleMath.QualifiesForTemple(0, -2)); // cruel (Mercy <= -2)
            Assert.IsFalse(TempleMath.QualifiesForTemple(0, -3));
            Assert.IsTrue(TempleMath.QualifiesForTemple(0, -1));  // not cruel enough
        }


        [Test]
        public void TempleMath_JoinGate_AcceptsAnOrdinaryHero()
        {
            Assert.IsTrue(TempleMath.QualifiesForTemple(0, 0));
            Assert.IsTrue(TempleMath.QualifiesForTemple(1, 2));
        }


        [Test]
        public void TempleMath_IsDevious_MatchesThresholdExactly()
        {
            Assert.IsFalse(TempleMath.IsDevious(1));
            Assert.IsTrue(TempleMath.IsDevious(2));
            Assert.IsTrue(TempleMath.IsDevious(3));
        }


        [Test]
        public void TempleMath_IsCruel_MatchesThresholdExactly()
        {
            Assert.IsFalse(TempleMath.IsCruel(-1));
            Assert.IsTrue(TempleMath.IsCruel(-2));
            Assert.IsTrue(TempleMath.IsCruel(-3));
        }


        [Test]
        public void TempleMath_SigilTunables_ArePositive()
        {
            Assert.Greater(TempleMath.SigilPurchaseCostGold, 0);
            Assert.Greater(TempleMath.SigilOnHitDemonDamage, 0f);
            Assert.Greater(TempleMath.SigilOnHitDemonRadius, 0f);
            Assert.Greater(TempleMath.SigilOnBlockMoraleGain, 0f);
        }


        [Test]
        public void TempleMath_QualifiesToPray_RespectsFloor()
        {
            Assert.IsTrue(TempleMath.QualifiesToPray(0, 0));   // exactly at the floor
            Assert.IsTrue(TempleMath.QualifiesToPray(2, 1));
            Assert.IsFalse(TempleMath.QualifiesToPray(-1, -1)); // below the floor
        }


        [Test]
        public void TempleMath_PrayerVirtueScale_IsOneAtFloorAndGrowsAboveIt()
        {
            Assert.AreEqual(1f, TempleMath.PrayerVirtueScale(0, 0), 0.0001f);
            Assert.Greater(TempleMath.PrayerVirtueScale(2, 2), TempleMath.PrayerVirtueScale(0, 0));
        }


        [Test]
        public void TempleMath_PrayerVirtueScale_IsCappedAtMax()
        {
            float scale = TempleMath.PrayerVirtueScale(2, 2); // Honor+Mercy max out at 2 each
            Assert.LessOrEqual(scale, TempleMath.PrayerMaxVirtueScale);
        }


        [Test]
        public void TempleMath_PrayerMoraleGain_ScalesWithVirtue()
        {
            float low = TempleMath.PrayerMoraleGain(0, 0);
            float high = TempleMath.PrayerMoraleGain(2, 2);
            Assert.Greater(high, low);
            Assert.AreEqual(TempleMath.PrayerBaseMoraleGain, low, 0.0001f);
        }


        [Test]
        public void TempleMath_PrayerHealFraction_ScalesWithVirtue()
        {
            float low = TempleMath.PrayerHealFraction(0, 0);
            float high = TempleMath.PrayerHealFraction(2, 2);
            Assert.Greater(high, low);
            Assert.AreEqual(TempleMath.PrayerBaseHealFraction, low, 0.0001f);
        }


        [Test]
        public void TempleMath_IsPrayerReady_TrueAfterCooldownFalseWithin()
        {
            Assert.IsTrue(TempleMath.IsPrayerReady(-1f));  // never prayed
            Assert.IsFalse(TempleMath.IsPrayerReady(0f));
            Assert.IsFalse(TempleMath.IsPrayerReady(TempleMath.PrayerCooldownDays - 0.01f));
            Assert.IsTrue(TempleMath.IsPrayerReady(TempleMath.PrayerCooldownDays));
        }


        [Test]
        public void TempleMath_NpcDailyPrayChance_IsAValidProbability()
        {
            Assert.Greater(TempleMath.NpcDailyPrayChance, 0.0);
            Assert.Less(TempleMath.NpcDailyPrayChance, 1.0);
        }
    }
}
