using System.Collections.Generic;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── ForeignMusterMath ───────────────────────────────────────────────
        [Test]
        public void ForeignMusterMath_PickCulture_IsStableWithinAWeek()
        {
            for (int i = 0; i < 20; i++)
            {
                string first  = ForeignMusterMath.PickCulture(500, "town_EW1");
                string second = ForeignMusterMath.PickCulture(500, "town_EW1");
                Assert.AreEqual(first, second);
            }
        }


        [Test]
        public void ForeignMusterMath_PickCulture_ChangesAcrossWeeks()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (long week = 0; week < 40; week++)
                seen.Add(ForeignMusterMath.PickCulture(week, "town_EW1"));

            // Five candidates, forty distinct weeks — extremely unlikely to
            // collapse onto a single culture unless the hash isn't mixing.
            Assert.Greater(seen.Count, 1);
        }


        [Test]
        public void ForeignMusterMath_PickCulture_NeverPicksEmpire()
        {
            for (long week = 0; week < 200; week++)
            {
                string culture = ForeignMusterMath.PickCulture(week, "town_EW4");
                Assert.AreNotEqual("empire", culture);
                CollectionAssert.Contains(ForeignMusterMath.NonEmpireCultures, culture);
            }
        }


        [Test]
        public void ForeignMusterMath_PickCulture_DiffersAcrossTownsInSameWeek()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 20; i++)
                seen.Add(ForeignMusterMath.PickCulture(500, "town_EW" + i));

            Assert.Greater(seen.Count, 1);
        }


        [Test]
        public void ForeignMusterMath_PickCultureIndex_AlwaysInBounds()
        {
            for (long week = 0; week < 100; week++)
            {
                int idx = ForeignMusterMath.PickCultureIndex(week, "town_EW1");
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, ForeignMusterMath.NonEmpireCultures.Length);
            }
        }


        [Test]
        public void ForeignMusterMath_HasPurchasesRemaining_RespectsCap()
        {
            Assert.IsTrue(ForeignMusterMath.HasPurchasesRemaining(0));
            Assert.IsTrue(ForeignMusterMath.HasPurchasesRemaining(ForeignMusterMath.WeeklyPurchaseCap - 1));
            Assert.IsFalse(ForeignMusterMath.HasPurchasesRemaining(ForeignMusterMath.WeeklyPurchaseCap));
            Assert.IsFalse(ForeignMusterMath.HasPurchasesRemaining(ForeignMusterMath.WeeklyPurchaseCap + 5));
        }
    }
}
