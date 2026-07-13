// =============================================================================
// THE DARKEST NIGHT — Economy/MarketScarcityCampaignBehavior.cs
//
// Requirement 3 — town/village market scarcity, and the food/militia half of
// Requirement 31 (garrison size itself is handled by EconomyGarrisonModel in
// ScarcityModels.cs). Runs on the daily tick, the same rate ElementalWildsBehavior
// and MiracleCampaignBehavior use for their own housekeeping.
//
// Town markets are pruned every day: coin is capped low, food-for-sale is cut
// to a trickle, weapons above a crude tier vanish entirely and what crude
// weapons remain are thinned, and horses are all but gone from the stalls.
// Village markets get the opposite treatment for food — boosted, so that
// villages become the real breadbasket the prompt calls for and traveling to
// them for supplies actually matters. Town food stocks and militia are capped
// (not zeroed) against the settlement's own Prosperity so poor towns don't
// look identical to rich ones, but every town sits at roughly half of where
// vanilla equilibrium would otherwise land — a ceiling, not a decay, so we
// never fight the base game's own daily change into an exponential crash.
//
// No state here needs to survive a save: the adjustment is re-derived fresh
// every day from whatever the settlement currently holds, so SyncData is a
// no-op (nothing to persist, matching TavernCampaignBehavior-style behaviors
// that carry no save-relevant fields).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AshAndEmber
{
    public class MarketScarcityCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Nothing persisted — see header comment.
        }

        private void OnDailyTick()
        {
            try
            {
                foreach (Settlement s in Settlement.All)
                {
                    try
                    {
                        if (s == null) continue;
                        if (s.IsTown && s.Town != null) ThinTownMarket(s.Town);
                        else if (s.IsVillage && s.Village != null) BoostVillageFood(s.Village);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ThinTownMarket(Town town)
        {
            // Poor traders. Gold has no public setter (SettlementComponent.Gold's
            // set accessor is private) — ChangeGold(delta) is the exposed seam.
            try
            {
                if (town.Gold > EconomyMath.TownTraderGoldCap)
                    town.ChangeGold(EconomyMath.TownTraderGoldCap - town.Gold);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // Half-starved food stores (Requirement 31). Militia has no public
            // setter (Fief.Militia is get-only) so its half is enforced instead
            // by slowing growth — see EconomyMilitiaModel in ScarcityModels.cs.
            try { town.FoodStocks = Math.Min(town.FoodStocks, EconomyMath.MaxFoodStocks(town.Prosperity)); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            AdjustRoster(town.Settlement?.ItemRoster, isVillage: false);
        }

        private static void BoostVillageFood(Village village)
        {
            AdjustRoster(village.Settlement?.ItemRoster, isVillage: true);
        }

        private static void AdjustRoster(ItemRoster roster, bool isVillage)
        {
            if (roster == null) return;
            try
            {
                // Snapshot first: mutating an ItemRoster mid-enumeration is unsafe.
                var snapshot = new List<(ItemObject item, int amount)>();
                for (int i = 0; i < roster.Count; i++)
                {
                    ItemObject item = roster.GetItemAtIndex(i);
                    int amount = roster.GetElementNumber(i);
                    if (item != null && amount > 0) snapshot.Add((item, amount));
                }

                foreach (var (item, amount) in snapshot)
                {
                    int target = amount;
                    if (item.HasFoodComponent || item.IsFood)
                        target = isVillage
                            ? EconomyMath.VillageFoodQuantity(amount)
                            : EconomyMath.TownFoodSaleQuantity(amount);
                    else if (!isVillage && item.HasHorseComponent)
                        target = EconomyMath.TownHorseSaleQuantity(amount);
                    else if (!isVillage && item.HasWeaponComponent)
                        target = EconomyMath.TownWeaponSaleQuantity(amount, (int)item.Tier);
                    else
                        continue;

                    int delta = target - amount;
                    if (delta != 0) roster.AddToCounts(item, delta);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
