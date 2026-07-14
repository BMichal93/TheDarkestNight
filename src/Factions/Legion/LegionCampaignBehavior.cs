// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionCampaignBehavior.cs
//
// Faction G — Legion (Western Empire). Owns everything Phase 7 adds for this
// faction: town-scoping to Lageta/Ortysia (via LegionSettlements), the
// aggression weighting that makes the Legion markedly more raid-happy and
// war-hungry than every other kingdom, and the training-fields town menu (see
// LegionCampaignBehavior.Menus.cs). Like the Northern Empire, the Western
// Empire had no prior baseline identity to extend — LegionCulture.cs is a
// from-scratch rename, not a supersession.
//
// ── Aggression mechanism (see LegionMath.RaidNudgeChance / PeaceToleranceDays) ──
// Bannerlord exposes no clean GameModel for raid/attack DECISION weighting —
// TaleWorlds.CampaignSystem.ComponentInterfaces.RaidModel only covers loot and
// hit-damage math once a raid is already under way (confirmed by reflecting
// RaidModel/DefaultRaidModel against the shipped DLL: CalculateHitDamage,
// GetRaidLootMultiplier, GetCommonLootItemScores, GoldRewardForEachLostHearth
// — nothing about deciding to raid). So, like DemonSpawnCampaignBehavior's
// nightly settlement assaults, the Legion is nudged directly:
//   - Daily, every idle Legion lord party (not mid-battle, not besieging or
//     already raiding, not in an army) rolls LegionMath.RaidNudgeChance to be
//     pointed at the nearest hostile settlement via SetMoveRaidSettlement —
//     a large, deliberate multiplier over vanilla's own rare, opportunistic
//     raid choices.
//   - If the Legion goes LegionMath.PeaceToleranceDays fully at peace with
//     every living kingdom, it is forced to declare war on its nearest
//     neighbour — a militarist realm does not sit idle.
// Both apply to Legion LORDS, not just the player — the whole kingdom reads
// as aggressive, matching the "campaign-AI-wide" requirement.
//
// Persistence: the peace-day streak is a single int (LEG_PEACE_STREAK); the
// training-fields cost/yield needs no persisted state at all.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public partial class LegionCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state ────────────────────────────────────────────────────
        private static int _peaceDayStreak = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("LEG_PEACE_STREAK", ref _peaceDayStreak); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _peaceDayStreak = 0;
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterLegionMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { LegionSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickRaidNudges();                        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickWarEagerness();                       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Kingdom GetLegionKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k.StringId == LegionCulture.KingdomId && !k.IsEliminated); }
            catch { return null; }
        }

        // ── Aggression: raid nudge ───────────────────────────────────────────────
        private static void TickRaidNudges()
        {
            var legion = GetLegionKingdom();
            if (legion == null) return;
            if (!IsAtWarWithAnyone(legion)) return; // nothing hostile to raid

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || !party.IsLordParty) continue;
                    if (party == MobileParty.MainParty) continue; // never override the player's own orders
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    if (leaderClan.Kingdom != legion) continue;
                    if (party.Army != null) continue;             // leave coordinated army moves alone
                    if (party.MapEvent != null) continue;         // already fighting
                    if (party.BesiegedSettlement != null) continue; // already assaulting
                    if (party.ShortTermBehavior == AiBehavior.RaidSettlement
                     || party.ShortTermBehavior == AiBehavior.BesiegeSettlement
                     || party.ShortTermBehavior == AiBehavior.AssaultSettlement) continue; // already on the warpath

                    if (!LegionMath.ShouldNudgeToRaid(_rng.NextDouble())) continue;

                    Settlement target = FindRaidTarget(party);
                    if (target == null) continue;

                    try { party.SetMoveRaidSettlement(target, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static Settlement FindRaidTarget(MobileParty party)
        {
            try
            {
                // Prefer an undefended village — the classic raid target.
                Settlement village = Settlement.All
                    .Where(s => s != null && s.IsVillage && s.Village != null
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
                if (village != null) return village;

                // No hostile village in reach — fall back to any hostile settlement.
                return Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle)
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        private static bool IsAtWarWithAnyone(Kingdom legion)
        {
            try { return Kingdom.All.Any(k => k != null && !k.IsEliminated && k != legion && legion.IsAtWarWith(k)); }
            catch { return false; }
        }

        // ── Aggression: forced war declaration after too long at peace ─────────
        private static void TickWarEagerness()
        {
            var legion = GetLegionKingdom();
            if (legion == null) { _peaceDayStreak = 0; return; }

            if (IsAtWarWithAnyone(legion)) { _peaceDayStreak = 0; return; }

            _peaceDayStreak++;
            if (!LegionMath.ShouldForceWarDeclaration(_peaceDayStreak)) return;

            var target = FindNearestKingdom(legion);
            if (target == null) return;

            try { DeclareWarAction.ApplyByDefault(legion, target); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            _peaceDayStreak = 0;

            if (LegionCulture.IsPlayerLegion)
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Legion has sat idle too long. The column marches on {target.Name} — might makes right.",
                    new Color(0.6f, 0.15f, 0.1f)));
        }

        private static Kingdom FindNearestKingdom(Kingdom legion)
        {
            try
            {
                var legionTowns = Settlement.All.Where(s => s != null && s.IsTown && s.MapFaction == legion).ToList();
                if (legionTowns.Count == 0) return null;

                Kingdom best = null;
                float bestDistSq = float.MaxValue;
                foreach (var k in Kingdom.All)
                {
                    if (k == null || k.IsEliminated || k == legion) continue;
                    var towns = Settlement.All.Where(s => s != null && s.IsTown && s.MapFaction == k).ToList();
                    if (towns.Count == 0) continue;

                    float dist = legionTowns.Min(lt => towns.Min(t => (lt.GetPosition2D - t.GetPosition2D).LengthSquared));
                    if (dist < bestDistSq) { bestDistSq = dist; best = k; }
                }
                return best;
            }
            catch { return null; }
        }
    }
}
