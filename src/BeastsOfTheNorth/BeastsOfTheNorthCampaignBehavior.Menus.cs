// =============================================================================
// THE DARKEST NIGHT — BeastsOfTheNorth/BeastsOfTheNorthCampaignBehavior.Menus.cs
//
// Game menu implementation for "Seek the Old Blood" — Wolf Brothers towns
// only. Mirrors WolfBrothersCampaignBehavior.Menus.cs / AI/AshenRecruit
// CampaignBehavior.Menus.cs's shape: a submenu with one option per recruit,
// each disabled-with-hint when its cost or monthly cap isn't met.
//
// Menu tree:
//   "town" → "Seek the Old Blood"
//     → "beasts_of_the_north_main"  (description)
//       → "beasts_of_the_north_giant"  (the Jotunn-Blooded)
//       → "beasts_of_the_north_rider"  (the Ulfhednar)
//       → "beasts_of_the_north_leave"
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class BeastsOfTheNorthCampaignBehavior
    {
        private static void RegisterBeastsOfTheNorthMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "beasts_of_the_north_enter", "{BEASTS_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!IsBeastsOfTheNorthTown(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("BEASTS_ENTER_TEXT", "Seek the Old Blood");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("beasts_of_the_north_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("beasts_of_the_north_main", "{BEASTS_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("BEASTS_MAIN_TEXT",
                            "The old blood does not come cheap, and it does not come often. What the pack asks for "
                          + "here is not coin so much as proof you can feed what you are about to raise — barrels "
                          + "of salted fish, and enough silver to cover what the fish alone will not.");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterGiantOption(starter);
            RegisterRiderOption(starter);

            try
            {
                starter.AddGameMenuOption("beasts_of_the_north_main", "beasts_of_the_north_leave", "Leave",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The Jotunn-Blooded ────────────────────────────────────────────────
        private static void RegisterGiantOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("beasts_of_the_north_main", "beasts_of_the_north_giant", "{BEASTS_GIANT_TEXT}",
                    args =>
                    {
                        try
                        {
                            Settlement s = Settlement.CurrentSettlement;
                            int bought = GiantPurchasedThisMonth(s.StringId);
                            bool capReached = !BeastsOfTheNorthMath.HasCapRemaining(bought, BeastsOfTheNorthMath.GiantMonthlyCap);
                            int fish = FishCarried();
                            int gold = Hero.MainHero?.Gold ?? 0;
                            bool canAfford = BeastsOfTheNorthMath.CanAffordFish(fish, BeastsOfTheNorthMath.GiantFishCost)
                                          && BeastsOfTheNorthMath.CanAffordGold(gold, BeastsOfTheNorthMath.GiantGoldCost);

                            string cost = $"[{BeastsOfTheNorthMath.GiantFishCost} fish, {BeastsOfTheNorthMath.GiantGoldCost} denars]";
                            if (capReached)
                                MBTextManager.SetTextVariable("BEASTS_GIANT_TEXT", $"Raise a Jotunn-Blooded  (none left to raise this month)");
                            else if (!canAfford)
                                MBTextManager.SetTextVariable("BEASTS_GIANT_TEXT", $"Raise a Jotunn-Blooded  {cost} (lacking)");
                            else
                                MBTextManager.SetTextVariable("BEASTS_GIANT_TEXT", $"Raise a Jotunn-Blooded  {cost}");

                            args.IsEnabled = !capReached && canAfford;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoRaiseGiant(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The Ulfhednar ─────────────────────────────────────────────────────
        private static void RegisterRiderOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("beasts_of_the_north_main", "beasts_of_the_north_rider", "{BEASTS_RIDER_TEXT}",
                    args =>
                    {
                        try
                        {
                            Settlement s = Settlement.CurrentSettlement;
                            int bought = RiderPurchasedThisMonth(s.StringId);
                            bool capReached = !BeastsOfTheNorthMath.HasCapRemaining(bought, BeastsOfTheNorthMath.WolfRiderMonthlyCap);
                            int fish = FishCarried();
                            int gold = Hero.MainHero?.Gold ?? 0;
                            bool canAfford = BeastsOfTheNorthMath.CanAffordFish(fish, BeastsOfTheNorthMath.WolfRiderFishCost)
                                          && BeastsOfTheNorthMath.CanAffordGold(gold, BeastsOfTheNorthMath.WolfRiderGoldCost);

                            string cost = $"[{BeastsOfTheNorthMath.WolfRiderFishCost} fish, {BeastsOfTheNorthMath.WolfRiderGoldCost} denars]";
                            if (capReached)
                                MBTextManager.SetTextVariable("BEASTS_RIDER_TEXT", "Bind an Ulfhednar  (none left to bind this month)");
                            else if (!canAfford)
                                MBTextManager.SetTextVariable("BEASTS_RIDER_TEXT", $"Bind an Ulfhednar  {cost} (lacking)");
                            else
                                MBTextManager.SetTextVariable("BEASTS_RIDER_TEXT", $"Bind an Ulfhednar  {cost}");

                            args.IsEnabled = !capReached && canAfford;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoBindRider(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Actions ──────────────────────────────────────────────────────────────
        private static void DoRaiseGiant()
        {
            Settlement s = Settlement.CurrentSettlement;
            if (!IsBeastsOfTheNorthTown(s)) return;
            if (!BeastsOfTheNorthMath.HasCapRemaining(GiantPurchasedThisMonth(s.StringId), BeastsOfTheNorthMath.GiantMonthlyCap)) return;

            Hero hero = Hero.MainHero;
            int fish = FishCarried();
            int gold = hero?.Gold ?? 0;
            if (!BeastsOfTheNorthMath.CanAffordFish(fish, BeastsOfTheNorthMath.GiantFishCost)) return;
            if (!BeastsOfTheNorthMath.CanAffordGold(gold, BeastsOfTheNorthMath.GiantGoldCost)) return;

            var troop = MBObjectManager.Instance?.GetObject<CharacterObject>(GiantTroopId);
            if (troop == null) return;

            try
            {
                SpendFish(BeastsOfTheNorthMath.GiantFishCost);
                hero.ChangeHeroGold(-BeastsOfTheNorthMath.GiantGoldCost);
                MobileParty.MainParty?.MemberRoster?.AddToCounts(troop, 1);
                RecordGiantPurchase(s.StringId);

                InformationManager.DisplayMessage(new InformationMessage(
                    "Something answers the old rite that is not quite a man. The Jotunn-Blooded falls in "
                  + "behind you, and the ground remembers every step.",
                    new Color(0.55f, 0.6f, 0.7f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoBindRider()
        {
            Settlement s = Settlement.CurrentSettlement;
            if (!IsBeastsOfTheNorthTown(s)) return;
            if (!BeastsOfTheNorthMath.HasCapRemaining(RiderPurchasedThisMonth(s.StringId), BeastsOfTheNorthMath.WolfRiderMonthlyCap)) return;

            Hero hero = Hero.MainHero;
            int fish = FishCarried();
            int gold = hero?.Gold ?? 0;
            if (!BeastsOfTheNorthMath.CanAffordFish(fish, BeastsOfTheNorthMath.WolfRiderFishCost)) return;
            if (!BeastsOfTheNorthMath.CanAffordGold(gold, BeastsOfTheNorthMath.WolfRiderGoldCost)) return;

            var troop = MBObjectManager.Instance?.GetObject<CharacterObject>(WolfRiderTroopId);
            if (troop == null) return;

            try
            {
                SpendFish(BeastsOfTheNorthMath.WolfRiderFishCost);
                hero.ChangeHeroGold(-BeastsOfTheNorthMath.WolfRiderGoldCost);
                MobileParty.MainParty?.MemberRoster?.AddToCounts(troop, 1);
                RecordRiderPurchase(s.StringId);

                InformationManager.DisplayMessage(new InformationMessage(
                    "An Ulfhednar swings up onto a horse the way a wolf remembers being one, and waits for "
                  + "you to give the order it is already straining against.",
                    new Color(0.55f, 0.6f, 0.7f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
