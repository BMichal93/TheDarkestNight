// =============================================================================
// THE DARKEST NIGHT — ForeignMuster/ForeignMusterCampaignBehavior.cs
//
// The Foreign Muster — Legion's old habit of folding auxiliaries into its
// ranks. Every "empire_w" (Legion) town offers ONE other main culture's
// tier-1 recruit, rotating weekly (ForeignMusterMath.PickCulture), capped at
// ForeignMusterMath.WeeklyPurchaseCap purchases per town per week. The price
// matches what the TOWN'S OWN culture's tier-1 recruit would cost via the
// live PartyWageModel, so this never undercuts ordinary recruiting. Menu UI
// is in ForeignMusterCampaignBehavior.Menus.cs.
//
// No campaign save data is required to pick the week's culture (it's a pure
// function of the week number + town id — see ForeignMusterMath), but the
// per-town weekly purchase cap IS persisted, so a save/reload mid-week can't
// be used to reset it and buy past the cap.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public partial class ForeignMusterCampaignBehavior : CampaignBehaviorBase
    {
        // Per-town weekly purchase tally: parallel lists (townStringId, the
        // week number that count applies to, the count itself). A stored
        // weekNumber that no longer matches the current week reads as "0
        // purchased this week" — no pruning needed, the list only grows to
        // the number of distinct Legion towns ever visited (at most a
        // handful over a campaign).
        private static readonly List<string> _townIds     = new List<string>();
        private static readonly List<long>   _weekNumbers = new List<long>();
        private static readonly List<int>    _counts      = new List<int>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                var ids    = _townIds.ToList();
                var weeks  = _weekNumbers.ToList();
                var counts = _counts.ToList();

                dataStore.SyncData("FMUSTER_TownIds", ref ids);
                dataStore.SyncData("FMUSTER_Weeks",    ref weeks);
                dataStore.SyncData("FMUSTER_Counts",   ref counts);

                if (ids != null && weeks != null && counts != null
                    && ids.Count == weeks.Count && ids.Count == counts.Count)
                {
                    _townIds.Clear(); _weekNumbers.Clear(); _counts.Clear();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        _townIds.Add(ids[i]);
                        _weekNumbers.Add(weeks[i]);
                        _counts.Add(counts[i]);
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterForeignMusterMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Gating ────────────────────────────────────────────────────────────
        internal static bool IsForeignMusterTown(Settlement s)
        {
            if (s == null || !s.IsTown) return false;
            try { return s.MapFaction?.StringId == LegionCulture.KingdomId; }
            catch { return false; }
        }

        internal static long CurrentWeekNumber()
        {
            try { return (long)CampaignTime.Now.ToWeeks; }
            catch { return 0; }
        }

        // The culture on offer at `settlement` this week — null if it cannot
        // be resolved (mod-conflict-safe: the option simply won't show).
        internal static CultureObject WeeklyCulture(Settlement settlement)
        {
            if (settlement == null) return null;
            try
            {
                string cultureId = ForeignMusterMath.PickCulture(CurrentWeekNumber(), settlement.StringId);
                return MBObjectManager.Instance?.GetObject<CultureObject>(cultureId);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // ── Weekly cap tracking ───────────────────────────────────────────────
        internal static int PurchasedThisWeek(string townStringId)
        {
            long week = CurrentWeekNumber();
            int idx = _townIds.IndexOf(townStringId);
            if (idx < 0 || _weekNumbers[idx] != week) return 0;
            return _counts[idx];
        }

        internal static void RecordPurchase(string townStringId)
        {
            long week = CurrentWeekNumber();
            int idx = _townIds.IndexOf(townStringId);
            if (idx < 0)
            {
                _townIds.Add(townStringId);
                _weekNumbers.Add(week);
                _counts.Add(1);
                return;
            }
            if (_weekNumbers[idx] != week)
            {
                _weekNumbers[idx] = week;
                _counts[idx] = 1;
            }
            else
            {
                _counts[idx] += 1;
            }
        }

        // ── Recruitment cost — matches the town's own culture's tier-1 recruit ──
        internal static int RecruitCostAt(Settlement settlement)
        {
            try
            {
                var ownCulture = settlement?.Town?.Culture;
                var ownRecruit = ownCulture?.BasicTroop;
                if (ownRecruit == null) return 0;
                var model = Campaign.Current?.Models?.PartyWageModel;
                if (model == null) return 0;
                return model.GetTroopRecruitmentCost(ownRecruit, Hero.MainHero, false).RoundedResultNumber;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }
    }
}
