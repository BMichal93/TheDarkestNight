// =============================================================================
// THE DARKEST NIGHT — ForeignMuster/ForeignMusterMath.cs
//
// Pure numeric core of the Foreign Muster (Legion / Western Empire, StringId
// "empire_w", towns): every Legion town offers ONE non-Empire main culture's
// tier-1 recruit each calendar week, picked deterministically from a hash of
// the week number and the town's own StringId — no save data is needed to
// know which culture a given town is offering this week; every session (and
// every save reload) recomputes the same answer for the same week.
//
// The five candidates are Bannerlord's other main playable cultures —
// vlandia, khuzait, battania, aserai, sturgia — confirmed against the shipped
// SandBoxCore/ModuleData/spcultures.xml (the six is_main_culture="true"
// entries are empire/aserai/sturgia/vlandia/battania/khuzait; nord/vakken/
// darshi are minor cultures, not offered here). "empire" (Legion's own
// culture) is never in the candidate array, so the modulo pick can never
// select it by construction — see PureLogicTests for an explicit assertion.
// =============================================================================

using System;
using System.Text;

namespace TheDarkestNight
{
    public static class ForeignMusterMath
    {
        // The other five main playable cultures. "empire" is deliberately
        // excluded — Legion never musters its own culture as a "foreign" one.
        public static readonly string[] NonEmpireCultures =
        {
            "vlandia", "khuzait", "battania", "aserai", "sturgia"
        };

        // Purchases allowed per town, per calendar week.
        public const int WeeklyPurchaseCap = 10;

        // ── Weekly culture pick ──────────────────────────────────────────────
        // Deterministic: the same (weekNumber, townStringId) pair always picks
        // the same index. Stable within a week (weekNumber is constant for
        // seven in-game days) and changes when the week rolls over, because
        // weekNumber is mixed into the hash. Always in
        // [0, NonEmpireCultures.Length).
        public static int PickCultureIndex(long weekNumber, string townStringId)
        {
            ulong hash = StableHash(weekNumber.ToString() + "|" + (townStringId ?? string.Empty));
            return (int)(hash % (ulong)NonEmpireCultures.Length);
        }

        public static string PickCulture(long weekNumber, string townStringId)
            => NonEmpireCultures[PickCultureIndex(weekNumber, townStringId)];

        // FNV-1a 64-bit. Deliberately NOT string.GetHashCode() — .NET
        // randomizes that per process (hash-flood mitigation), which would
        // make the weekly pick different every time the game launches. This
        // hash is stable across processes, platforms and .NET versions.
        internal static ulong StableHash(string s)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;
            byte[] bytes = Encoding.UTF8.GetBytes(s ?? string.Empty);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }
            return hash;
        }

        public static bool HasPurchasesRemaining(int purchasedThisWeek) => purchasedThisWeek < WeeklyPurchaseCap;
    }
}
