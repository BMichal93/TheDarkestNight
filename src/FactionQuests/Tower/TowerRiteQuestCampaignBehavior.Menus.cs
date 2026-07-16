// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Tower/TowerRiteQuestCampaignBehavior.Menus.cs
//
// The gather/delivery menu at Iyakis, once the Warlock has set the Unbinding
// Rite in motion (PhaseGathering). Mirrors WolfHuntQuestCampaignBehavior.
// Menus.cs's "town" option shape. Delivery consumes the exact quantities
// required (TowerRiteMath.Gather*Required) straight out of the player's own
// party ItemRoster — relics of ANY RelicCatalog entry count toward the relic
// tally, so the player is never forced to hunt one specific named relic.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public sealed partial class TowerRiteQuestCampaignBehavior
    {
        private static void RegisterRiteMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "towerrite_enter", "{TOWERRITE_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseGathering) return false;
                            if (!TowerSettlements.IsTowerSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("TOWERRITE_ENTER_TEXT", "Bring the grave-goods to the Great Rite");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { ShowRiteMenu(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ShowRiteMenu()
        {
            int relics = HeldRelicCount();
            int blood = HeldItemCount(BloodboundCatalog.DemonBloodItemId);
            int sigils = HeldItemCount(TempleSigilCatalog.HolySigilItemId);
            bool ready = TowerRiteMath.MeetsGatherThreshold(relics, blood, sigils);

            string body =
                "The Warlock's altar waits at the heart of the Tower, three empty settings cut into the stone.\n\n" +
                $"Relics carried: {relics} / {TowerRiteMath.GatherRelicsRequired}\n" +
                $"Demon Blood carried: {blood} / {TowerRiteMath.GatherDemonBloodRequired}\n" +
                $"Holy Sigils carried: {sigils} / {TowerRiteMath.GatherHolySigilsRequired}\n\n" +
                (ready
                    ? "Everything the rite calls for is in your saddlebags. Speak the working, or walk away and keep it a little longer."
                    : "You do not yet carry enough of everything the rite calls for.");

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Great Rite",
                    body,
                    ready, true,
                    "Speak the working.",
                    "Leave",
                    ready ? (Action)PerformRite : null,
                    () => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } }
                ), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void PerformRite()
        {
            try
            {
                SpendItems(RelicCatalog.AllItemIds(), TowerRiteMath.GatherRelicsRequired);
                SpendItem(BloodboundCatalog.DemonBloodItemId, TowerRiteMath.GatherDemonBloodRequired);
                SpendItem(TempleSigilCatalog.HolySigilItemId, TowerRiteMath.GatherHolySigilsRequired);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Rite Fails",
                    "The Warlock lays every offering on the altar and speaks the working. For one heartbeat the " +
                    "air folds shut around Iyakis — then folds the wrong way. The seal does not close. It tears. " +
                    "Something vast, and hungry, and not remotely bound, comes through where the door used to be.",
                    true, false, "So be it.", "",
                    () => { try { StartRampage(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    null
                ), true, true);
            }
            catch (System.Exception logEx)
            {
                TheDarkestNight.ModLog.Error(logEx);
                StartRampage();
            }
        }

        // ── Inventory helpers ─────────────────────────────────────────────────────
        private static int HeldItemCount(string itemId)
        {
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return 0;
                return roster.GetItemNumber(item);
            }
            catch { return 0; }
        }

        private static int HeldRelicCount()
        {
            int total = 0;
            foreach (var id in RelicCatalog.AllItemIds()) total += HeldItemCount(id);
            return total;
        }

        private static void SpendItem(string itemId, int amount)
        {
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null || amount <= 0) return;
                roster.AddToCounts(item, -amount);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Spends `amount` total units across however many of `itemIds` the
        // player happens to be carrying — so any mix of relics satisfies the
        // relic tally rather than requiring one specific named relic.
        private static void SpendItems(string[] itemIds, int amount)
        {
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster == null || itemIds == null) return;
                int remaining = amount;
                foreach (var id in itemIds)
                {
                    if (remaining <= 0) break;
                    var item = MBObjectManager.Instance?.GetObject<ItemObject>(id);
                    if (item == null) continue;
                    int have = roster.GetItemNumber(item);
                    if (have <= 0) continue;
                    int take = Math.Min(have, remaining);
                    roster.AddToCounts(item, -take);
                    remaining -= take;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
