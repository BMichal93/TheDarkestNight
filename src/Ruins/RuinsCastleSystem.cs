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
//   • its garrison party queued for destruction (DestroyPartyAction — batched
//     a few per day via ProcessPendingGarrisonDestroys, not fired synchronously
//     for every ruin at once: see that method's note on the 2026-07-19
//     new-game crashes that a synchronous version of this call caused);
//   • Prosperity/Security driven to 0 (both plain settable Town properties)
//     so it reads as abandoned in every vanilla UI that shows those numbers;
//   • our own menu (RuinsMenus) ADDS a ruin-crawl entry point alongside
//     whatever vanilla castle options the engine already shows for any
//     settlement RuinsCastleSystem marks as a ruin — it does NOT hide or
//     replace them (see that file's own note).
// A rival lord (or the player) can still besiege and "capture" a ruin through
// the completely untouched vanilla siege flow — deliberately not blocked,
// per the same behaviour.md rule cited above (no proven, verified way to
// intercept a siege in this codebase). Instead, ReapplyRuinNamesIfNeeded now
// runs every daily tick (not just once at session launch): if a ruin is ever
// captured and garrisoned, the very next day queues that new garrison for
// removal and zeroes prosperity/security again — so holding one is a
// permanently self-defeating, worthless act, not a true "no siege is
// possible" guarantee. This is an acceptable, low-risk simplification rather
// than a half-built "true ownerless/unsiegeable fief" feature that was never
// verified against the live game.
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

namespace TheDarkestNight
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
            _pendingGarrisonDestroys.Clear();
        }

        // Re-applies the ruin appearance (name/garrison-queue/prosperity) for
        // every settlement ConvertCastles selected this session. Originally a
        // one-shot backstop (v0.8.0 issue 6: the engine's own XML text reload
        // can revert the reflection-set name after OnSessionLaunchedEvent —
        // mirrors AshenCitySystem.Renaming.cs's own re-apply-from-daily-tick
        // pattern for the same reason). Now called EVERY daily tick, unconditionally,
        // because idempotent field writes are cheap and this is also the
        // mechanism that keeps a ruin a ruin: if the untouched vanilla siege
        // flow ever lets a rival lord capture and garrison one (Requirement 12
        // deliberately never blocks the siege itself — see the ownership note
        // at the top of this file), the very next daily tick queues the new
        // garrison for removal and zeroes prosperity/security again, so
        // holding a ruin never yields a working castle for more than a day.
        public static void ReapplyRuinNamesIfNeeded()
        {
            foreach (string id in _ruinIds.ToList())
            {
                try
                {
                    var s = Settlement.Find(id);
                    if (s != null) ApplyRuinAppearance(s);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Queries ──────────────────────────────────────────────────────────
        public static bool IsRuin(Settlement s) => s != null && _ruinIds.Contains(s.StringId);

        // Every settlement id currently converted to a ruin this session
        // (mod-author-directed addition — Phase 12's Temple questline needs
        // to deterministically pick five of these for guaranteed artifact
        // placement; see TempleQuestMath.SelectArtifactRuins). Defensive copy;
        // never mutate the returned list.
        public static List<string> AllRuinIds() => _ruinIds.ToList();
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
        // Garrison destruction is queued here (see ApplyRuinAppearance) rather
        // than run synchronously for every ruin at session launch: a new game
        // converts ~30-40 castles in one pass, and firing that many
        // DestroyPartyAction.Apply calls (each broadcasting
        // OnMobilePartyDestroyed/OnMapInteractableDestroyed to every listener)
        // back-to-back in the same synchronous burst as FactionScoping's own
        // kingdom-creation avalanche is the prime suspect for the 2026-07-19
        // ~22:35-22:53 new-game crashes (WER StackHash_f7a4) that began the
        // session this call was first made to actually fire (it silently
        // NRE'd — a no-op — before that fix). A small per-day batch keeps the
        // net effect (every ruin eventually loses its garrison) while never
        // asking the engine to tear down more than a handful of parties in a
        // single tick.
        private static readonly List<string> _pendingGarrisonDestroys = new List<string>();
        private const int GarrisonDestroysPerDay = 3;

        public static void DailyTick()
        {
            foreach (var key in _cooldownDays.Keys.ToList())
                if (_cooldownDays[key] > 0) _cooldownDays[key]--;

            try { ProcessPendingGarrisonDestroys(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ProcessPendingGarrisonDestroys()
        {
            if (_pendingGarrisonDestroys.Count == 0) return;
            int take = Math.Min(GarrisonDestroysPerDay, _pendingGarrisonDestroys.Count);
            var batch = _pendingGarrisonDestroys.GetRange(0, take);
            _pendingGarrisonDestroys.RemoveRange(0, take);

            foreach (string id in batch)
            {
                try
                {
                    var s = Settlement.Find(id);
                    var garrison = s?.Town?.GarrisonParty;
                    if (garrison != null) DestroyPartyAction.Apply(null, garrison);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
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
            try { ConvertCastles(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return true; } // fail exempt, never fail-convert
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
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try
            {
                // Queued (see ProcessPendingGarrisonDestroys), not called here —
                // calling DestroyPartyAction.Apply for every ruin synchronously in
                // this same session-launch pass is the prime suspect for the
                // 2026-07-19 new-game crashes; see DailyTick's comment.
                if (s.Town?.GarrisonParty != null && !_pendingGarrisonDestroys.Contains(s.StringId))
                    _pendingGarrisonDestroys.Add(s.StringId);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try
            {
                if (s.Town != null)
                {
                    s.Town.Prosperity = 0f;
                    s.Town.Security   = 0f;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try
            {
                // Ruins must never read as one of the eight core factions' territory
                // (per behaviour.md rule 3, still never OwnerClan = null — reassign to
                // a real kingdomless clan via the same ChangeOwnerOfSettlementAction
                // call FactionScoping.cs already uses). Runs every time ApplyRuinAppearance
                // does (session launch AND every daily ReapplyRuinNamesIfNeeded tick), so
                // if ReassignImperialSettlements (or any other system, or a rival lord's
                // siege) hands a ruin back to a kingdom, the very next daily tick strips
                // it again — the same self-healing pattern this file already uses for
                // garrison/prosperity. Never touches a player-held ruin.
                if (IsFactionOwned(s))
                {
                    Hero custodian = GetCustodianHero();
                    if (custodian != null && custodian.Clan != s.OwnerClan)
                        ChangeOwnerOfSettlementAction.ApplyByDefault(custodian, s);
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // True if the settlement is currently held by one of the eight core-faction
        // kingdoms (or any other real kingdom) rather than the player or a
        // kingdomless/city-state clan. Mirrors IsExempt's own city-state carve-out.
        private static bool IsFactionOwned(Settlement s)
        {
            if (s.OwnerClan == null || s.OwnerClan == Clan.PlayerClan) return false;
            if (s.OwnerClan.Kingdom == null) return false;
            string cityStateId = CityStateMath.CityStateKingdomId(s.OwnerClan.StringId);
            if (cityStateId != null && Kingdom.All.Any(k => k.StringId == cityStateId)) return false;
            return true;
        }

        // A single reusable kingdomless clan that "holds" every stripped ruin —
        // cached but re-validated, since the pool this is drawn from can shrink as
        // other clans join kingdoms or die out over a long campaign.
        private static Hero _custodianHero;

        private static Hero GetCustodianHero()
        {
            try
            {
                if (_custodianHero != null && _custodianHero.IsAlive
                    && _custodianHero.Clan != null && _custodianHero.Clan.Kingdom == null)
                    return _custodianHero;

                _custodianHero = Clan.All.FirstOrDefault(c =>
                        c != null && !c.IsEliminated && c != Clan.PlayerClan
                        && c.Kingdom == null && c.Leader != null && c.Leader.IsAlive
                        && !c.IsBanditFaction && !c.IsMinorFaction && !c.IsOutlaw)
                    ?.Leader;
                return _custodianHero;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }
    }
}
