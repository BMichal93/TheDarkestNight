// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionMath.cs
//
// Pure numeric core of Phase 7, Faction G — the Western Empire becomes
// "Legion": militarists who steal rather than produce. No TaleWorlds types
// (fully covered by PureLogicTests). Mirrors the shape of EmpireMath.cs /
// TempleMath.cs / BloodboundMath.cs.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class LegionMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Legion keeps exactly two seats: Lageta (town_EW1) and Ortysia
        // (town_EW4) — verified against the shipped SandBox/ModuleData/
        // settlements.xml ("{=Settlements.Settlement.name.town_EW1}Lageta" /
        // "town_EW4}Ortysia").
        public static readonly string[] StartingTownIds = { "town_EW1", "town_EW4" };

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Aggression weighting ─────────────────────────────────────────────────
        // "Might makes right; they steal rather than produce" — the Legion must
        // read as markedly more aggressive than every other kingdom. No clean
        // GameModel exposes raid/attack decision weighting (RaidModel only
        // covers loot/damage math once a raid is already under way), so the
        // Legion is nudged directly: every idle Legion lord party rolls this
        // chance, once per day, to be pointed at the nearest enemy settlement —
        // the same direct-order technique DemonSpawnCampaignBehavior already
        // uses for the Night Tide's settlement assaults. A 35% daily chance per
        // idle party is a large, deliberate multiplier over the vanilla AI's own
        // (much rarer, opportunistic) raid decisions — the Legion should visibly
        // raid far more often than any other faction's lords.
        public const double RaidNudgeChance = 0.35;

        public static bool ShouldNudgeToRaid(double roll01) => roll01 < RaidNudgeChance;

        // If the Legion goes this many consecutive days completely at peace
        // (with every living kingdom), it is forced into a war against its
        // nearest neighbour — a militarist realm does not stay idle for long.
        public const int PeaceToleranceDays = 8;

        public static bool ShouldForceWarDeclaration(int consecutivePeaceDays)
            => consecutivePeaceDays >= PeaceToleranceDays;

        // ── Training fields ───────────────────────────────────────────────────
        // Spend 1 focus point → gain 1 focus point in each of TWO random skills
        // drawn from the Vigor / Control / Endurance attribute groups (a double
        // yield, randomly placed). Skill grouping mirrors the vanilla character
        // sheet: Vigor = OneHanded, TwoHanded, Polearm, Bow (4); Control =
        // Crossbow, Throwing, Riding (3); Endurance = Athletics, Crafting (2).
        public const int TrainingFieldFocusCost      = 1;
        public const int TrainingFieldSkillsGranted  = 2;
        public const int TrainingFieldFocusPerSkill  = 1;

        public const int VigorSkillCount     = 4;
        public const int ControlSkillCount   = 3;
        public const int EnduranceSkillCount = 2;
        public const int TrainingSkillPoolSize = VigorSkillCount + ControlSkillCount + EnduranceSkillCount; // 9

        // Picks two DISTINCT indices in [0, TrainingSkillPoolSize) from two
        // independent uniform rolls in [0, 1). Pure and deterministic given the
        // rolls, so it is fully unit-testable without touching SkillObject.
        public static void PickTwoDistinctSkillIndices(double roll1, double roll2, out int first, out int second)
        {
            first = (int)(roll1 * TrainingSkillPoolSize);
            if (first < 0) first = 0;
            if (first >= TrainingSkillPoolSize) first = TrainingSkillPoolSize - 1;

            int secondPoolSize = TrainingSkillPoolSize - 1; // one slot removed (the first pick)
            int secondRaw = (int)(roll2 * secondPoolSize);
            if (secondRaw < 0) secondRaw = 0;
            if (secondRaw >= secondPoolSize) secondRaw = secondPoolSize - 1;

            // Skip over 'first' so the mapped index never collides with it.
            second = secondRaw >= first ? secondRaw + 1 : secondRaw;
        }
    }
}
