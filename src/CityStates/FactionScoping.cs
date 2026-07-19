// =============================================================================
// THE DARKEST NIGHT — CityStates/FactionScoping.cs
//
// Phase D fix (v0.8.0 playtest issue 4): a new campaign showed no free
// city-states because CityStateSystem.OnDailyTick waited
// CityStateMath.SettleDelayDays (3 days) before its first conversion pass,
// and each faction's own ScopeToStartingTowns ejection (which is what
// orphans a town in the first place) only runs from that same daily tick.
// A player who saved and reported the bug inside day 1-3 would see every
// non-core town still sitting with its renamed core faction.
//
// This helper makes the whole faction-scoping + city-state-conversion pass
// run once, eagerly, at new-game setup (see CampaignBehavior.Events.cs'
// FinishNewGameWorldSetup), in explicit order: scope every faction down to
// its starting towns FIRST (so their orphaned clans exist), then convert
// the newly-ownerless towns into city-states. The daily tick keeps running
// afterward as the ongoing repair pass — this is only the first sweep.
//
// StripExtraFactionTowns (v0.9.x issue 5) closes the remaining gap:
// ScopeToStartingTowns only EJECTS a clan that holds none of its faction's
// seats — a clan that holds a seat AND extra towns keeps all of them, so the
// faction ends up with more cities than its named seat list. This pass strips
// those extras: every town a short-list faction holds beyond its
// StartingTownIds is handed to a landless free clan so the existing
// city-state conversion turns it into its own "wretched free town".
//
// Only the FIVE non-Empire factions are stripped. The three Empire kingdoms
// (empire / empire_w / empire_s) are DELIBERATELY expanded with border cities
// by CampaignBehavior.Events.cs' legacy ReassignImperialSettlements pass (by
// id, by name, and by radius sweep), so their "extra" towns are by design and
// have no single clean id list to check against — stripping them here would
// undo that intentional map. If the Empire trio's size is ever itself the
// problem, that belongs with ReassignImperialSettlements, not here.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace TheDarkestNight
{
    internal static class FactionScoping
    {
        public static void ScopeAllFactionsNow()
        {
            try { WolfBrothersSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TowerSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ForestWidowsSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { BloodboundSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TempleSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { EmpireSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { LegionSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ChosenSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // The five factions whose intended holdings are exactly their named seats.
        private static (string kingdomId, string[] seats)[] ShortListFactions() => new[]
        {
            ("sturgia",  WolfBrothersMath.StartingTownIds),
            ("aserai",   TowerMath.StartingTownIds),
            ("battania", ForestWidowsMath.StartingTownIds),
            ("khuzait",  BloodboundMath.StartingTownIds),
            ("vlandia",  TempleMath.StartingTownIds),
        };

        // Strips every town a short-list faction holds beyond its named seats.
        // Runs after ScopeAllFactionsNow (so landless free clans already exist from
        // the ejections) and before CityStateSystem.ConvertOwnerlessTownsNow (which
        // turns each recipient clan's new town into a city-state). Fully guarded —
        // any failure degrades to leaving a town where it was, never a crash.
        public static void StripExtraFactionTowns()
        {
            try
            {
                if (Campaign.Current == null) return;

                // Pool of recipient clans: kingdomless, alive, not the player, not
                // Ashen, not already a city-state clan, and — preferred first —
                // landless (owns nothing), so each extra becomes a clean one-city
                // state rather than being bolted onto an existing clan's holdings.
                var pool = Clan.All
                    .Where(c => c != null && !c.IsEliminated && c != Clan.PlayerClan
                             && c.Kingdom == null && c.Leader != null && c.Leader.IsAlive)
                    .Where(IsUsableRecipient)
                    .OrderBy(c => c.Settlements != null && c.Settlements.Any() ? 1 : 0) // landless first
                    .ToList();
                int next = 0;

                foreach (var (kingdomId, seats) in ShortListFactions())
                {
                    try
                    {
                        Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId && !k.IsEliminated);
                        if (kingdom == null) continue;

                        var seatSet = new HashSet<string>(seats ?? new string[0], StringComparer.OrdinalIgnoreCase);
                        var extras = kingdom.Settlements
                            .Where(s => s != null && s.IsTown && !seatSet.Contains(s.StringId))
                            .ToList();

                        foreach (Settlement town in extras)
                        {
                            try
                            {
                                if (town.OwnerClan == Clan.PlayerClan) continue;      // never the player's ground
                                if (AshenCitySystem.IsAshenSettlement(town)) continue; // the cold realm keeps its own

                                if (next >= pool.Count) return; // out of recipients — leave the rest as-is
                                Hero recipient = pool[next++].Leader;
                                ChangeOwnerOfSettlementAction.ApplyByDefault(recipient, town);
                                StabiliseTown(town);
                            }
                            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static bool IsUsableRecipient(Clan c)
        {
            try
            {
                // CRITICAL: only a proper NOBLE clan may receive a town. Handing a
                // real town to a bandit / minor / outlaw / nomad / mercenary clan
                // makes CityStateSystem.ConvertOwnerlessTowns mint a KINGDOM ruled by
                // that clan — a corrupt map state the native campaign code hard-crashes
                // on (the 2026-07-19 new-game map crash). Noble, kingdom-capable clans
                // are exactly the ejected-vassal case the city-state path already
                // handles safely.
                if (!c.IsNoble || c.IsBanditFaction || c.IsMinorFaction || c.IsOutlaw
                    || c.IsNomad || c.IsClanTypeMercenary) return false;
                if (AshenCitySystem.IsAshenClanMember(c.Leader)) return false;
                // A clan that is (or is destined to become) a city-state ruler is a poor
                // recipient — CityStateMath keys the city-state kingdom off the clan's id,
                // so folding a second, unrelated town onto it would merge them.
                string csId = CityStateMath.CityStateKingdomId(c.StringId);
                if (csId != null && Kingdom.All.Any(k => k.StringId == csId)) return false;
                return true;
            }
            catch { return true; }
        }

        // Loyalty/security top-up after a forced transfer — mirrors
        // CampaignBehavior.Events.cs' private StabiliseSettlement.
        private static void StabiliseTown(Settlement s)
        {
            if (s?.Town == null) return;
            try { s.Town.Loyalty  = 100f; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { s.Town.Security = 100f; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
