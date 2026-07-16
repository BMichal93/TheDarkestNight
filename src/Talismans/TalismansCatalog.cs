// =============================================================================
// THE DARKEST NIGHT — Talismans/TalismansCatalog.cs
//
// Static definitions for the Temple's holy talismans — pure data, no
// TaleWorlds types, mirroring WandsCatalog's shape. Each entry binds ONE
// passive bonus (see TalismansMath.cs) to an item id; the battle/campaign
// wiring lives in TalismanEffects.cs. See ModuleData/items.xml for the
// matching <Item id="aae_talisman_*"> blocks.
//
// ── Stone base ────────────────────────────────────────────────────────────
// Every talisman clones the SAME proven one-handed-weapon template the Holy
// Sigil/Rod of the Apostle/Wands already use (body_name="bo_mace_a",
// item_usage="onehanded_block_shield_swing", physics_material="wood_weapon"
// — see items.xml's header) — but with a DIFFERENT vanilla stone mesh than
// the Sigil's "throwing_stone": mesh="projectile_rock" (vanilla item id
// "boulder", body_name="bo_projectile_rock" — a rough thrown/siege stone,
// verified present in SandBoxCore/ModuleData/items/weapons.xml). Only the
// mesh attribute changes; the collision body stays "bo_mace_a" exactly as
// the header note already documents as the known-good, swap-only-the-mesh
// technique — a different body (e.g. the vanilla boulder's own
// "bo_projectile_rock") is a thrown-weapon collision body, never verified
// paired with a one-handed melee item_usage anywhere in the shipped XML, so
// it is deliberately NOT reused here.
// =============================================================================

using System.Collections.Generic;

namespace TheDarkestNight
{
    public enum TalismanId
    {
        UnburntTongue   = 0, // spellburn resistance
        EmberVigil      = 1, // passive HP regen
        SteadfastLine   = 2, // passive morale bolster
        CleansingBrand  = 3, // bonus damage to demons on hit
        LastWard        = 4, // heal-back a fraction of a blocked blow
    }

    public struct TalismanDef
    {
        public TalismanId Id;
        public string     ItemId;   // matches ModuleData/items.xml id attribute
        public string     Name;
        public string     Lore;
    }

    public static class TalismansCatalog
    {
        private static readonly List<TalismanDef> _defs = new List<TalismanDef>
        {
            new TalismanDef
            {
                Id     = TalismanId.UnburntTongue,
                ItemId = "aae_talisman_unburnt_tongue",
                Name   = "Talisman of the Unburnt Tongue",
                Lore   = "A stone worn smooth against the throat of a novice who spoke the Fire wrong and "
                       + "lived. Carried close, it steadies a shaking voice — it does not silence the risk, "
                       + "only makes the tongue a little less likely to burn for the mistake.",
            },
            new TalismanDef
            {
                Id     = TalismanId.EmberVigil,
                ItemId = "aae_talisman_ember_vigil",
                Name   = "Talisman of the Ember Vigil",
                Lore   = "Kept warm by a coal that never quite goes out, this stone is passed hand to hand "
                       + "on the long watches before dawn. Its bearer heals a little slower toward death — "
                       + "the Vigil does not forgive a wound, it only refuses to let it close the account "
                       + "too quickly.",
            },
            new TalismanDef
            {
                Id     = TalismanId.SteadfastLine,
                ItemId = "aae_talisman_steadfast_line",
                Name   = "Talisman of the Steadfast Line",
                Lore   = "Cut from the threshold stone of a chapel that never fell, though everything "
                       + "around it did. Its bearer's resolve drifts upward on its own, slow and stubborn, "
                       + "the way a line of Templars holds after the banner-man himself has gone down.",
            },
            new TalismanDef
            {
                Id     = TalismanId.CleansingBrand,
                ItemId = "aae_talisman_cleansing_brand",
                Name   = "Talisman of the Cleansing Brand",
                Lore   = "Marked with the brand the Order presses into demon-hide after a hunt, carried now "
                       + "instead of burned. Any blow the bearer lands on one of the unclean lands a little "
                       + "harder than steel alone would manage — the Brand does not care what weapon carries "
                       + "it there.",
            },
            new TalismanDef
            {
                Id     = TalismanId.LastWard,
                ItemId = "aae_talisman_last_ward",
                Name   = "Talisman of the Last Ward",
                Lore   = "The smallest of the five, and the plainest — a stone meant to be gripped, not "
                       + "worn. A blow turned aside with it in hand gives back a sliver of what it would "
                       + "have cost, the way a held line gives ground slowly instead of breaking.",
            },
        };

        public static IReadOnlyList<TalismanDef> All => _defs;

        public static bool TryGet(TalismanId id, out TalismanDef def)
        {
            foreach (var d in _defs)
                if (d.Id == id) { def = d; return true; }
            def = default;
            return false;
        }

        public static bool TryGetByItemId(string itemId, out TalismanDef def)
        {
            def = default;
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var d in _defs)
                if (d.ItemId == itemId) { def = d; return true; }
            return false;
        }

        public static bool IsTalismanItemId(string itemId)
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
