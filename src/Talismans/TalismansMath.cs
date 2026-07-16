// =============================================================================
// THE DARKEST NIGHT — Talismans/TalismansMath.cs
//
// Pure numeric core for the Temple's holy talismans (mod-author-directed
// addition, not part of the original phased build — sibling to Wands/, built
// after it and following the same shape). No TaleWorlds types (fully covered
// by PureLogicTests). Runtime wiring lives in TalismanEffects.cs /
// TalismansCampaignBehavior*; the item table lives in TalismansCatalog.cs.
//
// ── What a talisman is ───────────────────────────────────────────────────
// A talisman is a small, ALWAYS-ON passive worn while carried — unlike a Wand
// (fires a spell on a landed hit, gated by a cooldown, spends charges) or the
// Holy Sigil (a real weapon you must draw and swing), a talisman does nothing
// dramatic on its own: it just quietly makes its bearer a little harder to
// kill, a little steadier, or a little more dangerous to the things it was
// blessed against. Each of the five grants exactly ONE passive bonus.
//
// ── Slot choice ──────────────────────────────────────────────────────────
// Every existing proven item template in this codebase (Crystals, the Holy
// Sigil, the Rod of the Apostle, Wands) is a ONE-HANDED WEAPON clone — that
// is the only combination verified not to fault on equip (see items.xml's
// header). Real armour slots (Cape/BodyArmor/HeadArmor/...) use a completely
// different multi-mesh, skeleton-bound asset pipeline with no proven-safe
// clone template anywhere in this mod, so reusing one for an unverified stone
// mesh would be a real crash risk for a purely cosmetic/flavour gain.
// Dark-Gift-sourced relics already solved "a passive trinket that must not
// compete with your active weapon" without needing a new slot type: they are
// PASSIVE WHILE CARRIED IN ANY OF THE FOUR WEAPON SLOTS, never required to be
// the one actually drawn (see RelicEffects.TryFindCarried). Talismans reuse
// that exact pattern — a talisman sits in one of your four weapon/carry
// slots (most loadouts do not fill all four), never needs to be wielded, and
// never displaces the sword or bow you actually fight with.
//
// ── Pricing ───────────────────────────────────────────────────────────────
// "Very expensive... matching the wands' price tier or slightly below": a
// flat price a little under WandsMath.StandardWandPriceGold (12000) — a
// talisman is a permanent, always-on passive with no charges, cooldown, or
// break chance, so it is priced firmly in the wand tier rather than at the
// Sigil's starter-relic 350.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class TalismansMath
    {
        // ── Pricing ───────────────────────────────────────────────────────────
        public const int TalismanPurchaseCostGold = 10000;

        // ── Talisman of the Unburnt Tongue — spellburn resistance ──────────────
        // Shaves flat percentage points off SpellbookMath.SpellburnChance,
        // still respecting that curve's own floor (SpellbookMath.
        // MinSpellburnChance) — the talisman steadies a shaking voice, it does
        // not make misspeaking the Fire wholly safe.
        public const float UnburntTongueSpellburnReduction = 0.12f;

        public static float ReducedSpellburnChance(float baseChance)
        {
            float reduced = baseChance - UnburntTongueSpellburnReduction;
            return reduced < SpellbookMath.MinSpellburnChance ? SpellbookMath.MinSpellburnChance : reduced;
        }

        // ── Talisman of the Ember Vigil — passive HP regen while carried ───────
        // A small heal every few seconds, comparable in cadence to a Dark
        // Gift-sourced relic aura (RelicEffects' DreadPresence tick), scaled
        // down to a steady trickle rather than a burst.
        public const float VigilHealPerTick        = 2f;
        public const float VigilTickIntervalSeconds = 4f;

        // ── Talisman of the Steadfast Line — passive morale bolster ────────────
        // A slow, continuous morale nudge upward, distinct from the Sigil's
        // on-block morale restore — this one needs no block at all, just to
        // be carried.
        public const float SteadfastMoraleGainPerTick        = 2f;
        public const float SteadfastTickIntervalSeconds       = 6f;

        // ── Talisman of the Cleansing Brand — bonus damage to demons on hit ────
        // Flat bonus damage added to the STRUCK target when it is a demon,
        // on ANY landed melee hit (not gated to a specific weapon, unlike the
        // Sigil's AoE scorch-nearby-demons proc) — a distinct mechanic from
        // both the Sigil (which hits demons standing NEAR the bearer, not
        // necessarily the target struck) and RelicMath.DemonBaneMultiplier
        // (a magic-damage multiplier applied inside SpellEffects.DamageAgent,
        // not a melee on-hit bonus).
        public const float CleansingBrandBonusDamage = 12f;

        // ── Talisman of the Last Ward — heal-back a fraction of a blocked blow ──
        // Mirrors the shape of the Dark Gift IronVeil relic (10% heal-back)
        // but themed to the Temple and pitched a little stronger, since this
        // is the talisman's whole purpose rather than one of two Dark Gift
        // effects bundled onto a relic.
        public const float LastWardBlockHealFrac = 0.15f;

        // ── Ruin loot ─────────────────────────────────────────────────────────
        // Rarer than a wand (WandsMath.RuinWandChance = 0.05) — a talisman is
        // a smaller, quieter find, but still meant to feel special.
        public const float RuinTalismanChance = 0.04f;

        public static bool RollRuinTalismanLoot(double roll01) => roll01 < RuinTalismanChance;

        // Picks which catalog talisman drops, uniform over however many exist
        // — mirrors WandsMath.PickWandIndex exactly.
        public static int PickTalismanIndex(double roll01, int talismanCount)
        {
            if (talismanCount <= 0) return -1;
            int idx = (int)(roll01 * talismanCount);
            if (idx < 0) idx = 0;
            if (idx >= talismanCount) idx = talismanCount - 1;
            return idx;
        }
    }
}
