// =============================================================================
// THE DARKEST NIGHT — CityStates/SettlementCultureNormalizer.cs
//
// Playtest fix (culture spread): a new campaign showed "most cities are Sea
// Riders" and, worse, several non-Temple cities wearing the Temple's own
// (Vlandia / "Templar") culture. Two causes:
//
//   1. ReassignImperialSettlements folds a handful of NON-Empire-culture border
//      towns (e.g. Rovalt/Ocs Hall = Vlandia) into the Empire trio's seat lists.
//      Because they then belong to a living kingdom, CityStateSystem.ConvertOwnerlessTowns
//      skips them (clan.Kingdom != null) and their culture is never touched — so
//      an Empire city kept reading as "Templar."
//   2. The towns that DID convert map their bandit culture off their ORIGINAL
//      culture, and central Calradia is overwhelmingly Empire-culture, so the
//      converted majority all became "sea_raiders."
//
// This pass makes the outcome deterministic and independent of which towns the
// conversion pipeline happened to reach:
//   • Every FACTION SEAT (the eight StartingTownIds lists) is pinned to its
//     faction's own culture — Empire trio seats → empire (this is the fix for
//     the border-town "Templar" leak), Temple seats → vlandia ("Templar", which
//     the Temple legitimately IS), Wolf Brothers → sturgia, and so on.
//   • Every other (neutral / city-state / leftover) town takes a bandit culture
//     split by its ORIGINAL regional culture (CityStateMath.BanditCultureIdFor),
//     read from a snapshot captured BEFORE any conversion rewrote it.
//   • Two deterministically-chosen neutral towns instead wear a full base
//     kingdom culture, for a little variety on an otherwise all-bandit map.
//
// The Camp / Children of the Forest sanctuaries, the Ashen realm, and player
// holdings are never touched — the sanctuaries deliberately keep their native
// (Sturgia / Battania) troop pool, exactly as CityStateSystem.CreateCityState
// already skips ApplyBanditCulture for them.
//
// Settlement.Culture is a public, non-readonly FIELD (see the header note in
// CityStateSystem.cs) — a plain assignment, no persisted id renamed, so this is
// fully save-compatible: it only rewrites the runtime culture pointer at
// new-game setup, the same field ApplyBanditCulture already writes.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal static class SettlementCultureNormalizer
    {
        // Seat settlement id -> the faction culture that seat must wear. Built once
        // from the eight StartingTownIds lists (castle ids in those lists are
        // harmless — only towns are ever looked up here).
        private static readonly Dictionary<string, string> _seatCultureById = BuildSeatMap();

        // Native (pre-conversion) culture id per town, captured before anything
        // rewrites Settlement.Culture — the geography a bandit culture is mapped
        // from. Cleared and refilled on every SnapshotNativeCultures call.
        private static readonly Dictionary<string, string> _nativeCultureById =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string> BuildSeatMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Add(string[] seats, string cultureId)
            {
                if (seats == null) return;
                foreach (var id in seats)
                    if (!string.IsNullOrEmpty(id)) map[id] = cultureId;
            }
            Add(WolfBrothersMath.StartingTownIds, "sturgia");
            Add(TowerMath.StartingTownIds,        "aserai");
            Add(ForestWidowsMath.StartingTownIds, "battania");
            Add(BloodboundMath.StartingTownIds,   "khuzait");
            Add(TempleMath.StartingTownIds,       "vlandia"); // the Temple = "Templar" (Vlandia)
            Add(EmpireMath.StartingTownIds,       "empire");
            Add(LegionMath.StartingTownIds,       "empire");
            Add(ChosenMath.StartingTownIds,       "empire");
            return map;
        }

        // Snapshot every town's ORIGINAL culture id. Must run BEFORE the first
        // conversion pass (CityStateSystem.ConvertOwnerlessTownsNow) rewrites any
        // Settlement.Culture — call it at the top of FinishNewGameWorldSetup.
        public static void SnapshotNativeCultures()
        {
            try
            {
                if (Campaign.Current == null) return;
                _nativeCultureById.Clear();
                foreach (Settlement s in Settlement.All)
                {
                    try
                    {
                        if (s == null || !s.IsTown) continue;
                        string cid = s.Culture?.StringId;
                        if (!string.IsNullOrEmpty(cid)) _nativeCultureById[s.StringId] = cid;
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Authoritative final culture pass — run at the END of new-game setup,
        // after every ownership/conversion pass has settled.
        public static void NormalizeAll()
        {
            try
            {
                if (Campaign.Current == null) return;

                var neutralTowns = new List<Settlement>();
                foreach (Settlement s in Settlement.All.ToList())
                {
                    try
                    {
                        if (s == null || !s.IsTown) continue;
                        if (s.OwnerClan == Clan.PlayerClan) continue; // never the player's own ground
                        if (IsSanctuaryOrAshen(s)) continue;

                        if (_seatCultureById.ContainsKey(s.StringId))
                        {
                            ApplyCulture(s, _seatCultureById[s.StringId]);
                            continue;
                        }
                        neutralTowns.Add(s);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                // Two deterministically-selected neutral towns wear a base culture
                // instead of a bandit one — stable ordering by StableHash so the
                // same map always picks the same pair.
                var otherPick = new HashSet<string>(
                    neutralTowns
                        .OrderBy(s => CityStateMath.StableHash(s.StringId))
                        .Take(CityStateMath.OtherCultureTownCount)
                        .Select(s => s.StringId),
                    StringComparer.OrdinalIgnoreCase);

                foreach (Settlement s in neutralTowns)
                {
                    try
                    {
                        string targetId = otherPick.Contains(s.StringId)
                            ? CityStateMath.OtherCultureIdFor(s.StringId)
                            : CityStateMath.BanditCultureIdFor(NativeCultureId(s));
                        ApplyCulture(s, targetId);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // The Camp / Children of the Forest keep their own troop culture (matched
        // exactly as CityStateSystem does — by home-settlement name and sanctuary
        // kingdom name); the Ashen realm keeps its own.
        private static bool IsSanctuaryOrAshen(Settlement s)
        {
            try
            {
                if (AshenCitySystem.IsAshenSettlement(s)) return true;

                string name = s.Name?.ToString();
                if (CityStateMath.IsRevylHomeSettlement(name) ||
                    CityStateMath.IsPenCannocHomeSettlement(name)) return true;

                string kingdomName = s.OwnerClan?.Kingdom?.Name?.ToString();
                if (kingdomName != null)
                    foreach (var sanctuary in CityStateMath.SanctuaryKingdomNames)
                        if (string.Equals(kingdomName, sanctuary, StringComparison.OrdinalIgnoreCase))
                            return true;

                return false;
            }
            // Fail SKIP — an exception here must never let the pass overwrite a
            // culture it could not prove is safe to touch.
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return true; }
        }

        private static string NativeCultureId(Settlement s) =>
            _nativeCultureById.TryGetValue(s.StringId, out var cid) ? cid : s.Culture?.StringId;

        private static void ApplyCulture(Settlement s, string cultureId)
        {
            var culture = MBObjectManager.Instance?.GetObject<CultureObject>(cultureId);
            if (culture == null) return;

            if (s.Culture != culture) s.Culture = culture;

            if (s.BoundVillages != null)
            {
                foreach (var village in s.BoundVillages)
                {
                    try
                    {
                        var vs = village?.Settlement;
                        if (vs != null && vs.Culture != culture) vs.Culture = culture;
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
        }
    }
}
