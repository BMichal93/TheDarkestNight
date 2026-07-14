// =============================================================================
// THE DARKEST NIGHT — Wands/WandsCampaignBehavior.cs
//
// Campaign-side glue for wands (mod-author-directed addition):
//   • Grants a wand to a rolled-once subset of Tower and Chosen lords
//     (WandsMath.TowerLordWandChance / ChosenLordWandChance) — mirrors
//     ChosenCampaignBehavior.Rod.cs's SweepGrantRodsToChosenLords, but rolled
//     ONCE per hero ever (not "top up everyone missing one every week") so
//     the population stays a stable rarity rather than creeping toward 100%
//     of the faction's lords over a long campaign.
//   • Picks one random CURRENTLY-HELD town per faction (Tower, Chosen) to
//     sell wands, once per campaign session — same "stable-but-random,
//     re-picked only if the chosen town is lost" technique used elsewhere in
//     this mod for a single-seat faction selection. See .Menus.cs for the
//     shop itself.
//   • Persists the player's per-wand-item-id charge pool (owned by
//     WandEffects, exported/imported here) via the same parallel-list
//     Dictionary<string,int> save pattern ExchangeCampaignBehavior already
//     uses for its cooldown dictionary.
//
// Hollow Choir distribution needs NO runtime code — see ModuleData/troops.xml's
// hollow_magus entry, which carries an extra EquipmentRoster variant that
// substitutes a wand for its "Item1" wrapped_stick prop (see WandsMath.
// HollowMagusWandRosterFraction for the intended ratio this represents).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class WandsCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state ────────────────────────────────────────────────────
        private static HashSet<string> _rolledLordIds = new HashSet<string>();
        private static string _towerShopTownId = null;
        private static string _chosenShopTownId = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try
            {
                var rolled = _rolledLordIds.ToList();
                store.SyncData("WND_RolledLordIds", ref rolled);
                _rolledLordIds = new HashSet<string>(rolled ?? new List<string>());

                store.SyncData("WND_TowerShopTown", ref _towerShopTownId);
                store.SyncData("WND_ChosenShopTown", ref _chosenShopTownId);

                var chargeKeys = WandEffects.ExportChargeKeys();
                var chargeVals = WandEffects.ExportChargeVals();
                store.SyncData("WND_ChargeKeys", ref chargeKeys);
                store.SyncData("WND_ChargeVals", ref chargeVals);
                if (store.IsLoading) WandEffects.ImportCharges(chargeKeys, chargeVals);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _rolledLordIds = new HashSet<string>();
            _towerShopTownId = null;
            _chosenShopTownId = null;
            WandEffects.ResetForNewGame();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterWandMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SelectShopTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SweepGrantWandsToLords(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void OnWeeklyTick()
        {
            try { SelectShopTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SweepGrantWandsToLords(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Shop town selection — stable-but-random, re-picked only if lost ────
        private static void SelectShopTowns()
        {
            try
            {
                if (string.IsNullOrEmpty(_towerShopTownId) || !IsHeldBy(_towerShopTownId, TowerCulture.CultureId))
                    _towerShopTownId = PickRandomHeldTown(TowerCulture.CultureId);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                if (string.IsNullOrEmpty(_chosenShopTownId) || !IsHeldBy(_chosenShopTownId, ChosenCulture.KingdomId))
                    _chosenShopTownId = PickRandomHeldTown(ChosenCulture.KingdomId);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool IsHeldBy(string settlementId, string kingdomId)
        {
            try
            {
                var s = Settlement.All.FirstOrDefault(x => x != null && x.StringId == settlementId);
                return s != null && s.IsTown && s.MapFaction?.StringId == kingdomId;
            }
            catch { return false; }
        }

        private static string PickRandomHeldTown(string kingdomId)
        {
            try
            {
                var towns = Settlement.All.Where(s => s != null && s.IsTown && s.MapFaction?.StringId == kingdomId).ToList();
                if (towns.Count == 0) return null;
                return towns[_rng.Next(towns.Count)].StringId;
            }
            catch { return null; }
        }

        internal static bool IsWandShopTown(Settlement s)
            => s != null && !string.IsNullOrEmpty(s.StringId)
            && (s.StringId == _towerShopTownId || s.StringId == _chosenShopTownId);

        // ── Lord distribution — rolled once per hero, ever ──────────────────────
        private static void SweepGrantWandsToLords()
        {
            try
            {
                foreach (var hero in Hero.AllAliveHeroes.ToList())
                {
                    try
                    {
                        if (hero == null || !hero.IsLord) continue;
                        if (_rolledLordIds.Contains(hero.StringId)) continue;

                        bool isTower = TowerCulture.IsTowerLord(hero);
                        bool isChosen = !isTower && ChosenCulture.IsChosenLord(hero);
                        if (!isTower && !isChosen) continue;

                        _rolledLordIds.Add(hero.StringId);

                        double chance = isTower ? WandsMath.TowerLordWandChance : WandsMath.ChosenLordWandChance;
                        if (!WandsMath.ShouldGrantLordWand(_rng.NextDouble(), chance)) continue;

                        var def = WandsCatalog.All[_rng.Next(WandsCatalog.All.Count)];
                        GrantWandToHero(hero, def);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void GrantWandToHero(Hero hero, WandDef def)
        {
            if (hero == null) return;
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = hero.PartyBelongedTo?.ItemRoster ?? (hero == Hero.MainHero ? MobileParty.MainParty?.ItemRoster : null);
                if (item == null || roster == null) return;
                roster.AddToCounts(item, 1);

                if (hero == Hero.MainHero)
                {
                    WandEffects.RefillPlayerCharges(def.ItemId);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{def.Name} is pressed into your hand.", new Color(0.6f, 0.45f, 0.85f)));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
