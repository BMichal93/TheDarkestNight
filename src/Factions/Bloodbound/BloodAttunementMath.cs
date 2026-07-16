// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodAttunementMath.cs
//
// Pure numeric core of the Bloodbound's FOURTH way to spend Demon Blood:
// drinking it to permanently attune to one of four elements (Fire, Water,
// Earth, Wind — Spirit deliberately excluded, per the mod author's brief).
// No TaleWorlds types — fully covered by PureLogicTests. Runtime behaviour
// (the tracker, the drinking menu, the battle input, the lord AI) lives in
// BloodAttunement.cs, BloodboundCampaignBehavior.Menus.cs,
// BloodAttunementInputHandler.cs, and BloodAttunementLordAI.cs.
//
// Mirrors the shape of BloodboundMath.cs.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class BloodAttunementMath
    {
        // ── Escalating cost ────────────────────────────────────────────────────
        // The 1st element attuned costs 1 Demon Blood, the 2nd costs 2, the 3rd
        // costs 3, the 4th costs 4 — cost is however many the hero already
        // knows, plus one.
        public static int AttunementCostBlood(int alreadyKnownCount)
        {
            if (alreadyKnownCount < 0) alreadyKnownCount = 0;
            return alreadyKnownCount + 1;
        }

        // ── Relation fallout ──────────────────────────────────────────────────
        // Every NEWLY learned element costs -5 relation with every other
        // faction's ruling clan/leader, except the Temple (-15) — the Temple
        // despises this demonic corruption most of all. Applied per instance,
        // uncapped: learning all four stacks to -20 with everyone else and -60
        // with the Temple. The mod author's numbers are explicitly per-drink,
        // so no diminishing/capping is applied here.
        public const int RelationPenaltyOther  = -5;
        public const int RelationPenaltyTemple = -15;

        // ── Usable hours — demonic power sleeps through full daylight ─────────
        // CurrentHourInDay (0..24). Distinct from (but consistent in spirit
        // with) DemonMath.IsNightHour/DuskHour/DawnHour's dusk-to-dawn night
        // band — this window is deliberately BROADER: it blocks only the deep
        // daylight core (roughly mid-morning through mid-afternoon) and allows
        // dawn, dusk, and the whole of night, so "night or twilight" both
        // answer the blood.
        public const float DaylightBlockStartHour = 9f;
        public const float DaylightBlockEndHour   = 17f;

        public static bool IsUsableHour(float hourOfDay)
            => hourOfDay < DaylightBlockStartHour || hourOfDay >= DaylightBlockEndHour;

        // ── The random permanent penalty rolled on every NEW drink ────────────
        // (A) -1 Social, (B) -1 Intellect, (C) permanent daytime-only morale
        // penalty, (D) permanent daytime-only party-speed penalty. Stacks
        // across however many elements are learned (up to 4 instances) and
        // never expires.
        public enum PenaltyKind { SocialDown, IntellectDown, DaytimeMorale, DaytimeSpeed }

        public static PenaltyKind RollPenalty(double roll01)
        {
            int idx = (int)(roll01 * 4);
            if (idx < 0) idx = 0;
            if (idx > 3) idx = 3;
            return (PenaltyKind)idx;
        }

        // Magnitude of the two flag-driven penalties — per stack, applied only
        // while the current hour is NOT a usable (night/twilight) hour.
        public const float DaytimeMoraleDrainPerDay  = 3f;      // RecentEventsMorale, per stack, per day
        public const float DaytimeSpeedPenaltyFactor = -0.06f;  // party speed factor, per stack (-6%)

        // ── NPC lord seeding ────────────────────────────────────────────────────
        // Each Bloodbound lord attunes to a random 1-to-4 subset of the four
        // elements at session start.
        public static int PickElementCount(double roll01)
        {
            int n = 1 + (int)(roll01 * 4);
            if (n < 1) n = 1;
            if (n > 4) n = 4;
            return n;
        }

        // Battle cooldown between a Bloodbound lord's attuned-element casts —
        // mirrors SpellcasterLordMath.CastCooldownSeconds.
        public const float LordCastCooldownSeconds = 18f;

        // A lord favours the attack over the wall most of the time, but will
        // occasionally raise a ward instead — mirrors NpcCastPlanner's
        // defensive-vs-offensive split in spirit without copying its state.
        public const double LordWallChance = 0.3;
    }
}
