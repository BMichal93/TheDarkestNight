// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Tower/TowerRiteHostParty.cs
//
// The demon host summoned by the Tower's failed Great Rite. Spawns
// TowerRiteMath.HostPartyCount separate war-bands near Iyakis, the same
// proven BanditPartyComponent.CreateBanditParty way every other roaming
// special party in this mod is built (see WolfHuntBeastParty.Spawn,
// ApocalypseCampaignBehavior.Gathering's SpawnGatheringParty,
// DemonSpawnCampaignBehavior.SpawnAmbushNear).
//
// Deliberately NOT registered with DemonSpawnCampaignBehavior's night-tide
// tracking dictionary (same reasoning as WolfHuntBeastParty): that system
// despawns every tracked party at dawn, which would let the rampage vanish
// before it ever raids anything. This host persists across day and night,
// exactly like Apocalypse's "Gathering" band, until the rampage clock runs
// out (TowerRiteMath.RampageDurationDays) or the player kills every band.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    internal static class TowerRiteHostParty
    {
        private static readonly Random _rng = new Random();

        private static List<string> _hostPartyIds = new List<string>();
        private static string       _remnantPartyId = null;

        internal static void SyncData(IDataStore store)
        {
            try { store.SyncData("TWRRITE_HostIds",     ref _hostPartyIds); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("TWRRITE_RemnantId",   ref _remnantPartyId); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            if (_hostPartyIds == null) _hostPartyIds = new List<string>();
        }

        internal static void ResetForNewGame()
        {
            _hostPartyIds = new List<string>();
            _remnantPartyId = null;
        }

        private static IEnumerable<MobileParty> LiveHostParties()
        {
            foreach (var id in _hostPartyIds.ToList())
            {
                MobileParty p = null;
                try { p = MobileParty.All.FirstOrDefault(x => x != null && x.StringId == id); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (p == null || !p.IsActive) { _hostPartyIds.Remove(id); continue; }
                yield return p;
            }
        }

        internal static int LiveHostCount()
        {
            try { return LiveHostParties().Count(); } catch { return 0; }
        }

        // ── Spawn ─────────────────────────────────────────────────────────────
        internal static void SpawnHost()
        {
            _hostPartyIds = new List<string>();
            Vec2 anchor = IyakisPosition();

            for (int i = 0; i < TowerRiteMath.HostPartyCount; i++)
                try { SpawnOneBand(anchor, i); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Vec2 IyakisPosition()
        {
            try
            {
                var iyakis = Settlement.All.FirstOrDefault(s => s != null && TowerMath.IsStartingTownId(s.StringId));
                if (iyakis != null) return iyakis.GetPosition2D;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return MobileParty.MainParty?.GetPosition2D ?? default;
        }

        private static void SpawnOneBand(Vec2 anchor, int bandIndex)
        {
            Clan banditClan = Clan.BanditFactions.FirstOrDefault(c => c != null && !c.IsEliminated);
            if (banditClan == null) return;
            var pt = banditClan.DefaultPartyTemplate;
            if (pt == null) return;

            Hideout hideout = Settlement.All
                .Where(s => s?.Hideout != null)
                .OrderBy(s => (s.GetPosition2D - anchor).LengthSquared)
                .FirstOrDefault()?.Hideout;
            if (hideout == null) return;

            var cvec = new CampaignVec2(anchor, true);
            string partyId = "towerrite_host_" + _rng.Next(999999).ToString("D6");
            MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, banditClan, hideout, false, pt, cvec);
            if (party == null) return;

            try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            for (int i = 0; i < TowerRiteMath.HostPartySizeEach; i++)
            {
                DemonMath.DemonTier tier = TowerRiteMath.HostTier(_rng);
                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId);
                if (troop == null) continue;
                try { party.MemberRoster.AddToCounts(troop, 1); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }

            try { party.Party.SetCustomName(new TextObject("The Rite-Torn Host")); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            _hostPartyIds.Add(party.StringId);
        }

        // ── Daily: send free bands raiding ───────────────────────────────────────
        internal static void TickRampage()
        {
            foreach (var party in LiveHostParties().ToList())
            {
                try
                {
                    if (party.MapEvent != null || party.BesiegedSettlement != null) continue;
                    if (!TowerRiteMath.RollHostRaids(_rng.NextDouble())) continue;

                    Settlement target = Settlement.All
                        .Where(s => s != null && (s.IsTown || s.IsCastle))
                        .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                        .FirstOrDefault();
                    if (target == null) continue;

                    try { party.SetMoveRaidSettlement(target, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The Rite-Torn Host surges against {target.Name}.", new Color(0.60f, 0.05f, 0.05f)));
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // ── End of rampage: leave one shrunk remnant behind, clear the rest ──────
        internal static void EndRampageLeaveRemnant()
        {
            var live = LiveHostParties().ToList();
            MobileParty keep = live.FirstOrDefault();
            foreach (var party in live)
            {
                try
                {
                    if (party == keep) continue;
                    DestroyPartyAction.Apply(party.Party, null);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }

            _hostPartyIds.Clear();
            if (keep == null) { _remnantPartyId = null; return; }

            try
            {
                int have = keep.MemberRoster?.TotalManCount ?? 0;
                int trim = have - TowerRiteMath.RemnantPartySize;
                if (trim > 0)
                {
                    var entries = keep.MemberRoster.GetTroopRoster().ToList();
                    foreach (var entry in entries)
                    {
                        if (trim <= 0) break;
                        if (entry.Character == null || entry.Number <= 0) continue;
                        int cut = Math.Min(trim, entry.Number);
                        keep.MemberRoster.AddToCounts(entry.Character, -cut);
                        trim -= cut;
                    }
                }
                try { keep.Party.SetCustomName(new TextObject("The Rite's Remnant")); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                _remnantPartyId = keep.StringId;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); _remnantPartyId = null; }
        }

        // If every host band is killed before the rampage clock runs out, there
        // is nothing left to shrink into a remnant — the rite's aftermath still
        // fires (the relation hit stands regardless), it just has no lingering
        // demon presence to point at.
        internal static void ClearAllHosts()
        {
            foreach (var party in LiveHostParties().ToList())
            {
                try { DestroyPartyAction.Apply(party.Party, null); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            _hostPartyIds.Clear();
        }
    }
}
