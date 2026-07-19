// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenMath.cs
//
// Pure numeric core of Phase 7, Faction H — the Southern Empire becomes
// "The Chosen": the first PriestKing had a vision of an angelic creature
// that blessed his line and promised salvation from demons. His followers
// believe his bloodline is divine, and his word is not debated. No
// TaleWorlds types (fully covered by PureLogicTests). Mirrors the shape of
// LegionMath.cs / TempleMath.cs / BloodboundMath.cs, and reuses the God-King
// mechanic's numeric shape (see Tribes/TribalKingdomBehavior.cs) retargeted
// to the Southern Empire.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class ChosenMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Chosen keep the SAME two seats the (deleted) Pale Widows held —
        // Phycaon (town_ES6) and Lycaron (town_ES4) — plus the border ground
        // ReassignImperialSettlements deliberately hands it at new-game: Razih
        // (town_A4) and Qasira (town_A8) from the Tower's Aserai border. Every
        // other native Southern Empire holding now falls out of scope like the
        // five remnant factions' own — the Chosen are one of Requirement 10's
        // "eight desperate factions," not the untouched vanilla imperial
        // bloc. Verified against the shipped SandBox/ModuleData/settlements.xml.
        //
        // NOT Akkalat (town_K2), despite ReassignImperialSettlements' comment
        // block listing it as a Southern Empire border grab: town_K2 is one of
        // BloodboundMath.StartingTownIds' own two protected seats ("Akkalat,
        // Chaikand"), and the seat-protection guard correctly refuses to hand
        // one faction's own declared capital to another — so that particular
        // grab has always been a silent no-op. Pre-existing content
        // contradiction between the two factions' seat lists (same class as
        // the Temple/Empire Ocs Hall-Pravend conflict noted in TempleMath.cs);
        // left for the mod author to resolve which faction actually keeps it.
        public static readonly string[] StartingTownIds =
        {
            "town_ES4", "town_ES6",   // Lycaron, Phycaon
            "town_A4",                // Razih
            "town_A8",                // Qasira
        };

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── PriestKing dominance ─────────────────────────────────────────────────
        // Mirrors TribalKingdomBehavior's GodKingInfluenceMin/LordInfluenceCap
        // exactly: the ruling clan's influence is pinned at/above this floor,
        // every other Chosen clan's influence is capped low — the mechanical
        // expression of "no vote regarding distribution of castles or
        // policies" among the Chosen's lords (Apostles in name, never equals
        // in practice).
        public const float PriestKingInfluenceMin = 4000f;
        public const float ApostleInfluenceCap    = 50f;

        // ── Wives of Conquest ────────────────────────────────────────────────────
        // Every settlement the Chosen capture adds a woman of that place to
        // the PriestKing's household, capped — mirrors TribalKingdomBehavior's
        // TribalWifeMax precedent exactly.
        public const int PriestKingWifeMax = 8;

        // ── Never allies with the Temple, at war whenever possible ──────────────
        // Nothing numeric is needed for the reversal itself (it is a direct
        // re-declare, see ChosenCampaignBehavior.OnMakePeace), but the daily/
        // weekly safety-net check needs a cadence: the Chosen re-check their
        // stance with the Temple this often even if no peace/alliance attempt
        // was caught directly.
        public const int TempleWarCheckIntervalDays = 1; // checked every daily tick

        // ── Expansionism — noticeably MORE aggressive than Legion ───────────────
        // Legion (LegionMath): 35% daily raid-nudge chance, 8 days of total
        // peace tolerated before a forced war declaration. The Chosen read as
        // more expansive still: a higher raid-nudge chance, roughly half the
        // peace tolerance, AND (uniquely) a chance to nudge an already-idle
        // Chosen lord into besieging a weakly-held hostile settlement instead
        // of merely raiding it — "very expansive" means real conquest, not
        // just raiding.
        public const double RaidNudgeChance  = 0.50;
        public const int    PeaceToleranceDays = 4;
        public const double SiegeNudgeChance = 0.15; // rolled only when a raid-nudge did NOT fire

        public static bool ShouldNudgeToRaid(double roll01) => roll01 < RaidNudgeChance;

        public static bool ShouldNudgeToSiege(double roll01) => roll01 < SiegeNudgeChance;

        public static bool ShouldForceWarDeclaration(int consecutivePeaceDays)
            => consecutivePeaceDays >= PeaceToleranceDays;

        // ── The Rod of the Apostle ───────────────────────────────────────────────
        // A relic-tier price — far beyond the Holy Sigil's 350, this is a
        // monstrous instrument of zealotry, not a starter trinket.
        public const int RodPurchaseCostGold = 6000;

        // On block: the wielder's own faith punishes the ally standing nearest
        // — 50 damage — while the wielder is fortified for holding the line —
        // 100 HP healed. Deliberately harsh: this is a cursed relic, not a
        // benevolent one.
        public const float RodOnBlockAllyDamage    = 50f;
        public const float RodOnBlockWielderHeal   = 100f;

        // On a landed hit: an ally near the wielder is killed outright AND a
        // demon is summoned at the wielder's side — the Rod feeds the Chosen's
        // war however it can, cost be damned.
        // (No numeric tunable needed beyond the kill itself — see
        // ChosenRodEffects.cs.)

        public const float RodEffectRadius = 5f; // metres — how "near the wielder" is measured

        // ── Male player wife-taking (God-King precedent, reused for the player) ──
        // Mirrors TribalKingdomBehavior's TribalWifeMax (8) — a male Chosen
        // player may take multiple wives via either UX path (settlement
        // capture prompt, or converting a prisoner at a Chosen town), capped
        // at the same reasoned number.
        public const int PlayerWifeMax = 8;

        // A newly-created wife hero is instantiated in the 18-26 adult range,
        // exactly like TribalKingdomBehavior.AcquireConsort's consorts.
        public const int PlayerWifeMinAge = 18;
        public const int PlayerWifeMaxAgeSpan = 8; // 18..25 inclusive when added to MinAge

        public static int RollWifeAge(double roll01)
        {
            int span = PlayerWifeMaxAgeSpan;
            int add = (int)(roll01 * span);
            if (add < 0) add = 0;
            if (add >= span) add = span - 1;
            return PlayerWifeMinAge + add;
        }
    }
}
