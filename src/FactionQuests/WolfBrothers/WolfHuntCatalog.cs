// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntCatalog.cs
//
// Pure data catalog: names and flavour text for the three named beasts of
// "The Great Hunt" (WolfHuntMath.StageCount). No TaleWorlds types. Mirrors the
// DemonCatalog/MiracleCatalog "enum index + data row" pattern.
// =============================================================================

namespace AshAndEmber
{
    public static class WolfHuntCatalog
    {
        public static string BeastName(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0: return "Long-Tooth";
                case 1: return "Cinder-Maw";
                default: return "Deep-Fang";
            }
        }

        public static string PackTitle(int stageIndex) => BeastName(stageIndex) + "'s Pack";

        public static string HuntDescription(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0:
                    return "Long-Tooth runs with a knot of lesser stalkers along the northern treeline — " +
                           "first quarry of the Great Hunt.";
                case 1:
                    return "Cinder-Maw's pack breathes hellfire and does not scatter — the second trial, " +
                           "and the first to bite back at range.";
                default:
                    return "Deep-Fang rides at the head of the largest pack the pack has ever marked — " +
                           "the last trial, and the hardest.";
            }
        }
    }
}
