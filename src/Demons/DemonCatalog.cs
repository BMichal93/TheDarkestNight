// =============================================================================
// THE DARKEST NIGHT — Demons/DemonCatalog.cs
//
// Pure data catalog: which troops.xml id is which DemonMath.DemonTier, and
// back again. No TaleWorlds types — CharacterObject lookup by these ids
// happens in DemonFactory/DemonSpawnCampaignBehavior, never here. Mirrors the
// MiracleCatalog / CrystalCatalog / AshenRuinDefs "enum + data row" pattern.
// =============================================================================

using System.Collections.Generic;

namespace TheDarkestNight
{
    public static class DemonCatalog
    {
        // troops.xml ids added alongside "elemental_being" (see ModuleData/troops.xml).
        public const string FiendTroopId     = "demon_fiend";
        public const string StalkerTroopId   = "demon_stalker";
        public const string RavagerTroopId   = "demon_ravager";
        public const string HellsteedTroopId = "demon_hellsteed";
        // Phase 11 — the Demon Lord (see Apocalypse/DemonLordSystem.cs). Never
        // included in AllTroopIds/RollTier's pool; only DemonLordSystem ever
        // requests this id, once, deliberately.
        public const string LordTroopId      = "demon_lord";

        private static readonly Dictionary<string, DemonMath.DemonTier> _tierByTroopId =
            new Dictionary<string, DemonMath.DemonTier>(System.StringComparer.OrdinalIgnoreCase)
        {
            { FiendTroopId,     DemonMath.DemonTier.Fiend     },
            { StalkerTroopId,   DemonMath.DemonTier.Stalker   },
            { RavagerTroopId,   DemonMath.DemonTier.Ravager   },
            { HellsteedTroopId, DemonMath.DemonTier.Hellsteed },
            { LordTroopId,      DemonMath.DemonTier.Lord      },
        };

        public static bool TryGetTier(string troopId, out DemonMath.DemonTier tier)
        {
            if (troopId != null && _tierByTroopId.TryGetValue(troopId, out tier)) return true;
            tier = DemonMath.DemonTier.Fiend;
            return false;
        }

        public static bool IsDemonTroopId(string troopId) => troopId != null && _tierByTroopId.ContainsKey(troopId);

        public static string TroopIdFor(DemonMath.DemonTier tier)
        {
            switch (tier)
            {
                case DemonMath.DemonTier.Fiend:     return FiendTroopId;
                case DemonMath.DemonTier.Stalker:   return StalkerTroopId;
                case DemonMath.DemonTier.Ravager:   return RavagerTroopId;
                case DemonMath.DemonTier.Hellsteed: return HellsteedTroopId;
                case DemonMath.DemonTier.Lord:      return LordTroopId;
                default:                            return FiendTroopId;
            }
        }

        // Every troop id a demon party can ever contain — used to fall back
        // through candidates and to identify "is this troop one of ours" without
        // touching the tier dictionary directly.
        public static readonly string[] AllTroopIds =
        {
            FiendTroopId, StalkerTroopId, RavagerTroopId, HellsteedTroopId,
        };
    }
}
