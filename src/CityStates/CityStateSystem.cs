// =============================================================================
// THE DARKEST NIGHT — CityStates/CityStateSystem.cs
//
// Phase 8 (Requirements 11 & 24) — "the wretched free towns."
//
// Every Phase 7 faction (Factions/*) scopes itself down to a handful of named
// starting towns and, on an unthrottled daily tick, ejects any of its own
// clans that do not hold one (ChangeKingdomAction.ApplyByLeaveKingdom — see
// e.g. ChosenSettlements.ScopeToStartingTowns). Because the 8 Phase 7
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
                .Concat(ForestWidowsMath.StartingTownIds)
                .Concat(BloodboundMath.StartingTownIds)
                .Concat(TempleMath.StartingTownIds)
                .Concat(EmpireMath.StartingTownIds)
                .Concat(LegionMath.StartingTownIds)
                .Concat(ChosenMath.StartingTownIds),
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

            // The Camp's peace stays enforced every tick, same as the
            // membership reassert above — a cheap, always-safe backstop
            // regardless of what triggered a war against/from it.
            try { ReassertCampPeace(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (!CityStateMath.ShouldBeginConversion(_daysSinceStart)) return;

            try { ConvertOwnerlessTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Session launch: rebrand a pre-existing Revyl city-state ────────────
        // A save made before this feature shipped may already hold Revyl's
        // city-state under the generic "Clan <X>" identity (or, on a fresh
        // conversion this same session, CreateCityState below already gave it
        // the right identity and this is a harmless no-op). Idempotent, fully
        // re-derived from live state — no new mandatory save keys.
        public static void OnSessionLaunched()
        {
            try { RebrandCampIfPresent(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RebrandCampIfPresent()
        {
            foreach (Kingdom kingdom in Kingdom.All.ToList())
            {
                try
                {
                    if (kingdom == null || kingdom.IsEliminated) continue;
                    if (!CityStateMath.IsCityStateKingdomId(kingdom.StringId)) continue;
                    if (kingdom.Name?.ToString() == CityStateMath.CampKingdomName) continue; // already rebranded

                    string homeName = kingdom.InitialHomeSettlement?.Name?.ToString();
                    if (!CityStateMath.IsRevylHomeSettlement(homeName)) continue;

                    ApplyCampIdentity(kingdom);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // Public so IsCampKingdom callers (the diplomacy model) have a single
        // source of truth for "is this The Camp" without duplicating the
        // name/settlement match.
        public static bool IsCampKingdom(IFaction faction)
        {
            try { return (faction as Kingdom)?.Name?.ToString() == CityStateMath.CampKingdomName; }
            catch { return false; }
        }

        // The Camp stays out of every war, in or out — see AshenDiplomacyModel
        // for the score-side discouragement; this is the belt-and-suspenders
        // backstop that actually forces peace if a war ever slips through
        // (a scheme, a quest trigger, a player declaration).
        private static void ReassertCampPeace()
        {
            Kingdom camp = Kingdom.All.FirstOrDefault(k => !k.IsEliminated && IsCampKingdom(k));
            if (camp == null) return;

            foreach (IFaction enemy in camp.FactionsAtWarWith.ToList())
            {
                try { MakePeaceAction.Apply(camp, enemy); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
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
                bool isCamp = CityStateMath.IsRevylHomeSettlement(homeSettlement.Name?.ToString());

                string kingdomName = isCamp
                    ? CityStateMath.CampKingdomName
                    : CityStateMath.CityStateKingdomName(clan.Name?.ToString());
                var culture = clan.Culture ?? homeSettlement.Culture;

                Banner banner = isCamp ? BuildCampBanner() : (clan.Banner ?? Banner.CreateRandomBanner());
                uint color1 = isCamp ? CityStateMath.CampPrimaryColor : clan.Color;
                uint color2 = isCamp ? CityStateMath.CampSecondaryColor : clan.Color2;
                string description = isCamp
                    ? CityStateMath.CampEncyclopediaText
                    : "A free town that bends its knee to no crown.";
                string rulerTitle = isCamp
                    ? CityStateMath.CampRulerTitle
                    : "Lord of " + homeSettlement.Name;

                var kingdom = Kingdom.CreateKingdom(kingdomId);
                kingdom.InitializeKingdom(
                    new TextObject(kingdomName),
                    new TextObject(kingdomName),
                    culture,
                    banner,
                    color1,
                    color2,
                    homeSettlement,
                    new TextObject(description),
                    new TextObject(kingdomName),
                    new TextObject(rulerTitle));

                ChangeKingdomAction.ApplyByCreateKingdom(clan, kingdom, false);

                if (isCamp)
                {
                    // Banner/colours must match on both the kingdom and its
                    // ruling clan — no random clan banner sitting under The
                    // Camp's black-and-white kingdom identity.
                    try
                    {
                        clan.Banner = banner;
                        clan.Color = color1;
                        clan.Color2 = color2;
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                // Requirement 24's bandit-culture reassignment (and its
                // deliberately weak two-rung troop pool) is for the ordinary
                // "wretched free towns" — Revyl reads as a mercenary hub, not
                // a bandit camp, and skipping the swap keeps its original
                // (Sturgia) troop tree so the town can actually field a
                // non-trivial garrison of its own.
                if (!isCamp) ApplyBanditCulture(homeSettlement);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Black field, white device — verified against TaleWorlds.Core.Banner:
        // CreateOneColoredBannerWithOneIcon(uint backgroundColor, uint
        // iconColor, int iconMeshId) builds a banner directly from raw ARGB
        // colours plus a native banner_icons.xml icon id, no palette-index
        // lookup involved.
        private static Banner BuildCampBanner() =>
            Banner.CreateOneColoredBannerWithOneIcon(
                CityStateMath.CampPrimaryColor,
                CityStateMath.CampSecondaryColor,
                CityStateMath.CampBannerIconMeshId);

        // Rewrites an already-existing city-state kingdom (and its ruling
        // clan) into The Camp's identity. Used both by RebrandCampIfPresent
        // (an older save that predates this feature) and is safe to call
        // repeatedly — every field it touches is simply overwritten, nothing
        // accumulates.
        //
        // Kingdom.Name/InformalName/EncyclopediaText/EncyclopediaRulerTitle/
        // Color/Color2 are all PRIVATE-set auto-properties (verified against
        // TaleWorlds.CampaignSystem.dll) — InitializeKingdom can set them at
        // creation time but nothing public can rewrite them on a live Kingdom
        // afterwards. Name/InformalName have a public Kingdom.ChangeKingdomName
        // method; the rest fall back to the same cached-backing-field
        // reflection pattern AshenCitySystem.Renaming.cs already uses for its
        // own kingdom renames (SetKingdomField). Banner is the only one of
        // these with a genuinely public setter.
        private static void ApplyCampIdentity(Kingdom kingdom)
        {
            try
            {
                var banner = BuildCampBanner();
                var nameText = new TextObject(CityStateMath.CampKingdomName);

                try { kingdom.ChangeKingdomName(nameText, nameText); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaText>k__BackingField" },
                    new TextObject(CityStateMath.CampEncyclopediaText));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField" },
                    new TextObject(CityStateMath.CampRulerTitle));
                SetKingdomColorField(kingdom, "<Color>k__BackingField", CityStateMath.CampPrimaryColor);
                SetKingdomColorField(kingdom, "<Color2>k__BackingField", CityStateMath.CampSecondaryColor);

                kingdom.Banner = banner;

                Clan ruler = kingdom.RulingClan;
                if (ruler != null)
                {
                    ruler.Banner = banner;
                    ruler.Color = CityStateMath.CampPrimaryColor;
                    ruler.Color2 = CityStateMath.CampSecondaryColor;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void SetKingdomField(Kingdom kingdom, string[] backingFieldCandidates, TextObject value)
        {
            foreach (var fieldName in backingFieldCandidates)
            {
                try
                {
                    var f = typeof(Kingdom).GetField(fieldName,
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null) { f.SetValue(kingdom, value); return; }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void SetKingdomColorField(Kingdom kingdom, string backingFieldName, uint value)
        {
            try
            {
                var f = typeof(Kingdom).GetField(backingFieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(kingdom, value);
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
