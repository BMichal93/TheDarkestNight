// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestCampaignBehavior.cs
//
// "THE REUNIFICATION" — the Empire's (Northern Empire) Phase 12 questline.
// The Empire believes reunifying Calradia under one crown is the only real
// answer to the Long Night. The player helps its conquest along; once
// two-thirds of Calradia's cities answer to the Empire again, the ruling
// clan's head is crowned Emperor for real — and, turning that momentum
// outward, the reunified Empire declares war on the demons themselves and
// presses the attack until a large tally of the dead is reached. Unlike
// most of this mod's other Phase 12 endings, this one is allowed to be a
// genuine triumph (see EmpireQuestMath.cs's header).
//
// Three stages, one file each:
//   1. PhaseConquering — weekly-tracks the Empire kingdom's own town count
//      against EmpireQuestMath.ConquestTownThreshold.
//   2. PhaseWar         — the threshold is reached: the crowning fires
//      (.Crowning.cs), war is declared on the demons (.Crowning.cs), Empire
//      lord parties start hunting demon parties (.War.cs), and every demon
//      kill counts toward EmpireQuestMath.KillTarget (.Kills.cs).
//   3. PhaseVictory      — the kill target is reached; a victory-flavoured
//      resolution fires (.Kills.cs) and the Empire simply keeps existing,
//      stronger for it — no disbandment, no splinter, no game-ending event.
//
// Wired into the shared, generic FactionQuestTrigger (FactionQuests/
// FactionQuestTrigger.cs) exactly as TempleQuestCampaignBehavior is.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class EmpireQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle       = 0; // not yet accepted
        internal const int PhaseConquering = 1; // accepted; tracking Empire town count
        internal const int PhaseWar        = 2; // crowned; war declared on demons, counting kills
        internal const int PhaseVictory    = 3; // kill target reached; the Empire's triumph

        private static int _phase = PhaseIdle;

        private static readonly Random _rng = new Random();

        public EmpireQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "empire_thereunification",
            LeaderResolver = EmpireLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "Word reaches you from Saneopa: the Emperor has said, at last, the old and dangerous thing " +
                "plainly — Calradia broke because it was divided, and dividing it further has never once slowed " +
                "the dark. Speak with the Emperor of it.",
            PlayerAskLine =
                "They say you've spoken a dangerous old thing plainly, Emperor. Tell me what you mean to do about it.",
            LeaderRevealLine =
                "Every kingdom left standing fights its own small war, alone, in its own corner of a dying map. " +
                "The old Empire did not fall because it was weak — it fell because it stopped being one thing. " +
                "Help me make it one thing again. Enough cities under one crown, and I will show this Long Night " +
                "what a whole Calradia can still do.",
            PlayerAcceptLine = "Then I'll help you build that crown back, city by city.",
            OnAccepted = OnAccepted,
        };

        internal static Hero EmpireLeader()
        {
            try { return GetEmpireKingdom()?.Leader ?? GetEmpireKingdom()?.RulingClan?.Leader; }
            catch { return null; }
        }

        // The Empire IS the Northern Empire kingdom (see EmpireCulture.cs's own
        // header) — matched by kingdom StringId exactly like TempleQuestCampaign
        // Behavior.GetTempleKingdom does for Vlandia.
        internal static Kingdom GetEmpireKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == EmpireCulture.CultureId && !k.IsEliminated); }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseConquering;
                EmpireQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Reunification.", new Color(0.75f, 0.62f, 0.20f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("EMPQ_Phase", ref _phase); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            SyncWarData(store);
            SyncKillData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            ResetWarState();
            ResetKillState();
        }

        private void OnWeeklyTick()
        {
            try { TickConquestProgress(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { ReassertWarOnDemonLord(null); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { TickAggressionNudges(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CheckVictoryDaily(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Conquest tracking (towns/cities only — see EmpireQuestMath.cs) ───────
        private static void TickConquestProgress()
        {
            if (_phase != PhaseConquering) return;

            var empire = GetEmpireKingdom();
            if (empire == null) return; // wiped out or otherwise gone — nothing to track

            int currentTowns = 0;
            try { currentTowns = empire.Fiefs?.Count(t => t?.Settlement != null && t.Settlement.IsTown) ?? 0; }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            EmpireQuestLog.Current?.LogTownProgress(currentTowns);

            if (EmpireQuestMath.HasReachedThreshold(currentTowns))
                CrownEmperorAndDeclareWar(empire);
        }
    }
}
