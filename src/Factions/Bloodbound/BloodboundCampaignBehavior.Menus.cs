// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundCampaignBehavior.Menus.cs
//
// Game menu implementation for the Bloodbound's blood-spending — Bloodbound
// towns only. Mirrors WolfBrothersCampaignBehavior.Menus.cs / TowerCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Spend Demon Blood among the Bloodbound"
//     → "bloodbound_hunt_main"        (description)
//       → "bloodbound_hunt_ignore"    (demons ignore you for 1-4 days)
//       → "bloodbound_hunt_hpbuff"    (+80 max HP for a week)
//       → "bloodbound_hunt_trade"     (-1 Social/Intellect for +1 Vigor/Endurance, random)
//       → "bloodbound_hunt_leave"
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

namespace AshAndEmber
{
    public partial class BloodboundCampaignBehavior
    {
        private static readonly Random _menuRng = new Random();

        private static void RegisterBloodboundMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "bloodbound_hunt_enter", "{BLOODBOUND_HUNT_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!BloodboundSettlements.IsBloodboundSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("BLOODBOUND_HUNT_ENTER_TEXT", "Spend Demon Blood among the Bloodbound");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("bloodbound_hunt_main", "{BLOODBOUND_HUNT_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("BLOODBOUND_HUNT_MAIN_TEXT",
                            "Vials of Demon Blood line the shelves here, dark and slow-moving even in the "
                          + "warmth. The Bloodbound will trade you a working of it — a few days unseen by the "
                          + "dark, a week of flesh too stubborn to die, or a swallow that reshapes what you are.");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterIgnoreOption(starter);
            RegisterHpBuffOption(starter);
            RegisterAttributeTradeOption(starter);

            try
            {
                starter.AddGameMenuOption("bloodbound_hunt_main", "bloodbound_hunt_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Option 1: demons ignore you for 1-4 days ─────────────────────────────
        private static void RegisterIgnoreOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("bloodbound_hunt_main", "bloodbound_hunt_ignore", "{BLOODBOUND_HUNT_IGNORE_TEXT}",
                    args =>
                    {
                        try
                        {
                            MBTextManager.SetTextVariable("BLOODBOUND_HUNT_IGNORE_TEXT",
                                $"Buy the dark's blindness (1-4 days)  [{BloodboundMath.IgnoreCostBlood} Demon Blood]");
                            args.IsEnabled = HaveBlood(BloodboundMath.IgnoreCostBlood);
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoBuyIgnore(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoBuyIgnore()
        {
            if (!SpendBlood(BloodboundMath.IgnoreCostBlood))
            {
                ShowDialog("Not Enough Blood", $"You need {BloodboundMath.IgnoreCostBlood} Demon Blood for this working.",
                    () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            float days = BloodboundMath.RollIgnoreDurationDays(_menuRng.NextDouble());
            GrantIgnore(Hero.MainHero, days);

            ShowDialog("Unseen", $"The dark passes over you without turning its head. ({(int)Math.Round(days)} day(s))",
                () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Option 2: +80 max HP for a week ──────────────────────────────────────
        private static void RegisterHpBuffOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("bloodbound_hunt_main", "bloodbound_hunt_hpbuff", "{BLOODBOUND_HUNT_HPBUFF_TEXT}",
                    args =>
                    {
                        try
                        {
                            MBTextManager.SetTextVariable("BLOODBOUND_HUNT_HPBUFF_TEXT",
                                $"Drink for hardened flesh (+{(int)BloodboundMath.HpBuffAmount} max HP, a week)  [{BloodboundMath.HpBuffCostBlood} Demon Blood]");
                            args.IsEnabled = HaveBlood(BloodboundMath.HpBuffCostBlood);
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoBuyHpBuff(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoBuyHpBuff()
        {
            if (!SpendBlood(BloodboundMath.HpBuffCostBlood))
            {
                ShowDialog("Not Enough Blood", $"You need {BloodboundMath.HpBuffCostBlood} Demon Blood for this working.",
                    () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            GrantHpBuff(Hero.MainHero);

            ShowDialog("Hardened", $"The blood settles under your skin. You can take more than you used to, for now. (+{(int)BloodboundMath.HpBuffAmount} max HP, {(int)BloodboundMath.HpBuffDurationDays} days)",
                () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Option 3: -1 Social/Intellect (random) for +1 Vigor/Endurance (random) ──
        private static void RegisterAttributeTradeOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("bloodbound_hunt_main", "bloodbound_hunt_trade", "{BLOODBOUND_HUNT_TRADE_TEXT}",
                    args =>
                    {
                        try
                        {
                            MBTextManager.SetTextVariable("BLOODBOUND_HUNT_TRADE_TEXT",
                                $"Swallow the working (trade a mind's edge for the body's)  [{BloodboundMath.AttributeTradeCostBlood} Demon Blood]");
                            args.IsEnabled = HaveBlood(BloodboundMath.AttributeTradeCostBlood);
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoAttributeTrade(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoAttributeTrade()
        {
            if (!SpendBlood(BloodboundMath.AttributeTradeCostBlood))
            {
                ShowDialog("Not Enough Blood", $"You need {BloodboundMath.AttributeTradeCostBlood} Demon Blood for this working.",
                    () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            Hero hero = Hero.MainHero;
            var down = BloodboundMath.PickPairIndex(_menuRng.NextDouble()) == 0
                ? DefaultCharacterAttributes.Social
                : DefaultCharacterAttributes.Intelligence;
            var up = BloodboundMath.PickPairIndex(_menuRng.NextDouble()) == 0
                ? DefaultCharacterAttributes.Vigor
                : DefaultCharacterAttributes.Endurance;

            try { hero.HeroDeveloper?.RemoveAttribute(down, 1); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { hero.HeroDeveloper?.AddAttribute(up, 1, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ShowDialog("Remade", $"Something in you gives ground so something else can grow. (-1 {down.Name}, +1 {up.Name})",
                () => { try { GameMenu.SwitchToMenu("bloodbound_hunt_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static bool HaveBlood(int amount)
        {
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(BloodboundCatalog.DemonBloodItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return false;
                return roster.GetItemNumber(item) >= amount;
            }
            catch { return false; }
        }

        private static bool SpendBlood(int amount)
        {
            try
            {
                if (!HaveBlood(amount)) return false;
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(BloodboundCatalog.DemonBloodItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return false;
                roster.AddToCounts(item, -amount);
                return true;
            }
            catch { return false; }
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
