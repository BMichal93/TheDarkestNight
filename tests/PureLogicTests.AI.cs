using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        [Test]
        public void AshenRecruitMath_PrisonerQualifies_TierGate()
        {
            Assert.IsTrue(AshenRecruitMath.PrisonerQualifies(prisonerTier: 3, requiredTier: 3));
            Assert.IsTrue(AshenRecruitMath.PrisonerQualifies(prisonerTier: 5, requiredTier: 3));
            Assert.IsFalse(AshenRecruitMath.PrisonerQualifies(prisonerTier: 2, requiredTier: 3));
        }
    }
}
