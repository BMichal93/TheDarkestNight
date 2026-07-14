// =============================================================================
// THE DARKEST NIGHT — Factions/ForestWidows/ForestWidowsCampaignBehavior.Menus.cs
//
// Game menu implementation for the Forest Widows' altar — Forest Widows
// towns only. Mirrors PaleWidowsCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Offer to the dark"
//     → "forestwidows_altar_main"          (description)
//       → "forestwidows_altar_soldier"     (sacrifice a soldier -> 1 day ignored per soldier, stacks)
//       → "forestwidows_altar_prisoner"    (sacrifice a captive lord -> demon troops + immunity)
//       → "forestwidows_altar_kinsman"     (sacrifice a male clan member -> demon troops + immunity)
//       → "forestwidows_altar_leave"
//
// A male player rolls ForestWidowsMath.RollSelfSacrifice on EVERY use of the
// prisoner-lord or clan-member option (the heavier offerings) — the dark is
// not always particular about whose blood pays a lordly debt. Failing the
// roll cancels the intended reward and instead applies
// ForestWidowsMath.SelfSacrificeHpFloor/RenownLoss to the player himself: a
// real, felt consequence (near-death HP, a renown hit) short of permanent
// character death.
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
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class ForestWidowsCampaignBehavior
    {
        private static readonly Random _menuRng = new Random();

        private static void RegisterForestWidowsMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "forestwidows_altar_enter", "{FORESTWIDOWS_ALTAR_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!ForestWidowsSettlements.IsForestWidowsSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("FORESTWIDOWS_ALTAR_ENTER_TEXT", "Offer to the dark");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("forestwidows_altar_main", "{FORESTWIDOWS_ALTAR_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("FORESTWIDOWS_ALTAR_MAIN_TEXT",
                            "The altar in the root-cellar is old stone scrubbed pale by centuries of use, and "
                          + "newly busy again. The Widows keep the bargain alive here — a soldier's life buys "
                          + "days of being overlooked; a lord's life buys soldiers of the dark itself, for a "
                          + "season.");
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            RegisterSoldierOption(starter);
            RegisterPrisonerOption(starter);
            RegisterKinsmanOption(starter);

            try
            {
                starter.AddGameMenuOption("forestwidows_altar_main", "forestwidows_altar_leave", "Leave the altar",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Option A: sacrifice a soldier of your own ────────────────────────────
        private static void RegisterSoldierOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("forestwidows_altar_main", "forestwidows_altar_soldier", "{FORESTWIDOWS_ALTAR_SOLDIER_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetWeakestTroop(out var character);
                            string note = has
                                ? $"  [{character.Name} — buys {(int)ForestWidowsMath.IgnoreDaysPerSoldierSacrificed} day(s) unseen]"
                                : "  [no soldiers to spare]";
                            MBTextManager.SetTextVariable("FORESTWIDOWS_ALTAR_SOLDIER_TEXT", "Give a soldier to the dark" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoSacrificeSoldier(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoSacrificeSoldier()
        {
            if (!TryGetWeakestTroop(out var character))
            {
                ShowDialog("Nothing To Give", "You have no soldiers you can spare.",
                    () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            var party = MobileParty.MainParty;
            party.MemberRoster.AddToCounts(character, -1);
            GrantIgnoreDays(Hero.MainHero, ForestWidowsMath.IgnoreDaysPerSoldierSacrificed);

            ShowDialog("Given", $"{character.Name} is taken by the dark without a sound. The road ahead is quieter for it. "
                + $"({(int)ForestWidowsMath.IgnoreDaysPerSoldierSacrificed} day(s) unseen)",
                () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Option B: sacrifice a captive lord ───────────────────────────────────
        private static void RegisterPrisonerOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("forestwidows_altar_main", "forestwidows_altar_prisoner", "{FORESTWIDOWS_ALTAR_PRISONER_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetMalePrisonerLord(out var hero);
                            string note = has ? $"  [{hero.Name}]" : "  [no captive lord to offer]";
                            MBTextManager.SetTextVariable("FORESTWIDOWS_ALTAR_PRISONER_TEXT", "Offer a captive lord" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoSacrificePrisonerLord(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoSacrificePrisonerLord()
        {
            if (!TryGetMalePrisonerLord(out var hero))
            {
                ShowDialog("Nothing To Give", "You hold no captive lord you can offer.",
                    () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            if (RollSelfSacrificeIfMalePlayer())
                return; // the player paid the price instead — no reward, see the roll's own dialog

            var executor = Hero.MainHero;
            try { KillCharacterAction.ApplyByExecution(hero, executor); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            GrantLordlyReward();

            ShowDialog("Taken", $"{hero.Name} is led to the altar and does not come back from it. The dark holds up its end of the bargain.",
                () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Option C: sacrifice a male clan member ───────────────────────────────
        private static void RegisterKinsmanOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("forestwidows_altar_main", "forestwidows_altar_kinsman", "{FORESTWIDOWS_ALTAR_KINSMAN_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetMaleClanMember(out var hero);
                            string note = has ? $"  [{hero.Name}]" : "  [no man of your clan to offer]";
                            MBTextManager.SetTextVariable("FORESTWIDOWS_ALTAR_KINSMAN_TEXT", "Offer a man of your own clan" + note);
                            args.IsEnabled = has;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoSacrificeKinsman(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoSacrificeKinsman()
        {
            if (!TryGetMaleClanMember(out var hero))
            {
                ShowDialog("Nothing To Give", "There is no man of your clan you can offer.",
                    () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
                return;
            }

            if (RollSelfSacrificeIfMalePlayer())
                return;

            try { KillCharacterAction.ApplyByMurder(hero, Hero.MainHero, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            GrantLordlyReward();

            ShowDialog("Taken", $"{hero.Name} walks to the altar without being made to. The dark holds up its end of the bargain.",
                () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
        }

        // ── Shared reward / self-sacrifice roll for the two lordly options ───────
        private static void GrantLordlyReward()
        {
            float immunityDays = ForestWidowsMath.RollLordSacrificeImmunityDays(_menuRng.NextDouble());
            GrantIgnoreDays(Hero.MainHero, immunityDays);

            float vanishDays = ForestWidowsMath.RollVanishDays(_menuRng.NextDouble());
            GrantDemonTroops(Hero.MainHero, DemonCatalog.FiendTroopId, ForestWidowsMath.DemonTroopsGrantedPerMaleSacrifice, vanishDays);
        }

        // Returns true if the player himself paid the price instead (caller
        // should stop — no reward, the dialog for the consequence is already
        // shown here).
        private static bool RollSelfSacrificeIfMalePlayer()
        {
            Hero player = Hero.MainHero;
            if (player == null || player.IsFemale) return false;
            if (!ForestWidowsMath.RollSelfSacrifice(_menuRng.NextDouble())) return false;

            try { player.HitPoints = ForestWidowsMath.SelfSacrificeHpFloor; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ClanRenown.Lose(player.Clan, ForestWidowsMath.SelfSacrificeRenownLoss); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ShowDialog("The Altar Chooses", "The Widows' knives are not always particular about whose blood pays the debt. "
                + "Hands seize you before you can protest — when they let go, you are bled near to death and the court has "
                + "watched every moment of it.",
                () => { try { GameMenu.SwitchToMenu("forestwidows_altar_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } });
            return true;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static bool TryGetWeakestTroop(out CharacterObject character)
        {
            character = null;
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
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetMalePrisonerLord(out Hero hero)
        {
            hero = null;
            try
            {
                var party = MobileParty.MainParty;
                if (party == null) return false;
                hero = Hero.AllAliveHeroes.FirstOrDefault(h =>
                    h != null && h.IsPrisoner && h.PartyBelongedToAsPrisoner == party.Party
                    && !h.IsFemale && h.IsLord);
                return hero != null;
            }
            catch { return false; }
        }

        private static bool TryGetMaleClanMember(out Hero hero)
        {
            hero = null;
            try
            {
                var clan = Clan.PlayerClan;
                hero = clan?.Heroes?.FirstOrDefault(h =>
                    h != null && h.IsAlive && !h.IsChild && !h.IsFemale
                    && h != Hero.MainHero && !h.IsPrisoner);
                return hero != null;
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
