// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterTroopCatalog.cs
//
// Requirement 15 — the Hollow Choir: a full recruit-to-tier-5 spellcaster
// troop tree (ModuleData/troops.xml: hollow_apprentice .. hollow_magus).
// Pure data — no TaleWorlds types — mapping each tier's troop id to the 2-3
// spells it knows, drawn from the Phase 4 SpellbookCatalog. Both
// SpellcasterTroops (battle AI) and SpellcasterTroopBehavior (seeding) read
// this; neither owns the tier list.
// =============================================================================

using System.Collections.Generic;

namespace TheDarkestNight
{
    public static class SpellcasterTroopCatalog
    {
        public struct TroopTier
        {
            public string TroopId;
            public int    Rank;   // 1 (Apprentice) .. 5 (Magus)
            public SpellId[] Spells;
        }

        // Every spell picked here is 5-9 marks (a Hollow speaks quickly in
        // battle, never the 12-20 mark Unbindings/Summon/Banish) and reads on
        // the tree's own rank: the Apprentice only mends and flickers; the
        // Magus commands the elements outright.
        public static readonly List<TroopTier> Tiers = new List<TroopTier>
        {
            new TroopTier { TroopId = "hollow_apprentice", Rank = 1,
                Spells = new[] { SpellId.SparkOfEmbers, SpellId.VeilOfAsh } },
            new TroopTier { TroopId = "hollow_adept", Rank = 2,
                Spells = new[] { SpellId.Frostbind, SpellId.SilentVeil, SpellId.WardingSigil } },
            new TroopTier { TroopId = "hollow_invoker", Rank = 3,
                Spells = new[] { SpellId.Fireball, SpellId.CursedGround, SpellId.SunderingCry } },
            new TroopTier { TroopId = "hollow_warden", Rank = 4,
                Spells = new[] { SpellId.GalesCall, SpellId.TorrentsEdge, SpellId.EmberWard } },
            new TroopTier { TroopId = "hollow_magus", Rank = 5,
                Spells = new[] { SpellId.StonerootStrike, SpellId.WailingNova, SpellId.TheLongWard } },
        };

        public static bool TryGetTier(string troopId, out TroopTier tier)
        {
            foreach (var t in Tiers)
            {
                if (t.TroopId == troopId) { tier = t; return true; }
            }
            tier = default(TroopTier);
            return false;
        }

        public static bool IsHollowChoirTroop(string troopId)
        {
            foreach (var t in Tiers)
                if (t.TroopId == troopId) return true;
            return false;
        }
    }
}
