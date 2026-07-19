using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

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
    }
}
