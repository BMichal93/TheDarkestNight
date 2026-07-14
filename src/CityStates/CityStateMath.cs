// =============================================================================
// THE DARKEST NIGHT — CityStates/CityStateMath.cs
//
// Pure tunables and naming/lookup helpers for the wretched free towns (Phase 8,
// Requirements 11 & 24). No TaleWorlds types — see PureLogicTests for coverage.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class CityStateMath
    {
        // How many days the campaign waits, after game start, before the first
        // conversion pass runs. Every Phase 7 faction ejects its non-starting-town
        // clans on an UNTHROTTLED daily tick (see e.g. ChosenSettlements.
        // ScopeToStartingTowns), so the ejections are already complete after the
        // very first daily tick — this delay exists purely as a safety margin so
        // Phase 8 never races the Ashen realm's own first-run settlement claims
        // (AshenCitySystem.Initialize, which can take a tick or two to settle on a
        // fresh game) into converting a settlement the Ashen were about to take.
        public const int SettleDelayDays = 3;

        public static bool ShouldBeginConversion(int daysSinceStart) => daysSinceStart >= SettleDelayDays;

        // Every city-state kingdom is keyed off its founding clan's own StringId,
        // so kingdom identity survives a save/reload with zero extra bookkeeping:
        // on any given tick we can always ask "does clan X already have a
        // city-state?" by looking for a Kingdom with this exact id, without
        // needing a separately-persisted registry.
        public const string KingdomIdPrefix = "citystate_";

        public static string CityStateKingdomId(string founderClanStringId)
        {
            if (string.IsNullOrEmpty(founderClanStringId)) return null;
            return KingdomIdPrefix + founderClanStringId;
        }

        public static bool IsCityStateKingdomId(string kingdomStringId) =>
            !string.IsNullOrEmpty(kingdomStringId)
            && kingdomStringId.StartsWith(KingdomIdPrefix, StringComparison.OrdinalIgnoreCase);

        // Naming convention (documented per Phase 8 Requirement 11): a city-state
        // keeps its settlement's own name and is identified only as "Clan
        // <RulingClanName>" — no invented dynasty lore, no special plotline.
        public static string CityStateKingdomName(string founderClanDisplayName) =>
            "Clan " + (founderClanDisplayName ?? "Unknown");

        // Requirement 24 — every city-state settlement (and its bound villages)
        // gets a Looter/Bandit culture instead of its old one, so notable
        // recruiting and garrison spawns draw from a deliberately weak troop
        // pool. Mapped by the settlement's ORIGINAL (pre-conversion) culture so
        // the flavour still reads as regionally appropriate banditry rather than
        // one generic "looters" reskin everywhere.
        public static string BanditCultureIdFor(string originalCultureId)
        {
            switch ((originalCultureId ?? string.Empty).ToLowerInvariant())
            {
                case "empire":   return "sea_raiders";     // the Empire's coasts
                case "sturgia":  return "mountain_bandits"; // the cold north
                case "vlandia":  return "forest_bandits";   // Vlandia's woodlands
                case "khuzait":  return "steppe_bandits";   // the eastern steppe
                case "aserai":   return "desert_bandits";   // the southern sands
                case "battania": return "forest_bandits";   // Battania's forests
                default:         return "looters";          // safe fallback
            }
        }
    }
}
