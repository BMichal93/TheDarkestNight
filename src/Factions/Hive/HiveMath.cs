// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveMath.cs
//
// Pure numeric core of Faction C — the Hive (formerly Battania). No
// TaleWorlds types (fully covered by PureLogicTests). Runtime behaviour —
// culture/kingdom renaming, dialogue, town-scoping, the elixir join ritual,
// the dosing/auto-leave tick, the free prisoner recruit, the death-succession
// hook, and the battle downsides — lives in HiveCulture.cs, HiveDialogue.cs,
// HiveSettlements.cs, HiveBattleEffects.cs and HiveCampaignBehavior(.Menus).cs.
// =============================================================================

using System;
using System.Collections.Generic;

namespace AshAndEmber
{
    public static class HiveMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Hive keeps exactly two seats: Marunath (town_B1) and Car Banseth
        // (town_B3). Every other town/castle Battania holds falls out of the
        // kingdom's scope — its clan is simply ejected (mirrors
        // WolfBrothersMath.StartingTownIds / TowerMath.StartingTownIds exactly).
        public static readonly string[] StartingTownIds = { "town_B1", "town_B3" }; // Marunath, Car Banseth

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── The dose ─────────────────────────────────────────────────────────
        // Being Integrated is not a rank, it is an infection kept willingly. The
        // fungus asks for a fresh dose on a fixed rhythm; missing it does not cut
        // the bond immediately — there is a grace window before the network lets
        // go — but past that window the bond simply ends, exactly as if the
        // Integrated had chosen to leave outright.
        public const float DoseIntervalDays = 7f;  // how often a dose is "due"
        public const float DoseGraceDays    = 3f;  // slack after the due date before the bond breaks

        public static float DoseWindowDays => DoseIntervalDays + DoseGraceDays;

        // daysSinceLastDose: CampaignTime.Now.ToDays - the day the last dose was taken.
        public static bool IsBondBroken(float daysSinceLastDose) => daysSinceLastDose > DoseWindowDays;

        // How many days remain before the bond breaks (never negative) — used to
        // warn the player as the window closes.
        public static float DaysUntilBondBreaks(float daysSinceLastDose)
        {
            float remaining = DoseWindowDays - daysSinceLastDose;
            return remaining < 0f ? 0f : remaining;
        }

        // ── Free prisoner recruitment ────────────────────────────────────────
        // "We absorb everything" — no gold or item cost, but a captive still has
        // to be walked into the network one body at a time (mirrors the existing
        // "spend one qualifying unit per click" menu shape used by the pack's
        // larder and the Tower's transmutation).
        public const int FreeRecruitPerClick = 1;

        // ── Battle downsides ──────────────────────────────────────────────────
        // Hallucinatory colour inversion and the network's other voices drowning
        // out your own commands are both rare, intermittent nuisances — never a
        // guaranteed occurrence, never every fight. Checked on a slow interval so
        // a single battle isn't guaranteed to see either, but a long campaign of
        // fights will.
        public const float DownsideCheckIntervalSeconds = 20f;
        public const float ColourInversionChance         = 0.08f; // per check, while Integrated and in a mission
        public const float WillSuppressionChance          = 0.06f; // per check, while Integrated and in a mission
        public const float ColourInversionDurationSeconds = 6f;

        public static bool RollColourInversion(double roll01) => roll01 < ColourInversionChance;
        public static bool RollWillSuppression(double roll01) => roll01 < WillSuppressionChance;

        // ── Death succession (Requirement — Hive-specific) ──────────────────
        // Picks a deterministic index into a candidate list of living Hive lords
        // given an external random draw. Pure so the selection logic itself is
        // testable without touching Hero/Clan.
        public static int PickSuccessorIndex(int candidateCount, int rawRandomDraw)
        {
            if (candidateCount <= 0) return -1;
            int idx = rawRandomDraw % candidateCount;
            return idx < 0 ? idx + candidateCount : idx;
        }
    }
}
