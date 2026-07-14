// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestArtifacts.cs
//
// The Five Vigils — pure data, no TaleWorlds types, mirroring TalismansCatalog's
// shape exactly. Each entry binds ONE item id (see ModuleData/items.xml for the
// matching <Item id="aae_temple_artifact_*"> block, cloned from the same
// verified-safe one-handed-weapon template the Holy Sigil/Talismans already
// use) to a ruin, chosen deterministically by TempleQuestMath.SelectArtifactRuins
// and assigned/tracked at runtime by TempleQuestCampaignBehavior.Artifacts.cs.
// =============================================================================

using System.Collections.Generic;

namespace AshAndEmber
{
    public struct TempleArtifactDef
    {
        public int    Index;   // 0..TempleQuestMath.ArtifactCount-1 — stable, never reordered
        public string ItemId;  // matches ModuleData/items.xml id attribute
        public string Name;
        public string Lore;
    }

    public static class TempleQuestArtifacts
    {
        private static readonly List<TempleArtifactDef> _defs = new List<TempleArtifactDef>
        {
            new TempleArtifactDef
            {
                Index  = 0,
                ItemId = "aae_temple_artifact_vigil_brand",
                Name   = "The Vigil-Brand of Ocs Hall",
                Lore   = "An iron seal, cold no matter how long it is held, said to have marked the gate of the " +
                         "first hall that ever held the dark back for a whole night. No one now living remembers " +
                         "which night that was.",
            },
            new TempleArtifactDef
            {
                Index  = 1,
                ItemId = "aae_temple_artifact_last_chalice",
                Name   = "The Chalice of the Last Light",
                Lore   = "Empty, and has been for longer than the Order's own records reach. Priests who have " +
                         "held it swear it is warmer than the room around it — whatever was once poured into it " +
                         "has never entirely left.",
            },
            new TempleArtifactDef
            {
                Index  = 2,
                ItemId = "aae_temple_artifact_ash_rosary",
                Name   = "The Rosary of Ash",
                Lore   = "Each bead is a name, worn smooth by thumbs that have long since stopped counting them. " +
                         "It is not a comfort to hold. It was never meant to be one.",
            },
            new TempleArtifactDef
            {
                Index  = 3,
                ItemId = "aae_temple_artifact_unbroken_standard",
                Name   = "The Standard of the Unbroken Vow",
                Lore   = "A banner-finial, alone — the cloth rotted away centuries ago, but the oath it flew " +
                         "over was never formally released. Whoever swore it is still, in the Order's eyes, " +
                         "sworn.",
            },
            new TempleArtifactDef
            {
                Index  = 4,
                ItemId = "aae_temple_artifact_pravend_seal",
                Name   = "The Seal of Pravend",
                Lore   = "A signet struck for a Templar Grand-Master whose name the Order will not speak aloud " +
                         "anymore — not out of shame, the old texts insist, but because saying it plainly was " +
                         "once enough to make something listen.",
            },
        };

        public static IReadOnlyList<TempleArtifactDef> All => _defs;

        public static bool TryGet(int index, out TempleArtifactDef def)
        {
            for (int i = 0; i < _defs.Count; i++)
                if (_defs[i].Index == index) { def = _defs[i]; return true; }
            def = default;
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
