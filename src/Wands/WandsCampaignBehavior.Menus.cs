// =============================================================================
// THE DARKEST NIGHT — Wands/WandsCampaignBehavior.Menus.cs
//
// Game menu implementation for the wand shop — appears ONLY in the one
// Tower town and the one Chosen town WandsCampaignBehavior.SelectShopTowns
// picked for this campaign session (IsWandShopTown). Mirrors
// ChosenCampaignBehavior.Menus.cs / TempleCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Seek the wandwright" (submenu, gated to the two chosen towns)
//     → "wand_shop_main"        (description)
//       → "wand_shop_buy_<n>"   (one option per WandsCatalog entry)
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
                        MBTextManager.SetTextVariable("WAND_SHOP_MAIN_TEXT",
                            "A locked case holds a dozen-odd rods, each one a single working caught and bound "
                          + "into wood and wire. \"Each answers only to the one thing it was made to say,\" the "
                          + "wandwright tells you, \"and each will only say it so many times before you.\"");
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

        private static void RegisterBuyOptions(CampaignGameStarter starter)
        {
            var all = WandsCatalog.All;
            for (int slot = 0; slot < all.Count; slot++)
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
                                var def = all[captured];
                                int cost = WandsMath.PriceForTier(def.Tier);
                                MBTextManager.SetTextVariable($"WAND_SHOP_BUY_{captured}_TEXT",
                                    $"Buy {def.Name}  [{cost} denars]");
                                args.IsEnabled = (Hero.MainHero?.Gold ?? 0) >= cost;
                                try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            }
                            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        },
                        args => { try { DoBuyWand(captured); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void DoBuyWand(int slot)
        {
            var all = WandsCatalog.All;
            if (slot < 0 || slot >= all.Count) return;
            var def = all[slot];
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
