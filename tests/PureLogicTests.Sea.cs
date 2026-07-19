using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── SeaMath tests ─────────────────────────────────────────────────────

        [Test]
        public void SeaMath_TravelHours_ClampsToMinimum()
        {
            Assert.AreEqual(SeaMath.MinVoyageHours, SeaMath.TravelHours(0f, false));
            Assert.AreEqual(SeaMath.MinVoyageHours, SeaMath.TravelHours(10f, false));
        }


        [Test]
        public void SeaMath_TravelHours_EmberwindHalvesLongCrossings()
        {
            float normal = SeaMath.TravelHours(400f, false);
            float windy  = SeaMath.TravelHours(400f, true);
            Assert.AreEqual(normal * SeaMath.EmberwindTimeMult, windy, 0.001f);
        }


        [Test]
        public void SeaMath_TravelHours_GrowsWithDistance()
        {
            Assert.Greater(SeaMath.TravelHours(600f, false), SeaMath.TravelHours(300f, false));
        }


        [Test]
        public void SeaMath_Fare_RoundsToTenAndHasFloor()
        {
            Assert.AreEqual(0, SeaMath.Fare(123f, 17) % 10);
            Assert.GreaterOrEqual(SeaMath.Fare(0f, 0), 50);
        }


        [Test]
        public void SeaMath_Fare_GrowsWithDistanceAndPartySize()
        {
            Assert.Greater(SeaMath.Fare(500f, 50), SeaMath.Fare(200f, 50));
            Assert.Greater(SeaMath.Fare(200f, 100), SeaMath.Fare(200f, 10));
        }


        [Test]
        public void SeaMath_PirateChance_StaysWithinBounds()
        {
            Assert.AreEqual(SeaMath.PirateChanceFloor, SeaMath.PirateChance(0f));
            Assert.AreEqual(SeaMath.PirateChanceCeiling, SeaMath.PirateChance(99999f));
            float mid = SeaMath.PirateChance(300f);
            Assert.GreaterOrEqual(mid, SeaMath.PirateChanceFloor);
            Assert.LessOrEqual(mid, SeaMath.PirateChanceCeiling);
        }


        [Test]
        public void SeaMath_AshenAdjusted_RaisesHazardForAshenPorts()
        {
            float baseChance = 0.20f;
            // Non-Ashen destination: chance is unchanged.
            Assert.AreEqual(baseChance, SeaMath.AshenAdjusted(baseChance, false), 0.0001f);
            // Ashen destination: chance is lifted by the multiplier.
            Assert.AreEqual(baseChance * SeaMath.AshenPortHazardMult,
                            SeaMath.AshenAdjusted(baseChance, true), 0.0001f);
            // …but never reaches certainty, even from an already-high base.
            Assert.LessOrEqual(SeaMath.AshenAdjusted(0.9f, true), 0.95f);
        }


        [Test]
        public void SeaMath_FleetStrength_SearTheTideMultiplies()
        {
            float baseStr = SeaMath.FleetStrength(60, 3f, 100, false);
            float seared  = SeaMath.FleetStrength(60, 3f, 100, true);
            Assert.AreEqual(baseStr * SeaMath.SearTheTideStrengthMult, seared, 0.001f);
        }


        [Test]
        public void SeaMath_FleetStrength_MoreMenIsStronger()
        {
            Assert.Greater(SeaMath.FleetStrength(100, 2f, 0, false),
                           SeaMath.FleetStrength(50, 2f, 0, false));
        }


        [Test]
        public void SeaMath_ResolveSeaBattle_OverwhelmingPlayerWinsCheaply()
        {
            var o = SeaMath.ResolveSeaBattle(1000f, 100f, 0.0);
            Assert.IsTrue(o.Victory);
            Assert.Greater(o.LootGold, 0);
            Assert.LessOrEqual(o.CasualtyFraction, 0.05f);
        }


        [Test]
        public void SeaMath_ResolveSeaBattle_OverwhelmingCorsairsWin()
        {
            var o = SeaMath.ResolveSeaBattle(100f, 1000f, 0.99);
            Assert.IsFalse(o.Victory);
            Assert.AreEqual(0, o.LootGold);
        }


        [Test]
        public void SeaMath_ResolveSeaBattle_CasualtiesStayInBounds()
        {
            foreach (var roll in new[] { 0.0, 0.5, 0.99 })
            {
                var win  = SeaMath.ResolveSeaBattle(500f, 50f, roll);
                var loss = SeaMath.ResolveSeaBattle(50f, 5000f, roll);
                Assert.GreaterOrEqual(win.CasualtyFraction, 0.02f);
                Assert.LessOrEqual(win.CasualtyFraction, 0.35f);
                Assert.GreaterOrEqual(loss.CasualtyFraction, 0.02f);
                Assert.LessOrEqual(loss.CasualtyFraction, 0.35f);
            }
        }


        [Test]
        public void SeaMath_ResolveSeaBattle_ZeroStrengthIsDefeatNotCrash()
        {
            var o = SeaMath.ResolveSeaBattle(0f, 100f, 0.5);
            Assert.IsFalse(o.Victory);
            Assert.AreEqual(0, o.LootGold);
        }


        [Test]
        public void SeaMath_TributeDemand_HasFloor()
        {
            Assert.GreaterOrEqual(SeaMath.TributeDemand(0, 0f), 200);
        }


        [Test]
        public void SeaMath_StormExtraHours_AtLeastTwo()
        {
            Assert.GreaterOrEqual(SeaMath.StormExtraHours(0f, 0.0), 2);
            Assert.GreaterOrEqual(SeaMath.StormExtraHours(40f, 0.5), 2);
        }


        [Test]
        public void SeaMath_VentureDays_FloorAndGrowth()
        {
            Assert.GreaterOrEqual(SeaMath.VentureDays(0f), 3);
            Assert.Greater(SeaMath.VentureDays(800f), SeaMath.VentureDays(200f));
        }


        [Test]
        public void SeaMath_VentureLossChance_BlessingHalves()
        {
            float bare    = SeaMath.VentureLossChance(400f, false);
            float blessed = SeaMath.VentureLossChance(400f, true);
            Assert.AreEqual(bare * 0.5f, blessed, 0.0001f);
            Assert.Less(bare, 1f);
        }


        [Test]
        public void SeaMath_ResolveVenture_LossPaysSalvage()
        {
            // lossRoll of 0 is always below any positive loss chance
            var o = SeaMath.ResolveVenture(2000, 400f, false, 0.0, 0.5);
            Assert.IsTrue(o.Lost);
            Assert.AreEqual(500, o.Payout);
        }


        [Test]
        public void SeaMath_ResolveVenture_SafeRunProfits()
        {
            // lossRoll of 1.0 is never below the loss chance
            var o = SeaMath.ResolveVenture(2000, 400f, false, 1.0, 0.5);
            Assert.IsFalse(o.Lost);
            Assert.Greater(o.Payout, 2000);
        }


        [Test]
        public void SeaMath_ResolveVenture_BlessingImprovesMargin()
        {
            var bare    = SeaMath.ResolveVenture(2000, 400f, false, 1.0, 0.5);
            var blessed = SeaMath.ResolveVenture(2000, 400f, true, 1.0, 0.5);
            Assert.Greater(blessed.Payout, bare.Payout);
        }


        [Test]
        public void SeaMath_VentureTiers_AreAscending()
        {
            for (int i = 1; i < SeaMath.VentureTiers.Length; i++)
                Assert.Greater(SeaMath.VentureTiers[i], SeaMath.VentureTiers[i - 1]);
        }


        [Test]
        public void SeaMath_NpcCrossingViable_RejectsShortHops()
        {
            Assert.IsFalse(SeaMath.NpcCrossingViable(SeaMath.NpcMinCrossing - 1f, false));
            Assert.IsFalse(SeaMath.NpcCrossingViable(SeaMath.NpcMinCrossing - 1f, true));
            Assert.IsTrue(SeaMath.NpcCrossingViable(SeaMath.NpcMinCrossing, false));
            Assert.IsTrue(SeaMath.NpcCrossingViable(SeaMath.NpcMinCrossing, true));
        }


        [Test]
        public void SeaMath_NpcCrossingViable_CapsCaravansOnly()
        {
            float beyond = SeaMath.NpcMaxCaravanCrossing + 1f;
            Assert.IsFalse(SeaMath.NpcCrossingViable(beyond, caravan: true));
            Assert.IsTrue(SeaMath.NpcCrossingViable(beyond, caravan: false));
        }


        [Test]
        public void SeaMath_BlockadeInterceptChance_ZeroStrengthIsZero()
        {
            Assert.AreEqual(0f, SeaMath.BlockadeInterceptChance(0f, 100f));
        }


        [Test]
        public void SeaMath_BlockadeInterceptChance_StrongerBlockadeRaisesChance()
        {
            float weak   = SeaMath.BlockadeInterceptChance(50f,  200f);
            float strong = SeaMath.BlockadeInterceptChance(200f, 200f);
            Assert.Greater(strong, weak);
        }


        [Test]
        public void SeaMath_BlockadeInterceptChance_ClampedToValidRange()
        {
            // Overwhelming blockade should not exceed 0.90
            Assert.LessOrEqual(SeaMath.BlockadeInterceptChance(99999f, 1f), 0.90f);
            // Tiny blockade vs massive fleet should be at least 0.20
            Assert.GreaterOrEqual(SeaMath.BlockadeInterceptChance(1f, 99999f), 0.20f);
        }


        [Test]
        public void SeaMath_NpcInvasionSailChance_LowerThanNormalSailChance()
        {
            Assert.Less(SeaMath.NpcInvasionSailChance, SeaMath.NpcLordSailChance);
        }
    }
}
