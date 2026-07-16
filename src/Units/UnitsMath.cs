// =============================================================================
// THE DARKEST NIGHT — Units/UnitsMath.cs
//
// Pure numeric core of Phase 3 (Requirements 4, 29, 30) — "armies look and
// cost like the end of the world." No TaleWorlds types (fully covered by
// PureLogicTests). Runtime plumbing — item selection, consumption, equipment
// swaps — lives in PromotionToll.cs / GearWeathering.cs / the campaign
// behaviors in this folder.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class UnitsMath
    {
        // ── Requirement 4 — costly promotion ────────────────────────────────────
        // Upgrading INTO tier 4 or 5 is the toll gate; tiers 1-3 stay free of it
        // (vanilla gold/xp costs still apply through EconomyTroopUpgradeModel).
        public const int PromotionTollMinTier = 4;

        // A weapon only counts toward the toll if it is worth spending — a rusty
        // practice blade does not buy a place in the line of the desperate.
        public const int GoodPriceWeaponMinValue = 300;

        public static bool RequiresPromotionToll(int targetTier)
            => targetTier >= PromotionTollMinTier;

        public static bool IsGoodPriceWeapon(int itemValue)
            => itemValue >= GoodPriceWeaponMinValue;

        // ── Requirement 30a — shabby mid-tiers ──────────────────────────────────
        // Tier 3-4 troops (every culture) are re-equipped with whatever is
        // cheapest of the same item type at session launch; tier 5 is left alone
        // (Requirement 30: "tier 5 stays good gear-wise").
        public const int ShabbyGearMinTier = 3;
        public const int ShabbyGearMaxTier = 4;
        public const int ShabbyArmorValueCap = 300;

        public static bool IsShabbyGearTier(int tier)
            => tier >= ShabbyGearMinTier && tier <= ShabbyGearMaxTier;

        public static bool NeedsGearDowngrade(int itemValue, int valueCap)
            => itemValue > valueCap;

        // ── Requirement 30b — tier-5 recruits are an investment too ────────────
        public const int Tier5RecruitMinTier = 5;

        public static bool IsTier5Recruit(int troopTier)
            => troopTier >= Tier5RecruitMinTier;

        // ── Requirement 29 — weary lords ────────────────────────────────────────
        // A piece counts as "gold/ornate/rich" (and gets stripped) if its price
        // clears the bar OR it carries a quality modifier (fine/masterwork/…) —
        // either marks it as finery a survivor would have sold or lost by now.
        public const int WearyLordValueCap = 500;

        public static bool IsOrnateLordGear(int itemValue, bool hasQualityModifier)
            => itemValue > WearyLordValueCap || hasQualityModifier;
    }
}
