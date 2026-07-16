// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundCatalog.cs
//
// The one new item this faction introduces: Demon Blood, a tradeable good
// vialed and bartered by the Bloodbound. Modelled byte-for-byte on the
// vanilla "wine" trade good (mesh="amphora_slim", item_category="wine",
// Type="Goods") — the closest existing "liquid trade good" template, per the
// brief's "use a wine-like texture/mesh, trade-good type." See
// ModuleData/items.xml for the matching <Item id="aae_demon_blood"> block.
// =============================================================================

namespace TheDarkestNight
{
    internal static class BloodboundCatalog
    {
        internal const string DemonBloodItemId = "aae_demon_blood";
    }
}
