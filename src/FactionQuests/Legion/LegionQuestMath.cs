// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestMath.cs
//
// Pure numeric core of "The Far Shore" — Faction G's (Legion, Western Empire)
// Phase 12 questline (Requirement 21). No TaleWorlds types (fully covered by
// PureLogicTests). Runtime behaviour lives in LegionQuestCampaignBehavior.*/
// LegionQuestLog.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// Legion does not believe the Long Night can be won, banished, or waited out
// — "might makes right" has taught its Warlord only one lesson that still
// applies: when a position cannot be held, you leave it. He means to build an
// ark at Ortysia — Legion's own harbour seat (LegionMath.StartingTownIds) —
// stock it past any reasonable margin, and sail every soul who will follow
// him beyond the sea, out from under the dark entirely. The player feeds that
// stockpile; per the brief, this REPURPOSES NorthmenStonesCampaignBehavior's
// exact "gather resources in one named town, tracked as a running total,
// stolen if the town falls" machinery (NorthmenStonesMath.cs/
// NorthmenStonesCampaignBehavior.NpcContribution.cs) rather than inventing a
// new one — this file mirrors that shape deliberately.
//
// ── Resource choice — why Hardwood and Iron, not five tracks ────────────────
// NorthmenStonesMath spreads its own working across five civilian materials
// plus a hard Kindled track because the Bonefire Circle is a working of
// stone AND fire AND blood. An ark is a hull: it needs timber for the ribs,
// keel, and decking, and iron for the fittings, nails, and anchor chain —
// two tracks, not five, keeps this an honest "build a ship," not a grab-bag
// of unrelated dressing (Working style: prefer the simpler, working design).
// Both item ids ("hardwood", "iron") are the exact strings NorthmenStones-
// CampaignBehavior.Menu.cs already resolves through MBObjectManager, so they
// are known-good against the shipped item catalog.
//
// ── Targets — why "very significant," concretely ────────────────────────────
// Legion is scoped to exactly TWO towns (LegionMath.StartingTownIds — Lageta
// and Ortysia), the same narrow economic base TowerRiteMath/BloodboundQuestMath
// already reason against for their own two-seat factions. NorthmenStonesMath's
// own targets (5,000 Iron / 5,000 Hardwood) were sized for Sturgia's much
// broader ~7-town economy (verified against the shipped settlements.xml: 7
// towns carry Culture.sturgia at game start) and are reachable there in
// "roughly 2 dedicated in-game years" per that file's own header, helped
// heavily by NPC lords donating from SEVEN separate fiefs' worth of trade.
// Scaling down 1-for-1 by town count (2/7 ≈ 29%) would undersell "a very
// significant stock" — Legion's own culture doesn't farm, it raids
// (LegionCulture.cs's own lore: "There is no harvest here worth the name")
// — so its lords have less of a background trickle to lean on than the
// Northmen's settled economy, meaning the PLAYER carries relatively more of
// the load. To keep that honest without turning the grind purely theoretical,
// the targets sit at roughly HALF of NorthmenStonesMath's own two civilian
// tracks rather than a strict population-share fraction — still a multi-year
// undertaking on a raiding economy, not a rounding error:
//   HardwoodTarget = 2,500 (half of NorthmenStonesMath.HardwoodTarget)
//   IronTarget     = 2,500 (half of NorthmenStonesMath.IronTarget)
// Both are tradeable Goods items (Phase 2's barter economy), so a determined
// player can trade for them as well as haul them personally — exactly the
// same "hard, not theoretical" balance NorthmenStonesMath's own header
// reasons for its targets.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class LegionQuestMath
    {
        // ── The ark's stock ──────────────────────────────────────────────────────
        public const int HardwoodTarget = 2_500;
        public const int IronTarget     = 2_500;

        public static float ClampedRatio(double current, double target)
        {
            if (target <= 0) return 1f;
            float r = (float)(current / target);
            if (r < 0f) r = 0f;
            if (r > 1f) r = 1f;
            return r;
        }

        // Mean of the two clamped ratios — the "how close is the ark" gauge used
        // for the donation menu header and the journal progress bar.
        public static float BlendedProgress(int hardwood, int iron)
            => (ClampedRatio(hardwood, HardwoodTarget) + ClampedRatio(iron, IronTarget)) / 2f;

        // Completion requires BOTH tracks individually full, not just a high
        // blended average — mirrors NorthmenStonesMath.IsMaterialsComplete.
        public static bool IsStockComplete(int hardwood, int iron)
            => hardwood >= HardwoodTarget && iron >= IronTarget;

        // ── Theft when Ortysia falls ─────────────────────────────────────────────
        // "Materials are persistent... unless the city is captured by a different
        // faction, in which case they disappear by 10 percent per week" — the
        // EXACT NorthmenStonesMath.DecayFactorPerWeek figure, deliberately not
        // softened (the balance-pass note calls this out as a required
        // mechanic, not optional flavour). Applies every week Ortysia is not
        // Legion's, regardless of who holds it instead — a rival kingdom's
        // siege and the Demon Lord's own late-game conquest
        // (DemonLordSystem.WeeklyTick besieges "s.IsTown || s.IsCastle" with no
        // distinction between the two) both change Settlement.MapFaction away
        // from Legion identically, so this one check already covers both capture
        // paths the brief calls out without needing to special-case either.
        public const float DecayFactorPerWeek = 0.90f;

        public static int ApplyWeeklyDecay(int amount)
        {
            if (amount <= 0) return 0;
            int decayed = (int)(amount * DecayFactorPerWeek);
            return decayed < 0 ? 0 : decayed;
        }

        // ── NPC lords contributing in the background ─────────────────────────────
        // A trickle, not the primary driver, and deliberately smaller than
        // NorthmenStonesMath's own NpcContributionAmount range — Legion's
        // "steal rather than produce" economy means its lords have less spare
        // stock to hand over than a settled kingdom's.
        public const float NpcWeeklyContributionChance = 0.12f;

        public static int NpcContributionAmount(Random rng, int min, int max)
        {
            if (rng == null || max <= min) return 0;
            return min + rng.Next(max - min + 1);
        }

        // ── Ending (b): the departing clans ──────────────────────────────────────
        // "A few random clans leave" — 2 or 3, evenly split, mirroring
        // TowerRiteMath.HostPartyCount's "small, readable count" reasoning
        // rather than a wide random range that could read as either "nobody
        // cared" (1) or "the kingdom collapsed" (a large roll).
        public const int MinDepartingClans = 2;
        public const int MaxDepartingClans = 3;

        public static int DepartingClanCount(double roll01)
            => roll01 < 0.5 ? MinDepartingClans : MaxDepartingClans;
    }
}
