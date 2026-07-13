// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersCampaignBehavior.Menus.cs
//
// Game menu implementation for the pack's larder — Wolf Brothers towns only.
// Mirrors Crystals/CrystallinesCampaignBehavior.Menus.cs / AI/AshenRecruitCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Render flesh for the pack"
//     → "wolfbrothers_larder_main"  (description)
//       → "wolfbrothers_larder_troop"      (render your weakest soldier)
//       → "wolfbrothers_larder_prisoner"   (render your weakest prisoner)
//       → "wolfbrothers_larder_leave"
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

namespace AshAndEmber
{
    public partial class WolfBrothersCampaignBehavior
    {
        private static void RegisterWolfBrothersMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "wolfbrothers_larder_enter", "{WOLFBROTHERS_LARDER_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!WolfBrothersSettlements.IsWolfBrothersSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("WOLFBROTHERS_LARDER_ENTER_TEXT", "Render flesh for the pack");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("wolfbrothers_larder_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("wolfbrothers_larder_main", "{WOLFBROTHERS_LARDER_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("WOLFBROTHERS_LARDER_MAIN_TEXT",
                            "The larder smells of smoke and salt. Nothing here is wasted — a fallen soldier or a "
                          + "captive too much trouble to keep both feed the pack the same way, and the pack does "
                          + "not ask which.");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterTroopOption(starter);
            RegisterPrisonerOption(starter);

            try
            {
                starter.AddGameMenuOption("wolfbrothers_larder_main", "wolfbrothers_larder_leave", "Leave the larder",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Render a soldier from your own ranks ────────────────────────────────
        private static void RegisterTroopOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("wolfbrothers_larder_main", "wolfbrothers_larder_troop", "{WOLFBROTHERS_LARDER_TROOP_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetWeakestTroop(out var character, out int tier);
                            string note = has
                                ? $"  [{character.Name} — yields {WolfBrothersMath.MeatFromTroop(tier)} meat]"
                                : "  [no soldiers to spare]";
                            MBTextManager.SetTextVariable("WOLFBROTHERS_LARDER_TROOP_TEXT", "Render one of your own soldiers" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoRenderTroop(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Render a prisoner ────────────────────────────────────────────────────
        private static void RegisterPrisonerOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("wolfbrothers_larder_main", "wolfbrothers_larder_prisoner", "{WOLFBROTHERS_LARDER_PRISONER_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetWeakestPrisoner(out var character, out int tier);
                            string note = has
                                ? $"  [{character.Name} — yields {WolfBrothersMath.MeatFromPrisoner(tier)} meat]"
                                : "  [no prisoners to spare]";
                            MBTextManager.SetTextVariable("WOLFBROTHERS_LARDER_PRISONER_TEXT", "Render a prisoner" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoRenderPrisoner(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Actions ──────────────────────────────────────────────────────────────
        private static void DoRenderTroop()
        {
            if (!TryGetWeakestTroop(out var character, out int tier))
            {
                ShowDialog("The Larder Has Nothing To Take", "You have no soldiers you can spare.",
                    () => { try { GameMenu.SwitchToMenu("wolfbrothers_larder_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            var party = MobileParty.MainParty;
            party.MemberRoster.AddToCounts(character, -1);
            int meat = WolfBrothersMath.MeatFromTroop(tier);
            GrantMeat(meat);

            ShowDialog("Rendered", $"{character.Name} is given to the larder. The pack eats tonight. (+{meat} meat)",
                () => { try { GameMenu.SwitchToMenu("wolfbrothers_larder_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        private static void DoRenderPrisoner()
        {
            if (!TryGetWeakestPrisoner(out var character, out int tier))
            {
                ShowDialog("The Larder Has Nothing To Take", "You hold no prisoners you can spare.",
                    () => { try { GameMenu.SwitchToMenu("wolfbrothers_larder_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            var party = MobileParty.MainParty;
            party.PrisonRoster.AddToCounts(character, -1);
            int meat = WolfBrothersMath.MeatFromPrisoner(tier);
            GrantMeat(meat);

            ShowDialog("Rendered", $"The captive does not leave the larder standing. (+{meat} meat)",
                () => { try { GameMenu.SwitchToMenu("wolfbrothers_larder_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static bool TryGetWeakestTroop(out CharacterObject character, out int tier)
        {
            character = null; tier = 0;
            try
            {
                var party = MobileParty.MainParty;
                if (party?.MemberRoster == null) return false;
                var entry = party.MemberRoster.GetTroopRoster()
                    .Where(e => e.Character != null && !e.Character.IsHero && e.Number > 0)
                    .OrderBy(e => e.Character.Tier)
                    .FirstOrDefault();
                if (entry.Character == null) return false;
                character = entry.Character;
                tier = entry.Character.Tier;
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetWeakestPrisoner(out CharacterObject character, out int tier)
        {
            character = null; tier = 0;
            try
            {
                var party = MobileParty.MainParty;
                if (party?.PrisonRoster == null) return false;
                var entry = party.PrisonRoster.GetTroopRoster()
                    .Where(e => e.Character != null && !e.Character.IsHero && e.Number > 0)
                    .OrderBy(e => e.Character.Tier)
                    .FirstOrDefault();
                if (entry.Character == null) return false;
                character = entry.Character;
                tier = entry.Character.Tier;
                return true;
            }
            catch { return false; }
        }

        private static void GrantMeat(int amount)
        {
            try
            {
                var meat = MBObjectManager.Instance?.GetObject<ItemObject>("meat");
                var party = MobileParty.MainParty;
                if (meat != null && party?.ItemRoster != null)
                    party.ItemRoster.AddToCounts(meat, amount);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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
