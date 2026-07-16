// =============================================================================
// THE DARKEST NIGHT — Apocalypse/ApocalypseCampaignBehavior.Gathering.cs
//
// Requirement 33, stage 2 — "they are gathering." From day 600, a single
// demon band spawns in one random corner of the map and, unlike the ordinary
// night tide (DemonSpawnCampaignBehavior — spawns at dusk, destroyed at
// dawn), this one PERSISTS across day and night and GROWS every week toward
// a cap. Persistence follows the same DEMON_*/ELEM_* "party StringId in a
// synced field" convention DemonSpawnCampaignBehavior and ElementalWildsBehavior
// already use. Partial of ApocalypseCampaignBehavior.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public partial class ApocalypseCampaignBehavior
    {
        private static string _gatheringPartyId  = null;
        private static int    _gatheringSpawnDay = -1;
        private static bool   _gatheringPortentShown = false;

        private static void SyncGatheringData(IDataStore store)
        {
            store.SyncData("APOC_GatherPartyId",  ref _gatheringPartyId);
            store.SyncData("APOC_GatherSpawnDay", ref _gatheringSpawnDay);
            store.SyncData("APOC_GatherPortent",  ref _gatheringPortentShown);
        }

        private static void ResetGatheringForNewGame()
        {
            _gatheringPartyId = null;
            _gatheringSpawnDay = -1;
            _gatheringPortentShown = false;
        }

        private static MobileParty CurrentGatheringParty()
        {
            if (string.IsNullOrEmpty(_gatheringPartyId)) return null;
            try { return MobileParty.All.FirstOrDefault(p => p != null && p.StringId == _gatheringPartyId); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        private void GatheringWeeklyTick()
        {
            int day = CurrentDay();
            if (!ApocalypseMath.IsGatheringStage(day)) return;

            if (string.IsNullOrEmpty(_gatheringPartyId))
            {
                // Only spawn once — DemonLordSystem absorbs this party the moment
                // he appears, at which point _gatheringPartyId is cleared again
                // (AbsorbIntoLord below) so a fresh gathering never re-spawns
                // after that (his own host has taken over the role).
                if (_gatheringSpawnDay >= 0) return; // already absorbed once — never again
                try { SpawnGatheringParty(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                return;
            }

            MobileParty party = CurrentGatheringParty();
            if (party == null || !party.IsActive)
            {
                // Died to the player or another faction before the Demon Lord ever
                // rose — simply forget it; it does not respawn.
                _gatheringPartyId = null;
                return;
            }

            int weeks = Math.Max(0, (day - _gatheringSpawnDay) / 7);
            int wanted = ApocalypseMath.GatheringSizeAfterWeeks(weeks);
            int have = party.MemberRoster?.TotalManCount ?? 0;
            if (have < wanted) AddGatheringBodies(party, wanted - have);
        }

        private static void SpawnGatheringParty()
        {
            Vec2 anchor = PickMapCorner();

            Clan banditClan = Clan.BanditFactions.FirstOrDefault(c => c != null && !c.IsEliminated);
            if (banditClan == null) return;
            var pt = banditClan.DefaultPartyTemplate;
            if (pt == null) return;

            Hideout hideout = Settlement.All.Where(s => s?.Hideout != null)
                .OrderBy(s => (s.GetPosition2D - anchor).LengthSquared)
                .Select(s => s.Hideout)
                .FirstOrDefault();
            if (hideout == null) return;

            var cvec = new CampaignVec2(anchor, true);
            string partyId = "apoc_gathering_" + new Random().Next(999999).ToString("D6");
            MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, banditClan, hideout, false, pt, cvec);
            if (party == null) return;

            try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            AddGatheringBodies(party, ApocalypseMath.GatheringInitialSize);
            try { party.Party.SetCustomName(new TextObject("The Gathering")); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            _gatheringPartyId = party.StringId;
            _gatheringSpawnDay = CurrentDay();

            if (!_gatheringPortentShown)
            {
                _gatheringPortentShown = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    "They are gathering. Somewhere at the edge of the map, the demons have stopped raiding and " +
                    "started massing — and every night, there are more of them.",
                    new Color(0.60f, 0.05f, 0.05f)));
            }
        }

        // One of the four extreme corners of the settled map (by settlement
        // bounding box), so "one random corner of the map" reads as a real
        // location rather than a uniformly random point that could land in the
        // middle of the continent.
        private static Vec2 PickMapCorner()
        {
            try
            {
                var positions = Settlement.All.Where(s => s != null).Select(s => s.GetPosition2D).ToList();
                if (positions.Count == 0) return default;
                float minX = positions.Min(p => p.x), maxX = positions.Max(p => p.x);
                float minY = positions.Min(p => p.y), maxY = positions.Max(p => p.y);
                var rng = new Random();
                float x = rng.Next(2) == 0 ? minX : maxX;
                float y = rng.Next(2) == 0 ? minY : maxY;
                return new Vec2(x, y);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return default; }
        }

        private static void AddGatheringBodies(MobileParty party, int count)
        {
            var rng = new Random();
            for (int i = 0; i < count; i++)
            {
                DemonMath.DemonTier tier = DemonMath.RollTier(rng);
                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId);
                if (troop == null) continue;
                try { party.MemberRoster.AddToCounts(troop, 1); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // Called once, from TryRollDemonLordAppearance, the moment he rises —
        // "binds all demons into HIS faction" folds the persistent Gathering
        // band's numbers straight into his own host instead of leaving two
        // separate demon armies wandering the map.
        private static void AbsorbGatheringIntoLord(MobileParty lordParty)
        {
            MobileParty gathering = CurrentGatheringParty();
            if (gathering == null || lordParty == null) { _gatheringPartyId = null; return; }
            try
            {
                foreach (var entry in gathering.MemberRoster.GetTroopRoster().ToList())
                {
                    if (entry.Character == null || entry.Number <= 0) continue;
                    lordParty.MemberRoster.AddToCounts(entry.Character, entry.Number);
                }
                TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(gathering.Party, null);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            _gatheringPartyId = null;
        }
    }
}
