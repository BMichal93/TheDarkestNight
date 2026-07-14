// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntQuestCampaignBehavior.cs
//
// "THE GREAT HUNT" — Faction A's (Wolf Brothers) Phase 12 questline
// (Requirement 21). Three named demon beasts, hunted in escalating order
// (WolfHuntMath.BeastTier), ending in a choice between feeding the pack the
// last beast's flesh (a permanent transformation) or burning it.
//
// Wired into the shared, generic FactionQuestTrigger (see FactionQuests/
// FactionQuestTrigger.cs) — this file owns only what is specific to the Great
// Hunt: the stage state machine, the hunt-target spawn/kill tracking (via
// WolfHuntBeastParty), and the journal (WolfHuntQuestLog). The final choice's
// menu and its two endings live in the .Menus.cs partial.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed partial class WolfHuntQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle                 = 0; // not yet accepted
        internal const int PhaseActive                = 1; // hunting _currentStage
        internal const int PhaseAwaitingFinalChoice   = 2; // all beasts dead; go choose at a Wolf Brothers town
        internal const int PhaseEndedTransformation   = 3;
        internal const int PhaseEndedBurning          = 4;

        private static int _phase        = PhaseIdle;
        private static int _currentStage = -1;

        private static readonly Random _rng = new Random();

        public WolfHuntQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire — so this
            // never races the trigger's own OnSessionLaunchedEvent dialogue
            // registration.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "wolfbrothers_greathunt",
            LeaderResolver = WolfBrothersLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "Word reaches you from the pack's own ground: the Packmaster means to test a Kinsman's " +
                "mettle against the largest things still living. Speak with them of the Great Hunt.",
            PlayerAskLine =
                "I hear you're testing more than loyalty from your Kinsmen these days. Tell me of this Great Hunt.",
            LeaderRevealLine =
                "Loyalty is cheap — anyone can kneel. What the pack needs is proof a Kinsman can put down " +
                "what still walks bigger than we do. Three of the largest things left drawing breath on our " +
                "ground. Bring me their deaths, one after another, each colder than the last — and I'll show " +
                "you what the pack does with what's left over.",
            PlayerAcceptLine = "Then point me at the first of them.",
            OnAccepted = OnAccepted,
        };

        internal static Hero WolfBrothersLeader()
        {
            try
            {
                var k = Kingdom.All.FirstOrDefault(x => x != null && x.StringId == WolfBrothersCulture.CultureId && !x.IsEliminated);
                return k?.Leader ?? k?.RulingClan?.Leader;
            }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseActive;
                _currentStage = 0;
                WolfHuntQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Great Hunt.", new Color(0.55f, 0.15f, 0.12f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEndedHandler);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("WLFHUNT_Phase", ref _phase); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("WLFHUNT_Stage", ref _currentStage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            WolfHuntBeastParty.SyncData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _currentStage = -1;
            WolfHuntBeastParty.ResetForNewGame();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterFinalChoiceMenu(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try { WolfHuntBeastParty.OnMapEventStarted(mapEvent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnMapEventEndedHandler(MapEvent mapEvent)
        {
            try { WolfHuntBeastParty.OnMapEventEnded(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { WolfHuntBeastParty.WeeklyTick(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Daily: spawn/track the current stage's beast ─────────────────────────
        private void OnDailyTick()
        {
            try { TickStageProgress(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void TickStageProgress()
        {
            if (_phase != PhaseActive) return;
            if (!WolfHuntMath.IsValidStage(_currentStage)) return;

            if (WolfHuntBeastParty.CurrentStage != _currentStage)
            {
                // No live (or ever-spawned) beast tracked for this stage yet —
                // spawn one. If the spawn itself fails (e.g. no hideout could be
                // resolved), CurrentStage stays unmatched and this retries the
                // next day — self-healing, matching this codebase's other
                // "retry until it works" spawn patterns.
                SpawnCurrentBeast();
                return;
            }

            if (WolfHuntBeastParty.IsAlive()) return; // still out there — still hunting
            AdvanceStage();
        }

        private static void SpawnCurrentBeast()
        {
            Vec2 anchor = PickAnchor();
            bool ok = WolfHuntBeastParty.Spawn(_currentStage, anchor);
            if (!ok) return;
            InformationManager.DisplayMessage(new InformationMessage(
                $"{WolfHuntCatalog.BeastName(_currentStage)} has been marked. {WolfHuntCatalog.HuntDescription(_currentStage)}",
                new Color(0.55f, 0.15f, 0.12f)));
        }

        private static Vec2 PickAnchor()
        {
            try
            {
                var towns = Settlement.All
                    .Where(s => s != null && s.IsTown && WolfBrothersSettlements.IsWolfBrothersSettlement(s))
                    .ToList();
                if (towns.Count > 0) return towns[_rng.Next(towns.Count)].GetPosition2D;
                return MobileParty.MainParty?.GetPosition2D ?? default;
            }
            catch { return default; }
        }

        private static void AdvanceStage()
        {
            int finished = _currentStage;
            WolfHuntQuestLog.Current?.LogBeastSlain(finished);

            int next = finished + 1;
            if (next >= WolfHuntMath.StageCount)
            {
                _phase = PhaseAwaitingFinalChoice;
                WolfHuntQuestLog.Current?.LogAllBeastsSlain();
                InformationManager.DisplayMessage(new InformationMessage(
                    "The last of them is down. Return to a Wolf Brothers town — the pack is waiting on what " +
                    "you do with the flesh.", new Color(0.55f, 0.15f, 0.12f)));
                return;
            }

            _currentStage = next;
            InformationManager.DisplayMessage(new InformationMessage(
                $"{WolfHuntCatalog.BeastName(next)} is next.", new Color(0.55f, 0.15f, 0.12f)));
        }
    }
}
