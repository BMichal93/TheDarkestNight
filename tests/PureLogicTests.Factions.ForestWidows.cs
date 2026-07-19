using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── ForestWidowsMath tests (Phase 7, Faction C) ─────────────────────────

        [Test]
        public void ForestWidowsMath_IsStartingTownId_MatchesOnlyMarunathAndCarBanseth()
        {
            Assert.IsTrue(ForestWidowsMath.IsStartingTownId("town_B1"));  // Marunath
            Assert.IsTrue(ForestWidowsMath.IsStartingTownId("town_B3"));  // Car Banseth
            Assert.IsFalse(ForestWidowsMath.IsStartingTownId("town_B2")); // Dunglanys
            Assert.IsFalse(ForestWidowsMath.IsStartingTownId("town_B4")); // Seonon
            Assert.IsFalse(ForestWidowsMath.IsStartingTownId("town_B5")); // Pen Cannoc
            Assert.IsFalse(ForestWidowsMath.IsStartingTownId(null));
            Assert.IsFalse(ForestWidowsMath.IsStartingTownId(""));
            Assert.AreEqual(2, ForestWidowsMath.StartingTownIds.Length, "The Forest Widows should keep exactly two starting towns.");
        }


        [Test]
        public void ForestWidowsMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(ForestWidowsMath.IsStartingTownId("TOWN_b1"));
            Assert.IsTrue(ForestWidowsMath.IsStartingTownId("Town_B3"));
        }


        [Test]
        public void ForestWidowsMath_RollVanishDays_StaysWithinOneToFourWeeks()
        {
            for (double roll = 0.0; roll <= 1.0; roll += 0.05)
            {
                float days = ForestWidowsMath.RollVanishDays(roll);
                Assert.GreaterOrEqual(days, ForestWidowsMath.MinVanishDays);
                Assert.LessOrEqual(days, ForestWidowsMath.MaxVanishDays);
            }
            Assert.AreEqual(ForestWidowsMath.MinVanishDays, ForestWidowsMath.RollVanishDays(0.0));
            Assert.AreEqual(ForestWidowsMath.MaxVanishDays, ForestWidowsMath.RollVanishDays(1.0));
        }


        [Test]
        public void ForestWidowsMath_RollLordSacrificeImmunityDays_StaysWithinBounds()
        {
            for (double roll = 0.0; roll <= 1.0; roll += 0.05)
            {
                float days = ForestWidowsMath.RollLordSacrificeImmunityDays(roll);
                Assert.GreaterOrEqual(days, ForestWidowsMath.MinLordSacrificeImmunityDays);
                Assert.LessOrEqual(days, ForestWidowsMath.MaxLordSacrificeImmunityDays);
            }
        }


        [Test]
        public void ForestWidowsMath_ExtendIgnoreExpiry_StacksInsteadOfOverwriting()
        {
            // No active window yet (currentExpiry == today): a 2-day grant simply
            // starts a fresh 2-day window from today.
            float extended = ForestWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 100f, grantDays: 2f);
            Assert.AreEqual(102f, extended);

            // An active window already runs to day 105: a further 3-day grant
            // extends it to day 108, not day 103 (today + grant) and not merely
            // day 105 (unchanged) — it STACKS on top of the remaining time.
            float stacked = ForestWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 105f, grantDays: 3f);
            Assert.AreEqual(108f, stacked);
        }


        [Test]
        public void ForestWidowsMath_ExtendIgnoreExpiry_IgnoresAnExpiredWindow()
        {
            // The old window already lapsed (expiry before today) — the new grant
            // should count from today, not from the stale expiry day.
            float extended = ForestWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 40f, grantDays: 5f);
            Assert.AreEqual(105f, extended);
        }


        [Test]
        public void ForestWidowsMath_IsIgnoreActive_ComparesAgainstExpiry()
        {
            Assert.IsTrue(ForestWidowsMath.IsIgnoreActive(today: 100f, expiryDay: 101f));
            Assert.IsFalse(ForestWidowsMath.IsIgnoreActive(today: 101f, expiryDay: 101f));
            Assert.IsFalse(ForestWidowsMath.IsIgnoreActive(today: 102f, expiryDay: 101f));
        }


        [Test]
        public void ForestWidowsMath_RollSelfSacrifice_RespectsChanceBoundary()
        {
            Assert.IsTrue(ForestWidowsMath.RollSelfSacrifice(0.0));
            Assert.IsFalse(ForestWidowsMath.RollSelfSacrifice(ForestWidowsMath.SelfSacrificeChance));
            Assert.IsFalse(ForestWidowsMath.RollSelfSacrifice(0.999));
        }


        [Test]
        public void ForestWidowsMath_SelfSacrificeChance_IsSmall()
        {
            Assert.Greater(ForestWidowsMath.SelfSacrificeChance, 0.0);
            Assert.Less(ForestWidowsMath.SelfSacrificeChance, 0.2, "The self-sacrifice roll should read as a small chance, not a coin flip.");
        }


        [Test]
        public void ForestWidowsMath_InfluenceAfterDailyDrain_DrainsOnlyAMaleMember()
        {
            Assert.AreEqual(0f, ForestWidowsMath.InfluenceAfterDailyDrain(isMale: true, isMember: true, currentInfluence: 250f));
            Assert.AreEqual(250f, ForestWidowsMath.InfluenceAfterDailyDrain(isMale: false, isMember: true, currentInfluence: 250f));
            Assert.AreEqual(250f, ForestWidowsMath.InfluenceAfterDailyDrain(isMale: true, isMember: false, currentInfluence: 250f));
            Assert.AreEqual(250f, ForestWidowsMath.InfluenceAfterDailyDrain(isMale: false, isMember: false, currentInfluence: 250f));
        }


        [Test]
        public void ForestWidowsMath_DemonTroopsGrantedPerMaleSacrifice_IsPositive()
        {
            Assert.Greater(ForestWidowsMath.DemonTroopsGrantedPerMaleSacrifice, 0);
        }
    }
}
