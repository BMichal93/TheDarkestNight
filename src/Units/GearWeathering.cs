// =============================================================================
// THE DARKEST NIGHT — Units/GearWeathering.cs
//
// Requirement 30a: "tier 3-4 troops of all cultures get lighter, poorer, more
// improvised gear." Rather than inventing a hand-picked "shabby" item per
// culture (risking non-existent ids — behaviour.md forbids guessing), every
// armour piece worth more than UnitsMath.ShabbyArmorValueCap on a tier 3-4
// troop template is swapped for the CHEAPEST item of the same slot type that
// actually exists in the loaded game data (found live via MBObjectManager,
// exactly like AshenVisuals.FindWitchyItem's cheapest-fallback lookup).
// Tier 5 is deliberately left untouched (Requirement 30: "tier 5 stays good
// gear-wise").
//
// Applied once at OnGameInitializationFinished (the same "re-apply after
// engine reload" slot MainSubModule already uses for the culture-text
// overrides) — CharacterObject.BattleEquipments are shared templates, so
// mutating them here changes every troop spawned from that id for the rest
// of the session. Idempotent: once a slot holds a cheap item its value sits
// at or under the cap, so a second pass makes no further changes.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    internal static class GearWeathering
    {
        private static readonly EquipmentIndex[] ArmourSlots =
        {
            EquipmentIndex.Head, EquipmentIndex.Body, EquipmentIndex.Leg,
            EquipmentIndex.Gloves, EquipmentIndex.Cape
        };

        private static readonly Dictionary<ItemObject.ItemTypeEnum, ItemObject> _cheapestByType
            = new Dictionary<ItemObject.ItemTypeEnum, ItemObject>();
        private static bool _catalogueBuilt;

        // Shared with LordGearWeathering (Requirement 29) so both requirements
        // scan the item catalogue once, not twice.
        internal static ItemObject CheapestOfType(ItemObject.ItemTypeEnum type)
        {
            try
            {
                BuildCheapestCatalogue(MBObjectManager.Instance);
                return _cheapestByType.TryGetValue(type, out var item) ? item : null;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return null; }
        }

        internal static void ApplyShabbyGearToTroopTrees()
        {
            try
            {
                var mgr = MBObjectManager.Instance;
                if (mgr == null) return;
                BuildCheapestCatalogue(mgr);

                foreach (var ch in mgr.GetObjectTypeList<CharacterObject>() ?? Enumerable.Empty<CharacterObject>())
                {
                    try
                    {
                        if (ch == null || ch.IsHero) continue;
                        if (ch.Occupation != Occupation.Soldier) continue;
                        if (!UnitsMath.IsShabbyGearTier(ch.Tier)) continue;

                        foreach (var equipment in ch.BattleEquipments ?? Enumerable.Empty<Equipment>())
                        {
                            if (equipment == null) continue;
                            ShabbyEquipSet(equipment);
                        }
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ShabbyEquipSet(Equipment equipment)
        {
            foreach (var slot in ArmourSlots)
            {
                var elem = equipment[slot];
                if (elem.IsEmpty || elem.Item == null) continue;
                if (!UnitsMath.NeedsGearDowngrade(elem.ItemValue, UnitsMath.ShabbyArmorValueCap)) continue;

                if (_cheapestByType.TryGetValue(elem.Item.ItemType, out var replacement)
                    && replacement != null && replacement.StringId != elem.Item.StringId)
                {
                    equipment[slot] = new EquipmentElement(replacement);
                }
            }
        }

        // Cheapest real item of each armour ItemType, built once and reused —
        // exactly the AshenVisuals.FindWitchyItem cheapest-fallback pattern.
        private static void BuildCheapestCatalogue(MBObjectManager mgr)
        {
            if (_catalogueBuilt) return;
            _catalogueBuilt = true;
            try
            {
                var items = mgr.GetObjectTypeList<ItemObject>();
                if (items == null) return;

                // Armour types cover the tier 3-4 troop pass (Requirement 30a);
                // weapon types are only consulted by LordGearWeathering
                // (Requirement 29), which also touches weapon slots.
                var slotTypes = new[]
                {
                    ItemObject.ItemTypeEnum.HeadArmor, ItemObject.ItemTypeEnum.BodyArmor,
                    ItemObject.ItemTypeEnum.ChestArmor, ItemObject.ItemTypeEnum.Cape,
                    ItemObject.ItemTypeEnum.LegArmor, ItemObject.ItemTypeEnum.HandArmor,
                    ItemObject.ItemTypeEnum.OneHandedWeapon, ItemObject.ItemTypeEnum.TwoHandedWeapon,
                    ItemObject.ItemTypeEnum.Polearm, ItemObject.ItemTypeEnum.Bow,
                    ItemObject.ItemTypeEnum.Crossbow, ItemObject.ItemTypeEnum.Thrown,
                    ItemObject.ItemTypeEnum.Shield
                };

                foreach (var type in slotTypes)
                {
                    var cheapest = items
                        .Where(it => it != null && it.ItemType == type && it.Value > 0)
                        .OrderBy(it => it.Value)
                        .ThenBy(it => it.StringId)
                        .FirstOrDefault();
                    if (cheapest != null) _cheapestByType[type] = cheapest;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
