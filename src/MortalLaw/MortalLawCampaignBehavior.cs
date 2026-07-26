// =============================================================================
// THE DARKEST NIGHT — MortalLaw/MortalLawCampaignBehavior.cs
//
// Phase 10 (Requirement 6) — "the NPCs live in the same nightmare, by the same
// rules." Owns campaign-AI shaping across all eight of Phase 7's kingdoms (see
// KingdomIds below) — this is deliberately its OWN folder rather than an
// extension of Campaign/CampaignBehavior.cs: that file is MagicCampaignBehavior,
// already large, and scoped to culture/atmosphere/aging concerns (per
// CLAUDE.md's folder map); "campaign AI shaping" for eight independent
// kingdoms is its own concern, matching how Tribes/Legion/Chosen each own
// their own faction-behavior folder rather than piling onto the shared one.
//
// Four sub-rules, one partial file each:
//   MortalLawCampaignBehavior.cs           — wiring + a) no great kingdoms
//   MortalLawCampaignBehavior.NightFear.cs — b) fear the night
//   MortalLawCampaignBehavior.Hunger.cs    — c) fight for food
//   MortalLawCampaignBehavior.Rosters.cs   — d) same rules as the player
//
// ── a) No great kingdoms ─────────────────────────────────────────────────────
// Reflecting Kingdom/DefaultKingdomDecisionModel-adjacent types turns up no
// clean GameModel governing "should this clan join/found a kingdom" or
// fief-growth decisions — the same finding Phase 7G already made for the raid
// TRIGGER (RaidModel only covers loot/damage math once a raid is under way).
// So, like LegionCampaignBehavior's raid nudge and DemonSpawnCampaignBehavior's
// nightly settlement assaults, over-cap kingdoms are policed directly:
//   - OnClanChangedKingdomEvent: a non-player clan joining (or defecting into)
//     a kingdom that is already over MortalLawMath.KingdomFiefCap is queued and
//     turned back out via ChangeKingdomAction.ApplyByLeaveKingdom on the very
//     next hourly tick — NOT synchronously from inside the event handler. Firing
//     a kingdom-changing action while still inside OnClanChangedKingdomEvent's
//     own dispatch re-enters the native campaign/diplomacy machinery mid-call
//     (exactly the re-entrancy CityStateSystem.OnClanChangedKingdom and
//     AshenCitySystem's own handler are deliberately left EMPTY to avoid — see
//     their comments) and was the root cause of the 2026-07-19 22:08 new-game
//     map crash: ReassignImperialSettlements' MigrateBorderLords joins a clan
//     into an Empire kingdom that new-game setup just pushed over the fief cap
//     in the same synchronous pass, and the old same-tick ApplyByLeaveKingdom
//     call re-entered kingdom-membership mutation before the join had returned.
//   - WarDeclared: if either side of a freshly declared war is an over-cap
//     kingdom (and the player is in neither), the war is immediately reversed
//     to peace via MakePeaceAction — an overextended realm cannot sustain
//     another campaign, so its war-declarations are throttled at the source.
// Both listeners explicitly skip the player's own clan/kingdom — this system
// never overrides the player's choices, exactly like Legion's raid nudge never
// touches MobileParty.MainParty.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class MortalLawCampaignBehavior : CampaignBehaviorBase
    {
        // The eight kingdoms Phase 7 renamed the map down to (StringIds are the
        // underlying vanilla culture/kingdom ids — Phase 7 never touched those,
        // only the display text and dialogue). Any surviving vanilla kingdom
        // outside this list (there should be none post-Phase-8, which turned
        // every other town into a one-city CityState) is left alone — it is
        // not one of ours to police.
        internal static readonly string[] KingdomIds =
        {
            "vlandia",   // the Temple
            "khuzait",   // the Bloodbound
            "battania",  // the Forest Widows
            "aserai",    // the Tower
            "sturgia",   // the Wolf Brothers
            "empire",    // the Empire
            "empire_w",  // Legion
            "empire_s",  // the Pale Widows
        };

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        }

        public override void SyncData(IDataStore store) { /* no persisted state — every sub-rule re-derives from live campaign state each tick */ }

        // Clans queued by OnClanChangedKingdom for ejection on the next hourly
        // tick — never mutated from inside the OnClanChangedKingdomEvent dispatch
        // itself (see the re-entrancy note in the file header). Not persisted:
        // a defection queued the instant a save is written is a harmless corner
        // case (worst case, one extra clan sits a save-reload longer than the
        // cap intends before being turned away).
        private static readonly List<Clan> _pendingDefectorEjections = new List<Clan>();

        public static void ResetForNewGame() { _pendingDefectorEjections.Clear(); }

        private void OnDailyTick()
        {
            try { TickHungerRaids(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnHourlyTick()
        {
            CrashDiagnostics.MarkHourly("MortalLaw.TickNightFear enter");
            try { TickNightFear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            CrashDiagnostics.MarkHourly("MortalLaw.ProcessPendingDefectorEjections enter");
            try { ProcessPendingDefectorEjections(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Runs from the safe top-level hourly-tick context (never from inside a
        // kingdom-change event dispatch) — the only place this system actually
        // calls ChangeKingdomAction for a defector turn-away.
        private void ProcessPendingDefectorEjections()
        {
            if (_pendingDefectorEjections.Count == 0) return;
            var batch = _pendingDefectorEjections.ToList();
            _pendingDefectorEjections.Clear();

            foreach (Clan clan in batch)
            {
                try
                {
                    if (clan == null || clan.IsEliminated || clan == Clan.PlayerClan) continue;
                    if (!IsOneOfOurKingdoms(clan.Kingdom)) continue; // already left, or moved on, by other means
                    if (!MortalLawMath.ShouldTurnAwayDefector(FiefCount(clan.Kingdom))) continue; // cap eased since queued

                    ChangeKingdomAction.ApplyByLeaveKingdom(clan, showNotification: false);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private void OnWeeklyTick()
        {
            try { TickRosterTrim(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static bool IsOneOfOurKingdoms(Kingdom kingdom)
            => kingdom != null && KingdomIds.Contains(kingdom.StringId);

        internal static int FiefCount(Kingdom kingdom)
        {
            try { return kingdom?.Fiefs?.Count ?? 0; }
            catch { return 0; }
        }

        // ── a) No great kingdoms: turn away defectors ──────────────────────────
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || clan == Clan.PlayerClan) return;
                if (newKingdom == null || !IsOneOfOurKingdoms(newKingdom)) return;
                if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom
                 || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion
                 || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary
                 || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction
                 || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction)
                    return; // this clan is LEAVING newKingdom's rival, not joining — nothing to turn away

                if (!MortalLawMath.ShouldTurnAwayDefector(FiefCount(newKingdom))) return;

                // Queue, don't act: calling ChangeKingdomAction from inside this
                // event's own dispatch is the re-entrancy hazard described in the
                // file header. ProcessPendingDefectorEjections (hourly tick) does
                // the actual ApplyByLeaveKingdom from a safe top-level context.
                if (!_pendingDefectorEjections.Contains(clan)) _pendingDefectorEjections.Add(clan);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── a) No great kingdoms: throttle war declarations ────────────────────
        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            try
            {
                var k1 = faction1 as Kingdom;
                var k2 = faction2 as Kingdom;
                if (k1 == null || k2 == null) return; // only kingdom-vs-kingdom wars are in scope

                bool overCap = (IsOneOfOurKingdoms(k1) && MortalLawMath.ShouldThrottleWarDeclaration(FiefCount(k1)))
                            || (IsOneOfOurKingdoms(k2) && MortalLawMath.ShouldThrottleWarDeclaration(FiefCount(k2)));
                if (!overCap) return;

                // Never override the player's own kingdom's wars.
                var playerKingdom = Clan.PlayerClan?.Kingdom;
                if (playerKingdom != null && (playerKingdom == k1 || playerKingdom == k2)) return;

                try { MakePeaceAction.Apply(k1, k2); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
