// =============================================================================
// THE DARKEST NIGHT — Factions/ForestWidows/ForestWidowsCampaignBehavior.cs
//
// Faction C — the Forest Widows (Battania). Owns everything Phase 7 adds for
// this faction: town-scoping to Marunath/Car Banseth (via
// ForestWidowsSettlements), the female-only leadership rule, the daily
// influence drain on a male player, the "demons ignore you" timed buff
// bought at the sacrifice menu (see ForestWidowsCampaignBehavior.Menus.cs),
// and the timed demon-troop grant from a lordly sacrifice. Mechanically
// identical to PaleWidowsCampaignBehavior.cs (Southern Empire) — see that
// file for the fuller design rationale — retargeted to Battania.
//
// ── Female-only leadership (two mechanisms, per the brief) ────────────────
//   1. CLEAN HOOK: CampaignEvents.OnClanLeaderChangedEvent (Action<Hero
//      oldLeader, Hero newLeader>) fires the instant vanilla installs a new
//      clan leader — including its own automatic succession on a leader's
//      death. If the freshly-installed leader of a Forest Widows clan is
//      male, he is immediately replaced (see EnforceFemaleLeadership).
//   2. FALLBACK SWEEP: a daily tick re-checks every Forest Widows clan
//      anyway, in case some path installs a leader without firing that
//      event (mod conflicts, edge-case engine flows) — belt and braces.
// Replacement preference: the deposed leader's own wife if she is alive,
// adult, and in the same clan; otherwise the oldest living adult female
// clan member. If neither exists the clan is left alone.
//
// The PLAYER'S OWN clan is deliberately exempt from the forced swap — see
// PaleWidowsCampaignBehavior's identical exemption. Instead, a male player
// pays the kingdom's price a different way: total daily influence drain
// (see TickInfluenceDrain / ForestWidowsMath.InfluenceAfterDailyDrain).
//
// ── "Demons ignore you" reuse decision ──────────────────────────────────
// Same decision as PaleWidowsCampaignBehavior: BloodboundCampaignBehavior.
// IsPartyIgnored/GrantIgnore is a pair of private-state statics keyed to
// Bloodbound's own persisted lists, not a generic API, and
// DemonSpawnCampaignBehavior.DirectDemonParties calls each faction's tracker
// by name. This file owns its OWN parallel tracker with the identical shape
// (hero-keyed parallel lists, same query contract: IsPartyIgnored
// (MobileParty)), and DemonSpawnCampaignBehavior.DirectDemonParties is
// extended with a THIRD OR-clause alongside the Bloodbound and Pale Widows
// checks — the same choke point, now consulting three factions' trackers.
// See that file's DirectDemonParties.
//
// Persistence: ignore windows (FW_IGNORE_*), demon-troop grants
// (FW_DEMONGRANT_*), parallel lists exactly like PaleWidowsCampaignBehavior's
// ignore/grant trackers.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public partial class ForestWidowsCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state (parallel lists, serialised per save) ────────────
        private static List<string> _ignoreHeroIds   = new List<string>();
        private static List<float>  _ignoreExpiryDay = new List<float>();

        private static List<string> _grantHeroIds    = new List<string>();
        private static List<string> _grantTroopIds   = new List<string>();
        private static List<int>    _grantCounts      = new List<int>();
        private static List<float>  _grantVanishDay   = new List<float>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("FW_IGNORE_IDS",        ref _ignoreHeroIds);   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FW_IGNORE_EXPIRY_DAY",  ref _ignoreExpiryDay); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FW_GRANT_HERO_IDS",     ref _grantHeroIds);    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FW_GRANT_TROOP_IDS",    ref _grantTroopIds);   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FW_GRANT_COUNTS",       ref _grantCounts);     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FW_GRANT_VANISH_DAY",   ref _grantVanishDay);  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (_ignoreHeroIds == null) _ignoreHeroIds = new List<string>();
            if (_ignoreExpiryDay == null) _ignoreExpiryDay = new List<float>();
            if (_grantHeroIds == null) _grantHeroIds = new List<string>();
            if (_grantTroopIds == null) _grantTroopIds = new List<string>();
            if (_grantCounts == null) _grantCounts = new List<int>();
            if (_grantVanishDay == null) _grantVanishDay = new List<float>();
        }

        public static void ResetForNewGame()
        {
            _ignoreHeroIds = new List<string>();
            _ignoreExpiryDay = new List<float>();
            _grantHeroIds = new List<string>();
            _grantTroopIds = new List<string>();
            _grantCounts = new List<int>();
            _grantVanishDay = new List<float>();
        }

        // The kingdom-name rename itself runs from the same hook every other
        // culture identity uses — AshenCitySystem.EnsureKingdomRenames (fired
        // off OnSessionLaunchedEvent), which calls
        // ForestWidowsCulture.RenameForestWidowsKingdom(). This behavior only
        // owns what is unique to the Forest Widows.
        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterForestWidowsMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Enforce at session start too, not only on the next daily tick —
            // a fresh load of a save with a male Battanian lord should not
            // get a free day of illegitimate rule.
            try { EnforceFemaleLeadership(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { ForestWidowsSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { EnforceFemaleLeadership(); }                      catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickInfluenceDrain(); }                           catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { PruneExpiredIgnores(); }                          catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickDemonGrantExpiry(); }                         catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Kingdom GetForestWidowsKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k.StringId == ForestWidowsCulture.CultureId && !k.IsEliminated); }
            catch { return null; }
        }

        // ── Female-only leadership ────────────────────────────────────────────
        private static void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
        {
            try
            {
                if (newLeader == null) return;
                Clan clan = newLeader.Clan;
                if (clan == null || clan == Clan.PlayerClan) return;
                if (clan.MapFaction?.StringId != ForestWidowsCulture.CultureId) return;
                if (!newLeader.IsFemale) TryReplaceWithFemaleLeader(clan, newLeader);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void EnforceFemaleLeadership()
        {
            var kingdom = GetForestWidowsKingdom();
            if (kingdom == null) return;

            foreach (var clan in kingdom.Clans.ToList())
            {
                try
                {
                    if (clan == null || clan.IsEliminated || clan == Clan.PlayerClan) continue;
                    Hero leader = clan.Leader;
                    if (leader == null || leader.IsFemale) continue;
                    TryReplaceWithFemaleLeader(clan, leader);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void TryReplaceWithFemaleLeader(Clan clan, Hero deposedMale)
        {
            try
            {
                // Prefer the deposed man's own wife, if she is alive, an adult,
                // and a member of the same clan.
                Hero heir = deposedMale.Spouse;
                if (heir == null || !heir.IsAlive || heir.IsChild || !heir.IsFemale || heir.Clan != clan)
                    heir = null;

                if (heir == null)
                    heir = clan.Heroes?
                        .Where(h => h != null && h.IsAlive && !h.IsChild && h.IsFemale && h != deposedMale)
                        .OrderByDescending(h => h.Age)
                        .FirstOrDefault();

                if (heir == null) return; // no woman left to hold the seat — leave the clan alone

                try { ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, heir); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return; }

                InformationManager.DisplayMessage(new InformationMessage(
                    $"{heir.Name} sets aside {deposedMale.Name} and takes {clan.Name}'s seat — a husband does not rule among the Forest Widows.",
                    new Color(0.20f, 0.45f, 0.25f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Daily influence drain for a male player ─────────────────────────────
        private static void TickInfluenceDrain()
        {
            try
            {
                Hero player = Hero.MainHero;
                Clan clan = Clan.PlayerClan;
                if (player == null || clan == null) return;
                bool isMember = ForestWidowsCulture.IsPlayerForestWidow;
                float updated = ForestWidowsMath.InfluenceAfterDailyDrain(!player.IsFemale, isMember, clan.Influence);
                if (updated != clan.Influence) clan.Influence = updated;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── "The dark's blindness" ignore buff ──────────────────────────────────
        // Stacks: a fresh grant extends whatever window is currently active
        // rather than overwriting it (see ForestWidowsMath.ExtendIgnoreExpiry).
        internal static void GrantIgnoreDays(Hero hero, float days)
        {
            if (hero == null || days <= 0f) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _ignoreHeroIds.IndexOf(hero.StringId);
                if (idx >= 0)
                {
                    _ignoreExpiryDay[idx] = ForestWidowsMath.ExtendIgnoreExpiry(today, _ignoreExpiryDay[idx], days);
                    return;
                }
                _ignoreHeroIds.Add(hero.StringId);
                _ignoreExpiryDay.Add(ForestWidowsMath.ExtendIgnoreExpiry(today, today, days));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Consulted by DemonSpawnCampaignBehavior.DirectDemonParties to skip a
        // currently-ignored party when picking prey — the same query contract
        // as BloodboundCampaignBehavior.IsPartyIgnored / PaleWidowsCampaign-
        // Behavior.IsPartyIgnored, deliberately mirrored rather than shared
        // (see header note).
        internal static bool IsPartyIgnored(MobileParty party)
        {
            if (party == null) return false;
            try
            {
                Hero owner = party.LeaderHero ?? (party.IsMainParty ? Hero.MainHero : null);
                if (owner == null) return false;

                int idx = _ignoreHeroIds.IndexOf(owner.StringId);
                if (idx < 0) return false;

                float today = (float)CampaignTime.Now.ToDays;
                return ForestWidowsMath.IsIgnoreActive(today, _ignoreExpiryDay[idx]);
            }
            catch { return false; }
        }

        private void PruneExpiredIgnores()
        {
            if (_ignoreHeroIds.Count == 0) return;
            float today = (float)CampaignTime.Now.ToDays;
            for (int i = _ignoreHeroIds.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (ForestWidowsMath.IsIgnoreActive(today, _ignoreExpiryDay[i])) continue;
                    _ignoreHeroIds.RemoveAt(i);
                    _ignoreExpiryDay.RemoveAt(i);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // ── Demon-troop grant from a lordly sacrifice — vanishes after the roll ──
        internal static void GrantDemonTroops(Hero owner, string troopId, int count, float vanishDays)
        {
            if (owner == null || string.IsNullOrEmpty(troopId) || count <= 0) return;
            try
            {
                var party = owner == Hero.MainHero ? MobileParty.MainParty : owner.PartyBelongedTo;
                var character = MBObjectManager.Instance?.GetObject<CharacterObject>(troopId);
                if (party?.MemberRoster == null || character == null) return;

                party.MemberRoster.AddToCounts(character, count);

                float today = (float)CampaignTime.Now.ToDays;
                _grantHeroIds.Add(owner.StringId);
                _grantTroopIds.Add(troopId);
                _grantCounts.Add(count);
                _grantVanishDay.Add(today + vanishDays);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void TickDemonGrantExpiry()
        {
            if (_grantHeroIds.Count == 0) return;
            float today = (float)CampaignTime.Now.ToDays;
            for (int i = _grantHeroIds.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (today < _grantVanishDay[i]) continue;

                    Hero owner = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _grantHeroIds[i])
                              ?? (Hero.MainHero != null && Hero.MainHero.StringId == _grantHeroIds[i] ? Hero.MainHero : null);
                    var party = owner == Hero.MainHero ? MobileParty.MainParty : owner?.PartyBelongedTo;
                    var character = MBObjectManager.Instance?.GetObject<CharacterObject>(_grantTroopIds[i]);
                    if (party?.MemberRoster != null && character != null)
                    {
                        int have = party.MemberRoster.GetTroopRoster()
                            .Where(e => e.Character == character).Sum(e => e.Number);
                        int take = Math.Min(have, _grantCounts[i]);
                        if (take > 0) party.MemberRoster.AddToCounts(character, -take);

                        if (party.IsMainParty)
                            InformationManager.DisplayMessage(new InformationMessage(
                                "The demons bought with lordly blood slip back into the dark — their debt is paid.",
                                new Color(0.20f, 0.45f, 0.25f)));
                    }

                    _grantHeroIds.RemoveAt(i);
                    _grantTroopIds.RemoveAt(i);
                    _grantCounts.RemoveAt(i);
                    _grantVanishDay.RemoveAt(i);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }
    }
}
