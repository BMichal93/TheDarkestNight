// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleCampaignBehavior.cs
//
// Faction E — the Temple (Vlandia). Vlandia is ALREADY "The Holy Temple" in
// the baseline AshAndEmber codebase (src/AI/TempleCulture.cs, TempleDialogue.cs,
// AshenCitySystem.ApplyTempleCultureTexts / RenameHolyTempleKingdom) — this
// behavior only owns what Phase 7 of The Darkest Night ADDS on top of that
// existing identity:
//   • town-scoping to Ocs Hall + Pravend (via TempleSettlements), daily
//   • the join gate — the Order turns away the devious and the cruel (see
//     OnClanChangedKingdom below), mirroring BloodboundCampaignBehavior's
//     Qualifies/refusal pattern
//   • granting the player a Holy Sigil the moment they genuinely join
//   • the town menus: buy a Holy Sigil, and pray (see
//     TempleCampaignBehavior.Menus.cs)
//
// The kingdom-name rename itself already runs from AshenCitySystem's
// EnsureKingdomRenames (TempleCulture.SetupTempleKingdom / RenameHolyTempleKingdom)
// — untouched by this file.
//
// Persistence: the last-prayer day is tracked as parallel lists
// (TPL_PRAYER_IDS/TPL_PRAYER_DAY), the same "small persisted roster" pattern
// BloodboundCampaignBehavior's ignore-buff tracker already uses.
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
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public partial class TempleCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // ── Persistent state (parallel lists, serialised per save) ────────────
        private static List<string> _prayerHeroIds = new List<string>();
        private static List<float>  _prayerLastDay  = new List<float>();

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
            try { store.SyncData("TPL_PRAYER_IDS", ref _prayerHeroIds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("TPL_PRAYER_DAY",  ref _prayerLastDay); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (_prayerHeroIds == null) _prayerHeroIds = new List<string>();
            if (_prayerLastDay == null) _prayerLastDay = new List<float>();
        }

        public static void ResetForNewGame()
        {
            _prayerHeroIds = new List<string>();
            _prayerLastDay = new List<float>();
            _pendingUnworthyEjections.Clear();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterTempleMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { TempleSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickLordPrayers(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
                    if (clan.Kingdom == null || clan.Kingdom.StringId != "vlandia") continue;
                    ChangeKingdomAction.ApplyByLeaveKingdom(clan, clan == Clan.PlayerClan);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // Temple bonuses apply to Temple lords too: a small daily chance a
        // qualifying (non-devious, non-cruel) Temple lord prays on their own,
        // exactly the way MiracleCampaignBehavior already simulates NPC Grace
        // use for virtuous lords.
        private void TickLordPrayers()
        {
            foreach (var hero in Hero.AllAliveHeroes.ToList())
            {
                try
                {
                    if (hero == Hero.MainHero || hero.PartyBelongedTo == null) continue;
                    if (!TempleCulture.IsTempleLord(hero)) continue;
                    if (!Qualifies(hero)) continue;
                    if (!CanPrayToday(hero)) continue;
                    if (_rng.NextDouble() >= TempleMath.NpcDailyPrayChance) continue;

                    ApplyPrayer(hero);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── The join gate — the Order refuses the devious and the cruel ────────
        // Fires for every clan that genuinely joins the Temple (was not already
        // Vlandia/Temple before). Mirrors BloodboundCampaignBehavior's gate
        // exactly: a leader who does not qualify is turned right back out with
        // an in-fiction refusal instead of a silent bounce. A qualifying player
        // additionally receives a Holy Sigil on the spot.
        //
        // The ejection itself is QUEUED (ProcessPendingUnworthyEjections, next
        // hourly tick), never called synchronously from in here — see
        // MortalLawCampaignBehavior's header for why calling a kingdom-changing
        // action from inside this event's own dispatch is a re-entrancy hazard
        // (the confirmed root cause of the 2026-07-19 new-game map crashes).
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || newKingdom == null) return;
                if (newKingdom.StringId != "vlandia") return;
                if (oldKingdom != null && oldKingdom.StringId == "vlandia") return; // already Temple

                Hero leader = clan.Leader;
                bool isPlayerClan = clan == Clan.PlayerClan;

                if (leader != null && !Qualifies(leader))
                {
                    if (!_pendingUnworthyEjections.Contains(clan)) _pendingUnworthyEjections.Add(clan);

                    if (isPlayerClan)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "The Order looks past your oath and into what you are. \"We do not arm a cruel hand, "
                          + "nor bind a devious one to our vow. Go, and do not swear falsely to the Light again.\"",
                            new Color(0.90f, 0.82f, 0.42f)));
                    }
                    return;
                }

                if (isPlayerClan)
                    try { GrantHolySigil(Hero.MainHero, announce: true); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static bool Qualifies(Hero hero)
        {
            if (hero == null) return true; // never hard-block on a null hero
            try
            {
                int calculating = hero.GetTraitLevel(DefaultTraits.Calculating);
                int mercy       = hero.GetTraitLevel(DefaultTraits.Mercy);
                return TempleMath.QualifiesForTemple(calculating, mercy);
            }
            catch { return true; } // fail open — never hard-block on an unexpected engine error
        }

        // ── The Holy Sigil ───────────────────────────────────────────────────
        internal static void GrantHolySigil(Hero hero, bool announce)
        {
            if (hero == null) return;
            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(TempleSigilCatalog.HolySigilItemId);
                var roster = hero.PartyBelongedTo?.ItemRoster ?? (hero == Hero.MainHero ? MobileParty.MainParty?.ItemRoster : null);
                if (item == null || roster == null) return;
                roster.AddToCounts(item, 1);

                if (announce && hero == Hero.MainHero)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "A Holy Sigil is pressed into your hand — plain grey stone, warm to the touch. "
                      + "\"Carry it against the dark. It does not care whose hand holds it, only what that hand has sworn.\"",
                        new Color(0.90f, 0.82f, 0.42f)));
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── The pray menu ────────────────────────────────────────────────────
        internal static bool CanPrayToday(Hero hero)
        {
            if (hero == null) return false;
            try
            {
                int idx = _prayerHeroIds.IndexOf(hero.StringId);
                if (idx < 0) return true;
                float today = (float)CampaignTime.Now.ToDays;
                return TempleMath.IsPrayerReady(today - _prayerLastDay[idx]);
            }
            catch { return true; }
        }

        internal static void MarkPrayed(Hero hero)
        {
            if (hero == null) return;
            try
            {
                float today = (float)CampaignTime.Now.ToDays;
                int idx = _prayerHeroIds.IndexOf(hero.StringId);
                if (idx >= 0) _prayerLastDay[idx] = today;
                else { _prayerHeroIds.Add(hero.StringId); _prayerLastDay.Add(today); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Applies the prayer's beneficial party effects: heals every member of
        // the hero's party by a fraction of their missing HP and lifts party
        // morale, both scaled by the hero's own Honor+Mercy (see TempleMath —
        // per the Miracles lore, conviction is drawn from the caster, never
        // granted by a watching Light). Applies identically to the player and
        // to Temple lords, so a virtuous Templar lord's own party benefits too.
        internal static void ApplyPrayer(Hero hero)
        {
            if (hero == null) return;
            try
            {
                int honor = hero.GetTraitLevel(DefaultTraits.Honor);
                int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
                float healFrac = TempleMath.PrayerHealFraction(honor, mercy);
                float moraleGain = TempleMath.PrayerMoraleGain(honor, mercy);

                var party = hero.PartyBelongedTo;
                if (party != null)
                {
                    party.RecentEventsMorale = Math.Min(party.RecentEventsMorale + moraleGain, 100f);

                    foreach (var member in party.MemberRoster.GetTroopRoster().ToList())
                    {
                        if (member.Character == null) continue;
                        // Wounded-troop healing on the campaign roster: heal a fraction of
                        // the wounded count back to healthy, same "heal party" idiom used
                        // elsewhere in this codebase's ritual/altar systems.
                        int wounded = member.WoundedNumber;
                        if (wounded <= 0) continue;
                        int healed = (int)Math.Ceiling(wounded * healFrac);
                        if (healed <= 0) continue;
                        try { party.MemberRoster.AddToCounts(member.Character, 0, false, -Math.Min(healed, wounded)); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                }

                MarkPrayed(hero);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
