// =============================================================================
// THE DARKEST NIGHT — BeastsOfTheNorth/BeastsOfTheNorthCampaignBehavior.cs
//
// "Seek the Old Blood" — Wolf Brothers (Sturgia) towns' costly recruitment of
// two special troops (NPCCharacters "aae_jotunn_blooded" / "aae_ulfhednar" in
// ModuleData/troops.xml): the Jotunn-Blooded (a giant) and the Ulfhednar (a
// wolf-rider mounted on "aae_ulfhednar_mount" from ModuleData/items.xml).
// Paid in fish (see BeastsOfTheNorthMath's header for why fish, not "meat")
// plus a modest gold toll, capped per town per month. Menu UI is in
// BeastsOfTheNorthCampaignBehavior.Menus.cs.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public partial class BeastsOfTheNorthCampaignBehavior : CampaignBehaviorBase
    {
        internal const string GiantTroopId     = "aae_jotunn_blooded";
        internal const string WolfRiderTroopId = "aae_ulfhednar";

        // Mission-side: the Jotunn-Blooded stands a true head-and-shoulders
        // above other men — a real skeleton scale (BeastsOfTheNorthMath.
        // GiantAgentScale), not just the maxed body sliders troops.xml gives
        // it. Called from MagicMissionBehavior.OnAgentBuild for every agent;
        // no-op unless the troop id matches. (troops.xml's original note that
        // "there is no runtime agent-scale hook in this build" predates the
        // demon tiers proving Agent.SetInitialAgentScale real — see
        // DemonFactory.SetAgentScale.)
        internal static void TryApplyGiantScale(TaleWorlds.MountAndBlade.Agent agent)
        {
            try
            {
                if (agent == null || agent.IsMount) return;
                string id = null;
                try { id = agent.Character?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (!string.Equals(id, GiantTroopId, StringComparison.OrdinalIgnoreCase)) return;
                DemonFactory.SetAgentScale(agent, BeastsOfTheNorthMath.GiantAgentScale);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Per-town monthly purchase tally, one set of parallel lists per
        // recruit kind — same shape as ForeignMusterCampaignBehavior's weekly
        // tally (a stored month number that no longer matches the current
        // month reads as "0 purchased this month").
        private static readonly List<string> _giantTownIds  = new List<string>();
        private static readonly List<long>   _giantMonths   = new List<long>();
        private static readonly List<int>    _giantCounts   = new List<int>();

        private static readonly List<string> _riderTownIds  = new List<string>();
        private static readonly List<long>   _riderMonths   = new List<long>();
        private static readonly List<int>    _riderCounts   = new List<int>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                SyncTally(dataStore, "BOTN_GiantTowns", "BOTN_GiantMonths", "BOTN_GiantCounts",
                    _giantTownIds, _giantMonths, _giantCounts);
                SyncTally(dataStore, "BOTN_RiderTowns", "BOTN_RiderMonths", "BOTN_RiderCounts",
                    _riderTownIds, _riderMonths, _riderCounts);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void SyncTally(IDataStore dataStore, string idsKey, string monthsKey, string countsKey,
            List<string> townIds, List<long> months, List<int> counts)
        {
            var ids  = townIds.ToList();
            var mos  = months.ToList();
            var cnts = counts.ToList();

            dataStore.SyncData(idsKey, ref ids);
            dataStore.SyncData(monthsKey, ref mos);
            dataStore.SyncData(countsKey, ref cnts);

            if (ids != null && mos != null && cnts != null && ids.Count == mos.Count && ids.Count == cnts.Count)
            {
                townIds.Clear(); months.Clear(); counts.Clear();
                for (int i = 0; i < ids.Count; i++)
                {
                    townIds.Add(ids[i]);
                    months.Add(mos[i]);
                    counts.Add(cnts[i]);
                }
            }
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterBeastsOfTheNorthMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Gating ────────────────────────────────────────────────────────────
        internal static bool IsBeastsOfTheNorthTown(Settlement s)
        {
            if (s == null || !s.IsTown) return false;
            try { return s.MapFaction?.StringId == WolfBrothersCulture.CultureId; }
            catch { return false; }
        }

        internal static long CurrentMonthNumber()
        {
            try { return (long)(CampaignTime.Now.ToDays / BeastsOfTheNorthMath.DaysPerMonth); }
            catch { return 0; }
        }

        // ── Monthly cap tracking ─────────────────────────────────────────────
        internal static int GiantPurchasedThisMonth(string townStringId) =>
            PurchasedThisMonth(townStringId, _giantTownIds, _giantMonths, _giantCounts);

        internal static int RiderPurchasedThisMonth(string townStringId) =>
            PurchasedThisMonth(townStringId, _riderTownIds, _riderMonths, _riderCounts);

        internal static void RecordGiantPurchase(string townStringId) =>
            RecordPurchase(townStringId, _giantTownIds, _giantMonths, _giantCounts);

        internal static void RecordRiderPurchase(string townStringId) =>
            RecordPurchase(townStringId, _riderTownIds, _riderMonths, _riderCounts);

        private static int PurchasedThisMonth(string townStringId, List<string> townIds, List<long> months, List<int> counts)
        {
            long month = CurrentMonthNumber();
            int idx = townIds.IndexOf(townStringId);
            if (idx < 0 || months[idx] != month) return 0;
            return counts[idx];
        }

        private static void RecordPurchase(string townStringId, List<string> townIds, List<long> months, List<int> counts)
        {
            long month = CurrentMonthNumber();
            int idx = townIds.IndexOf(townStringId);
            if (idx < 0)
            {
                townIds.Add(townStringId);
                months.Add(month);
                counts.Add(1);
                return;
            }
            if (months[idx] != month)
            {
                months[idx] = month;
                counts[idx] = 1;
            }
            else
            {
                counts[idx] += 1;
            }
        }

        // ── The player's fish stores ─────────────────────────────────────────
        internal static int FishCarried()
        {
            try
            {
                var fish = MBObjectManager.Instance?.GetObject<ItemObject>(BeastsOfTheNorthMath.TributeItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (fish == null || roster == null) return 0;
                return roster.GetItemNumber(fish);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }

        internal static void SpendFish(int amount)
        {
            try
            {
                var fish = MBObjectManager.Instance?.GetObject<ItemObject>(BeastsOfTheNorthMath.TributeItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (fish == null || roster == null) return;
                roster.AddToCounts(fish, -amount);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
