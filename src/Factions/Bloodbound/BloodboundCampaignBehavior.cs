// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundCampaignBehavior.cs
//
// Faction D — the Bloodbound (formerly Khuzait). Owns:
//   • session renaming (kingdom/culture text, via BloodboundCulture)
//   • town-scoping to Akkalat + Chaikand (via BloodboundSettlements), daily
//   • the join gate — a candidate too soft or too untrained to survive the
//     hunt is turned away the instant the kingdom-join fires (see
//     OnClanChangedKingdom below)
//   • the "demons ignore you" and "hardened flesh" timed buffs bought at the
//     spending menu (see BloodboundCampaignBehavior.Menus.cs), including the
//     query DemonSpawnCampaignBehavior.DirectDemonParties uses to skip a
//     currently-ignored party when picking prey
//   • the permanent attribute trade bought at the same menu
//
// Demon Blood is EARNED from DemonSpawnCampaignBehavior.OnMapEventEnded (see
// that file's RollDemonBloodDrop, which mirrors RollRelicDrop but checks the
// WINNING side for Bloodbound affiliation instead of always the player, so a
// Bloodbound lord's own victories pay out too) — this file only owns the
// SPENDING side. Per the brief, a full lord-side spending AI loop is out of
// scope for this pass; lords simply accumulate Demon Blood on their own
// party's item roster the same way the player does.
//
// Persistence: the ignore-buff and HP-buff windows are tracked as parallel
// lists (BLD_IGNORE_IDS/BLD_IGNORE_DAY, BLD_HPBUFF_IDS/BLD_HPBUFF_DAY),
// exactly the pattern HiveCampaignBehavior's dosing tracker already uses for
// this codebase's "small persisted roster" state.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public partial class BloodboundCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // The combat skills the join gate checks for any invested focus point —
        // mirrors the exact set AmbientRemarks.cs already treats as "weapon
        // skills" for this codebase. Built on demand rather than in a static
        // field initializer: the DefaultSkills.* accessors are null until the
        // engine has populated them, and this type's .cctor runs during
        // OnGameStart (via ResetForNewGame) — reading them there threw a
        // TypeInitializationException that poisoned the whole behavior for the
        // session. AmbientRemarks.cs reads the same set inside a method for the
        // same reason.
        private static SkillObject[] CombatSkills() => new[]
        {
            DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
            DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Throwing,
        };

        // ── Persistent state (parallel lists, serialised per save) ────────────
        private static List<string> _ignoreHeroIds = new List<string>();
        private static List<float>  _ignoreGrantDay = new List<float>();
        private static List<float>  _ignoreDurationDays = new List<float>();

        private static List<string> _hpBuffHeroIds = new List<string>();
        private static List<float>  _hpBuffGrantDay = new List<float>();

        // Blood-attunement (mod-author-directed addition — see
        // BloodAttunement.cs) persists its own four parallel lists, synced
        // below alongside this behavior's other Demon Blood spending trackers.

        // Clans queued by OnClanChangedKingdom for the join-gate bounce, processed
        // on the next hourly tick instead of synchronously — see the note on
        // OnClanChangedKingdom below. Not persisted (harmless if a reload drops one
        // in-flight bounce).
        private static readonly List<Clan> _pendingUnworthyEjections = new List<Clan>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("BLD_IGNORE_IDS",       ref _ignoreHeroIds);       } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_IGNORE_GRANT_DAY",  ref _ignoreGrantDay);      } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_IGNORE_DURATION",   ref _ignoreDurationDays);  } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_HPBUFF_IDS",        ref _hpBuffHeroIds);       } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_HPBUFF_GRANT_DAY",  ref _hpBuffGrantDay);      } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_ATT_HERO_IDS",      ref BloodAttunement.HeroIds);            } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_ATT_ELEMENT_MASKS", ref BloodAttunement.ElementMasks);        } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_ATT_MORALE_STACKS", ref BloodAttunement.DaytimeMoraleStacks);  } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("BLD_ATT_SPEED_STACKS",  ref BloodAttunement.DaytimeSpeedStacks);   } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (_ignoreHeroIds == null) _ignoreHeroIds = new List<string>();
            if (_ignoreGrantDay == null) _ignoreGrantDay = new List<float>();
            if (_ignoreDurationDays == null) _ignoreDurationDays = new List<float>();
            if (_hpBuffHeroIds == null) _hpBuffHeroIds = new List<string>();
            if (_hpBuffGrantDay == null) _hpBuffGrantDay = new List<float>();
            BloodAttunement.EnsureListsSane();
        }

        public static void ResetForNewGame()
        {
            _ignoreHeroIds = new List<string>();
            _ignoreGrantDay = new List<float>();
            _ignoreDurationDays = new List<float>();
            _hpBuffHeroIds = new List<string>();
            _hpBuffGrantDay = new List<float>();
            BloodAttunement.ResetForNewGame();
            BloodAttunementLordAI.ResetForNewGame();
            _pendingUnworthyEjections.Clear();
        }

        // The kingdom-name rename itself runs from the SAME hook every other
        // culture identity uses — AshenCitySystem.EnsureKingdomRenames (fired off
        // OnSessionLaunchedEvent), which now calls BloodboundCulture.RenameBloodboundKingdom()
        // in place of the retired RenameTribesKingdom(). This behavior only owns
        // what is unique to the Bloodbound: the town-scoping, the join gate, the
        // spending menus, and the timed-buff ticks.
        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterBloodboundMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { BloodboundSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickHpBuffExpiry(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { PruneExpiredIgnores(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { BloodAttunement.TickDaytimeMoralePenalty(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnHourlyTick()
        {
            try { ProcessPendingUnworthyEjections(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Runs from the safe top-level hourly-tick context — never from inside
        // OnClanChangedKingdomEvent's own dispatch (see that handler's note).
        private void ProcessPendingUnworthyEjections()
        {
            if (_pendingUnworthyEjections.Count == 0) return;
            var batch = _pendingUnworthyEjections.ToList();
            _pendingUnworthyEjections.Clear();

            foreach (Clan clan in batch)
            {
                try
                {
                    if (clan == null || clan.IsEliminated) continue;
                    if (clan.Kingdom == null || clan.Kingdom.StringId != BloodboundCulture.CultureId) continue;
                    ChangeKingdomAction.ApplyByLeaveKingdom(clan, clan == Clan.PlayerClan);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── The join gate — refuses the soft and the untested ───────────────────
        // Fires for every clan that genuinely joins the Bloodbound (was not
        // already Khuzait/Bloodbound before). Covers a defecting lord exactly as
        // it covers the player swearing in through the vanilla diplomacy flow —
        // both call the same ChangeKingdomAction.ApplyByJoinToKingdom internally,
        // triggering this event. A clan whose leader does not qualify is turned
        // right back out the same way HiveSettlements evicts a landless clan:
        // ChangeKingdomAction.ApplyByLeaveKingdom — queued for the next hourly
        // tick, NOT called synchronously here. Calling a kingdom-changing action
        // from inside this event's own dispatch re-enters the native campaign/
        // diplomacy machinery mid-call — the same re-entrancy hazard
        // CityStateSystem.OnClanChangedKingdom and AshenCitySystem's handler are
        // deliberately left empty to avoid, and the confirmed root cause of the
        // 2026-07-19 new-game map crashes (see MortalLawCampaignBehavior's header).
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || newKingdom == null) return;
                if (newKingdom.StringId != BloodboundCulture.CultureId) return;
                if (oldKingdom != null && oldKingdom.StringId == BloodboundCulture.CultureId) return; // already Bloodbound

                Hero leader = clan.Leader;
                if (leader == null || Qualifies(leader)) return; // no leader to judge, or judged worthy

                bool isPlayerClan = clan == Clan.PlayerClan;
                if (!_pendingUnworthyEjections.Contains(clan)) _pendingUnworthyEjections.Add(clan);

                if (isPlayerClan)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "The Bloodbound look you over and find you wanting — too soft, too untested. "
                      + "\"Come back when the killing has left its mark on you.\"",
                        new Color(0.55f, 0.10f, 0.10f)));
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static bool Qualifies(Hero hero)
        {
            if (hero == null) return true; // never hard-block on a null hero
            try
            {
                int vigor = hero.GetAttributeValue(DefaultCharacterAttributes.Vigor);
                int endurance = hero.GetAttributeValue(DefaultCharacterAttributes.Endurance);
                int combatFocus = 0;
                if (hero.HeroDeveloper != null)
                    foreach (var skill in CombatSkills())
                        try { combatFocus += hero.HeroDeveloper.GetFocus(skill); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                return BloodboundMath.QualifiesForBloodbound(vigor, endurance, combatFocus);
            }
            catch { return true; } // fail open — never hard-block on an unexpected engine error
        }

        // ── Demon Blood earning hook (called from DemonSpawnCampaignBehavior) ───
        // Grants Demon Blood to a winning party's item roster if that party
        // belongs to a Bloodbound vassal (player or lord). Kept here so the
        // yield math/party-affiliation check stays with the faction that owns
        // it; DemonSpawnCampaignBehavior only supplies "who won a fight against
        // demons," mirroring how it already supplies that same fact to
        // RelicMath's roll.
        internal static void GrantDemonBlood(PartyBase winner)
        {
            if (winner == null) return;
            try
            {
                if (!BloodboundCulture.IsBloodboundParty(winner)) return;

                int amount = BloodboundMath.RollDemonBloodYield(_rng.NextDouble());
                var item = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<ItemObject>(BloodboundCatalog.DemonBloodItemId);
                var roster = winner.ItemRoster;
                if (item == null || roster == null) return;

                roster.AddToCounts(item, amount);

                if (winner.MobileParty == MobileParty.MainParty)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The hunt pays out: {amount} vial(s) of Demon Blood taken from the kill.",
                        new Color(0.55f, 0.10f, 0.10f)));
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Ignore buff ──────────────────────────────────────────────────────────
        internal static void GrantIgnore(Hero hero, float durationDays)
        {
            if (hero == null) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _ignoreHeroIds.IndexOf(hero.StringId);
                if (idx >= 0)
                {
                    _ignoreGrantDay[idx] = today;
                    _ignoreDurationDays[idx] = durationDays;
                    return;
                }
                _ignoreHeroIds.Add(hero.StringId);
                _ignoreGrantDay.Add(today);
                _ignoreDurationDays.Add(durationDays);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Consulted by DemonSpawnCampaignBehavior.DirectDemonParties to skip a
        // currently-ignored party when picking prey.
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
                float daysSince = today - _ignoreGrantDay[idx];
                return BloodboundMath.IsIgnoreActive(daysSince, _ignoreDurationDays[idx]);
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
                    float daysSince = today - _ignoreGrantDay[i];
                    if (BloodboundMath.IsIgnoreActive(daysSince, _ignoreDurationDays[i])) continue;
                    _ignoreHeroIds.RemoveAt(i);
                    _ignoreGrantDay.RemoveAt(i);
                    _ignoreDurationDays.RemoveAt(i);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Hardened-flesh HP buff ────────────────────────────────────────────
        internal static void GrantHpBuff(Hero hero)
        {
            if (hero == null) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _hpBuffHeroIds.IndexOf(hero.StringId);
                if (idx >= 0) _hpBuffGrantDay[idx] = today;
                else { _hpBuffHeroIds.Add(hero.StringId); _hpBuffGrantDay.Add(today); }

                float ceiling = hero.MaxHitPoints + BloodboundMath.HpBuffAmount;
                hero.HitPoints = Math.Min(hero.HitPoints + (int)BloodboundMath.HpBuffAmount, (int)ceiling);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void TickHpBuffExpiry()
        {
            if (_hpBuffHeroIds.Count == 0) return;
            float today = (float)CampaignTime.Now.ToDays;
            for (int i = _hpBuffHeroIds.Count - 1; i >= 0; i--)
            {
                try
                {
                    float daysSince = today - _hpBuffGrantDay[i];
                    if (BloodboundMath.IsHpBuffActive(daysSince)) continue;

                    Hero hero = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _hpBuffHeroIds[i]);
                    if (hero != null)
                    {
                        // The week is over — let the overheal fall back to the hero's
                        // normal ceiling. Never raises HP, only clamps an inflated one down.
                        try { hero.HitPoints = Math.Min(hero.HitPoints, hero.MaxHitPoints); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                        if (hero == Hero.MainHero)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                "The hardened flesh fades — your body settles back to its old limits.",
                                new Color(0.55f, 0.10f, 0.10f)));
                        }
                    }

                    _hpBuffHeroIds.RemoveAt(i);
                    _hpBuffGrantDay.RemoveAt(i);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }
    }
}
