// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterTroopMath.cs
//
// Pure tunables for how rare the Hollow Choir (Requirement 15) is to meet or
// recruit. No TaleWorlds types; SpellcasterTroopBehavior.cs is the only
// caller.
// =============================================================================

namespace AshAndEmber
{
    public static class SpellcasterTroopMath
    {
        // Rolled once per eligible town, once per week. Deliberately far
        // rarer than PriestTroops' weekly top-up (which runs every week with
        // no roll at all) — a whole town may go a full campaign without ever
        // producing one.
        public const float WeeklySeedChance = 0.015f;

        public static bool RollSeeds(double roll0To1) => roll0To1 < WeeklySeedChance;
    }
}
