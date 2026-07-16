// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestMath.cs
//
// Pure numeric core of "The Final Peace" — the Forest Widows' (Battania)
// Phase 12 questline. No TaleWorlds types (fully covered by PureLogicTests).
// Runtime behaviour lives in ForestWidowsQuestCampaignBehavior.*/
// ForestWidowsQuestLog.cs. Repurposes GreatAwakeningMath's accumulate-toward-
// a-threshold shape (see GreatAwakening/GreatAwakeningMath.cs).
//
// ── Premise ───────────────────────────────────────────────────────────────
// The Widows already sell the demons borrowed days, one soldier at a time
// (ForestWidowsMath.IgnoreDaysPerSoldierSacrificed — see
// Factions/ForestWidows/ForestWidowsMath.cs). The Grand Widow's court has
// come to believe the bargain can be made permanent: not another season of
// being overlooked, but the end of the ledger itself. The player helps the
// Widows feed that larger, one-time offering. Once it is met, the twist:
// the peace is real — bought by the Widows (and, if the player stays, the
// player) ceasing to be anything the demons need to fight at all.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class ForestWidowsQuestMath
    {
        // ── The sacrifice threshold — "enough sacrificed men will stop the
        // demons, not just days at a time" ──────────────────────────────────────
        //
        // The existing altar (ForestWidowsMath) already sells daily reprieve at
        // a literal 1-soldier-per-day rate. A LASTING peace has to dwarf what
        // any amount of day-buying could add up to across a single campaign —
        // the Widows are not haggling for another season, they are trying to
        // buy themselves out of the war entirely.
        //
        // Great Awakening's Duneborn sacrifice ten thousand lives to drag
        // something ancient bodily into Calradia — an act of creation, from a
        // kingdom that starts with the game's normal, sprawling holding. The
        // Forest Widows are explicitly scoped to just two seats
        // (ForestWidowsMath.StartingTownIds — Marunath, Car Banseth) and are
        // not creating anything, only paying off a debt that already has a
        // known unit price. SacrificeTarget = 3,000 is deliberately smaller
        // than the Great Awakening's ten thousand — under a third of it —
        // because a two-town scarcity economy simply cannot plausibly bleed at
        // Duneborn's scale within one campaign. It is still enormous against
        // that economy: at the existing altar's 1-day-per-soldier rate, 3,000
        // lives would only buy roughly eight in-game years of borrowed peace
        // fed one soldier at a time. This buys it outright, forever — which is
        // why it costs an order of magnitude more than any single-campaign
        // string of day-by-day bargains through the old altar could
        // realistically accumulate.
        public const int SacrificeTarget = 3000;

        public static bool HasReachedThreshold(int menSacrificed) => menSacrificed >= SacrificeTarget;

        // Clamped 0..threshold, for the discrete journal objective bar.
        public static int ClampedProgress(int menSacrificed)
        {
            if (menSacrificed < 0) return 0;
            if (menSacrificed > SacrificeTarget) return SacrificeTarget;
            return menSacrificed;
        }

        // ── NPC contribution — background trickle, never the primary driver ──
        // Mirrors GreatAwakeningMath.NpcWeeklyContributionChance/Amount, tuned
        // a little richer per-lord since the Forest Widows kingdom holds far
        // fewer lords than Duneborn's whole sprawling realm ever did — without
        // this the NPC trickle would be negligible against a 3,000 target.
        public const float NpcWeeklyContributionChance = 0.20f;
        public const int   NpcContributionMin = 5;
        public const int   NpcContributionMax = 20;

        public static int NpcContributionAmount(Random rng, int prisonersHeld)
        {
            if (rng == null || prisonersHeld <= 0) return 0;
            int roll = NpcContributionMin + rng.Next(NpcContributionMax - NpcContributionMin + 1);
            return roll < prisonersHeld ? roll : prisonersHeld;
        }

        // ── Garrison replacement — the demon-thrall occupation force ────────────
        // A fixed-size mixed garrison dropped into every settlement the Forest
        // Widows still hold the moment the ending fires. Not scaled to the
        // town's own garrison capacity — the point is a hard, readable "this
        // town belongs to the dark now" wall, not a vanilla-balanced defense.
        public const int GarrisonFiends   = 40;
        public const int GarrisonStalkers = 15;
    }
}
