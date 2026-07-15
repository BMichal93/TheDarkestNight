// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleMath.cs
//
// Pure numeric core of Faction E — the Temple (Vlandia). No TaleWorlds types
// (fully covered by PureLogicTests). Vlandia is ALREADY "The Holy Temple" in
// the baseline AshAndEmber codebase (src/AI/TempleCulture.cs,
// AshenCitySystem.ApplyTempleCultureTexts) — this faction only ADDS the
// Phase-7-specific mechanics: town-scoping to Ocs Hall + Pravend, the Holy
// Sigil (item + battle effect + purchase), the pray menu, and the join gate
// that turns away the devious and the cruel. Runtime behaviour lives in
// TempleFactionCulture.cs (the new, additive helpers — NOT a replacement for
// the existing TempleCulture.cs), TempleSettlements.cs, TempleSigilEffects.cs
// and TempleCampaignBehavior(.Menus).cs.
//
// Mirrors the shape of HiveMath.cs / TowerMath.cs / WolfBrothersMath.cs /
// BloodboundMath.cs.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class TempleMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Temple keeps exactly two seats: Ocs Hall (town_V2) and Pravend
        // (town_V3) — verified against the shipped SandBox/ModuleData/settlements.xml
        // ("{=Settlements.Settlement.name.town_V2}Ocs Hall" /
        // "{=Settlements.Settlement.name.town_V3}Pravend").
        public static readonly string[] StartingTownIds = { "town_V2", "town_V3" }; // Ocs Hall, Pravend

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── The join gate — the Temple refuses the devious and the cruel ────────
        // Bannerlord's five personality traits are Honor, Mercy, Valor,
        // Generosity and Calculating (see DefaultTraits). There is no literal
        // "devious"/"cruel" trait, so this maps the brief's language onto the
        // closest existing ones, mirroring how ElementLordRegistry already reads
        // Calculating as "cunning" and (Honor<=-2 && Mercy<=-2) as "cruel enough
        // to serve the Ashen": a Calculating mind that schemes is devious; a
        // heart with no Mercy left is cruel. Either alone is enough to be
        // turned away — the Order does not need both vices in the same person.
        public const int DeviousCalculatingThreshold = 2;   // Calculating >= this ⇒ devious
        public const int CruelMercyThreshold          = -2; // Mercy <= this ⇒ cruel

        public static bool IsDevious(int calculating) => calculating >= DeviousCalculatingThreshold;

        public static bool IsCruel(int mercy) => mercy <= CruelMercyThreshold;

        public static bool QualifiesForTemple(int calculating, int mercy)
            => !IsDevious(calculating) && !IsCruel(mercy);

        // ── The Holy Sigil — purchase cost ──────────────────────────────────────
        // A meaningful but not absurd sum: comparable to a plain vanilla mace,
        // reflecting that the Sigil's power is in its working, not its steel.
        public const int SigilPurchaseCostGold = 350;

        // ── The Holy Sigil — battle effect ──────────────────────────────────────
        // On hit: a small burst of damage to nearby demons (the Sigil answers a
        // landed blow by scorching whatever unclean thing stands close, not by
        // hurting the thing actually struck — so it still triggers against
        // living foes). On block: a slight morale restore for the bearer.
        // Deliberately weak per-instance — this is a starter relic, not a
        // late-game trophy — but it never runs dry the way a Crystal does.
        public const float SigilOnHitDemonDamage  = 10f;
        public const float SigilOnHitDemonRadius  = 4f;   // metres
        public const float SigilOnBlockMoraleGain = 6f;

        // ── The pray menu — gated by personality, scaled by conviction ─────────
        // Lore (see Miracles/ header notes): Grace is not bestowed, it is drawn
        // through the caster's own alignment. A prayer's magnitude therefore
        // scales with the SAME virtue traits the join gate polices for — Honor
        // and Mercy — and a hero who does not clear the join gate's own
        // threshold cannot pray at all (they would not be standing in a Temple
        // town as a member in the first place, but a hero whose traits soured
        // AFTER joining is caught here too).
        public const int   PrayerMinHonorPlusMercy = 0;    // floor to attempt a prayer at all
        public const float PrayerBaseMoraleGain    = 8f;
        public const float PrayerBaseHealFraction  = 0.10f; // fraction of missing HP restored, per party member
        public const float PrayerVirtuePerPoint    = 0.15f; // extra scale per point of Honor+Mercy above the floor
        public const float PrayerMaxVirtueScale    = 2.0f;  // hard cap so a maxed-out saint isn't absurd
        public const float PrayerCooldownDays      = 1f;    // once per day

        public static bool QualifiesToPray(int honor, int mercy) => (honor + mercy) >= PrayerMinHonorPlusMercy;

        // 1.0 at the floor, scaling up with virtue above it, capped.
        public static float PrayerVirtueScale(int honor, int mercy)
        {
            int aboveFloor = (honor + mercy) - PrayerMinHonorPlusMercy;
            if (aboveFloor < 0) aboveFloor = 0;
            float scale = 1f + aboveFloor * PrayerVirtuePerPoint;
            return scale > PrayerMaxVirtueScale ? PrayerMaxVirtueScale : scale;
        }

        public static float PrayerMoraleGain(int honor, int mercy)
            => PrayerBaseMoraleGain * PrayerVirtueScale(honor, mercy);

        public static float PrayerHealFraction(int honor, int mercy)
            => PrayerBaseHealFraction * PrayerVirtueScale(honor, mercy);

        public static bool IsPrayerReady(float daysSinceLastPrayer)
            => daysSinceLastPrayer < 0f || daysSinceLastPrayer >= PrayerCooldownDays;

        // ── NPC lords pray too ───────────────────────────────────────────────
        // Small daily chance a qualifying Temple lord prays on their own,
        // mirroring MiracleCampaignBehavior's NPC Grace-use chance — Temple
        // bonuses apply to Temple lords, not only the player.
        public const double NpcDailyPrayChance = 0.10;
    }
}
