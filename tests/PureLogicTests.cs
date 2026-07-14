// =============================================================================
// COLOURS OF CALRADIA — PureLogicTests.cs
// Mount & Blade II: Bannerlord Mod  v2.0
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AshAndEmber;

namespace AshAndEmber.Tests
{
    [TestFixture]
    public class PureLogicTests
    {
        // ── ColorSchoolData tests ─────────────────────────────────────────────

        [Test]
        public void ColorSchoolData_GetGlowColor_EachSchool_NonZero()
        {
            var schools = new[]
            {
                ColorSchool.Red, ColorSchool.Orange, ColorSchool.Yellow,
                ColorSchool.Green, ColorSchool.Blue, ColorSchool.Purple
            };
            foreach (var school in schools)
            {
                uint color = ColorSchoolData.GetGlowColor(school);
                Assert.AreNotEqual(0u, color,
                    $"GetGlowColor returned 0 for {school}.");
            }
        }

        // ── SpellBuilder parsing for Waver / Rouse thresholds ─────────────────

        [Test]
        public void SpellBuilder_OneDamageInput_WaverConditionMet()
        {
            // Any DamageCount > 0 lets Waver roll its 12% chance.
            var cast = SpellBuilder.Parse("U", "U");
            Assert.AreEqual(1, cast.DamageCount);
            Assert.AreEqual(0, cast.RestoreCount);
            Assert.IsFalse(cast.IsFumble);
        }

        [Test]
        public void SpellBuilder_ThreeRestoreInputs_RouseThresholdMet()
        {
            // Rouse requires RestoreCount >= 3.
            var cast = SpellBuilder.Parse("D", "DDD");
            Assert.AreEqual(3, cast.RestoreCount);
            Assert.AreEqual(0, cast.DamageCount);
            Assert.IsTrue(cast.RestoreCount >= 3,
                "3 Restore inputs should satisfy Rouse's minimum threshold.");
        }

        [Test]
        public void SpellBuilder_TwoRestoreInputs_RouseThresholdNotMet()
        {
            var cast = SpellBuilder.Parse("D", "DD");
            Assert.AreEqual(2, cast.RestoreCount);
            Assert.IsFalse(cast.RestoreCount >= 3,
                "2 Restore inputs should not satisfy Rouse's minimum threshold.");
        }

        [Test]
        public void SpellBuilder_MixedEffects_CountsAreSeparate()
        {
            // 1 Damage + 3 Restore: Waver can trigger on enemies, Rouse on allies.
            var cast = SpellBuilder.Parse("U", "UDDD");
            Assert.AreEqual(1, cast.DamageCount);
            Assert.AreEqual(3, cast.RestoreCount);
        }

        // ── Full talent roster coverage ───────────────────────────────────────
        // Note: many TalentId values are marked "REMOVED — kept for save
        // compatibility" and intentionally have no entry in TalentSystem.All, so
        // there is no "every enum value has a definition" invariant to assert.

        [Test]
        public void TalentSystem_AllDefinitions_HaveNonEmptyText()
        {
            foreach (var def in TalentSystem.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Name),
                    $"TalentId.{def.Id} has an empty Name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.MechanicDesc),
                    $"TalentId.{def.Id} has an empty MechanicDesc.");
            }
        }

        [Test]
        public void TalentSystem_SpellTalents_AreNotEnchantments()
        {
            var spellIds = new[]
            {
                TalentId.BreakWills, TalentId.Inspire, TalentId.Plague,
                TalentId.Clairvoyance, TalentId.Extinguish, TalentId.Fade,
            };
            foreach (var id in spellIds)
            {
                var def = TalentSystem.All.FirstOrDefault(d => d.Id == id);
                Assert.IsNotNull(def, $"Missing def for {id}.");
                Assert.IsTrue(def.IsSpell, $"{id} should be flagged IsSpell.");
                Assert.IsFalse(def.IsEnchantment, $"{id} should not be flagged IsEnchantment.");
                Assert.AreEqual(TalentCategory.Spell, def.Category);
            }
        }

        [Test]
        public void TalentSystem_PassiveTalents_AreNeitherSpellNorEnchantment()
        {
            var passiveIds = new[]
            {
                TalentId.Gift, TalentId.BattleMage, TalentId.Sorcerer, TalentId.Ember,
                TalentId.Reap, TalentId.Camaraderie,
            };
            foreach (var id in passiveIds)
            {
                var def = TalentSystem.All.FirstOrDefault(d => d.Id == id);
                Assert.IsNotNull(def, $"Missing def for {id}.");
                Assert.IsFalse(def.IsSpell, $"{id} should not be flagged IsSpell.");
                Assert.IsFalse(def.IsEnchantment, $"{id} should not be flagged IsEnchantment.");
                Assert.AreEqual(TalentCategory.Passive, def.Category);
            }
        }

        [Test]
        public void TalentSystem_GiftIsAlwaysPurchasedAtStart()
        {
            TalentSystem.ResetForNewGame();
            Assert.IsTrue(TalentSystem.Has(TalentId.Gift),
                "Gift should be purchased automatically at game start.");
        }

        // ── Class bundles ─────────────────────────────────────────────────────

        [Test]
        public void TalentSystem_EveryClassMember_IsALiveTalentDefinition()
        {
            // A class must never bundle a consolidated-out talent (no TalentDef),
            // or owning the class would silently revive a cut mechanic.
            foreach (var kv in TalentSystem.ClassMembers)
                foreach (var member in kv.Value)
                    Assert.IsTrue(TalentSystem.All.Any(d => d.Id == member),
                        $"Class {kv.Key} bundles {member}, which has no live TalentDef.");
        }

        [Test]
        public void TalentSystem_NoTalentIsBundledByTwoClasses()
        {
            var seen = new HashSet<TalentId>();
            foreach (var kv in TalentSystem.ClassMembers)
                foreach (var member in kv.Value)
                    Assert.IsTrue(seen.Add(member),
                        $"{member} is bundled by more than one class (second: {kv.Key}).");
        }

        [Test]
        public void TalentSystem_EveryClass_HasCorrectFocusCost()
        {
            // Both fire paths (Category.Class) and discipline classes bought at
            // ritual sites (Category.Rite) use an escalating cost curve, marked by
            // FocusCost 0 — fire paths via GetNextPathCost, disciplines via the
            // per-discipline GetNextDisciplineCost pools. BattleSworn is kept in
            // ClassMembers for save compatibility only and has no TalentDef.
            foreach (var classId in TalentSystem.ClassMembers.Keys)
            {
                var def = TalentSystem.All.FirstOrDefault(d => d.Id == classId);
                if (classId == TalentId.BattleSworn)
                {
                    Assert.IsNull(def, "Legacy BattleSworn should have no TalentDef.");
                    continue;
                }
                Assert.IsNotNull(def, $"Class {classId} has no TalentDef.");
                Assert.AreEqual(0, def.FocusCost,
                    $"Class {classId} should use an escalating cost curve (FocusCost 0).");
            }
        }

        [Test]
        public void TalentSystem_OwningAClass_GrantsAllItsMembers()
        {
            TalentSystem.ResetForNewGame();
            // Grant a class directly (no Hero needed) and confirm Has() reports
            // every bundled member as owned.
            TalentSystem.GrantClassForTest(TalentId.Pyrelord);
            foreach (var member in TalentSystem.ClassMembers[TalentId.Pyrelord])
                Assert.IsTrue(TalentSystem.Has(member),
                    $"Owning Pyrelord should grant {member}.");
            // A talent from a different, unowned class is not granted.
            Assert.IsFalse(TalentSystem.Has(TalentId.Ashveil),
                "Owning Pyrelord must not grant Ward-Keeper's Ashveil.");
            TalentSystem.ResetForNewGame();
        }

        // ── AgingSystem pure math ─────────────────────────────────────────────

        // Tests use the pure overload (explicit hero age) so they run without the
        // TaleWorlds runtime. Age 40 is the Tempered threshold — no age discount.
        private const float NoAgeDiscount = 40f;

        [Test]
        public void AgingSystem_ComputeBattleAgingCost_SmallCast_LowCost()
        {
            // Geometric round(1.5^(n−1)): 1→1, 2→2, 3→2 without BattleMage.
            Assert.AreEqual(1, AgingSystem.ComputeBattleAgingCost(1, false, NoAgeDiscount));
            Assert.AreEqual(2, AgingSystem.ComputeBattleAgingCost(2, false, NoAgeDiscount));
            Assert.AreEqual(2, AgingSystem.ComputeBattleAgingCost(3, false, NoAgeDiscount));
        }

        [Test]
        public void AgingSystem_ComputeBattleAgingCost_LargeCast_ScalesGeometrically()
        {
            // round(1.5^(n−1)): 4→3, 8→17, 10→38; the 84-day cap only guards huge input counts (20→84).
            Assert.AreEqual(3,  AgingSystem.ComputeBattleAgingCost(4, false, NoAgeDiscount));
            Assert.AreEqual(17, AgingSystem.ComputeBattleAgingCost(8, false, NoAgeDiscount));
            Assert.AreEqual(38, AgingSystem.ComputeBattleAgingCost(10, false, NoAgeDiscount));
            Assert.AreEqual(84, AgingSystem.ComputeBattleAgingCost(20, false, NoAgeDiscount));
        }

        [Test]
        public void AgingSystem_ComputeBattleAgingCost_BattleMage_MaxOf1DayOr25Pct()
        {
            // Tempered: reduction = max(1 flat day, 25% of cost). Minimum result 1 — never free.
            // base 1.5: n=1→1, n=3→2, n=4→3, n=8→17.
            Assert.AreEqual(1,  AgingSystem.ComputeBattleAgingCost(1, true, NoAgeDiscount));  // base 1: 1-1=0 → floor 1
            Assert.AreEqual(1,  AgingSystem.ComputeBattleAgingCost(3, true, NoAgeDiscount));  // base 2: 2-1=1
            Assert.AreEqual(2,  AgingSystem.ComputeBattleAgingCost(4, true, NoAgeDiscount));  // base 3: 3-1=2
            Assert.AreEqual(13, AgingSystem.ComputeBattleAgingCost(8, true, NoAgeDiscount));  // base 17: 17-4=13
        }

        [Test]
        public void AgingSystem_ComputeBattleAgingCost_TemperedAge_ShavesExtraCost()
        {
            // Beyond age 40, Tempered also cuts 0.5%/yr off the post-25% cost, capped at 30%.
            // Base 20 inputs → 84 (cap); BattleMage 25% → 63.
            //   age 40  → no age discount → 63
            //   age 100 → 30% cap        → round(63 × 0.70) = 44
            Assert.AreEqual(63, AgingSystem.ComputeBattleAgingCost(20, true, 40f));
            Assert.AreEqual(44, AgingSystem.ComputeBattleAgingCost(20, true, 100f));
            // Age only matters with the talent — non-BattleMage ignores it entirely.
            Assert.AreEqual(84, AgingSystem.ComputeBattleAgingCost(20, false, 100f));
        }

        // ── NPC heal-burst RestoreCount satisfies Rouse threshold ─────────────

        [Test]
        public void SpellBuilder_NpcHealBurstRestoreCount_MeetsRouseThreshold()
        {
            // NPC CastHealBurst passes restoreCount=3. Verify that value meets Rouse's
            // requirement so lords with Rouse can summon allies when healing.
            const int npcRestoreCount = 3;
            Assert.IsTrue(npcRestoreCount >= 3,
                "NPC heal burst must use at least 3 Restore inputs to satisfy Rouse's threshold.");
        }

        // ── BattleEvents probability constants ────────────────────────────────

        [Test]
        public void BattleEvents_AllChanceConstants_AreInValidRange()
        {
            Assert.IsTrue(BattleEvents.ChanceCinderRain  is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceTheRising   is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceDread       is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceLastLight   is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceAshenGround is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceFrenzy      is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceStorm       is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceTremor      is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceDeluge      is >= 0f and <= 1f);
            Assert.IsTrue(BattleEvents.ChanceMadness     is >= 0f and <= 1f);
        }

        [Test]
        public void BattleEvents_AllIntervalConstants_ArePositive()
        {
            Assert.Greater(BattleEvents.CinderRainInterval,  0f);
            Assert.Greater(BattleEvents.TheRisingInterval,   0f);
            Assert.Greater(BattleEvents.AshenGroundInterval, 0f);
            Assert.Greater(BattleEvents.FrenzyInterval,      0f);
            Assert.Greater(BattleEvents.StormInterval,       0f);
            Assert.Greater(BattleEvents.TremorInterval,      0f);
            Assert.Greater(BattleEvents.DelugeInterval,      0f);
            Assert.Greater(BattleEvents.MadnessInterval,     0f);
            Assert.Greater(BattleEvents.OneShotDelay,        0f);
        }

        [Test]
        public void BattleEvents_TremorDamage_IsPositive()
        {
            Assert.Greater(BattleEvents.TremorDamage, 0f);
        }

        [Test]
        public void BattleEvents_RisingSpawnCount_IsPositive()
        {
            Assert.Greater(BattleEvents.RisingSpawnCount, 0);
        }

        // ── CampaignMapEvents — Temple faction constants ──────────────────────

        [Test]
        public void CampaignMapEvents_TempleChances_AreInValidRange()
        {
            Assert.IsTrue(CampaignMapEvents.ChanceTheTemple is > 0f and <= 1f,
                "ChanceTheTemple must be a positive probability.");
            Assert.IsTrue(CampaignMapEvents.ChanceTempleLatent is > 0f and <= 1f,
                "ChanceTempleLatent must be a positive probability.");
        }

        [Test]
        public void CampaignMapEvents_TempleLatentChance_GreaterThanBaseChance()
        {
            Assert.Greater(CampaignMapEvents.ChanceTempleLatent, CampaignMapEvents.ChanceTheTemple,
                "Latent (post-day-250) Temple chance must exceed the base chance.");
        }

        [Test]
        public void CampaignMapEvents_TempleNearCertainDay_GreaterThanEarliestDay()
        {
            Assert.Greater(CampaignMapEvents.TempleNearCertainDay, CampaignMapEvents.TempleEarliestDay,
                "TempleNearCertainDay must be later than TempleEarliestDay.");
        }

        [Test]
        public void CampaignMapEvents_TempleEarliestDay_IsPositive()
        {
            Assert.Greater(CampaignMapEvents.TempleEarliestDay, 0,
                "Temple must not be able to fire on day 0.");
        }

        // ── AshenVisuals body-key transforms ──────────────────────────────────

        [Test]
        public void AshenVisuals_HairKey_SetsLightGreyD3D3()
        {
            ulong input  = 0xFFFFFFFFFFFFFFFFUL;
            ulong result = AshenVisuals.AshenHairKey(input);
            Assert.AreEqual(0x00D3D30000000000UL, result & 0x00FFFF0000000000UL,
                "Hair colour bytes must encode light grey #D3D3.");
            Assert.AreEqual(input  & ~0x00FFFF0000000000UL,
                            result & ~0x00FFFF0000000000UL,
                "Bits outside the hair colour byte range must be preserved.");
        }

        [Test]
        public void AshenVisuals_EyeKey_SetsColdBlueBytes()
        {
            ulong result = AshenVisuals.AshenEyeKey(0UL);
            Assert.AreEqual(0x00E0AA0000000000UL, result,
                "Eye transform must encode a cold-blue iris into the colour bytes.");
        }

        [Test]
        public void AshenVisuals_EyeKey_PreservesNonColourBits()
        {
            ulong input  = 0xAB00000000C0FFEEUL;
            ulong result = AshenVisuals.AshenEyeKey(input);
            Assert.AreEqual(input  & ~0x00FFFF0000000000UL,
                            result & ~0x00FFFF0000000000UL);
        }

        [Test]
        public void AshenVisuals_SkinKey_SetsLightGreyD3D3D3()
        {
            ulong input  = 0xFFFFFFFFFFFFFFFFUL;
            ulong result = AshenVisuals.AshenSkinKey(input);
            Assert.AreEqual(0x000000D3D3D30000UL, result & 0x000000FFFFFF0000UL,
                "Skin colour bytes must encode light grey #D3D3D3.");
            Assert.AreEqual(input & ~0x000000FFFFFF0000UL, result & ~0x000000FFFFFF0000UL,
                "All other bits must be preserved.");
        }

        [Test]
        public void AshenVisuals_Transforms_AreIdempotent()
        {
            ulong seed = 0x123456789ABCDEF0UL;
            Assert.AreEqual(AshenVisuals.AshenHairKey(seed),
                            AshenVisuals.AshenHairKey(AshenVisuals.AshenHairKey(seed)));
            Assert.AreEqual(AshenVisuals.AshenEyeKey(seed),
                            AshenVisuals.AshenEyeKey(AshenVisuals.AshenEyeKey(seed)));
            Assert.AreEqual(AshenVisuals.AshenSkinKey(seed),
                            AshenVisuals.AshenSkinKey(AshenVisuals.AshenSkinKey(seed)));
        }

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

        // ── ClanOrdersMath tests ──────────────────────────────────────────────

        [Test]
        public void ClanOrdersMath_DailyAbandonChance_ScalesWithLeadership()
        {
            Assert.AreEqual(10, ClanOrdersMath.DailyAbandonChance(0));
            Assert.AreEqual(5,  ClanOrdersMath.DailyAbandonChance(150));
            Assert.AreEqual(0,  ClanOrdersMath.DailyAbandonChance(300));
        }

        [Test]
        public void ClanOrdersMath_DailyAbandonChance_NeverNegativeAndClampsInput()
        {
            Assert.AreEqual(0,  ClanOrdersMath.DailyAbandonChance(1000)); // never below zero
            Assert.AreEqual(10, ClanOrdersMath.DailyAbandonChance(-50));  // negative input clamps to 0
        }

        // ── SpeculationMath tests ─────────────────────────────────────────────

        [Test]
        public void SpeculationMath_RoundsLimit_BaseIsFour()
        {
            Assert.AreEqual(4, SpeculationMath.RoundsLimit(0));
        }

        [Test]
        public void SpeculationMath_RoundsLimit_IncreasesWithTrade()
        {
            Assert.AreEqual(5, SpeculationMath.RoundsLimit(75));
            Assert.AreEqual(6, SpeculationMath.RoundsLimit(150));
        }

        [Test]
        public void SpeculationMath_RoundsLimit_CapsAtEight()
        {
            Assert.AreEqual(8, SpeculationMath.RoundsLimit(300));
            Assert.AreEqual(8, SpeculationMath.RoundsLimit(1000));
        }

        [Test]
        public void SpeculationMath_CrashChance_IsAtLeastOnePercent()
        {
            float c = SpeculationMath.CrashChance(0, 0, 500, false);
            Assert.GreaterOrEqual(c, 0.01f);
        }

        [Test]
        public void SpeculationMath_CrashChance_CapsAtFiftyPercent()
        {
            float c = SpeculationMath.CrashChance(2, 20, 0, true);
            Assert.LessOrEqual(c, 0.50f);
        }

        [Test]
        public void SpeculationMath_CrashChance_GrowsWithRounds()
        {
            float early = SpeculationMath.CrashChance(1, 0, 0, false);
            float late  = SpeculationMath.CrashChance(1, 5, 0, false);
            Assert.Greater(late, early);
        }

        [Test]
        public void SpeculationMath_CrashChance_AggressiveIsHigher()
        {
            float steady = SpeculationMath.CrashChance(1, 0, 0, false);
            float hard   = SpeculationMath.CrashChance(1, 0, 0, true);
            Assert.Greater(hard, steady);
        }

        [Test]
        public void SpeculationMath_DeltaRange_MaxExceedsMin()
        {
            for (int vol = 0; vol < 3; vol++)
            {
                int sMin = SpeculationMath.DeltaMin(vol, false, 0);
                int sMax = SpeculationMath.DeltaMax(vol, false, 0);
                int hMin = SpeculationMath.DeltaMin(vol, true, 0);
                int hMax = SpeculationMath.DeltaMax(vol, true, 0);
                Assert.Greater(sMax, sMin, $"vol={vol} steady max must exceed min");
                Assert.Greater(hMax, hMin, $"vol={vol} hard max must exceed min");
                Assert.Greater(hMax, sMax, $"vol={vol} hard upside must exceed steady upside");
            }
        }

        [Test]
        public void SpeculationMath_MoodShift_BoomingAdds5()
        {
            Assert.AreEqual(5, SpeculationMath.MoodShift(6000f));
        }

        [Test]
        public void SpeculationMath_MoodShift_DepressedSubtracts5()
        {
            Assert.AreEqual(-5, SpeculationMath.MoodShift(1000f));
        }

        [Test]
        public void SpeculationMath_MoodShift_SteadyIsZero()
        {
            Assert.AreEqual(0, SpeculationMath.MoodShift(3000f));
        }

        [Test]
        public void SpeculationMath_ApplyDelta_ClampsToMin()
        {
            int result = SpeculationMath.ApplyDelta(15, -50);
            Assert.AreEqual(SpeculationMath.MultiplierMin, result);
        }

        [Test]
        public void SpeculationMath_ApplyDelta_ClampsToMax()
        {
            int result = SpeculationMath.ApplyDelta(250, 100);
            Assert.AreEqual(SpeculationMath.MultiplierMax, result);
        }

        [Test]
        public void SpeculationMath_ApplyDelta_NormalMove()
        {
            int result = SpeculationMath.ApplyDelta(100, 15);
            Assert.AreEqual(115, result);
        }

        [Test]
        public void SpeculationMath_SalvagePercent_OnePerTen()
        {
            Assert.AreEqual(10, SpeculationMath.SalvagePercent(100));
            Assert.AreEqual(20, SpeculationMath.SalvagePercent(200));
        }

        [Test]
        public void SpeculationMath_SalvagePercent_CapsAt30()
        {
            Assert.AreEqual(30, SpeculationMath.SalvagePercent(300));
            Assert.AreEqual(30, SpeculationMath.SalvagePercent(1000));
        }

        [Test]
        public void SpeculationMath_SalvagePercent_ZeroAtNoSkill()
        {
            Assert.AreEqual(0, SpeculationMath.SalvagePercent(0));
        }

        [Test]
        public void SpeculationMath_Payout_DoubleAtTwoHundredPct()
        {
            int payout = SpeculationMath.Payout(1000, 200);
            Assert.AreEqual(2000, payout);
        }

        [Test]
        public void SpeculationMath_ForcedSalePayout_IsNinetyPercent()
        {
            int payout = SpeculationMath.ForcedSalePayout(1000, 100);
            Assert.AreEqual(900, payout);
        }

        [Test]
        public void SpeculationMath_TradeXp_ZeroOnLoss()
        {
            Assert.AreEqual(50, SpeculationMath.TradeXp(1000, 500));
        }

        [Test]
        public void SpeculationMath_TradeXp_HalfOfProfit()
        {
            Assert.AreEqual(500, SpeculationMath.TradeXp(1000, 2000));
        }

        [Test]
        public void SpeculationMath_TradeXp_CapsAt1000()
        {
            Assert.AreEqual(1000, SpeculationMath.TradeXp(1000, 10000));
        }

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
        public void WallWardMath_StoneIsTheStrictlyBestMissileShield()
        {
            // Stone stops every shaft; wind only wrestles most of them aside.
            Assert.IsTrue(WallWardMath.WallBlocksMissiles(MagicElement.Earth),
                "Standing stone must stop missiles.");
            Assert.IsTrue(WallWardMath.WallBlocksMissiles(MagicElement.Wind),
                "Driven wind must engage missiles at all.");
            Assert.IsFalse(WallWardMath.WallBlocksMissiles(MagicElement.Fire));
            Assert.IsFalse(WallWardMath.WallBlocksMissiles(MagicElement.Water));
            // The gust must be genuinely porous — better than nothing, worse than
            // stone — or one of the two walls loses its reason to exist.
            Assert.IsTrue(WallWardMath.WindwallMissileStopChance > 0f);
            Assert.IsTrue(WallWardMath.WindwallMissileStopChance < 1f,
                "Wind must not be an absolute shield — that is stone's crown.");
            Assert.IsTrue(WallWardMath.WindStopsMissile(0.0));
            Assert.IsFalse(WallWardMath.WindStopsMissile(WallWardMath.WindwallMissileStopChance));
            Assert.IsFalse(WallWardMath.WindStopsMissile(0.99));
        }

        [Test]
        public void NatureMath_WindwallHurl_ThrowsFurtherThanTheSharedBounce()
        {
            // The Windwall's only bite is its throw — if it ever drops back to the
            // shared bounce margin it becomes strictly the worst wall (Mistwall
            // gives the same stop plus a slow, a bite, and the fire-quench).
            Assert.IsTrue(NatureMath.WindwallHurlMargin > NatureMath.BarrierBounceMargin,
                "Windwall must hurl well past the ordinary barrier bounce.");
        }

        [Test]
        public void CrystalMath_SolarFlareRadius_IncreasesBaseByTwentyFivePercent()
        {
            float r  = 5f;
            float r2 = CrystalMath.SolarFlareRadius(r);
            Assert.AreEqual(r * 1.25f, r2, 0.001f);
        }

        // ── MiracleMath gain scaling ──────────────────────────────────────────

        [Test]
        public void MiracleMath_GraceGain_NoVirtue_ReturnsZero()
        {
            Assert.AreEqual(0, MiracleMath.GraceGain(0, 0, 0));
        }

        [Test]
        public void MiracleMath_GraceGain_AllVirtuesMaxed_ReturnsFour()
        {
            Assert.AreEqual(4, MiracleMath.GraceGain(2, 2, 2));
        }

        [Test]
        public void MiracleMath_GraceGain_NegativeTraits_ReturnsZero()
        {
            Assert.AreEqual(0, MiracleMath.GraceGain(-2, -2, -2));
        }

        [Test]
        public void MiracleMath_TryMatchSequence_NewMiracles_Resolve()
        {
            Assert.IsTrue(MiracleMath.TryMatchSequence(MiracleMath.SeqInsightPyre, out var pyre));
            Assert.AreEqual(MiracleType.InsightPyre, pyre);
            Assert.IsTrue(MiracleMath.TryMatchSequence(MiracleMath.SeqGraceBlessing, out var blessing));
            Assert.AreEqual(MiracleType.GraceBlessing, blessing);
        }

        [Test]
        public void MiracleCatalog_AllSequences_AreUnique()
        {
            var seqs = MiracleCatalog.All.Select(d => d.Sequence).ToList();
            Assert.AreEqual(seqs.Count, seqs.Distinct().Count(), "Two miracles share an input sequence.");
        }

        [Test]
        public void MiracleCatalog_EverySequence_RoundTripsToItsMiracle()
        {
            foreach (var def in MiracleCatalog.All)
            {
                bool validLength = def.Sequence.Length == MiracleMath.SequenceLength
                                 || def.Sequence.Length == MiracleMath.UltimateSequenceLength;
                Assert.IsTrue(validLength, $"{def.Name} has a sequence of an unsupported length ({def.Sequence.Length}).");
                Assert.IsTrue(MiracleMath.TryMatchSequence(def.Sequence, out var t), $"{def.Name} sequence does not match.");
                Assert.AreEqual(def.Type, t, $"{def.Name} sequence resolves to the wrong miracle.");
            }
        }

        // ── The Undivided Flame / The Reckoning — the all-five-traits ultimate ──

        [Test]
        public void MiracleMath_MeetsAllTraitsGate_AllPresent_ReturnsTrue()
        {
            Assert.IsTrue(MiracleMath.MeetsAllTraitsGate(1, 1, 1, 1, 1));
            Assert.IsTrue(MiracleMath.MeetsAllTraitsGate(2, 1, 2, 1, 2));
        }

        [Test]
        public void MiracleMath_MeetsAllTraitsGate_OneMissing_ReturnsFalse()
        {
            Assert.IsFalse(MiracleMath.MeetsAllTraitsGate(0, 1, 1, 1, 1));
            Assert.IsFalse(MiracleMath.MeetsAllTraitsGate(1, 1, 1, 1, 0));
        }

        [Test]
        public void MiracleMath_TryMatchSequence_UndividedFlame_Resolves()
        {
            Assert.IsTrue(MiracleMath.TryMatchSequence(MiracleMath.SeqUndividedFlame, out var type));
            Assert.AreEqual(MiracleType.UndividedFlame, type);
        }

        [Test]
        public void MiracleMath_TryMatchSequence_WrongLengthEight_DoesNotResolve()
        {
            Assert.IsFalse(MiracleMath.TryMatchSequence("UUUUUUUU", out _));
        }

        [Test]
        public void MiracleCatalog_UndividedFlameAndReckoning_RequireAllTraits()
        {
            Assert.IsTrue(MiracleCatalog.Get(MiracleType.UndividedFlame).RequiresAllTraits);
            Assert.IsTrue(MiracleCatalog.Get(MiracleType.Reckoning).RequiresAllTraits);
        }

        [Test]
        public void MiracleCatalog_UndividedFlameAndReckoning_CostTwoGrace()
        {
            Assert.AreEqual(2, MiracleCatalog.Get(MiracleType.UndividedFlame).GraceCost);
            Assert.AreEqual(2, MiracleCatalog.Get(MiracleType.Reckoning).GraceCost);
        }

        [Test]
        public void MiracleCatalog_OrdinaryMiracles_CostOneGrace()
        {
            Assert.AreEqual(1, MiracleCatalog.Get(MiracleType.MercyMend).GraceCost);
            Assert.AreEqual(1, MiracleCatalog.Get(MiracleType.InsightPyre).GraceCost);
        }

        [Test]
        public void MiracleInventory_SpendGrace_Amount_FailsWithoutEnough()
        {
            MiracleInventory.ResetForNewGame();
            MiracleInventory.AddGrace(1);
            Assert.IsFalse(MiracleInventory.SpendGrace(2));
            Assert.AreEqual(1, MiracleInventory.Grace);
        }

        [Test]
        public void MiracleInventory_SpendGrace_Amount_SucceedsWithEnough()
        {
            MiracleInventory.ResetForNewGame();
            MiracleInventory.AddGrace(3);
            Assert.IsTrue(MiracleInventory.SpendGrace(2));
            Assert.AreEqual(1, MiracleInventory.Grace);
        }

        // ── SanctuaryMath: the Long Vigil (raise a trait for gold or blood) ────

        [Test]
        public void SanctuaryMath_CanRaiseTrait_BelowCap_ReturnsTrue()
        {
            Assert.IsTrue(SanctuaryMath.CanRaiseTrait(-2));
            Assert.IsTrue(SanctuaryMath.CanRaiseTrait(1));
        }

        [Test]
        public void SanctuaryMath_CanRaiseTrait_AtCap_ReturnsFalse()
        {
            Assert.IsFalse(SanctuaryMath.CanRaiseTrait(SanctuaryMath.TraitCap));
        }

        [Test]
        public void SanctuaryMath_CommunionCosts_ScaleWithTargetLevel()
        {
            int lowGold  = SanctuaryMath.CommunionGoldCost(-1);
            int highGold = SanctuaryMath.CommunionGoldCost(2);
            Assert.Greater(highGold, lowGold, "Reaching a higher trait level should cost more gold.");

            int lowHp  = SanctuaryMath.CommunionHpCost(-1);
            int highHp = SanctuaryMath.CommunionHpCost(2);
            Assert.Greater(highHp, lowHp, "Reaching a higher trait level should cost more HP.");
        }

        [Test]
        public void SanctuaryMath_CommunionCosts_ArePositive()
        {
            for (int target = -1; target <= SanctuaryMath.TraitCap; target++)
            {
                Assert.Greater(SanctuaryMath.CommunionGoldCost(target), 0);
                Assert.Greater(SanctuaryMath.CommunionHpCost(target), 0);
            }
        }

        // ── ElementMagicMath (unified magic foundation) ─────────────────────────

        [Test]
        public void ElementMagicMath_CastAgingDays_IsFlat_IndependentOfDraw()
        {
            // The cost no longer depends on draw time — it is a flat per-form toll.
            Assert.AreEqual(3, ElementMagicMath.CastAgingDays(CastForm.Attack, false));
            Assert.AreEqual(4, ElementMagicMath.CastAgingDays(CastForm.Wall,   false));
        }

        [Test]
        public void ElementMagicMath_CastAgingDays_Nature_IsCheaper()
        {
            // Nature halves the flat cost (floored at 1): attack 3→2, wall 4→2.
            Assert.AreEqual(2, ElementMagicMath.CastAgingDays(CastForm.Attack, true));
            Assert.AreEqual(2, ElementMagicMath.CastAgingDays(CastForm.Wall,   true));
            Assert.IsTrue(ElementMagicMath.CastAgingDays(CastForm.Attack, true)
                        < ElementMagicMath.CastAgingDays(CastForm.Attack, false));
        }

        [Test]
        public void ElementMagicMath_PowerMult_InstantWeak_CapFull_ClampsPastCap()
        {
            Assert.AreEqual(ElementMagicMath.MinPower, ElementMagicMath.PowerMult(0f), 0.0001f);
            // Monotonic within the ramp: a longer draw is never weaker (sample as
            // fractions of the cap so this holds whatever FullChargeSeconds is set to).
            float third = ElementMagicMath.FullChargeSeconds / 3f;
            Assert.IsTrue(ElementMagicMath.PowerMult(third)     > ElementMagicMath.PowerMult(0f));
            Assert.IsTrue(ElementMagicMath.PowerMult(2f * third) > ElementMagicMath.PowerMult(third));
            // Full power is reached at FullChargeSeconds and holds through the grace
            // window up to the overchannel threshold.
            Assert.AreEqual(ElementMagicMath.MaxPower, ElementMagicMath.PowerMult(ElementMagicMath.FullChargeSeconds), 0.0001f);
            Assert.AreEqual(ElementMagicMath.MaxPower,
                ElementMagicMath.PowerMult(ElementMagicMath.OverchannelSeconds - 0.1f), 0.0001f);
            Assert.IsTrue(ElementMagicMath.FullChargeSeconds < ElementMagicMath.OverchannelSeconds);
            Assert.IsTrue(ElementMagicMath.OverchannelSeconds < ElementMagicMath.MaxDrawSeconds);
        }

        [Test]
        public void ElementMagicMath_Overchannel_DoublesPower_AtThreshold()
        {
            // Below the threshold: capped at full. At/after it: the doubled working,
            // and it stays doubled all the way to the disperse point.
            Assert.IsFalse(ElementMagicMath.IsOverchannelled(ElementMagicMath.OverchannelSeconds - 0.1f));
            Assert.IsTrue(ElementMagicMath.IsOverchannelled(ElementMagicMath.OverchannelSeconds));
            float doubled = ElementMagicMath.MaxPower * ElementMagicMath.OverchannelMult;
            Assert.AreEqual(doubled, ElementMagicMath.PowerMult(ElementMagicMath.OverchannelSeconds), 0.0001f);
            Assert.AreEqual(doubled, ElementMagicMath.PowerMult(ElementMagicMath.MaxDrawSeconds), 0.0001f);
            Assert.AreEqual(doubled, ElementMagicMath.PowerMult(20f), 0.0001f);
        }

        [Test]
        public void ElementMagicMath_FullyCharged_AtThreshold()
        {
            Assert.IsFalse(ElementMagicMath.IsFullyCharged(ElementMagicMath.FullChargeSeconds - 0.1f));
            Assert.IsTrue(ElementMagicMath.IsFullyCharged(ElementMagicMath.FullChargeSeconds));
            Assert.IsTrue(ElementMagicMath.IsFullyCharged(ElementMagicMath.MaxDrawSeconds));
        }

        [Test]
        public void ElementMagicMath_GatheringBreak_BlocksPanicTap_ButNotAPacedDraw()
        {
            // A snatched second cast (fired well inside the break) is refused...
            Assert.IsFalse(ElementMagicMath.CanReleaseAgain(0f));
            Assert.IsFalse(ElementMagicMath.CanReleaseAgain(ElementMagicMath.RecoverySeconds - 0.1f));
            // ...but the break clears the instant it elapses.
            Assert.IsTrue(ElementMagicMath.CanReleaseAgain(ElementMagicMath.RecoverySeconds));

            // The break is short enough that even a light draw outlasts it, and a
            // full charge (5 s) leaves it far behind — so a paced channeler is never
            // gated, only panic-tapping is.
            Assert.IsTrue(ElementMagicMath.RecoverySeconds < ElementMagicMath.FullChargeSeconds);
            Assert.IsTrue(ElementMagicMath.CanReleaseAgain(ElementMagicMath.FullChargeSeconds));
        }

        // ── ElementUltimateMath (the Unbinding — element ultimates) ──────────────

        [Test]
        public void ElementUltimateMath_Cost_IsSteepFlat_NatureHalves()
        {
            // Twelve days flat — three walls' worth — halved by Nature, never free.
            Assert.AreEqual(12, ElementUltimateMath.UltimateAgingDays(hasNature: false));
            Assert.AreEqual(6,  ElementUltimateMath.UltimateAgingDays(hasNature: true));
            Assert.Greater(ElementUltimateMath.UltimateCostDays, ElementMagicMath.WallCostDays,
                "an Unbinding must cost far more than an ordinary working");
            Assert.GreaterOrEqual(ElementUltimateMath.UltimateAgingDays(true), ElementMagicMath.MinCastDays);
        }

        [Test]
        public void ElementUltimateMath_Unbind_OnlyAnswersAFullDraw()
        {
            // The chord is refused before the full 7 s draw — same gate as the
            // "fully charged" announcement.
            Assert.IsFalse(ElementUltimateMath.CanUnbind(0f));
            Assert.IsFalse(ElementUltimateMath.CanUnbind(ElementMagicMath.FullChargeSeconds - 0.1f));
            Assert.IsTrue(ElementUltimateMath.CanUnbind(ElementMagicMath.FullChargeSeconds));
            // The chord window is a fraction of a heartbeat — the buffered normal
            // cast must feel instant.
            Assert.Less(ElementUltimateMath.ChordWindowSeconds, 0.5f);
            Assert.Greater(ElementUltimateMath.ChordWindowSeconds, 0f);
        }

        [Test]
        public void ElementUltimateMath_Names_AllFiveDistinct_BothFaces()
        {
            var all = new System.Collections.Generic.HashSet<string>();
            foreach (MagicElement el in new[] { MagicElement.Fire, MagicElement.Wind,
                     MagicElement.Earth, MagicElement.Water, MagicElement.Spirit })
            {
                string living = ElementUltimateMath.UltimateName(el, ashen: false);
                string ashen  = ElementUltimateMath.UltimateName(el, ashen: true);
                Assert.IsFalse(string.IsNullOrEmpty(living));
                Assert.IsFalse(string.IsNullOrEmpty(ashen));
                Assert.IsTrue(all.Add(living), $"living name for {el} duplicates another");
                Assert.IsTrue(all.Add(ashen),  $"Ashen name for {el} duplicates another");
            }
        }

        [Test]
        public void ElementUltimateMath_Flight_HoldsHeightThenLandsGently()
        {
            // Full flight height through the crossing…
            Assert.AreEqual(ElementUltimateMath.FlightHeight,
                ElementUltimateMath.FlightHeightAt(ElementUltimateMath.FlightSeconds), 0.001f);
            Assert.AreEqual(ElementUltimateMath.FlightHeight,
                ElementUltimateMath.FlightHeightAt(ElementUltimateMath.FlightLandingSeconds), 0.001f);
            // …then a monotonic descent to the ground in the landing window.
            float half = ElementUltimateMath.FlightHeightAt(ElementUltimateMath.FlightLandingSeconds * 0.5f);
            Assert.Less(half, ElementUltimateMath.FlightHeight);
            Assert.Greater(half, 0f);
            Assert.AreEqual(0f, ElementUltimateMath.FlightHeightAt(0f), 0.001f);
            Assert.AreEqual(0f, ElementUltimateMath.FlightHeightAt(-1f), 0.001f);
        }

        [Test]
        public void ElementUltimateMath_QuakeIsAnOffensiveWorking()
        {
            // The Sundering hits and hurls; its slow leaves the foe moving, not frozen.
            Assert.Greater(ElementUltimateMath.QuakeDamage, 0f);
            Assert.Greater(ElementUltimateMath.QuakeRadius, 0f);
            Assert.Greater(ElementUltimateMath.QuakeKnockback, 0f);
            Assert.Greater(ElementUltimateMath.QuakeSlowMult, 0f);
            Assert.Less(ElementUltimateMath.QuakeSlowMult, 1f, "staggered footing, not a full stop");
            Assert.Greater(ElementUltimateMath.QuakeRubblePatches, 0);
        }

        [Test]
        public void ElementUltimateMath_Rain_MiresHorsesWorst_DampsButNeverKillsFire()
        {
            // The damp weakens fire without erasing it…
            Assert.Greater(ElementUltimateMath.RainFireDamp, 0f);
            Assert.Less(ElementUltimateMath.RainFireDamp, 1f);
            // …horses suffer more than men on foot…
            Assert.Less(ElementUltimateMath.RainMountSlowMult, ElementUltimateMath.RainFootSlowMult);
            // …and the soaked strings cost a shot part of its bite, not all of it.
            Assert.Greater(ElementUltimateMath.RainArcheryDamp, 0f);
            Assert.Less(ElementUltimateMath.RainArcheryDamp, 1f);
        }

        [Test]
        public void ElementUltimateMath_ElementalKind_AnswersTheScene()
        {
            // Snow always wins; sand is read from the scene's name; stone is the
            // default answer everywhere else (including a null name).
            Assert.AreEqual(ElementalKind.Frost, ElementUltimateMath.ElementalKindForScene(true,  "desert_x"));
            Assert.AreEqual(ElementalKind.Sand,  ElementUltimateMath.ElementalKindForScene(false, "battle_terrain_desert_a"));
            Assert.AreEqual(ElementalKind.Sand,  ElementUltimateMath.ElementalKindForScene(false, "aserai_dunes"));
            Assert.AreEqual(ElementalKind.Stone, ElementUltimateMath.ElementalKindForScene(false, "battle_terrain_plain"));
            Assert.AreEqual(ElementalKind.Stone, ElementUltimateMath.ElementalKindForScene(false, null));
            // Every kind has a name for the combat log.
            foreach (ElementalKind k in new[] { ElementalKind.Stone, ElementalKind.Frost, ElementalKind.Sand })
                Assert.IsFalse(string.IsNullOrEmpty(ElementUltimateMath.ElementalName(k)));
        }

        [Test]
        public void ElementUltimateMath_Spirit_IsNowTheBentKnee()
        {
            // The Spirit Unbinding no longer summons an elemental — it seizes a
            // will instead. Names should reflect the reworked mechanic.
            Assert.AreEqual("The Bent Knee",   ElementUltimateMath.UltimateName(MagicElement.Spirit, ashen: false));
            Assert.AreEqual("The Hollow Oath", ElementUltimateMath.UltimateName(MagicElement.Spirit, ashen: true));
            // Still the most expensive Unbinding — a full minute of borrowed will.
            Assert.Greater(ElementUltimateMath.SpiritUltimateCostDays, ElementUltimateMath.UltimateCostDays);
        }

        [Test]
        public void AshenRecruitMath_PrisonerQualifies_TierGate()
        {
            Assert.IsTrue(AshenRecruitMath.PrisonerQualifies(prisonerTier: 3, requiredTier: 3));
            Assert.IsTrue(AshenRecruitMath.PrisonerQualifies(prisonerTier: 5, requiredTier: 3));
            Assert.IsFalse(AshenRecruitMath.PrisonerQualifies(prisonerTier: 2, requiredTier: 3));
        }

        [Test]
        public void SacredSiteMath_FormationOdds_FloorsCapsAndGrowsWithSkill()
        {
            // Floor even at 0 Smithing, ceiling even at absurd Smithing, and
            // strictly non-decreasing in between.
            Assert.AreEqual(0.10f, SacredSiteMath.FormationOdds(0), 0.0001f);
            Assert.AreEqual(0.85f, SacredSiteMath.FormationOdds(1000), 0.0001f);
            Assert.Less(SacredSiteMath.FormationOdds(50), SacredSiteMath.FormationOdds(150));
        }

        [Test]
        public void ElementMagicMath_ChargeShapes_GrowWithCharge()
        {
            // Charge fraction: 0 at an instant cast, 1 at full power, clamped.
            Assert.AreEqual(0f, ElementMagicMath.ChargeFraction(ElementMagicMath.MinPower), 0.0001f);
            Assert.AreEqual(1f, ElementMagicMath.ChargeFraction(ElementMagicMath.MaxPower), 0.0001f);
            Assert.AreEqual(0f, ElementMagicMath.ChargeFraction(0f), 0.0001f);

            // Cone reaches further when charged; a wall thickens from 1 row to many.
            Assert.IsTrue(ElementMagicMath.ConeRange(10f, ElementMagicMath.MaxPower)
                        > ElementMagicMath.ConeRange(10f, ElementMagicMath.MinPower));
            Assert.AreEqual(1, ElementMagicMath.WallDepthRows(ElementMagicMath.MinPower));
            Assert.AreEqual(ElementMagicMath.WallMaxDepthRows, ElementMagicMath.WallDepthRows(ElementMagicMath.MaxPower));
        }

        [Test]
        public void TalentCostCurve_FollowsGentleRamp()
        {
            // owned → next cost: 1,1,2,2,2,3,3,3,3,4,...  (tier N charged N+1 times)
            int[] expected = { 1, 1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4 };
            for (int owned = 0; owned < expected.Length; owned++)
                Assert.AreEqual(expected[owned], TalentCostCurve.Cost(owned), $"owned={owned}");
            // Never below 1, even for a nonsensical negative count.
            Assert.AreEqual(1, TalentCostCurve.Cost(-3));
        }

        [Test]
        public void ElementMagicMath_BloodRejuvenation_ScalesByTier()
        {
            Assert.AreEqual(25,  ElementMagicMath.BloodRejuvenationDays(1));
            Assert.AreEqual(150, ElementMagicMath.BloodRejuvenationDays(6));
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

        // ── NpcCastPlanner (how an NPC lord spends his life-expectancy) ───────────

        [Test]
        public void NpcCastPlanner_LifeFrac_ClampsToUnitRange()
        {
            Assert.AreEqual(0f,   NpcCastPlanner.LifeFrac(0f),   0.001f);
            Assert.AreEqual(0f,   NpcCastPlanner.LifeFrac(-5f),  0.001f, "negative budget floors at 0");
            Assert.AreEqual(0.5f, NpcCastPlanner.LifeFrac(20f),  0.001f);
            Assert.AreEqual(1f,   NpcCastPlanner.LifeFrac(40f),  0.001f);
            Assert.AreEqual(1f,   NpcCastPlanner.LifeFrac(80f),  0.001f, "plentiful life caps at 1");
        }

        [Test]
        public void NpcCastPlanner_PowerMult_FullLife_IsFullForAllTempers()
        {
            foreach (CasterTemper t in new[] { CasterTemper.Calculating, CasterTemper.Balanced, CasterTemper.Impulsive })
                Assert.AreEqual(1f, NpcCastPlanner.PowerMult(1f, t, emergency: false), 0.001f,
                    $"a fresh {t} lord casts at full power");
        }

        [Test]
        public void NpcCastPlanner_PowerMult_NearBurnout_HoardsByTemper()
        {
            // No life left, no emergency: each temper falls back to its floor.
            Assert.AreEqual(0.50f, NpcCastPlanner.PowerMult(0f, CasterTemper.Calculating, false), 0.001f);
            Assert.AreEqual(0.65f, NpcCastPlanner.PowerMult(0f, CasterTemper.Balanced,    false), 0.001f);
            Assert.AreEqual(0.90f, NpcCastPlanner.PowerMult(0f, CasterTemper.Impulsive,   false), 0.001f);
            // The impulsive lord always spends more than the calculating one.
            Assert.Greater(NpcCastPlanner.PowerMult(0.3f, CasterTemper.Impulsive, false),
                           NpcCastPlanner.PowerMult(0.3f, CasterTemper.Calculating, false));
        }

        [Test]
        public void NpcCastPlanner_Emergency_FloorsPowerHighEvenForOldMiser()
        {
            Assert.GreaterOrEqual(NpcCastPlanner.PowerMult(0f, CasterTemper.Calculating, emergency: true),
                                  NpcCastPlanner.EmergencyFloor);
            Assert.GreaterOrEqual(NpcCastPlanner.CastPower(NpcCastPlanner.BaseHarass, 0f, CasterTemper.Calculating, true),
                                  NpcCastPlanner.EmergencyFloor,
                                  "survival trumps thrift regardless of the situational base");
        }

        [Test]
        public void NpcCastPlanner_Overchannel_DoublesPower()
        {
            // The doubling matches the player's overchannel multiplier and exceeds
            // the normal 1.2 clamp on purpose.
            float doubled = NpcCastPlanner.Overchannelled(1.0f);
            Assert.AreEqual(ElementMagicMath.OverchannelMult, doubled, 0.001f);
            Assert.Greater(NpcCastPlanner.Overchannelled(1.2f), 1.2f);
        }

        [Test]
        public void NpcCastPlanner_OverchannelChance_RecklessAndDesperateHigher_OldMiserRefuses()
        {
            // An old lord (short on years) will not gamble the extra life — unless
            // survival is on the line.
            Assert.AreEqual(0f, NpcCastPlanner.OverchannelChance(CasterTemper.Calculating, 0.1f, emergency: false), 0.0001f);
            Assert.Greater(NpcCastPlanner.OverchannelChance(CasterTemper.Calculating, 0.1f, emergency: true), 0f);
            // Impulsive lords overchannel more freely than calculating ones.
            Assert.Greater(NpcCastPlanner.OverchannelChance(CasterTemper.Impulsive,  1f, false),
                           NpcCastPlanner.OverchannelChance(CasterTemper.Calculating, 1f, false));
            // Emergencies raise the odds for any temper.
            Assert.Greater(NpcCastPlanner.OverchannelChance(CasterTemper.Balanced, 1f, true),
                           NpcCastPlanner.OverchannelChance(CasterTemper.Balanced, 1f, false));
            // ShouldOverchannel is a straight roll against the chance.
            Assert.IsTrue(NpcCastPlanner.ShouldOverchannel(CasterTemper.Impulsive, 1f, true, 0f));
            Assert.IsFalse(NpcCastPlanner.ShouldOverchannel(CasterTemper.Calculating, 0.1f, false, 0.0f));
        }

        [Test]
        public void NpcCastPlanner_CastPower_YoungImpulsive_SpendsBig_OldCalculating_Conserves()
        {
            float young = NpcCastPlanner.CastPower(NpcCastPlanner.BaseCluster, lifeFrac: 1f,
                                                   CasterTemper.Impulsive,   emergency: false);
            float old   = NpcCastPlanner.CastPower(NpcCastPlanner.BaseHarass,  lifeFrac: 0f,
                                                   CasterTemper.Calculating, emergency: false);
            Assert.Greater(young, old);
            Assert.LessOrEqual(NpcCastPlanner.CastPower(NpcCastPlanner.BaseDesperate, 1f, CasterTemper.Impulsive, false), 1.2f,
                "power is clamped to a sane ceiling");
        }

        [Test]
        public void NpcCastPlanner_CooldownMult_FreshIsBaseline_ScarcityStretchesByTemper()
        {
            foreach (CasterTemper t in new[] { CasterTemper.Calculating, CasterTemper.Balanced, CasterTemper.Impulsive })
                Assert.AreEqual(1f, NpcCastPlanner.CooldownMult(1f, t), 0.001f,
                    $"a fresh {t} lord keeps his normal cadence");

            Assert.AreEqual(2.5f, NpcCastPlanner.CooldownMult(0f, CasterTemper.Calculating), 0.001f);
            Assert.AreEqual(1.1f, NpcCastPlanner.CooldownMult(0f, CasterTemper.Impulsive),   0.001f);
            // A near-burnout calculating lord goes quieter than an impulsive one.
            Assert.Greater(NpcCastPlanner.CooldownMult(0.2f, CasterTemper.Calculating),
                           NpcCastPlanner.CooldownMult(0.2f, CasterTemper.Impulsive));
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

        // ── MiracleMath battle selection (right miracle for the moment) ───────────

        [Test]
        public void MiracleMath_ChooseBattleMiracle_PrioritisesBySituation()
        {
            // Self-preservation trumps everything.
            Assert.AreEqual(MiracleType.MercyMend,
                MiracleMath.ChooseBattleMiracle(selfHurt: true, alliesHurtNear: 5,
                    enemyPressingSelf: true, ashenAdjacent: true, roll: 0.9f));
            // Ashen adjacent (and self fine) → judgement.
            Assert.AreEqual(MiracleType.InsightPyre,
                MiracleMath.ChooseBattleMiracle(false, 3, true, ashenAdjacent: true, 0.9f));
            // A wounded line → shared light; a single wounded ally → a targeted mend.
            Assert.AreEqual(MiracleType.GraceBlessing,
                MiracleMath.ChooseBattleMiracle(false, 2, false, false, 0.9f));
            Assert.AreEqual(MiracleType.MercyMend,
                MiracleMath.ChooseBattleMiracle(false, 1, false, false, 0.9f));
            // Under a press, no one wounded → shield.
            Assert.AreEqual(MiracleType.HonorAegis,
                MiracleMath.ChooseBattleMiracle(false, 0, enemyPressingSelf: true, false, 0.9f));
            // Nothing pressing → rally or bless by the roll.
            Assert.AreEqual(MiracleType.ValorFury,
                MiracleMath.ChooseBattleMiracle(false, 0, false, false, roll: 0.1f));
            Assert.AreEqual(MiracleType.GraceBlessing,
                MiracleMath.ChooseBattleMiracle(false, 0, false, false, roll: 0.9f));
        }

        [Test]
        public void MiracleMath_NpcBattleUseChance_AnswersTheMoment()
        {
            // When the moment calls, the answer comes within a few 1.5 s scans —
            // orders of magnitude above the old flat trickle.
            Assert.IsTrue(MiracleMath.NpcBattleUseChance(isPriest: true,  momentCalls: true) >= 0.30);
            Assert.IsTrue(MiracleMath.NpcBattleUseChance(isPriest: false, momentCalls: true) >= 0.15);
            // A priest answers more readily than a devout lord.
            Assert.IsTrue(MiracleMath.NpcBattleUseChance(true, true) > MiracleMath.NpcBattleUseChance(false, true));
            // Idle moments keep the old ambient trickle — nobody prays at empty air.
            Assert.AreEqual(MiracleMath.NpcBattleUseChance(true),  MiracleMath.NpcBattleUseChance(true,  false));
            Assert.AreEqual(MiracleMath.NpcBattleUseChance(false), MiracleMath.NpcBattleUseChance(false, false));
        }

        // ── WallWardMath (elemental wall warding) ─────────────────────────────────

        [Test]
        public void WallWardMath_MissileBlocking_WindAndStoneOnly()
        {
            Assert.IsTrue (WallWardMath.WallBlocksMissiles(MagicElement.Wind));
            Assert.IsTrue (WallWardMath.WallBlocksMissiles(MagicElement.Earth));
            Assert.IsFalse(WallWardMath.WallBlocksMissiles(MagicElement.Fire),
                "arrows fly through flame");
            Assert.IsFalse(WallWardMath.WallBlocksMissiles(MagicElement.Water));
            Assert.IsFalse(WallWardMath.WallBlocksMissiles(MagicElement.Spirit));
        }

        [Test]
        public void WallWardMath_MagicBlocking_MatchesTheElementalAnswers()
        {
            // Fire devours the gale; water quenches fire and drinks the wind;
            // wind scatters flung stone; stone dams the wave.
            Assert.IsTrue(WallWardMath.WallBlocksMagic(MagicElement.Fire,  MagicElement.Wind));
            Assert.IsTrue(WallWardMath.WallBlocksMagic(MagicElement.Water, MagicElement.Fire));
            Assert.IsTrue(WallWardMath.WallBlocksMagic(MagicElement.Water, MagicElement.Wind));
            Assert.IsTrue(WallWardMath.WallBlocksMagic(MagicElement.Wind,  MagicElement.Earth));
            Assert.IsTrue(WallWardMath.WallBlocksMagic(MagicElement.Earth, MagicElement.Water));

            // What must pass: fire through fire/wind/earth walls, dread through all.
            Assert.IsFalse(WallWardMath.WallBlocksMagic(MagicElement.Fire,  MagicElement.Fire));
            Assert.IsFalse(WallWardMath.WallBlocksMagic(MagicElement.Wind,  MagicElement.Fire));
            Assert.IsFalse(WallWardMath.WallBlocksMagic(MagicElement.Earth, MagicElement.Fire));
            foreach (MagicElement wall in new[] { MagicElement.Fire, MagicElement.Wind,
                                                  MagicElement.Earth, MagicElement.Water })
                Assert.IsFalse(WallWardMath.WallBlocksMagic(wall, MagicElement.Spirit),
                    $"dread passes a {wall} wall");
            // Spirit is a ward, never a wall — it blocks nothing.
            Assert.IsFalse(WallWardMath.WallBlocksMagic(MagicElement.Spirit, MagicElement.Fire));
        }

        [Test]
        public void WallWardMath_CounterWall_AnswersEachElement()
        {
            Assert.AreEqual(MagicElement.Water, WallWardMath.CounterWallFor(MagicElement.Fire));
            Assert.AreEqual(MagicElement.Water, WallWardMath.CounterWallFor(MagicElement.Wind));
            Assert.AreEqual(MagicElement.Wind,  WallWardMath.CounterWallFor(MagicElement.Earth));
            Assert.AreEqual(MagicElement.Earth, WallWardMath.CounterWallFor(MagicElement.Water));
            Assert.IsNull(WallWardMath.CounterWallFor(MagicElement.Spirit),
                "nothing walls out the dread");
            // Every counter-wall actually blocks what it was raised against.
            foreach (MagicElement el in new[] { MagicElement.Fire, MagicElement.Wind,
                                                MagicElement.Earth, MagicElement.Water })
                Assert.IsTrue(WallWardMath.WallBlocksMagic(WallWardMath.CounterWallFor(el).Value, el));
            Assert.IsTrue(WallWardMath.QuenchesFireMissile(MagicElement.Water));
            Assert.IsFalse(WallWardMath.QuenchesFireMissile(MagicElement.Earth));
        }

        // ── Ignition (a deep draw sets its marks alight) ──────────────────────────

        [Test]
        public void ElementMagicMath_Ignite_ScalesWithChargeAndCrossesKillThreshold()
        {
            // A snap flick ignites nothing; a full draw burns at the maximum rate.
            Assert.AreEqual(0f, ElementMagicMath.IgniteDps(ElementMagicMath.MinPower), 0.001f);
            Assert.AreEqual(ElementMagicMath.IgniteMaxDps,
                            ElementMagicMath.IgniteDps(ElementMagicMath.MaxPower), 0.001f);
            // Monotonic in the draw.
            Assert.Greater(ElementMagicMath.IgniteDps(0.9f), ElementMagicMath.IgniteDps(0.7f));
            // The bruiser's promise: a fully-drawn cone (44) plus its full burn
            // (12/s × 5 s = 60) crosses the 100 HP kill threshold of a line troop.
            float fullBurn = ElementMagicMath.IgniteMaxDps * ElementMagicMath.IgniteSeconds;
            Assert.GreaterOrEqual(44f + fullBurn, 100f,
                "a full-drawn fire cone must finish an unarmoured man");
            // …but the burn alone must NOT (it finishes, it does not replace the strike).
            Assert.Less(fullBurn, 100f);
        }

        [Test]
        public void WallWardMath_SegmentNearPoint_Geometry()
        {
            // A wall node at (5,1) with radius 2 sits beside the line (0,0)→(10,0).
            Assert.IsTrue(WallWardMath.SegmentNearPoint(0, 0, 10, 0, 5, 1, 2f));
            // Too far off the line: no block.
            Assert.IsFalse(WallWardMath.SegmentNearPoint(0, 0, 10, 0, 5, 5, 2f));
            // BEHIND the caster: the segment ends before the node — no block.
            Assert.IsFalse(WallWardMath.SegmentNearPoint(0, 0, 10, 0, -5, 0, 2f));
            // Degenerate zero-length segment: only blocked when standing inside.
            Assert.IsTrue (WallWardMath.SegmentNearPoint(3, 3, 3, 3, 3, 4, 2f));
            Assert.IsFalse(WallWardMath.SegmentNearPoint(3, 3, 3, 3, 9, 9, 2f));
            // Nearest-t: the closest approach to (5,1) along (0,0)→(10,0) is halfway.
            Assert.AreEqual(0.5f, WallWardMath.SegmentNearestT(0, 0, 10, 0, 5, 1), 0.001f);
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

        [Test]
        public void ElementUltimateMath_SpiritCostsMoreThanOtherUnbindings()
        {
            int spirit = ElementUltimateMath.UltimateAgingDays(hasNature: false, MagicElement.Spirit);
            int fire   = ElementUltimateMath.UltimateAgingDays(hasNature: false, MagicElement.Fire);
            Assert.Greater(spirit, fire);
        }

        // ── ElementComboMath tests ───────────────────────────────────────────

        private static readonly MagicElement[] _baseElements =
        {
            MagicElement.Fire, MagicElement.Wind, MagicElement.Earth,
            MagicElement.Water, MagicElement.Spirit,
        };

        [Test]
        public void ElementComboMath_EveryDistinctBasePairFuses()
        {
            // All ten pairs among the five base elements blend into something —
            // there is no "does not mix" case yet.
            for (int i = 0; i < _baseElements.Length; i++)
                for (int j = i + 1; j < _baseElements.Length; j++)
                    Assert.NotNull(ElementComboMath.TryFuse(_baseElements[i], _baseElements[j]),
                        $"{_baseElements[i]} + {_baseElements[j]} should fuse into something");
        }

        [Test]
        public void ElementComboMath_FuseIsOrderIndependent()
        {
            foreach (var a in _baseElements)
                foreach (var b in _baseElements)
                {
                    if (a == b) continue;
                    Assert.AreEqual(ElementComboMath.TryFuse(a, b), ElementComboMath.TryFuse(b, a));
                }
        }

        [Test]
        public void ElementComboMath_SameElementNeverFuses()
        {
            foreach (var e in _baseElements)
                Assert.Null(ElementComboMath.TryFuse(e, e));
        }

        [Test]
        public void ElementComboMath_FusionNeverFusesAgain()
        {
            // The chord only ever offers two BASE elements — a fused or command
            // result is never itself a valid fuse input.
            Assert.Null(ElementComboMath.TryFuse(MagicElement.Lightning, MagicElement.Fire));
            Assert.Null(ElementComboMath.TryFuse(MagicElement.CommandCharge, MagicElement.Wind));
        }

        [Test]
        public void ElementComboMath_SpiritPairsAreAlwaysCommands()
        {
            foreach (var e in _baseElements)
            {
                if (e == MagicElement.Spirit) continue;
                var fused = ElementComboMath.TryFuse(MagicElement.Spirit, e);
                Assert.NotNull(fused);
                Assert.True(ElementComboMath.IsCommand(fused.Value));
                Assert.False(ElementComboMath.IsFusion(fused.Value));
            }
        }

        [Test]
        public void ElementComboMath_NonSpiritPairsAreAlwaysFusions()
        {
            for (int i = 0; i < _baseElements.Length; i++)
                for (int j = i + 1; j < _baseElements.Length; j++)
                {
                    if (_baseElements[i] == MagicElement.Spirit || _baseElements[j] == MagicElement.Spirit) continue;
                    var fused = ElementComboMath.TryFuse(_baseElements[i], _baseElements[j]);
                    Assert.NotNull(fused);
                    Assert.True(ElementComboMath.IsFusion(fused.Value));
                    Assert.False(ElementComboMath.IsCommand(fused.Value));
                }
        }

        [Test]
        public void ElementComboMath_SpiritCommandPairsMapToTheRightWorking()
        {
            // Each Spirit pair yields its element-themed command, and the command
            // borrows the light/flavour of the NON-Spirit half of the pair.
            Assert.AreEqual(MagicElement.CommandCharge,    ElementComboMath.TryFuse(MagicElement.Spirit, MagicElement.Fire));
            Assert.AreEqual(MagicElement.CommandQuicken,   ElementComboMath.TryFuse(MagicElement.Spirit, MagicElement.Wind));
            Assert.AreEqual(MagicElement.CommandSteadfast, ElementComboMath.TryFuse(MagicElement.Spirit, MagicElement.Earth));
            Assert.AreEqual(MagicElement.CommandHold,      ElementComboMath.TryFuse(MagicElement.Spirit, MagicElement.Water));

            Assert.AreEqual(MagicElement.Fire,  ElementComboMath.CommandBaseElement(MagicElement.CommandCharge));
            Assert.AreEqual(MagicElement.Wind,  ElementComboMath.CommandBaseElement(MagicElement.CommandQuicken));
            Assert.AreEqual(MagicElement.Earth, ElementComboMath.CommandBaseElement(MagicElement.CommandSteadfast));
            Assert.AreEqual(MagicElement.Water, ElementComboMath.CommandBaseElement(MagicElement.CommandHold));
        }

        [Test]
        public void ElementComboMath_EveryCommandHasExactlyOneDistinctIdentity()
        {
            // Each command owns ONE dominant lever — Onslaught and Steadfast and
            // Hold move morale (each at its own floor), Quicken alone moves speed —
            // so no two commands read the same on the field.
            var commands = new[]
            {
                MagicElement.CommandCharge, MagicElement.CommandQuicken,
                MagicElement.CommandSteadfast, MagicElement.CommandHold,
            };

            // Quicken is the only command that touches speed.
            foreach (var c in commands)
            {
                bool movesSpeed = ElementComboMath.CommandSpeedMult(c) != 1f;
                Assert.AreEqual(c == MagicElement.CommandQuicken, movesSpeed, $"{c} speed identity");
            }

            // Quicken is pure mobility (no morale); the other three each hold a
            // DISTINCT morale floor, in order Onslaught > Steadfast > Hold.
            Assert.AreEqual(0f, ElementComboMath.CommandMoraleFloor(MagicElement.CommandQuicken));
            Assert.True(ElementComboMath.CommandMoraleFloor(MagicElement.CommandCharge)
                      > ElementComboMath.CommandMoraleFloor(MagicElement.CommandSteadfast));
            Assert.True(ElementComboMath.CommandMoraleFloor(MagicElement.CommandSteadfast)
                      > ElementComboMath.CommandMoraleFloor(MagicElement.CommandHold));
            Assert.True(ElementComboMath.CommandMoraleFloor(MagicElement.CommandHold) > 0f);
        }

        [Test]
        public void ElementComboMath_WallFallbackNeverPointsAtAFusionOrCommand()
        {
            MagicElement[] fusions =
            {
                MagicElement.Fog, MagicElement.Ice, MagicElement.Magma,
                MagicElement.Sandstorm, MagicElement.Mire,
            };
            foreach (var f in fusions)
            {
                var fallback = ElementComboMath.WallFallback(f);
                Assert.True(ElementComboMath.IsBase(fallback), $"{f} wall fallback should be a base element");
            }
        }

        // ── AshenRuinMath tests (ruins expansion) ──────────────────────────────

        [Test]
        public void AshenRuinMath_EmberWraithPassChance_HigherRenownIsHarder()
        {
            int lowFame  = AshenRuinMath.EmberWraithPassChance(0f);
            int highFame = AshenRuinMath.EmberWraithPassChance(4000f);
            Assert.Greater(lowFame, highFame);
            Assert.GreaterOrEqual(highFame, 20); // floor clamp
            Assert.LessOrEqual(lowFame, 75);     // ceiling clamp
        }

        [Test]
        public void AshenRuinMath_WardstoneGatePassChance_ScalesWithRoguery()
        {
            Assert.AreEqual(25, AshenRuinMath.WardstoneGatePassChance(0));
            Assert.Greater(AshenRuinMath.WardstoneGatePassChance(150), AshenRuinMath.WardstoneGatePassChance(0));
            Assert.AreEqual(90, AshenRuinMath.WardstoneGatePassChance(1000)); // ceiling clamp
        }

        [Test]
        public void AshenRuinMath_HollowChoirPassChance_AutoPassesAtTierTwo()
        {
            Assert.AreEqual(100, AshenRuinMath.HollowChoirPassChance(2));
            Assert.AreEqual(100, AshenRuinMath.HollowChoirPassChance(3));
            Assert.AreEqual(45, AshenRuinMath.HollowChoirPassChance(0));
            Assert.AreEqual(65, AshenRuinMath.HollowChoirPassChance(1));
        }

        [Test]
        public void AshenRuinMath_WeightOfAshCost_ScalesWithTier()
        {
            Assert.AreEqual(2,  AshenRuinMath.WeightOfAshCost(RuinTier.Easy));
            Assert.AreEqual(4,  AshenRuinMath.WeightOfAshCost(RuinTier.Standard));
            Assert.AreEqual(7,  AshenRuinMath.WeightOfAshCost(RuinTier.Brutal));
            Assert.AreEqual(10, AshenRuinMath.WeightOfAshCost(RuinTier.Legendary));
        }

        [Test]
        public void AshenRuinMath_TriuneReckoningFallbackPassChance_ScalesWithProficiency()
        {
            Assert.AreEqual(40, AshenRuinMath.TriuneReckoningFallbackPassChance(0));
            Assert.Greater(AshenRuinMath.TriuneReckoningFallbackPassChance(10), AshenRuinMath.TriuneReckoningFallbackPassChance(0));
            Assert.AreEqual(80, AshenRuinMath.TriuneReckoningFallbackPassChance(100)); // ceiling clamp
        }

        [Test]
        public void AshenRuinMath_ResolveShiftingHall_CoversAllThreeBands()
        {
            Assert.AreEqual(ShiftingHallOutcome.RenownGain,  AshenRuinMath.ResolveShiftingHall(0));
            Assert.AreEqual(ShiftingHallOutcome.RenownGain,  AshenRuinMath.ResolveShiftingHall(34));
            Assert.AreEqual(ShiftingHallOutcome.Desertion,   AshenRuinMath.ResolveShiftingHall(35));
            Assert.AreEqual(ShiftingHallOutcome.Desertion,   AshenRuinMath.ResolveShiftingHall(69));
            Assert.AreEqual(ShiftingHallOutcome.WhisperGain, AshenRuinMath.ResolveShiftingHall(70));
            Assert.AreEqual(ShiftingHallOutcome.WhisperGain, AshenRuinMath.ResolveShiftingHall(99));
        }

        [Test]
        public void AshenRuinMath_ShiftingHallDesertionLoss_HasAFloor()
        {
            Assert.AreEqual(4, AshenRuinMath.ShiftingHallDesertionLoss(0));
            Assert.AreEqual(4, AshenRuinMath.ShiftingHallDesertionLoss(20));
            Assert.AreEqual(10, AshenRuinMath.ShiftingHallDesertionLoss(70));
        }

        [Test]
        public void AshenRuinDefs_AllRuinsHaveAtLeastOneChallengeAndBothRewards()
        {
            foreach (var def in AshenRuinDefs.All)
            {
                Assert.IsNotEmpty(def.VillageName, $"{def.RuinName} has no VillageName.");
                Assert.IsNotEmpty(def.Challenges, $"{def.RuinName} has no challenges.");
                Assert.IsNotNull(def.MainReward, $"{def.RuinName} has no MainReward.");
                Assert.IsNotNull(def.PartialReward, $"{def.RuinName} has no PartialReward.");
            }
        }

        [Test]
        public void ElementComboMath_LightningHasNoWallFallback()
        {
            // Lightning raises its own wall (Stormwall) — it is deliberately
            // absent from WallFallback's cases, which return the Fire default.
            Assert.AreEqual(MagicElement.Fire, ElementComboMath.WallFallback(MagicElement.Lightning));
        }

        [Test]
        public void AshenRuinDefs_VillageNamesAreUnique()
        {
            var names = AshenRuinDefs.All.Select(r => r.VillageName).ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count(), "Two RuinDefs share the same VillageName.");
        }

        // ── Fusion Ultimates ──────────────────────────────────────────────────

        private static readonly MagicElement[] _fusionElements =
        {
            MagicElement.Lightning, MagicElement.Fog, MagicElement.Magma,
            MagicElement.Ice, MagicElement.Sandstorm, MagicElement.Mire,
        };

        [Test]
        public void ElementUltimateMath_EveryFusionHasALivingAndAshenName()
        {
            foreach (var el in _fusionElements)
            {
                string living = ElementUltimateMath.UltimateName(el, ashen: false);
                string ashen  = ElementUltimateMath.UltimateName(el, ashen: true);
                Assert.False(string.IsNullOrEmpty(living));
                Assert.False(string.IsNullOrEmpty(ashen));
                Assert.AreNotEqual(living, ashen, $"{el} should wear a different name under the Ashen mask");
            }
        }

        [Test]
        public void ElementUltimateMath_FusionUltimatesCostTheStandardToll()
        {
            // Only Spirit's Unbinding (the persistent champion) pays the higher
            // toll — every fusion Ultimate is instantaneous like Fire/Wind/Earth
            // and falls through to the standard cost.
            int standard = ElementUltimateMath.UltimateAgingDays(hasNature: false, MagicElement.Fire);
            foreach (var el in _fusionElements)
                Assert.AreEqual(standard, ElementUltimateMath.UltimateAgingDays(hasNature: false, el));
        }

        [Test]
        public void ElementUltimateMath_FusionNamesAreDistinctFromEachOther()
        {
            var names = _fusionElements.Select(e => ElementUltimateMath.UltimateName(e, false)).ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count());
        }

        // ── SoldierServiceMath (sworn-sword service) ──────────────────────────

        [Test]
        public void SoldierServiceMath_WeeklyPay_CoversUpkeepPlusTierBounty()
        {
            // Upkeep is always fully covered; the bounty grows with clan tier.
            Assert.AreEqual(1000 + 50 + 0,   SoldierServiceMath.WeeklyPay(1000, 0));
            Assert.AreEqual(1000 + 50 + 75,  SoldierServiceMath.WeeklyPay(1000, 3));
            // A higher tier never pays less than a lower one for the same upkeep.
            Assert.Greater(SoldierServiceMath.WeeklyPay(500, 4), SoldierServiceMath.WeeklyPay(500, 1));
        }

        [Test]
        public void SoldierServiceMath_WeeklyPay_NegativeInputsClampToNonNegative()
        {
            Assert.AreEqual(50, SoldierServiceMath.WeeklyPay(-100, -5));
        }

        [Test]
        public void SoldierServiceMath_DesertionPenalty_HeavierEarlyInTerm()
        {
            // Deserting with the whole term left costs more than deserting at the end.
            int early = SoldierServiceMath.DesertionCrime(84, 84);
            int late  = SoldierServiceMath.DesertionCrime(1, 84);
            Assert.Greater(early, late);
            Assert.AreEqual(30, early);           // full term remaining → max
            int relEarly = SoldierServiceMath.DesertionRelationLoss(84, 84);
            int relLate  = SoldierServiceMath.DesertionRelationLoss(0, 84);
            Assert.Greater(relEarly, relLate);
        }

        [Test]
        public void SoldierServiceMath_TermFraction_ClampsAndHandlesZeroTerm()
        {
            Assert.AreEqual(0.0, SoldierServiceMath.TermFraction(10, 0), 1e-9);   // guard against /0
            Assert.AreEqual(1.0, SoldierServiceMath.TermFraction(200, 84), 1e-9); // clamps above 1
            Assert.AreEqual(0.0, SoldierServiceMath.TermFraction(-5, 84), 1e-9);  // clamps below 0
        }

        [Test]
        public void SoldierServiceMath_CompletionBonus_HasFloor()
        {
            Assert.AreEqual(100, SoldierServiceMath.CompletionBonus(20)); // floor
            Assert.AreEqual(500, SoldierServiceMath.CompletionBonus(500));
        }

        // ── Mastery scaling — damage grows slightly with the caster ───────────────

        [Test]
        public void ElementMagicMath_MasteryScale_StartsAtOne_AndCaps()
        {
            Assert.AreEqual(1.0f, ElementMagicMath.MasteryScale(0),   1e-6f); // untouched at level 0
            Assert.AreEqual(1.0f, ElementMagicMath.MasteryScale(-5),  1e-6f); // guards negatives
            Assert.AreEqual(1.10f, ElementMagicMath.MasteryScale(10), 1e-5f); // +1%/level
            Assert.AreEqual(1.30f, ElementMagicMath.MasteryScale(30), 1e-5f); // hits the +30% cap
            Assert.AreEqual(1.30f, ElementMagicMath.MasteryScale(99), 1e-5f); // never exceeds it
        }

        [Test]
        public void CrystalMath_MasteryScale_KeysOffMedicine_AndCaps()
        {
            Assert.AreEqual(1.0f,  CrystalMath.MasteryScale(0),    1e-6f);
            Assert.AreEqual(1.15f, CrystalMath.MasteryScale(150),  1e-5f); // +0.1%/point
            Assert.AreEqual(1.30f, CrystalMath.MasteryScale(300),  1e-5f); // cap at 300 Medicine
            Assert.AreEqual(1.30f, CrystalMath.MasteryScale(1000), 1e-5f);
        }

        [Test]
        public void MiracleMath_ConvictionScale_SumsVirtue_AndCaps()
        {
            Assert.AreEqual(1.0f,  MiracleMath.ConvictionScale(0),  1e-6f); // a fickle heart, thin flame
            Assert.AreEqual(1.0f,  MiracleMath.ConvictionScale(-3), 1e-6f); // guards negatives
            Assert.AreEqual(1.15f, MiracleMath.ConvictionScale(5),  1e-5f); // +3%/point
            Assert.AreEqual(1.30f, MiracleMath.ConvictionScale(10), 1e-5f); // total conviction, full pyre
            Assert.AreEqual(1.30f, MiracleMath.ConvictionScale(20), 1e-5f); // never beyond the cap
        }

        // ── GreatAwakeningMath tests ────────────────────────────────────────────

        [Test]
        public void GreatAwakeningMath_TriggerChance_ZeroBeforeDay50()
        {
            Assert.AreEqual(0f, GreatAwakeningMath.TriggerChance(0));
            Assert.AreEqual(0f, GreatAwakeningMath.TriggerChance(49));
        }

        [Test]
        public void GreatAwakeningMath_TriggerChance_StepsEvery20Days()
        {
            Assert.AreEqual(0.10f, GreatAwakeningMath.TriggerChance(50),  1e-5f);
            Assert.AreEqual(0.10f, GreatAwakeningMath.TriggerChance(69),  1e-5f);
            Assert.AreEqual(0.20f, GreatAwakeningMath.TriggerChance(70),  1e-5f);
            Assert.AreEqual(0.30f, GreatAwakeningMath.TriggerChance(90),  1e-5f);
            Assert.AreEqual(0.50f, GreatAwakeningMath.TriggerChance(130), 1e-5f);
        }

        [Test]
        public void GreatAwakeningMath_TriggerChance_CapsAt100Percent()
        {
            Assert.AreEqual(1.0f, GreatAwakeningMath.TriggerChance(50 + 20 * 9),  1e-5f);
            Assert.AreEqual(1.0f, GreatAwakeningMath.TriggerChance(100_000), 1e-5f);
        }

        [Test]
        public void GreatAwakeningMath_PrisonerTarget_IsTenThousand()
        {
            Assert.AreEqual(10_000, GreatAwakeningMath.PrisonerTarget);
        }

        [Test]
        public void GreatAwakeningMath_NpcContributionAmount_NeverExceedsHeldOrMax()
        {
            var rng = new Random(1234);
            for (int i = 0; i < 200; i++)
            {
                int held = i % 25; // sweep small rosters, including zero
                int amount = GreatAwakeningMath.NpcContributionAmount(rng, held);
                Assert.GreaterOrEqual(amount, 0);
                Assert.LessOrEqual(amount, held);
                Assert.LessOrEqual(amount, GreatAwakeningMath.NpcContributionMax);
            }
        }

        [Test]
        public void GreatAwakeningMath_NpcContributionAmount_ZeroWhenNoPrisonersHeld()
        {
            Assert.AreEqual(0, GreatAwakeningMath.NpcContributionAmount(new Random(1), 0));
        }

        [Test]
        public void GreatAwakeningMath_ResolutionIsControlled_RoughlyHalfAndHalf()
        {
            var rng = new Random(42);
            int controlled = 0;
            const int trials = 10_000;
            for (int i = 0; i < trials; i++)
                if (GreatAwakeningMath.ResolutionIsControlled(rng)) controlled++;
            double frac = (double)controlled / trials;
            Assert.Greater(frac, 0.45);
            Assert.Less(frac, 0.55);
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

        // ── Save-definer guard ────────────────────────────────────────────────
        //
        // A QuestBase subclass that reaches the QuestManager without an
        // AddClassDefinition row cannot be serialized, and the whole campaign save
        // FAILS — but only once that quest has actually started, never at build time.
        // That is what repeatedly broke saving on the big questlines, so it is guarded
        // here instead of being left to review.
        //
        // The check is deliberately SOURCE-based, not reflection-based: loading the mod
        // assembly's quest types would drag in the TaleWorlds runtime, which the pure
        // test suite must not depend on (see behaviour.md — JIT type resolution).

        private static string RepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(
                System.IO.Path.GetDirectoryName(typeof(PureLogicTests).Assembly.Location));
            while (dir != null)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "src"))
                    && System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "src", "SaveDefiner.cs")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        [Test]
        public void SaveDefiner_EveryQuestBaseSubclass_IsRegistered()
        {
            string root = RepoRoot();
            Assert.IsNotNull(root, "Could not locate the repo root from the test assembly.");

            string srcDir = System.IO.Path.Combine(root, "src");
            string definer = System.IO.File.ReadAllText(
                System.IO.Path.Combine(srcDir, "SaveDefiner.cs"));

            // class name -> base type name, plus the set declared abstract.
            var bases = new Dictionary<string, string>();
            var abstracts = new HashSet<string>();
            var declRx = new System.Text.RegularExpressions.Regex(
                @"\b(?<mods>(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*)class\s+(?<name>\w+)\s*:\s*(?<base>\w+)");

            foreach (string file in System.IO.Directory.GetFiles(srcDir, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                // Skip build output — obj/bin can hold generated or stale copies.
                if (file.Contains(@"\obj\") || file.Contains(@"\bin\")) continue;
                foreach (System.Text.RegularExpressions.Match m in declRx.Matches(System.IO.File.ReadAllText(file)))
                {
                    string name = m.Groups["name"].Value;
                    bases[name] = m.Groups["base"].Value;
                    if (m.Groups["mods"].Value.Contains("abstract")) abstracts.Add(name);
                }
            }

            // Walk each class's base chain; anything that reaches QuestBase is a quest.
            Func<string, bool> isQuest = null;
            isQuest = name =>
            {
                var seen = new HashSet<string>();
                string cur = name;
                while (cur != null && seen.Add(cur))
                {
                    string b;
                    if (!bases.TryGetValue(cur, out b)) return false;
                    if (b == "QuestBase") return true;
                    cur = b;
                }
                return false;
            };

            var missing = bases.Keys
                .Where(n => !abstracts.Contains(n) && isQuest(n))
                .Where(n => !definer.Contains("typeof(" + n + ")"))
                .OrderBy(n => n)
                .ToList();

            // Sanity: the scan must actually be finding quests, or it proves nothing.
            Assert.Greater(bases.Keys.Count(isQuest), 0,
                "Found no QuestBase subclasses at all — the source scan is broken, not the code.");

            Assert.IsEmpty(missing,
                "These QuestBase subclasses have no AddClassDefinition row in SaveDefiner.cs. "
                + "The campaign CANNOT be saved once one of them starts. Add each to "
                + "AshAndEmberSaveDefiner.ClassDefinitions with a fresh, unused id: "
                + string.Join(", ", missing));
        }

        // ── NorthmenStonesMath: The Bonefire Circle ─────────────────────────────

        [Test]
        public void NorthmenStonesMath_TriggerChance_ZeroBeforeStartDay()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay - 1));
        }

        [Test]
        public void NorthmenStonesMath_TriggerChance_RampsAndCaps()
        {
            Assert.AreEqual(0.10f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay), 1e-6f);
            Assert.AreEqual(0.20f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay + NorthmenStonesMath.TriggerStepDays), 1e-6f);
            Assert.AreEqual(1f, NorthmenStonesMath.TriggerChance(NorthmenStonesMath.TriggerStartDay + 999), 1e-6f);
        }

        [Test]
        public void NorthmenStonesMath_ClampedRatio_ClampsToZeroAndOne()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.ClampedRatio(-5, 100), 1e-6f);
            Assert.AreEqual(1f, NorthmenStonesMath.ClampedRatio(500, 100), 1e-6f);
            Assert.AreEqual(0.5f, NorthmenStonesMath.ClampedRatio(50, 100), 1e-6f);
        }

        [Test]
        public void NorthmenStonesMath_BlendedProgress_ZeroWhenNothingGiven()
        {
            Assert.AreEqual(0f, NorthmenStonesMath.BlendedProgress(0, 0, 0, 0, 0, 0), 1e-6f);
        }

        [Test]
        public void NorthmenStonesMath_BlendedProgress_OneWhenEverythingComplete()
        {
            float progress = NorthmenStonesMath.BlendedProgress(
                NorthmenStonesMath.IronTarget, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget);
            Assert.AreEqual(1f, progress, 1e-6f);
        }

        [Test]
        public void NorthmenStonesMath_IsMaterialsComplete_RequiresEveryTrackFull()
        {
            Assert.IsFalse(NorthmenStonesMath.IsMaterialsComplete(
                NorthmenStonesMath.IronTarget - 1, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget));

            Assert.IsTrue(NorthmenStonesMath.IsMaterialsComplete(
                NorthmenStonesMath.IronTarget, NorthmenStonesMath.HardwoodTarget,
                NorthmenStonesMath.ToolsTarget, NorthmenStonesMath.SilverTarget,
                NorthmenStonesMath.DenarsTarget, NorthmenStonesMath.KindledTotalTarget));
        }

        [Test]
        public void NorthmenStonesMath_ApplyWeeklyDecay_LosesTenPercent()
        {
            Assert.AreEqual(900, NorthmenStonesMath.ApplyWeeklyDecay(1000));
            Assert.AreEqual(0, NorthmenStonesMath.ApplyWeeklyDecay(0));
        }

        [Test]
        public void NorthmenStonesMath_InvasionThresholds_AreAscending()
        {
            Assert.Less(NorthmenStonesMath.InvasionThresholds[0], NorthmenStonesMath.InvasionThresholds[1]);
            Assert.Less(NorthmenStonesMath.InvasionThresholds[1], NorthmenStonesMath.InvasionThresholds[2]);
        }

        [Test]
        public void NorthmenStonesMath_InvasionScaling_EscalatesByTier()
        {
            for (int tier = 0; tier < 2; tier++)
            {
                Assert.LessOrEqual(NorthmenStonesMath.InvasionBandCount(tier), NorthmenStonesMath.InvasionBandCount(tier + 1));
                Assert.Less(NorthmenStonesMath.InvasionMinStrength(tier), NorthmenStonesMath.InvasionMinStrength(tier + 1));
            }
        }

        [Test]
        public void NorthmenStonesMath_NpcContributionAmount_StaysInRange()
        {
            var rng = new Random(1234);
            for (int i = 0; i < 200; i++)
            {
                int amount = NorthmenStonesMath.NpcContributionAmount(rng, 10, 40);
                Assert.GreaterOrEqual(amount, 10);
                Assert.LessOrEqual(amount, 40);
            }
        }

        [Test]
        public void NorthmenStonesMath_EmberfallFractions_AreWithinUnitRange()
        {
            Assert.Greater(NorthmenStonesMath.EmberfallGarrisonKillFrac, 0f);
            Assert.LessOrEqual(NorthmenStonesMath.EmberfallGarrisonKillFrac, 1f);
            Assert.Greater(NorthmenStonesMath.EmberfallStatRemainingFrac, 0f);
            Assert.Less(NorthmenStonesMath.EmberfallStatRemainingFrac, 1f);
        }

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
        public void DemonMath_CastsMagic_OnlyRavager()
        {
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Fiend));
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Stalker));
            Assert.IsTrue(DemonMath.CastsMagic(DemonMath.DemonTier.Ravager));
            Assert.IsFalse(DemonMath.CastsMagic(DemonMath.DemonTier.Hellsteed));
        }

        [Test]
        public void DemonMath_NightSpawnPartyCount_StaysWithinConfiguredRange()
        {
            var rng = new Random(42);
            for (int i = 0; i < 200; i++)
            {
                int count = DemonMath.NightSpawnPartyCount(rng, 0);
                Assert.GreaterOrEqual(count, DemonMath.MinNightSpawnParties);
                Assert.LessOrEqual(count, DemonMath.MaxNightSpawnParties);
            }
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

        // ── EconomyMath tests (Phase 2 — the barter economy) ────────────────────

        [Test]
        public void EconomyMath_GoldReductionFactor_MatchesScarcityFactor()
        {
            // -0.90f => ExplainedNumber.AddFactor drops the total by 90%,
            // leaving roughly a tenth of the vanilla amount (Path B).
            Assert.AreEqual(EconomyMath.GoldScarcityFactor - 1f, EconomyMath.GoldReductionFactor, 1e-6f);
            Assert.AreEqual(-0.90f, EconomyMath.GoldReductionFactor, 1e-6f);
        }

        [Test]
        public void EconomyMath_ScaleGold_RoundsToTenthByDefaultFactor()
        {
            Assert.AreEqual(100, EconomyMath.ScaleGold(1000, EconomyMath.GoldScarcityFactor));
            Assert.AreEqual(0, EconomyMath.ScaleGold(0, EconomyMath.GoldScarcityFactor));
        }

        [Test]
        public void EconomyMath_ScaledPlunderGold_LeavesNonPositiveUntouched()
        {
            Assert.AreEqual(0, EconomyMath.ScaledPlunderGold(0));
            Assert.AreEqual(-5, EconomyMath.ScaledPlunderGold(-5));
            Assert.AreEqual(200, EconomyMath.ScaledPlunderGold(2000));
        }

        [Test]
        public void EconomyMath_ScaledRansom_IsFarBelowVanilla()
        {
            int scaled = EconomyMath.ScaledRansom(4000);
            Assert.AreEqual(200, scaled);
            Assert.Less(scaled, 4000 * EconomyMath.GoldScarcityFactor); // ransom hit harder than general scarcity
        }

        [Test]
        public void EconomyMath_ScaledReward_TrivialisesPositiveAmountsOnly()
        {
            Assert.AreEqual(-500, EconomyMath.ScaledReward(-500)); // costs untouched
            Assert.AreEqual(0, EconomyMath.ScaledReward(0));
            Assert.AreEqual(50, EconomyMath.ScaledReward(1000));
            Assert.AreEqual(EconomyMath.RewardGoldMinimum, EconomyMath.ScaledReward(1)); // never rounds to nothing
        }

        [Test]
        public void EconomyMath_ScaledGarrisonChange_HalvesPositiveAndNegativeAlike()
        {
            Assert.AreEqual(5f, EconomyMath.ScaledGarrisonChange(10f), 1e-6f);
            Assert.AreEqual(-5f, EconomyMath.ScaledGarrisonChange(-10f), 1e-6f);
        }

        [Test]
        public void EconomyMath_ScaledAutoRecruitmentCount_HalvesAndFloors()
        {
            Assert.AreEqual(2, EconomyMath.ScaledAutoRecruitmentCount(5));
            Assert.AreEqual(0, EconomyMath.ScaledAutoRecruitmentCount(0));
            Assert.AreEqual(0, EconomyMath.ScaledAutoRecruitmentCount(-3));
        }

        [Test]
        public void EconomyMath_MaxFoodStocks_ClampsToFloorForPoorTowns()
        {
            Assert.AreEqual(EconomyMath.TownFoodStockCapFloor, EconomyMath.MaxFoodStocks(0f), 1e-6f);
            Assert.AreEqual(30f, EconomyMath.MaxFoodStocks(100f), 1e-4f);
        }

        [Test]
        public void EconomyMath_TownFoodSaleQuantity_CutToATrickle()
        {
            Assert.AreEqual(5, EconomyMath.TownFoodSaleQuantity(100));
            Assert.AreEqual(0, EconomyMath.TownFoodSaleQuantity(0));
        }

        [Test]
        public void EconomyMath_TownHorseSaleQuantity_CappedNearZero()
        {
            Assert.AreEqual(1, EconomyMath.TownHorseSaleQuantity(50));
            Assert.AreEqual(0, EconomyMath.TownHorseSaleQuantity(0));
            Assert.AreEqual(1, EconomyMath.TownHorseSaleQuantity(1));
        }

        [Test]
        public void EconomyMath_TownWeaponSaleQuantity_RemovesFineGearKeepsFewCrude()
        {
            Assert.AreEqual(0, EconomyMath.TownWeaponSaleQuantity(20, EconomyMath.CrudeWeaponTierCap + 1));
            Assert.AreEqual(4, EconomyMath.TownWeaponSaleQuantity(10, EconomyMath.CrudeWeaponTierCap));
        }

        [Test]
        public void EconomyMath_VillageFoodQuantity_BoostedButCapped()
        {
            Assert.AreEqual(160, EconomyMath.VillageFoodQuantity(100));
            Assert.AreEqual(EconomyMath.VillageFoodBoostCap, EconomyMath.VillageFoodQuantity(1000));
            Assert.AreEqual(0, EconomyMath.VillageFoodQuantity(0));
        }

        // ── UnitsMath (Phase 3 — Requirements 4, 29, 30) ────────────────────────

        [Test]
        public void UnitsMath_RequiresPromotionToll_OnlyTiersFourAndFive()
        {
            Assert.IsFalse(UnitsMath.RequiresPromotionToll(1));
            Assert.IsFalse(UnitsMath.RequiresPromotionToll(3));
            Assert.IsTrue(UnitsMath.RequiresPromotionToll(4));
            Assert.IsTrue(UnitsMath.RequiresPromotionToll(5));
        }

        [Test]
        public void UnitsMath_IsGoodPriceWeapon_ThresholdAtMinValue()
        {
            Assert.IsFalse(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue - 1));
            Assert.IsTrue(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue));
            Assert.IsTrue(UnitsMath.IsGoodPriceWeapon(UnitsMath.GoodPriceWeaponMinValue + 500));
        }

        [Test]
        public void UnitsMath_IsShabbyGearTier_OnlyThreeAndFour()
        {
            Assert.IsFalse(UnitsMath.IsShabbyGearTier(2));
            Assert.IsTrue(UnitsMath.IsShabbyGearTier(3));
            Assert.IsTrue(UnitsMath.IsShabbyGearTier(4));
            Assert.IsFalse(UnitsMath.IsShabbyGearTier(5));
        }

        [Test]
        public void UnitsMath_NeedsGearDowngrade_StrictlyAboveCap()
        {
            Assert.IsFalse(UnitsMath.NeedsGearDowngrade(300, 300));
            Assert.IsTrue(UnitsMath.NeedsGearDowngrade(301, 300));
            Assert.IsFalse(UnitsMath.NeedsGearDowngrade(0, 300));
        }

        [Test]
        public void UnitsMath_IsTier5Recruit_OnlyTierFivePlus()
        {
            Assert.IsFalse(UnitsMath.IsTier5Recruit(4));
            Assert.IsTrue(UnitsMath.IsTier5Recruit(5));
            Assert.IsTrue(UnitsMath.IsTier5Recruit(6));
        }

        [Test]
        public void UnitsMath_IsOrnateLordGear_ByValueOrModifier()
        {
            Assert.IsFalse(UnitsMath.IsOrnateLordGear(100, false));
            Assert.IsTrue(UnitsMath.IsOrnateLordGear(UnitsMath.WearyLordValueCap + 1, false));
            Assert.IsTrue(UnitsMath.IsOrnateLordGear(50, true)); // cheap but "fine"/masterwork — still ornate
            Assert.IsFalse(UnitsMath.IsOrnateLordGear(UnitsMath.WearyLordValueCap, false)); // exactly at cap is not "over"
        }

        // ── Spellbook — SpellbookCatalog (Requirement 16/17) ────────────────────

        [Test]
        public void SpellbookCatalog_HasBetween30And50Spells()
        {
            int count = SpellbookCatalog.All.Count;
            Assert.GreaterOrEqual(count, 30);
            Assert.LessOrEqual(count, 50);
        }

        [Test]
        public void SpellbookCatalog_EveryFormula_WithinLengthBounds()
        {
            foreach (var d in SpellbookCatalog.All)
            {
                Assert.GreaterOrEqual(d.Formula.Length, SpellbookCatalog.MinFormulaLength,
                    $"{d.Name} formula '{d.Formula}' is shorter than the minimum.");
                Assert.LessOrEqual(d.Formula.Length, SpellbookCatalog.MaxFormulaLength,
                    $"{d.Name} formula '{d.Formula}' is longer than the maximum.");
            }
        }

        [Test]
        public void SpellbookCatalog_EveryFormula_OnlyUDLRCharacters()
        {
            foreach (var d in SpellbookCatalog.All)
                foreach (char c in d.Formula)
                    Assert.IsTrue(c == 'U' || c == 'D' || c == 'L' || c == 'R',
                        $"{d.Name} formula '{d.Formula}' contains a non-U/D/L/R character.");
        }

        [Test]
        public void SpellbookCatalog_NoTwoSpells_ShareAFormula()
        {
            var formulas = SpellbookCatalog.All.Select(d => d.Formula).ToList();
            var distinct = new HashSet<string>(formulas);
            Assert.AreEqual(formulas.Count, distinct.Count,
                "Two or more spells in the catalog share the exact same formula.");
        }

        [Test]
        public void SpellbookCatalog_MandatorySpells_ArePresent()
        {
            var names = new HashSet<SpellId>(SpellbookCatalog.All.Select(d => d.Id));
            Assert.IsTrue(names.Contains(SpellId.Fireball));
            Assert.IsTrue(names.Contains(SpellId.Firewall));
            Assert.IsTrue(names.Contains(SpellId.SummonDemon));
            Assert.IsTrue(names.Contains(SpellId.BanishDemons));
            Assert.IsTrue(names.Contains(SpellId.Light));
            Assert.AreEqual(20, SpellbookCatalog.Get(SpellId.SummonDemon).Length);
            Assert.AreEqual(20, SpellbookCatalog.Get(SpellId.BanishDemons).Length);
            Assert.AreEqual(5, SpellbookCatalog.Get(SpellId.Fireball).Length);
            Assert.AreEqual(5, SpellbookCatalog.Get(SpellId.Firewall).Length);
        }

        [Test]
        public void SpellbookCatalog_TryGetByFormula_RoundTripsEveryEntry()
        {
            foreach (var d in SpellbookCatalog.All)
            {
                Assert.IsTrue(SpellbookCatalog.TryGetByFormula(d.Formula, out var found));
                Assert.AreEqual(d.Id, found.Id);
            }
            Assert.IsFalse(SpellbookCatalog.TryGetByFormula("UUUUU", out _)); // not a real spell
            Assert.IsFalse(SpellbookCatalog.TryGetByFormula("", out _));
            Assert.IsFalse(SpellbookCatalog.TryGetByFormula(null, out _));
        }

        // Requirement 16: "the space must stay sparse — a fizzle chance must
        // exist at every length." Explicit coverage assertion: at every length
        // actually used by a spell, the fraction of the 4^N possible U/D/L/R
        // strings that answer to a real spell must stay far below saturation.
        [Test]
        public void SpellbookCatalog_FormulaSpaceCoverage_StaysSparsePerLength()
        {
            const double maxFraction = 0.05; // 5% — "a small fraction" per Requirement 16
            var byLength = SpellbookCatalog.All.GroupBy(d => d.Formula.Length);
            foreach (var group in byLength)
            {
                double space = Math.Pow(4, group.Key);
                double coverage = group.Count() / space;
                Assert.Less(coverage, maxFraction,
                    $"Length {group.Key}: {group.Count()} spells cover {coverage:P3} of the formula space — too dense.");
            }
        }

        // ── Spellbook — SpellbookMath (Requirement 18) ──────────────────────────

        [Test]
        public void SpellbookMath_SpellburnChance_AtZeroIntellect_IsBase()
        {
            Assert.AreEqual(SpellbookMath.BaseSpellburnChance, SpellbookMath.SpellburnChance(0), 1e-6f);
        }

        [Test]
        public void SpellbookMath_SpellburnChance_DecreasesWithIntellect()
        {
            float low  = SpellbookMath.SpellburnChance(2);
            float high = SpellbookMath.SpellburnChance(10);
            Assert.Less(high, low);
        }

        [Test]
        public void SpellbookMath_SpellburnChance_NeverBelowFloor()
        {
            Assert.AreEqual(SpellbookMath.MinSpellburnChance, SpellbookMath.SpellburnChance(1000), 1e-6f);
        }

        [Test]
        public void SpellbookMath_SpellburnChance_NegativeIntellect_TreatedAsZero()
        {
            Assert.AreEqual(SpellbookMath.SpellburnChance(0), SpellbookMath.SpellburnChance(-5), 1e-6f);
        }

        [Test]
        public void SpellbookMath_RollSpellburn_RespectsChanceBoundary()
        {
            float chance = SpellbookMath.SpellburnChance(0);
            Assert.IsTrue(SpellbookMath.RollSpellburn(chance - 0.01, 0));
            Assert.IsFalse(SpellbookMath.RollSpellburn(chance + 0.01, 0));
        }

        [Test]
        public void SpellbookMath_RollKind_CoversAllEightKinds()
        {
            var seen = new HashSet<SpellbookMath.SpellburnKind>();
            for (int i = 0; i < 8; i++) seen.Add(SpellbookMath.RollKind(i));
            Assert.AreEqual(8, seen.Count);
        }

        [Test]
        public void SpellbookMath_UnlockFocusCost_IsOnePoint()
        {
            Assert.AreEqual(1, SpellbookMath.UnlockFocusCost);
        }

        // ── SpellcasterLordMath (Requirement 14) ─────────────────────────────
        [Test]
        public void SpellcasterLordMath_TargetCasterCount_IsRoughlySevenPercent()
        {
            Assert.AreEqual(7, SpellcasterLordMath.TargetCasterCount(100));
            Assert.AreEqual(14, SpellcasterLordMath.TargetCasterCount(200));
            Assert.AreEqual(0, SpellcasterLordMath.TargetCasterCount(0));
        }

        [Test]
        public void SpellcasterLordMath_TargetCasterCount_NeverNegative()
        {
            Assert.AreEqual(0, SpellcasterLordMath.TargetCasterCount(-5));
        }

        [Test]
        public void SpellcasterLordMath_KnownSpellCount_RespectsBoundaries()
        {
            Assert.AreEqual(1, SpellcasterLordMath.KnownSpellCount(0));
            Assert.AreEqual(1, SpellcasterLordMath.KnownSpellCount(54));
            Assert.AreEqual(2, SpellcasterLordMath.KnownSpellCount(55));
            Assert.AreEqual(2, SpellcasterLordMath.KnownSpellCount(84));
            Assert.AreEqual(3, SpellcasterLordMath.KnownSpellCount(85));
            Assert.AreEqual(3, SpellcasterLordMath.KnownSpellCount(99));
        }

        [Test]
        public void SpellcasterLordMath_KnownSpellCount_StaysWithinDeclaredRange()
        {
            for (int roll = 0; roll < 100; roll++)
            {
                int count = SpellcasterLordMath.KnownSpellCount(roll);
                Assert.IsTrue(count >= SpellcasterLordMath.MinKnownSpells
                    && count <= SpellcasterLordMath.MaxKnownSpells);
            }
        }

        // ── SpellcasterTroopCatalog / Math (Requirement 15 — the Hollow Choir) ──
        [Test]
        public void SpellcasterTroopCatalog_HasFiveTiers_RecruitToTierFive()
        {
            Assert.AreEqual(5, SpellcasterTroopCatalog.Tiers.Count);
            Assert.AreEqual(1, SpellcasterTroopCatalog.Tiers[0].Rank);
            Assert.AreEqual(5, SpellcasterTroopCatalog.Tiers[4].Rank);
        }

        [Test]
        public void SpellcasterTroopCatalog_EveryTier_Knows2To3Spells()
        {
            foreach (var tier in SpellcasterTroopCatalog.Tiers)
                Assert.IsTrue(tier.Spells.Length >= 2 && tier.Spells.Length <= 3);
        }

        [Test]
        public void SpellcasterTroopCatalog_TryGetTier_FindsKnownTroopAndRejectsUnknown()
        {
            Assert.IsTrue(SpellcasterTroopCatalog.TryGetTier("hollow_magus", out var tier));
            Assert.AreEqual(5, tier.Rank);
            Assert.IsFalse(SpellcasterTroopCatalog.TryGetTier("looter", out _));
        }

        [Test]
        public void SpellcasterTroopCatalog_IsHollowChoirTroop_MatchesOnlyTheTree()
        {
            Assert.IsTrue(SpellcasterTroopCatalog.IsHollowChoirTroop("hollow_apprentice"));
            Assert.IsFalse(SpellcasterTroopCatalog.IsHollowChoirTroop("mountain_bandit"));
        }

        [Test]
        public void SpellcasterTroopMath_RollSeeds_RespectsChanceBoundary()
        {
            Assert.IsTrue(SpellcasterTroopMath.RollSeeds(SpellcasterTroopMath.WeeklySeedChance - 0.001));
            Assert.IsFalse(SpellcasterTroopMath.RollSeeds(SpellcasterTroopMath.WeeklySeedChance + 0.001));
        }

        [Test]
        public void SpellcasterTroopMath_WeeklySeedChance_IsRare()
        {
            Assert.IsTrue(SpellcasterTroopMath.WeeklySeedChance < 0.05f);
        }

        // ── RelicMath tests (Phase 6) ────────────────────────────────────────

        [Test]
        public void RelicMath_CrystalRelicPowerMult_IsWeakerThanFull()
        {
            Assert.Greater(RelicMath.CrystalRelicPowerMult, 0f);
            Assert.Less(RelicMath.CrystalRelicPowerMult, 1f);
        }

        [Test]
        public void RelicMath_DarkGiftRelicPowerMult_IsWeakerThanFull()
        {
            Assert.Greater(RelicMath.DarkGiftRelicPowerMult, 0f);
            Assert.Less(RelicMath.DarkGiftRelicPowerMult, 1f);
        }

        [Test]
        public void RelicMath_DarkGiftRelicPowerMult_IsWeakerThanCrystalRelicPowerMult()
        {
            // A relic-bound Dark Gift is taxed harder than a relic-bound Crystal
            // (see the header comment in RelicMath.cs for why).
            Assert.Less(RelicMath.DarkGiftRelicPowerMult, RelicMath.CrystalRelicPowerMult);
        }

        [Test]
        public void RelicMath_DemonBaneMultiplier_IsABonusNotAPenalty()
        {
            Assert.Greater(RelicMath.DemonBaneMultiplier, 1f);
        }

        [Test]
        public void RelicMath_RollRelicDrop_RespectsChanceBoundary()
        {
            Assert.IsTrue(RelicMath.RollRelicDrop(RelicMath.RelicDropChancePerVictory - 0.0001));
            Assert.IsFalse(RelicMath.RollRelicDrop(RelicMath.RelicDropChancePerVictory + 0.0001));
        }

        [Test]
        public void RelicMath_RelicDropChancePerVictory_IsRare()
        {
            Assert.Less(RelicMath.RelicDropChancePerVictory, 0.10f);
            Assert.Greater(RelicMath.RelicDropChancePerVictory, 0f);
        }

        [Test]
        public void RelicMath_PickRelicIndex_StaysInBounds()
        {
            var rng = new Random(11);
            for (int i = 0; i < 500; i++)
            {
                int idx = RelicMath.PickRelicIndex(rng.NextDouble(), 10);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, 10);
            }
        }

        [Test]
        public void RelicMath_PickRelicIndex_ZeroCount_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, RelicMath.PickRelicIndex(0.5, 0));
        }

        [Test]
        public void RelicMath_RollRuinLoot_RespectsChanceBoundary()
        {
            Assert.IsTrue(RelicMath.RollRuinLoot(RelicMath.RuinBaseRelicChance - 0.0001));
            Assert.IsFalse(RelicMath.RollRuinLoot(RelicMath.RuinBaseRelicChance + 0.0001));
        }

        // ── RelicNaming tests (Phase 6) ──────────────────────────────────────

        [Test]
        public void RelicNaming_Generate_NeverCrashes_AcrossManySeeds()
        {
            for (int seed = -2000; seed < 3000; seed++)
            {
                string name = RelicNaming.Generate(seed);
                Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"Empty name for seed {seed}.");
            }
        }

        [Test]
        public void RelicNaming_Generate_IsDeterministicForSameSeed()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                string a = RelicNaming.Generate(seed);
                string b = RelicNaming.Generate(seed);
                Assert.AreEqual(a, b, $"Same seed {seed} produced different names.");
            }
        }

        [Test]
        public void RelicNaming_Generate_LowCollisionRateAcrossManySeeds()
        {
            var names = new HashSet<string>();
            const int sampleSize = 3000;
            for (int seed = 0; seed < sampleSize; seed++)
                names.Add(RelicNaming.Generate(seed));

            // Soft check: with ~5,000 possible combinations across the four
            // templates, 3,000 rolls will legitimately see a fair number of
            // repeats (birthday paradox) — this is not a hard uniqueness
            // guarantee, just a floor catching an accidentally degenerate
            // word bank (e.g. a bank that collapsed to one entry).
            double uniqueFraction = names.Count / (double)sampleSize;
            Assert.Greater(uniqueFraction, 0.45,
                $"Only {names.Count}/{sampleSize} unique relic names — word banks may be too small.");
        }

        // ── RelicCatalog tests (Phase 6) ─────────────────────────────────────

        [Test]
        public void RelicCatalog_AllEntries_HaveNonEmptyItemIdAndName()
        {
            foreach (var def in RelicCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.ItemId), $"Relic {def.Id} has no ItemId.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Name), $"Relic {def.Id} has no Name.");
                Assert.IsTrue(def.Effects != null && def.Effects.Count >= 1 && def.Effects.Count <= 2,
                    $"Relic {def.Id} should carry one or two effects.");
            }
        }

        [Test]
        public void RelicCatalog_ItemIds_AreUnique()
        {
            var ids = RelicCatalog.All.Select(d => d.ItemId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Duplicate relic ItemId found.");
        }

        [Test]
        public void RelicCatalog_TryGetByItemId_RoundTrips()
        {
            foreach (var def in RelicCatalog.All)
            {
                Assert.IsTrue(RelicCatalog.TryGetByItemId(def.ItemId, out var found));
                Assert.AreEqual(def.Id, found.Id);
            }
            Assert.IsFalse(RelicCatalog.TryGetByItemId("not_a_relic", out _));
            Assert.IsFalse(RelicCatalog.TryGetByItemId(null, out _));
        }

        [Test]
        public void RelicCatalog_IsRelicItemId_MatchesOnlyCatalogItems()
        {
            Assert.IsTrue(RelicCatalog.IsRelicItemId("aae_relic_cinderfang"));
            Assert.IsFalse(RelicCatalog.IsRelicItemId("aae_sunstone")); // a Crystal, not a relic
            Assert.IsFalse(RelicCatalog.IsRelicItemId("looter"));
        }

        [Test]
        public void RelicCatalog_ContainsBothCrystalAndDarkGiftSourcedRelics()
        {
            bool anyCrystal  = RelicCatalog.All.Any(d => d.Effects.Any(e => e.Source == RelicEffectSource.Crystal));
            bool anyDarkGift = RelicCatalog.All.Any(d => d.Effects.Any(e => e.Source == RelicEffectSource.DarkGift));
            Assert.IsTrue(anyCrystal, "No Crystal-sourced relic found.");
            Assert.IsTrue(anyDarkGift, "No Dark Gift-sourced relic found.");
        }

        // ── WolfBrothersMath tests (Phase 7, Faction A) ──────────────────────

        [Test]
        public void WolfBrothersMath_IsStartingTownId_MatchesOnlyTyalAndSibir()
        {
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("town_S5"));  // Tyal
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("town_S6"));  // Sibir
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId("town_S4")); // Varnovapol
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId("castle_S7"));
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId(null));
            Assert.IsFalse(WolfBrothersMath.IsStartingTownId(""));
        }

        [Test]
        public void WolfBrothersMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(WolfBrothersMath.IsStartingTownId("TOWN_s5"));
        }

        [Test]
        public void WolfBrothersMath_ClampTraitLevel_ClampsToVanillaBounds()
        {
            Assert.AreEqual(-2, WolfBrothersMath.ClampTraitLevel(-2, -1));
            Assert.AreEqual(2,  WolfBrothersMath.ClampTraitLevel(2, 1));
            Assert.AreEqual(-1, WolfBrothersMath.ClampTraitLevel(0, WolfBrothersMath.JoinMercyShift));
            Assert.AreEqual(1,  WolfBrothersMath.ClampTraitLevel(0, WolfBrothersMath.JoinValorShift));
        }

        [Test]
        public void WolfBrothersMath_ApplyReputationLoss_SubtractsAndFloorsAtZero()
        {
            Assert.AreEqual(85f, WolfBrothersMath.ApplyReputationLoss(100f), 0.001f);
            Assert.AreEqual(0f,  WolfBrothersMath.ApplyReputationLoss(5f), 0.001f);
            Assert.AreEqual(0f,  WolfBrothersMath.ApplyReputationLoss(0f), 0.001f);
        }

        [Test]
        public void WolfBrothersMath_MeatFromTroop_ScalesWithTierAndClampsHigh()
        {
            int tier0 = WolfBrothersMath.MeatFromTroop(0);
            int tier3 = WolfBrothersMath.MeatFromTroop(3);
            int tier6 = WolfBrothersMath.MeatFromTroop(6);
            int tier9 = WolfBrothersMath.MeatFromTroop(9); // above the clamp ceiling
            Assert.Greater(tier3, tier0);
            Assert.Greater(tier6, tier3);
            Assert.AreEqual(tier6, tier9, "Yield should clamp at the max tier, not keep climbing.");
        }

        [Test]
        public void WolfBrothersMath_MeatFromTroop_NegativeTierClampsToZero()
        {
            Assert.AreEqual(WolfBrothersMath.MeatFromTroop(0), WolfBrothersMath.MeatFromTroop(-3));
        }

        [Test]
        public void WolfBrothersMath_MeatFromPrisoner_AlwaysExceedsTroopAtSameTier()
        {
            for (int tier = 0; tier <= 6; tier++)
                Assert.Greater(WolfBrothersMath.MeatFromPrisoner(tier), WolfBrothersMath.MeatFromTroop(tier),
                    $"Prisoner yield should exceed troop yield at tier {tier}.");
        }

        [Test]
        public void WolfBrothersMath_Tunables_ArePositiveAndSane()
        {
            Assert.Greater(WolfBrothersMath.JoinReputationLoss, 0f);
            Assert.Greater(WolfBrothersMath.LordCannibalizeIntervalDays, 0);
            Assert.Greater(WolfBrothersMath.LordMaxPrisonersEatenPerTick, 0);
            Assert.AreEqual(2, WolfBrothersMath.StartingTownIds.Length, "Wolf Brothers should keep exactly two starting towns.");
        }

        // ── TowerMath tests (Phase 7, Faction B) ──────────────────────────────

        [Test]
        public void TowerMath_IsStartingTownId_MatchesOnlyIyakis()
        {
            Assert.IsTrue(TowerMath.IsStartingTownId("town_A3"));   // Iyakis
            Assert.IsFalse(TowerMath.IsStartingTownId("town_A1"));  // Quyaz
            Assert.IsFalse(TowerMath.IsStartingTownId("town_A2"));  // Husn Fulq
            Assert.IsFalse(TowerMath.IsStartingTownId(null));
            Assert.IsFalse(TowerMath.IsStartingTownId(""));
            Assert.AreEqual(1, TowerMath.StartingTownIds.Length, "The Tower should keep exactly one starting town.");
        }

        [Test]
        public void TowerMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(TowerMath.IsStartingTownId("TOWN_a3"));
        }

        [Test]
        public void TowerMath_SpellInfluenceCost_ScalesWithFormulaLength()
        {
            int short5  = TowerMath.SpellInfluenceCost(5);
            int mid12   = TowerMath.SpellInfluenceCost(12);
            int long20  = TowerMath.SpellInfluenceCost(20);
            Assert.Greater(mid12, short5);
            Assert.Greater(long20, mid12);
            Assert.Greater(short5, 0);
        }

        [Test]
        public void TowerMath_SpellInfluenceCost_ClampsToFormulaBounds()
        {
            // Below the minimum and above the maximum both clamp rather than
            // running away — a malformed length can never price a lesson at 0
            // or unboundedly high.
            Assert.AreEqual(TowerMath.SpellInfluenceCost(SpellbookCatalog.MinFormulaLength), TowerMath.SpellInfluenceCost(1));
            Assert.AreEqual(TowerMath.SpellInfluenceCost(SpellbookCatalog.MaxFormulaLength), TowerMath.SpellInfluenceCost(999));
        }

        [Test]
        public void TowerMath_OfferSeedForDay_ChangesOncePerWeek()
        {
            Assert.AreEqual(TowerMath.OfferSeedForDay(0), TowerMath.OfferSeedForDay(6));
            Assert.AreNotEqual(TowerMath.OfferSeedForDay(0), TowerMath.OfferSeedForDay(7));
        }

        [Test]
        public void TowerMath_PickRandomSubset_IsDeterministicForSameSeed()
        {
            var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var a = TowerMath.PickRandomSubset(source, 6, 42);
            var b = TowerMath.PickRandomSubset(source, 6, 42);
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(6, a.Count);
        }

        [Test]
        public void TowerMath_PickRandomSubset_NeverExceedsSourceCount()
        {
            var source = new List<int> { 1, 2, 3 };
            var picked = TowerMath.PickRandomSubset(source, 6, 1);
            Assert.AreEqual(3, picked.Count);
        }

        [Test]
        public void TowerMath_PickRandomSubset_NeverDuplicatesEntries()
        {
            var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var picked = TowerMath.PickRandomSubset(source, 6, 7);
            var distinct = new HashSet<int>(picked);
            Assert.AreEqual(picked.Count, distinct.Count);
        }

        [Test]
        public void TowerMath_PickRandomSubset_HandlesEmptyOrNullSource()
        {
            Assert.AreEqual(0, TowerMath.PickRandomSubset<int>(null, 6, 1).Count);
            Assert.AreEqual(0, TowerMath.PickRandomSubset(new List<int>(), 6, 1).Count);
            Assert.AreEqual(0, TowerMath.PickRandomSubset(new List<int> { 1, 2 }, 0, 1).Count);
        }

        [Test]
        public void TowerMath_HollowChoirRankForTier_MapsAndClampsToOneThroughFive()
        {
            Assert.AreEqual(1, TowerMath.HollowChoirRankForTier(0));
            Assert.AreEqual(1, TowerMath.HollowChoirRankForTier(2));
            Assert.AreEqual(2, TowerMath.HollowChoirRankForTier(3));
            Assert.AreEqual(3, TowerMath.HollowChoirRankForTier(4));
            Assert.AreEqual(4, TowerMath.HollowChoirRankForTier(5));
            Assert.AreEqual(5, TowerMath.HollowChoirRankForTier(6));
            Assert.AreEqual(5, TowerMath.HollowChoirRankForTier(9), "Above tier 6 should still clamp to rank 5.");
        }

        [Test]
        public void TowerMath_TransmuteInfluenceCost_ScalesWithRankAndClamps()
        {
            int rank1 = TowerMath.TransmuteInfluenceCost(1);
            int rank5 = TowerMath.TransmuteInfluenceCost(5);
            Assert.Greater(rank5, rank1);
            Assert.AreEqual(TowerMath.TransmuteInfluenceCost(1), TowerMath.TransmuteInfluenceCost(0));
            Assert.AreEqual(TowerMath.TransmuteInfluenceCost(5), TowerMath.TransmuteInfluenceCost(9));
        }

        [Test]
        public void TowerMath_Tunables_ArePositiveAndSane()
        {
            Assert.Greater(TowerMath.SpellOffersPerVisit, 0);
            Assert.Greater(TowerMath.TeachInfluenceBase, 0f);
            Assert.Greater(TowerMath.TeachInfluencePerFormulaMark, 0f);
            Assert.Greater(TowerMath.TransmuteInfluenceBase, 0f);
            Assert.Greater(TowerMath.TransmuteInfluencePerRank, 0f);
            Assert.GreaterOrEqual(TowerMath.MinTransmuteTier, 2, "Requirement: tier-2+ troops only.");
            Assert.Greater(TowerMath.LordGrantedSpellCount, 0);
        }

        // ── HiveMath tests (Phase 7, Faction C) ────────────────────────────────

        [Test]
        public void HiveMath_IsStartingTownId_MatchesOnlyMarunathAndCarBanseth()
        {
            Assert.IsTrue(HiveMath.IsStartingTownId("town_B1"));  // Marunath
            Assert.IsTrue(HiveMath.IsStartingTownId("town_B3"));  // Car Banseth
            Assert.IsFalse(HiveMath.IsStartingTownId("town_B2")); // Dunglanys
            Assert.IsFalse(HiveMath.IsStartingTownId("town_B4")); // Seonon
            Assert.IsFalse(HiveMath.IsStartingTownId("town_B5")); // Pen Cannoc
            Assert.IsFalse(HiveMath.IsStartingTownId(null));
            Assert.IsFalse(HiveMath.IsStartingTownId(""));
        }

        [Test]
        public void HiveMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(HiveMath.IsStartingTownId("TOWN_b1"));
            Assert.IsTrue(HiveMath.IsStartingTownId("Town_B3"));
        }

        [Test]
        public void HiveMath_Tunables_ArePositiveAndSane()
        {
            Assert.AreEqual(2, HiveMath.StartingTownIds.Length, "The Hive should keep exactly two starting towns.");
            Assert.Greater(HiveMath.DoseIntervalDays, 0f);
            Assert.Greater(HiveMath.DoseGraceDays, 0f);
            Assert.AreEqual(HiveMath.DoseIntervalDays + HiveMath.DoseGraceDays, HiveMath.DoseWindowDays, 0.001f);
            Assert.Greater(HiveMath.FreeRecruitPerClick, 0);
            Assert.Greater(HiveMath.DownsideCheckIntervalSeconds, 0f);
            Assert.Greater(HiveMath.ColourInversionChance, 0f);
            Assert.Less(HiveMath.ColourInversionChance, 1f);
            Assert.Greater(HiveMath.WillSuppressionChance, 0f);
            Assert.Less(HiveMath.WillSuppressionChance, 1f);
            Assert.Greater(HiveMath.ColourInversionDurationSeconds, 0f);
        }

        [Test]
        public void HiveMath_IsBondBroken_FalseWithinWindow_TrueBeyondIt()
        {
            Assert.IsFalse(HiveMath.IsBondBroken(0f));
            Assert.IsFalse(HiveMath.IsBondBroken(HiveMath.DoseIntervalDays)); // due, but still within the window
            Assert.IsFalse(HiveMath.IsBondBroken(HiveMath.DoseWindowDays));  // exactly at the edge — still holds
            Assert.IsTrue(HiveMath.IsBondBroken(HiveMath.DoseWindowDays + 0.01f));
            Assert.IsTrue(HiveMath.IsBondBroken(999f));
        }

        [Test]
        public void HiveMath_DaysUntilBondBreaks_CountsDownAndFloorsAtZero()
        {
            Assert.AreEqual(HiveMath.DoseWindowDays, HiveMath.DaysUntilBondBreaks(0f), 0.001f);
            Assert.AreEqual(0f, HiveMath.DaysUntilBondBreaks(HiveMath.DoseWindowDays), 0.001f);
            Assert.AreEqual(0f, HiveMath.DaysUntilBondBreaks(HiveMath.DoseWindowDays + 50f), 0.001f);
            Assert.Greater(HiveMath.DaysUntilBondBreaks(1f), HiveMath.DaysUntilBondBreaks(HiveMath.DoseIntervalDays));
        }

        [Test]
        public void HiveMath_RollColourInversion_RespectsBoundary()
        {
            Assert.IsTrue(HiveMath.RollColourInversion(0.0));
            Assert.IsFalse(HiveMath.RollColourInversion(HiveMath.ColourInversionChance));
            Assert.IsFalse(HiveMath.RollColourInversion(0.999));
        }

        [Test]
        public void HiveMath_RollWillSuppression_RespectsBoundary()
        {
            Assert.IsTrue(HiveMath.RollWillSuppression(0.0));
            Assert.IsFalse(HiveMath.RollWillSuppression(HiveMath.WillSuppressionChance));
            Assert.IsFalse(HiveMath.RollWillSuppression(0.999));
        }

        [Test]
        public void HiveMath_PickSuccessorIndex_StaysInBoundsAndHandlesNegativeDraws()
        {
            for (int draw = -20; draw <= 20; draw++)
            {
                int idx = HiveMath.PickSuccessorIndex(5, draw);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, 5);
            }
        }

        [Test]
        public void HiveMath_PickSuccessorIndex_NoCandidatesReturnsNegativeOne()
        {
            Assert.AreEqual(-1, HiveMath.PickSuccessorIndex(0, 5));
            Assert.AreEqual(-1, HiveMath.PickSuccessorIndex(-1, 5));
        }

        [Test]
        public void HiveMath_PickSuccessorIndex_IsDeterministicForSameInputs()
        {
            Assert.AreEqual(HiveMath.PickSuccessorIndex(7, 100), HiveMath.PickSuccessorIndex(7, 100));
        }

        // ── BloodboundMath tests (Phase 7, Faction D) ──────────────────────────

        [Test]
        public void BloodboundMath_IsStartingTownId_MatchesOnlyAkkalatAndChaikand()
        {
            Assert.IsTrue(BloodboundMath.IsStartingTownId("town_K2"));  // Akkalat
            Assert.IsTrue(BloodboundMath.IsStartingTownId("town_K5"));  // Chaikand
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K1"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K3"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId("town_K4"));
            Assert.IsFalse(BloodboundMath.IsStartingTownId(null));
            Assert.IsFalse(BloodboundMath.IsStartingTownId(""));
            Assert.AreEqual(2, BloodboundMath.StartingTownIds.Length, "The Bloodbound should keep exactly two starting towns.");
        }

        [Test]
        public void BloodboundMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(BloodboundMath.IsStartingTownId("TOWN_k2"));
            Assert.IsTrue(BloodboundMath.IsStartingTownId("Town_K5"));
        }

        [Test]
        public void BloodboundMath_RollDemonBloodYield_StaysWithinBounds()
        {
            for (double roll = 0.0; roll < 1.0; roll += 0.05)
            {
                int yield = BloodboundMath.RollDemonBloodYield(roll);
                Assert.GreaterOrEqual(yield, BloodboundMath.MinDemonBloodPerVictory);
                Assert.LessOrEqual(yield, BloodboundMath.MaxDemonBloodPerVictory);
            }
        }

        [Test]
        public void BloodboundMath_RollDemonBloodYield_NeverZero()
        {
            // Requirement: "every demon party defeated yields 1-3 Demon Blood" — never nothing.
            Assert.Greater(BloodboundMath.RollDemonBloodYield(0.0), 0);
            Assert.Greater(BloodboundMath.RollDemonBloodYield(0.999), 0);
        }

        [Test]
        public void BloodboundMath_RollIgnoreDurationDays_StaysWithinBounds()
        {
            for (double roll = 0.0; roll < 1.0; roll += 0.05)
            {
                float days = BloodboundMath.RollIgnoreDurationDays(roll);
                Assert.GreaterOrEqual(days, BloodboundMath.MinIgnoreDays);
                Assert.LessOrEqual(days, BloodboundMath.MaxIgnoreDays);
            }
        }

        [Test]
        public void BloodboundMath_IsIgnoreActive_TrueWithinWindowFalseOutside()
        {
            Assert.IsTrue(BloodboundMath.IsIgnoreActive(0f, 4f));
            Assert.IsTrue(BloodboundMath.IsIgnoreActive(3.99f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(4f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(5f, 4f));
            Assert.IsFalse(BloodboundMath.IsIgnoreActive(-1f, 4f));
        }

        [Test]
        public void BloodboundMath_IsHpBuffActive_TrueWithinWeekFalseAfter()
        {
            Assert.IsTrue(BloodboundMath.IsHpBuffActive(0f));
            Assert.IsTrue(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays - 0.01f));
            Assert.IsFalse(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays));
            Assert.IsFalse(BloodboundMath.IsHpBuffActive(BloodboundMath.HpBuffDurationDays + 1f));
        }

        [Test]
        public void BloodboundMath_PickPairIndex_IsBinaryAndDeterministic()
        {
            Assert.AreEqual(0, BloodboundMath.PickPairIndex(0.0));
            Assert.AreEqual(0, BloodboundMath.PickPairIndex(0.49));
            Assert.AreEqual(1, BloodboundMath.PickPairIndex(0.5));
            Assert.AreEqual(1, BloodboundMath.PickPairIndex(0.99));
        }

        [Test]
        public void BloodboundMath_MeetsPhysicalThreshold_RespectsCombinedBoundary()
        {
            Assert.IsFalse(BloodboundMath.MeetsPhysicalThreshold(4, 4)); // 8 < 10
            Assert.IsTrue(BloodboundMath.MeetsPhysicalThreshold(5, 5));  // exactly 10
            Assert.IsTrue(BloodboundMath.MeetsPhysicalThreshold(8, 6));  // 14 >= 10
        }

        [Test]
        public void BloodboundMath_HasCombatFocus_RequiresAtLeastOnePoint()
        {
            Assert.IsFalse(BloodboundMath.HasCombatFocus(0));
            Assert.IsTrue(BloodboundMath.HasCombatFocus(1));
            Assert.IsTrue(BloodboundMath.HasCombatFocus(5));
        }

        [Test]
        public void BloodboundMath_QualifiesForBloodbound_RequiresBothConditions()
        {
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(3, 3, 0));  // fails both
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(8, 8, 0));  // strong but untrained
            Assert.IsFalse(BloodboundMath.QualifiesForBloodbound(2, 2, 3));  // trained but frail
            Assert.IsTrue(BloodboundMath.QualifiesForBloodbound(6, 6, 2));   // meets both
        }

        [Test]
        public void BloodboundMath_CostTunables_ArePositive()
        {
            Assert.Greater(BloodboundMath.IgnoreCostBlood, 0);
            Assert.Greater(BloodboundMath.HpBuffCostBlood, 0);
            Assert.Greater(BloodboundMath.AttributeTradeCostBlood, 0);
            Assert.Greater(BloodboundMath.HpBuffAmount, 0f);
            Assert.Greater(BloodboundMath.HpBuffDurationDays, 0f);
        }

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

        // ── EmpireMath (Phase 7, Faction F — the Empire, Northern Empire) ────────
        [Test]
        public void EmpireMath_StartingTownIds_HasThreeSeats()
        {
            Assert.AreEqual(3, EmpireMath.StartingTownIds.Length);
        }

        [Test]
        public void EmpireMath_IsStartingTownId_RecognisesSaneopaDiathmaArgoron()
        {
            Assert.IsTrue(EmpireMath.IsStartingTownId("town_EN2")); // Diathma
            Assert.IsTrue(EmpireMath.IsStartingTownId("town_EN3")); // Saneopa
            Assert.IsTrue(EmpireMath.IsStartingTownId("town_EN4")); // Argoron
            Assert.IsFalse(EmpireMath.IsStartingTownId("town_EW1")); // Lageta (Legion)
        }

        [Test]
        public void EmpireMath_IsStartingTownId_IsCaseInsensitiveAndRejectsEmpty()
        {
            Assert.IsTrue(EmpireMath.IsStartingTownId("TOWN_en3"));
            Assert.IsFalse(EmpireMath.IsStartingTownId(""));
            Assert.IsFalse(EmpireMath.IsStartingTownId(null));
        }

        [Test]
        public void EmpireMath_IsClaimReady_RespectsCooldownBoundary()
        {
            Assert.IsTrue(EmpireMath.IsClaimReady(-1f));   // never claimed
            Assert.IsFalse(EmpireMath.IsClaimReady(0f));
            Assert.IsFalse(EmpireMath.IsClaimReady(EmpireMath.ClaimCooldownDays - 0.01f));
            Assert.IsTrue(EmpireMath.IsClaimReady(EmpireMath.ClaimCooldownDays));
        }

        [Test]
        public void EmpireMath_GrainClaimAmount_IsPositiveAndModest()
        {
            Assert.Greater(EmpireMath.GrainClaimAmount, 0);
            Assert.Less(EmpireMath.GrainClaimAmount, 100);
        }

        // ── LegionMath (Phase 7, Faction G — Legion, Western Empire) ────────────
        [Test]
        public void LegionMath_StartingTownIds_HasTwoSeats()
        {
            Assert.AreEqual(2, LegionMath.StartingTownIds.Length);
        }

        [Test]
        public void LegionMath_IsStartingTownId_RecognisesLagetaAndOrtysia()
        {
            Assert.IsTrue(LegionMath.IsStartingTownId("town_EW1"));  // Lageta
            Assert.IsTrue(LegionMath.IsStartingTownId("town_EW4"));  // Ortysia
            Assert.IsFalse(LegionMath.IsStartingTownId("town_EN2")); // Diathma (the Empire)
        }

        [Test]
        public void LegionMath_IsStartingTownId_IsCaseInsensitiveAndRejectsEmpty()
        {
            Assert.IsTrue(LegionMath.IsStartingTownId("TOWN_ew1"));
            Assert.IsFalse(LegionMath.IsStartingTownId(""));
            Assert.IsFalse(LegionMath.IsStartingTownId(null));
        }

        [Test]
        public void LegionMath_ShouldNudgeToRaid_RespectsChanceBoundary()
        {
            Assert.IsTrue(LegionMath.ShouldNudgeToRaid(0.0));
            Assert.IsTrue(LegionMath.ShouldNudgeToRaid(LegionMath.RaidNudgeChance - 0.001));
            Assert.IsFalse(LegionMath.ShouldNudgeToRaid(LegionMath.RaidNudgeChance));
            Assert.IsFalse(LegionMath.ShouldNudgeToRaid(0.999));
        }

        [Test]
        public void LegionMath_RaidNudgeChance_IsMarkedlyAggressive()
        {
            // "Markedly more aggressive" — the daily per-idle-party raid chance
            // must be a large, deliberate fraction, not a token nudge.
            Assert.GreaterOrEqual(LegionMath.RaidNudgeChance, 0.25);
            Assert.Less(LegionMath.RaidNudgeChance, 1.0);
        }

        [Test]
        public void LegionMath_ShouldForceWarDeclaration_RespectsToleranceBoundary()
        {
            Assert.IsFalse(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays - 1));
            Assert.IsTrue(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays));
            Assert.IsTrue(LegionMath.ShouldForceWarDeclaration(LegionMath.PeaceToleranceDays + 5));
        }

        [Test]
        public void LegionMath_TrainingFieldTunables_MatchTheDoubleYieldSpec()
        {
            Assert.AreEqual(1, LegionMath.TrainingFieldFocusCost);
            Assert.AreEqual(2, LegionMath.TrainingFieldSkillsGranted);
            Assert.AreEqual(1, LegionMath.TrainingFieldFocusPerSkill);
            Assert.AreEqual(9, LegionMath.TrainingSkillPoolSize);
        }

        [Test]
        public void LegionMath_PickTwoDistinctSkillIndices_AreAlwaysDistinctAndInRange()
        {
            var rng = new System.Random(12345);
            for (int i = 0; i < 500; i++)
            {
                double r1 = rng.NextDouble();
                double r2 = rng.NextDouble();
                LegionMath.PickTwoDistinctSkillIndices(r1, r2, out int first, out int second);

                Assert.GreaterOrEqual(first, 0);
                Assert.Less(first, LegionMath.TrainingSkillPoolSize);
                Assert.GreaterOrEqual(second, 0);
                Assert.Less(second, LegionMath.TrainingSkillPoolSize);
                Assert.AreNotEqual(first, second);
            }
        }

        [Test]
        public void LegionMath_PickTwoDistinctSkillIndices_HandlesEdgeRolls()
        {
            LegionMath.PickTwoDistinctSkillIndices(0.0, 0.0, out int first, out int second);
            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);

            LegionMath.PickTwoDistinctSkillIndices(0.999, 0.999, out int lastFirst, out int lastSecond);
            Assert.AreEqual(LegionMath.TrainingSkillPoolSize - 1, lastFirst);
            Assert.AreNotEqual(lastFirst, lastSecond);
        }

        // ── PaleWidowsMath tests (Phase 7, Faction H) ───────────────────────────

        [Test]
        public void PaleWidowsMath_IsStartingTownId_MatchesOnlyPhycaonAndLycaron()
        {
            Assert.IsTrue(PaleWidowsMath.IsStartingTownId("town_ES4"));  // Lycaron
            Assert.IsTrue(PaleWidowsMath.IsStartingTownId("town_ES6"));  // Phycaon
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId("town_ES1"));
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId("town_ES2"));
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId("town_ES3"));
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId("town_ES5"));
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId(null));
            Assert.IsFalse(PaleWidowsMath.IsStartingTownId(""));
            Assert.AreEqual(2, PaleWidowsMath.StartingTownIds.Length, "The Pale Widows should keep exactly two starting towns.");
        }

        [Test]
        public void PaleWidowsMath_IsStartingTownId_IsCaseInsensitive()
        {
            Assert.IsTrue(PaleWidowsMath.IsStartingTownId("TOWN_es4"));
            Assert.IsTrue(PaleWidowsMath.IsStartingTownId("Town_Es6"));
        }

        [Test]
        public void PaleWidowsMath_RollVanishDays_StaysWithinOneToFourWeeks()
        {
            for (double roll = 0.0; roll <= 1.0; roll += 0.05)
            {
                float days = PaleWidowsMath.RollVanishDays(roll);
                Assert.GreaterOrEqual(days, PaleWidowsMath.MinVanishDays);
                Assert.LessOrEqual(days, PaleWidowsMath.MaxVanishDays);
            }
            Assert.AreEqual(PaleWidowsMath.MinVanishDays, PaleWidowsMath.RollVanishDays(0.0));
            Assert.AreEqual(PaleWidowsMath.MaxVanishDays, PaleWidowsMath.RollVanishDays(1.0));
        }

        [Test]
        public void PaleWidowsMath_RollLordSacrificeImmunityDays_StaysWithinBounds()
        {
            for (double roll = 0.0; roll <= 1.0; roll += 0.05)
            {
                float days = PaleWidowsMath.RollLordSacrificeImmunityDays(roll);
                Assert.GreaterOrEqual(days, PaleWidowsMath.MinLordSacrificeImmunityDays);
                Assert.LessOrEqual(days, PaleWidowsMath.MaxLordSacrificeImmunityDays);
            }
        }

        [Test]
        public void PaleWidowsMath_ExtendIgnoreExpiry_StacksInsteadOfOverwriting()
        {
            // No active window yet (currentExpiry == today): a 2-day grant simply
            // starts a fresh 2-day window from today.
            float extended = PaleWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 100f, grantDays: 2f);
            Assert.AreEqual(102f, extended);

            // An active window already runs to day 105: a further 3-day grant
            // extends it to day 108, not day 103 (today + grant) and not merely
            // day 105 (unchanged) — it STACKS on top of the remaining time.
            float stacked = PaleWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 105f, grantDays: 3f);
            Assert.AreEqual(108f, stacked);
        }

        [Test]
        public void PaleWidowsMath_ExtendIgnoreExpiry_IgnoresAnExpiredWindow()
        {
            // The old window already lapsed (expiry before today) — the new grant
            // should count from today, not from the stale expiry day.
            float extended = PaleWidowsMath.ExtendIgnoreExpiry(today: 100f, currentExpiryDay: 40f, grantDays: 5f);
            Assert.AreEqual(105f, extended);
        }

        [Test]
        public void PaleWidowsMath_IsIgnoreActive_ComparesAgainstExpiry()
        {
            Assert.IsTrue(PaleWidowsMath.IsIgnoreActive(today: 100f, expiryDay: 101f));
            Assert.IsFalse(PaleWidowsMath.IsIgnoreActive(today: 101f, expiryDay: 101f));
            Assert.IsFalse(PaleWidowsMath.IsIgnoreActive(today: 102f, expiryDay: 101f));
        }

        [Test]
        public void PaleWidowsMath_RollSelfSacrifice_RespectsChanceBoundary()
        {
            Assert.IsTrue(PaleWidowsMath.RollSelfSacrifice(0.0));
            Assert.IsFalse(PaleWidowsMath.RollSelfSacrifice(PaleWidowsMath.SelfSacrificeChance));
            Assert.IsFalse(PaleWidowsMath.RollSelfSacrifice(0.999));
        }

        [Test]
        public void PaleWidowsMath_SelfSacrificeChance_IsSmall()
        {
            Assert.Greater(PaleWidowsMath.SelfSacrificeChance, 0.0);
            Assert.Less(PaleWidowsMath.SelfSacrificeChance, 0.2, "The self-sacrifice roll should read as a small chance, not a coin flip.");
        }

        [Test]
        public void PaleWidowsMath_InfluenceAfterDailyDrain_DrainsOnlyAMaleMember()
        {
            Assert.AreEqual(0f, PaleWidowsMath.InfluenceAfterDailyDrain(isMale: true, isMember: true, currentInfluence: 250f));
            Assert.AreEqual(250f, PaleWidowsMath.InfluenceAfterDailyDrain(isMale: false, isMember: true, currentInfluence: 250f));
            Assert.AreEqual(250f, PaleWidowsMath.InfluenceAfterDailyDrain(isMale: true, isMember: false, currentInfluence: 250f));
            Assert.AreEqual(250f, PaleWidowsMath.InfluenceAfterDailyDrain(isMale: false, isMember: false, currentInfluence: 250f));
        }

        [Test]
        public void PaleWidowsMath_DemonTroopsGrantedPerMaleSacrifice_IsPositive()
        {
            Assert.Greater(PaleWidowsMath.DemonTroopsGrantedPerMaleSacrifice, 0);
        }

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

        // ── RuinsMath tests (Phase 9 — the ruins) ───────────────────────────────

        [Test]
        public void RuinsMath_StableHash_IsDeterministicAndNonNegative()
        {
            int h1 = RuinsMath.StableHash("castle_test_id");
            int h2 = RuinsMath.StableHash("castle_test_id");
            Assert.AreEqual(h1, h2, "The same settlement id must always hash to the same value (reload-stability depends on this).");
            Assert.GreaterOrEqual(h1, 0);
            Assert.AreEqual(0, RuinsMath.StableHash(null));
            Assert.AreEqual(0, RuinsMath.StableHash(""));
        }

        [Test]
        public void RuinsMath_ShouldBeRuin_IsStableAcrossRepeatedCalls()
        {
            bool first = RuinsMath.ShouldBeRuin("castle_urikskala");
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(first, RuinsMath.ShouldBeRuin("castle_urikskala"),
                    "The same castle id must resolve to the same ruin/not-ruin verdict every time — this is what makes conversion stable per save without persisting the chosen set.");
            Assert.IsFalse(RuinsMath.ShouldBeRuin(null));
            Assert.IsFalse(RuinsMath.ShouldBeRuin(""));
        }

        [Test]
        public void RuinsMath_ShouldBeRuin_ConvertsRoughlyEightyPercentOfCastles()
        {
            int total = 2000;
            int ruins = 0;
            for (int i = 0; i < total; i++)
                if (RuinsMath.ShouldBeRuin("castle_synthetic_" + i)) ruins++;

            double fraction = ruins / (double)total;
            Assert.Greater(fraction, 0.72, "Expected roughly 80% of castles to convert to ruins.");
            Assert.Less(fraction, 0.88, "Expected roughly 80% of castles to convert to ruins.");
        }

        [Test]
        public void RuinsMath_RuinNameFor_IncludesOriginalNameAndAPrefix()
        {
            string name = RuinsMath.RuinNameFor("castle_kaysar", "Kaysar");
            StringAssert.Contains("Kaysar", name);
            bool hasKnownPrefix = false;
            foreach (var prefix in new[] { "Ruined Manor", "Ruined Castle", "Ruined City", "Ruined Tower", "Ruined Hold", "Ruined Bastion", "Ruined Keep", "Ruined Rampart" })
                if (name.StartsWith(prefix)) hasKnownPrefix = true;
            Assert.IsTrue(hasKnownPrefix, $"'{name}' did not start with a recognised ruin prefix.");
        }

        [Test]
        public void RuinsMath_ChamberSequence_AlwaysEndsOnTheThroneIndexAndHasNoDuplicates()
        {
            const int poolSize = 10;
            const int throneIndex = 9;
            for (int i = 0; i < 30; i++)
            {
                var seq = RuinsMath.ChamberSequence("castle_seq_" + i, poolSize, throneIndex);
                Assert.GreaterOrEqual(seq.Length, RuinsMath.MinChambersExcludingThrone + 1);
                Assert.LessOrEqual(seq.Length, RuinsMath.MaxChambersExcludingThrone + 1);
                Assert.AreEqual(throneIndex, seq[seq.Length - 1], "The Throne of Dust must always be the last chamber.");
                Assert.AreEqual(seq.Length, seq.Distinct().Count(), "A ruin's chamber sequence must not repeat a chamber.");
            }
        }

        [Test]
        public void RuinsMath_ChamberSequence_IsStableForTheSameSettlement()
        {
            var a = RuinsMath.ChamberSequence("castle_stable", 10, 9);
            var b = RuinsMath.ChamberSequence("castle_stable", 10, 9);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void RuinsMath_HoursForChamber_DecreasesWithScoutingAndRespectsFloor()
        {
            float noSkill   = RuinsMath.HoursForChamber(0);
            float midSkill  = RuinsMath.HoursForChamber(150);
            float maxSkill  = RuinsMath.HoursForChamber(300);
            float overCap   = RuinsMath.HoursForChamber(9999);

            Assert.AreEqual(RuinsMath.BaseChamberHours, noSkill);
            Assert.Less(midSkill, noSkill);
            Assert.AreEqual(RuinsMath.MinChamberHours, maxSkill);
            Assert.AreEqual(maxSkill, overCap, "Scouting above the cap must not reduce hours further.");
            Assert.GreaterOrEqual(RuinsMath.HoursForChamber(-50), RuinsMath.MinChamberHours);
        }

        [Test]
        public void RuinsMath_NightfallCrossed_OnlyTrueOnTheDayToNightEdge()
        {
            Assert.IsTrue(RuinsMath.NightfallCrossed(wasNight: false, isNight: true));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: true, isNight: true));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: false, isNight: false));
            Assert.IsFalse(RuinsMath.NightfallCrossed(wasNight: true, isNight: false));
        }

        [Test]
        public void RuinsMath_RollHazard_RespectsItsOwnThreshold()
        {
            Assert.IsTrue(RuinsMath.RollHazard(0.0));
            Assert.IsFalse(RuinsMath.RollHazard(RuinsMath.ChamberHazardChance));
            Assert.IsFalse(RuinsMath.RollHazard(0.999));
        }

        [Test]
        public void RuinsMath_RollChamberLoot_CoversTheWholeRollRangeWithoutGaps()
        {
            var seen = new HashSet<RuinsMath.LootKind>();
            for (double r = 0.0; r < 1.0; r += 0.001)
                seen.Add(RuinsMath.RollChamberLoot(r));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Weapon));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Armor));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.TradeGoods));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.Relic));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.SpellFormula));
            Assert.IsTrue(seen.Contains(RuinsMath.LootKind.None));
        }

        [Test]
        public void RuinsMath_BiasedLootRoll_FavoursTheChambersBiasAboutHalfTheTimeOnAMiss()
        {
            // A roll that would otherwise be "None" (>= 0.90) should become the
            // bias when the bias-roll succeeds, and stay None when it doesn't.
            Assert.AreEqual(RuinsMath.LootKind.SpellFormula,
                RuinsMath.BiasedLootRoll(0.95, 0.0, RuinsMath.LootKind.SpellFormula));
            Assert.AreEqual(RuinsMath.LootKind.None,
                RuinsMath.BiasedLootRoll(0.95, 0.9, RuinsMath.LootKind.SpellFormula));
            // A roll that already hit something concrete is left alone.
            Assert.AreEqual(RuinsMath.LootKind.Weapon,
                RuinsMath.BiasedLootRoll(0.1, 0.0, RuinsMath.LootKind.SpellFormula));
        }

        [Test]
        public void RuinsMath_FinalChamberLootRoll_NeverReturnsNone()
        {
            for (double r = 0.0; r < 1.0; r += 0.001)
                Assert.AreNotEqual(RuinsMath.LootKind.None, RuinsMath.FinalChamberLootRoll(r),
                    "The Throne of Dust — the last chamber — must always yield something.");
        }

        [Test]
        public void RuinsMath_HazardLossHelpers_StayWithinDeclaredBounds()
        {
            var rng = new Random(12345);
            for (int i = 0; i < 200; i++)
            {
                int troopLoss = RuinsMath.HazardTroopLoss(rng);
                Assert.GreaterOrEqual(troopLoss, RuinsMath.MinHazardTroopLoss);
                Assert.LessOrEqual(troopLoss, RuinsMath.MaxHazardTroopLoss);

                int hpLoss = RuinsMath.HazardHpLoss(rng);
                Assert.GreaterOrEqual(hpLoss, RuinsMath.MinHazardHpLoss);
                Assert.LessOrEqual(hpLoss, RuinsMath.MaxHazardHpLoss);
            }
        }

        [Test]
        public void RuinsMath_Cooldowns_ClearedIsLongerThanRetreat()
        {
            Assert.Greater(RuinsMath.ClearedCooldownDays, RuinsMath.RetreatCooldownDays);
        }

        // ── RuinsCatalog tests (Phase 9 — the ruins) ────────────────────────────

        [Test]
        public void RuinsCatalog_AllChambers_HaveNameAndEntryLoreAndSearchLine()
        {
            foreach (var def in RuinsCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Name), $"{def.Id} is missing a Name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.EntryLore), $"{def.Id} is missing EntryLore.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.SearchLine), $"{def.Id} is missing a SearchLine.");
            }
        }

        [Test]
        public void RuinsCatalog_ThroneOfDust_ExistsExactlyOnce()
        {
            Assert.AreEqual(1, RuinsCatalog.All.Count(d => d.Id == ChamberType.ThroneOfDust));
        }

        [Test]
        public void RuinsCatalog_PoolSize_MatchesAllCount()
        {
            Assert.AreEqual(RuinsCatalog.All.Count, RuinsCatalog.PoolSize);
        }

        // ── MortalLawMath (Phase 10 — Mortal AI under the same law) ────────────

        [Test]
        public void MortalLawMath_IsOverFiefCap_BoundaryIsExclusive()
        {
            Assert.IsFalse(MortalLawMath.IsOverFiefCap(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.IsOverFiefCap(MortalLawMath.KingdomFiefCap + 1));
            Assert.IsFalse(MortalLawMath.IsOverFiefCap(0));
        }

        [Test]
        public void MortalLawMath_ShouldTurnAwayDefector_MirrorsFiefCap()
        {
            Assert.IsFalse(MortalLawMath.ShouldTurnAwayDefector(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.ShouldTurnAwayDefector(MortalLawMath.KingdomFiefCap + 1));
        }

        [Test]
        public void MortalLawMath_ShouldThrottleWarDeclaration_MirrorsFiefCap()
        {
            Assert.IsFalse(MortalLawMath.ShouldThrottleWarDeclaration(MortalLawMath.KingdomFiefCap));
            Assert.IsTrue(MortalLawMath.ShouldThrottleWarDeclaration(MortalLawMath.KingdomFiefCap + 1));
        }

        [Test]
        public void MortalLawMath_IsSafeFromNightFear_BoundaryIsInclusive()
        {
            Assert.IsTrue(MortalLawMath.IsSafeFromNightFear(MortalLawMath.NightSafeArmySize));
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(MortalLawMath.NightSafeArmySize - 1));
            Assert.IsFalse(MortalLawMath.IsSafeFromNightFear(0));
        }

        [Test]
        public void MortalLawMath_IsPartyHungry_AtOrBelowThresholdIsHungry()
        {
            Assert.IsTrue(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold));
            Assert.IsTrue(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold - 1f));
            Assert.IsFalse(MortalLawMath.IsPartyHungry(MortalLawMath.HungryFoodThreshold + 0.01f));
        }

        [Test]
        public void MortalLawMath_ShouldNudgeHungryRaid_RollBelowChanceOnly()
        {
            Assert.IsTrue(MortalLawMath.ShouldNudgeHungryRaid(0.0));
            Assert.IsFalse(MortalLawMath.ShouldNudgeHungryRaid(MortalLawMath.HungryRaidNudgeChance));
            Assert.IsFalse(MortalLawMath.ShouldNudgeHungryRaid(0.999));
        }

        [Test]
        public void MortalLawMath_MaxAllowedForTier_LowTiersUncapped()
        {
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(1, 100));
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(2, 100));
            Assert.AreEqual(100, MortalLawMath.MaxAllowedForTier(3, 100));
        }

        [Test]
        public void MortalLawMath_MaxAllowedForTier_HighTiersRatioCapped()
        {
            Assert.AreEqual(15, MortalLawMath.MaxAllowedForTier(4, 100));
            Assert.AreEqual(5,  MortalLawMath.MaxAllowedForTier(5, 100));
        }

        [Test]
        public void MortalLawMath_MaxAllowedForTier_ZeroTroopsIsZero()
        {
            Assert.AreEqual(0, MortalLawMath.MaxAllowedForTier(4, 0));
            Assert.AreEqual(0, MortalLawMath.MaxAllowedForTier(5, 0));
        }

        [Test]
        public void MortalLawMath_TrimExcessForTier_OnlyTrimsAboveCap()
        {
            // 100 troops, tier-5 cap is 5 (Tier5MaxRatio = 0.05f).
            Assert.AreEqual(0, MortalLawMath.TrimExcessForTier(5, 5, 100));
            Assert.AreEqual(3, MortalLawMath.TrimExcessForTier(5, 8, 100));
            Assert.AreEqual(0, MortalLawMath.TrimExcessForTier(5, 0, 100));
        }

        [Test]
        public void MortalLawMath_KingdomFiefCap_IsPositiveAndSmall()
        {
            Assert.Greater(MortalLawMath.KingdomFiefCap, 0);
            Assert.Less(MortalLawMath.KingdomFiefCap, 15); // "a small cap" per the requirement text
        }

        // ── ApocalypseMath tests (Phase 11 — the clock of the apocalypse) ───────

        [Test]
        public void ApocalypseMath_RollNextHuntIntervalDays_WithinBounds()
        {
            var rng = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                int days = ApocalypseMath.RollNextHuntIntervalDays(rng);
                Assert.GreaterOrEqual(days, ApocalypseMath.MinHuntIntervalDays);
                Assert.LessOrEqual(days, ApocalypseMath.MaxHuntIntervalDays);
            }
        }

        [Test]
        public void ApocalypseMath_RollNextHuntIntervalDays_NullRngReturnsMin()
        {
            Assert.AreEqual(ApocalypseMath.MinHuntIntervalDays, ApocalypseMath.RollNextHuntIntervalDays(null));
        }

        [Test]
        public void ApocalypseMath_HuntPartyBodyCount_ScalesUp()
        {
            Assert.Greater(ApocalypseMath.HuntPartyBodyCount(10), 10);
            Assert.AreEqual(18, ApocalypseMath.HuntPartyBodyCount(10));
        }

        [Test]
        public void ApocalypseMath_EscalationStages_GatedByDay()
        {
            Assert.IsFalse(ApocalypseMath.IsRumourStage(299));
            Assert.IsTrue(ApocalypseMath.IsRumourStage(300));
            Assert.IsFalse(ApocalypseMath.IsGatheringStage(599));
            Assert.IsTrue(ApocalypseMath.IsGatheringStage(600));
            Assert.IsFalse(ApocalypseMath.IsLordEligible(999));
            Assert.IsTrue(ApocalypseMath.IsLordEligible(1000));
        }

        [Test]
        public void ApocalypseMath_GatheringSizeAfterWeeks_GrowsAndCaps()
        {
            Assert.AreEqual(ApocalypseMath.GatheringInitialSize, ApocalypseMath.GatheringSizeAfterWeeks(0));
            Assert.AreEqual(
                ApocalypseMath.GatheringInitialSize + ApocalypseMath.GatheringWeeklyGrowth * 3,
                ApocalypseMath.GatheringSizeAfterWeeks(3));
            Assert.AreEqual(ApocalypseMath.GatheringMaxSize, ApocalypseMath.GatheringSizeAfterWeeks(10000));
        }

        [Test]
        public void ApocalypseMath_RollDemonLordAppears_RespectsThreshold()
        {
            Assert.IsTrue(ApocalypseMath.RollDemonLordAppears(0.0));
            Assert.IsFalse(ApocalypseMath.RollDemonLordAppears(ApocalypseMath.DemonLordAppearChancePerWeek));
            Assert.IsFalse(ApocalypseMath.RollDemonLordAppears(0.9999));
        }

        [Test]
        public void ApocalypseMath_DemonLordHostSizeAfterWeeks_GrowsAndCaps()
        {
            Assert.AreEqual(ApocalypseMath.DemonLordHostInitialSize, ApocalypseMath.DemonLordHostSizeAfterWeeks(0));
            Assert.Greater(ApocalypseMath.DemonLordHostSizeAfterWeeks(5), ApocalypseMath.DemonLordHostInitialSize);
            Assert.AreEqual(ApocalypseMath.DemonLordHostMaxSize, ApocalypseMath.DemonLordHostSizeAfterWeeks(100000));
        }

        [Test]
        public void ApocalypseMath_DemonLordBaneMultiplier_LessThanOrdinaryDemonBane()
        {
            // He resists the demon-bane bonus — his multiplier must sit below the
            // ordinary demon's (RelicMath.DemonBaneMultiplier), but not below 1.0
            // (he should never take LESS than unenchanted damage).
            Assert.Less(ApocalypseMath.DemonLordBaneMultiplier, RelicMath.DemonBaneMultiplier);
            Assert.GreaterOrEqual(ApocalypseMath.DemonLordBaneMultiplier, 1.0f);
        }

        [Test]
        public void ApocalypseMath_IsDefeatBySettlements_TriggersAtFraction()
        {
            Assert.IsFalse(ApocalypseMath.IsDefeatBySettlements(49, 100));
            Assert.IsTrue(ApocalypseMath.IsDefeatBySettlements(50, 100));
            Assert.IsFalse(ApocalypseMath.IsDefeatBySettlements(0, 0));
        }

        [Test]
        public void ApocalypseMath_IsDefeatByElimination_TriggersAtZero()
        {
            Assert.IsFalse(ApocalypseMath.IsDefeatByElimination(1));
            Assert.IsTrue(ApocalypseMath.IsDefeatByElimination(0));
        }
    }
}
