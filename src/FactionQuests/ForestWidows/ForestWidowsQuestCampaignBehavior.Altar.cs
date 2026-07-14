// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestCampaignBehavior.Altar.cs
//
// The Final Peace's own donation option, added to the EXISTING
// "forestwidows_altar_main" menu (see Factions/ForestWidows/
// ForestWidowsCampaignBehavior.Menus.cs) rather than a new menu tree — the
// same root-cellar stone the day-to-day altar already uses, now also
// counting toward the Grand Widow's larger bargain. Mirrors
// GreatAwakeningCampaignBehavior.Altar.cs's "give everything you're
// holding at once" contribution shape: the player empties their current
// prisoner roster in one action rather than a slow one-at-a-time menu
// click, since reaching ForestWidowsQuestMath.SacrificeTarget one soldier
// at a time (like the old altar) would take an unreasonable number of menu
// visits. NPC contribution (Forest Widows lords occasionally feeding their
// own prisoners into the count) mirrors
// GreatAwakeningCampaignBehavior.NpcContribution.cs's weekly-roll pattern.
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed partial class ForestWidowsQuestCampaignBehavior
    {
        internal static void RegisterQuestAltarMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("forestwidows_altar_main", "fwq_pledge", "{FWQ_PLEDGE_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseAccumulating) return false;
                            int held = PlayerPrisonerCount();
                            string note = held > 0 ? $"  [{held} to give]" : "  [you hold no prisoners]";
                            MBTextManager.SetTextVariable("FWQ_PLEDGE_TEXT",
                                $"Pledge every prisoner you hold to the Grand Widow's lasting peace " +
                                $"[{_menSacrificed:N0} / {ForestWidowsQuestMath.SacrificeTarget:N0}]{note}");
                            args.IsEnabled = held > 0;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Continue; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args => DoPledge());
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static int PlayerPrisonerCount()
        {
            try
            {
                var roster = MobileParty.MainParty?.PrisonRoster?.GetTroopRoster();
                return roster?.Where(e => e.Character != null && !e.Character.IsHero).Sum(e => e.Number) ?? 0;
            }
            catch { return 0; }
        }

        private static void DoPledge()
        {
            try
            {
                var party = MobileParty.MainParty;
                var roster = party?.PrisonRoster?.GetTroopRoster()?.ToList();
                if (roster == null) return;

                int given = 0;
                foreach (var entry in roster)
                {
                    if (entry.Character == null || entry.Character.IsHero || entry.Number <= 0) continue;
                    party.PrisonRoster.AddToCounts(entry.Character, -entry.Number);
                    given += entry.Number;
                }
                if (given <= 0) return;

                AddSacrifice(given);

                MBInformationManager.AddQuickInformation(new TextObject(
                    $"{given} are led to the altar and do not come back. The Grand Widow's ledger stands at " +
                    $"{_menSacrificed:N0} / {ForestWidowsQuestMath.SacrificeTarget:N0}."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── NPC contribution — background trickle ────────────────────────────────
        private void NpcContributionWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;

            Kingdom widows = GetForestWidowsKingdom();
            if (widows == null) return;

            int total = 0;
            try
            {
                foreach (Hero lord in widows.Heroes.Where(h =>
                             h != null && h.IsLord && h.IsAlive && !h.IsChild && h != Hero.MainHero).ToList())
                {
                    var party = lord.PartyBelongedTo;
                    var roster = party?.PrisonRoster;
                    if (roster == null) continue;
                    int held = roster.GetTroopRoster().Where(e => e.Character != null && !e.Character.IsHero).Sum(e => e.Number);
                    if (held <= 0) continue;
                    if (_rng.NextDouble() >= ForestWidowsQuestMath.NpcWeeklyContributionChance) continue;

                    int give = ForestWidowsQuestMath.NpcContributionAmount(_rng, held);
                    if (give <= 0) continue;

                    int left = give;
                    foreach (var entry in roster.GetTroopRoster().Where(e => e.Character != null && !e.Character.IsHero).ToList())
                    {
                        if (left <= 0) break;
                        int take = System.Math.Min(entry.Number, left);
                        roster.AddToCounts(entry.Character, -take);
                        left -= take;
                    }
                    total += give - left;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (total > 0) AddSacrifice(total);
        }

        // ── Threshold check — hands off to .Resolution.cs the moment it's met ───
        private void CheckThresholdWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;
            if (!ForestWidowsQuestMath.HasReachedThreshold(_menSacrificed)) return;

            _phase = PhaseAwaitingChoice;
            try { ShowResolutionChoice(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
