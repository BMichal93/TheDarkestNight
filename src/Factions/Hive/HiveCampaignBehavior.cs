// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveCampaignBehavior.cs
//
// Faction C — the Hive (formerly Battania). Owns:
//   • session renaming (kingdom/culture text, via HiveCulture)
//   • town-scoping to Marunath + Car Banseth (via HiveSettlements), daily
//   • the elixir join ritual and the dosing tick that keeps it alive — see
//     HiveCampaignBehavior.Menus.cs for the "drink the elixir" city menu
//   • the death-succession hook: an Integrated player who dies has control
//     handed to a random living Hive lord instead of ending the run
//   • the Hive's own lords absorbing spare prisoners into their ranks for
//     free, on the same rhythm the player's city menu uses
//
// Persistence: the set of Integrated heroes and each one's last-dose day are
// tracked as parallel lists (HIVE_INTEGRATED_IDS / HIVE_LAST_DOSE_DAY),
// exactly the pattern ClanOrdersCampaignBehavior/SEA_*/ELEM_* keys already
// use for this codebase's "small persisted roster" state.
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

namespace AshAndEmber
{
    public partial class HiveCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state (parallel lists, serialised per save) ────────────
        private static List<string> _integratedHeroIds = new List<string>();
        private static List<float>  _lastDoseDay        = new List<float>();

        // Set the instant a death-succession swap happens, so nothing else in
        // this tick (or the dosing tick that follows it) treats the old body as
        // still being the player.
        private static bool _successionInProgress = false;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, OnBeforeMainCharacterDied);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("HIVE_INTEGRATED_IDS",  ref _integratedHeroIds); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("HIVE_LAST_DOSE_DAY",   ref _lastDoseDay);       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            if (_integratedHeroIds == null) _integratedHeroIds = new List<string>();
            if (_lastDoseDay == null) _lastDoseDay = new List<float>();
        }

        public static void ResetForNewGame()
        {
            _integratedHeroIds = new List<string>();
            _lastDoseDay = new List<float>();
            _successionInProgress = false;
        }

        // The kingdom-name rename itself runs from the SAME hook every other
        // culture identity uses — AshenCitySystem.EnsureKingdomRenames (fired off
        // OnSessionLaunchedEvent), which now calls HiveCulture.RenameHiveKingdom()
        // in place of the retired RenameForestClansKingdom(). This behavior only
        // owns what is unique to the Hive: the town-scoping, the elixir/free
        // recruit menus, the dosing tick, and the death-succession hook.
        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterHiveMenus(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { HiveSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickDosing(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickLordFreeRecruit(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Integration bookkeeping ─────────────────────────────────────────────
        internal static bool IsIntegrated(Hero hero)
        {
            if (hero == null) return false;
            try { return _integratedHeroIds.Contains(hero.StringId); } catch { return false; }
        }

        // Called by the "drink the elixir" menu action for both the first dose
        // (joining) and every redose after — the mechanism is identical, only
        // the flavour text at the call site differs.
        internal static void RegisterOrRedose(Hero hero)
        {
            if (hero == null) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _integratedHeroIds.IndexOf(hero.StringId);
                if (idx >= 0) { _lastDoseDay[idx] = today; return; }
                _integratedHeroIds.Add(hero.StringId);
                _lastDoseDay.Add(today);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void Unregister(Hero hero)
        {
            if (hero == null) return;
            UnregisterById(hero.StringId);
        }

        private static void UnregisterById(string heroId)
        {
            if (string.IsNullOrEmpty(heroId)) return;
            try
            {
                int idx = _integratedHeroIds.IndexOf(heroId);
                if (idx < 0) return;
                _integratedHeroIds.RemoveAt(idx);
                _lastDoseDay.RemoveAt(idx);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The join ritual — every genuine joiner is Integrated on the spot ───
        // Fires for every hero in a clan that genuinely joins the Hive (was not
        // already Battania/Hive before) — covers a lord who defects in exactly
        // as it covers the player drinking the elixir at the city menu (which
        // itself calls ChangeKingdomAction.ApplyByJoinToKingdom, triggering this
        // same event). Leaving — by any path, manual or the dosing tick's
        // auto-leave — fires the mirror image: the bond's bookkeeping is dropped
        // and the Hive's bonuses/downsides simply stop applying.
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null) return;

                bool joinedHive = newKingdom != null && newKingdom.StringId == HiveCulture.CultureId
                    && (oldKingdom == null || oldKingdom.StringId != HiveCulture.CultureId);
                bool leftHive = oldKingdom != null && oldKingdom.StringId == HiveCulture.CultureId
                    && (newKingdom == null || newKingdom.StringId != HiveCulture.CultureId);

                if (joinedHive)
                {
                    foreach (Hero hero in clan.Heroes.Where(h => h != null && h.IsAlive).ToList())
                        try { RegisterOrRedose(hero); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                else if (leftHive)
                {
                    foreach (Hero hero in clan.Heroes.Where(h => h != null).ToList())
                        try { Unregister(hero); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The dose must be kept up, or the network lets go ────────────────────
        private void TickDosing()
        {
            if (_integratedHeroIds.Count == 0) return;
            float today = (float)CampaignTime.Now.ToDays;

            // Snapshot indices before mutating — leaving a Hive kingdom during
            // this loop (via the auto-leave call below) re-enters OnClanChangedKingdom,
            // which mutates the very lists we're iterating.
            var snapshot = new List<(string id, float lastDose)>();
            for (int i = 0; i < _integratedHeroIds.Count; i++)
                snapshot.Add((_integratedHeroIds[i], _lastDoseDay[i]));

            foreach (var entry in snapshot)
            {
                try
                {
                    Hero hero = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == entry.id);
                    if (hero == null || !hero.IsAlive) { UnregisterById(entry.id); continue; }

                    float daysSince = today - entry.lastDose;
                    if (!HiveMath.IsBondBroken(daysSince)) continue;

                    // The bond ends exactly the way any manual departure would —
                    // the same ChangeKingdomAction.ApplyByLeaveKingdom path — so
                    // the Hive's bonuses/downsides simply stop applying, with no
                    // special-cased consequence.
                    if (hero.Clan == null || hero.Clan.Kingdom == null || hero.Clan.Kingdom.StringId != HiveCulture.CultureId)
                    {
                        Unregister(hero);
                        continue;
                    }

                    bool isPlayer = hero == Hero.MainHero;
                    ChangeKingdomAction.ApplyByLeaveKingdom(hero.Clan, isPlayer);
                    Unregister(hero);

                    if (isPlayer)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "The dose ran out too long ago. The network's grip loosens, then lets go — you are Integrated no longer.",
                            new Color(0.5f, 0.75f, 0.4f)));
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // ── The Hive's own lords absorb prisoners too ────────────────────────────
        private int _lordRecruitThrottle = 0;
        private void TickLordFreeRecruit()
        {
            if (--_lordRecruitThrottle > 0) return;
            _lordRecruitThrottle = 3; // slow, steady absorption — never every day

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || party.IsMainParty) continue;
                    var lord = party.LeaderHero;
                    if (lord == null || !HiveCulture.IsHiveLord(lord)) continue;
                    if (party.PrisonRoster == null || party.MemberRoster == null) continue;

                    var entry = party.PrisonRoster.GetTroopRoster()
                        .Where(e => e.Character != null && !e.Character.IsHero && e.Number > 0)
                        .OrderBy(e => e.Character.Tier)
                        .FirstOrDefault();
                    if (entry.Character == null) continue;

                    party.PrisonRoster.AddToCounts(entry.Character, -1);
                    party.MemberRoster.AddToCounts(entry.Character, 1);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // ── Death succession ──────────────────────────────────────────────────
        // Investigated: Bannerlord's own "continue as your heir" mechanic is
        // exactly this — SandBox.CampaignBehaviors.HeirSelectionCampaignBehavior
        // listens on the SAME CampaignEvents.OnBeforeMainCharacterDiedEvent,
        // scores candidate heirs via HeirSelectionCalculationModel, and (when a
        // valid heir exists) hands control over with
        // TaleWorlds.CampaignSystem.Actions.ChangePlayerCharacterAction.Apply(Hero) —
        // the same primitive TaleWorlds.CampaignSystem.Actions.ApplyHeirSelectionAction
        // uses internally. That is a genuinely safe, engine-sanctioned way to move
        // "which Hero is the player" mid-campaign, so this hook reuses it directly
        // rather than touching Hero.MainHero or any save-graph field by hand.
        //
        // The one real risk is registration order: campaign events invoke every
        // listener in registration order, and the native SandBox module (which
        // owns HeirSelectionCampaignBehavior) is depended on by, and therefore
        // loads and registers before, this mod — so if the vanilla listener ever
        // decides "no heir, game over" faster than a synchronous call can beat it,
        // our swap below could lose that race. We mitigate as much as a mod can:
        // this listener does no scoring, no UI, and no waiting — it swaps the
        // controlled hero unconditionally and immediately the instant the event
        // fires, which is the fastest any listener on this event can act. Given
        // the Hive always has a candidate pool independent of the player's own
        // clan (unlike vanilla's family-only heir search), this hook does not
        // even need vanilla's own heir check to fail — it always tries. This is
        // the closest safe approximation achievable without patching engine
        // internals, and is documented here per behaviour.md's fragility warning
        // about main-hero reassignment.
        private void OnBeforeMainCharacterDied(Hero victim, Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            try
            {
                if (_successionInProgress) return;
                if (victim == null || victim != Hero.MainHero) return;
                if (!IsIntegrated(victim)) return;

                var candidates = Hero.AllAliveHeroes
                    .Where(h => h != null && h != victim && h.IsAlive && h.IsLord
                             && !h.IsPrisoner && HiveCulture.IsHiveLord(h))
                    .ToList();
                if (candidates.Count == 0) return; // no living Hive lord left — closest safe fallback is to let vanilla resolve the death normally

                Hero successor = candidates[HiveMath.PickSuccessorIndex(candidates.Count, _rng.Next())];

                _successionInProgress = true;
                try
                {
                    Unregister(victim);
                    RegisterOrRedose(successor);
                    ChangePlayerCharacterAction.Apply(successor);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The network does not end with one root. Your mind surfaces in {successor.Name} — the Hive continues.",
                        new Color(0.5f, 0.75f, 0.4f)));
                }
                finally { _successionInProgress = false; }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
