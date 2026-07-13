// =============================================================================
// THE DARKEST NIGHT — Units/PromotionToll.cs
//
// Requirement 4: promoting a troop into tier 4 or 5 costs a horse, an armour
// piece, and a good-price weapon out of the player's own saddlebags — the
// same idea vanilla already applies to cavalry (a horse from the party's item
// roster), extended to every high-tier line. Thresholds live in UnitsMath;
// this file only touches TaleWorlds types (party roster, item lookup).
//
// Gate: EconomyTroopUpgradeModel.DoesPartyHaveRequiredItemsForUpgrade (see
// Economy/ScarcityModels.cs) calls AnyUpgradeTargetNeedsToll/HasPromotionToll
// here before letting the party-screen offer the upgrade at all.
// Consume: PromotionCampaignBehavior listens to PlayerUpgradedTroopsEvent and
// calls ConsumePromotionToll once the upgrade has actually happened.
//
// Requirement 30b rides the same plumbing: recruiting a tier-5 troop straight
// out of the prisoner cage costs an armour piece and a good weapon (no horse —
// see UnitsRecruitModel in RecruitToll.cs).
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AshAndEmber
{
    internal static class PromotionToll
    {
        // True when at least one of this troop's upgrade targets lands in the
        // tolled tiers — DoesPartyHaveRequiredItemsForUpgrade is only handed the
        // troop being upgraded FROM, not the chosen target.
        internal static bool AnyUpgradeTargetNeedsToll(CharacterObject source)
        {
            if (source?.UpgradeTargets == null) return false;
            try
            {
                return source.UpgradeTargets.Any(t => t != null && UnitsMath.RequiresPromotionToll(t.Tier));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return false; }
        }

        // ── Gate: does the party carry a full toll (horse + armour + good weapon)? ──
        internal static bool HasPromotionToll(PartyBase party)
        {
            try
            {
                var roster = party?.ItemRoster;
                if (roster == null) return false;
                return HasHorse(roster) && HasArmourPiece(roster) && HasGoodPriceWeapon(roster);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return false; }
        }

        // Requirement 30b: armour + good weapon, no horse required.
        internal static bool HasRecruitToll(PartyBase party)
        {
            try
            {
                var roster = party?.ItemRoster;
                if (roster == null) return false;
                return HasArmourPiece(roster) && HasGoodPriceWeapon(roster);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return false; }
        }

        private static bool IsArmourType(ItemObject.ItemTypeEnum t)
            => t == ItemObject.ItemTypeEnum.HeadArmor
            || t == ItemObject.ItemTypeEnum.BodyArmor
            || t == ItemObject.ItemTypeEnum.ChestArmor
            || t == ItemObject.ItemTypeEnum.Cape
            || t == ItemObject.ItemTypeEnum.LegArmor
            || t == ItemObject.ItemTypeEnum.HandArmor;

        private static bool IsWeaponType(ItemObject.ItemTypeEnum t)
            => t == ItemObject.ItemTypeEnum.OneHandedWeapon
            || t == ItemObject.ItemTypeEnum.TwoHandedWeapon
            || t == ItemObject.ItemTypeEnum.Polearm
            || t == ItemObject.ItemTypeEnum.Bow
            || t == ItemObject.ItemTypeEnum.Crossbow
            || t == ItemObject.ItemTypeEnum.Thrown;

        private static bool HasHorse(ItemRoster roster)
            => roster.Any(e => !e.IsEmpty && e.EquipmentElement.Item != null
                             && e.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Horse);

        private static bool HasArmourPiece(ItemRoster roster)
            => roster.Any(e => !e.IsEmpty && e.EquipmentElement.Item != null
                             && IsArmourType(e.EquipmentElement.Item.ItemType));

        private static bool HasGoodPriceWeapon(ItemRoster roster)
            => roster.Any(e => !e.IsEmpty && e.EquipmentElement.Item != null
                             && IsWeaponType(e.EquipmentElement.Item.ItemType)
                             && UnitsMath.IsGoodPriceWeapon(e.EquipmentElement.ItemValue));

        // ── Consumption ──────────────────────────────────────────────────────────
        // Spends the cheapest qualifying item of each kind first (mirrors
        // AshenRecruitCampaignBehavior's "cheapest prisoner first" rule) so a
        // player is not punished for happening to be carrying their very best
        // gear when a promotion goes through.
        internal static void ConsumePromotionToll(PartyBase party, int count)
        {
            if (party == null || count <= 0) return;
            try
            {
                var roster = party.ItemRoster;
                if (roster == null) return;
                for (int i = 0; i < count; i++)
                {
                    if (!HasPromotionToll(party)) break; // best effort — stop if the party ran dry
                    ConsumeCheapest(roster, e => e.EquipmentElement.Item != null
                        && e.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Horse);
                    ConsumeCheapest(roster, e => e.EquipmentElement.Item != null
                        && IsArmourType(e.EquipmentElement.Item.ItemType));
                    ConsumeCheapest(roster, e => e.EquipmentElement.Item != null
                        && IsWeaponType(e.EquipmentElement.Item.ItemType)
                        && UnitsMath.IsGoodPriceWeapon(e.EquipmentElement.ItemValue));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static void ConsumeRecruitToll(PartyBase party, int count)
        {
            if (party == null || count <= 0) return;
            try
            {
                var roster = party.ItemRoster;
                if (roster == null) return;
                for (int i = 0; i < count; i++)
                {
                    if (!HasRecruitToll(party)) break;
                    ConsumeCheapest(roster, e => e.EquipmentElement.Item != null
                        && IsArmourType(e.EquipmentElement.Item.ItemType));
                    ConsumeCheapest(roster, e => e.EquipmentElement.Item != null
                        && IsWeaponType(e.EquipmentElement.Item.ItemType)
                        && UnitsMath.IsGoodPriceWeapon(e.EquipmentElement.ItemValue));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ConsumeCheapest(ItemRoster roster,
            Func<ItemRosterElement, bool> qualifies)
        {
            ItemObject cheapest = null;
            int cheapestValue = int.MaxValue;
            foreach (var e in roster)
            {
                if (e.IsEmpty || !qualifies(e)) continue;
                int value = e.EquipmentElement.ItemValue;
                if (value < cheapestValue)
                {
                    cheapestValue = value;
                    cheapest = e.EquipmentElement.Item;
                }
            }
            if (cheapest != null) roster.AddToCounts(cheapest, -1);
        }
    }
}
