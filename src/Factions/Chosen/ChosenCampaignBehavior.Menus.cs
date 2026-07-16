// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenCampaignBehavior.Menus.cs
//
// Game menu implementation for the Chosen's town options — Chosen towns only
// (Phycaon + Lycaron, or whatever the kingdom currently holds — see
// ChosenSettlements.IsChosenSettlement). Mirrors
// TempleCampaignBehavior.Menus.cs / BloodboundCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Seek the Chosen" (submenu)
//     → "chosen_order_main"          (description)
//       → "chosen_order_buy_rod"     (purchase a Rod of the Apostle)
//       → "chosen_order_take_wife"   (convert a qualifying female prisoner
//                                      into a new wife — see
//                                      ChosenCampaignBehavior.Wives.cs)
//       → "chosen_order_leave"
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public partial class ChosenCampaignBehavior
    {
        private static void RegisterChosenMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "chosen_order_enter", "{CHOSEN_ORDER_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!ChosenSettlements.IsChosenSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("CHOSEN_ORDER_ENTER_TEXT", "Seek the Chosen");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("chosen_order_main", "{CHOSEN_ORDER_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("CHOSEN_ORDER_MAIN_TEXT",
                            "The vigil-hall smells of tallow and old iron. An Apostle kneels before a relic rack, "
                          + "not praying so much as counting. \"The PriestKing's line does not want for coin, only "
                          + "for hands willing to carry what Heaven demands of us.\"");
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            RegisterBuyRodOption(starter);
            RegisterTakeWifeOption(starter);

            try
            {
                starter.AddGameMenuOption("chosen_order_main", "chosen_order_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Option 1: buy a Rod of the Apostle ────────────────────────────────
        private static void RegisterBuyRodOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("chosen_order_main", "chosen_order_buy_rod", "{CHOSEN_ORDER_ROD_TEXT}",
                    args =>
                    {
                        try
                        {
                            MBTextManager.SetTextVariable("CHOSEN_ORDER_ROD_TEXT",
                                $"Buy a Rod of the Apostle  [{ChosenMath.RodPurchaseCostGold} denars]");
                            args.IsEnabled = (Hero.MainHero?.Gold ?? 0) >= ChosenMath.RodPurchaseCostGold;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoBuyRod(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoBuyRod()
        {
            Hero hero = Hero.MainHero;
            if (hero == null || hero.Gold < ChosenMath.RodPurchaseCostGold)
            {
                ShowDialog("Not Enough Coin", $"You need {ChosenMath.RodPurchaseCostGold} denars for a relic of that weight.",
                    () => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            try { hero.ChangeHeroGold(-ChosenMath.RodPurchaseCostGold); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            GrantRodOfApostle(hero, announce: false);

            ShowDialog("Purchased", "The iron is cold, then it isn't. \"Carry it well,\" the Apostle says, and does not explain further.",
                () => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
        }

        // ── Option 2: take a qualifying female prisoner as a new wife ──────────
        private static void RegisterTakeWifeOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("chosen_order_main", "chosen_order_take_wife", "{CHOSEN_ORDER_WIFE_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool eligiblePlayer = IsMalePlayerEligibleForMoreWives();
                            bool hasPrisoner = TryGetQualifyingFemalePrisoner(out var prisonerTemplate);
                            string note = !eligiblePlayer
                                ? "  [not available to you]"
                                : !hasPrisoner
                                    ? "  [no qualifying prisoner]"
                                    : $"  [{prisonerTemplate.Name}]";
                            MBTextManager.SetTextVariable("CHOSEN_ORDER_WIFE_TEXT", "Take a prisoner as a wife" + note);
                            args.IsEnabled = eligiblePlayer && hasPrisoner;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoTakePrisonerAsWife(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoTakePrisonerAsWife()
        {
            if (!IsMalePlayerEligibleForMoreWives())
            {
                ShowDialog("Cannot Wed", "This is not a road open to you.",
                    () => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            if (!TryGetQualifyingFemalePrisoner(out var prisonerTemplate))
            {
                ShowDialog("Nothing To Give", "You hold no qualifying captive to offer the Apostle's blessing.",
                    () => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            string wifeName = prisonerTemplate.Name?.ToString() ?? "The captive";
            ConsumePrisonerAndWed(prisonerTemplate);

            ShowDialog("Wed", $"{wifeName} is led from the cage and into the PriestKing's own rite. She does not leave your household again.",
                () => { try { GameMenu.SwitchToMenu("chosen_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
        }

        // ── Helpers ────────────────────────────────────────────────────────────
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
                try { onClose?.Invoke(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }
    }
}
