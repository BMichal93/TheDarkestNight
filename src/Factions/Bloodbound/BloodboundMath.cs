// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundMath.cs
//
// Pure numeric core of Faction D — the Bloodbound (formerly Khuzait). No
// TaleWorlds types (fully covered by PureLogicTests). Runtime behaviour —
// culture/kingdom renaming, dialogue, town-scoping, the demon-blood yield
// hook, the join-gate check, and the spending menu — lives in
// BloodboundCulture.cs, BloodboundDialogue.cs, BloodboundSettlements.cs, and
// BloodboundCampaignBehavior(.Menus).cs.
//
// Mirrors the shape of HiveMath.cs / TowerMath.cs / WolfBrothersMath.cs.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class BloodboundMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Bloodbound keep exactly two seats: Akkalat (town_K2) and Chaikand
        // (town_K5) — verified against the shipped SandBox/ModuleData/settlements.xml
        // (the prompt's "Akalat"/"Chaikland" spellings match these real ids).
        public static readonly string[] StartingTownIds = { "town_K2", "town_K5" }; // Akkalat, Chaikand

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Demon Blood yield on a demon-party victory ───────────────────────────
        // Every demon party a Bloodbound vassal (player OR lord) personally
        // defeats yields a small haul of Demon Blood — never nothing (they always
        // walk away with proof of the kill), never a flood (it stays a resource
        // worth rationing, not a currency that trivialises the spending menu).
        public const int MinDemonBloodPerVictory = 1;
        public const int MaxDemonBloodPerVictory = 3;

        public static int RollDemonBloodYield(double roll01)
        {
            int span = MaxDemonBloodPerVictory - MinDemonBloodPerVictory + 1;
            int idx = (int)(roll01 * span);
            if (idx < 0) idx = 0;
            if (idx >= span) idx = span - 1;
            return MinDemonBloodPerVictory + idx;
        }

        // ── Spending: demons ignore you ──────────────────────────────────────────
        public const int   IgnoreCostBlood   = 3;
        public const float MinIgnoreDays     = 1f;
        public const float MaxIgnoreDays     = 4f;

        public static float RollIgnoreDurationDays(double roll01)
        {
            float span = MaxIgnoreDays - MinIgnoreDays;
            float days = MinIgnoreDays + (float)roll01 * span;
            if (days < MinIgnoreDays) days = MinIgnoreDays;
            if (days > MaxIgnoreDays) days = MaxIgnoreDays;
            return days;
        }

        // daysSinceGranted: CampaignTime.Now.ToDays - the day the ignore was bought.
        public static bool IsIgnoreActive(float daysSinceGranted, float durationDays)
            => daysSinceGranted >= 0f && daysSinceGranted < durationDays;

        // ── Spending: a week of hardened flesh (+80 max HP) ──────────────────────
        // Hero.MaxHitPoints has no setter (it is derived from Endurance/level) —
        // this is implemented as a week-long "ceiling raise": the hero is topped
        // up by HpBuffAmount above their normal max the instant it is bought, and
        // that raised ceiling (MaxHitPoints + HpBuffAmount) is re-enforced by the
        // daily tick for HpBuffDurationDays, then let fall back to the normal cap.
        public const int   HpBuffCostBlood      = 5;
        public const float HpBuffAmount         = 80f;
        public const float HpBuffDurationDays   = 7f;

        public static bool IsHpBuffActive(float daysSinceGranted)
            => daysSinceGranted >= 0f && daysSinceGranted < HpBuffDurationDays;

        // ── Spending: surpass the body's old limits (permanent attribute trade) ──
        // -1 Social/Intellect (random) for +1 Vigor/Endurance (random).
        public const int AttributeTradeCostBlood = 4;

        // roll01 picks which of the two paired attributes swings this time.
        public static int PickPairIndex(double roll01) => roll01 < 0.5 ? 0 : 1;

        // ── The join gate — the Bloodbound refuse the soft and the untested ─────
        // A candidate needs BOTH: a physical floor (Vigor + Endurance combined)
        // AND at least one focus point sunk into an actual weapon skill. Either
        // alone is not enough — a strong body that never trained is still raw
        // meat to them, and a trained hand in a frail body will not survive the
        // hunt.
        public const int MinVigorPlusEndurance = 10;

        public static bool MeetsPhysicalThreshold(int vigor, int endurance)
            => (vigor + endurance) >= MinVigorPlusEndurance;

        public static bool HasCombatFocus(int totalCombatFocusPoints)
            => totalCombatFocusPoints > 0;

        public static bool QualifiesForBloodbound(int vigor, int endurance, int totalCombatFocusPoints)
            => MeetsPhysicalThreshold(vigor, endurance) && HasCombatFocus(totalCombatFocusPoints);
    }
}
