// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerMath.cs
//
// Pure numeric core of Faction B — the Tower (formerly Aserai/Duneborn). No
// TaleWorlds types (fully covered by PureLogicTests). Runtime behaviour —
// culture/kingdom renaming, dialogue, town-scoping, the join ritual, and the
// "teach a formula / transmute a soldier" city menus — lives in
// TowerCulture.cs, TowerDialogue.cs, TowerSettlements.cs and
// TowerCampaignBehavior(.Menus).cs.
//
// The Tower may reference the (equally pure) SpellbookCatalog directly —
// formula length drives what a formula costs to be taught, and that is a
// Tower-owned decision, not a Spellbook one.
// =============================================================================

using System;
using System.Collections.Generic;

namespace AshAndEmber
{
    public static class TowerMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Tower keeps exactly one seat: Iyakis (town_A3). Every other town or
        // castle Aserai holds falls out of the kingdom's scope — its clan is
        // simply ejected (mirrors WolfBrothersMath.StartingTownIds exactly).
        public static readonly string[] StartingTownIds = { "town_A3" }; // Iyakis

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Teaching a spoken formula, for influence ────────────────────────────
        // A longer/more powerful formula is a deeper secret — its price scales
        // with its length (5-20 marks, per SpellbookCatalog).
        public const int   SpellOffersPerVisit        = 6;
        public const float TeachInfluenceBase          = 20f;
        public const float TeachInfluencePerFormulaMark = 8f;

        public static int SpellInfluenceCost(int formulaLength)
        {
            int len = formulaLength < SpellbookCatalog.MinFormulaLength
                ? SpellbookCatalog.MinFormulaLength
                : (formulaLength > SpellbookCatalog.MaxFormulaLength ? SpellbookCatalog.MaxFormulaLength : formulaLength);
            return (int)Math.Round(TeachInfluenceBase + len * TeachInfluencePerFormulaMark, MidpointRounding.AwayFromZero);
        }

        // Deterministic weekly refresh: the same day-bucket always yields the
        // same offer, but it changes from week to week (mirrors how a market
        // restocks rather than rerolling on every visit).
        public static int OfferSeedForDay(int dayNumber) => dayNumber / 7;

        // Fisher-Yates partial shuffle, seeded — picks up to `count` distinct
        // items from `source` without mutating it. Pure: given the same source,
        // count and seed it always returns the same subset in the same order.
        public static List<T> PickRandomSubset<T>(IReadOnlyList<T> source, int count, int seed)
        {
            var result = new List<T>();
            if (source == null || source.Count == 0 || count <= 0) return result;

            var pool = new List<T>(source);
            var rng = new Random(seed);
            int take = count < pool.Count ? count : pool.Count;
            for (int i = 0; i < take; i++)
            {
                int j = i + rng.Next(pool.Count - i);
                T tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
                result.Add(pool[i]);
            }
            return result;
        }

        // ── Transmuting a soldier into a Hollow Choir spellcaster, for influence ─
        // Any tier-2+ troop can be given to the Tower; it comes back as the
        // Hollow Choir rank matching the tier surrendered (rank 1..5, clamped),
        // so a tier-6 elite still only buys the top of the tree (rank 5), not
        // something beyond it.
        public const int MinTransmuteTier = 2;

        public static int HollowChoirRankForTier(int troopTier)
        {
            int rank = troopTier - 1;
            if (rank < 1) rank = 1;
            if (rank > 5) rank = 5;
            return rank;
        }

        public const float TransmuteInfluenceBase    = 40f;
        public const float TransmuteInfluencePerRank = 25f;

        public static int TransmuteInfluenceCost(int rank)
        {
            int r = rank < 1 ? 1 : (rank > 5 ? 5 : rank);
            return (int)Math.Round(TransmuteInfluenceBase + r * TransmuteInfluencePerRank, MidpointRounding.AwayFromZero);
        }

        // ── Lords opening the way to magic too ───────────────────────────────────
        // A Tower lord who is not already a recognised spellcaster (Requirement
        // 14's population) is granted this many spells the moment they join —
        // the closest lord-facing equivalent of the player's free spellbook
        // unlock, since SpellbookCampaignBehavior's unlock flag is player-only
        // state. See TowerCampaignBehavior.ApplyTowerJoinMagic for the call site.
        public const int LordGrantedSpellCount = 2;
    }
}
