// =============================================================================
// THE DARKEST NIGHT — CityStates/CityStateMath.cs
//
// Pure tunables and naming/lookup helpers for the wretched free towns (Phase 8,
// Requirements 11 & 24). No TaleWorlds types — see PureLogicTests for coverage.
// =============================================================================

using System;

namespace TheDarkestNight
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

        // ── Culture normalization (playtest fix: culture spread) ────────────────
        // The base (non-bandit) kingdom cultures a settlement can wear. Used two
        // ways by SettlementCultureNormalizer: as the "other culture" flavour pool
        // for the two random free towns that are NOT turned into bandit ground, and
        // as the identity set the eight factions' own seats are pinned to.
        public static readonly string[] BaseCultureIds =
            { "empire", "sturgia", "aserai", "vlandia", "battania", "khuzait" };

        // How many neutral free towns get a full base culture (a "real" kingdom
        // culture, stronger garrison) instead of a bandit culture — a little
        // variety so the map is not wall-to-wall bandit banners.
        public const int OtherCultureTownCount = 2;

        // Deterministic, process-STABLE hash (FNV-1a). String.GetHashCode is
        // randomized per process in .NET Framework, so it cannot be used anywhere
        // the same input must sort/select identically across sessions and in unit
        // tests — which the two helpers below both require.
        public static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (s != null)
                    foreach (char c in s) { h ^= c; h *= 16777619u; }
                return (int)(h & 0x7fffffff);
            }
        }

        // Deterministic base-culture pick for one of the two "other culture" free
        // towns, keyed off the settlement's own id so the choice is stable across
        // reloads and varies town to town.
        public static string OtherCultureIdFor(string settlementStringId) =>
            BaseCultureIds[StableHash(settlementStringId) % BaseCultureIds.Length];

        // ── The Camp (Requirement: Revyl special-case) ──────────────────────────
        // Wolf Brothers scopes Sturgia down to Tyal + Sibir only (see
        // WolfBrothersSettlements.ScopeToStartingTowns), so Revyl's clan is
        // ejected and would otherwise fall through the ordinary "Clan <X>"
        // city-state path above. Revyl instead gets a fixed identity: a
        // mercenary free-camp that flies every banner's colours, and none.
        //
        // Matched by settlement name (case-insensitive), not a hardcoded
        // StringId guess — confirmed at runtime by CityStateSystem against the
        // settlement actually being converted, exactly like every other
        // name-matched lookup in this codebase (see SeaCampaignBehavior's
        // harbor towns).
        public const string RevylSettlementName = "Revyl";

        public static bool IsRevylHomeSettlement(string settlementName) =>
            !string.IsNullOrEmpty(settlementName)
            && settlementName.Equals(RevylSettlementName, StringComparison.OrdinalIgnoreCase);

        public const string CampKingdomName = "The Camp";
        public const string CampInformalName = "The Camp";
        public const string CampRulerTitle = "First Among Equals";

        public const string CampEncyclopediaText =
            "No crown claims this ground, and none of the eight will bother trying. " +
            "Adventurers, deserters, and the plainly unaffiliated make their trade here — " +
            "every banner's colours pass through the gate, and none of them fly over it. " +
            "The camp keeps one law: business is business, settled before you leave, " +
            "and nobody's war is worth the coin lost minding it.";

        // Black field, white device — they fly everyone's colours by flying
        // none of their own.
        public const uint CampPrimaryColor = 0xFF141414;
        public const uint CampSecondaryColor = 0xFFF2F2F2;

        // Native banner_icons.xml — BannerIconGroup id=5 ("Sign"), first icon.
        // A plain mark rather than a house device, fitting a camp that claims
        // no lineage. See CityStateSystem.BuildCampBanner for the verified
        // Banner.CreateOneColoredBannerWithOneIcon construction.
        public const int CampBannerIconMeshId = 400;

        // ── The Children of the Forest (Pen Cannoc special-case) ────────────────
        // Forest Widows scopes Battania down to Marunath + Car Banseth only
        // (ForestWidowsMath.StartingTownIds), so Pen Cannoc's clan is ejected
        // and would otherwise fall through the ordinary "Clan <X>" city-state
        // path above — the exact same re-identification seam The Camp's Revyl
        // special-case rides. Matched by settlement name (case-insensitive,
        // tolerant of the brief's "Per Cannoc" misspelling being just that —
        // a misspelling; the real settlement is "Pen Cannoc").
        public const string PenCannocSettlementName = "Pen Cannoc";

        public static bool IsPenCannocHomeSettlement(string settlementName) =>
            !string.IsNullOrEmpty(settlementName)
            && settlementName.Equals(PenCannocSettlementName, StringComparison.OrdinalIgnoreCase);

        public const string ForestKingdomName = "Children of the Forest";
        public const string ForestInformalName = "Children of the Forest";
        public const string ForestRulerTitle = "Warden of the Strange Wood";

        public const string ForestEncyclopediaText =
            "They dwell at the edge of woods older and stranger than the Night itself, and it is there " +
            "they cut their wands. They keep no army of their own — no muster, no levy, no gate-guard " +
            "roster. Those who march on the Children come home changed, and stay to guard the trees " +
            "they once meant to burn.";

        // Deep forest green field, pale device — they answer no muster because
        // the wood itself answers for them.
        public const uint ForestPrimaryColor = 0xFF1B3A22;
        public const uint ForestSecondaryColor = 0xFFE8E4C9;

        // Native banner_icons.xml — BannerIconGroup id=3 ("Flora"), first icon
        // (id=200) — verified directly against the shipped ModuleData file.
        public const int ForestBannerIconMeshId = 200;

        // ── Sanctuary kingdoms (shared peace predicate) ──────────────────────
        // Both The Camp and the Children of the Forest are neutral ground —
        // matched by kingdom name so CityStateSystem.IsSanctuaryKingdom (and
        // the diplomacy model behind it) has one shared list to extend for any
        // future sanctuary kingdom.
        public static readonly string[] SanctuaryKingdomNames = { CampKingdomName, ForestKingdomName };

        // ── Forest lords — young adults, held there for the whole campaign ──
        // The brief asks for "16-18, Bannerlord's young-adult band." Verified
        // against TaleWorlds.CampaignSystem.dll's DefaultAgeModel:
        // HeroComesOfAge = 18 (BecomeTeenagerAge = 14, MiddleAdultHoodAge = 35)
        // — 16 is BELOW coming-of-age and would risk breaking party leadership/
        // command eligibility, which the brief itself says must never happen.
        // The window is shifted to sit AT and just past coming-of-age instead
        // (18-20): still unmistakably "young adult" against a 35+ middle-
        // adulthood baseline, never below the safety floor.
        public const float ForestLordMinAge = 18f; // == DefaultAgeModel.HeroComesOfAge
        public const float ForestLordMaxAge = 20f;

        public static bool ForestLordAgeDrifted(double currentAgeYears) =>
            currentAgeYears < ForestLordMinAge || currentAgeYears > ForestLordMaxAge;

        // Deterministic per-hero target age within the window, from a hash of
        // the hero's own StringId — stable across reloads (never re-rolled),
        // varies lord to lord so the whole ruling clan isn't one identical age.
        public static double ForestLordTargetAge(string heroStringId)
        {
            int h = (heroStringId ?? string.Empty).GetHashCode();
            if (h == int.MinValue) h = 0; else if (h < 0) h = -h;
            double t = (h % 1000) / 1000.0; // 0..1, deterministic
            return ForestLordMinAge + t * (ForestLordMaxAge - ForestLordMinAge);
        }

        // How many days to shift a hero's BirthDay forward so their age lands
        // back on targetAgeYears — mirrors AshenCitySystem.Tick.cs's "keep age
        // at 35" anchor (excessDays = (currentAge - targetAge) * 365), reused
        // here as a pure helper so it's independently testable.
        public const double DaysPerYear = 365.0;

        public static double ForestLordReanchorShiftDays(double currentAgeYears, double targetAgeYears) =>
            (currentAgeYears - targetAgeYears) * DaysPerYear;
    }
}
