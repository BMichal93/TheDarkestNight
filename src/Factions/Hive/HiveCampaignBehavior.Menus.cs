// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveCampaignBehavior.Menus.cs
//
// Game menu implementation for the Hive's network — Hive towns only.
// Mirrors Factions/WolfBrothers/WolfBrothersCampaignBehavior.Menus.cs /
// Factions/Tower/TowerCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Answer the network"
//     → "hive_network_main"  (description)
//       → "hive_network_drink"     (drink the elixir — joins if not Integrated,
//                                    redoses if already Integrated; either way
//                                    gated behind an InquiryData confirmation —
//                                    this is the "ceremony", not a bare
//                                    ChangeKingdomAction call)
//       → "hive_network_recruit"   (recruit one prisoner into your army, free)
//       → "hive_network_leave"
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public partial class HiveCampaignBehavior
    {
        private static void RegisterHiveMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "hive_network_enter", "{HIVE_NETWORK_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!HiveSettlements.IsHiveSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("HIVE_NETWORK_ENTER_TEXT", "Answer the network");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("hive_network_main", "{HIVE_NETWORK_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("HIVE_NETWORK_MAIN_TEXT",
                            "The air here is thick with spores that never quite settle. Somewhere below the "
                          + "floorboards, roots as thick as a man's arm pulse in a slow rhythm that is not quite "
                          + "a heartbeat. A shallow bowl of the elixir waits on a stone table, refilled by hands "
                          + "no one ever sees move.");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterDrinkOption(starter);
            RegisterRecruitOption(starter);

            try
            {
                starter.AddGameMenuOption("hive_network_main", "hive_network_leave", "Leave the network",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Drink the elixir — the join / redose ritual ─────────────────────────
        private static void RegisterDrinkOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("hive_network_main", "hive_network_drink", "{HIVE_NETWORK_DRINK_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool already = IsIntegrated(Hero.MainHero);
                            MBTextManager.SetTextVariable("HIVE_NETWORK_DRINK_TEXT",
                                already ? "Drink the elixir (renew the bond)" : "Drink the elixir and join the network");
                            args.IsEnabled = true;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { OfferDrink(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void OfferDrink()
        {
            bool already = IsIntegrated(Hero.MainHero);
            string title = already ? "Renew The Bond" : "Drink The Elixir";
            string body = already
                ? "The bowl is still warm. Drink again, and the network's grip on you does not loosen this week."
                : "The bowl holds something that is not quite wine and not quite rot. Drink, and you stop being one voice.";

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    title, body, true, true, "Drink", "Not yet",
                    () => { try { DoDrink(already); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    null));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoDrink(bool wasAlreadyIntegrated)
        {
            var hero = Hero.MainHero;
            if (hero == null) return;

            if (!wasAlreadyIntegrated)
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == HiveCulture.CultureId && !k.IsEliminated);
                var clan = hero.Clan;
                if (kingdom == null || clan == null)
                {
                    ShowDialog("The Network Recoils", "Something in the rite falters — the Hive cannot bind you right now.",
                        () => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                    return;
                }

                try
                {
                    if (clan.Kingdom != null && clan.Kingdom != kingdom)
                        ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
                    if (clan.Kingdom == null || clan.Kingdom != kingdom)
                        ChangeKingdomAction.ApplyByJoinToKingdom(clan, kingdom, CampaignTime.Never, false);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // OnClanChangedKingdom (HiveCampaignBehavior.cs) registers the dose for
                // every hero in the clan the instant the kingdom-change event fires —
                // no separate RegisterOrRedose call needed here for the join case.
                ShowDialog("Integrated", "The elixir goes down like cold iron, then warmth. You are Integrated now — the network hears you, and you hear it.",
                    () => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            else
            {
                RegisterOrRedose(hero);
                ShowDialog("The Bond Renews", "The dose settles. The network's voice is a little louder in your thoughts again.",
                    () => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
        }

        // ── Recruit a prisoner into your army, free ──────────────────────────────
        private static void RegisterRecruitOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("hive_network_main", "hive_network_recruit", "{HIVE_NETWORK_RECRUIT_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetWeakestPrisoner(out var character);
                            string note = has ? $"  [{character.Name} — no cost]" : "  [no prisoners to absorb]";
                            MBTextManager.SetTextVariable("HIVE_NETWORK_RECRUIT_TEXT", "Recruit a prisoner into your army" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoFreeRecruit(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoFreeRecruit()
        {
            if (!TryGetWeakestPrisoner(out var character))
            {
                ShowDialog("Nothing To Absorb", "You hold no prisoners the network can take in.",
                    () => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            var party = MobileParty.MainParty;
            party.PrisonRoster.AddToCounts(character, -HiveMath.FreeRecruitPerClick);
            party.MemberRoster.AddToCounts(character, HiveMath.FreeRecruitPerClick);

            ShowDialog("Absorbed", $"{character.Name} stops struggling the moment the spores reach them. They march under your banner now — the network does not charge for what it wants to keep.",
                () => { try { GameMenu.SwitchToMenu("hive_network_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        private static bool TryGetWeakestPrisoner(out CharacterObject character)
        {
            character = null;
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
