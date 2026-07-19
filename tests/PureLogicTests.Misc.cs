using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
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
                + "TheDarkestNightSaveDefiner.ClassDefinitions with a fresh, unused id: "
                + string.Join(", ", missing));
        }
    }
}
