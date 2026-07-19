using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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


        [Test]
        public void MiracleMath_ConvictionScale_SumsVirtue_AndCaps()
        {
            Assert.AreEqual(1.0f,  MiracleMath.ConvictionScale(0),  1e-6f); // a fickle heart, thin flame
            Assert.AreEqual(1.0f,  MiracleMath.ConvictionScale(-3), 1e-6f); // guards negatives
            Assert.AreEqual(1.15f, MiracleMath.ConvictionScale(5),  1e-5f); // +3%/point
            Assert.AreEqual(1.30f, MiracleMath.ConvictionScale(10), 1e-5f); // total conviction, full pyre
            Assert.AreEqual(1.30f, MiracleMath.ConvictionScale(20), 1e-5f); // never beyond the cap
        }
    }
}
