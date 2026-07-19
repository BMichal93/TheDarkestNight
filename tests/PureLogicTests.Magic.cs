using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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
        public void ElementMagicMath_BloodRejuvenation_ScalesByTier()
        {
            Assert.AreEqual(25,  ElementMagicMath.BloodRejuvenationDays(1));
            Assert.AreEqual(150, ElementMagicMath.BloodRejuvenationDays(6));
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


        [Test]
        public void ElementUltimateMath_SpiritCostsMoreThanOtherUnbindings()
        {
            int spirit = ElementUltimateMath.UltimateAgingDays(hasNature: false, MagicElement.Spirit);
            int fire   = ElementUltimateMath.UltimateAgingDays(hasNature: false, MagicElement.Fire);
            Assert.Greater(spirit, fire);
        }


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


        [Test]
        public void ElementComboMath_LightningHasNoWallFallback()
        {
            // Lightning raises its own wall (Stormwall) — it is deliberately
            // absent from WallFallback's cases, which return the Fire default.
            Assert.AreEqual(MagicElement.Fire, ElementComboMath.WallFallback(MagicElement.Lightning));
        }


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
    }
}
