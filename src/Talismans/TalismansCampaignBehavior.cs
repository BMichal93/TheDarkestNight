// =============================================================================
// THE DARKEST NIGHT — Talismans/TalismansCampaignBehavior.cs
//
// Campaign-side glue for the Temple's holy talismans (mod-author-directed
// addition, sibling to Wands/):
//   • Picks ONE random CURRENTLY-HELD Temple town to sell talismans, once per
//     campaign session — the exact "stable-but-random, re-picked only if the
//     chosen town is lost" technique WandsCampaignBehavior.SelectShopTowns
//     already uses, scoped to Temple settlements via
//     TempleSettlements.IsTempleSettlement rather than a raw kingdom id
//     string (the Temple's own scoping helper already exists and is the
//     faction's documented source of truth for "is this a Temple town").
//     See .Menus.cs for the shop itself.
//
// Talismans have no per-instance state to persist (no charges, no cooldown,
// no break chance — see TalismansMath.cs's header for why they are simpler
// than Wands) beyond the shop town id itself, so SyncData only needs the one
// string.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class TalismansCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state ────────────────────────────────────────────────────
        private static string _shopTownId = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try
            {
                store.SyncData("TLM_ShopTown", ref _shopTownId);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _shopTownId = null;
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterTalismanMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SelectShopTown(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void OnWeeklyTick()
        {
            try { SelectShopTown(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Shop town selection — stable-but-random, re-picked only if lost ────
        private static void SelectShopTown()
        {
            try
            {
                if (string.IsNullOrEmpty(_shopTownId) || !IsStillTempleTown(_shopTownId))
                    _shopTownId = PickRandomTempleTown();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool IsStillTempleTown(string settlementId)
        {
            try
            {
                var s = Settlement.All.FirstOrDefault(x => x != null && x.StringId == settlementId);
                return s != null && s.IsTown && TempleSettlements.IsTempleSettlement(s);
            }
            catch { return false; }
        }

        private static string PickRandomTempleTown()
        {
            try
            {
                var towns = Settlement.All.Where(s => s != null && s.IsTown && TempleSettlements.IsTempleSettlement(s)).ToList();
                if (towns.Count == 0) return null;
                return towns[_rng.Next(towns.Count)].StringId;
            }
            catch { return null; }
        }

        internal static bool IsTalismanShopTown(Settlement s)
            => s != null && !string.IsNullOrEmpty(s.StringId) && s.StringId == _shopTownId;

        private static void GrantTalismanToHero(Hero hero, TalismanDef def)
        {
            if (hero == null) return;
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = hero.PartyBelongedTo?.ItemRoster ?? (hero == Hero.MainHero ? TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.ItemRoster : null);
                if (item == null || roster == null) return;
                roster.AddToCounts(item, 1);

                if (hero == Hero.MainHero)
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{def.Name} is pressed into your hand.", new Color(0.90f, 0.82f, 0.42f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
