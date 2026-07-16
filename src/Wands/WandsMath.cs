// =============================================================================
// THE DARKEST NIGHT — Wands/WandsMath.cs
//
// Pure numeric core for magical wands (mod-author-directed addition, not part
// of the original phased build). No TaleWorlds types (fully covered by
// PureLogicTests). Runtime wiring lives in WandEffects.cs / WandsCampaignBehavior*;
// the item/spell table lives in WandsCatalog.cs.
//
// ── What a wand is ───────────────────────────────────────────────────────
// A wand is a carried item that releases a single fixed SpellbookCatalog
// spell (SpellbookEffects.Cast) on a landed melee hit, gated by a short
// cooldown so it cannot be spammed every swing. Two very different economies
// govern how "used up" a wand gets, because the two wielder types need
// fundamentally different state:
//
//   • THE PLAYER — real charges, tracked per (wand item id) in a small
//     persisted dictionary the mod owns (see WandEffects.cs). Bannerlord's
//     ItemObject/EquipmentElement have no clean per-physical-instance charge
//     slot without abusing ItemModifier ladders across every roster stack
//     that carries the item (LordGearWeathering.cs confirms EquipmentElement
//     DOES carry an ItemModifier, but building new modifier tiers and
//     reliably re-slotting them across arbitrary roster stacks is a much
//     larger, riskier surface than this feature warrants). The documented
//     simplification: "the wand your party carries has N charges left" —
//     charges are NOT per physical instance. If the player somehow holds two
//     Wands of Fireball, they share one charge pool. Acquiring a wand (buy
//     or ruin loot) tops the pool back up to PlayerMaxCharges rather than
//     adding to it — a "fresh" wand entering the party effectively replaces
//     a spent one for bookkeeping purposes.
//
//   • NPCs (lords, Hollow Choir troops) — no persisted per-instance state at
//     all. Every cast rolls NpcBreakChancePerUse; on a break the wand goes
//     inert for the rest of that mission (and, for a hero wielder, one copy
//     is struck from their party's roster) — simpler than tracking charge
//     state for however many hundreds of AI agents might carry one.
// =============================================================================

using System;
using System.Collections.Generic;

namespace TheDarkestNight
{
    public static class WandsMath
    {
        // ── Pricing ───────────────────────────────────────────────────────────
        // "Well above the Rod of the Apostle's 6000" (ChosenMath.RodPurchaseCostGold):
        // a Standard wand (one of the 10 element attack/wall pairs) is double
        // the Rod's price; a Dramatic wand (a demon-facing working or one of
        // the grander invented spells) is more than triple it.
        public const int StandardWandPriceGold = 12000;
        public const int DramaticWandPriceGold  = 20000;

        public static int PriceForTier(WandTier tier)
            => tier == WandTier.Dramatic ? DramaticWandPriceGold : StandardWandPriceGold;

        // ── Player charges ───────────────────────────────────────────────────
        // Enough for a real battle's worth of casts without being free/infinite.
        public const int PlayerMaxCharges = 8;

        // ── Cast cooldown (all wielders) ─────────────────────────────────────
        // Prevents "cast on every swing" — comparable to the Ashen lords' fast
        // 6s NPC-cast cadence (ElementLordAI), since a wand is meant to feel
        // like a real but bounded battlefield tool, not a proc-on-every-hit trinket.
        public const float CastCooldownSeconds = 6f;

        // ── NPC break-instead-of-charges ─────────────────────────────────────
        // Roughly 1-in-8 uses shatters an NPC's wand — simpler than tracking
        // charge state per AI agent, and naturally caps how much value any one
        // NPC wand-holder extracts from it over a long campaign.
        public const float NpcBreakChancePerUse = 0.12f;

        public static bool NpcWandBreaks(double roll01) => roll01 < NpcBreakChancePerUse;

        // ── Ruin loot ─────────────────────────────────────────────────────────
        // Rarer than a relic drop (RelicMath.RuinBaseRelicChance = 0.15) or a
        // spell-formula find — a wand is meant to be a genuine, rare treasure.
        public const float RuinWandChance = 0.05f;

        public static bool RollRuinWandLoot(double roll01) => roll01 < RuinWandChance;

        // Picks which catalog wand drops, uniform over however many exist —
        // mirrors RelicMath.PickRelicIndex exactly.
        public static int PickWandIndex(double roll01, int wandCount)
        {
            if (wandCount <= 0) return -1;
            int idx = (int)(roll01 * wandCount);
            if (idx < 0) idx = 0;
            if (idx >= wandCount) idx = wandCount - 1;
            return idx;
        }

        // ── NPC distribution (Tower / Chosen lords) ──────────────────────────
        // Rolled ONCE per hero, ever (WandsCampaignBehavior tracks who has
        // already been rolled) — a stable rarity, not a chance that creeps
        // toward 100% of the faction's lords the longer a campaign runs.
        public const double TowerLordWandChance  = 0.15;
        public const double ChosenLordWandChance = 0.15;

        // The Children of the Forest have no army of their own — the wand IS
        // their soldiery, so the overwhelming majority of their lords carry
        // (and, per WandsCampaignBehavior.EnsureLordWandEquipped, actually
        // wield) one. "Mostly," not universally — a few still ride to battle
        // bare-handed of it.
        public const double ForestLordWandChance = 0.85;

        public static bool ShouldGrantLordWand(double roll01, double chance) => roll01 < chance;

        // ── NPC distribution (Hollow Choir) ──────────────────────────────────
        // Documents the intent behind ModuleData/troops.xml's hollow_magus
        // equipment-roster duplication (1 wand-bearing roster alongside 3
        // plain ones = 25%): troops.xml equipment rosters are picked uniformly
        // by the engine at spawn with no runtime hook back into C# constants,
        // so this constant is test-covered documentation of that XML ratio,
        // not a value actually read at runtime.
        public const double HollowMagusWandRosterFraction = 0.25;

        // ── Shop scarcity (The Children of the Forest prompt) ────────────────
        // The wandwright no longer stocks the whole catalog — each shop town
        // holds a small rotating case, re-rolled on a slow cadence, and every
        // slot sells once before it needs to be restocked.
        public const int ShopStockSize = 3;
        public const int ForestShopStockSize = 5; // Pen Cannoc cuts the wands — their case runs fuller
        public const int ShopRestockDays = 14;

        public static bool ShouldRestock(int lastRestockDay, int currentDay)
            => currentDay - lastRestockDay >= ShopRestockDays;

        // Deterministic per-town, per-restock-cycle seed — combines the town's
        // own identity with which restock cycle this is, so the same town on
        // the same cycle always rolls the same case (reload-safe) while a new
        // cycle (or a different town) rolls a different one.
        public static int RestockSeed(string townStringId, int restockCycle)
        {
            unchecked
            {
                int h = 17;
                h = h * 397 + (townStringId ?? string.Empty).GetHashCode();
                h = h * 397 + restockCycle;
                return h;
            }
        }

        // Picks `stockSize` distinct catalog indices out of `catalogCount`,
        // deterministic for a given seed — a partial Fisher-Yates shuffle.
        // Mirrors PickWandIndex's shape: pure, no TaleWorlds types.
        public static List<int> PickShopStock(int seed, int catalogCount, int stockSize)
        {
            var result = new List<int>();
            if (catalogCount <= 0 || stockSize <= 0) return result;
            stockSize = Math.Min(stockSize, catalogCount);

            var pool = new List<int>(catalogCount);
            for (int i = 0; i < catalogCount; i++) pool.Add(i);

            var rng = new Random(seed);
            for (int i = 0; i < stockSize; i++)
            {
                int j = i + rng.Next(pool.Count - i);
                int tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }

            for (int i = 0; i < stockSize; i++) result.Add(pool[i]);
            return result;
        }
    }
}
