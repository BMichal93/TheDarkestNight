// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntBeastParty.cs
//
// The campaign-map party that carries the current stage's named beast and its
// pack. Spawned the same proven way every other roaming special party in this
// mod is (BanditPartyComponent.CreateBanditParty — see GreatOtherParty.Summon,
// DemonLordSystem.TryAppear, DemonSpawnCampaignBehavior.SpawnAmbushNear),
// seeded entirely with WolfHuntMath.BeastTier(stage) troops.
//
// Deliberately NOT registered with DemonSpawnCampaignBehavior's night-tide
// tracking dictionary — that system despawns every tracked party at dawn and
// replenishes survivors nightly, which would let a hunt target vanish before
// the player ever finds it (or silently heal back up). A hunt target must
// persist exactly like GreatOtherParty/DemonLordSystem's own hosts do: alive
// until killed, tracked only here, cleared only when the mission confirms the
// pack is dead (see WolfHuntQuestCampaignBehavior.TickStageProgress).
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal static class WolfHuntBeastParty
    {
        private static readonly Random _rng = new Random();

        private static string _partyId = null;
        private static int    _stage   = -1;

        internal static void SyncData(IDataStore store)
        {
            try { store.SyncData("WLFHUNT_BeastPartyId", ref _partyId); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("WLFHUNT_BeastStage",   ref _stage); }   catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void ResetForNewGame()
        {
            _partyId = null;
            _stage = -1;
        }

        // The stage index the CURRENTLY TRACKED party (alive or just killed)
        // belongs to — -1 if nothing has ever been spawned. The campaign
        // behavior compares this against its own "which stage am I on" state
        // to know whether a fresh spawn or a completion check is due.
        internal static int CurrentStage => _stage;

        private static MobileParty CurrentParty()
        {
            if (string.IsNullOrEmpty(_partyId)) return null;
            try { return MobileParty.All.FirstOrDefault(p => p != null && p.StringId == _partyId); }
            catch { return null; }
        }

        internal static bool IsAlive() => CurrentParty() != null;

        // ── Spawn ─────────────────────────────────────────────────────────────
        internal static bool Spawn(int stage, Vec2 anchor)
        {
            if (!WolfHuntMath.IsValidStage(stage)) return false;
            try
            {
                Clan banditClan = Clan.BanditFactions.FirstOrDefault(c => c != null && !c.IsEliminated);
                if (banditClan == null) return false;
                var pt = banditClan.DefaultPartyTemplate;
                if (pt == null) return false;

                Hideout hideout = Settlement.All
                    .Where(s => s?.Hideout != null)
                    .OrderBy(s => (s.GetPosition2D - anchor).LengthSquared)
                    .FirstOrDefault()?.Hideout;
                if (hideout == null) return false;

                var cvec = new CampaignVec2(anchor, true);
                string partyId = "wolfhunt_beast_" + _rng.Next(999999).ToString("D6");
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, banditClan, hideout, false, pt, cvec);
                if (party == null) return false;

                try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                DemonMath.DemonTier tier = WolfHuntMath.BeastTier(stage);
                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId);
                if (troop != null)
                    party.MemberRoster.AddToCounts(troop, WolfHuntMath.PackSize(stage));

                try { party.Party.SetCustomName(new TextObject(WolfHuntCatalog.PackTitle(stage))); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                _partyId = party.StringId;
                _stage = stage;
                return true;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
        }

        // ── Weekly: roam near Wolf Brothers ground so the pack can be found ─────
        internal static void WeeklyTick()
        {
            var party = CurrentParty();
            if (party == null) return;
            if (party.MapEvent != null || party.BesiegedSettlement != null) return;
            if (_rng.NextDouble() >= 0.5) return;

            try
            {
                var towns = Settlement.All
                    .Where(s => s != null && s.IsTown && WolfBrothersSettlements.IsWolfBrothersSettlement(s))
                    .ToList();
                if (towns.Count == 0) return;
                Settlement target = towns[_rng.Next(towns.Count)];
                party.SetMoveGoToSettlement(target, MobileParty.NavigationType.Default, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Mission hookup — mirrors DemonSpawnCampaignBehavior's
        // PendingVariant hookup, generalised to DemonBattleBehavior.
        // PendingBossMultiplier so OnAgentBuild scales every demon in a
        // mission that genuinely involves this tracked party. ─────────────────
        internal static void OnMapEventStarted(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null || string.IsNullOrEmpty(_partyId)) { DemonBattleBehavior.PendingBossMultiplier = 1f; return; }
                bool involved = false;
                foreach (var side in new[] { mapEvent.AttackerSide, mapEvent.DefenderSide })
                {
                    if (side == null) continue;
                    if (side.Parties.Any(p => p?.Party?.MobileParty?.StringId == _partyId)) { involved = true; break; }
                }
                DemonBattleBehavior.PendingBossMultiplier = involved ? WolfHuntMath.BeastHealthMultiplier(_stage) : 1f;
            }
            catch (System.Exception logEx)
            {
                TheDarkestNight.ModLog.Error(logEx);
                DemonBattleBehavior.PendingBossMultiplier = 1f;
            }
        }

        internal static void OnMapEventEnded()
        {
            try { DemonBattleBehavior.PendingBossMultiplier = 1f; }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
