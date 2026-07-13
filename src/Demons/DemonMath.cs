// =============================================================================
// THE DARKEST NIGHT — Demons/DemonMath.cs
//
// Pure numeric core of THE NIGHT TIDE — the demon hordes that rise from below
// every dusk and sink back at dawn. No TaleWorlds types (fully covered by
// PureLogicTests). Runtime behaviour — spawning, battle AI, prisoner fate —
// lives in DemonSpawnCampaignBehavior.cs / DemonBattleBehavior.cs / DemonFactory.cs.
//
// Mirrors the shape of Elementals/ElementalMath.cs: identity/stat tables here,
// look in DemonVisuals, spawning in DemonFactory/DemonSpawnCampaignBehavior.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class DemonMath
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        public enum DemonTier { Fiend = 0, Stalker = 1, Ravager = 2, Hellsteed = 3 }

        // The land a demon rose under leaves a mark — a pale, hardier Snow demon;
        // a leaner, quicker Desert demon; a quieter Forest demon. Pure lookup by a
        // lower-cased culture/terrain hint, exactly like ElementalMath.WildKindForBiome.
        public enum EnvironmentVariant { Default = 0, Snow = 1, Desert = 2, Forest = 3 }

        public static EnvironmentVariant VariantForCulture(string cultureIdLower)
        {
            switch (cultureIdLower ?? string.Empty)
            {
                case "sturgia":  return EnvironmentVariant.Snow;
                case "aserai":   return EnvironmentVariant.Desert;
                case "battania": return EnvironmentVariant.Forest;
                default:         return EnvironmentVariant.Default;
            }
        }

        // ── Bodily toughness (base, before environment scaling) ─────────────────
        public static float BaseHealth(DemonTier tier)
        {
            switch (tier)
            {
                case DemonTier.Fiend:     return 70f;
                case DemonTier.Stalker:   return 100f;
                case DemonTier.Ravager:   return 150f;
                case DemonTier.Hellsteed: return 120f; // rider; the horse carries its own pool
                default:                  return 70f;
            }
        }

        // Snow demons are paler and hardier (tougher, slower); desert demons are
        // faster but frailer; forest demons are stealthier (no stat swing here —
        // their "stealth" is expressed as detection range, not raw stats).
        public static float HealthMultiplier(EnvironmentVariant v)
        {
            switch (v)
            {
                case EnvironmentVariant.Snow:   return 1.20f;
                case EnvironmentVariant.Desert: return 0.90f;
                default:                        return 1.00f;
            }
        }

        public static float SpeedMultiplier(EnvironmentVariant v)
        {
            switch (v)
            {
                case EnvironmentVariant.Snow:   return 0.90f;
                case EnvironmentVariant.Desert: return 1.20f;
                default:                        return 1.00f;
            }
        }

        // Forest demons close without being noticed — a shorter distance at which
        // a wandering party's own lookout would spot them first. Consumed by the
        // campaign layer (map "detection" is engine-controlled), kept here as the
        // single pure source of the multiplier for later wiring/tests.
        public static float DetectionRangeMultiplier(EnvironmentVariant v)
            => v == EnvironmentVariant.Forest ? 0.65f : 1.00f;

        public static float Health(DemonTier tier, EnvironmentVariant v)
            => BaseHealth(tier) * HealthMultiplier(v);

        // ── Melee bite ────────────────────────────────────────────────────────
        // Multiplies the base claw-weapon damage the equipped item already deals
        // (the cleaver in troops.xml) — a coarse per-tier "how hard this one hits"
        // knob for anything in code that wants it (e.g. balancing against relics
        // in a later phase).
        public static float MeleeDamageMultiplier(DemonTier tier)
        {
            switch (tier)
            {
                case DemonTier.Fiend:     return 1.00f;
                case DemonTier.Stalker:   return 1.25f;
                case DemonTier.Ravager:   return 1.60f;
                case DemonTier.Hellsteed: return 1.15f;
                default:                  return 1.00f;
            }
        }

        // Only Ravagers loose a working of their own — a cone of hellfire on a
        // cooldown, exactly like a Kindled looses its element (ElementalBeings).
        public static bool CastsMagic(DemonTier tier) => tier == DemonTier.Ravager;
        public const float RavagerCastCooldownSeconds = 6.5f;
        public const float RavagerCastPower           = 0.65f;

        // ── Night window ──────────────────────────────────────────────────────
        // The tide rises at dusk and sinks at dawn. Hours are CampaignTime's
        // CurrentHourInDay (0..24).
        public const float DuskHour = 20f;
        public const float DawnHour = 6f;
        public static bool IsNightHour(float hourOfDay) => hourOfDay >= DuskHour || hourOfDay < DawnHour;

        // ── Spawning (the night tide) ─────────────────────────────────────────
        // How many fresh demon parties rise on the hourly tick that crosses into
        // night, scaled so the map "crawls" without drowning the campaign in
        // parties the engine then has to simulate all day.
        public const int    MinNightSpawnParties = 3;
        public const int    MaxNightSpawnParties = 6;
        public const int    MaxLivingDemonParties = 40;

        public static int NightSpawnPartyCount(Random rng, int currentLivingParties)
        {
            if (rng == null) return MinNightSpawnParties;
            int room = Math.Max(0, MaxLivingDemonParties - currentLivingParties);
            int wanted = MinNightSpawnParties + rng.Next(MaxNightSpawnParties - MinNightSpawnParties + 1);
            return Math.Min(wanted, room);
        }

        // A band is numerous — several bodies deep — but weighted toward the weak
        // Fiend tier so a Ravager or Hellsteed reads as a real threat when it shows.
        public const int MinPartyBodies = 10;
        public const int MaxPartyBodies = 22;

        public static int PartyBodyCount(Random rng)
        {
            if (rng == null) return MinPartyBodies;
            return MinPartyBodies + rng.Next(MaxPartyBodies - MinPartyBodies + 1);
        }

        // Composition weights out of 100: mostly Fiends, some Stalkers, a knot of
        // Ravagers, a rare handful of Hellsteeds. Pure so the mix is testable.
        public static DemonTier RollTier(Random rng)
        {
            if (rng == null) return DemonTier.Fiend;
            int roll = rng.Next(100);
            if (roll < 55) return DemonTier.Fiend;      // 0..54   (55%)
            if (roll < 80) return DemonTier.Stalker;    // 55..79  (25%)
            if (roll < 93) return DemonTier.Ravager;    // 80..92  (13%)
            return DemonTier.Hellsteed;                 //  93..99  (7%)
        }

        // Where a party rises: mostly near roads/settlements so the player and
        // NPCs actually run into them, some in the deep wilds.
        public enum SpawnLocationKind { NearSettlement = 0, Road = 1, Wilderness = 2 }

        public static SpawnLocationKind RollSpawnLocation(Random rng)
        {
            if (rng == null) return SpawnLocationKind.Wilderness;
            int roll = rng.Next(100);
            if (roll < 40) return SpawnLocationKind.NearSettlement; // 40%
            if (roll < 75) return SpawnLocationKind.Road;           // 35%
            return SpawnLocationKind.Wilderness;                    // 25%
        }

        // ── Replenishment (the tide regathers) ────────────────────────────────
        // A party that survived the day regrows part of the gap to its original
        // strength at the next nightfall — never instantly full (that would make
        // whittling one down pointless), never zero (that would let one rot away
        // to nothing and stop threatening anyone).
        public const float ReplenishFractionOfGap = 0.5f;

        public static int ReplenishAmount(int currentSize, int originalSize)
        {
            int gap = originalSize - currentSize;
            if (gap <= 0) return 0;
            int amount = (int)Math.Ceiling(gap * ReplenishFractionOfGap);
            return Math.Max(1, amount);
        }

        // ── Rare settlement assault ───────────────────────────────────────────
        // Per living demon party, per night, a small chance it turns from raiding
        // the roads to actually assaulting a town or castle. Kept rare — "small
        // nightly chance," not a nightly certainty — but non-zero, so tests can
        // assert it fires "sometimes" over many rolls.
        public const float SettlementAssaultChancePerPartyPerNight = 0.02f;

        public static bool RollSettlementAssault(double roll)
            => roll < SettlementAssaultChancePerPartyPerNight;

        // ── Prisoners: no lord survives demon captivity ───────────────────────
        // Every captured hero or troop gets one on-the-spot escape roll. Heroes
        // (guarded by their own reputation, more likely to have fought free
        // before) get a slightly better chance than rank-and-file troops, but
        // both fail far more often than not — this is an execution, not a mercy.
        public const float HeroEscapeChance  = 0.18f;
        public const float TroopEscapeChance = 0.10f;

        public static bool RollCaptiveEscapes(double roll, bool isHero)
            => roll < (isHero ? HeroEscapeChance : TroopEscapeChance);

        // Aggregate roll for a whole troop-roster stack (rank-and-file, counted
        // together rather than one at a time in caller code). Returns how many of
        // `total` escape; the rest are executed.
        public static int CountEscapees(int total, Random rng, bool isHero)
        {
            if (total <= 0 || rng == null) return 0;
            int escaped = 0;
            for (int i = 0; i < total; i++)
                if (RollCaptiveEscapes(rng.NextDouble(), isHero)) escaped++;
            return escaped;
        }
    }
}
