// =============================================================================
// THE DARKEST NIGHT — Wands/WandsCampaignBehavior.Menus.cs
//
// Game menu implementation for the wand shop — appears ONLY in the wand shop
// towns WandsCampaignBehavior.SelectShopTowns/IsWandShopTown picks (the one
// Tower town, the one Chosen town, and — from the Children of the Forest —
// the permanent seat at Pen Cannoc). Mirrors ChosenCampaignBehavior.Menus.cs
// / TempleCampaignBehavior.Menus.cs.
//
// Each town holds a small ROTATING CASE (WandsCampaignBehavior.EnsureStock /
// StockSizeForTown), not the whole catalog — buy options are registered for
// the largest possible case size and hidden (not disabled) for any slot past
// the current town's stock, or already sold this cycle.
//
// Menu tree:
//   "town" → "Seek the wandwright" (submenu, gated to the wand shop towns)
//     → "wand_shop_main"        (description)
//       → "wand_shop_buy_<n>"   (one option per case SLOT, up to the widest case)
//       → "wand_shop_leave"
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class WandsCampaignBehavior
    {
        private static void RegisterWandMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "wand_shop_enter", "{WAND_SHOP_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!IsWandShopTown(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("WAND_SHOP_ENTER_TEXT", "Seek the wandwright");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("wand_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("wand_shop_main", "{WAND_SHOP_MAIN_TEXT}", args =>
                {
                    try
                    {
                        string townId = GetCurrentTownId();
                        EnsureStock(townId);

                        MBTextManager.SetTextVariable("WAND_SHOP_MAIN_TEXT", IsForestShopTown(townId)
                            ? "The wandwright here is a Child of the Forest, and speaks of the wood the way others "
                            + "speak of kin. The case behind them holds more rods than you've seen anywhere else — "
                            + "cut, they say, from branches that answered back. \"Each answers only to the one "
                            + "thing it was made to say,\" they tell you, \"and each will only say it so many times "
                            + "before you.\""
                            : "A locked case holds a handful of rods, each one a single working caught and bound "
                            + "into wood and wire — most of the slots stand empty today. \"Each answers only to the "
                            + "one thing it was made to say,\" the wandwright tells you, \"and each will only say it "
                            + "so many times before you. The case fills slow — come back another season.\"");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterBuyOptions(starter);

            try
            {
                starter.AddGameMenuOption("wand_shop_main", "wand_shop_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static string GetCurrentTownId()
        {
            try { return Settlement.CurrentSettlement?.StringId; } catch { return null; }
        }

        // Registered for the widest possible case (the Children of the
        // Forest's fuller stock) — a slot past the current town's own stock
        // size, or already sold this cycle, hides itself (returns false).
        private static void RegisterBuyOptions(CampaignGameStarter starter)
        {
            int widestCase = Math.Max(WandsMath.ShopStockSize, WandsMath.ForestShopStockSize);
            for (int slot = 0; slot < widestCase; slot++)
            {
                int captured = slot;
                try
                {
                    starter.AddGameMenuOption("wand_shop_main", $"wand_shop_buy_{captured}",
                        $"{{WAND_SHOP_BUY_{captured}_TEXT}}",
                        args =>
                        {
                            try
                            {
                                string townId = GetCurrentTownId();
                                var stock = GetStock(townId);
                                if (stock == null || captured >= stock.Count || stock[captured] < 0) return false;

                                var def = WandsCatalog.All[stock[captured]];
                                int cost = WandsMath.PriceForTier(def.Tier);
                                MBTextManager.SetTextVariable($"WAND_SHOP_BUY_{captured}_TEXT",
                                    $"Buy {def.Name}  [{cost} denars]");
                                args.IsEnabled = (Hero.MainHero?.Gold ?? 0) >= cost;
                                try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            }
                            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return false; }
                            return true;
                        },
                        args => { try { DoBuyWand(captured); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void DoBuyWand(int slot)
        {
            string townId = GetCurrentTownId();
            var stock = GetStock(townId);
            if (stock == null || slot < 0 || slot >= stock.Count || stock[slot] < 0) return;

            var def = WandsCatalog.All[stock[slot]];
            int cost = WandsMath.PriceForTier(def.Tier);

            Hero hero = Hero.MainHero;
            if (hero == null || hero.Gold < cost)
            {
                ShowDialog("Not Enough Coin", $"You need {cost} denars for {def.Name}.",
                    () => { try { GameMenu.SwitchToMenu("wand_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            try { hero.ChangeHeroGold(-cost); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            GrantWandToHero(hero, def);
            MarkSlotSold(townId, slot);

            ShowDialog("Purchased", $"\"Carry it well,\" the wandwright says, \"and it will carry you.\" ({def.Name})",
                () => { try { GameMenu.SwitchToMenu("wand_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        private static void ShowDialog(string title, string body, Action onClose)
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    title, body, true, false, "So be it.", "",
                    onClose, null));
            }
            catch
            {
                string brief = body.Length > 100 ? body.Substring(0, 100) + "…" : body;
                MBInformationManager.AddQuickInformation(new TextObject(brief));
                try { onClose?.Invoke(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }
    }
}
