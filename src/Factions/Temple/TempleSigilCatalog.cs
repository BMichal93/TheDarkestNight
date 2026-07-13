// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleSigilCatalog.cs
//
// The one new item this faction introduces: the Holy Sigil, a stone mace
// carried by the Temple's Brother Templars. Modelled on the Crystal weapon
// items' verified-safe template (mesh="throwing_stone" — "a rough held
// mineral" — over body_name="bo_mace_a", the closest stock combination to a
// hand-worked stone head, see ModuleData/items.xml header for why that
// template is the known-good one), but given real melee stats instead of the
// crystals' token 3/3 — the Sigil is a genuine weapon, not a pure effect
// trigger. See ModuleData/items.xml for the matching <Item id="aae_holy_sigil">
// block, and TempleSigilEffects.cs for the on-hit/on-block battle wiring.
// =============================================================================

namespace AshAndEmber
{
    internal static class TempleSigilCatalog
    {
        internal const string HolySigilItemId = "aae_holy_sigil";
    }
}
