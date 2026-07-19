using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── DemonMath tests (The Darkest Night — Phase 1: the demons) ─────────

        [Test]
        public void DemonMath_IsNightHour_DuskAndDawnBoundaries()
        {
            Assert.IsTrue(DemonMath.IsNightHour(20f));   // dusk itself is night
            Assert.IsFalse(DemonMath.IsNightHour(19.99f));
            Assert.IsTrue(DemonMath.IsNightHour(23.99f));
            Assert.IsTrue(DemonMath.IsNightHour(0f));
            Assert.IsTrue(DemonMath.IsNightHour(5.99f));
            Assert.IsFalse(DemonMath.IsNightHour(6f));   // dawn itself is day
            Assert.IsFalse(DemonMath.IsNightHour(12f));
        }


        [Test]
        public void DemonMath_VariantForCulture_MapsKnownCultures()
        {
            Assert.AreEqual(DemonMath.EnvironmentVariant.Snow,   DemonMath.VariantForCulture("sturgia"));
            Assert.AreEqual(DemonMath.EnvironmentVariant.Desert, DemonMath.VariantForCulture("aserai"));
            Assert.AreEqual(DemonMath.EnvironmentVariant.Forest, DemonMath.VariantForCulture("battania"));
            Assert.AreEqual(DemonMath.EnvironmentVariant.Default, DemonMath.VariantForCulture("vlandia"));
            Assert.AreEqual(DemonMath.EnvironmentVariant.Default, DemonMath.VariantForCulture(null));
        }


        [Test]
        public void DemonMath_HealthMultiplier_SnowHardierDesertFrailer()
        {
            Assert.Greater(DemonMath.HealthMultiplier(DemonMath.EnvironmentVariant.Snow), 1f);
            Assert.Less(DemonMath.HealthMultiplier(DemonMath.EnvironmentVariant.Desert), 1f);
            Assert.AreEqual(1f, DemonMath.HealthMultiplier(DemonMath.EnvironmentVariant.Default));
        }


        [Test]
        public void DemonMath_SpeedMultiplier_DesertFasterSnowSlower()
        {
            Assert.Greater(DemonMath.SpeedMultiplier(DemonMath.EnvironmentVariant.Desert), 1f);
            Assert.Less(DemonMath.SpeedMultiplier(DemonMath.EnvironmentVariant.Snow), 1f);
        }


        [Test]
        public void DemonMath_DetectionRangeMultiplier_ForestIsStealthier()
        {
            Assert.Less(DemonMath.DetectionRangeMultiplier(DemonMath.EnvironmentVariant.Forest), 1f);
            Assert.AreEqual(1f, DemonMath.DetectionRangeMultiplier(DemonMath.EnvironmentVariant.Default));
            Assert.AreEqual(1f, DemonMath.DetectionRangeMultiplier(DemonMath.EnvironmentVariant.Snow));
        }


        [Test]
        public void DemonMath_Health_CombinesBaseAndVariant()
        {
            float expected = DemonMath.BaseHealth(DemonMath.DemonTier.Ravager)
                            * DemonMath.HealthMultiplier(DemonMath.EnvironmentVariant.Snow);
            Assert.AreEqual(expected, DemonMath.Health(DemonMath.DemonTier.Ravager, DemonMath.EnvironmentVariant.Snow), 0.001f);
        }


        [Test]
        public void DemonMath_MeleeDamageMultiplier_EscalatesFiendToRavager()
        {
            Assert.Less(DemonMath.MeleeDamageMultiplier(DemonMath.DemonTier.Fiend),
                        DemonMath.MeleeDamageMultiplier(DemonMath.DemonTier.Stalker));
            Assert.Less(DemonMath.MeleeDamageMultiplier(DemonMath.DemonTier.Stalker),
                        DemonMath.MeleeDamageMultiplier(DemonMath.DemonTier.Ravager));
        }


        [Test]
        public void DemonMath_CastsMagic_RavagerAndLordOnly()
        {
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Fiend));
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Stalker));
            Assert.IsTrue(DemonMath.CastsMagic(DemonMath.DemonTier.Ravager));
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Hellsteed));
            Assert.IsTrue(DemonMath.CastsMagic(DemonMath.DemonTier.Lord));
        }


        [Test]
        public void DemonMath_LordCastPatternIndex_AlternatesFireThenSpirit()
        {
            Assert.AreEqual(0, DemonMath.LordCastPatternIndex(0));
            Assert.AreEqual(1, DemonMath.LordCastPatternIndex(1));
            Assert.AreEqual(0, DemonMath.LordCastPatternIndex(2));
            Assert.AreEqual(1, DemonMath.LordCastPatternIndex(3));
            Assert.AreEqual(0, DemonMath.LordCastPatternIndex(-1)); // never throws on a bad count
        }


        [Test]
        public void DemonMath_VisualScale_RavagerBiggestLordOnlyUncannilyTall()
        {
            Assert.AreEqual(1.00f, DemonMath.VisualScale(DemonMath.DemonTier.Fiend), 0.001f);
            Assert.Greater(DemonMath.VisualScale(DemonMath.DemonTier.Stalker), 1.00f);
            Assert.Greater(DemonMath.VisualScale(DemonMath.DemonTier.Ravager), DemonMath.VisualScale(DemonMath.DemonTier.Stalker));
            // The Lord is uncanny, not bestial: taller than a man, but well
            // short of the Ravager's monstrous frame.
            Assert.Greater(DemonMath.VisualScale(DemonMath.DemonTier.Lord), 1.00f);
            Assert.Less(DemonMath.VisualScale(DemonMath.DemonTier.Lord), DemonMath.VisualScale(DemonMath.DemonTier.Ravager));
        }


        [Test]
        public void DemonMath_MonsterIdFor_OnlyRavagerGetsTheHulkingCapsule()
        {
            Assert.IsNull(DemonMath.MonsterIdFor(DemonMath.DemonTier.Fiend));
            Assert.IsNull(DemonMath.MonsterIdFor(DemonMath.DemonTier.Stalker));
            Assert.AreEqual(DemonMath.HulkingMonsterId, DemonMath.MonsterIdFor(DemonMath.DemonTier.Ravager));
            Assert.IsNull(DemonMath.MonsterIdFor(DemonMath.DemonTier.Hellsteed));
            // The Lord stays on the human capsule — fighting him should feel
            // like fighting a man, right up until it doesn't.
            Assert.IsNull(DemonMath.MonsterIdFor(DemonMath.DemonTier.Lord));
        }


        [Test]
        public void DemonMath_SpeedMultiplier_FiendAndStalkerFasterRavagerSlower()
        {
            Assert.Greater(DemonMath.SpeedMultiplier(DemonMath.DemonTier.Fiend), 1.00f);
            Assert.Greater(DemonMath.SpeedMultiplier(DemonMath.DemonTier.Stalker), DemonMath.SpeedMultiplier(DemonMath.DemonTier.Fiend));
            Assert.Less(DemonMath.SpeedMultiplier(DemonMath.DemonTier.Ravager), 1.00f);
        }


        [Test]
        public void DemonMath_BoneWarps_EveryTierIsWarpedWithinEngineProvenRange()
        {
            // Native's own skeleton_scales.xml ships per-bone values 0.8..2.1;
            // ours stay well inside that proven envelope so nothing shreds.
            foreach (DemonMath.DemonTier tier in Enum.GetValues(typeof(DemonMath.DemonTier)))
            {
                DemonMath.BoneWarp[] warps = DemonMath.BoneWarps(tier);
                Assert.Greater(warps.Length, 0, $"{tier} should carry a warp");
                foreach (DemonMath.BoneWarp w in warps)
                {
                    Assert.GreaterOrEqual(w.X, 0.8f); Assert.LessOrEqual(w.X, 1.6f);
                    Assert.GreaterOrEqual(w.Y, 0.8f); Assert.LessOrEqual(w.Y, 1.6f);
                    Assert.GreaterOrEqual(w.Z, 0.8f); Assert.LessOrEqual(w.Z, 1.6f);
                }
            }
        }


        [Test]
        public void DemonMath_BoneWarps_FiendIsLopsided()
        {
            // The Starved's arms must never match — asymmetry is the read.
            DemonMath.BoneWarp[] warps = DemonMath.BoneWarps(DemonMath.DemonTier.Fiend);
            float left = 0f, right = 0f;
            foreach (DemonMath.BoneWarp w in warps)
            {
                if (w.Part == DemonMath.BonePart.LeftArm)  left  = w.Y;
                if (w.Part == DemonMath.BonePart.RightArm) right = w.Y;
            }
            Assert.Greater(left, 0f);
            Assert.Greater(right, 0f);
            Assert.AreNotEqual(left, right);
        }


        [Test]
        public void DemonMath_FacialAnimation_AllTiersSnarl_LordAloneInFury()
        {
            foreach (DemonMath.DemonTier tier in Enum.GetValues(typeof(DemonMath.DemonTier)))
                Assert.IsFalse(string.IsNullOrEmpty(DemonMath.FacialAnimation(tier)));
            Assert.AreNotEqual(DemonMath.FacialAnimation(DemonMath.DemonTier.Fiend),
                               DemonMath.FacialAnimation(DemonMath.DemonTier.Lord));
        }


        [Test]
        public void DemonMath_NightSpawnPartyCount_StaysWithinConfiguredRange()
        {
            var rng = new Random(42);
            for (int i = 0; i < 200; i++)
            {
                int count = DemonMath.NightSpawnPartyCount(rng, 0);
                Assert.GreaterOrEqual(count, DemonMath.QuietMinParties);
                Assert.LessOrEqual(count, DemonMath.SurgeMaxParties);
            }
        }


        [Test]
        public void DemonMath_RollNightIntensity_AllThreeBandsAppearOverManyRolls()
        {
            var rng = new Random(11);
            bool sawQuiet = false, sawRestless = false, sawSurge = false;
            for (int i = 0; i < 500; i++)
            {
                switch (DemonMath.RollNightIntensity(rng))
                {
                    case DemonMath.NightIntensity.Quiet:    sawQuiet    = true; break;
                    case DemonMath.NightIntensity.Restless: sawRestless = true; break;
                    case DemonMath.NightIntensity.Surge:    sawSurge    = true; break;
                }
            }
            Assert.IsTrue(sawQuiet && sawRestless && sawSurge);
        }


        [Test]
        public void DemonMath_NightSpawnPartyCount_CappedByRoomLeft()
        {
            var rng = new Random(7);
            // No room left at all — never spawn more than the cap allows.
            Assert.AreEqual(0, DemonMath.NightSpawnPartyCount(rng, DemonMath.MaxLivingDemonParties));
            // Only room for 2 more, even though the normal roll could ask for more.
            int count = DemonMath.NightSpawnPartyCount(rng, DemonMath.MaxLivingDemonParties - 2);
            Assert.LessOrEqual(count, 2);
        }


        [Test]
        public void DemonMath_PartyBodyCount_StaysWithinConfiguredRange()
        {
            var rng = new Random(11);
            for (int i = 0; i < 200; i++)
            {
                int count = DemonMath.PartyBodyCount(rng);
                Assert.GreaterOrEqual(count, DemonMath.MinPartyBodies);
                Assert.LessOrEqual(count, DemonMath.MaxPartyBodies);
            }
        }


        [Test]
        public void DemonMath_RollTier_MostlyWeakerTiersOverManyRolls()
        {
            var rng = new Random(99);
            int[] counts = new int[4];
            const int trials = 5000;
            for (int i = 0; i < trials; i++)
                counts[(int)DemonMath.RollTier(rng)]++;

            // Fiend should dominate the mix (~55%), Hellsteed should be rarest (~7%).
            Assert.Greater(counts[(int)DemonMath.DemonTier.Fiend], counts[(int)DemonMath.DemonTier.Stalker]);
            Assert.Greater(counts[(int)DemonMath.DemonTier.Stalker], counts[(int)DemonMath.DemonTier.Ravager]);
            Assert.Greater(counts[(int)DemonMath.DemonTier.Ravager], counts[(int)DemonMath.DemonTier.Hellsteed]);
            Assert.Greater(counts[(int)DemonMath.DemonTier.Fiend], trials / 3); // clearly the plurality
        }


        [Test]
        public void DemonMath_RollSpawnLocation_AllThreeKindsOccur()
        {
            var rng = new Random(3);
            var seen = new HashSet<DemonMath.SpawnLocationKind>();
            for (int i = 0; i < 500; i++)
                seen.Add(DemonMath.RollSpawnLocation(rng));
            Assert.AreEqual(3, seen.Count);
        }


        [Test]
        public void DemonMath_ReplenishAmount_ZeroWhenAtOrAboveOriginal()
        {
            Assert.AreEqual(0, DemonMath.ReplenishAmount(20, 20));
            Assert.AreEqual(0, DemonMath.ReplenishAmount(25, 20));
        }


        [Test]
        public void DemonMath_ReplenishAmount_PositiveButNeverExceedsGap()
        {
            int amount = DemonMath.ReplenishAmount(10, 20);
            Assert.Greater(amount, 0);
            Assert.LessOrEqual(amount, 10);
        }


        [Test]
        public void DemonMath_ReplenishAmount_AtLeastOneWhenAnyGapExists()
        {
            // A tiny gap must still round up to at least one body — a party that
            // lost one soldier should not wait forever to get it back.
            Assert.AreEqual(1, DemonMath.ReplenishAmount(19, 20));
        }


        [Test]
        public void DemonMath_RollSettlementAssault_ChanceIsSmallButNonZero()
        {
            Assert.Greater(DemonMath.SettlementAssaultChancePerPartyPerNight, 0f);
            Assert.Less(DemonMath.SettlementAssaultChancePerPartyPerNight, 0.10f); // "rare"
            Assert.IsTrue(DemonMath.RollSettlementAssault(0.0));
            Assert.IsFalse(DemonMath.RollSettlementAssault(0.99));
        }


        [Test]
        public void DemonMath_RollSettlementAssault_FiresSometimesOverManyNights()
        {
            var rng = new Random(5);
            int hits = 0;
            const int nights = 20000;
            for (int i = 0; i < nights; i++)
                if (DemonMath.RollSettlementAssault(rng.NextDouble())) hits++;
            Assert.Greater(hits, 0);
            // Roughly matches the configured chance within a generous band.
            double observed = (double)hits / nights;
            Assert.Less(observed, DemonMath.SettlementAssaultChancePerPartyPerNight * 3.0);
        }


        [Test]
        public void DemonMath_RollCaptiveEscapes_HeroBetterOddsThanTroop()
        {
            Assert.Greater(DemonMath.HeroEscapeChance, DemonMath.TroopEscapeChance);
            Assert.IsTrue(DemonMath.RollCaptiveEscapes(0.0, true));
            Assert.IsFalse(DemonMath.RollCaptiveEscapes(0.99, true));
            Assert.IsFalse(DemonMath.RollCaptiveEscapes(0.99, false));
        }


        [Test]
        public void DemonMath_RollCaptiveEscapes_FailsFarMoreOftenThanNot()
        {
            // "No lord or unit survives demon captivity otherwise" — both escape
            // chances must stay well under 50%.
            Assert.Less(DemonMath.HeroEscapeChance, 0.5f);
            Assert.Less(DemonMath.TroopEscapeChance, 0.5f);
        }


        [Test]
        public void DemonMath_CountEscapees_NeverExceedsTotal()
        {
            var rng = new Random(21);
            int escaped = DemonMath.CountEscapees(50, rng, false);
            Assert.GreaterOrEqual(escaped, 0);
            Assert.LessOrEqual(escaped, 50);
        }


        [Test]
        public void DemonMath_CountEscapees_ZeroForZeroOrNullInputs()
        {
            var rng = new Random(1);
            Assert.AreEqual(0, DemonMath.CountEscapees(0, rng, true));
            Assert.AreEqual(0, DemonMath.CountEscapees(10, null, true));
        }


        [Test]
        public void DemonMath_CountEscapees_RoughlyMatchesConfiguredChance()
        {
            var rng = new Random(123);
            const int total = 20000;
            int escaped = DemonMath.CountEscapees(total, rng, false); // troops
            double observed = (double)escaped / total;
            Assert.Less(Math.Abs(observed - DemonMath.TroopEscapeChance), 0.03);
        }


        // ── DemonCatalog tests ──────────────────────────────────────────────────

        [Test]
        public void DemonCatalog_TroopIdFor_RoundTripsThroughTryGetTier()
        {
            foreach (DemonMath.DemonTier tier in Enum.GetValues(typeof(DemonMath.DemonTier)))
            {
                string id = DemonCatalog.TroopIdFor(tier);
                Assert.IsTrue(DemonCatalog.TryGetTier(id, out DemonMath.DemonTier roundTripped));
                Assert.AreEqual(tier, roundTripped);
            }
        }


        [Test]
        public void DemonCatalog_IsDemonTroopId_FalseForUnknownId()
        {
            Assert.IsFalse(DemonCatalog.IsDemonTroopId("mountain_bandit"));
            Assert.IsFalse(DemonCatalog.IsDemonTroopId(null));
            Assert.IsFalse(DemonCatalog.IsDemonTroopId("elemental_being"));
        }


        [Test]
        public void DemonCatalog_AllTroopIds_ContainsAllFourTiers()
        {
            Assert.AreEqual(4, DemonCatalog.AllTroopIds.Length);
            Assert.Contains(DemonCatalog.FiendTroopId,     DemonCatalog.AllTroopIds);
            Assert.Contains(DemonCatalog.StalkerTroopId,   DemonCatalog.AllTroopIds);
            Assert.Contains(DemonCatalog.RavagerTroopId,   DemonCatalog.AllTroopIds);
            Assert.Contains(DemonCatalog.HellsteedTroopId, DemonCatalog.AllTroopIds);
        }
    }
}
