// =============================================================================
// THE DARKEST NIGHT — TeamSafety.cs
//
// Team.IsEnemyOf drops into native code (IMono_MBTeam::is_enemy) that, when
// EITHER team's TeamIndex is invalid, prints a "SCRIPT ERROR: is_enemy:
// other_team_index_invalid!" line and returns false. That print happens at the
// engine level — a C# try/catch around the call CANNOT suppress it, because no
// managed exception is thrown.
//
// In non-battle missions (town/tavern/backstreet walk-arounds) wandering agents
// can carry a non-null-but-invalid Team, so any of our per-tick combat loops
// that calls IsEnemyOf on every agent floods the error log — tens of megabytes
// in a few seconds — which stalls the frame (string-format + file I/O per call)
// until the watchdog force-dumps and kills the game. This was the "leave town
// and it crashes" report.
//
// The fix is to never call IsEnemyOf with an invalid team. Always route agent-
// team comparisons in per-tick loops through IsEnemyOfSafe.
// =============================================================================

using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    internal static class TeamSafety
    {
        // True only when both teams exist, are valid, and are enemies. Returns
        // false (never spams the native log) for any null/invalid team.
        public static bool IsEnemyOfSafe(this Team self, Team other)
        {
            if (self == null || other == null) return false;
            if (!self.IsValid || !other.IsValid) return false;
            return self.IsEnemyOf(other);
        }
    }
}
