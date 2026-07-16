// =============================================================================
// THE DARKEST NIGHT — Units/LordGearWeathering.cs
//
// Requirement 29: "Lords' equipment gets de-blinged at session launch: strip
// gold/ornate/rich items, re-dress in worn/patched gear." Applied in the same
// OnGameInitializationFinished slot MainSubModule already uses to re-apply
// the culture-text overrides after Game.Initialize() reloads XML data.
//
// "Gold/ornate/rich" is read two ways per UnitsMath.IsOrnateLordGear:
//   - the piece's own value clears WearyLordValueCap (a plain expensive item), or
//   - it carries an ItemModifier at all (Fine/Masterwork/Legendary/etc. — the
//     "quality" enchantment vanilla puts on lordly loot).
// Either marks it as finery a survivor of the Long Night would have sold,
// lost, or never owned — it gets replaced with the cheapest real item of the
// same slot type (GearWeathering.CheapestOfType), stripped of its modifier.
//
// Hero.BattleEquipment returns a live, mutable Equipment reference (see
// CrystallinesCampaignBehavior.SeedCrystalOnHero for the same indexer-write
// pattern), so no separate "set equipment" call is needed.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TheDarkestNight
{
    internal static class LordGearWeathering
    {
        private static readonly EquipmentIndex[] AllSlots =
        {
            EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
            EquipmentIndex.Head, EquipmentIndex.Body, EquipmentIndex.Leg,
            EquipmentIndex.Gloves, EquipmentIndex.Cape
        };

        internal static void ApplyToAllLords()
        {
            try
            {
                foreach (Hero h in Hero.AllAliveHeroes.Where(h => h.IsLord && h != Hero.MainHero).ToList())
                {
                    try { WeatherHeroEquipment(h); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void WeatherHeroEquipment(Hero hero)
        {
            var equipment = hero?.BattleEquipment;
            if (equipment == null) return;

            foreach (var slot in AllSlots)
            {
                var elem = equipment[slot];
                if (elem.IsEmpty || elem.Item == null) continue;
                // Wands are expensive by design (12000-20000 denars) but are
                // never "ornate lord finery" — exempting them here means the
                // Children of the Forest / Tower / Chosen wand-equip sweep
                // (WandsCampaignBehavior.EnsureLordWandEquipped) never races
                // this one-time session-launch pass.
                if (WandsCatalog.IsWandItemId(elem.Item.StringId)) continue;
                if (!UnitsMath.IsOrnateLordGear(elem.ItemValue, elem.ItemModifier != null)) continue;

                var replacement = GearWeathering.CheapestOfType(elem.Item.ItemType);
                if (replacement != null && replacement.StringId != elem.Item.StringId)
                    equipment[slot] = new EquipmentElement(replacement);
            }
        }
    }
}
