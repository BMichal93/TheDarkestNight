// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellbookMath.cs
//
// Pure numeric core of the spoken formulas: the fizzle/spellburn chance curve
// (Requirement 18) and the spellburn table's tunables. No TaleWorlds types —
// covered by tests/PureLogicTests.cs.
//
// ── Spellburn scaling ────────────────────────────────────────────────────
// A completed-but-wrong formula always fizzles; whether it also SPELLBURNS is
// a straight roll against BaseSpellburnChance (60%, per Requirement 18),
// reduced linearly by the caster's Intellect attribute — 1.5% per point,
// floored at MinSpellburnChance (10%, so even a towering intellect cannot
// make the Fire wholly safe to misspeak). Bannerlord's Intellect attribute
// runs roughly 0-10 for most builds (up to the high teens for a dedicated
// scholar), so the curve crosses the floor around Intellect ≈ 33 — in
// practice the floor is a safety net, not something most heroes reach.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class SpellbookMath
    {
        public const float BaseSpellburnChance = 0.60f;
        public const float ChancePerIntellect  = 0.015f;
        public const float MinSpellburnChance  = 0.10f;

        // The one-time cost to unlock casting at all (Requirement 13).
        public const int UnlockFocusCost = 1;

        public static float SpellburnChance(int intellect)
        {
            if (intellect < 0) intellect = 0;
            float chance = BaseSpellburnChance - intellect * ChancePerIntellect;
            return chance < MinSpellburnChance ? MinSpellburnChance : chance;
        }

        public static bool RollSpellburn(double roll01, int intellect)
            => roll01 < SpellburnChance(intellect);

        // ── The spellburn table ──────────────────────────────────────────────
        // Five listed by Requirement 18 plus three invented in the same spirit.
        public enum SpellburnKind
        {
            BurnSelf = 0,       // listed — 30 damage to the caster
            Immobilise = 1,     // listed — 30 seconds unable to move
            RandomCommand = 2,  // listed — a random order to your own units
            Explode = 3,        // listed — 20 damage to everyone nearby
            DemonAppears = 4,   // listed — a demon appears, switching sides
            WeaponSeal = 5,     // invented — the weapon-hand seizes, disarmed
            FalseNight = 6,     // invented — a false night falls over the field
            VoiceTears = 7,     // invented — your voice tears; party morale drops
        }

        private const int KindCount = 8;

        public static SpellburnKind RollKind(int roll0To7)
        {
            int idx = ((roll0To7 % KindCount) + KindCount) % KindCount;
            return (SpellburnKind)idx;
        }

        public static SpellburnKind RollKind(Random rng)
            => RollKind(rng?.Next(KindCount) ?? 0);

        // Tunables for the individual burns — kept here so the whole table's
        // numbers live in one pure, tested place.
        public const float BurnSelfDamage        = 30f;
        public const float ImmobiliseSeconds      = 30f;
        public const float ExplodeDamage          = 20f;
        public const float ExplodeRadius          = 6f;
        public const float DemonAppearSeconds     = 70f;
        public const float DemonSideSwitchSeconds = 10f;
        public const float FalseNightSeconds      = 20f;
        public const float VoiceTearsMoraleLoss   = 15f;
    }
}
