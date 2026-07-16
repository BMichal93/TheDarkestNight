// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireCampaignBehavior.cs
//
// Faction F — the Empire (Northern Empire). Owns everything Phase 7 adds for
// this faction: town-scoping to Saneopa/Diathma/Argoron (via
// EmpireSettlements), and the daily free-grain claim in Empire towns (see
// EmpireCampaignBehavior.Menus.cs). Unlike Vlandia/Khuzait/Sturgia/Aserai/
// Battania, the Northern Empire had no prior baseline identity to extend —
// EmpireCulture.cs is a from-scratch rename, not a supersession.
//
// Persistence: last-claim day is tracked as parallel lists (EMP_GRAIN_IDS/
// EMP_GRAIN_DAY), the same "small persisted roster" pattern TempleCampaign
// Behavior's prayer tracker and BloodboundCampaignBehavior's ignore-buff
// tracker already use.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TheDarkestNight
{
    public partial class EmpireCampaignBehavior : CampaignBehaviorBase
    {
        // ── Persistent state (parallel lists, serialised per save) ────────────
        private static List<string> _claimHeroIds = new List<string>();
        private static List<float>  _claimLastDay  = new List<float>();

        // Not persisted — a missed roll after a reload just waits out the next
        // interval, exactly like DemonSpawnCampaignBehavior's day counters.
        private static int _daysUntilNextScheme = -1;
        private static readonly Random _schemeRng = new Random();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("EMP_GRAIN_IDS", ref _claimHeroIds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("EMP_GRAIN_DAY",  ref _claimLastDay); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (_claimHeroIds == null) _claimHeroIds = new List<string>();
            if (_claimLastDay == null) _claimLastDay = new List<float>();
        }

        public static void ResetForNewGame()
        {
            _claimHeroIds = new List<string>();
            _claimLastDay = new List<float>();
            _daysUntilNextScheme = EmpireMath.RollSchemeIntervalDays(_schemeRng);
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterEmpireMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { EmpireSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickLordGrainClaims(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickNpcSchemes(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Issue 11 — the Empire's Schemes access (SchemeSystem.cs — influence-only,
        // gated to EmpireCulture.IsPlayerEmpireKingdom for the PLAYER menu) had no
        // NPC-side counterpart: TryQueueNpcScheme existed but nothing called it.
        // Every rolled interval, one Empire lord at war with a rival kingdom is
        // picked to run one scheme through that same, now-wired execution path.
        private void TickNpcSchemes()
        {
            if (_daysUntilNextScheme < 0) _daysUntilNextScheme = EmpireMath.RollSchemeIntervalDays(_schemeRng);
            if (--_daysUntilNextScheme > 0) return;
            _daysUntilNextScheme = EmpireMath.RollSchemeIntervalDays(_schemeRng);

            var empire = Kingdom.All.FirstOrDefault(k => k.StringId == "empire" && !k.IsEliminated);
            if (empire == null) return;

            var rivalLord = Hero.AllAliveHeroes
                .Where(h => h.IsLord && h.IsAlive && !h.IsPrisoner && !h.IsChild
                         && h.Clan != null && h.Clan.Kingdom == empire
                         && h.Clan.Kingdom.Leader != h) // the ruler stays above the scheming
                .OrderBy(_ => _schemeRng.Next())
                .FirstOrDefault();
            if (rivalLord == null) return;

            try { SchemeSystem.TryQueueNpcScheme(rivalLord); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Empire bonuses apply to Empire lords too: every Empire lord party
        // simply receives its daily grain automatically (no menu trip needed
        // for an AI-controlled party), on the same cooldown the player faces.
        private void TickLordGrainClaims()
        {
            foreach (var hero in Hero.AllAliveHeroes.ToList())
            {
                try
                {
                    if (hero == Hero.MainHero || hero.PartyBelongedTo == null) continue;
                    if (!EmpireCulture.IsEmpireLord(hero)) continue;
                    if (!CanClaimToday(hero)) continue;

                    GrantGrain(hero.PartyBelongedTo);
                    MarkClaimed(hero);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        internal static bool CanClaimToday(Hero hero)
        {
            if (hero == null) return false;
            try
            {
                int idx = _claimHeroIds.IndexOf(hero.StringId);
                if (idx < 0) return true;
                float today = (float)CampaignTime.Now.ToDays;
                return EmpireMath.IsClaimReady(today - _claimLastDay[idx]);
            }
            catch { return true; }
        }

        internal static void MarkClaimed(Hero hero)
        {
            if (hero == null) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _claimHeroIds.IndexOf(hero.StringId);
                if (idx >= 0) _claimLastDay[idx] = today;
                else { _claimHeroIds.Add(hero.StringId); _claimLastDay.Add(today); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void GrantGrain(TaleWorlds.CampaignSystem.Party.MobileParty party)
        {
            if (party?.ItemRoster == null) return;
            try
            {
                var grain = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<ItemObject>("grain");
                if (grain == null) return;
                party.ItemRoster.AddToCounts(grain, EmpireMath.GrainClaimAmount);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
