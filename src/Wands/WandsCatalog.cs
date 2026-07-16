// =============================================================================
// THE DARKEST NIGHT — Wands/WandsCatalog.cs
//
// Static definitions for magical wands — pure data, no TaleWorlds types,
// mirroring RelicCatalog's shape. Every wand binds ONE SpellbookCatalog spell
// (see Spellbook/SpellbookCatalog.cs) to an item id; the battle effect is a
// single, direct dispatch through SpellbookEffects.Cast(SpellId, Agent) — the
// same choke point the player's spoken formulas and Chosen's Rod of the
// Apostle already cast through (see ChosenRodEffects.cs).
//
// ── Selection (16 of the Spellbook's 40 spells) ─────────────────────────────
// The five elements contribute their FULL attack/wall pair (10 spells) — a
// wand's whole appeal is "hold a fixed working in your hand," and that reads
// most naturally on the elements' own clean attack/wall shapes. The Unbindings
// (the 12-mark ultimates) are deliberately excluded — a wand releasing a
// full Unbinding on a landed melee hit, gated only by a 6s cooldown, would
// dwarf everything else in the item slot and undercut the Codex investment
// the Unbindings are meant to reward. The remaining six are the most dramatic
// of the invented/demon-facing workings — Summon Demon and Banish Demons
// (the two demon-facing extremes), Light and its greater form Hearthlight
// Beacon, and two of the grander invented curses/blasts (Grave Chant, The
// Ashen Calling) — chosen for how well "a wand that does THIS on a hit" reads
// as a treasure worth 12,000-20,000 denars.
// =============================================================================

using System.Collections.Generic;
using System.Linq;

namespace TheDarkestNight
{
    public enum WandTier { Standard, Dramatic }

    public struct WandDef
    {
        public SpellId  Spell;
        public string   ItemId;   // matches ModuleData/items.xml id attribute
        public string   Name;     // "Wand of <Spell Name>"
        public WandTier Tier;
    }

    public static class WandsCatalog
    {
        private static WandDef Def(SpellId spell, string itemId, WandTier tier)
        {
            string spellName = SpellbookCatalog.Get(spell).Name;
            return new WandDef { Spell = spell, ItemId = itemId, Name = "Wand of " + spellName, Tier = tier };
        }

        private static readonly List<WandDef> _defs = new List<WandDef>
        {
            // ── The five elements — full attack/wall pairs ──────────────────
            Def(SpellId.Fireball,          "aae_wand_fireball",           WandTier.Standard),
            Def(SpellId.Firewall,          "aae_wand_firewall",           WandTier.Standard),
            Def(SpellId.GalesCall,         "aae_wand_galescall",          WandTier.Standard),
            Def(SpellId.WindwardVeil,      "aae_wand_windwardveil",       WandTier.Standard),
            Def(SpellId.StonerootStrike,   "aae_wand_stonerootstrike",    WandTier.Standard),
            Def(SpellId.Thornwall,         "aae_wand_thornwall",          WandTier.Standard),
            Def(SpellId.TorrentsEdge,      "aae_wand_torrentsedge",       WandTier.Standard),
            Def(SpellId.Mistwall,          "aae_wand_mistwall",           WandTier.Standard),
            Def(SpellId.WailingNova,       "aae_wand_wailingnova",        WandTier.Standard),
            Def(SpellId.WardOfWhispers,    "aae_wand_wardofwhispers",     WandTier.Standard),

            // ── The dramatic six ─────────────────────────────────────────────
            Def(SpellId.SummonDemon,        "aae_wand_summondemon",        WandTier.Dramatic),
            Def(SpellId.BanishDemons,       "aae_wand_banishdemons",       WandTier.Dramatic),
            Def(SpellId.Light,              "aae_wand_light",              WandTier.Dramatic),
            Def(SpellId.HearthlightBeacon,  "aae_wand_hearthlightbeacon",  WandTier.Dramatic),
            Def(SpellId.GraveChant,         "aae_wand_gravechant",         WandTier.Dramatic),
            Def(SpellId.TheAshenCalling,    "aae_wand_theashencalling",    WandTier.Dramatic),
        };

        public static IReadOnlyList<WandDef> All => _defs;

        public static bool TryGetBySpell(SpellId spell, out WandDef def)
        {
            foreach (var d in _defs)
                if (d.Spell == spell) { def = d; return true; }
            def = default;
            return false;
        }

        public static bool TryGetByItemId(string itemId, out WandDef def)
        {
            def = default;
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var d in _defs)
                if (d.ItemId == itemId) { def = d; return true; }
            return false;
        }

        public static bool IsWandItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var d in _defs)
                if (d.ItemId == itemId) return true;
            return false;
        }

        public static string[] AllItemIds()
        {
            var ids = new string[_defs.Count];
            for (int i = 0; i < _defs.Count; i++) ids[i] = _defs[i].ItemId;
            return ids;
        }
    }
}
