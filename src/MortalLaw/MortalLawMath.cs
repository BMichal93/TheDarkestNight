// =============================================================================
// THE DARKEST NIGHT — MortalLaw/MortalLawMath.cs
//
// Pure numeric core of Phase 10 (Requirement 6) — "the NPCs live in the same
// nightmare, by the same rules." No TaleWorlds types (fully covered by
// PureLogicTests). Runtime plumbing — kingdom/party queries, movement orders,
// roster edits — lives in MortalLawCampaignBehavior.*.cs.
//
// Four sub-rules, each with its own section below:
//   a) No great kingdoms   — fief-count cap; over-cap kingdoms lose defectors
//                             and have their wars thrown back to peace.
//   b) Fear the night      — small/army-less parties shelter at dusk.
//   c) Fight for food      — hungry parties are nudged toward raiding.
//   d) Same rules as the player — tier-ratio caps trim NPC rosters toward the
//                             same "mostly rabble, a few elites" shape the
//                             player's own barter economy already forces.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class MortalLawMath
    {
        // ── a) No great kingdoms ────────────────────────────────────────────────
        // Phase 7 renamed the whole map down to eight kingdoms (see
        // MortalLawCampaignBehavior.KingdomIds for the StringId list). None of
        // them may re-form a vanilla-style empire: past this many fiefs (towns +
        // castles — Kingdom.Fiefs counts both), the kingdom's growth engine is
        // throttled. Small enough to bite well before any one kingdom could
        // swallow the map, large enough that a kingdom can still hold a real,
        // playable little realm.
        public const int KingdomFiefCap = 6;

        public static bool IsOverFiefCap(int fiefCount) => fiefCount > KingdomFiefCap;

        // A clan defecting INTO a kingdom that is already over the cap is turned
        // away (the join is reversed the same tick). Evaluated against the
        // destination kingdom's fief count at the moment of the join.
        public static bool ShouldTurnAwayDefector(int destinationFiefCount)
            => IsOverFiefCap(destinationFiefCount);

        // A kingdom that is already over the cap has its wars thrown back to
        // peace the moment they are declared — an overextended realm cannot
        // sustain another campaign of conquest. Same predicate, named for its
        // call site so the two concerns read distinctly at a glance.
        public static bool ShouldThrottleWarDeclaration(int kingdomFiefCount)
            => IsOverFiefCap(kingdomFiefCount);

        // ── b) Fear the night ────────────────────────────────────────────────────
        // A party's own troop count (or, if it is riding with an army, the whole
        // army's combined headcount) has to clear this bar before it is allowed
        // to keep moving after dusk. Below it, the party is redirected toward
        // the nearest friendly settlement and held there until dawn — reuses
        // DemonMath.IsNightHour/DuskHour/DawnHour for the actual dusk/dawn gate
        // so the two systems never drift out of sync on what "night" means.
        public const int NightSafeArmySize = 120;

        public static bool IsSafeFromNightFear(int effectiveManCount)
            => effectiveManCount >= NightSafeArmySize;

        // ── c) Fight for food ───────────────────────────────────────────────────
        // MobileParty.Food is vanilla's own running food balance (negative once
        // a party is eating into debt/starving). A party at or below this line
        // is hungry enough to raid for it. The daily roll below decides whether
        // a hungry, idle, at-war lord party gets nudged toward the nearest
        // hostile village this tick — mirrors LegionMath.RaidNudgeChance's
        // direct-order technique (no clean vanilla GameModel governs the raid
        // TRIGGER decision, only in-progress raid math).
        public const float HungryFoodThreshold = 0f;

        public static bool IsPartyHungry(float food) => food <= HungryFoodThreshold;

        public const double HungryRaidNudgeChance = 0.5;

        public static bool ShouldNudgeHungryRaid(double roll01) => roll01 < HungryRaidNudgeChance;

        // ── d) Same rules as the player ─────────────────────────────────────────
        // NPC rosters read like scarcity too: tiers 1-3 are left uncapped (any
        // army may be mostly rabble), but tier 4 and tier 5 troops are capped as
        // a fraction of the party's total headcount. A periodic pass trims any
        // excess down to the cap — see UnitsMath.cs for the player-facing
        // promotion toll this conceptually mirrors; a full NPC-side item-cost
        // simulation was scoped out as fragile/overkill (see the campaign
        // behavior header for the write-up) — trimming existing rosters toward
        // the same tier shape is the acceptance criterion ("enemy armies look as
        // ragged as yours"), and it is what this enforces.
        public const float Tier4MaxRatio = 0.15f;
        public const float Tier5MaxRatio = 0.05f;

        public static int MaxAllowedForTier(int tier, int totalTroops)
        {
            if (totalTroops <= 0) return 0;
            if (tier >= 5) return (int)Math.Floor(totalTroops * Tier5MaxRatio);
            if (tier == 4) return (int)Math.Floor(totalTroops * Tier4MaxRatio);
            return totalTroops; // tiers 1-3 are uncapped — "mostly low-tier" needs no ceiling
        }

        public static int TrimExcessForTier(int tier, int currentCount, int totalTroops)
        {
            int allowed = MaxAllowedForTier(tier, totalTroops);
            int excess = currentCount - allowed;
            return excess > 0 ? excess : 0;
        }
    }
}
