// =============================================================================
// THE DARKEST NIGHT — Factions/PaleWidows/PaleWidowsMath.cs
//
// Pure numeric core of Phase 7, Faction H — the Southern Empire becomes
// "The Pale Widows": men failed to hold the Long Night back, so the women of
// the Southern court seized power and bought peace by feeding the demons the
// men who could not save them. No TaleWorlds types (fully covered by
// PureLogicTests). Mirrors the shape of LegionMath.cs / BloodboundMath.cs.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class PaleWidowsMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Pale Widows keep exactly two seats: Phycaon (town_ES6) and
        // Lycaron (town_ES4) — verified against the shipped SandBox/ModuleData/
        // settlements.xml ("{=Settlements.Settlement.name.town_ES6}Phycaon" /
        // "town_ES4}Lycaron").
        public static readonly string[] StartingTownIds = { "town_ES4", "town_ES6" };

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Soldier sacrifice → "the dark's blindness" ───────────────────────────
        // "1 day of demons ignoring you per soldier sacrificed" — literal from the
        // brief. The menu renders one soldier per use (mirrors WolfBrothersMath's
        // one-at-a-time larder); repeated use stacks (see
        // PaleWidowsCampaignBehavior.GrantIgnoreDays, which extends an existing
        // window rather than overwriting it, unlike Bloodbound's reset-on-refresh
        // buff — a Widow's bargain accumulates, it doesn't merely refresh).
        public const float IgnoreDaysPerSoldierSacrificed = 1f;

        // ── Prisoner-lord / clan-member sacrifice → demon soldiers + immunity ────
        // A far heavier offering than a common soldier, so it buys more: a
        // handful of demon troops fight at your side for 1-4 weeks, and the
        // immunity window is longer than the soldier-sacrifice rate (3-7 days,
        // rolled) — the dark owes a bigger debt for a lordly life than for a
        // common one.
        public const int DemonTroopsGrantedPerMaleSacrifice = 3;

        public const float MinVanishDays = 7f;   // one week
        public const float MaxVanishDays = 28f;  // four weeks

        public static float RollVanishDays(double roll01)
        {
            float span = MaxVanishDays - MinVanishDays;
            float days = MinVanishDays + (float)roll01 * span;
            if (days < MinVanishDays) days = MinVanishDays;
            if (days > MaxVanishDays) days = MaxVanishDays;
            return days;
        }

        public const float MinLordSacrificeImmunityDays = 3f;
        public const float MaxLordSacrificeImmunityDays = 7f;

        public static float RollLordSacrificeImmunityDays(double roll01)
        {
            float span = MaxLordSacrificeImmunityDays - MinLordSacrificeImmunityDays;
            float days = MinLordSacrificeImmunityDays + (float)roll01 * span;
            if (days < MinLordSacrificeImmunityDays) days = MinLordSacrificeImmunityDays;
            if (days > MaxLordSacrificeImmunityDays) days = MaxLordSacrificeImmunityDays;
            return days;
        }

        // A grant EXTENDS an existing window instead of resetting it — stacking,
        // not refreshing. `currentExpiryDay` is the campaign day the previous
        // grant runs out (or `today` / earlier if there is none active).
        public static float ExtendIgnoreExpiry(float today, float currentExpiryDay, float grantDays)
        {
            float baseline = currentExpiryDay > today ? currentExpiryDay : today;
            return baseline + grantDays;
        }

        public static bool IsIgnoreActive(float today, float expiryDay) => today < expiryDay;

        // ── The small chance the offering claims the offerer instead ───────────
        // A male player rolls this every time he uses EITHER sacrifice menu
        // option (soldier or lord/clan-member) — the Widows' knives are not
        // always particular about whose blood pays the debt.
        public const double SelfSacrificeChance = 0.08; // 8%

        public static bool RollSelfSacrifice(double roll01) => roll01 < SelfSacrificeChance;

        // Consequence of a failed roll: the player is dragged to the altar
        // himself, bled near to death, and disgraced in front of the court —
        // a real, felt cost, short of permanent death (which would nuke a
        // whole playthrough for a single unlucky menu click).
        public const int   SelfSacrificeHpFloor    = 1;    // left at the edge of death
        public const float SelfSacrificeRenownLoss = 100f; // the court does not forget the spectacle

        // ── Daily influence drain for a male player ──────────────────────────────
        // "A male player in this faction loses all influence every day" — literal
        // from the brief: drains the player's clan influence to zero, once per
        // daily tick, for as long as he is both male and a Pale Widows vassal.
        public static float InfluenceAfterDailyDrain(bool isMale, bool isMember, float currentInfluence)
        {
            if (isMale && isMember) return 0f;
            return currentInfluence;
        }
    }
}
