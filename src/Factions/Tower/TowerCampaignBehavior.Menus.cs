// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerCampaignBehavior.Menus.cs
//
// Game menu implementation for the Tower's two teachings — Tower towns only.
// Mirrors WolfBrothersCampaignBehavior.Menus.cs / Crystals/CrystallinesCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Seek the Tower's teaching"
//     → "tower_teaching_main"  (description)
//       → "tower_teaching_spell_0".."_5"  (learn one of 6 formulas offered this week, for influence)
//       → "tower_teaching_transmute"       (surrender a tier-2+ soldier for a Hollow Choir caster, for influence)
//       → "tower_teaching_leave"
//
// The 6-formula offer refreshes weekly (TowerMath.OfferSeedForDay) and skips
// formulas the player already knows, so the same visit never repeats a
// lesson already learned. Learning here calls SpellbookCampaignBehavior's own
// LearnSpellFromTower — indistinguishable from learning the formula any
// other way (a found ruin scroll, a lucky battlefield guess).
// =============================================================================

using System;
using System.Collections.Generic;
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
    public partial class TowerCampaignBehavior
    {
        private static void RegisterTowerMenus(CampaignGameStarter starter)
        {
            RegisterTownEntry(starter);
            RegisterMainMenu(starter);
        }

        // ── Town entry ─────────────────────────────────────────────────────────
        private static void RegisterTownEntry(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "tower_teaching_enter", "{TOWER_TEACHING_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!TowerSettlements.IsTowerSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("TOWER_TEACHING_ENTER_TEXT", "Seek the Tower's teaching");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Main menu ──────────────────────────────────────────────────────────
        private static void RegisterMainMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenu("tower_teaching_main", "{TOWER_TEACHING_MAIN_TEXT}", args =>
                {
                    try
                    {
                        MBTextManager.SetTextVariable("TOWER_TEACHING_MAIN_TEXT",
                            "Shelves of formulas climb the walls, each guarded as jealously as coin elsewhere. "
                          + "The Tower will teach what it knows, or remake a soldier into something that reads "
                          + "the Fire — for those who bring influence enough to be worth the lesson.");
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            RegisterSpellOptions(starter);
            RegisterTransmuteOption(starter);

            try
            {
                starter.AddGameMenuOption("tower_teaching_main", "tower_teaching_leave", "Leave the Tower",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── The six offered formulas ──────────────────────────────────────────
        // Weekly-stable, deterministic subset of the whole SpellbookCatalog,
        // filtered down to formulas the player does not already know.
        private static List<SpellDef> CurrentOffer()
        {
            int day = 0;
            try { day = (int)CampaignTime.Now.ToDays; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            int seed = TowerMath.OfferSeedForDay(day);

            var unknown = SpellbookCatalog.All
                .Where(d => !SpellbookCampaignBehavior.KnowsSpell(d.Id))
                .ToList();
            var picked = TowerMath.PickRandomSubset(unknown, TowerMath.SpellOffersPerVisit, seed);
            return picked;
        }

        private static void RegisterSpellOptions(CampaignGameStarter starter)
        {
            for (int slot = 0; slot < TowerMath.SpellOffersPerVisit; slot++)
            {
                int captured = slot; // closure capture
                try
                {
                    starter.AddGameMenuOption("tower_teaching_main", $"tower_teaching_spell_{captured}",
                        $"{{TOWER_TEACHING_SPELL_{captured}_TEXT}}",
                        args =>
                        {
                            try
                            {
                                var offer = CurrentOffer();
                                if (captured >= offer.Count)
                                {
                                    MBTextManager.SetTextVariable($"TOWER_TEACHING_SPELL_{captured}_TEXT", "  (no further lesson offered this week)");
                                    return false;
                                }

                                var def = offer[captured];
                                int cost = TowerMath.SpellInfluenceCost(def.Length);
                                MBTextManager.SetTextVariable($"TOWER_TEACHING_SPELL_{captured}_TEXT",
                                    $"Learn {def.Name}  [{cost} influence]");
                                args.IsEnabled = (Hero.MainHero?.Clan?.Influence ?? 0f) >= cost;
                                try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            }
                            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            return true;
                        },
                        args => { try { DoLearnSpell(captured); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static void DoLearnSpell(int slot)
        {
            var offer = CurrentOffer();
            if (slot >= offer.Count)
            {
                ShowDialog("Nothing Left To Teach", "The Tower has nothing further to teach you this week.",
                    () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            var def = offer[slot];
            int cost = TowerMath.SpellInfluenceCost(def.Length);
            var clan = Hero.MainHero?.Clan;
            if (clan == null || clan.Influence < cost)
            {
                ShowDialog("Insufficient Influence", $"You need {cost} influence for this lesson.",
                    () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            try { clan.Influence -= cost; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellbookCampaignBehavior.LearnSpellFromTower(def.Id); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            ShowDialog("Taught", $"The Tower teaches you {def.Name}. (-{cost} influence)",
                () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
        }

        // ── Transmute a soldier into a Hollow Choir spellcaster ────────────────
        private static void RegisterTransmuteOption(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("tower_teaching_main", "tower_teaching_transmute", "{TOWER_TEACHING_TRANSMUTE_TEXT}",
                    args =>
                    {
                        try
                        {
                            bool has = TryGetTransmuteCandidate(out var character, out int rank, out var troopId, out int cost);
                            string note = has
                                ? $"  [{character.Name} -> {troopId} — {cost} influence]"
                                : "  [no soldier of tier 2 or higher to spare]";
                            MBTextManager.SetTextVariable("TOWER_TEACHING_TRANSMUTE_TEXT", "Remake a soldier into a spellcaster" + note);
                            args.IsEnabled = has && (Hero.MainHero?.Clan?.Influence ?? 0f) >= cost;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { DoTransmute(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoTransmute()
        {
            if (!TryGetTransmuteCandidate(out var character, out int rank, out var troopId, out int cost))
            {
                ShowDialog("Nothing To Remake", "You have no soldier of tier 2 or higher to spare.",
                    () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            var clan = Hero.MainHero?.Clan;
            if (clan == null || clan.Influence < cost)
            {
                ShowDialog("Insufficient Influence", $"You need {cost} influence for this working.",
                    () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            var hollow = MBObjectManager.Instance?.GetObject<CharacterObject>(troopId);
            if (hollow == null)
            {
                ShowDialog("The Working Fails", "Something in the rite falters — the Tower cannot complete it right now.",
                    () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
                return;
            }

            var party = MobileParty.MainParty;
            party.MemberRoster.AddToCounts(character, -1);
            party.MemberRoster.AddToCounts(hollow, 1);
            try { clan.Influence -= cost; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            ShowDialog("Remade", $"{character.Name} is led into the Tower and returns changed. (+1 {hollow.Name}, -{cost} influence)",
                () => { try { GameMenu.SwitchToMenu("tower_teaching_main"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } });
        }

        // Picks the weakest tier-2+ troop the player carries (mirrors
        // WolfBrothersCampaignBehavior.Menus.TryGetWeakestTroop's "spend the
        // least valuable qualifying unit" pattern) and resolves the Hollow
        // Choir rank/cost it converts into.
        private static bool TryGetTransmuteCandidate(out CharacterObject character, out int rank, out string troopId, out int cost)
        {
            character = null; rank = 0; troopId = null; cost = 0;
            try
            {
                var party = MobileParty.MainParty;
                if (party?.MemberRoster == null) return false;
                var entry = party.MemberRoster.GetTroopRoster()
                    .Where(e => e.Character != null && !e.Character.IsHero && e.Number > 0
                             && e.Character.Tier >= TowerMath.MinTransmuteTier
                             && !SpellcasterTroopCatalog.IsHollowChoirTroop(e.Character.StringId))
                    .OrderBy(e => e.Character.Tier)
                    .FirstOrDefault();
                if (entry.Character == null) return false;

                character = entry.Character;
                int resolvedRank = TowerMath.HollowChoirRankForTier(entry.Character.Tier);
                rank = resolvedRank;
                var tier = SpellcasterTroopCatalog.Tiers.FirstOrDefault(t => t.Rank == resolvedRank);
                troopId = tier.TroopId;
                if (string.IsNullOrEmpty(troopId)) return false;
                cost = TowerMath.TransmuteInfluenceCost(rank);
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
                try { onClose?.Invoke(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }
    }
}
