// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestCampaignBehavior.Menu.cs
//
// "Stock the ark" — the Ortysia town submenu. Each option hands over the
// player's entire carried stock of one material, capped to what's still
// needed. Mirrors NorthmenStonesCampaignBehavior.Menu.cs's exact shape
// (RegisterMaterialOption), scoped to Ortysia only and to the two tracks
// LegionQuestMath.cs defines.
// =============================================================================

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
    public sealed partial class LegionQuestCampaignBehavior
    {
        private static bool AtOrtysia()
        {
            try { return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == OrtysiaSettlement(); }
            catch { return false; }
        }

        private static ItemObject GetItem(string id)
        {
            try { return MBObjectManager.Instance?.GetObject<ItemObject>(id); }
            catch { return null; }
        }

        private static void RegisterMenus(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "legq_stock_enter", "{LEGQ_STOCK_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseGathering) return false;
                            if (!AtOrtysia()) return false;
                            int pct = (int)(LegionQuestMath.BlendedProgress(_hardwood, _iron) * 100f);
                            MBTextManager.SetTextVariable("LEGQ_STOCK_ENTER_TEXT",
                                $"Stock the ark with hardwood and iron  [{pct}%]");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("legq_stock_menu"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenu("legq_stock_menu", "{LEGQ_STOCK_HEADER}", args =>
                {
                    try
                    {
                        string ownerNote = IsOrtysiaLegionOwned()
                            ? "Ortysia still flies Legion's banner. What is stowed here stays stowed."
                            : "Ortysia has fallen out of Legion hands. What is stowed here is bleeding away, " +
                              "ten in every hundred lost with every week it stays lost.";
                        MBTextManager.SetTextVariable("LEGQ_STOCK_HEADER",
                            "Shipwrights work the ark's ribs and decking by turns, waiting on what the hull still " +
                            "needs.\n\n" +
                            $"Hardwood: {_hardwood:N0} / {LegionQuestMath.HardwoodTarget:N0}\n" +
                            $"Iron: {_iron:N0} / {LegionQuestMath.IronTarget:N0}\n\n" +
                            ownerNote);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterMaterialOption(starter, "hardwood", "Hardwood", () => _hardwood, v => _hardwood = v, LegionQuestMath.HardwoodTarget);
            RegisterMaterialOption(starter, "iron",     "Iron",     () => _iron,     v => _iron = v,     LegionQuestMath.IronTarget);

            try
            {
                starter.AddGameMenuOption("legq_stock_menu", "legq_stock_leave", "Step away",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RegisterMaterialOption(
            CampaignGameStarter starter, string itemId, string label,
            System.Func<int> get, System.Action<int> set, int target)
        {
            string optId = $"legq_stock_{itemId}";
            string varId = $"LEGQ_STOCK_{itemId.ToUpperInvariant()}_TEXT";
            try
            {
                starter.AddGameMenuOption("legq_stock_menu", optId, "{" + varId + "}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseGathering) return false;
                            var item = GetItem(itemId);
                            int held = item != null ? (MobileParty.MainParty?.ItemRoster?.GetItemNumber(item) ?? 0) : 0;
                            MBTextManager.SetTextVariable(varId, $"Give {label} you carry ({held})");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Continue; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = item != null && held > 0 && get() < target;
                            return true;
                        }
                        catch { return false; }
                    },
                    args =>
                    {
                        try
                        {
                            var item = GetItem(itemId);
                            var party = MobileParty.MainParty;
                            if (item == null || party?.ItemRoster == null) return;
                            int held = party.ItemRoster.GetItemNumber(item);
                            int remaining = target - get();
                            int give = System.Math.Min(held, remaining);
                            if (give <= 0) return;
                            party.ItemRoster.AddToCounts(item, -give);
                            set(get() + give);
                            NotifyStockChanged($"{give} {label} given to the ark.");
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
