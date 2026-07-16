// =============================================================================
// THE DARKEST NIGHT — Economy/EconomyMath.cs
//
// THE BARTER ECONOMY. Pure tuning for Phase 2 (Requirements 3, 5, 9, 31): gold
// ceases to matter, food and goods are what keeps a party alive.
//
// ── Requirement 5 feasibility spike — decision: PATH B (gold ~10x scarcer) ──
// Before writing a line of override code, every vanilla gold flow was mapped
// against the GameModel that controls it:
//
//   Flow                    Controlling model / behavior      Zeroable cleanly?
//   -----------------------------------------------------------------------
//   Troop wages              DefaultPartyWageModel              yes (factor)
//   Recruit cost              DefaultPartyWageModel.
//                             GetTroopRecruitmentCost            yes (factor)
//   Tier upgrade cost         DefaultPartyTroopUpgradeModel       yes (factor)
//   Building/boost cost       DefaultBuildingConstructionModel    yes (factor)
//   Battlefield gold loot     DefaultBattleRewardModel            yes (factor)
//   Ransom                    DefaultRansomValueCalculationModel  yes (factor)
//   Garrison wages/limits     DefaultSettlementGarrisonModel      yes (factor)
//   Quest/favor gold rewards  ad-hoc ChangeGold/GainGold helpers  yes (call site)
//   ---------------------------------------------------------------------
//   Town/village TRADE SCREEN (buy/sell at a settlement) and the party-screen
//   ITEM EXCHANGE popup are NOT model seams — TaleWorlds hard-codes gold as
//   the transaction unit inside those UI view-models (SPTradeItemVM / the
//   inventory exchange screen). There is no GameModel hook that turns the
//   town store into a pure barter screen; doing so would mean rewriting two
//   core UI flows outside this mod's override architecture, with no proven
//   precedent anywhere in this codebase (behaviour.md: "prefer the simple
//   working solution"). That is the one vanilla flow Requirement 5 explicitly
//   anticipates failing Path A ("gold can't be removed for some flow you
//   can't override cleanly ... fallback").
//
// Because every OTHER flow in the table above genuinely can be zeroed, gold
// removal (Path A) was tempting — but leaving the town store gold-priced while
// every other system pretends gold does not exist would be a worse, more
// confusing player experience than a single consistent rule. So: Path B,
// applied uniformly. Gold income and every gold cost drop to roughly a tenth
// of vanilla (GoldScarcityFactor), rewards are cut harder still so they read
// as "trivial" per Requirement 9, and town markets are additionally starved
// of the goods that actually matter (food, weapons, horses) so that barter —
// trading items for items at the settlement store, which the reduced gold on
// both sides of every transaction naturally pushes players toward — becomes
// the practical way to get by, without requiring an unproven UI rewrite.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class EconomyMath
    {
        // ── Path B scarcity factors ─────────────────────────────────────────
        // Applied as ExplainedNumber.AddFactor(GoldReductionFactor) at every
        // override site below: −90% = roughly a tenth of the vanilla amount.
        public const float GoldScarcityFactor = 0.10f;
        public const float GoldReductionFactor = GoldScarcityFactor - 1f; // -0.90f

        // Ransom is worth almost nothing under the new economy.
        public const float RansomScarcityFactor = 0.05f;

        // Gold looted from a defeated party's coffers.
        public const float PlunderGoldScarcityFactor = 0.10f;

        // NPC quest/favor/event gold rewards (Requirement 9): cut far harder
        // than the general scarcity factor so any unavoidable gold reward
        // reads as trivial, never as a real payday.
        public const float RewardGoldScarcityFactor = 0.05f;
        public const int RewardGoldMinimum = 1;

        // ── Requirement 31 — thin garrisons ─────────────────────────────────
        public const float GarrisonGrowthScale = 0.5f;
        public const float AutoRecruitmentScale = 0.5f;

        // ── v0.8.0 (issue 15) — lord parties read scarcity-thin too, not just
        // gold. Applied as a straight multiplier on the vanilla party-size
        // limit (EconomyPartySizeModel.GetPartyMemberSizeLimit).
        public const float LordPartySizeMult = 0.5f;

        // A new character starts with roughly a tenth of vanilla's 1000 gold —
        // consistent with GoldScarcityFactor everywhere else in this economy.
        public const int PlayerStartingGold = 50;

        // Town food-stock / militia ceilings, expressed as a fraction of the
        // settlement's Prosperity so richer towns still hold a little more
        // than poor ones, but every town sits at roughly half (or less) of
        // where vanilla equilibrium would otherwise settle.
        public const float TownFoodStockCapFactor = 0.30f;
        public const float TownFoodStockCapFloor = 20f;

        // ── Requirement 3 — town/village market scarcity ────────────────────
        // Town traders hold little coin...
        public const int TownTraderGoldCap = 800;
        // ...almost no food for sale...
        public const float TownFoodSaleFactor = 0.05f;
        // ...horses are almost unavailable — most towns show none at all, the
        // rest gate what few they have behind a per-town-per-day roll (see
        // TownSellsHorsesToday) so finding one reads as a lucky day, not a
        // guarantee...
        public const int TownHorseSaleCap = 1;
        public const int HorseAvailableTownFraction = 3; // roughly 1 town in 3, on any given day
        // ...and only the crudest weapons remain, in far reduced numbers
        // (Children of the Forest prompt — tightened from tier 3 / ×0.40)...
        public const int CrudeWeaponTierCap = 2;
        public const float TownWeaponSaleFactor = 0.15f;
        // ...and armour above a middling tier vanishes too — previously
        // untouched by market scarcity at all.
        public const int ArmorTierCap = 3;
        public const float TownArmorSaleFactor = 0.30f;

        // The Children of the Forest sell almost no weapons at all — their
        // wandwright deals in wands, not blades (Pen Cannoc, see
        // CityStateMath/CityStateSystem's Children of the Forest wiring).
        public const float ForestWeaponSaleFactor = 0f;

        // Villages become the main food source: their stalls run fuller than
        // a town's, up to a cap so a single village can't feed an army alone.
        public const float VillageFoodBoostFactor = 1.6f;
        public const int VillageFoodBoostCap = 400;

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Scale a gold amount by a flat factor, rounding to the nearest coin.</summary>
        public static int ScaleGold(int amount, float factor)
        {
            return (int)Math.Round(amount * factor, MidpointRounding.AwayFromZero);
        }

        public static int ScaledPlunderGold(int vanillaAmount)
        {
            if (vanillaAmount <= 0) return vanillaAmount;
            return ScaleGold(vanillaAmount, PlunderGoldScarcityFactor);
        }

        public static int ScaledRansom(int vanillaAmount)
        {
            if (vanillaAmount <= 0) return vanillaAmount;
            return ScaleGold(vanillaAmount, RansomScarcityFactor);
        }

        /// <summary>
        /// Requirement 9: reward gold from NPC quests/favors/events becomes trivial.
        /// Only ever shrinks a POSITIVE amount — costs paid BY the player (negative
        /// amounts) are left untouched here; they are governed by the general
        /// scarcity factor at their own call sites, not by this reward path.
        /// </summary>
        public static int ScaledReward(int vanillaAmount)
        {
            if (vanillaAmount <= 0) return vanillaAmount;
            int scaled = ScaleGold(vanillaAmount, RewardGoldScarcityFactor);
            return Math.Max(RewardGoldMinimum, scaled);
        }

        public static float ScaledGarrisonChange(float vanillaChange)
        {
            return vanillaChange * GarrisonGrowthScale;
        }

        public static int ScaledAutoRecruitmentCount(int vanillaCount)
        {
            if (vanillaCount <= 0) return 0;
            return Math.Max(0, (int)Math.Floor(vanillaCount * AutoRecruitmentScale));
        }

        public static float MaxFoodStocks(float prosperity)
        {
            return Math.Max(TownFoodStockCapFloor, prosperity * TownFoodStockCapFactor);
        }

        public static int TownFoodSaleQuantity(int currentAmount)
        {
            if (currentAmount <= 0) return 0;
            return (int)Math.Floor(currentAmount * TownFoodSaleFactor);
        }

        public static int TownHorseSaleQuantity(int currentAmount)
        {
            if (currentAmount <= 0) return 0;
            return Math.Min(currentAmount, TownHorseSaleCap);
        }

        public static int TownWeaponSaleQuantity(int currentAmount, int tier)
        {
            if (currentAmount <= 0) return 0;
            if (tier > CrudeWeaponTierCap) return 0;
            return (int)Math.Floor(currentAmount * TownWeaponSaleFactor);
        }

        /// <summary>Children of the Forest markets: no weapons at all, regardless of tier.</summary>
        public static int ForestWeaponSaleQuantity(int currentAmount)
        {
            if (currentAmount <= 0) return 0;
            return (int)Math.Floor(currentAmount * ForestWeaponSaleFactor);
        }

        public static int TownArmorSaleQuantity(int currentAmount, int tier)
        {
            if (currentAmount <= 0) return 0;
            if (tier > ArmorTierCap) return 0;
            return (int)Math.Floor(currentAmount * TownArmorSaleFactor);
        }

        /// <summary>
        /// Deterministic per-town-per-day gate for horse availability: roughly
        /// one town in HorseAvailableTownFraction shows any horses at all on a
        /// given day, so a horse for sale reads as a lucky find. Pure — no
        /// TaleWorlds types, hashes the settlement id together with the day.
        /// </summary>
        public static bool TownSellsHorsesToday(string settlementStringId, int currentDay)
        {
            unchecked
            {
                int h = 17;
                h = h * 397 + (settlementStringId ?? string.Empty).GetHashCode();
                h = h * 397 + currentDay;
                if (h < 0) h = ~h;
                return h % HorseAvailableTownFraction == 0;
            }
        }

        public static int VillageFoodQuantity(int currentAmount)
        {
            if (currentAmount <= 0) return 0;
            int boosted = (int)Math.Round(currentAmount * VillageFoodBoostFactor, MidpointRounding.AwayFromZero);
            return Math.Min(VillageFoodBoostCap, boosted);
        }
    }
}
