using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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
    }
}
