// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsCastleSystem.cs
//
// Phase 9 (Requirement 12) — castle conversion and per-castle chamber
// sequencing. Runs on session launch, exactly like AshenRuinMenus.
// OnSessionLaunched — this is the "which settlements exist, what are they
// called, what state are they in right now" side; chamber-by-chamber
// exploration itself lives in RuinsExplorationSystem.
//
// ── Ownership decision (documented, per behaviour.md rule 3: "no proven
//    usage anywhere in src/ ⇒ choose a different, already-proven approach")
// Every settlement-ownership change anywhere in this codebase goes through
// ChangeOwnerOfSettlementAction.ApplyByDefault(hero, settlement) — always TO
// a real hero, never to null, and there is no existing example anywhere of
// setting Settlement/Town.OwnerClan to null directly. Doing so unassisted is
// untested engine territory (risking an auto-claim/quest AI reaction to a
// "no owner" fief that nothing in this codebase has ever exercised). Given
// the "simple working solution over the clever fragile one" rule, Ruins
// castles are converted WITHOUT touching OwnerClan: the settlement keeps its
// nominal owner in the background (irrelevant — see below) while every
// player-facing signal reads as an abandoned ruin:
//   • renamed "Ruined <Something> of <original name>" (RuinsMath.RuinNameFor),
//     using the exact reflection technique AshenCitySystem.Renaming.cs already
//     proved safe for settlement names;
//   • its garrison party destroyed (DestroyPartyAction, the same proven call
//     DemonSpawnCampaignBehavior already uses to despawn a party cleanly);
//   • Prosperity/Security driven to 0 (both plain settable Town properties)
//     so it reads as abandoned in every vanilla UI that shows those numbers;
//   • our own menu (RuinsMenus) replaces the normal town/castle interaction
//     with the ruin-crawl entry point for any settlement RuinsCastleSystem
//     marks as a ruin, so the player never sees a functioning fief screen
//     there regardless of who nominally owns it.
// A rival lord could in principle still besiege and "capture" a ruin through
// the untouched vanilla siege flow — but with zero garrison, zero prosperity,
// and no lord ever headquartered there, it holds no strategic value to fight
// over, so this is an acceptable, low-risk simplification rather than a
// half-built "true ownerless fief" feature that was never verified against
// the live game.
//
// ── Interaction with Phase 7 / Phase 8 (documented per the Phase 9 prompt)
// Phase 7's eight factions each keep only a short list of starting TOWNS
// (never castles); Phase 8 turns every other TOWN into a one-city city-state.
// Neither phase claims castles specifically, so nothing here can literally
// collide with either — but a castle currently held by a Phase 7 core-faction
// clan, or by a Phase 8 city-state's founding clan, is exempted anyway: a
// faction/city-state that has just been scoped down to a single town needs
// whatever castles it still holds for its own defence, and stripping one out
// from under it the moment the game starts would read as a bug, not a
// feature. The player's own holdings are exempted for the same reason
// CityStateSystem never auto-annexes them.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public static class RuinsCastleSystem
    {
        // Settlement ids RuinsMath selected AND that passed the exemption
        // check the last time OnSessionLaunched ran. Recomputed fresh each
        // session (cheap: a hash check + a handful of string comparisons per
        // castle) rather than persisted — exactly like AshenRuinMenus doesn't
        // persist its village resolution either.
        private static readonly HashSet<string> _ruinIds = new HashSet<string>();
        private static readonly Dictionary<string, int[]> _chamberSequence = new Dictionary<string, int[]>();

        // Exploration progress that DOES need to survive a save: which ruins
        // are fully cleared, and revisit cooldowns for both partial and full
        // clears. Parallel-list pattern, exactly like AshenRuinSystem /
        // DemonSpawnCampaignBehavior's own persisted state.
        private static readonly HashSet<string> _cleared = new HashSet<string>();
        private static readonly Dictionary<string, int> _cooldownDays = new Dictionary<string, int>();

        public static void ResetForNewGame()
        {
            _ruinIds.Clear();
            _chamberSequence.Clear();
            _cleared.Clear();
            _cooldownDays.Clear();
        }

        // ── Queries ──────────────────────────────────────────────────────────
        public static bool IsRuin(Settlement s) => s != null && _ruinIds.Contains(s.StringId);
        public static bool IsCleared(string settlementStringId) => _cleared.Contains(settlementStringId);
        public static bool IsOnCooldown(string settlementStringId) =>
            _cooldownDays.TryGetValue(settlementStringId, out int d) && d > 0;
        public static int CooldownDaysLeft(string settlementStringId) =>
            _cooldownDays.TryGetValue(settlementStringId, out int d) ? d : 0;

        public static int[] ChamberSequenceFor(Settlement s)
            => s != null && _chamberSequence.TryGetValue(s.StringId, out var seq) ? seq : Array.Empty<int>();

        public static void MarkCleared(string settlementStringId)
        {
            _cleared.Add(settlementStringId);
            _cooldownDays[settlementStringId] = RuinsMath.ClearedCooldownDays;
        }

        public static void SetRetreatCooldown(string settlementStringId)
        {
            if (!_cleared.Contains(settlementStringId))
                _cooldownDays[settlementStringId] = RuinsMath.RetreatCooldownDays;
        }

        // ── Daily tick ───────────────────────────────────────────────────────
        public static void DailyTick()
        {
            foreach (var key in _cooldownDays.Keys.ToList())
                if (_cooldownDays[key] > 0) _cooldownDays[key]--;
        }

        // ── Persistence ──────────────────────────────────────────────────────
        public static void SyncData(IDataStore dataStore)
        {
            var clearedList = _cleared.ToList();
            dataStore.SyncData("RUINS_ClearedIds", ref clearedList);

            var cdKeys = _cooldownDays.Keys.ToList();
            var cdVals = cdKeys.Select(k => _cooldownDays[k]).ToList();
            dataStore.SyncData("RUINS_CooldownKeys", ref cdKeys);
            dataStore.SyncData("RUINS_CooldownDays", ref cdVals);

            if (dataStore.IsLoading)
            {
                _cleared.Clear();
                if (clearedList != null) foreach (var id in clearedList) _cleared.Add(id);

                _cooldownDays.Clear();
                if (cdKeys != null && cdVals != null)
                    for (int i = 0; i < cdKeys.Count && i < cdVals.Count; i++)
                        _cooldownDays[cdKeys[i]] = cdVals[i];
            }
        }

        // ── Session launch: conversion pass ─────────────────────────────────
        private static readonly HashSet<string> _exemptTownIds = new HashSet<string>(
            WolfBrothersMath.StartingTownIds
                .Concat(TowerMath.StartingTownIds)
                .Concat(ForestWidowsMath.StartingTownIds)
                .Concat(BloodboundMath.StartingTownIds)
                .Concat(TempleMath.StartingTownIds)
                .Concat(EmpireMath.StartingTownIds)
                .Concat(LegionMath.StartingTownIds)
                .Concat(ChosenMath.StartingTownIds),
            StringComparer.OrdinalIgnoreCase);

        public static void OnSessionLaunched()
        {
            try { ConvertCastles(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ConvertCastles()
        {
            _ruinIds.Clear();
            _chamberSequence.Clear();
            if (Campaign.Current == null) return;

            foreach (Settlement s in Settlement.All)
            {
                try
                {
                    if (s == null || !s.IsCastle) continue;
                    if (!RuinsMath.ShouldBeRuin(s.StringId)) continue;
                    if (IsExempt(s)) continue;

                    _ruinIds.Add(s.StringId);

                    int throneIndex = RuinsCatalog.All
                        .Select((c, i) => (c, i))
                        .First(t => t.c.Id == ChamberType.ThroneOfDust).i;
                    _chamberSequence[s.StringId] =
                        RuinsMath.ChamberSequence(s.StringId, RuinsCatalog.PoolSize, throneIndex);

                    ApplyRuinAppearance(s);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsExempt(Settlement s)
        {
            try
            {
                if (s.OwnerClan == Clan.PlayerClan) return true;
                if (_exemptTownIds.Contains(s.StringId)) return true;
                if (AshenCitySystem.IsTargetSettlementId(s.StringId)) return true;
                if (s.OwnerClan != null)
                {
                    string cityStateId = CityStateMath.CityStateKingdomId(s.OwnerClan.StringId);
                    if (cityStateId != null && Kingdom.All.Any(k => k.StringId == cityStateId))
                        return true;
                }
                return false;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return true; } // fail exempt, never fail-convert
        }

        // Reflection handle mirrors AshenCitySystem.Renaming.cs exactly:
        // Settlement shadows MBObjectBase._name with its own field, and only
        // that shadowing field is actually read by Settlement.Name.
        private static readonly System.Reflection.FieldInfo _settlementNameField =
            typeof(Settlement).GetField("_name",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static void ApplyRuinAppearance(Settlement s)
        {
            try
            {
                string original = s.Name?.ToString() ?? s.StringId;
                string ruinName = RuinsMath.RuinNameFor(s.StringId, original);
                _settlementNameField?.SetValue(s, new TextObject(ruinName));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                var garrison = s.Town?.GarrisonParty;
                if (garrison != null) DestroyPartyAction.Apply(garrison.Party, null);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                if (s.Town != null)
                {
                    s.Town.Prosperity = 0f;
                    s.Town.Security   = 0f;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
