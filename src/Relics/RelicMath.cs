// =============================================================================
// THE DARKEST NIGHT — Relics/RelicMath.cs
//
// Pure numeric core for relics — rare magical items that never break. No
// TaleWorlds types (fully covered by PureLogicTests). Runtime wiring lives in
// RelicEffects.cs; the item/effect table lives in RelicCatalog.cs.
//
// ── Why relics are rebalanced WEAKER than their source ──────────────────────
// A Crystal is a one-shot: it burns down (CrystalMath.BurndownChance) and is
// gone. A Dark Gift is bound to one soul, permanent, and paid for with real
// aging/corruption cost (DarkGiftSystem). A relic pays neither price — it
// never breaks and it can, in principle, be handed to anyone who picks it up
// — so a relic-bound copy of either effect must cost meaningfully less power,
// or a relic quietly becomes strictly better than the thing it was copied
// from. The multipliers below are that tax.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class RelicMath
    {
        // ── Rebalance multipliers ────────────────────────────────────────────
        // A Crystal-sourced relic effect fires on every attack-button press,
        // forever, with no burndown risk — half the crystal's raw magnitude
        // keeps it clearly weaker than actually carrying that crystal.
        public const float CrystalRelicPowerMult = 0.50f;

        // A Dark Gift-sourced relic effect is passive and permanent-while-
        // carried, but per-ITEM rather than per-SOUL (drop it, lose it; hand
        // it off, someone else gets it) — trimmed harder than the crystal tax
        // since the source gift itself already costs the bearer nothing extra
        // once purchased.
        public const float DarkGiftRelicPowerMult = 0.40f;

        // ── Requirement 20 — demons fear the flame ───────────────────────────
        // Bonus multiplier applied to magic damage (element spells, spoken
        // formulas, relic procs) landing on a registered demon. Kept well
        // short of "instantly deletes anything" — the intent is "a caster
        // carves through demons visibly faster than a swordsman," not "a
        // single fireball ends the fight."
        public const float DemonBaneMultiplier = 1.5f;

        // ── Relic drops from demon battles (Requirement 19) ──────────────────
        // Small chance, per demon party the player personally defeats, that
        // one relic falls out of the wreckage. Kept low — relics are meant to
        // be rare and worth talking about.
        public const float RelicDropChancePerVictory = 0.05f;

        public static bool RollRelicDrop(double roll) => roll < RelicDropChancePerVictory;

        // Picks which catalog relic drops, uniform over however many exist.
        // Pure indexing helper so callers never need to touch Random directly
        // in a way that risks an out-of-range index.
        public static int PickRelicIndex(double roll01, int relicCount)
        {
            if (relicCount <= 0) return -1;
            int idx = (int)(roll01 * relicCount);
            if (idx < 0) idx = 0;
            if (idx >= relicCount) idx = relicCount - 1;
            return idx;
        }

        // ── Ruin loot hook (Phase 9 calls this; not built here) ──────────────
        // Ruins are meant to be a richer source of relics than a battlefield
        // scavenge — Phase 9's chamber-clear loot roll can call this directly
        // once it exists, without RelicMath needing to know anything about
        // chambers, Scouting, or wait-menus.
        public const float RuinBaseRelicChance = 0.15f;

        public static bool RollRuinLoot(double roll, float chance = RuinBaseRelicChance)
            => roll < chance;
    }
}
