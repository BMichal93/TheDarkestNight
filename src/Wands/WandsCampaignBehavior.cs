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
//   • Each shop town holds a small ROTATING CASE (WandsMath.ShopStockSize,
//     re-rolled every WandsMath.ShopRestockDays) rather than the whole
//     catalog — a stocked wand can be bought once; the slot then sits empty
//     until the next restock. Stock selection is a pure, deterministic
//     function of (town id, restock cycle) in WandsMath.PickShopStock, so a
//     reload never re-rolls the case mid-cycle.
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

namespace TheDarkestNight
{
    public partial class WandsCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state ────────────────────────────────────────────────────
        private static HashSet<string> _rolledLordIds = new HashSet<string>();
        private static string _towerShopTownId = null;
        private static string _chosenShopTownId = null;

        // Rotating shop stock: town id -> list of WandsCatalog indices
        // currently on display (a sold slot is left as -1 until restock).
        private static Dictionary<string, List<int>> _shopStock = new Dictionary<string, List<int>>();
        private static Dictionary<string, int> _lastRestockDay = new Dictionary<string, int>();

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

                var stockTownIds = new List<string>();
                var stockIndices = new List<int>();
                foreach (var kv in _shopStock)
                {
                    foreach (int idx in kv.Value)
                    {
                        stockTownIds.Add(kv.Key);
                        stockIndices.Add(idx);
                    }
                }
                store.SyncData("WND_StockTownIds", ref stockTownIds);
                store.SyncData("WND_StockIndices", ref stockIndices);
                if (store.IsLoading)
                {
                    _shopStock = new Dictionary<string, List<int>>();
                    if (stockTownIds != null && stockIndices != null && stockTownIds.Count == stockIndices.Count)
                    {
                        for (int i = 0; i < stockTownIds.Count; i++)
                        {
                            string t = stockTownIds[i];
                            if (!_shopStock.TryGetValue(t, out var list)) { list = new List<int>(); _shopStock[t] = list; }
                            list.Add(stockIndices[i]);
                        }
                    }
                }

                var restockTownIds = _lastRestockDay.Keys.ToList();
                var restockDays = restockTownIds.Select(t => _lastRestockDay[t]).ToList();
                store.SyncData("WND_RestockTownIds", ref restockTownIds);
                store.SyncData("WND_RestockDays", ref restockDays);
                if (store.IsLoading)
                {
                    _lastRestockDay = new Dictionary<string, int>();
                    if (restockTownIds != null && restockDays != null && restockTownIds.Count == restockDays.Count)
                        for (int i = 0; i < restockTownIds.Count; i++)
                            _lastRestockDay[restockTownIds[i]] = restockDays[i];
                }

                var chargeKeys = WandEffects.ExportChargeKeys();
                var chargeVals = WandEffects.ExportChargeVals();
                store.SyncData("WND_ChargeKeys", ref chargeKeys);
                store.SyncData("WND_ChargeVals", ref chargeVals);
                if (store.IsLoading) WandEffects.ImportCharges(chargeKeys, chargeVals);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _rolledLordIds = new HashSet<string>();
            _towerShopTownId = null;
            _chosenShopTownId = null;
            _forestShopTownId = null;
            _shopStock = new Dictionary<string, List<int>>();
            _lastRestockDay = new Dictionary<string, int>();
            WandEffects.ResetForNewGame();
        }

        // ── Rotating shop stock ─────────────────────────────────────────────────
        private static int CurrentDay()
        {
            try { return (int)CampaignTime.Now.ToDays; } catch { return 0; }
        }

        // Overridden by the Children of the Forest's fuller case — see
        // .Menus.cs / IsForestShopTown wiring added alongside CityStates.
        internal static int StockSizeForTown(string townId)
        {
            if (IsForestShopTown(townId)) return WandsMath.ForestShopStockSize;
            return WandsMath.ShopStockSize;
        }

        internal static bool IsForestShopTown(string townId)
            => !string.IsNullOrEmpty(townId) && townId == _forestShopTownId;

        // Pen Cannoc — the Children's own seat. Unlike the Tower/Chosen shop
        // towns (imported stock, re-picked if lost), this one is PERMANENT:
        // they make the wands, so as long as the Children of the Forest hold
        // any town at all (a one-city kingdom, so that town is always Pen
        // Cannoc) it is re-derived fresh every session/weekly tick from live
        // CityStateSystem.IsForestSettlement state — no separate save key.
        private static string _forestShopTownId = null;

        internal static void EnsureStock(string townId)
        {
            if (string.IsNullOrEmpty(townId)) return;
            int day = CurrentDay();
            int stockSize = StockSizeForTown(townId);

            if (!_shopStock.ContainsKey(townId)
                || !_lastRestockDay.TryGetValue(townId, out int lastDay)
                || WandsMath.ShouldRestock(lastDay, day))
            {
                RestockTown(townId, stockSize, day);
            }
        }

        private static void RestockTown(string townId, int stockSize, int day)
        {
            int cycle = WandsMath.ShopRestockDays > 0 ? day / WandsMath.ShopRestockDays : day;
            int seed = WandsMath.RestockSeed(townId, cycle);
            _shopStock[townId] = WandsMath.PickShopStock(seed, WandsCatalog.All.Count, stockSize);
            _lastRestockDay[townId] = day;
        }

        internal static List<int> GetStock(string townId)
            => _shopStock.TryGetValue(townId, out var list) ? list : null;

        internal static void MarkSlotSold(string townId, int slot)
        {
            if (_shopStock.TryGetValue(townId, out var list) && slot >= 0 && slot < list.Count)
                list[slot] = -1;
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterWandMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SelectShopTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SweepGrantWandsToLords(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnWeeklyTick()
        {
            try { SelectShopTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SweepGrantWandsToLords(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Shop town selection — stable-but-random, re-picked only if lost ────
        private static void SelectShopTowns()
        {
            try
            {
                if (string.IsNullOrEmpty(_towerShopTownId) || !IsHeldBy(_towerShopTownId, TowerCulture.CultureId))
                    _towerShopTownId = PickRandomHeldTown(TowerCulture.CultureId);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try
            {
                if (string.IsNullOrEmpty(_chosenShopTownId) || !IsHeldBy(_chosenShopTownId, ChosenCulture.KingdomId))
                    _chosenShopTownId = PickRandomHeldTown(ChosenCulture.KingdomId);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // Pen Cannoc — always re-derived, never randomly re-picked: the
            // Children of the Forest are a one-city kingdom, so whichever
            // town they currently hold (if any) IS Pen Cannoc.
            try
            {
                _forestShopTownId = Settlement.All
                    .FirstOrDefault(s => s != null && s.IsTown && CityStateSystem.IsForestSettlement(s))
                    ?.StringId;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
            && (s.StringId == _towerShopTownId || s.StringId == _chosenShopTownId || s.StringId == _forestShopTownId);

        // ── Lord distribution — rolled once per hero, ever ──────────────────────
        // Also carries two Children-of-the-Forest-only concerns that need to
        // run EVERY week regardless of the once-per-hero roll: re-asserting a
        // granted wand into BattleEquipment (agents spawn from BattleEquipment,
        // never the roster — see the header note) and re-anchoring their age
        // back into the young-adult window (CityStateSystem.ReanchorForestLordAge).
        // Both are idempotent no-ops once already correct.
        private static void SweepGrantWandsToLords()
        {
            try
            {
                foreach (var hero in Hero.AllAliveHeroes.ToList())
                {
                    try
                    {
                        if (hero == null || !hero.IsLord) continue;

                        bool isTower = TowerCulture.IsTowerLord(hero);
                        bool isChosen = !isTower && ChosenCulture.IsChosenLord(hero);
                        bool isForest = !isTower && !isChosen && CityStateSystem.IsForestKingdom(hero.Clan?.Kingdom);

                        if (isForest)
                        {
                            try { CityStateSystem.ReanchorForestLordAge(hero); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }

                        // Self-heal: any lord (Tower/Chosen/Forest alike)
                        // already carrying a wand in their roster gets it
                        // re-asserted as their equipped weapon every week —
                        // this is what actually makes the wand FIRE in battle
                        // (WandEffects.OnAgentHit reads the wielded weapon,
                        // not the roster) and survives LordGearWeathering's
                        // one-time session-launch pass without needing exact
                        // ordering between the two systems (LordGearWeathering
                        // also now exempts wand items directly — see its
                        // IsOrnateLordGear check).
                        try { EnsureLordWandEquipped(hero); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                        if (_rolledLordIds.Contains(hero.StringId)) continue;
                        if (!isTower && !isChosen && !isForest) continue;

                        _rolledLordIds.Add(hero.StringId);

                        double chance = isTower ? WandsMath.TowerLordWandChance
                            : isChosen ? WandsMath.ChosenLordWandChance
                            : WandsMath.ForestLordWandChance;
                        if (!WandsMath.ShouldGrantLordWand(_rng.NextDouble(), chance)) continue;

                        var def = WandsCatalog.All[_rng.Next(WandsCatalog.All.Count)];
                        GrantWandToHero(hero, def);
                        try { EnsureLordWandEquipped(hero); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Writes a wand the hero's roster already carries into their
        // BattleEquipment Weapon0 slot — same indexer-write pattern
        // CrystallinesCampaignBehavior uses for hero equipment. Only Weapon0
        // is touched, so whatever occupies Weapon1-3 (a real sidearm) is left
        // alone — "wands mostly, not helplessly wand-only when it breaks."
        // A no-op if the hero already has that exact wand equipped.
        private static void EnsureLordWandEquipped(Hero hero)
        {
            if (hero == null || !hero.IsAlive) return;
            var roster = hero.PartyBelongedTo?.ItemRoster;
            if (roster == null) return;

            ItemObject wandItem = null;
            for (int i = 0; i < roster.Count; i++)
            {
                var item = roster.GetItemAtIndex(i);
                if (item != null && WandsCatalog.IsWandItemId(item.StringId) && roster.GetElementNumber(i) > 0)
                {
                    wandItem = item;
                    break;
                }
            }
            if (wandItem == null) return;

            var equipment = hero.BattleEquipment;
            if (equipment == null) return;

            var current = equipment[EquipmentIndex.Weapon0];
            if (!current.IsEmpty && current.Item != null && current.Item.StringId == wandItem.StringId) return;

            equipment[EquipmentIndex.Weapon0] = new EquipmentElement(wandItem);
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
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
