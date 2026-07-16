// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Chosen/ChosenQuestMath.cs
//
// Pure numeric core of "The Promise" — Faction H's (the Chosen, formerly the
// Southern Empire / the deleted Pale Widows) Phase 12 questline
// (Requirement 21). No TaleWorlds types (fully covered by PureLogicTests).
// Runtime behaviour lives in ChosenQuestCampaignBehavior.cs / ChosenQuestLog.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// The PriestKing finally speaks his founding vision plain: the angelic
// creature that named his bloodline chosen promised salvation from the
// demons — but only once the Chosen reclaim enough of Calradia in Heaven's
// name. The player helps the Chosen kingdom conquer settlements toward that
// promise. Once the threshold is reached... nothing happens. No angel
// descends, no demon vanishes early. The PriestKing's own certainty is what
// breaks: the Chosen kingdom fractures into 2-3 successor kingdoms that
// turn on each other, each now certain the OTHERS misread the vision.
//
// ── Conquest threshold — why this number ────────────────────────────────────
// Verified against the shipped SandBox/ModuleData/settlements.xml: Calradia
// has 53 towns and 67 castles map-wide — 120 total fiefs (Kingdom.Fiefs
// counts both, mirroring MortalLawCampaignBehavior.FiefCount's exact
// technique). The prompt this supersedes had the (deleted) Empire questline
// aim at "conquer 2/3 of all cities" — roughly 35 towns, a full-map-dominance
// threshold appropriate for a kingdom that starts with a normal, sprawling
// holding. The Chosen start from just ChosenMath.StartingTownIds.Length (2)
// seats — Phycaon and Lycaron — so a target on that scale would be
// unreachable inside a single campaign. ConquestFiefThreshold = 24 total
// fiefs (towns + castles) is deliberately smaller than the superseded
// two-thirds benchmark: about a fifth of the entire map, but twelve times
// the Chosen's starting footprint. That is still "a large chunk of Calradia"
// — real, sustained conquest across a long campaign, siege after siege — but
// stays within reach the way the balance-pass note demands, rather than
// gating the ending behind numbers only the base game's most successful AI
// kingdoms ever reach.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class ChosenQuestMath
    {
        // ── Conquest tracking ────────────────────────────────────────────────────
        public const int ConquestFiefThreshold = 24;

        public static bool HasReachedThreshold(int currentFiefs) => currentFiefs >= ConquestFiefThreshold;

        // Clamped 0..threshold, for the discrete journal objective bar.
        public static int ClampedProgress(int currentFiefs)
        {
            if (currentFiefs < 0) return 0;
            if (currentFiefs > ConquestFiefThreshold) return ConquestFiefThreshold;
            return currentFiefs;
        }

        // ── The split ─────────────────────────────────────────────────────────────
        // Splinter count scales with how many non-player Chosen clans actually
        // exist when the promise comes due — a kingdom that has been whittled
        // down to one clan (the PriestKing's own) has nothing left to fracture
        // INTO, so the split degenerates to "no split, the promise simply dies
        // quietly" rather than forcing a kingdom of one clan to fight itself.
        public static int SplinterCount(int nonPlayerClanCount)
        {
            if (nonPlayerClanCount <= 0) return 0;
            if (nonPlayerClanCount == 1) return 1;
            if (nonPlayerClanCount == 2) return 2;
            return 3;
        }

        // Deterministic round-robin partition: clan index 0 (always the ruling
        // clan / the PriestKing's own — callers sort the input that way) lands
        // in group 0 and, when the clan count doesn't divide evenly, group 0
        // (and then group 1, etc.) picks up the remainder — so the PriestKing's
        // own splinter is never the SMALLEST of the new kingdoms, matching the
        // "he leads the largest splinter, unable to admit the vision failed"
        // fate chosen for him.
        public static int[] AssignSplinterGroups(int clanCount, int splinterCount)
        {
            if (clanCount <= 0 || splinterCount <= 0) return new int[0];
            var result = new int[clanCount];
            for (int i = 0; i < clanCount; i++)
                result[i] = i % splinterCount;
            return result;
        }

        // Evocative, Gothic splinter identities — fractured readings of the
        // same broken faith rather than generic "Kingdom of Clan X" names.
        // Index 0 is always the PriestKing's own splinter.
        public static readonly string[] SplinterKingdomIds =
        {
            "chosen_unbrokenword", "chosen_ashenapostles", "chosen_lastvigil",
        };

        public static readonly string[] SplinterKingdomNames =
        {
            "The Unbroken Word", "The Ashen Apostles", "The Last Vigil",
        };
    }
}
