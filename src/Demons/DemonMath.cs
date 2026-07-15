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
        // Lord = 4 — the Phase 11 Demon Lord. Never produced by RollTier (an
        // ordinary night-tide roll can only land on 0..3); he is spawned once,
        // deliberately, by DemonLordSystem. Kept in this same enum (rather than
        // a parallel type) so he rides through every existing demon-recognition
        // choke point for free: DemonBattleBehavior.IsDemon, the demon-bane
        // damage bonus, the visuals shroud, the "never retreat" AI.
        public enum DemonTier { Fiend = 0, Stalker = 1, Ravager = 2, Hellsteed = 3, Lord = 4 }

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
                // Base only — DemonLordSystem layers ApocalypseMath.DemonLordHealthMultiplier
                // on top of this when he is actually built, per Phase 11's "boss stat
                // multipliers" tunable. Left large even unscaled so nothing that reads
                // DemonMath.Health(Lord, ...) directly (e.g. a stray visual scale check)
                // ever sees him as a Fiend by accident.
                case DemonTier.Lord:      return 400f;
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
                case DemonTier.Lord:      return 1.60f; // further scaled by ApocalypseMath.DemonLordDamageMultiplier
                default:                  return 1.00f;
            }
        }

        // Only Ravagers and the Lord loose a working of their own — a cone of
        // hellfire on a cooldown, exactly like a Kindled looses its element
        // (ElementalBeings). The Lord alternates Fire/Spirit so his boss fight
        // reads as a creature commanding more than one working, not just a
        // bigger Ravager.
        public static bool CastsMagic(DemonTier tier) => tier == DemonTier.Ravager || tier == DemonTier.Lord;
        public const float RavagerCastCooldownSeconds = 6.5f;
        public const float RavagerCastPower           = 0.65f;
        public const float LordCastCooldownSeconds    = 4.0f;
        public const float LordCastPower              = 0.85f;

        // Which element a tier's own working takes, and how many hellfire
        // casts land between each "off-element" cast for tiers that alternate
        // (the Lord only, today). Pure so NpcCastPlanner-style callers can be
        // unit tested without touching MagicElement (defined outside this
        // assembly's pure layer) — callers translate the index to their own
        // element enum.
        public const int LordCastPatternLength = 2; // Fire, then Spirit, repeating

        // index 0 = Fire, 1 = Spirit — the caller (DemonBattleBehavior) maps
        // this onto MagicElement so DemonMath itself never needs to reference
        // a TaleWorlds/engine-adjacent enum.
        public static int LordCastPatternIndex(int castCount)
        {
            if (castCount < 0) castCount = 0;
            return castCount % LordCastPatternLength;
        }

        // ── Silhouette scale (bind-once, applied right after spawn) ────────────
        // 1.0 = ordinary human size. Kept modest for the rank-and-file tiers
        // (a subtle "this isn't quite human" cue) and pushed hard for the
        // Ravager, who also spawns on the larger demon_hulking Monster capsule
        // so the bigger silhouette is a real hitbox, not an illusion. The Lord
        // deliberately is NOT a monster of size — he is a man a half-head too
        // tall (uncanny, not bestial; see BoneWarps), and his menace lives in
        // his stats and his workings, not his frame.
        public static float VisualScale(DemonTier tier)
        {
            switch (tier)
            {
                case DemonTier.Fiend:     return 1.00f;
                case DemonTier.Stalker:   return 1.08f;
                case DemonTier.Ravager:   return 1.28f;
                case DemonTier.Hellsteed: return 1.05f; // the rider
                case DemonTier.Lord:      return 1.12f;
                default:                  return 1.00f;
            }
        }

        // Separate scale for the Hellsteed's own mount agent (the horse), so
        // the warhorse itself reads as unnaturally large under its rider.
        public const float HellsteedMountScale = 1.12f;

        // ── Monster override (the bigger, additive demon_hulking capsule) ──────
        // Null/empty means "spawn on the ordinary human Monster" — every tier
        // except the ones bulky enough to need a real (not just visual) bigger
        // hitbox. See ModuleData/monsters.xml. The Lord stays on the human
        // capsule on purpose: an uncannily man-shaped thing should FEEL like
        // fighting a man, right up until it doesn't.
        public const string HulkingMonsterId = "demon_hulking";

        public static string MonsterIdFor(DemonTier tier)
            => tier == DemonTier.Ravager ? HulkingMonsterId : null;

        // ── The warp — per-bone disfigurement ────────────────────────────────
        // Each tier's body is WRONG in its own way: not a scaled-up man but a
        // thing whose proportions never sat right. Applied through the engine's
        // own per-bone skeleton-scale channel (the same mechanism Native's
        // skeleton_scales.xml uses to fatten the Sturgian horse — vanilla ships
        // per-bone values from 0.8 to 2.1, so this range is engine-proven).
        // Pure data here; the bone-part → skeleton-bone-index mapping and the
        // MBAgentVisuals.ApplySkeletonScale call live in DemonFactory.
        //
        // Axis convention (from Native's own horse entries — the tail grows
        // LONGER via Y): Y runs along the bone, X/Z are girth.
        public enum BonePart
        {
            Head = 0, Neck = 1, SpineUpper = 2, Pelvis = 3,
            LeftArm = 4, RightArm = 5, MainHand = 6, OffHand = 7,
        }

        public struct BoneWarp
        {
            public BonePart Part;
            public float X, Y, Z;
            public BoneWarp(BonePart part, float x, float y, float z)
            { Part = part; X = x; Y = y; Z = z; }
        }

        public static BoneWarp[] BoneWarps(DemonTier tier)
        {
            switch (tier)
            {
                // The Starved — a swollen head on a wasted frame, grasping
                // overgrown hands, and arms that never grew to match each
                // other. The asymmetry is the point: lopsided reads wrong in
                // a way symmetric bulk never does.
                case DemonTier.Fiend: return new[]
                {
                    new BoneWarp(BonePart.Head,     1.20f, 1.20f, 1.20f),
                    new BoneWarp(BonePart.MainHand, 1.30f, 1.30f, 1.30f),
                    new BoneWarp(BonePart.OffHand,  1.30f, 1.30f, 1.30f),
                    new BoneWarp(BonePart.LeftArm,  1.12f, 1.18f, 1.12f),
                    new BoneWarp(BonePart.RightArm, 0.90f, 0.94f, 0.90f),
                };
                // The Long-Armed — a hunting thing: stretched neck, arms a
                // hand too long, claw-splayed hands, a chest gone gaunt.
                case DemonTier.Stalker: return new[]
                {
                    new BoneWarp(BonePart.Neck,       1.10f, 1.25f, 1.10f),
                    new BoneWarp(BonePart.LeftArm,    1.10f, 1.25f, 1.10f),
                    new BoneWarp(BonePart.RightArm,   1.10f, 1.25f, 1.10f),
                    new BoneWarp(BonePart.MainHand,   1.35f, 1.35f, 1.35f),
                    new BoneWarp(BonePart.OffHand,    1.35f, 1.35f, 1.35f),
                    new BoneWarp(BonePart.SpineUpper, 0.92f, 1.00f, 0.92f),
                };
                // The Mass — all shoulders and forelimb, a head too small for
                // the body it crowns; a thing built to break lines, not to think.
                case DemonTier.Ravager: return new[]
                {
                    new BoneWarp(BonePart.SpineUpper, 1.30f, 1.10f, 1.30f),
                    new BoneWarp(BonePart.LeftArm,    1.25f, 1.15f, 1.25f),
                    new BoneWarp(BonePart.RightArm,   1.25f, 1.15f, 1.25f),
                    new BoneWarp(BonePart.MainHand,   1.25f, 1.25f, 1.25f),
                    new BoneWarp(BonePart.OffHand,    1.25f, 1.25f, 1.25f),
                    new BoneWarp(BonePart.Head,       0.90f, 0.90f, 0.90f),
                    new BoneWarp(BonePart.Pelvis,     1.10f, 1.00f, 1.10f),
                };
                // The rider — gaunt and drawn-out, stretched thin over the
                // saddle like something pulled from its grave by the reins.
                case DemonTier.Hellsteed: return new[]
                {
                    new BoneWarp(BonePart.SpineUpper, 0.90f, 1.12f, 0.90f),
                    new BoneWarp(BonePart.Neck,       0.95f, 1.18f, 0.95f),
                    new BoneWarp(BonePart.LeftArm,    0.95f, 1.12f, 0.95f),
                    new BoneWarp(BonePart.RightArm,   0.95f, 1.12f, 0.95f),
                };
                // The Lord — the uncanny one. Where every lesser tier is openly
                // bestial, he is ALMOST a man: proportions off by a hair — a
                // neck a shade too long, arms that don't quite match, fingers
                // a knuckle past right — each within the range the eye can't
                // name but can't stop noticing. The wrongness is the horror.
                case DemonTier.Lord: return new[]
                {
                    new BoneWarp(BonePart.Neck,     1.00f, 1.08f, 1.00f),
                    new BoneWarp(BonePart.LeftArm,  1.03f, 1.05f, 1.03f),
                    new BoneWarp(BonePart.RightArm, 0.97f, 0.98f, 0.97f),
                    new BoneWarp(BonePart.MainHand, 1.08f, 1.10f, 1.08f),
                    new BoneWarp(BonePart.OffHand,  1.08f, 1.10f, 1.08f),
                };
                default: return new BoneWarp[0];
            }
        }

        // The face never rests — a permanent bared-teeth snarl (a real SandBox
        // facial-animation id, confirmed against the shipped DLLs), looped for
        // the demon's whole life. The Lord alone does not snarl: he wears a
        // gentle, unbroken smile through everything — the calmest face on the
        // field, and the wrongest.
        public static string FacialAnimation(DemonTier tier)
            => tier == DemonTier.Lord ? "convo_innocent_smile" : "convo_bared_teeth";

        // ── Unnatural movement ───────────────────────────────────────────────
        // A relative multiplier on top of the troop's own walking speed
        // (Agent.SetMaximumSpeedLimit(mult, isMultiplier: true)) — reasserted
        // on a tick, since the engine's own speed-limit hook decays. Fiends
        // and Stalkers run unnervingly fast (prey-driven, always hunting);
        // Ravagers are slower but heavier — a lurching, unstoppable mass, not
        // a sprinting one. The Lord is fast despite his bulk — wrongness, not
        // realism.
        public static float SpeedMultiplier(DemonTier tier)
        {
            switch (tier)
            {
                case DemonTier.Fiend:     return 1.10f;
                case DemonTier.Stalker:   return 1.20f;
                case DemonTier.Ravager:   return 0.95f;
                case DemonTier.Hellsteed: return 1.00f; // mounted — the horse's own gait carries this
                case DemonTier.Lord:      return 1.05f;
                default:                  return 1.00f;
            }
        }

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
