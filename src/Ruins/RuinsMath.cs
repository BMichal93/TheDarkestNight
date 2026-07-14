// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsMath.cs
//
// Pure numeric core of Phase 9 (Requirement 12). No TaleWorlds types — fully
// covered by PureLogicTests. Mirrors the shape of AshenRuinMath.cs but for a
// different mechanic entirely: which castles become ruins, what chambers a
// given ruin holds, how long each chamber takes to search (Scouting-scaled),
// and what a chamber's search turns up.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace AshAndEmber
{
    public static class RuinsMath
    {
        // ── Castle conversion (Requirement 12: "~80% of castles") ──────────────
        // Selection is a deterministic hash of the settlement's own StringId, so
        // the SAME set of castles reads as ruins on every load of the same save
        // with zero persistence — a fresh session simply re-derives the same
        // answer for the same id, the same way CityStateMath.CityStateKingdomId
        // re-derives a stable kingdom id from a clan id every tick.
        public const int ConversionPercentOfCastles = 80;

        public static int StableHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            unchecked
            {
                int hash = 23;
                foreach (char c in s) hash = hash * 31 + c;
                return hash & 0x7FFFFFFF;
            }
        }

        public static bool ShouldBeRuin(string settlementStringId)
        {
            if (string.IsNullOrEmpty(settlementStringId)) return false;
            return (StableHash(settlementStringId) % 100) < ConversionPercentOfCastles;
        }

        // ── Ruin naming ──────────────────────────────────────────────────────
        private static readonly string[] RuinPrefixes =
        {
            "Ruined Manor", "Ruined Castle", "Ruined City", "Ruined Tower",
            "Ruined Hold", "Ruined Bastion", "Ruined Keep", "Ruined Rampart",
        };

        public static string RuinPrefixFor(string settlementStringId)
        {
            if (string.IsNullOrEmpty(settlementStringId)) return RuinPrefixes[0];
            int h = StableHash("prefix_" + settlementStringId);
            return RuinPrefixes[h % RuinPrefixes.Length];
        }

        public static string RuinNameFor(string settlementStringId, string originalName)
            => $"{RuinPrefixFor(settlementStringId)} of {originalName}";

        // ── Chamber sequence ─────────────────────────────────────────────────
        // Every ruin gets 3-5 chambers, ending on the Throne of Dust (the pool's
        // final, always-last entry — see RuinsCatalog) so every crawl has the
        // same narrative shape: a handful of rooms, then the seat itself.
        public const int MinChambersExcludingThrone = 2;
        public const int MaxChambersExcludingThrone = 4;

        // `throneIndex` is RuinsCatalog's pool index of ThroneOfDust (passed in
        // so this stays pure — no dependency on the catalog's enum ordering).
        public static int[] ChamberSequence(string settlementStringId, int poolSize, int throneIndex)
        {
            if (poolSize <= 1) return new[] { 0 };

            int h = StableHash("count_" + settlementStringId);
            int span = MaxChambersExcludingThrone - MinChambersExcludingThrone + 1;
            int wantExcludingThrone = MinChambersExcludingThrone + (h % span);

            var pool = new List<int>();
            for (int i = 0; i < poolSize; i++)
                if (i != throneIndex) pool.Add(i);

            // Deterministic shuffle, seeded from the settlement id — same
            // settlement always yields the same ordered draw.
            var rng = new Random(StableHash("seq_" + settlementStringId));
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }

            int take = Math.Min(wantExcludingThrone, pool.Count);
            var sequence = pool.Take(take).ToList();
            sequence.Add(throneIndex);
            return sequence.ToArray();
        }

        // ── Progression pacing — Scouting-scaled wait between chambers ──────────
        // A poor scout (0 Scouting) takes the full base time to clear a chamber
        // and move to the next; a master scout (skill 300, the practical vanilla
        // cap) clears it in the floor time instead. Linear between the two so the
        // benefit is felt at every level of investment, not just at the cap.
        public const float BaseChamberHours = 8f;
        public const float MinChamberHours  = 2f;
        public const int   ScoutingSkillCap = 300;

        public static float HoursForChamber(int scoutingSkill)
        {
            int clamped = Math.Max(0, Math.Min(ScoutingSkillCap, scoutingSkill));
            float reduction = (BaseChamberHours - MinChamberHours) * clamped / (float)ScoutingSkillCap;
            return Math.Max(MinChamberHours, BaseChamberHours - reduction);
        }

        // ── Nightfall risk while waiting (Requirement 12) ───────────────────────
        // Pure edge-detector: campaign code polls DemonMath.IsNightHour(hour)
        // each tick of the wait and calls this with the previous/current reading.
        public static bool NightfallCrossed(bool wasNight, bool isNight) => !wasNight && isNight;

        // ── Danger while searching a chamber ────────────────────────────────────
        // A small, chamber-independent chance of a close call (a few troops or a
        // bite of HP) on top of the loot roll — ruins are never entirely safe,
        // just quiet.
        public const float ChamberHazardChance = 0.22f;
        public static bool RollHazard(double roll01) => roll01 < ChamberHazardChance;

        public const int MinHazardTroopLoss = 1;
        public const int MaxHazardTroopLoss = 4;
        public static int HazardTroopLoss(Random rng)
        {
            if (rng == null) return MinHazardTroopLoss;
            return MinHazardTroopLoss + rng.Next(MaxHazardTroopLoss - MinHazardTroopLoss + 1);
        }

        public const int MinHazardHpLoss = 5;
        public const int MaxHazardHpLoss = 18;
        public static int HazardHpLoss(Random rng)
        {
            if (rng == null) return MinHazardHpLoss;
            return MinHazardHpLoss + rng.Next(MaxHazardHpLoss - MinHazardHpLoss + 1);
        }

        // ── Loot (Requirement 12: weapons, armor, trade goods, relics, formulas) ─
        public enum LootKind { None, Weapon, Armor, TradeGoods, Relic, SpellFormula }

        // Base weights out of 100 (before a chamber's own bias nudges the odds
        // toward its favoured kind — see BiasedLootRoll). Relics and spell
        // formulas stay rarer than mundane finds; every chamber can still turn
        // up nothing at all.
        public static LootKind RollChamberLoot(double roll01)
        {
            if (roll01 < 0.24) return LootKind.Weapon;
            if (roll01 < 0.44) return LootKind.Armor;
            if (roll01 < 0.66) return LootKind.TradeGoods;
            if (roll01 < 0.80) return LootKind.Relic;
            if (roll01 < 0.90) return LootKind.SpellFormula;
            return LootKind.None;
        }

        // A chamber's declared bias replaces a plain "none" result with its
        // favoured kind about half the time, so the room's flavour text (an
        // armoury, a scholar's study...) usually pays off in what it promised
        // without making every chamber a guaranteed drop.
        public static LootKind BiasedLootRoll(double roll01, double biasRoll01, LootKind bias)
        {
            LootKind result = RollChamberLoot(roll01);
            if (result == LootKind.None && biasRoll01 < 0.5) return bias;
            return result;
        }

        // The final chamber (the Throne of Dust) always turns up something —
        // the last room of a dead house does not come up empty.
        public static LootKind FinalChamberLootRoll(double roll01)
        {
            LootKind result = RollChamberLoot(roll01);
            return result == LootKind.None ? LootKind.TradeGoods : result;
        }

        // ── Revisit cooldown ─────────────────────────────────────────────────
        // A ruin that was retreated from (not fully cleared) can be tried again
        // after a short cooldown; a fully cleared ruin is exhausted for much
        // longer (there's nothing left worth crawling back for in the same
        // season). Mirrors AshenRuinSystem's cooldown pattern.
        public const int RetreatCooldownDays = 7;
        public const int ClearedCooldownDays = 60;
    }
}
