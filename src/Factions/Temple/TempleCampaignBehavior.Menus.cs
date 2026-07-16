// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleCampaignBehavior.Menus.cs
//
// Game menu implementation for the Temple's town options — Temple towns only
// (Ocs Hall + Pravend, or whatever the kingdom currently holds — see
// TempleSettlements.IsTempleSettlement). Mirrors
// BloodboundCampaignBehavior.Menus.cs / WolfBrothersCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Seek the Order" (submenu)
//     → "temple_order_main"        (description)
//       → "temple_order_buy_sigil" (purchase a Holy Sigil for gold)
//       → "temple_order_pray"      (pray — gated by personality traits)
//       → "temple_order_leave"
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public partial class TempleCampaignBehavior
    {
        private static void RegisterTempleMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "temple_order_enter", "{TEMPLE_ORDER_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!TempleSettlements.IsTempleSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("TEMPLE_ORDER_ENTER_TEXT", "Seek the Order");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("temple_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("temple_order_main", "{TEMPLE_ORDER_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("TEMPLE_ORDER_MAIN_TEXT",
                            "The vigil-house is quiet at this hour, lit by a handful of tallow candles. A Brother "
                          + "Templar tends the altar without turning. \"Stone can be shaped into a vow, if you have "
                          + "the coin for the shaping. Or you may simply kneel and ask the Light to hear you.\"");
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            RegisterBuySigilOption(starter);
            RegisterPrayOption(starter);

            try
            {
                starter.AddGameMenuOption("temple_order_main", "temple_order_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Option 1: buy a Holy Sigil ────────────────────────────────────────
        private static void RegisterBuySigilOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("temple_order_main", "temple_order_buy_sigil", "{TEMPLE_ORDER_SIGIL_TEXT}",
                    args =>
                    {
                        try
                        {
                            MBTextManager.SetTextVariable("TEMPLE_ORDER_SIGIL_TEXT",
                                $"Buy a Holy Sigil  [{TempleMath.SigilPurchaseCostGold} denars]");
                            args.IsEnabled = (Hero.MainHero?.Gold ?? 0) >= TempleMath.SigilPurchaseCostGold;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoBuySigil(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoBuySigil()
        {
            Hero hero = Hero.MainHero;
            if (hero == null || hero.Gold < TempleMath.SigilPurchaseCostGold)
            {
                ShowDialog("Not Enough Coin", $"You need {TempleMath.SigilPurchaseCostGold} denars for the shaping.",
                    () => { try { GameMenu.SwitchToMenu("temple_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            try { hero.ChangeHeroGold(-TempleMath.SigilPurchaseCostGold); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            GrantHolySigil(hero, announce: false);

            ShowDialog("Shaped", "The stone is warm before it even leaves the mason's hand. \"Carry it well.\"",
                () => { try { GameMenu.SwitchToMenu("temple_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
        }

        // ── Option 2: pray — gated by personality traits ─────────────────────
        private static void RegisterPrayOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("temple_order_main", "temple_order_pray", "{TEMPLE_ORDER_PRAY_TEXT}",
                    args =>
                    {
                        try
                        {
                            Hero hero = Hero.MainHero;
                            int honor = 0, mercy = 0;
                            try { honor = hero.GetTraitLevel(DefaultTraits.Honor); mercy = hero.GetTraitLevel(DefaultTraits.Mercy); }
                            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                            bool qualifies = TempleMath.QualifiesToPray(honor, mercy);
                            bool ready = CanPrayToday(hero);

                            MBTextManager.SetTextVariable("TEMPLE_ORDER_PRAY_TEXT",
                                !qualifies ? "Pray (the Light does not hear a hollow vow)"
                                : !ready   ? "Pray (already prayed today)"
                                           : "Pray at the altar");
                            args.IsEnabled = qualifies && ready;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoPray(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoPray()
        {
            Hero hero = Hero.MainHero;
            if (hero == null)
            {
                GameMenu.SwitchToMenu("temple_order_main");
                return;
            }

            int honor = hero.GetTraitLevel(DefaultTraits.Honor);
            int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
            if (!TempleMath.QualifiesToPray(honor, mercy) || !CanPrayToday(hero))
            {
                ShowDialog("No Answer", "You kneel. The stone stays cold. Whatever you carry tonight, it is not "
                    + "the shape of a prayer the Order would recognise.",
                    () => { try { GameMenu.SwitchToMenu("temple_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            ApplyPrayer(hero);

            ShowDialog("Answered", "The words leave you and something in your own chest answers back — steadier, "
                + "warmer. Your column feels it too. (Party morale and wounded soldiers restored.)",
                () => { try { GameMenu.SwitchToMenu("temple_order_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
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
