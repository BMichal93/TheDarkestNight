using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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
    }
}
