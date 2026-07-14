// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestCampaignBehavior.cs
//
// "THE FINAL PEACE" — the Forest Widows' (Battania) Phase 12 questline. The
// Grand Widow proposes to buy the Widows out of the war entirely: not another
// season of the dark being paid to look away
// (Factions/ForestWidows/ForestWidowsCampaignBehavior.Menus.cs's existing
// altar), but an end to the ledger, bought with a single, enormous offering
// (ForestWidowsQuestMath.SacrificeTarget). The player (and, per Great
// Awakening's precedent, the Widows' own lords) feed that count.
//
// The twist: the peace is real, and it costs exactly what it sounds like it
// costs. Once the count is paid, the Widows stop being anyone the demons have
// any reason to fight — because they stop being anyone at all. Every
// remaining Forest Widows clan is permanently bound to the dark (see
// .Resolution.cs): hostile to everyone including the player, their
// settlements garrisoned with the Kindled's own troops
// (Demons/DemonCatalog.cs), and quietly excused from the Night Tide's hunt
// (DemonSpawnCampaignBehavior.DirectDemonParties, extended with
// ForestWidowsQuestCampaignBehavior.IsDarkPactParty). The player gets one
// real choice at that moment — walk away, or let the same thing happen to
// them (see .Resolution.cs for both endings).
//
// Wired into the shared, generic FactionQuestTrigger (FactionQuests/
// FactionQuestTrigger.cs) exactly as WolfHuntQuestCampaignBehavior/
// TowerRiteQuestCampaignBehavior/ChosenQuestCampaignBehavior are — this file
// owns only what is specific to the Final Peace: the phase state machine and
// wiring. .Altar.cs owns the donation menu + NPC trickle; .Resolution.cs owns
// the ending itself.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class ForestWidowsQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle          = 0; // not yet accepted
        internal const int PhaseAccumulating  = 1; // accepted; tracking the sacrifice count
        internal const int PhaseAwaitingChoice = 2; // threshold met, resolution inquiry pending/showing
        internal const int PhaseEndedCastOut  = 3; // player refused — Widows bound to the dark, player free
        internal const int PhaseEndedStayed   = 4; // player accepted the same fate
        internal const int PhaseEndedFactionGone = 5; // Widows wiped out before the count was ever met — balance-pass closure, see CheckFactionGoneWeeklyTick

        private static int _phase = PhaseIdle;

        // Global, save-persistent running total toward ForestWidowsQuestMath.SacrificeTarget.
        private static int _menSacrificed = 0;

        internal static readonly Random _rng = new Random();

        public ForestWidowsQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "forestwidows_finalpeace",
            LeaderResolver = ForestWidowsLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "The Grand Widow calls the court together and names a bargain larger than any the altar has sold " +
                "before: enough sacrificed men, offered at once, to buy the Widows a peace that does not run out. " +
                "Speak with her of it.",
            PlayerAskLine =
                "They say you mean to buy something more than a season's quiet, Grand Widow. Tell me what it costs.",
            LeaderRevealLine =
                "A day at a time is a beggar's bargain — it keeps us alive and nothing more. I mean to pay the " +
                "whole debt at once, and be done with it. Every soldier, every captive, every man this court can " +
                "still spare, fed to the altar until the count is met. When it is, the dark will have no more " +
                "reason to come for Marunath, ever again.",
            PlayerAcceptLine = "Then I'll help you meet that count, Grand Widow — whatever it takes.",
            OnAccepted = OnAccepted,
        };

        internal static Hero ForestWidowsLeader()
        {
            try { return GetForestWidowsKingdom()?.Leader ?? GetForestWidowsKingdom()?.RulingClan?.Leader; }
            catch { return null; }
        }

        internal static Kingdom GetForestWidowsKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == ForestWidowsCulture.CultureId && !k.IsEliminated); }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseAccumulating;
                _menSacrificed = 0;
                ForestWidowsQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Final Peace.", new Color(0.20f, 0.45f, 0.25f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("FWQ_Phase",         ref _phase); }         catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("FWQ_MenSacrificed",  ref _menSacrificed); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            SyncResolutionData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _menSacrificed = 0;
            ResetResolutionState();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterQuestAltarMenu(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { NpcContributionWeeklyTick(); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CheckThresholdWeeklyTick(); }   catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CheckFactionGoneWeeklyTick(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Balance-pass reliability fix: the Forest Widows are scoped to only
        // two seats (ForestWidowsMath.StartingTownIds), so a rival kingdom's war
        // or the Night Tide itself can plausibly wipe them out before the
        // 3,000-man count is ever met. Without this check the quest would sit at
        // PhaseAccumulating forever — the pledge option is only reachable through
        // "forestwidows_altar_main," itself gated behind a Forest-Widows-owned
        // settlement (ForestWidowsSettlements.IsForestWidowsSettlement), which
        // permanently stops existing once the kingdom is gone. Once the kingdom
        // is confirmed gone, the quest resolves to a documented failure instead
        // of leaving a dangling journal entry with no possible closure.
        private static void CheckFactionGoneWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;
            if (GetForestWidowsKingdom() != null) return; // still exists — nothing to do

            _phase = PhaseEndedFactionGone;
            try { ForestWidowsQuestLog.Current?.LogFactionGone(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Shared: record a contribution toward the count ──────────────────────
        internal static void AddSacrifice(int amount)
        {
            if (amount <= 0 || _phase != PhaseAccumulating) return;
            _menSacrificed += amount;
            try { ForestWidowsQuestLog.Current?.LogProgress(_menSacrificed); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Consulted by DemonSpawnCampaignBehavior.DirectDemonParties: once the
        // dark pact is sealed, the Forest Widows are no longer prey — the Night
        // Tide never picks one of their parties, matching "the demons have no
        // more reason to come for Marunath." ─────────────────────────────────────
        internal static bool IsDarkPactParty(MobileParty party)
        {
            if (party == null || !HasJoinedTheDark) return false;
            try { return ForestWidowsCulture.IsForestWidowParty(party.Party); }
            catch { return false; }
        }
    }
}
