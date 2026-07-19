using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── FactionQuestMath tests (Phase 12 shared trigger) ─────────────────────

        [Test]
        public void FactionQuestMath_IsTriggerEligible_GatesAtDay50()
        {
            Assert.IsFalse(FactionQuestMath.IsTriggerEligible(0));
            Assert.IsFalse(FactionQuestMath.IsTriggerEligible(49));
            Assert.IsTrue(FactionQuestMath.IsTriggerEligible(50));
            Assert.IsTrue(FactionQuestMath.IsTriggerEligible(51));
            Assert.IsTrue(FactionQuestMath.IsTriggerEligible(10_000));
        }


        // ── WolfHuntMath tests (Phase 12, Faction A — The Great Hunt) ────────────

        [Test]
        public void WolfHuntMath_StageCount_IsThree()
        {
            Assert.AreEqual(3, WolfHuntMath.StageCount);
        }


        [Test]
        public void WolfHuntMath_IsValidStage_OnlyWithinRange()
        {
            Assert.IsFalse(WolfHuntMath.IsValidStage(-1));
            Assert.IsTrue(WolfHuntMath.IsValidStage(0));
            Assert.IsTrue(WolfHuntMath.IsValidStage(WolfHuntMath.StageCount - 1));
            Assert.IsFalse(WolfHuntMath.IsValidStage(WolfHuntMath.StageCount));
        }


        [Test]
        public void WolfHuntMath_BeastTier_EscalatesEveryStage()
        {
            Assert.AreEqual(DemonMath.DemonTier.Stalker,   WolfHuntMath.BeastTier(0));
            Assert.AreEqual(DemonMath.DemonTier.Ravager,   WolfHuntMath.BeastTier(1));
            Assert.AreEqual(DemonMath.DemonTier.Hellsteed, WolfHuntMath.BeastTier(2));
        }


        [Test]
        public void WolfHuntMath_PackSize_GrowsEveryStage()
        {
            int last = 0;
            for (int i = 0; i < WolfHuntMath.StageCount; i++)
            {
                int size = WolfHuntMath.PackSize(i);
                Assert.Greater(size, last, $"stage {i} pack should be larger than the previous stage's");
                last = size;
            }
        }


        [Test]
        public void WolfHuntMath_PackSize_SmallerThanOrdinaryNightTideParty()
        {
            // The Great Hunt's packs are deliberately smaller than an ordinary
            // night-tide party (DemonMath.MaxPartyBodies) — the challenge is
            // concentrated in the tier/health boost, not sheer numbers.
            for (int i = 0; i < WolfHuntMath.StageCount; i++)
                Assert.Less(WolfHuntMath.PackSize(i), DemonMath.MaxPartyBodies);
        }


        [Test]
        public void WolfHuntMath_BeastHealthMultiplier_EscalatesAndStaysBelowDemonLord()
        {
            float last = 1f;
            for (int i = 0; i < WolfHuntMath.StageCount; i++)
            {
                float mult = WolfHuntMath.BeastHealthMultiplier(i);
                Assert.Greater(mult, last, $"stage {i} health multiplier should exceed the previous stage's");
                Assert.Less(mult, ApocalypseMath.DemonLordHealthMultiplier,
                    "a mid-quest beast must never out-scale the campaign's actual endgame boss");
                last = mult;
            }
        }


        [Test]
        public void WolfHuntMath_FinalChoice_ConsequencesAreDistinctAndOpposedTraitShifts()
        {
            // Transformation hardens (Mercy down, Valor up); Burning partially
            // restores mercy — genuinely opposite mechanical directions, not
            // just different flavour text.
            Assert.Less(WolfHuntMath.TransformationMercyShift, 0);
            Assert.Greater(WolfHuntMath.TransformationValorShift, 0);
            Assert.Greater(WolfHuntMath.BurningMercyShift, 0);
            Assert.AreNotEqual(WolfHuntMath.TransformationMercyShift, WolfHuntMath.BurningMercyShift);
        }


        [Test]
        public void WolfHuntMath_FinalChoice_OnlyTransformationGrantsPermanentHp()
        {
            Assert.Greater(WolfHuntMath.TransformationHpBonus, 0f);
        }


        [Test]
        public void WolfHuntMath_FinalChoice_BurningRenownExceedsTransformationRenown()
        {
            // Word of restraint travels further than word of the kill — see
            // WolfHuntMath's header comment for the design reasoning.
            Assert.Greater(WolfHuntMath.BurningRenownGain, WolfHuntMath.TransformationRenownGain);
        }


        // ── TowerRiteMath tests (Phase 12, Faction B — The Unbinding Rite) ───────

        [Test]
        public void TowerRiteMath_MeetsGatherThreshold_RequiresAllThreeTracks()
        {
            int r = TowerRiteMath.GatherRelicsRequired;
            int b = TowerRiteMath.GatherDemonBloodRequired;
            int s = TowerRiteMath.GatherHolySigilsRequired;

            Assert.IsTrue(TowerRiteMath.MeetsGatherThreshold(r, b, s));
            Assert.IsFalse(TowerRiteMath.MeetsGatherThreshold(r - 1, b, s));
            Assert.IsFalse(TowerRiteMath.MeetsGatherThreshold(r, b - 1, s));
            Assert.IsFalse(TowerRiteMath.MeetsGatherThreshold(r, b, s - 1));
            Assert.IsTrue(TowerRiteMath.MeetsGatherThreshold(r + 5, b + 5, s + 5));
        }


        [Test]
        public void TowerRiteMath_GatherQuantities_AreGenuinelyScarce()
        {
            // Relics: at RelicMath's own drop chance, gathering the required
            // count takes real, sustained play — dozens of personally-won
            // demon-party victories, not a lucky single fight.
            double expectedRelicVictories = TowerRiteMath.GatherRelicsRequired / RelicMath.RelicDropChancePerVictory;
            Assert.Greater(expectedRelicVictories, 30);

            // Demon Blood and Holy Sigils are both required in more than a
            // token amount — never satisfiable by a single lucky drop or a
            // single cheap purchase.
            Assert.GreaterOrEqual(TowerRiteMath.GatherDemonBloodRequired, BloodboundMath.MaxDemonBloodPerVictory * 3);
            Assert.GreaterOrEqual(TowerRiteMath.GatherHolySigilsRequired, 3);
        }


        [Test]
        public void TowerRiteMath_HostTier_NeverProducesFiends()
        {
            var rng = new System.Random(1234);
            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual(DemonMath.DemonTier.Fiend, TowerRiteMath.HostTier(rng));
        }


        [Test]
        public void TowerRiteMath_HostPartySize_IsLargerThanOrdinaryNightTideParty()
        {
            Assert.Greater(TowerRiteMath.HostPartySizeEach, DemonMath.MaxPartyBodies);
            Assert.Greater(TowerRiteMath.HostPartyCount, 1);
        }


        [Test]
        public void TowerRiteMath_RaidChance_ExceedsOrdinaryNightTideAssaultChance()
        {
            // This IS the disaster, not a rare escalation of the ordinary tide.
            Assert.Greater(TowerRiteMath.RaidChancePerHostPerDay, DemonMath.SettlementAssaultChancePerPartyPerNight);
        }


        [Test]
        public void TowerRiteMath_IsRampageOver_GatesAtConfiguredDuration()
        {
            Assert.IsFalse(TowerRiteMath.IsRampageOver(TowerRiteMath.RampageDurationDays - 1));
            Assert.IsTrue(TowerRiteMath.IsRampageOver(TowerRiteMath.RampageDurationDays));
            Assert.IsTrue(TowerRiteMath.IsRampageOver(TowerRiteMath.RampageDurationDays + 100));
        }


        [Test]
        public void TowerRiteMath_Aftermath_RelationPenaltyIsNegative()
        {
            Assert.Less(TowerRiteMath.AftermathRelationPenalty, 0);
        }


        [Test]
        public void TowerRiteMath_RemnantPartySize_SmallerThanFullHostBand()
        {
            Assert.Less(TowerRiteMath.RemnantPartySize, TowerRiteMath.HostPartySizeEach);
            Assert.Greater(TowerRiteMath.RemnantPartySize, 0);
        }


        // ── ChosenQuestMath tests (Phase 12, Faction H — The Promise) ────────────
        [Test]
        public void ChosenQuestMath_HasReachedThreshold_GatesAtConfiguredFiefCount()
        {
            Assert.IsFalse(ChosenQuestMath.HasReachedThreshold(ChosenQuestMath.ConquestFiefThreshold - 1));
            Assert.IsTrue(ChosenQuestMath.HasReachedThreshold(ChosenQuestMath.ConquestFiefThreshold));
            Assert.IsTrue(ChosenQuestMath.HasReachedThreshold(ChosenQuestMath.ConquestFiefThreshold + 50));
        }


        [Test]
        public void ChosenQuestMath_ConquestThreshold_IsSmallerThanTheSupersededTwoThirdsBenchmark()
        {
            // The superseded prompt's Empire questline aimed at ~2/3 of all towns
            // (~35, verified against the shipped settlements.xml's 53 towns). The
            // Chosen start from just 2 seats, so the threshold must sit clearly
            // below that full-map benchmark while still being a real, multi-fief
            // conquest push (far beyond the Chosen's starting 2 towns).
            const int supersededTwoThirdsBenchmark = 35;
            Assert.Less(ChosenQuestMath.ConquestFiefThreshold, supersededTwoThirdsBenchmark);
            Assert.Greater(ChosenQuestMath.ConquestFiefThreshold, ChosenMath.StartingTownIds.Length * 5);
        }


        [Test]
        public void ChosenQuestMath_ClampedProgress_NeverExceedsThresholdOrGoesNegative()
        {
            Assert.AreEqual(0, ChosenQuestMath.ClampedProgress(-5));
            Assert.AreEqual(0, ChosenQuestMath.ClampedProgress(0));
            Assert.AreEqual(ChosenQuestMath.ConquestFiefThreshold, ChosenQuestMath.ClampedProgress(ChosenQuestMath.ConquestFiefThreshold));
            Assert.AreEqual(ChosenQuestMath.ConquestFiefThreshold, ChosenQuestMath.ClampedProgress(ChosenQuestMath.ConquestFiefThreshold + 999));
        }


        [Test]
        public void ChosenQuestMath_SplinterCount_ScalesWithNonPlayerClanPool()
        {
            Assert.AreEqual(0, ChosenQuestMath.SplinterCount(0));
            Assert.AreEqual(0, ChosenQuestMath.SplinterCount(-3));
            Assert.AreEqual(1, ChosenQuestMath.SplinterCount(1));
            Assert.AreEqual(2, ChosenQuestMath.SplinterCount(2));
            Assert.AreEqual(3, ChosenQuestMath.SplinterCount(3));
            Assert.AreEqual(3, ChosenQuestMath.SplinterCount(10));
        }


        [Test]
        public void ChosenQuestMath_AssignSplinterGroups_EveryClanGetsAGroupWithinRange()
        {
            int[] groups = ChosenQuestMath.AssignSplinterGroups(7, 3);
            Assert.AreEqual(7, groups.Length);
            foreach (int g in groups)
            {
                Assert.GreaterOrEqual(g, 0);
                Assert.Less(g, 3);
            }
        }


        [Test]
        public void ChosenQuestMath_AssignSplinterGroups_GroupZeroIsNeverSmallest()
        {
            // Group 0 always anchors the ruling clan (index 0 of the caller's
            // sorted input) and, per the round-robin remainder bias, is never
            // smaller than any other group — matching the PriestKing "leads the
            // largest splinter" fate.
            for (int clanCount = 1; clanCount <= 12; clanCount++)
            {
                for (int splinterCount = 1; splinterCount <= 3; splinterCount++)
                {
                    int[] groups = ChosenQuestMath.AssignSplinterGroups(clanCount, splinterCount);
                    var counts = new int[splinterCount];
                    foreach (int g in groups) counts[g]++;
                    for (int g = 1; g < splinterCount; g++)
                        Assert.GreaterOrEqual(counts[0], counts[g],
                            $"clanCount={clanCount}, splinterCount={splinterCount}");
                }
            }
        }


        [Test]
        public void ChosenQuestMath_AssignSplinterGroups_DegenerateInputsReturnEmpty()
        {
            Assert.AreEqual(0, ChosenQuestMath.AssignSplinterGroups(0, 3).Length);
            Assert.AreEqual(0, ChosenQuestMath.AssignSplinterGroups(5, 0).Length);
        }


        [Test]
        public void ChosenQuestMath_SplinterIdentities_AreUniqueAndFullyNamed()
        {
            Assert.AreEqual(ChosenQuestMath.SplinterKingdomIds.Length, ChosenQuestMath.SplinterKingdomNames.Length);
            Assert.AreEqual(ChosenQuestMath.SplinterKingdomIds.Distinct().Count(), ChosenQuestMath.SplinterKingdomIds.Length);
            Assert.AreEqual(ChosenQuestMath.SplinterKingdomNames.Distinct().Count(), ChosenQuestMath.SplinterKingdomNames.Length);
            foreach (var id in ChosenQuestMath.SplinterKingdomIds) Assert.IsFalse(string.IsNullOrWhiteSpace(id));
            foreach (var name in ChosenQuestMath.SplinterKingdomNames) Assert.IsFalse(string.IsNullOrWhiteSpace(name));
        }


        // ── ForestWidowsQuestMath tests (Phase 12, Faction C — The Final Peace) ──
        [Test]
        public void ForestWidowsQuestMath_HasReachedThreshold_GatesAtConfiguredCount()
        {
            Assert.IsFalse(ForestWidowsQuestMath.HasReachedThreshold(ForestWidowsQuestMath.SacrificeTarget - 1));
            Assert.IsTrue(ForestWidowsQuestMath.HasReachedThreshold(ForestWidowsQuestMath.SacrificeTarget));
            Assert.IsTrue(ForestWidowsQuestMath.HasReachedThreshold(ForestWidowsQuestMath.SacrificeTarget + 500));
        }


        [Test]
        public void ForestWidowsQuestMath_SacrificeTarget_IsEnormousButSmallerThanGreatAwakening()
        {
            // Smaller than Duneborn's ten thousand (a two-town scarcity economy
            // cannot plausibly bleed at that kingdom's scale), but still an order
            // of magnitude beyond what the old altar's 1-day-per-soldier rate
            // could plausibly accumulate across a single campaign.
            Assert.Less(ForestWidowsQuestMath.SacrificeTarget, GreatAwakeningMath.PrisonerTarget);
            Assert.Greater(ForestWidowsQuestMath.SacrificeTarget, ForestWidowsMath.StartingTownIds.Length * 500);
        }


        [Test]
        public void ForestWidowsQuestMath_ClampedProgress_NeverExceedsThresholdOrGoesNegative()
        {
            Assert.AreEqual(0, ForestWidowsQuestMath.ClampedProgress(-5));
            Assert.AreEqual(0, ForestWidowsQuestMath.ClampedProgress(0));
            Assert.AreEqual(ForestWidowsQuestMath.SacrificeTarget, ForestWidowsQuestMath.ClampedProgress(ForestWidowsQuestMath.SacrificeTarget));
            Assert.AreEqual(ForestWidowsQuestMath.SacrificeTarget, ForestWidowsQuestMath.ClampedProgress(ForestWidowsQuestMath.SacrificeTarget + 999));
        }


        [Test]
        public void ForestWidowsQuestMath_NpcContributionAmount_NeverExceedsHeldOrGoesNegative()
        {
            var rng = new System.Random(12345);
            for (int i = 0; i < 200; i++)
            {
                int held = rng.Next(0, 40);
                int given = ForestWidowsQuestMath.NpcContributionAmount(rng, held);
                Assert.GreaterOrEqual(given, 0);
                Assert.LessOrEqual(given, held);
            }
        }


        [Test]
        public void ForestWidowsQuestMath_NpcContributionAmount_DegenerateInputsReturnZero()
        {
            Assert.AreEqual(0, ForestWidowsQuestMath.NpcContributionAmount(null, 10));
            Assert.AreEqual(0, ForestWidowsQuestMath.NpcContributionAmount(new System.Random(1), 0));
            Assert.AreEqual(0, ForestWidowsQuestMath.NpcContributionAmount(new System.Random(1), -5));
        }


        [Test]
        public void ForestWidowsQuestMath_GarrisonCounts_ArePositive()
        {
            Assert.Greater(ForestWidowsQuestMath.GarrisonFiends, 0);
            Assert.Greater(ForestWidowsQuestMath.GarrisonStalkers, 0);
        }


        // ── BloodboundQuestMath tests (Phase 12, Faction D — The Surpassing Rite) ──
        [Test]
        public void BloodboundQuestMath_HasReachedThreshold_GatesAtConfiguredCount()
        {
            Assert.IsFalse(BloodboundQuestMath.HasReachedThreshold(BloodboundQuestMath.DonationTarget - 1));
            Assert.IsTrue(BloodboundQuestMath.HasReachedThreshold(BloodboundQuestMath.DonationTarget));
            Assert.IsTrue(BloodboundQuestMath.HasReachedThreshold(BloodboundQuestMath.DonationTarget + 500));
        }


        [Test]
        public void BloodboundQuestMath_DonationTarget_IsEnormousButScaledBelowGreatAwakeningAndFinalPeace()
        {
            // Demon Blood is hard-capped at 1-3 vials per personally-won demon-party
            // victory (BloodboundMath.RollDemonBloodYield), unlike the Final Peace's
            // prisoners or Great Awakening's prisoners which a single big battle can
            // hand over dozens of at once — so the raw target must sit well under
            // both, even though the number of FIGHTS it demands is still enormous.
            Assert.Less(BloodboundQuestMath.DonationTarget, GreatAwakeningMath.PrisonerTarget);
            Assert.Less(BloodboundQuestMath.DonationTarget, ForestWidowsQuestMath.SacrificeTarget);

            // Still a real, multi-hundred-fight grind against the yield rate: the
            // expected personal-victory count (target / average yield) must clear
            // TowerRiteMath's own "genuinely hard" bar for a single gather track
            // (3 relics / 0.05 drop chance = 60 fights) by a wide margin, since this
            // IS the entire D questline rather than one ingredient of three.
            double avgYield = (BloodboundMath.MinDemonBloodPerVictory + BloodboundMath.MaxDemonBloodPerVictory) / 2.0;
            double expectedVictories = BloodboundQuestMath.DonationTarget / avgYield;
            Assert.Greater(expectedVictories, 200);
        }


        [Test]
        public void BloodboundQuestMath_ClampedProgress_NeverExceedsThresholdOrGoesNegative()
        {
            Assert.AreEqual(0, BloodboundQuestMath.ClampedProgress(-5));
            Assert.AreEqual(0, BloodboundQuestMath.ClampedProgress(0));
            Assert.AreEqual(BloodboundQuestMath.DonationTarget, BloodboundQuestMath.ClampedProgress(BloodboundQuestMath.DonationTarget));
            Assert.AreEqual(BloodboundQuestMath.DonationTarget, BloodboundQuestMath.ClampedProgress(BloodboundQuestMath.DonationTarget + 999));
        }


        [Test]
        public void BloodboundQuestMath_NpcContributionAmount_NeverExceedsHeldOrGoesNegative()
        {
            var rng = new System.Random(54321);
            for (int i = 0; i < 200; i++)
            {
                int held = rng.Next(0, 20);
                int given = BloodboundQuestMath.NpcContributionAmount(rng, held);
                Assert.GreaterOrEqual(given, 0);
                Assert.LessOrEqual(given, held);
            }
        }


        [Test]
        public void BloodboundQuestMath_NpcContributionAmount_DegenerateInputsReturnZero()
        {
            Assert.AreEqual(0, BloodboundQuestMath.NpcContributionAmount(null, 10));
            Assert.AreEqual(0, BloodboundQuestMath.NpcContributionAmount(new System.Random(1), 0));
            Assert.AreEqual(0, BloodboundQuestMath.NpcContributionAmount(new System.Random(1), -5));
        }


        [Test]
        public void BloodboundQuestMath_IsDemonAttackDue_GatesAtConfiguredDelay()
        {
            Assert.IsFalse(BloodboundQuestMath.IsDemonAttackDue(BloodboundQuestMath.DemonAttackDelayDays - 1));
            Assert.IsTrue(BloodboundQuestMath.IsDemonAttackDue(BloodboundQuestMath.DemonAttackDelayDays));
            Assert.IsTrue(BloodboundQuestMath.IsDemonAttackDue(BloodboundQuestMath.DemonAttackDelayDays + 10));
        }


        [Test]
        public void BloodboundQuestMath_AttackPartiesPerSettlement_IsPositive()
        {
            Assert.Greater(BloodboundQuestMath.AttackPartiesPerSettlement, 0);
        }


        // ── TempleQuestMath tests (Phase 12, Faction E — The Unbroken Vow) ──────

        [Test]
        public void TempleQuestMath_SelectArtifactRuins_PicksExactlyDesiredCountWhenEnoughExist()
        {
            var ruins = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 20; i++) ruins.Add("ruin_" + i);

            var picked = TempleQuestMath.SelectArtifactRuins(ruins, TempleQuestMath.ArtifactCount);

            Assert.AreEqual(TempleQuestMath.ArtifactCount, picked.Count);
            // Every picked id must have come from the input set, and no duplicates.
            Assert.AreEqual(picked.Count, new System.Collections.Generic.HashSet<string>(picked).Count);
            foreach (var id in picked) Assert.Contains(id, ruins);
        }


        [Test]
        public void TempleQuestMath_SelectArtifactRuins_IsDeterministic_SameInputSameOutput()
        {
            var ruins = new System.Collections.Generic.List<string> { "ruin_a", "ruin_b", "ruin_c", "ruin_d", "ruin_e", "ruin_f", "ruin_g" };

            var first  = TempleQuestMath.SelectArtifactRuins(ruins, TempleQuestMath.ArtifactCount);
            var second = TempleQuestMath.SelectArtifactRuins(new System.Collections.Generic.List<string>(ruins), TempleQuestMath.ArtifactCount);

            CollectionAssert.AreEqual(first, second);
        }


        [Test]
        public void TempleQuestMath_SelectArtifactRuins_FallsBackToFewerWhenNotEnoughRuinsExist()
        {
            var ruins = new System.Collections.Generic.List<string> { "ruin_only_one", "ruin_only_two" };

            var picked = TempleQuestMath.SelectArtifactRuins(ruins, TempleQuestMath.ArtifactCount);

            Assert.AreEqual(2, picked.Count);
            Assert.Contains("ruin_only_one", picked);
            Assert.Contains("ruin_only_two", picked);
        }


        [Test]
        public void TempleQuestMath_SelectArtifactRuins_DegenerateInputsReturnEmpty()
        {
            Assert.AreEqual(0, TempleQuestMath.SelectArtifactRuins(null, TempleQuestMath.ArtifactCount).Count);
            Assert.AreEqual(0, TempleQuestMath.SelectArtifactRuins(new System.Collections.Generic.List<string>(), TempleQuestMath.ArtifactCount).Count);
        }


        [Test]
        public void TempleQuestMath_SelectArtifactRuins_IgnoresBlankAndDuplicateIds()
        {
            var ruins = new System.Collections.Generic.List<string> { "ruin_x", "ruin_x", "", null, "ruin_y" };

            var picked = TempleQuestMath.SelectArtifactRuins(ruins, TempleQuestMath.ArtifactCount);

            Assert.AreEqual(2, picked.Count);
            CollectionAssert.AllItemsAreUnique(picked);
        }


        [Test]
        public void TempleQuestMath_HasAllArtifacts_GatesAtConfiguredCount()
        {
            Assert.IsFalse(TempleQuestMath.HasAllArtifacts(TempleQuestMath.ArtifactCount - 1));
            Assert.IsTrue(TempleQuestMath.HasAllArtifacts(TempleQuestMath.ArtifactCount));
            Assert.IsTrue(TempleQuestMath.HasAllArtifacts(TempleQuestMath.ArtifactCount + 1));
        }


        [Test]
        public void TempleQuestMath_HasReachedKillTarget_GatesAtConfiguredCount()
        {
            Assert.IsFalse(TempleQuestMath.HasReachedKillTarget(TempleQuestMath.KillTarget - 1));
            Assert.IsTrue(TempleQuestMath.HasReachedKillTarget(TempleQuestMath.KillTarget));
            Assert.IsTrue(TempleQuestMath.HasReachedKillTarget(TempleQuestMath.KillTarget + 1000));
        }


        [Test]
        public void TempleQuestMath_KillTarget_IsHugeButFinite()
        {
            // "A huge but technically countable number" — genuinely large next
            // to a single demon party (DemonMath.MaxPartyBodies), but not so
            // large it can never in principle be reached over a very long,
            // sustained campaign.
            Assert.Greater(TempleQuestMath.KillTarget, DemonMath.MaxPartyBodies * 100);
            Assert.Less(TempleQuestMath.KillTarget, int.MaxValue / 2);
        }


        [Test]
        public void TempleQuestMath_ClampedKillProgress_NeverExceedsTargetOrGoesNegative()
        {
            Assert.AreEqual(0, TempleQuestMath.ClampedKillProgress(-5));
            Assert.AreEqual(0, TempleQuestMath.ClampedKillProgress(0));
            Assert.AreEqual(TempleQuestMath.KillTarget, TempleQuestMath.ClampedKillProgress(TempleQuestMath.KillTarget));
            Assert.AreEqual(TempleQuestMath.KillTarget, TempleQuestMath.ClampedKillProgress(TempleQuestMath.KillTarget + 999));
        }


        [Test]
        public void TempleQuestMath_ArmyCohesionTopUp_IsAboveTheFloor()
        {
            Assert.Greater(TempleQuestMath.ArmyCohesionTopUp, TempleQuestMath.ArmyCohesionFloor);
        }


        // ── EmpireQuestMath tests (Phase 12, Faction F — The Reunification) ──────

        [Test]
        public void EmpireQuestMath_HasReachedThreshold_GatesAtConfiguredTownCount()
        {
            Assert.IsFalse(EmpireQuestMath.HasReachedThreshold(EmpireQuestMath.ConquestTownThreshold - 1));
            Assert.IsTrue(EmpireQuestMath.HasReachedThreshold(EmpireQuestMath.ConquestTownThreshold));
            Assert.IsTrue(EmpireQuestMath.HasReachedThreshold(EmpireQuestMath.ConquestTownThreshold + 10));
        }


        [Test]
        public void EmpireQuestMath_ConquestThreshold_IsTwoThirdsOfTheMapsFiftyThreeTowns()
        {
            // Verified against the shipped SandBox/ModuleData/settlements.xml (53
            // towns map-wide, unchanged by Phase 8/9 — see EmpireQuestMath.cs's
            // header). Two-thirds of 53 floors to 35.
            const int totalTownsMapWide = 53;
            int expected = (totalTownsMapWide * 2) / 3;
            Assert.AreEqual(expected, EmpireQuestMath.ConquestTownThreshold);
        }


        [Test]
        public void EmpireQuestMath_ConquestThreshold_IsMuchHigherThanTheChosensOwnFiefThreshold()
        {
            // The brief explicitly demands a MUCH higher bar than the Chosen's
            // own ConquestFiefThreshold (24, roughly a fifth of the 120-fief map)
            // — two-thirds of ALL cities, not a modest chunk. (The threshold must
            // also clear the Empire's OWN starting town count by a wide margin —
            // not asserted here as a raw multiple of StartingTownIds.Length, since
            // that array now also holds the 2026-07-19 remnant-kingdom border
            // castles, not towns alone; see EmpireQuestMath.cs's header for the
            // real "starts with 5 of the 53 towns" derivation.)
            Assert.Greater(EmpireQuestMath.ConquestTownThreshold, ChosenQuestMath.ConquestFiefThreshold);
        }


        [Test]
        public void EmpireQuestMath_ClampedTownProgress_NeverExceedsThresholdOrGoesNegative()
        {
            Assert.AreEqual(0, EmpireQuestMath.ClampedTownProgress(-5));
            Assert.AreEqual(0, EmpireQuestMath.ClampedTownProgress(0));
            Assert.AreEqual(EmpireQuestMath.ConquestTownThreshold, EmpireQuestMath.ClampedTownProgress(EmpireQuestMath.ConquestTownThreshold));
            Assert.AreEqual(EmpireQuestMath.ConquestTownThreshold, EmpireQuestMath.ClampedTownProgress(EmpireQuestMath.ConquestTownThreshold + 999));
        }


        [Test]
        public void EmpireQuestMath_HasReachedKillTarget_GatesAtConfiguredCount()
        {
            Assert.IsFalse(EmpireQuestMath.HasReachedKillTarget(EmpireQuestMath.KillTarget - 1));
            Assert.IsTrue(EmpireQuestMath.HasReachedKillTarget(EmpireQuestMath.KillTarget));
            Assert.IsTrue(EmpireQuestMath.HasReachedKillTarget(EmpireQuestMath.KillTarget + 1000));
        }


        [Test]
        public void EmpireQuestMath_KillTarget_IsLargeButDeliberatelySmallerThanTheTemples()
        {
            // "A very large number... smaller than the Temple's 50,000" per the
            // brief — this ending is meant to be a winnable, triumphant arc, not
            // the Temple's near-impossible war of attrition.
            Assert.Greater(EmpireQuestMath.KillTarget, DemonMath.MaxPartyBodies * 100);
            Assert.Less(EmpireQuestMath.KillTarget, TempleQuestMath.KillTarget);
        }


        [Test]
        public void EmpireQuestMath_ClampedKillProgress_NeverExceedsTargetOrGoesNegative()
        {
            Assert.AreEqual(0, EmpireQuestMath.ClampedKillProgress(-5));
            Assert.AreEqual(0, EmpireQuestMath.ClampedKillProgress(0));
            Assert.AreEqual(EmpireQuestMath.KillTarget, EmpireQuestMath.ClampedKillProgress(EmpireQuestMath.KillTarget));
            Assert.AreEqual(EmpireQuestMath.KillTarget, EmpireQuestMath.ClampedKillProgress(EmpireQuestMath.KillTarget + 999));
        }


        [Test]
        public void EmpireQuestMath_ShouldNudgeToEngageDemons_RollsAgainstConfiguredChance()
        {
            Assert.IsTrue(EmpireQuestMath.ShouldNudgeToEngageDemons(0.0));
            Assert.IsFalse(EmpireQuestMath.ShouldNudgeToEngageDemons(EmpireQuestMath.AggressionNudgeChance));
            Assert.IsFalse(EmpireQuestMath.ShouldNudgeToEngageDemons(0.999));
        }


        [Test]
        public void EmpireQuestMath_AggressionNudgeChance_IsHigherThanLegionsBaselineRaidNudge()
        {
            // The brief calls for INCREASED aggression, not the same baseline
            // eagerness every other kingdom's lords already show toward rivals.
            Assert.Greater(EmpireQuestMath.AggressionNudgeChance, LegionMath.RaidNudgeChance);
        }


        [Test]
        public void EmpireQuestMath_VictoryRenownBonus_IsPositiveAndSmallerThanTheCrowning()
        {
            Assert.Greater(EmpireQuestMath.VictoryRenownBonus, 0f);
            Assert.Greater(EmpireQuestMath.CrownRenownBonus, EmpireQuestMath.VictoryRenownBonus);
        }


        [Test]
        public void TempleQuestArtifacts_HasExactlyArtifactCountEntries_WithUniqueIndicesAndItemIds()
        {
            var all = TempleQuestArtifacts.All;
            Assert.AreEqual(TempleQuestMath.ArtifactCount, all.Count);

            var indices = new System.Collections.Generic.HashSet<int>();
            var itemIds = new System.Collections.Generic.HashSet<string>();
            foreach (var def in all)
            {
                Assert.IsTrue(indices.Add(def.Index), "Duplicate artifact index: " + def.Index);
                Assert.IsTrue(itemIds.Add(def.ItemId), "Duplicate artifact item id: " + def.ItemId);
                Assert.IsFalse(string.IsNullOrEmpty(def.Name));
                Assert.IsFalse(string.IsNullOrEmpty(def.Lore));
            }
        }


        [Test]
        public void TempleQuestArtifacts_TryGet_FindsEveryDefinedIndex_AndFailsOutOfRange()
        {
            for (int i = 0; i < TempleQuestMath.ArtifactCount; i++)
                Assert.IsTrue(TempleQuestArtifacts.TryGet(i, out _), "Missing artifact index " + i);

            Assert.IsFalse(TempleQuestArtifacts.TryGet(-1, out _));
            Assert.IsFalse(TempleQuestArtifacts.TryGet(TempleQuestMath.ArtifactCount, out _));
        }


        // ── LegionQuestMath (Phase 12, Faction G — Legion's "The Far Shore") ────
        [Test]
        public void LegionQuestMath_IsStockComplete_RequiresBothTracksIndividuallyFull()
        {
            Assert.IsFalse(LegionQuestMath.IsStockComplete(LegionQuestMath.HardwoodTarget, 0));
            Assert.IsFalse(LegionQuestMath.IsStockComplete(0, LegionQuestMath.IronTarget));
            Assert.IsTrue(LegionQuestMath.IsStockComplete(LegionQuestMath.HardwoodTarget, LegionQuestMath.IronTarget));
        }


        [Test]
        public void LegionQuestMath_ClampedRatio_StaysWithinZeroToOne()
        {
            Assert.AreEqual(0f, LegionQuestMath.ClampedRatio(-5, 100));
            Assert.AreEqual(1f, LegionQuestMath.ClampedRatio(500, 100));
            Assert.AreEqual(0.5f, LegionQuestMath.ClampedRatio(50, 100), 0.0001f);
        }


        [Test]
        public void LegionQuestMath_BlendedProgress_IsMeanOfBothRatios()
        {
            float expected = (LegionQuestMath.ClampedRatio(1250, LegionQuestMath.HardwoodTarget)
                + LegionQuestMath.ClampedRatio(2500, LegionQuestMath.IronTarget)) / 2f;
            Assert.AreEqual(expected, LegionQuestMath.BlendedProgress(1250, 2500), 0.0001f);
        }


        [Test]
        public void LegionQuestMath_ApplyWeeklyDecay_Reduces10PercentAndFloorsAtZero()
        {
            Assert.AreEqual(900, LegionQuestMath.ApplyWeeklyDecay(1000));
            Assert.AreEqual(0, LegionQuestMath.ApplyWeeklyDecay(0));
            Assert.AreEqual(0, LegionQuestMath.ApplyWeeklyDecay(-10));
        }


        [Test]
        public void LegionQuestMath_DepartingClanCount_IsTwoOrThree()
        {
            Assert.AreEqual(LegionQuestMath.MinDepartingClans, LegionQuestMath.DepartingClanCount(0.0));
            Assert.AreEqual(LegionQuestMath.MaxDepartingClans, LegionQuestMath.DepartingClanCount(0.99));
        }


        [Test]
        public void LegionQuestMath_NpcContributionAmount_StaysWithinRange()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 50; i++)
            {
                int amount = LegionQuestMath.NpcContributionAmount(rng, 15, 60);
                Assert.GreaterOrEqual(amount, 15);
                Assert.LessOrEqual(amount, 60);
            }
        }
    }
}
