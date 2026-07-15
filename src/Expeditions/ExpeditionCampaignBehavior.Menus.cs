// =============================================================================
// THE DARKEST NIGHT — Expeditions/ExpeditionCampaignBehavior.Menus.cs
//
// Town-menu flow for "Charter an Expedition" (the Antiquarian Charter):
//   "town" → "exp_charter_main"
//     → leader select (ShowMultiSelectionInquiry)
//       → team select (ShowMultiSelectionInquiry)
//         → destination select (ShowMultiSelectionInquiry, filtered against
//           AshenRuinSystem.IsOnCooldown / IsContested)
//           → confirmation (InquiryData) → LaunchExpedition
//
// Gated on the settlement belonging to The Camp (Revyl's mercenary
// city-state kingdom) via CityStateSystem.IsCampSettlement — the same
// MapFaction-membership check LegionSettlements.IsLegionSettlement/
// WolfBrothersSettlements.IsWolfBrothersSettlement use for their own
// kingdoms. Formerly gated on Legion ownership; moved here wholesale
// (mechanics unchanged, cost now gold instead of influence — see
// ExpeditionMath.GoldCost — and text reflavoured to the Camp's mercenary
// voice). NOT gated on MageKnowledge.IsMage.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public partial class ExpeditionCampaignBehavior
    {
        internal static int ActiveDaysLeft => _activeDaysLeft;

        // ── Menu registration ────────────────────────────────────────────────
        private static void RegisterExpeditionMenus(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "exp_charter_enter", "{EXP_CHARTER_ENTER}",
                    args =>
                    {
                        try
                        {
                            var s = Settlement.CurrentSettlement;
                            if (s == null || !s.IsTown) return false;
                            if (!CityStateSystem.IsCampSettlement(s)) return false;

                            string note = _active ? "  [charter already under way]" : "";
                            MBTextManager.SetTextVariable("EXP_CHARTER_ENTER", $"Charter an Expedition{note}");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("exp_charter_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenu("exp_charter_main", "{EXP_CHARTER_HDR}", args =>
                {
                    try
                    {
                        string hdr = _active
                            ? $"The Antiquarian Charter\n\nA charter is already in the field. Word will come back in "
                            + $"{_activeDaysLeft} day{(_activeDaysLeft != 1 ? "s" : "")}, one way or another."
                            : "The Antiquarian Charter\n\nThe Camp doesn't care whose ruin it was, only what it's worth: "
                            + "pay in coin, not favors, and name a leader, fund a team, choose a ruin, and wait for what "
                            + "comes back — if anything does. Business is business.";
                        MBTextManager.SetTextVariable("EXP_CHARTER_HDR", hdr);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenuOption("exp_charter_main", "exp_charter_begin", "Begin the Charter",
                    args =>
                    {
                        try
                        {
                            args.IsEnabled = !_active;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args =>
                    {
                        try { GameMenu.SwitchToMenu("town"); ShowLeaderSelection(); }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenuOption("exp_charter_main", "exp_charter_leave", "Leave",
                    args =>
                    {
                        try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        return true;
                    },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Step A: leader ────────────────────────────────────────────────────
        private static void ShowLeaderSelection()
        {
            try
            {
                if (_leaderPool.Count == 0) GenerateFreshPool();
                var offered = PickRandomLeaders(3);
                if (offered.Count == 0) return;

                var elements = offered.Select(l =>
                    new InquiryElement(l.Id, $"{l.Name}{(l.Proven ? "  [proven]" : "")}", null, true,
                        $"{SpecialtyName(l.Specialty)} — {SpecialtyBlurb(l.Specialty)}")).ToList();

                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Charter an Expedition — Choose a Leader",
                    "Three names are put forward for the charter, each carrying a strength worth funding and a weakness worth weighing.",
                    elements, true, 1, 1, "Choose", "Back",
                    chosen =>
                    {
                        try
                        {
                            if (chosen == null || chosen.Count == 0) return;
                            string leaderId = chosen[0].Identifier as string;
                            if (leaderId == null) return;
                            ShowTeamSelection(leaderId);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static List<LeaderRecord> PickRandomLeaders(int count)
        {
            var pool = _leaderPool.ToList();
            var result = new List<LeaderRecord>();
            int n = Math.Min(count, pool.Count);
            for (int i = 0; i < n; i++)
            {
                int idx = _rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }

        // ── Step B: core team ─────────────────────────────────────────────────
        private static void ShowTeamSelection(string leaderId)
        {
            try
            {
                var teams = (ExpeditionTeamType[])Enum.GetValues(typeof(ExpeditionTeamType));
                var elements = teams.Select(t =>
                    new InquiryElement((int)t, TeamName(t), null, true, TeamBlurb(t))).ToList();

                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Charter an Expedition — Core Team",
                    "Every charter needs a spine — the hired hands who do the digging, and the dying, if it comes to that.",
                    elements, true, 1, 1, "Choose", "Back",
                    chosen =>
                    {
                        try
                        {
                            if (chosen == null || chosen.Count == 0) return;
                            var team = (ExpeditionTeamType)(int)chosen[0].Identifier;
                            ShowDestinationSelection(leaderId, team);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Step C: destination ───────────────────────────────────────────────
        private static void ShowDestinationSelection(string leaderId, ExpeditionTeamType team)
        {
            try
            {
                var leader = FindLeader(leaderId);
                if (leader == null) return;

                var candidates = AshenRuinDefs.All
                    .Where(r => !AshenRuinSystem.IsOnCooldown(r.VillageName) && !AshenRuinSystem.IsContested(r.VillageName))
                    // Temple Wardens refuse the darkest sites.
                    .Where(r => team != ExpeditionTeamType.TempleWardens || r.Tier < RuinTier.Legendary)
                    .OrderBy(r => r.Tier).ThenBy(r => r.RuinName)
                    .Take(60)
                    .ToList();

                if (candidates.Count == 0)
                {
                    MBInformationManager.AddQuickInformation(new TextObject(
                        "No ruin currently answers the charter — every site is disturbed, contested, or too dark for this team."));
                    return;
                }

                var elements = candidates.Select(r =>
                {
                    int chance = ExpeditionMath.SuccessChance(r.Tier, leader.Specialty, team, leader.Proven);
                    int days   = ExpeditionMath.DurationDays(r.Tier, leader.Specialty, team);
                    int cost   = ExpeditionMath.GoldCost(r.Tier, team);
                    string label = $"{r.RuinName}  [{TierRiskLabel(r.Tier)}]";
                    string hint  = $"Success: {chance}%  |  Duration: {days} day(s)  |  Cost: {cost} denars";
                    return new InquiryElement(r.VillageName, label, null, true, hint);
                }).ToList();

                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    "Charter an Expedition — Destination",
                    "The antiquarians spread their maps. Choose where the charter digs.",
                    elements, true, 1, 1, "Choose", "Back",
                    chosen =>
                    {
                        try
                        {
                            if (chosen == null || chosen.Count == 0) return;
                            string village = chosen[0].Identifier as string;
                            if (village == null) return;
                            ShowConfirmation(leaderId, team, village);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Step D: confirm and pay ───────────────────────────────────────────
        private static void ShowConfirmation(string leaderId, ExpeditionTeamType team, string village)
        {
            try
            {
                var def = AshenRuinDefs.All.FirstOrDefault(r => r.VillageName == village);
                var leader = FindLeader(leaderId);
                if (def == null || leader == null) return;

                int chance = ExpeditionMath.SuccessChance(def.Tier, leader.Specialty, team, leader.Proven);
                int days   = ExpeditionMath.DurationDays(def.Tier, leader.Specialty, team);
                int cost   = ExpeditionMath.GoldCost(def.Tier, team);

                int gold = 0;
                try { gold = Hero.MainHero?.Gold ?? 0; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                string body = $"{leader.Name} will lead {TeamName(team)} to {def.RuinName}  [{TierRiskLabel(def.Tier)}].\n\n"
                            + $"Success chance: {chance}%\nDuration: {days} day(s)\nCost: {cost} denars "
                            + "(paid now, not refunded on failure)\n\n"
                            + $"Your denars: {gold}";

                InformationManager.ShowInquiry(new InquiryData(
                    "Seal the Charter", body, true, true, "Seal the Charter", "Back",
                    () => LaunchExpedition(leaderId, team, village, cost, days),
                    () => { }), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void LaunchExpedition(string leaderId, ExpeditionTeamType team, string village, int cost, int days)
        {
            try
            {
                if (_active)
                {
                    MBInformationManager.AddQuickInformation(new TextObject("A charter is already under way — wait for it to resolve."));
                    return;
                }
                if (Hero.MainHero == null) return;

                // The Camp trades in coin, not clan influence — check
                // affordability, THEN deduct, same guard-before-spend pattern
                // every other gold-cost menu in the mod uses (Wands/Talismans/
                // ForeignMuster/Sanctuary).
                if (Hero.MainHero.Gold < cost)
                {
                    MBInformationManager.AddQuickInformation(new TextObject("Insufficient denars — the charter cannot be sealed."));
                    return;
                }
                try { Hero.MainHero.ChangeHeroGold(-cost); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                _active         = true;
                _activeLeaderId = leaderId;
                _activeVillage  = village;
                _activeTeam     = (int)team;
                _activeDaysLeft = days;

                MBInformationManager.AddQuickInformation(new TextObject($"The charter is sealed. Word will come in {days} day(s)."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Flavour text helpers ──────────────────────────────────────────────
        private static string SpecialtyName(ExpeditionLeaderSpecialty s) => s switch
        {
            ExpeditionLeaderSpecialty.ScholarOfTheOldScript     => "Scholar of the Old Script",
            ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads => "Veteran of the Southern Roads",
            ExpeditionLeaderSpecialty.TombRobber                => "Tomb-Robber",
            ExpeditionLeaderSpecialty.ZealousAntiquarian         => "Zealous Antiquarian",
            _ => s.ToString(),
        };

        private static string SpecialtyBlurb(ExpeditionLeaderSpecialty s) => s switch
        {
            ExpeditionLeaderSpecialty.ScholarOfTheOldScript =>
                "Reads the old warnings before they matter. Better odds against the deepest sites, but careful work is slow work.",
            ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads =>
                "Has done this before and does not linger. Faster in and out, but takes less than a thorough hand would.",
            ExpeditionLeaderSpecialty.TombRobber =>
                "Knows what to grab and where. Brings back more — and sometimes brings back less than promised, for themself.",
            ExpeditionLeaderSpecialty.ZealousAntiquarian =>
                "Believes in the work more than is strictly safe. Better odds, and a small chance the wrong eye notices.",
            _ => "",
        };

        // Display names only — the underlying ExpeditionTeamType enum (and its
        // mechanical profile in ExpeditionMath) is unchanged by the move to
        // The Camp. LegionVeterans/ImperialScholars read as Legion-owned
        // branding that no longer fits a mercenary free-camp; HiredBlades and
        // TempleWardens already read as outside talent the Camp is buying in.
        private static string TeamName(ExpeditionTeamType t) => t switch
        {
            ExpeditionTeamType.LegionVeterans   => "Retired Sellswords",
            ExpeditionTeamType.ImperialScholars => "Wandering Scholars",
            ExpeditionTeamType.HiredBlades      => "Hired Blades",
            ExpeditionTeamType.TempleWardens    => "Temple Wardens",
            _ => t.ToString(),
        };

        private static string TeamBlurb(ExpeditionTeamType t) => t switch
        {
            ExpeditionTeamType.LegionVeterans   => "Old company men who took their discharge in coin, not a pension. Steadier under pressure, less likely to lose the leader — but they do not come cheap.",
            ExpeditionTeamType.ImperialScholars => "Sharper against the deepest sites, but the first thing to break when it goes wrong.",
            ExpeditionTeamType.HiredBlades      => "Cheap, quick, and loyal exactly as far as the coin reaches.",
            ExpeditionTeamType.TempleWardens    => "Steady against the cold and the Ashen — and unwilling to go anywhere truly dark.",
            _ => "",
        };

        private static string TierRiskLabel(RuinTier t) => t switch
        {
            RuinTier.Easy     => "Quiet",
            RuinTier.Standard => "Restless",
            RuinTier.Brutal   => "Brutal",
            _                 => "Doomed", // Legendary
        };
    }
}
