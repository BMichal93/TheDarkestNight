// =============================================================================
// THE DARKEST NIGHT — Apocalypse/ApocalypseMath.cs
//
// Pure numeric core of Phase 11, "the clock of the apocalypse" (requirements
// 32/33). No TaleWorlds types — fully covered by PureLogicTests. Runtime
// scheduling and world state live in ApocalypseCampaignBehavior.cs /
// DemonLordSystem.cs, exactly as DemonMath.cs is to DemonSpawnCampaignBehavior.
//
// Three tiers of tunable here:
//   1. Requirement 32 — the Night of the Hunt: a recurring, more violent
//      night-tide, rolled on a random interval.
//   2. Requirement 33 — the three escalation stages (rumours / the Gathering /
//      the Demon Lord), gated by campaign day.
//   3. Victory / defeat — the reachable end states once the Demon Lord walks.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class ApocalypseMath
    {
        // ── Requirement 32 — Night of the Hunt ──────────────────────────────────
        // Every 20-82 days (rolled fresh after each firing), one night-tide is
        // far worse than usual: more parties rise, each party is fuller, and a
        // few villages take a direct raid.
        public const int MinHuntIntervalDays = 20;
        public const int MaxHuntIntervalDays = 82;

        public static int RollNextHuntIntervalDays(Random rng)
        {
            if (rng == null) return MinHuntIntervalDays;
            return MinHuntIntervalDays + rng.Next(MaxHuntIntervalDays - MinHuntIntervalDays + 1);
        }

        // How much worse a Hunt night is than an ordinary one: extra night-tide
        // parties beyond the normal roll, and each of those parties is deeper.
        public const int HuntExtraParties          = 5;
        public const float HuntPartyBodyMultiplier  = 1.8f;
        public const int HuntVillagesRaided         = 3;
        // A raided village's hearth is cut to this fraction of its prior value
        // (mirrors CampaignMapEvents' Great Withering, slightly less severe).
        public const float HuntVillageHearthFraction = 0.35f;

        public static int HuntPartyBodyCount(int ordinaryBodyCount)
            => Math.Max(ordinaryBodyCount, (int)Math.Round(ordinaryBodyCount * HuntPartyBodyMultiplier));

        // ── Requirement 33 — escalation stages ──────────────────────────────────
        public const int RumoursStartDay     = 300; // stage 1: tavern rumours + portents
        public const int GatheringStartDay   = 600; // stage 2: the persistent, growing band
        public const int DemonLordEligibleDay = 1000; // stage 3: the Demon Lord may appear

        public static bool IsRumourStage(int day)    => day >= RumoursStartDay;
        public static bool IsGatheringStage(int day) => day >= GatheringStartDay;
        public static bool IsLordEligible(int day)   => day >= DemonLordEligibleDay;

        // ── Stage 2 — the Gathering ──────────────────────────────────────────────
        // A single band, spawned once at GatheringStartDay, that never despawns
        // at dawn (unlike the ordinary night tide) and grows every week toward a
        // cap, so it reads as an encroaching threat rather than an infinite one.
        public const int GatheringInitialSize   = 60;
        public const int GatheringWeeklyGrowth  = 15;
        public const int GatheringMaxSize       = 900;

        public static int GatheringSizeAfterWeeks(int weeksElapsed)
        {
            if (weeksElapsed < 0) weeksElapsed = 0;
            long grown = (long)GatheringInitialSize + (long)GatheringWeeklyGrowth * weeksElapsed;
            return (int)Math.Min(GatheringMaxSize, grown);
        }

        // ── Stage 3 — the Demon Lord ─────────────────────────────────────────────
        // Once eligible, each weekly period rolls this flat chance. ~5%/week
        // averages to roughly 20 weeks (~140 days) past day 1000 before he
        // walks — a real "late-game arrives" horizon, not a certainty at 1001.
        public const float DemonLordAppearChancePerWeek = 0.05f;

        public static bool RollDemonLordAppears(double roll) => roll < DemonLordAppearChancePerWeek;

        // Boss-tier scaling layered ON TOP of DemonMath's own Lord-tier base
        // stats (DemonMath.DemonTier.Lord) — kept here, not in DemonMath, since
        // this is specifically the Phase 11 "how much of a boss is he" knob.
        public const float DemonLordHealthMultiplier  = 6.0f;
        public const float DemonLordDamageMultiplier  = 2.2f;
        // His host: how many demon bodies his own party starts with the moment
        // he manifests, and how many more join per week while he lives and is
        // not yet defeated — his "growing Army/host."
        public const int DemonLordHostInitialSize = 250;
        public const int DemonLordHostWeeklyGrowth = 40;
        public const int DemonLordHostMaxSize      = 3000;

        public static int DemonLordHostSizeAfterWeeks(int weeksSinceAppearance)
        {
            if (weeksSinceAppearance < 0) weeksSinceAppearance = 0;
            long grown = (long)DemonLordHostInitialSize + (long)DemonLordHostWeeklyGrowth * weeksSinceAppearance;
            return (int)Math.Min(DemonLordHostMaxSize, grown);
        }

        // Requirement 20 (RelicMath.DemonBaneMultiplier, ×1.5) inverted for the
        // Demon Lord specifically: he shrugs off most of the demon-bane bonus
        // instead of taking it. Kept intentionally just above 1.0, not below —
        // "resist or reduce that bonus," not grant him extra resistance versus
        // an unenchanted hit.
        public const float DemonLordBaneMultiplier = 1.05f;

        // ── Victory / defeat ──────────────────────────────────────────────────
        // Defeat: the Demon Lord's conquest is judged to have broken the world
        // once his kingdom holds this fraction of all settlements still
        // standing, OR every one of the eight Phase 7 core factions has been
        // eliminated (by him or otherwise — the world is simply gone either way).
        public const float DefeatSettlementFraction = 0.5f;

        public static bool IsDefeatBySettlements(int settlementsHeldByLord, int totalSettlements)
        {
            if (totalSettlements <= 0) return false;
            return settlementsHeldByLord >= totalSettlements * DefeatSettlementFraction;
        }

        public static bool IsDefeatByElimination(int aliveCoreFactionCount) => aliveCoreFactionCount <= 0;
    }
}
