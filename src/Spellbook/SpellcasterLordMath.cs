// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterLordMath.cs
//
// Pure tunables for Requirement 14 — roughly 15% of named lords/companions
// know 1-3 spells from the Phase 4 spellbook and cast them in battle. No
// TaleWorlds types here; SpellcasterLords.cs (the campaign/mission-facing
// half) is the only caller.
//
// v0.8.0 (issue 17) raised this from 7% to 15%: with the legacy unified-
// element NPC lord casters retired for new games (LegacyContent.
// LegacyNpcCastersEnabled), the Spellbook's own rare-caster lords are now the
// only lord-tier magic NPCs field, so battles need more of them to read as
// magic-populated at all.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class SpellcasterLordMath
    {
        public const float TargetFraction = 0.15f;

        public const int MinKnownSpells = 1;
        public const int MaxKnownSpells = 3;

        // How long a caster lord waits between spoken workings in battle.
        public const float CastCooldownSeconds = 18f;

        public static int TargetCasterCount(int eligiblePoolCount)
        {
            if (eligiblePoolCount <= 0) return 0;
            return Math.Max(0, (int)Math.Round(eligiblePoolCount * TargetFraction, MidpointRounding.AwayFromZero));
        }

        // roll must be in [0, 100). Weighted so a single spell is the common
        // case and a full three-spell repertoire is rare — a caster lord
        // should read as dangerous, not as a second player.
        public static int KnownSpellCount(int roll0To99)
        {
            if (roll0To99 < 0 || roll0To99 > 99)
                roll0To99 = ((roll0To99 % 100) + 100) % 100;
            if (roll0To99 < 55) return MinKnownSpells;
            if (roll0To99 < 85) return 2;
            return MaxKnownSpells;
        }
    }
}
