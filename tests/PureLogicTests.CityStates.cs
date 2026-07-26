using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── CityStateMath tests (Phase 8 — the wretched free towns) ────────────

        [Test]
        public void CityStateMath_ShouldBeginConversion_RespectsSettleDelay()
        {
            Assert.IsFalse(CityStateMath.ShouldBeginConversion(0));
            Assert.IsFalse(CityStateMath.ShouldBeginConversion(CityStateMath.SettleDelayDays - 1));
            Assert.IsTrue(CityStateMath.ShouldBeginConversion(CityStateMath.SettleDelayDays));
            Assert.IsTrue(CityStateMath.ShouldBeginConversion(CityStateMath.SettleDelayDays + 10));
        }


        [Test]
        public void CityStateMath_CityStateKingdomId_IsPrefixedAndStable()
        {
            Assert.AreEqual("citystate_clan_test", CityStateMath.CityStateKingdomId("clan_test"));
            Assert.AreEqual(
                CityStateMath.CityStateKingdomId("clan_test"),
                CityStateMath.CityStateKingdomId("clan_test"),
                "The same clan must always map to the same city-state kingdom id (reload-safety depends on this).");
            Assert.IsNull(CityStateMath.CityStateKingdomId(null));
            Assert.IsNull(CityStateMath.CityStateKingdomId(""));
        }


        [Test]
        public void CityStateMath_IsCityStateKingdomId_OnlyMatchesThePrefix()
        {
            Assert.IsTrue(CityStateMath.IsCityStateKingdomId("citystate_clan_test"));
            Assert.IsFalse(CityStateMath.IsCityStateKingdomId("clan_test"));
            Assert.IsFalse(CityStateMath.IsCityStateKingdomId("ashen_kingdom"));
            Assert.IsFalse(CityStateMath.IsCityStateKingdomId(null));
        }


        [Test]
        public void CityStateMath_CityStateKingdomName_UsesClanNameConvention()
        {
            Assert.AreEqual("Clan Uxkhal", CityStateMath.CityStateKingdomName("Uxkhal"));
            Assert.AreEqual("Clan Unknown", CityStateMath.CityStateKingdomName(null));
        }


        [Test]
        public void CityStateMath_BanditCultureIdFor_MapsEachCoreCultureToABanditCulture()
        {
            Assert.AreEqual("sea_raiders",     CityStateMath.BanditCultureIdFor("empire"));
            Assert.AreEqual("mountain_bandits", CityStateMath.BanditCultureIdFor("sturgia"));
            Assert.AreEqual("forest_bandits",  CityStateMath.BanditCultureIdFor("vlandia"));
            Assert.AreEqual("steppe_bandits",  CityStateMath.BanditCultureIdFor("khuzait"));
            Assert.AreEqual("desert_bandits",  CityStateMath.BanditCultureIdFor("aserai"));
            Assert.AreEqual("forest_bandits",  CityStateMath.BanditCultureIdFor("battania"));
            Assert.AreEqual("looters",         CityStateMath.BanditCultureIdFor("some_unknown_culture"));
            Assert.AreEqual("looters",         CityStateMath.BanditCultureIdFor(null));
        }


        [Test]
        public void CityStateMath_BanditCultureIdFor_IsCaseInsensitive()
        {
            Assert.AreEqual("steppe_bandits", CityStateMath.BanditCultureIdFor("KHUZAIT"));
        }


        // ── CityStateMath tests — culture normalization helpers ─────────────────

        [Test]
        public void CityStateMath_StableHash_IsDeterministicAndNonNegative()
        {
            Assert.AreEqual(CityStateMath.StableHash("town_V2"), CityStateMath.StableHash("town_V2"));
            Assert.AreNotEqual(CityStateMath.StableHash("town_V2"), CityStateMath.StableHash("town_A4"));
            Assert.GreaterOrEqual(CityStateMath.StableHash("town_V2"), 0);
            Assert.GreaterOrEqual(CityStateMath.StableHash(null), 0);
        }


        [Test]
        public void CityStateMath_OtherCultureIdFor_ReturnsABaseCultureDeterministically()
        {
            string first = CityStateMath.OtherCultureIdFor("town_V2");
            Assert.AreEqual(first, CityStateMath.OtherCultureIdFor("town_V2")); // stable
            CollectionAssert.Contains(CityStateMath.BaseCultureIds, first);     // always a base culture
        }


        [Test]
        public void CityStateMath_BaseCultureIds_AreTheSixVanillaKingdomCultures()
        {
            CollectionAssert.AreEquivalent(
                new[] { "empire", "sturgia", "aserai", "vlandia", "battania", "khuzait" },
                CityStateMath.BaseCultureIds);
        }


        // ── CityStateMath tests — The Camp (Revyl special-case) ─────────────────

        [Test]
        public void CityStateMath_IsRevylHomeSettlement_MatchesByNameCaseInsensitive()
        {
            Assert.IsTrue(CityStateMath.IsRevylHomeSettlement("Revyl"));
            Assert.IsTrue(CityStateMath.IsRevylHomeSettlement("REVYL"));
            Assert.IsTrue(CityStateMath.IsRevylHomeSettlement("revyl"));
            Assert.IsFalse(CityStateMath.IsRevylHomeSettlement("Sibir"));
            Assert.IsFalse(CityStateMath.IsRevylHomeSettlement(null));
            Assert.IsFalse(CityStateMath.IsRevylHomeSettlement(""));
        }


        [Test]
        public void CityStateMath_CampIdentity_IsFixedAndDistinctFromGenericCityStates()
        {
            Assert.AreEqual("The Camp", CityStateMath.CampKingdomName);
            Assert.AreNotEqual(CityStateMath.CampKingdomName, CityStateMath.CityStateKingdomName("Revyl"),
                "The Camp must never fall back to the generic \"Clan <X>\" naming convention.");
            Assert.IsFalse(string.IsNullOrEmpty(CityStateMath.CampRulerTitle));
            Assert.IsFalse(string.IsNullOrEmpty(CityStateMath.CampEncyclopediaText));
        }


        [Test]
        public void CityStateMath_CampColors_AreBlackAndWhite()
        {
            Assert.AreEqual(0xFF141414u, CityStateMath.CampPrimaryColor);
            Assert.AreEqual(0xFFF2F2F2u, CityStateMath.CampSecondaryColor);
        }


        // ── CityStateMath tests — the Children of the Forest (Pen Cannoc) ───────

        [Test]
        public void CityStateMath_IsPenCannocHomeSettlement_MatchesByNameCaseInsensitive()
        {
            Assert.IsTrue(CityStateMath.IsPenCannocHomeSettlement("Pen Cannoc"));
            Assert.IsTrue(CityStateMath.IsPenCannocHomeSettlement("PEN CANNOC"));
            Assert.IsTrue(CityStateMath.IsPenCannocHomeSettlement("pen cannoc"));
            Assert.IsFalse(CityStateMath.IsPenCannocHomeSettlement("Marunath"));
            Assert.IsFalse(CityStateMath.IsPenCannocHomeSettlement(null));
            Assert.IsFalse(CityStateMath.IsPenCannocHomeSettlement(""));
        }


        [Test]
        public void CityStateMath_ForestIdentity_IsFixedAndDistinctFromGenericCityStatesAndCamp()
        {
            Assert.AreEqual("Children of the Forest", CityStateMath.ForestKingdomName);
            Assert.AreNotEqual(CityStateMath.ForestKingdomName, CityStateMath.CampKingdomName);
            Assert.AreNotEqual(CityStateMath.ForestKingdomName, CityStateMath.CityStateKingdomName("Pen Cannoc"),
                "the Children of the Forest must not read as an ordinary generic city-state");
            Assert.IsFalse(string.IsNullOrEmpty(CityStateMath.ForestRulerTitle));
            Assert.IsFalse(string.IsNullOrEmpty(CityStateMath.ForestEncyclopediaText));
        }


        [Test]
        public void CityStateMath_ForestColors_AreForestGreenAndPale()
        {
            Assert.AreEqual(0xFF1B3A22u, CityStateMath.ForestPrimaryColor);
            Assert.AreEqual(0xFFE8E4C9u, CityStateMath.ForestSecondaryColor);
        }


        [Test]
        public void CityStateMath_SanctuaryKingdomNames_IncludesCampAndForest()
        {
            CollectionAssert.Contains(CityStateMath.SanctuaryKingdomNames, CityStateMath.CampKingdomName);
            CollectionAssert.Contains(CityStateMath.SanctuaryKingdomNames, CityStateMath.ForestKingdomName);
        }


        [Test]
        public void CityStateMath_ForestLordAgeWindow_NeverBelowComingOfAge()
        {
            // DefaultAgeModel.HeroComesOfAge is 18 — verified against the DLL.
            Assert.GreaterOrEqual(CityStateMath.ForestLordMinAge, 18f);
            Assert.Greater(CityStateMath.ForestLordMaxAge, CityStateMath.ForestLordMinAge);
            // Well below MiddleAdultHoodAge (35) — reads as young, not middle-aged.
            Assert.Less(CityStateMath.ForestLordMaxAge, 35f);
        }


        [Test]
        public void CityStateMath_ForestLordAgeDrifted_RespectsWindow()
        {
            Assert.IsFalse(CityStateMath.ForestLordAgeDrifted(CityStateMath.ForestLordMinAge));
            Assert.IsFalse(CityStateMath.ForestLordAgeDrifted(CityStateMath.ForestLordMaxAge));
            Assert.IsTrue(CityStateMath.ForestLordAgeDrifted(CityStateMath.ForestLordMinAge - 0.1));
            Assert.IsTrue(CityStateMath.ForestLordAgeDrifted(CityStateMath.ForestLordMaxAge + 0.1));
            Assert.IsTrue(CityStateMath.ForestLordAgeDrifted(50.0));
        }


        [Test]
        public void CityStateMath_ForestLordTargetAge_StaysInWindowAndIsDeterministic()
        {
            string[] ids = { "hero_a", "hero_b", "hero_c", "", null };
            foreach (var id in ids)
            {
                double age = CityStateMath.ForestLordTargetAge(id);
                Assert.GreaterOrEqual(age, CityStateMath.ForestLordMinAge);
                Assert.LessOrEqual(age, CityStateMath.ForestLordMaxAge);
                Assert.AreEqual(age, CityStateMath.ForestLordTargetAge(id));
            }
        }


        [Test]
        public void CityStateMath_ForestLordReanchorShiftDays_ZeroWhenAlreadyAtTarget()
        {
            Assert.AreEqual(0.0, CityStateMath.ForestLordReanchorShiftDays(19.0, 19.0), 1e-9);
        }


        [Test]
        public void CityStateMath_ForestLordReanchorShiftDays_PositiveWhenOlderThanTarget()
        {
            double shift = CityStateMath.ForestLordReanchorShiftDays(50.0, 18.0);
            Assert.Greater(shift, 0.0);
            Assert.AreEqual((50.0 - 18.0) * CityStateMath.DaysPerYear, shift, 1e-6);
        }
    }
}
