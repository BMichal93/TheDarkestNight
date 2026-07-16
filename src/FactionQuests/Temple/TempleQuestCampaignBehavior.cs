// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestCampaignBehavior.cs
//
// "THE UNBROKEN VOW" — the Temple's (Vlandia) Phase 12 questline. The Order
// believes five lost relics — the Five Vigils — can, reunited, bind every
// Templar sword into a single deathless host. There is no clever ending: once
// bound, the Order marches until the dark is gone or they are (see
// TempleQuestMath.cs's header for why "Kill 50,000 demons" is framed as a
// near-impossible, bittersweet milestone rather than a grindable finish
// line).
//
// Four stages, one file each:
//   1. PhaseSeeking   — the five Vigils are assigned to five real ruins the
//      moment the quest is accepted (guaranteed placement — see .Artifacts.cs
//      and TempleQuestMath's header on why the normal ruin-loot RNG is never
//      used for this).
//   2. PhaseDelivery  — once all five are carried, bring them to a Temple
//      town (Ocs Hall or Pravend) to perform the Rite of Union (.Artifacts.cs).
//   3. PhaseBound      — every Temple lord's party is folded into one
//      permanent army (.Army.cs) and every demon kill counts toward the
//      50,000 target (.Kills.cs).
//   4. PhaseEndedDisbanded — the target is somehow reached; hope dwindles and
//      the Order disbands (.Army.cs).
//
// Wired into the shared, generic FactionQuestTrigger (FactionQuests/
// FactionQuestTrigger.cs) exactly as BloodboundQuestCampaignBehavior is.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public sealed partial class TempleQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle            = 0; // not yet accepted
        internal const int PhaseSeeking          = 1; // accepted; the five ruins are assigned, hunting them
        internal const int PhaseDelivery         = 2; // all five carried; must bring them to a Temple town
        internal const int PhaseBound            = 3; // the Vow is sealed — one permanent army, counting kills
        internal const int PhaseEndedDisbanded   = 4; // 50,000 reached — the Order disbands
        internal const int PhaseEndedFactionGone = 5; // the Temple/Vlandia wiped out entirely before the Vow could be kept or broken — balance-pass closure

        private static int _phase = PhaseIdle;

        // Persisted: the five ruin settlement ids the Vigils were assigned to
        // (TempleQuestMath.SelectArtifactRuins — see .Artifacts.cs), and which
        // artifact INDEXES (TempleQuestArtifacts) have actually been found.
        private static List<string> _artifactRuinIds = new List<string>();
        private static List<int>    _foundArtifactIndices = new List<int>();

        public TempleQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "temple_unbrokenvow",
            LeaderResolver = TempleLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "The Grand-Master calls for you at Ocs Hall. The Order has found, in five scattered ruins, the " +
                "trail of relics the old texts call the Five Vigils — and believes that reunited, they could do " +
                "what no living Templar ever has. Speak with the Grand-Master of it.",
            PlayerAskLine =
                "They say you've found the trail of something old, Grand-Master — five relics, scattered. Tell me of it.",
            LeaderRevealLine =
                "The Five Vigils. Every one of them lost in a hall that fell before the Night, and every one of " +
                "them, the old texts swear, meant to be held together, not apart. Bring me all five, and I will " +
                "show every sword in this Order what a true muster looks like.",
            PlayerAcceptLine = "Then I'll find your Vigils, Grand-Master — however far the ruins have scattered them.",
            OnAccepted = OnAccepted,
        };

        internal static Hero TempleLeader()
        {
            try { return GetTempleKingdom()?.Leader ?? GetTempleKingdom()?.RulingClan?.Leader; }
            catch { return null; }
        }

        // The Temple IS Vlandia (see TempleCampaignBehavior.cs's own header) —
        // matched by kingdom StringId exactly like TempleCampaignBehavior.
        // OnClanChangedKingdom already does.
        internal static Kingdom GetTempleKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == "vlandia" && !k.IsEliminated); }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseSeeking;
                _foundArtifactIndices = new List<int>();
                AssignArtifactRuins();
                TempleQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Unbroken Vow.", new Color(0.90f, 0.82f, 0.42f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("TPLQ_Phase",       ref _phase); }               catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("TPLQ_RuinIds",      ref _artifactRuinIds); }     catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("TPLQ_FoundIndices", ref _foundArtifactIndices); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (_artifactRuinIds == null) _artifactRuinIds = new List<string>();
            if (_foundArtifactIndices == null) _foundArtifactIndices = new List<int>();
            SyncArmyData(store);
            SyncKillData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _artifactRuinIds = new List<string>();
            _foundArtifactIndices = new List<int>();
            ResetArmyState();
            ResetKillState();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterDeliveryMenu(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try
            {
                // Session launch can fire more than once per process (a new game
                // started after a load, or vice versa) — unsubscribe first so a
                // repeat launch never stacks a second copy of the same handler
                // onto RuinsExplorationSystem's static event (which would double-
                // grant an artifact on the next ruin full-clear).
                RuinsExplorationSystem.RuinFullyCleared -= OnRuinFullyCleared;
                RuinsExplorationSystem.RuinFullyCleared += OnRuinFullyCleared;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { CheckDisbandDaily(); }         catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { CheckFactionGoneDailyTick(); }  catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Balance-pass reliability fix: Vlandia starts with a normal, sprawling
        // holding, so full elimination is far less likely than for the 1-2-town
        // factions — but a hard enough campaign can still wipe any kingdom out.
        // PhaseSeeking/PhaseDelivery would otherwise stall silently (relic
        // gathering keeps working since the ruins don't care who Vlandia is, but
        // RegisterDeliveryMenu's TempleSettlements.IsTempleSettlement gate can
        // never open again with no Temple town left to open it on) and
        // PhaseBound's permanent army can never be reasserted
        // (ReassertPermanentArmy's own kingdom==null guard, above) — leaving the
        // 50,000-kill tally to wait on a host that no longer exists. Resolve to a
        // documented failure the moment the kingdom is confirmed gone in any of
        // those three phases, rather than a journal entry with no possible close.
        private static void CheckFactionGoneDailyTick()
        {
            if (_phase != PhaseSeeking && _phase != PhaseDelivery && _phase != PhaseBound) return;
            if (GetTempleKingdom() != null) return; // still exists — nothing to do

            _phase = PhaseEndedFactionGone;
            try { TempleQuestLog.Current?.LogFactionGone(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnHourlyTick()
        {
            try { ReassertPermanentArmyHourly(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
