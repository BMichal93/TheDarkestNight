using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── ExpeditionMath (Legion Expeditions / "Antiquarian Charter") ────────

        [Test]
        public void ExpeditionMath_BaseSuccessChance_DecreasesWithTier()
        {
            Assert.AreEqual(75, ExpeditionMath.BaseSuccessChance(RuinTier.Easy));
            Assert.AreEqual(60, ExpeditionMath.BaseSuccessChance(RuinTier.Standard));
            Assert.AreEqual(45, ExpeditionMath.BaseSuccessChance(RuinTier.Brutal));
            Assert.AreEqual(30, ExpeditionMath.BaseSuccessChance(RuinTier.Legendary));
        }


        [Test]
        public void ExpeditionMath_SuccessChance_ScholarHelpsOnlyOnHighTierRuins()
        {
            int lowTierWithScholar  = ExpeditionMath.SuccessChance(RuinTier.Easy, ExpeditionLeaderSpecialty.ScholarOfTheOldScript, ExpeditionTeamType.HiredBlades, false);
            int lowTierNoScholar    = ExpeditionMath.SuccessChance(RuinTier.Easy, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.HiredBlades, false);
            Assert.AreEqual(lowTierNoScholar, lowTierWithScholar); // no bonus below Brutal

            int highTierWithScholar = ExpeditionMath.SuccessChance(RuinTier.Brutal, ExpeditionLeaderSpecialty.ScholarOfTheOldScript, ExpeditionTeamType.HiredBlades, false);
            int highTierNoScholar   = ExpeditionMath.SuccessChance(RuinTier.Brutal, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.HiredBlades, false);
            Assert.Greater(highTierWithScholar, highTierNoScholar);
        }


        [Test]
        public void ExpeditionMath_SuccessChance_ProvenLeaderAddsFlatBonus()
        {
            int unproven = ExpeditionMath.SuccessChance(RuinTier.Standard, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.ImperialScholars, false);
            int proven   = ExpeditionMath.SuccessChance(RuinTier.Standard, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.ImperialScholars, true);
            Assert.AreEqual(unproven + 5, proven);
        }


        [Test]
        public void ExpeditionMath_SuccessChance_ClampsToBounds()
        {
            int floor = ExpeditionMath.SuccessChance(RuinTier.Legendary, ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads, ExpeditionTeamType.HiredBlades, false);
            Assert.GreaterOrEqual(floor, ExpeditionMath.MinSuccessChance);
            int ceiling = ExpeditionMath.SuccessChance(RuinTier.Easy, ExpeditionLeaderSpecialty.ZealousAntiquarian, ExpeditionTeamType.ImperialScholars, true);
            Assert.LessOrEqual(ceiling, ExpeditionMath.MaxSuccessChance);
        }


        [Test]
        public void ExpeditionMath_DurationDays_FollowsSixPlusTwoTimesTierFormula()
        {
            int baseEasy = ExpeditionMath.DurationDays(RuinTier.Easy, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.ImperialScholars);
            Assert.AreEqual(6 + 2 * (int)RuinTier.Easy, baseEasy);

            int baseLegendary = ExpeditionMath.DurationDays(RuinTier.Legendary, ExpeditionLeaderSpecialty.TombRobber, ExpeditionTeamType.ImperialScholars);
            Assert.AreEqual(6 + 2 * (int)RuinTier.Legendary, baseLegendary);
        }


        [Test]
        public void ExpeditionMath_DurationDays_VeteranIsFasterScholarIsSlower()
        {
            int veteran = ExpeditionMath.DurationDays(RuinTier.Standard, ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads, ExpeditionTeamType.ImperialScholars);
            int scholar = ExpeditionMath.DurationDays(RuinTier.Standard, ExpeditionLeaderSpecialty.ScholarOfTheOldScript, ExpeditionTeamType.ImperialScholars);
            Assert.Less(veteran, scholar);
        }


        [Test]
        public void ExpeditionMath_DurationDays_NeverDropsBelowFloor()
        {
            int days = ExpeditionMath.DurationDays(RuinTier.Easy, ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads, ExpeditionTeamType.HiredBlades);
            Assert.GreaterOrEqual(days, ExpeditionMath.MinDurationDays);
        }


        [Test]
        public void ExpeditionMath_InfluenceCost_ScalesWithTierAndClampsToBounds()
        {
            int easy      = ExpeditionMath.InfluenceCost(RuinTier.Easy, ExpeditionTeamType.ImperialScholars);
            int legendary = ExpeditionMath.InfluenceCost(RuinTier.Legendary, ExpeditionTeamType.ImperialScholars);
            Assert.Less(easy, legendary);
            Assert.GreaterOrEqual(easy, ExpeditionMath.MinInfluenceCost);
            Assert.LessOrEqual(legendary, ExpeditionMath.MaxInfluenceCost);
        }


        [Test]
        public void ExpeditionMath_InfluenceCost_HiredBladesCheaperThanLegionVeterans()
        {
            int blades   = ExpeditionMath.InfluenceCost(RuinTier.Standard, ExpeditionTeamType.HiredBlades);
            int veterans = ExpeditionMath.InfluenceCost(RuinTier.Standard, ExpeditionTeamType.LegionVeterans);
            Assert.Less(blades, veterans);
        }


        // ── ExpeditionMath.GoldCost (The Camp — moved off influence) ───────────

        [Test]
        public void ExpeditionMath_GoldCost_IsInfluenceCostTimesTheFlatMultiplier()
        {
            foreach (RuinTier tier in System.Enum.GetValues(typeof(RuinTier)))
            {
                foreach (ExpeditionTeamType team in System.Enum.GetValues(typeof(ExpeditionTeamType)))
                {
                    int expected = ExpeditionMath.InfluenceCost(tier, team) * ExpeditionMath.GoldPerInfluenceUnit;
                    Assert.AreEqual(expected, ExpeditionMath.GoldCost(tier, team));
                }
            }
        }


        [Test]
        public void ExpeditionMath_GoldCost_TierOneIsAffordableAndTopTierStings()
        {
            int cheapest  = ExpeditionMath.GoldCost(RuinTier.Easy, ExpeditionTeamType.HiredBlades);
            int priciest  = ExpeditionMath.GoldCost(RuinTier.Legendary, ExpeditionTeamType.LegionVeterans);
            Assert.Less(cheapest, priciest);
            Assert.GreaterOrEqual(cheapest, ExpeditionMath.MinInfluenceCost * ExpeditionMath.GoldPerInfluenceUnit);
            Assert.LessOrEqual(priciest, ExpeditionMath.MaxInfluenceCost * ExpeditionMath.GoldPerInfluenceUnit);
        }


        [Test]
        public void ExpeditionMath_SuccessGold_ScalesWithTierAndLeaderTrait()
        {
            int lowTier  = ExpeditionMath.SuccessGold(RuinTier.Easy, ExpeditionLeaderSpecialty.ZealousAntiquarian, 0);
            int highTier = ExpeditionMath.SuccessGold(RuinTier.Legendary, ExpeditionLeaderSpecialty.ZealousAntiquarian, 0);
            Assert.Greater(highTier, lowTier);

            int veteranGold = ExpeditionMath.SuccessGold(RuinTier.Standard, ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads, 0);
            int tombRobberGold = ExpeditionMath.SuccessGold(RuinTier.Standard, ExpeditionLeaderSpecialty.TombRobber, 0);
            Assert.Less(veteranGold, tombRobberGold);
        }


        [Test]
        public void ExpeditionMath_SuccessRenown_ScalesWithTier()
        {
            Assert.AreEqual(9,  ExpeditionMath.SuccessRenown(RuinTier.Easy));
            Assert.AreEqual(21, ExpeditionMath.SuccessRenown(RuinTier.Legendary));
        }


        [Test]
        public void ExpeditionMath_RollGrantsCrystal_RespectsTierScaledThreshold()
        {
            Assert.IsTrue(ExpeditionMath.RollGrantsCrystal(0, RuinTier.Easy));
            Assert.IsTrue(ExpeditionMath.RollGrantsCrystal(24, RuinTier.Easy));  // threshold is 25 for Easy
            Assert.IsFalse(ExpeditionMath.RollGrantsCrystal(25, RuinTier.Easy));
            Assert.IsTrue(ExpeditionMath.RollGrantsCrystal(39, RuinTier.Legendary)); // threshold is 40 for Legendary
            Assert.IsFalse(ExpeditionMath.RollGrantsCrystal(40, RuinTier.Legendary));
        }


        [Test]
        public void ExpeditionMath_RollGrantsRelic_RespectsTierScaledThreshold()
        {
            Assert.IsTrue(ExpeditionMath.RollGrantsRelic(0, RuinTier.Easy));
            Assert.IsFalse(ExpeditionMath.RollGrantsRelic(8, RuinTier.Easy)); // threshold is 8 for Easy
            Assert.IsTrue(ExpeditionMath.RollGrantsRelic(16, RuinTier.Legendary)); // threshold is 17 for Legendary
        }


        [Test]
        public void ExpeditionMath_TombRobberAbscondFraction_HasABoundary()
        {
            Assert.AreEqual(0.4f, ExpeditionMath.TombRobberAbscondFraction(24));
            Assert.AreEqual(0f,   ExpeditionMath.TombRobberAbscondFraction(25));
        }


        [Test]
        public void ExpeditionMath_RollAshenTakesNote_HasABoundary()
        {
            Assert.IsTrue(ExpeditionMath.RollAshenTakesNote(11));
            Assert.IsFalse(ExpeditionMath.RollAshenTakesNote(12));
        }


        [Test]
        public void ExpeditionMath_LeaderLostOnFailure_LegionVeteransMitigateLoss()
        {
            int withoutVeterans = ExpeditionMath.LeaderLostOnFailure(20, RuinTier.Legendary, ExpeditionTeamType.HiredBlades) ? 1 : 0;
            int withVeterans    = ExpeditionMath.LeaderLostOnFailure(20, RuinTier.Legendary, ExpeditionTeamType.LegionVeterans) ? 1 : 0;
            Assert.GreaterOrEqual(withoutVeterans, withVeterans);

            // Boundary: Easy tier chance is 18, Legion Veterans knock it to 8.
            Assert.IsTrue(ExpeditionMath.LeaderLostOnFailure(7, RuinTier.Easy, ExpeditionTeamType.LegionVeterans));
            Assert.IsFalse(ExpeditionMath.LeaderLostOnFailure(8, RuinTier.Easy, ExpeditionTeamType.LegionVeterans));
        }


        // ── ExpeditionMath (v0.8.0, issue 12 — The Camp's own NPC charters) ─────
        [Test]
        public void ExpeditionMath_RollNpcExpeditionStarts_RespectsChanceBoundary()
        {
            Assert.IsTrue(ExpeditionMath.RollNpcExpeditionStarts(0));
            Assert.IsTrue(ExpeditionMath.RollNpcExpeditionStarts(ExpeditionMath.NpcExpeditionChancePercentPerWeek - 1));
            Assert.IsFalse(ExpeditionMath.RollNpcExpeditionStarts(ExpeditionMath.NpcExpeditionChancePercentPerWeek));
            Assert.IsFalse(ExpeditionMath.RollNpcExpeditionStarts(99));
        }


        [Test]
        public void ExpeditionMath_RollNpcExpeditionDays_StaysWithinConfiguredRange()
        {
            var rng = new Random(9);
            for (int i = 0; i < 100; i++)
            {
                int days = ExpeditionMath.RollNpcExpeditionDays(rng);
                Assert.GreaterOrEqual(days, ExpeditionMath.NpcExpeditionMinDays);
                Assert.LessOrEqual(days, ExpeditionMath.NpcExpeditionMaxDays);
            }
        }
    }
}
