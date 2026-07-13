// =============================================================================
// THE DARKEST NIGHT — Economy/ScarcityModels.cs
//
// PATH B GOLD SCARCITY (see EconomyMath.cs for the full feasibility write-up):
// every model below multiplies a vanilla gold figure down to roughly a tenth
// (EconomyMath.GoldScarcityFactor). Registered in MainSubModule.OnGameStart
// exactly like AshenDiplomacyModel / ForestClansSpeedModel — one AddModel call
// per class, each guarded by its own try/catch at the registration site.
//
//   EconomyWageModel          — troop wages + recruitment cost, ×0.1
//   EconomyTroopUpgradeModel  — tier-upgrade gold cost, ×0.1
//   EconomyBuildingModel      — town/castle boost cost, ×0.1
//   EconomyBattleRewardModel  — gold plundered from a defeated party, ×0.1
//   EconomyRansomModel        — prisoner ransom value, ×0.05 (almost nothing)
//   EconomyGarrisonModel      — Requirement 31: garrison growth and auto-
//                               recruitment roughly halved.
//   EconomyMilitiaModel       — Requirement 31: militia growth roughly halved
//                               (Fief.Militia has no public setter, so growth
//                               rate is the only clean seam — see
//                               MarketScarcityCampaignBehavior for the food-
//                               stock ceiling, which IS directly settable).
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    internal sealed class EconomyWageModel : DefaultPartyWageModel
    {
        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
        {
            var result = base.GetTotalWage(mobileParty, troopRoster, includeDescriptions);
            try
            {
                result.AddFactor(EconomyMath.GoldReductionFactor,
                    new TextObject("{=dn_barter_economy}The Barter Economy"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }

        public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            var result = base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
            try
            {
                result.AddFactor(EconomyMath.GoldReductionFactor,
                    new TextObject("{=dn_barter_economy}The Barter Economy"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }
    }

    // Phase 3 (Requirement 4) rides this SAME model class rather than a second
    // AddModel<PartyTroopUpgradeModel> registration — the campaign starter keeps
    // one model per interface, so a second implementation would silently
    // replace this one and drop the gold-scarcity factor above. See
    // Units/PromotionToll.cs for the horse+armour+weapon check this delegates to.
    internal sealed class EconomyTroopUpgradeModel : DefaultPartyTroopUpgradeModel
    {
        public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
        {
            var result = base.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);
            try
            {
                result.AddFactor(EconomyMath.GoldReductionFactor,
                    new TextObject("{=dn_barter_economy}The Barter Economy"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }

        // Requirement 4 — tier 4/5 promotion needs a horse, an armour piece, and
        // a good-price weapon in the party's saddlebags before the upgrade is
        // even offered. `characterObject` here is the troop being upgraded FROM,
        // so the toll applies if ANY of its upgrade targets lands tier 4+.
        public override bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject characterObject)
        {
            bool baseResult = base.DoesPartyHaveRequiredItemsForUpgrade(party, characterObject);
            if (!baseResult) return false;
            try
            {
                if (PromotionToll.AnyUpgradeTargetNeedsToll(characterObject))
                    return PromotionToll.HasPromotionToll(party);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return baseResult;
        }
    }

    internal sealed class EconomyBuildingModel : DefaultBuildingConstructionModel
    {
        public override int GetBoostCost(Town town)
        {
            int baseCost = base.GetBoostCost(town);
            try { return EconomyMath.ScaleGold(baseCost, EconomyMath.GoldScarcityFactor); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return baseCost; }
        }
    }

    internal sealed class EconomyBattleRewardModel : DefaultBattleRewardModel
    {
        public override int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty)
        {
            int baseAmount = base.CalculatePlunderedGoldAmountFromDefeatedParty(defeatedParty);
            try { return EconomyMath.ScaledPlunderGold(baseAmount); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return baseAmount; }
        }
    }

    internal sealed class EconomyRansomModel : DefaultRansomValueCalculationModel
    {
        public override int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null)
        {
            int baseValue = base.PrisonerRansomValue(prisoner, sellerHero);
            try { return EconomyMath.ScaledRansom(baseValue); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return baseValue; }
        }
    }

    internal sealed class EconomyGarrisonModel : DefaultSettlementGarrisonModel
    {
        public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
        {
            var result = base.CalculateBaseGarrisonChange(settlement, includeDescriptions);
            try
            {
                result.AddFactor(EconomyMath.GarrisonGrowthScale - 1f,
                    new TextObject("{=dn_thin_garrisons}Thin Garrisons"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }

        public override int GetMaximumDailyAutoRecruitmentCount(Town town)
        {
            int baseCount = base.GetMaximumDailyAutoRecruitmentCount(town);
            try { return EconomyMath.ScaledAutoRecruitmentCount(baseCount); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return baseCount; }
        }
    }

    internal sealed class EconomyMilitiaModel : DefaultSettlementMilitiaModel
    {
        public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var result = base.CalculateMilitiaChange(settlement, includeDescriptions);
            try
            {
                result.AddFactor(EconomyMath.GarrisonGrowthScale - 1f,
                    new TextObject("{=dn_thin_garrisons}Thin Garrisons"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }
    }
}
