// =============================================================================
// THE DARKEST NIGHT — CityStates/CityStateSystem.cs
//
// Phase 8 (Requirements 11 & 24) — "the wretched free towns."
//
// Every Phase 7 faction (Factions/*) scopes itself down to a handful of named
// starting towns and, on an unthrottled daily tick, ejects any of its own
// clans that do not hold one (ChangeKingdomAction.ApplyByLeaveKingdom — see
// e.g. PaleWidowsSettlements.ScopeToStartingTowns). Because the 8 Phase 7
// factions ARE the 8 vanilla kingdoms (Vlandia/Sturgia/Khuzait/Aserai/
// Battania/Empire/Empire_w/Empire_s), every clan that ends up independent
// (Clan.Kingdom == null) while still holding a town is a direct product of
// that ejection — an orphaned town with nowhere to belong.
//
// This system (modelled directly on AI/AshenCitySystem's mechanics, per the
// Phase 8 prompt) turns each such clan into its own permanent one-city
// "kingdom": a real Kingdom object so the player can join it normally
// (mercenary contract or vassalage), named "Clan <RulingClanName>", that can
// never fold back into one of the 8 core kingdoms.
//
// Kingdom identity is keyed off the founding clan's own StringId
// (CityStateMath.CityStateKingdomId), not a separately-persisted registry —
// so it is trivially reload-safe: "does clan X have a city-state?" is always
// answered by looking for a Kingdom with that exact id.
//
// Requirement 24: on conversion, the settlement (and its bound villages) has
// its Settlement.Culture field reassigned to a Looter/Bandit CultureObject
// (verified against TaleWorlds.CampaignSystem.dll — Settlement.Culture is a
// public, non-readonly FIELD, not a computed property, and Town.Culture reads
// it via its owning Settlement). Bandit cultures such as "mountain_bandits" /
// "forest_bandits" / "desert_bandits" / "steppe_bandits" / "sea_raiders" ship
// with can_have_settlement="true" and a two-rung basic/elite troop pool with
// no upgrade tree — exactly the "deliberately bad troops" the requirement
// asks for — so notable recruiting and garrison spawning both draw from that
// weak pool the moment the field changes.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public static class CityStateSystem
    {
        // Days elapsed since OnGameStart's ResetForNewGame — see CityStateMath.
        // Not saved: it only gates the FIRST conversion pass, and on a mid-game
        // load there is nothing left to gate (Phase 7 ejections already happened
        // long ago), so starting the counter over on reload is harmless — the
        // very first post-load tick simply waits SettleDelayDays again before
        // scanning, which is a no-op cost on an already-settled save.
        private static int _daysSinceStart = 0;

        public static void ResetForNewGame()
        {
            _daysSinceStart = 0;
        }

        // ── Union of every Phase 7 faction's starting-town ids ──────────────────
        // These towns stay with their core kingdom forever (Phase 7's own daily
        // ejection guarantees that) and must never be swept into a city-state.
        private static readonly HashSet<string> _coreFactionTownIds = new HashSet<string>(
            WolfBrothersMath.StartingTownIds
                .Concat(TowerMath.StartingTownIds)
                .Concat(HiveMath.StartingTownIds)
                .Concat(BloodboundMath.StartingTownIds)
                .Concat(TempleMath.StartingTownIds)
                .Concat(EmpireMath.StartingTownIds)
                .Concat(LegionMath.StartingTownIds)
                .Concat(PaleWidowsMath.StartingTownIds),
            StringComparer.OrdinalIgnoreCase);

        private static bool IsCoreFactionTown(string settlementStringId) =>
            settlementStringId != null && _coreFactionTownIds.Contains(settlementStringId);

        // ── Daily tick ───────────────────────────────────────────────────────────
        public static void OnDailyTick()
        {
            if (Campaign.Current == null) return;
            _daysSinceStart++;

            // Repair pass first: reasserts any city-state clan that vanilla
            // diplomacy (or anything else) tried to move into a different
            // kingdom. Runs every tick regardless of the settle delay — a
            // city-state that already exists must never be allowed to drift.
            try { ReassertCityStateMembership(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (!CityStateMath.ShouldBeginConversion(_daysSinceStart)) return;

            try { ConvertOwnerlessTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Kingdom-join guard ───────────────────────────────────────────────────
        // Intentionally empty, exactly like AshenCitySystem.OnClanChangedKingdom:
        // calling ApplyByLeaveKingdom from inside this event refires the same
        // event and risks a re-entrancy crash. The actual reversal happens in
        // ReassertCityStateMembership on the next daily tick instead.
        public static void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification) { }

        // Reverts any city-state clan that ended up somewhere other than its own
        // city-state kingdom (a rival kingdom's diplomacy AI invited it, or its
        // kingdom was transiently eliminated and needs reactivating). Fully
        // re-derived from live Kingdom/Clan state every call — nothing here is
        // saved, and nothing here needs to be.
        private static void ReassertCityStateMembership()
        {
            foreach (Clan clan in Clan.All.ToList())
            {
                try
                {
                    if (clan == null || clan.IsEliminated) continue;
                    if (clan == Clan.PlayerClan) continue; // never force the player's own clan around

                    string kingdomId = CityStateMath.CityStateKingdomId(clan.StringId);
                    if (kingdomId == null) continue;

                    Kingdom cityKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId);
                    if (cityKingdom == null) continue; // this clan never became a city-state

                    if (clan.Kingdom == cityKingdom) continue; // already home

                    if (cityKingdom.IsEliminated)
                        try { cityKingdom.ReactivateKingdom(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                    if (clan.Kingdom != null)
                        try { ChangeKingdomAction.ApplyByLeaveKingdom(clan, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                    bool needsRuler = cityKingdom.RulingClan == null;
                    if (needsRuler)
                        try { ChangeKingdomAction.ApplyByCreateKingdom(clan, cityKingdom, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    else
                        try
                        {
                            ChangeKingdomAction.ApplyByJoinToKingdom(
                                clan, cityKingdom, CampaignTime.Now + CampaignTime.Years(1000), false);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // Finds every ownerless (kingdomless) town outside the Phase 7 core
        // factions and the Ashen realm, and mints a brand-new one-city kingdom
        // for its owning clan. Villages bound to that town, and any other
        // settlement the same clan already holds, come along automatically —
        // they belong to the clan being moved, not enumerated individually.
        private static void ConvertOwnerlessTowns()
        {
            foreach (Settlement settlement in Settlement.All.ToList())
            {
                try
                {
                    if (settlement == null || !settlement.IsTown) continue;
                    if (IsCoreFactionTown(settlement.StringId)) continue;
                    if (AshenCitySystem.IsTargetSettlementId(settlement.StringId)) continue;

                    Clan clan = settlement.OwnerClan;
                    if (clan == null || clan.IsEliminated) continue;
                    if (clan == Clan.PlayerClan) continue; // never auto-annex the player's own holdings
                    if (clan.Kingdom != null) continue;    // still belongs to a living kingdom — not orphaned

                    string kingdomId = CityStateMath.CityStateKingdomId(clan.StringId);
                    if (kingdomId == null) continue;
                    if (Kingdom.All.Any(k => k.StringId == kingdomId)) continue; // already has (or had) one

                    CreateCityState(clan, settlement, kingdomId);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void CreateCityState(Clan clan, Settlement homeSettlement, string kingdomId)
        {
            try
            {
                string kingdomName = CityStateMath.CityStateKingdomName(clan.Name?.ToString());
                var culture = clan.Culture ?? homeSettlement.Culture;

                var kingdom = Kingdom.CreateKingdom(kingdomId);
                kingdom.InitializeKingdom(
                    new TextObject(kingdomName),
                    new TextObject(kingdomName),
                    culture,
                    clan.Banner ?? Banner.CreateRandomBanner(),
                    clan.Color,
                    clan.Color2,
                    homeSettlement,
                    new TextObject("A free town that bends its knee to no crown."),
                    new TextObject(kingdomName),
                    new TextObject("Lord of " + homeSettlement.Name));

                ChangeKingdomAction.ApplyByCreateKingdom(clan, kingdom, false);

                ApplyBanditCulture(homeSettlement);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Requirement 24 — reassigns the settlement's culture (and its bound
        // villages') to a bandit culture matching its original regional flavour.
        private static void ApplyBanditCulture(Settlement town)
        {
            try
            {
                string banditId = CityStateMath.BanditCultureIdFor(town.Culture?.StringId);
                var banditCulture = MBObjectManager.Instance?.GetObject<CultureObject>(banditId)
                                  ?? MBObjectManager.Instance?.GetObject<CultureObject>("looters");
                if (banditCulture == null) return;

                town.Culture = banditCulture;

                if (town.BoundVillages != null)
                {
                    foreach (var village in town.BoundVillages)
                    {
                        try
                        {
                            var villageSettlement = village?.Settlement;
                            if (villageSettlement != null)
                                villageSettlement.Culture = banditCulture;
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
