using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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


        // ── SpellbookMath.PickDistinctIndices (Requirement 23, Step 6) ───────
        [Test]
        public void SpellbookMath_PickDistinctIndices_ReturnsRequestedCount()
        {
            var picks = SpellbookMath.PickDistinctIndices(10, 2, new Random(1));
            Assert.AreEqual(2, picks.Count);
        }


        [Test]
        public void SpellbookMath_PickDistinctIndices_NeverRepeatsAnIndex()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var picks = SpellbookMath.PickDistinctIndices(18, 2, new Random(seed));
                Assert.AreEqual(2, new HashSet<int>(picks).Count);
            }
        }


        [Test]
        public void SpellbookMath_PickDistinctIndices_StaysWithinPoolBounds()
        {
            var picks = SpellbookMath.PickDistinctIndices(5, 2, new Random(7));
            foreach (int i in picks)
            {
                Assert.GreaterOrEqual(i, 0);
                Assert.Less(i, 5);
            }
        }


        [Test]
        public void SpellbookMath_PickDistinctIndices_ClampsCountToPoolSize()
        {
            var picks = SpellbookMath.PickDistinctIndices(1, 2, new Random(3));
            Assert.AreEqual(1, picks.Count);
        }


        [Test]
        public void SpellbookMath_PickDistinctIndices_EmptyPool_ReturnsEmpty()
        {
            Assert.AreEqual(0, SpellbookMath.PickDistinctIndices(0, 2, new Random(3)).Count);
        }


        // ── SpellbookCatalog.QualifyingForArcaneStart (Requirement 23, Step 6) ──
        [Test]
        public void SpellbookCatalog_QualifyingForArcaneStart_AllAtMostSevenChars()
        {
            foreach (var d in SpellbookCatalog.QualifyingForArcaneStart)
                Assert.LessOrEqual(d.Formula.Length, 7);
        }


        [Test]
        public void SpellbookCatalog_QualifyingForArcaneStart_HasAtLeastTwoSpells()
        {
            Assert.GreaterOrEqual(SpellbookCatalog.QualifyingForArcaneStart.Count(), 2);
        }


        [Test]
        public void SpellbookCatalog_QualifyingForArcaneStart_IsStrictSubsetOfAll()
        {
            int qualifying = SpellbookCatalog.QualifyingForArcaneStart.Count();
            Assert.Greater(SpellbookCatalog.All.Count, qualifying);
            Assert.Greater(qualifying, 0);
        }


        // ── SpellcasterLordMath (Requirement 14) ─────────────────────────────
        [Test]
        public void SpellcasterLordMath_TargetCasterCount_IsRoughlyFifteenPercent()
        {
            Assert.AreEqual(15, SpellcasterLordMath.TargetCasterCount(100));
            Assert.AreEqual(30, SpellcasterLordMath.TargetCasterCount(200));
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


        // ── Rune magic — catalog invariants (RUNE_MAGIC_PLAN.md §2) ───────────

        [Test]
        public void RuneCatalog_Triplets_AreThreeMarks_OnlyUDLR_AllDistinct()
        {
            var seen = new HashSet<string>();
            foreach (var r in RuneCatalog.All)
            {
                Assert.AreEqual(RuneCatalog.RuneLength, r.Triplet.Length, $"{r.Name} triplet length");
                foreach (char c in r.Triplet)
                    Assert.IsTrue(c == 'U' || c == 'D' || c == 'L' || c == 'R', $"{r.Name} has non-UDLR mark");
                Assert.IsTrue(seen.Add(r.Triplet), $"{r.Name} triplet {r.Triplet} is a duplicate");
            }
        }


        [Test]
        public void RuneCatalog_Count_IsThirtySix_AndSpaceStaysSparse()
        {
            Assert.AreEqual(36, RuneCatalog.All.Length);
            // 36 of the 64 possible triplets are real — at least 40% of the space
            // stays permanently empty so misdrawn bindings stay dangerous.
            Assert.LessOrEqual(RuneCatalog.All.Length / 64.0, 0.60);
        }


        [Test]
        public void RuneCatalog_RoleCensus_FormsCappedAtEight_EffectsOutnumberForms()
        {
            int matter  = RuneCatalog.All.Count(r => r.Role == RuneRole.Matter);
            int forms   = RuneCatalog.All.Count(r => r.Role == RuneRole.Form);
            int manners = RuneCatalog.All.Count(r => r.Role == RuneRole.Manner);
            int codas   = RuneCatalog.All.Count(r => r.Role == RuneRole.Coda);
            Assert.AreEqual(5, matter);
            Assert.AreEqual(RuneCatalog.FormCount, forms);   // 8, capped forever
            Assert.AreEqual(8, manners);
            Assert.AreEqual(15, codas);
            // Effect-side (matter + codas) must outnumber forms 2:1.
            Assert.Greater(matter + codas, 2 * forms);
        }


        [Test]
        public void RuneCatalog_EveryFormRuneHasADistinctForm()
        {
            var forms = RuneCatalog.All.Where(r => r.Role == RuneRole.Form).Select(r => r.Form).ToList();
            Assert.IsFalse(forms.Contains(RuneForm.None));
            Assert.AreEqual(forms.Count, forms.Distinct().Count());
        }


        // ── Rune magic — resolver (RUNE_MAGIC_PLAN.md §3) ─────────────────────

        [Test]
        public void RuneSequence_AmplifyScale_MatchesCurve()
        {
            Assert.AreEqual(1.0f, RuneSequenceMath.AmplifyScale(1));
            Assert.AreEqual(1.75f, RuneSequenceMath.AmplifyScale(2));
            Assert.AreEqual(2.4f, RuneSequenceMath.AmplifyScale(3));
            Assert.AreEqual(3.0f, RuneSequenceMath.AmplifyScale(4));
            Assert.AreEqual(3.0f, RuneSequenceMath.AmplifyScale(9)); // capped
        }


        [Test]
        public void RuneSequence_Strain_ZeroUnderFourRunes_ThenGrows()
        {
            Assert.AreEqual(0f, RuneSequenceMath.StrainChance(1));
            Assert.AreEqual(0f, RuneSequenceMath.StrainChance(3));
            Assert.AreEqual(0.04f, RuneSequenceMath.StrainChance(4), 1e-5);
            Assert.AreEqual(0.16f, RuneSequenceMath.StrainChance(7), 1e-5);
        }


        [Test]
        public void RuneSequence_Chunk_ValidTriplets_Split_TrailingMarks_Malformed()
        {
            Assert.IsTrue(RuneSequenceMath.TryChunk("DDD", out var one, out _));
            Assert.AreEqual(1, one.Count);
            Assert.AreEqual(RuneId.Cinder, one[0]);

            Assert.IsTrue(RuneSequenceMath.TryChunk("DDDLLL", out var two, out _));
            Assert.AreEqual(2, two.Count);

            Assert.IsFalse(RuneSequenceMath.TryChunk("DDDD", out _, out _));   // trailing mark
            Assert.IsFalse(RuneSequenceMath.TryChunk("UUL", out _, out _));    // not a real rune
            Assert.IsFalse(RuneSequenceMath.TryChunk("", out _, out _));       // empty
        }


        [Test]
        public void RuneSequence_SingleElement_Resolves()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Cinder });
            Assert.IsFalse(r.Malformed);
            Assert.AreEqual(MatterKind.Single, r.Matter);
            Assert.AreEqual(MagicElement.Fire, r.Element);
        }


        [Test]
        public void RuneSequence_TwoElements_Fuse()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Cinder, RuneId.Tide }); // Fire+Water
            Assert.IsFalse(r.Malformed);
            Assert.AreEqual(MatterKind.Fusion, r.Matter);
            Assert.AreEqual(MagicElement.Fog, r.Element);
        }


        [Test]
        public void RuneSequence_WyrdPlusElement_IsCommand()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Wyrd, RuneId.Cinder });
            Assert.IsFalse(r.Malformed);
            Assert.AreEqual(MatterKind.Command, r.Matter);
        }


        [Test]
        public void RuneSequence_ThreeElements_AreATriad()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Cinder, RuneId.Gale, RuneId.Tide });
            Assert.IsFalse(r.Malformed);
            Assert.AreEqual(MatterKind.Triad, r.Matter);
            Assert.AreEqual("the Tempest", r.TriadName);
        }


        [Test]
        public void RuneSequence_FourElements_AreTheUnboundWeave()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Cinder, RuneId.Gale, RuneId.Stone, RuneId.Tide });
            Assert.IsFalse(r.Malformed);
            Assert.AreEqual(MatterKind.Unbound, r.Matter);
        }


        [Test]
        public void RuneSequence_WyrdWithTwoOtherElements_IsMalformed()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Wyrd, RuneId.Cinder, RuneId.Tide });
            Assert.IsTrue(r.Malformed);
        }


        [Test]
        public void RuneSequence_TwoForms_IsMalformed()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.LongMark, RuneId.Bar });
            Assert.IsTrue(r.Malformed);
        }


        [Test]
        public void RuneSequence_DeclaredContradictions_AreMalformed()
        {
            Assert.IsTrue(RuneSequenceMath.Resolve(new[] { RuneId.NightMark, RuneId.Lamp }).Malformed);
            Assert.IsTrue(RuneSequenceMath.Resolve(new[] { RuneId.Mirror, RuneId.Gift }).Malformed);
            Assert.IsTrue(RuneSequenceMath.Resolve(new[] { RuneId.Still, RuneId.Vigil }).Malformed);
        }


        [Test]
        public void RuneSequence_CommandOrUnbound_TakeNoForm()
        {
            // command (Wyrd+Cinder) + Bar
            Assert.IsTrue(RuneSequenceMath.Resolve(new[] { RuneId.Wyrd, RuneId.Cinder, RuneId.Bar }).Malformed);
            // unbound (all four) + Bar
            Assert.IsTrue(RuneSequenceMath.Resolve(
                new[] { RuneId.Cinder, RuneId.Gale, RuneId.Stone, RuneId.Tide, RuneId.Bar }).Malformed);
        }


        [Test]
        public void RuneSequence_ElementPlusForm_Resolves_AndAmplifies()
        {
            var fireball = RuneSequenceMath.Resolve(new[] { RuneId.Cinder, RuneId.LongMark });
            Assert.IsFalse(fireball.Malformed);
            Assert.AreEqual(RuneForm.LongMark, fireball.Form);
            StringAssert.Contains("Fire", fireball.Name);

            var twiceFire = RuneSequenceMath.Resolve(new[] { RuneId.Cinder, RuneId.Cinder });
            Assert.IsFalse(twiceFire.Malformed);
            Assert.AreEqual(1.75f, twiceFire.Power, 1e-5);
        }


        [Test]
        public void RuneSequence_CompletenessMatrix_EveryMatterByForm_NoSilentHole()
        {
            Assert.AreEqual(20, _matterStates.Length, "there must be exactly 20 matter-states");
            Assert.AreEqual(RuneCatalog.FormCount, _formRunes.Length);

            foreach (var (name, matter, formRejected) in _matterStates)
            {
                // formless
                var bare = RuneSequenceMath.Resolve(matter);
                Assert.IsFalse(bare.Malformed, $"{name} (formless) should resolve");
                Assert.IsNotEmpty(bare.Name ?? "", $"{name} (formless) has no composed name");

                foreach (var form in _formRunes)
                {
                    var seq = matter.Concat(new[] { form }).ToList();
                    var r = RuneSequenceMath.Resolve(seq);
                    if (formRejected)
                        Assert.IsTrue(r.Malformed && !string.IsNullOrEmpty(r.Reason),
                            $"{name} + {form} must be a DECLARED contradiction");
                    else
                    {
                        Assert.IsFalse(r.Malformed, $"{name} + {form} should resolve, not fizzle");
                        Assert.IsNotEmpty(r.Name ?? "", $"{name} + {form} has no composed name");
                    }
                }
            }
        }


        [Test]
        public void RuneSequence_CompletenessMatrix_EveryMannerByForm_NoSilentHole()
        {
            // A single manner laid on a valid element+form binding must never be a
            // silent hole: it either applies or is inert, but the working still fires.
            RuneId[] manners = { RuneId.Echo, RuneId.Chain, RuneId.Vigil, RuneId.Price,
                                 RuneId.NightMark, RuneId.Mirror, RuneId.Still, RuneId.Gift };
            foreach (var manner in manners)
                foreach (var form in _formRunes)
                {
                    var seq = new List<RuneId> { RuneId.Cinder, form, manner };
                    var r = RuneSequenceMath.Resolve(seq);
                    // Fire + form + one manner is a legal binding (no declared
                    // manner-manner contradiction present), so it must resolve.
                    Assert.IsFalse(r.Malformed, $"Fire + {form} + {manner} should resolve");
                }
        }


        [Test]
        public void RuneSequence_PureManners_AreHarmlessFizzle_NotABurn()
        {
            var r = RuneSequenceMath.Resolve(new[] { RuneId.Echo });
            Assert.IsTrue(r.Malformed);
            Assert.IsTrue(r.Harmless);   // a real rune that composes nothing — no burn
        }


        [Test]
        public void RuneSequence_GenuineMisbindings_AreNotHarmless()
        {
            // contradictions and weave-tears MUST still carry a burn risk
            Assert.IsFalse(RuneSequenceMath.Resolve(new[] { RuneId.NightMark, RuneId.Lamp }).Harmless);
            Assert.IsFalse(RuneSequenceMath.Resolve(new[] { RuneId.Wyrd, RuneId.Cinder, RuneId.Tide }).Harmless);
            Assert.IsFalse(RuneSequenceMath.Resolve(new[] { RuneId.LongMark, RuneId.Bar }).Harmless);
        }


        // ── Rune magic — starter pair + migration (RUNE_MAGIC_PLAN.md §7) ─────

        [Test]
        public void RuneCatalog_PickStarterPair_FirstIsElement_SecondEligible_Distinct()
        {
            var rng = new Random(12345);
            for (int i = 0; i < 200; i++)
            {
                var (first, second) = RuneCatalog.PickStarterPair(rng);
                Assert.Contains(first, RuneCatalog.ElementRunes);
                Assert.Contains(second, RuneCatalog.StarterEligibleSecond);
                Assert.AreNotEqual(first, second);
                // Both castable day one: element alone works; element+element fuses;
                // element+form shapes — so the pair never resolves malformed together.
                var r = RuneSequenceMath.Resolve(new[] { first, second });
                Assert.IsFalse(r.Malformed, $"starter pair {first}+{second} was malformed");
            }
        }


        [Test]
        public void SpellbookMath_StrainAfterIntellect_ReducesAndFloorsAtZero()
        {
            Assert.AreEqual(0.16f, SpellbookMath.StrainAfterIntellect(0.16f, 0), 1e-5);
            Assert.AreEqual(0.13f, SpellbookMath.StrainAfterIntellect(0.16f, 2), 1e-5); // -0.015*2
            Assert.AreEqual(0f, SpellbookMath.StrainAfterIntellect(0.04f, 20));         // floored
        }


        [Test]
        public void SpellbookMath_NpcSpellburn_TemperamentAndFloor()
        {
            // base 12% at Int 0
            Assert.AreEqual(0.12f, SpellbookMath.NpcSpellburnChance(0, false, false), 1e-5);
            // calculating halves, impulsive raises
            Assert.Less(SpellbookMath.NpcSpellburnChance(0, true, false), 0.12f);
            Assert.Greater(SpellbookMath.NpcSpellburnChance(0, false, true), 0.12f);
            // floored at 2% even for a towering intellect
            Assert.AreEqual(0.02f, SpellbookMath.NpcSpellburnChance(50, true, false), 1e-5);
        }


        [Test]
        public void RuneCatalog_EveryLegacySpell_MapsToKnownRunes()
        {
            var valid = new HashSet<RuneId>(RuneCatalog.All.Select(r => r.Id));
            foreach (SpellId id in Enum.GetValues(typeof(SpellId)))
            {
                var runes = RuneCatalog.RunesForLegacySpell(id);
                Assert.IsNotNull(runes);
                Assert.IsNotEmpty(runes, $"{id} maps to no runes");
                foreach (var rune in runes)
                    Assert.IsTrue(valid.Contains(rune), $"{id} maps to unknown rune {rune}");
            }
        }
    }
}
