// =============================================================================
// THE DARKEST NIGHT — ForeignMuster/ForeignMusterCampaignBehavior.Menus.cs
//
// Game menu implementation for the Foreign Muster — a single "town" option in
// every Legion (empire_w) town. Mirrors AshenRecruitCampaignBehavior.Menus.cs
// (AI/AshenRecruitCampaignBehavior.Menus.cs)'s condition/consequence shape:
// disabled-with-hint when unavailable, a plain gold purchase when it is.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public partial class ForeignMusterCampaignBehavior
    {
        private static void RegisterForeignMusterMenus(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "foreign_muster_enter", "{FOREIGN_MUSTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            Settlement s = Settlement.CurrentSettlement;
                            if (!IsForeignMusterTown(s)) return false;

                            CultureObject culture = WeeklyCulture(s);
                            CharacterObject recruit = culture?.BasicTroop;
                            if (culture == null || recruit == null) return false;

                            int cost = RecruitCostAt(s);
                            int bought = PurchasedThisWeek(s.StringId);
                            bool capReached = !ForeignMusterMath.HasPurchasesRemaining(bought);
                            int have = Hero.MainHero?.Gold ?? 0;
                            bool canAfford = have >= cost;

                            if (capReached)
                            {
                                MBTextManager.SetTextVariable("FOREIGN_MUSTER_TEXT",
                                    $"The Foreign Muster (no more {culture.Name} volunteers left this week)");
                                args.IsEnabled = false;
                            }
                            else if (!canAfford)
                            {
                                MBTextManager.SetTextVariable("FOREIGN_MUSTER_TEXT",
                                    $"The Foreign Muster — hire a {recruit.Name} ({culture.Name})  [{cost} denars, cannot afford]");
                                args.IsEnabled = false;
                            }
                            else
                            {
                                MBTextManager.SetTextVariable("FOREIGN_MUSTER_TEXT",
                                    $"The Foreign Muster — hire a {recruit.Name} ({culture.Name})  [{cost} denars, {ForeignMusterMath.WeeklyPurchaseCap - bought} left this week]");
                                args.IsEnabled = true;
                            }
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Recruit; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { DoMuster(Settlement.CurrentSettlement); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoMuster(Settlement s)
        {
            if (!IsForeignMusterTown(s)) return;

            CultureObject culture = WeeklyCulture(s);
            CharacterObject recruit = culture?.BasicTroop;
            if (culture == null || recruit == null) return;

            if (!ForeignMusterMath.HasPurchasesRemaining(PurchasedThisWeek(s.StringId))) return;

            int cost = RecruitCostAt(s);
            Hero hero = Hero.MainHero;
            if (hero == null || hero.Gold < cost) return;

            try
            {
                hero.ChangeHeroGold(-cost);
                MobileParty.MainParty?.MemberRoster?.AddToCounts(recruit, 1);
                RecordPurchase(s.StringId);

                InformationManager.DisplayMessage(new InformationMessage(
                    $"A {culture.Name} auxiliary takes the Legion's coin and falls in with your column. " +
                    "It will not be the last foreign blade Legion buys this week.",
                    new Color(0.7f, 0.65f, 0.55f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
