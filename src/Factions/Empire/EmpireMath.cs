// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireMath.cs
//
// Pure numeric core of Phase 7, Faction F — the Northern Empire becomes
// simply "The Empire": heirs of the imperial throne, keepers of the old
// rites. No TaleWorlds types (fully covered by PureLogicTests). Mirrors the
// shape of TempleMath.cs / BloodboundMath.cs / HiveMath.cs.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class EmpireMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Empire keeps its three home seats — Saneopa (town_EN3), Diathma
        // (town_EN2) and Argoron (town_EN4) — plus the border ground
        // CampaignBehavior.Events.cs' ReassignImperialSettlements deliberately
        // hands it at new-game: castles B5/B2, Seonon (town_B4, Battania
        // border) and Rovalt (town_V9, Vlandia border). Every OTHER native
        // Northern Empire town (Myzea/EN5 and the rest) now falls out of scope
        // exactly like the five remnant factions' own holdings — the Empire is
        // one of Requirement 10's "eight desperate factions," not the
        // untouched vanilla imperial bloc. Verified against the shipped
        // SandBox/ModuleData/settlements.xml.
        public static readonly string[] StartingTownIds =
        {
            "town_EN2", "town_EN3", "town_EN4",   // Diathma, Saneopa, Argoron
            "castle_B5", "castle_B2",              // border castles (explicit grab)
            "town_B4",                             // Seonon
            "town_V9",                             // Rovalt
        };

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── The daily grain claim ────────────────────────────────────────────────
        // A modest, once-a-day handout — enough to matter under this mod's food
        // scarcity (Phase 2) without making an Empire town a free granary.
        public const int   GrainClaimAmount   = 15;
        public const float ClaimCooldownDays  = 1f;

        public static bool IsClaimReady(float daysSinceLastClaim)
            => daysSinceLastClaim < 0f || daysSinceLastClaim >= ClaimCooldownDays;

        // ── v0.8.0 (issue 11) — the Empire actually uses its scheme access ──────
        // A random interval, not a fixed cadence, so it doesn't read as clockwork.
        public const int SchemeIntervalMinDays = 10;
        public const int SchemeIntervalMaxDays = 14;

        public static int RollSchemeIntervalDays(Random rng)
        {
            if (rng == null) return SchemeIntervalMinDays;
            return SchemeIntervalMinDays + rng.Next(SchemeIntervalMaxDays - SchemeIntervalMinDays + 1);
        }
    }
}
