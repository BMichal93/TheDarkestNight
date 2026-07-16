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
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterEmpireMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { EmpireSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickLordGrainClaims(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
