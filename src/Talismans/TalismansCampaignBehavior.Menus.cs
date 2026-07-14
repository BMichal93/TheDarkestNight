// =============================================================================
// THE DARKEST NIGHT — Talismans/TalismansCampaignBehavior.Menus.cs
//
// Game menu implementation for the talisman shop — appears ONLY in the one
// Temple town TalismansCampaignBehavior.SelectShopTown picked for this
// campaign session (IsTalismanShopTown). Mirrors
// Wands/WandsCampaignBehavior.Menus.cs almost exactly.
//
// Menu tree:
//   "town" → "Visit the reliquary" (submenu, gated to the one chosen town)
//     → "talisman_shop_main"          (description)
//       → "talisman_shop_buy_<n>"     (one option per TalismansCatalog entry)
//       → "talisman_shop_leave"
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
    public partial class TalismansCampaignBehavior
    {
        private static void RegisterTalismanMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "talisman_shop_enter", "{TALISMAN_SHOP_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!IsTalismanShopTown(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("TALISMAN_SHOP_ENTER_TEXT", "Visit the reliquary");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("talisman_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("talisman_shop_main", "{TALISMAN_SHOP_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("TALISMAN_SHOP_MAIN_TEXT",
                            "A reliquary keeper unlocks a case of stone talismans, each one blessed once and "
                          + "never again. \"They do not flare or burn,\" she tells you, \"they only make a "
                          + "quiet difference, for as long as you keep one close.\"");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterBuyOptions(starter);

            try
            {
                starter.AddGameMenuOption("talisman_shop_main", "talisman_shop_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RegisterBuyOptions(CampaignGameStarter starter)
        {
            var all = TalismansCatalog.All;
            for (int slot = 0; slot < all.Count; slot++)
            {
                int captured = slot;
                try
                {
                    starter.AddGameMenuOption("talisman_shop_main", $"talisman_shop_buy_{captured}",
                        $"{{TALISMAN_SHOP_BUY_{captured}_TEXT}}",
                        args =>
                        {
                            try
                            {
                                var def = all[captured];
                                int cost = TalismansMath.TalismanPurchaseCostGold;
                                MBTextManager.SetTextVariable($"TALISMAN_SHOP_BUY_{captured}_TEXT",
                                    $"Buy {def.Name}  [{cost} denars]");
                                args.IsEnabled = (Hero.MainHero?.Gold ?? 0) >= cost;
                                try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            }
                            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        },
                        args => { try { DoBuyTalisman(captured); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void DoBuyTalisman(int slot)
        {
            var all = TalismansCatalog.All;
            if (slot < 0 || slot >= all.Count) return;
            var def = all[slot];
            int cost = TalismansMath.TalismanPurchaseCostGold;

            Hero hero = Hero.MainHero;
            if (hero == null || hero.Gold < cost)
            {
                ShowDialog("Not Enough Coin", $"You need {cost} denars for {def.Name}.",
                    () => { try { GameMenu.SwitchToMenu("talisman_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            try { hero.ChangeHeroGold(-cost); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            GrantTalismanToHero(hero, def);

            ShowDialog("Purchased", $"\"Wear it well,\" the reliquary keeper says, \"and let it do its quiet work.\" ({def.Name})",
                () => { try { GameMenu.SwitchToMenu("talisman_shop_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
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
