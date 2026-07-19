using System;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── EmpireMath (Phase 7, Faction F — the Empire, Northern Empire) ────────
        [Test]
        public void EmpireMath_StartingTownIds_HasSevenSeats()
        {
            // 3 home seats (Diathma/Saneopa/Argoron) + 4 deliberate border
            // grabs (castle_B5, castle_B2, Seonon, Rovalt) — see EmpireMath.cs.
            Assert.AreEqual(7, EmpireMath.StartingTownIds.Length);
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


        [Test]
        public void EmpireMath_RollSchemeIntervalDays_StaysWithinConfiguredRange()
        {
            var rng = new Random(5);
            for (int i = 0; i < 100; i++)
            {
                int days = EmpireMath.RollSchemeIntervalDays(rng);
                Assert.GreaterOrEqual(days, EmpireMath.SchemeIntervalMinDays);
                Assert.LessOrEqual(days, EmpireMath.SchemeIntervalMaxDays);
            }
        }
    }
}
