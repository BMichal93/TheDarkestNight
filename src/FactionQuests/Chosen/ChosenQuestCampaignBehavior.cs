// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Chosen/ChosenQuestCampaignBehavior.cs
//
// "THE PROMISE" — Faction H's (the Chosen, formerly the Southern Empire / the
// deleted Pale Widows) Phase 12 questline (Requirement 21). The PriestKing
// finally speaks his founding vision plain: the angel that named his
// bloodline chosen promised salvation from the demons, once the Chosen
// reclaim enough of Calradia in Heaven's name. The player helps the Chosen
// kingdom's conquest along; once ChosenQuestMath.ConquestFiefThreshold total
// fiefs (towns + castles) are held at once, the promise comes due — and
// nothing answers. No angel, no reprieve, the demons keep coming exactly as
// before. The certainty that held the Chosen together does not survive the
// silence: the kingdom fractures into 2-3 successor kingdoms (see
// ChosenQuestMath.SplinterKingdomIds/Names), all now at war with each other,
// each certain the others misread the vision. The PriestKing cannot admit
// the vision failed — he leads the largest splinter, doubling down rather
// than repenting.
//
// Wired into the shared, generic FactionQuestTrigger (see FactionQuests/
// FactionQuestTrigger.cs) exactly as WolfHuntQuestCampaignBehavior/
// TowerRiteQuestCampaignBehavior are — this file owns only what is specific
// to the Promise: the phase state machine, the weekly conquest tick, the
// hollow-revelation resolution, and the kingdom split itself.
//
// Player clan handling: if the player is a Chosen vassal when the split
// fires, their clan is simply released to independence
// (ChangeKingdomAction.ApplyByLeaveKingdom) rather than folded into a
// splinter chosen for them — "the vision breaks, and no fracture speaks for
// you" is the honest reading, and it avoids dragging the player into a war
// declared without their consent mid-conversation (the same crash-avoidance
// reasoning Soldier/SoldierServiceCampaignBehavior's header already documents
// for encounter-time faction changes — this fires on a clean weekly tick, not
// mid-encounter, but the "never surprise the player into an unconsented war"
// principle still applies).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed partial class ChosenQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle          = 0; // not yet accepted
        internal const int PhaseConquering    = 1; // accepted; tracking Chosen fief count
        internal const int PhaseEnded         = 2; // revelation + split applied
        internal const int PhaseEndedFactionGone = 3; // Chosen wiped out before the promise ever came due — balance-pass closure

        private static int _phase = PhaseIdle;

        private static readonly Random _rng = new Random();

        public ChosenQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "chosen_thepromise",
            LeaderResolver = ChosenLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "The PriestKing summons the faithful and, for the first time, speaks the vision plain: the " +
                "creature of light promised salvation from the demons — but only once the Chosen reclaim enough " +
                "of Calradia in Heaven's name. Speak with him of the promise.",
            PlayerAskLine =
                "They say you've finally spoken the vision plain, PriestKing. Tell me what it demands of us.",
            LeaderRevealLine =
                "The creature did not promise safety for the asking. It promised it for the earning — that the " +
                "dark would not have Calradia while enough of it knelt to the blood Heaven marked. Every town, " +
                "every castle we wrest back is a stone in that wall. Bring me enough of them, and the promise " +
                "comes due.",
            PlayerAcceptLine = "Then I'll help you build that wall, stone by stone.",
            OnAccepted = OnAccepted,
        };

        internal static Hero ChosenLeader()
        {
            try { return ChosenCulture.GetChosenKingdom()?.Leader; }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseConquering;
                ChosenQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Promise.", new Color(0.85f, 0.72f, 0.25f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("CHOQ_Phase", ref _phase); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
        }

        private void OnWeeklyTick()
        {
            try { TickConquestProgress(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Conquest tracking ────────────────────────────────────────────────────
        private static void TickConquestProgress()
        {
            if (_phase != PhaseConquering) return;

            var chosen = ChosenCulture.GetChosenKingdom();
            if (chosen == null)
            {
                // Balance-pass reliability fix: the Chosen are scoped to only two
                // seats (ChosenMath.StartingTownIds), so a rival kingdom's war or
                // the Night Tide itself can plausibly wipe them out before
                // ConquestFiefThreshold is ever reached. Left unhandled this would
                // leave the quest at PhaseConquering forever (a weekly tick that
                // silently no-ops with a dangling, unresolvable journal entry) —
                // instead, resolve to a documented failure the moment the kingdom
                // is confirmed gone.
                _phase = PhaseEndedFactionGone;
                try { ChosenQuestLog.Current?.LogFactionGone(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                return;
            }

            int currentFiefs = 0;
            try { currentFiefs = chosen.Fiefs?.Count ?? 0; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ChosenQuestLog.Current?.LogProgress(currentFiefs);

            if (ChosenQuestMath.HasReachedThreshold(currentFiefs))
                TriggerHollowRevelation(chosen);
        }

        // ── The hollow revelation ────────────────────────────────────────────────
        private static void TriggerHollowRevelation(Kingdom chosen)
        {
            try
            {
                ChosenQuestLog.Current?.LogPromiseIsHollow();

                InformationManager.ShowInquiry(new InquiryData(
                    "The Promise Comes Due",

                    "The Chosen hold more of Calradia than they ever have. The PriestKing climbs to the highest " +
                    "walls of Phycaon at dawn, as the first PriestKing once did, and waits for the sky to open.\n\n" +
                    "It does not. No creature of light descends. No demon falls silent, or turns, or kneels. The " +
                    "Long Night does not so much as pause — somewhere beyond the walls, the same tide rises it " +
                    "always has, indifferent to how much ground was bled for in its name.\n\n" +
                    "The PriestKing will not believe the vision lied. He believes, instead, that someone among " +
                    "his own Apostles must have — that the promise failed because the reclaiming was impure. " +
                    "The accusation does not stay contained. By nightfall, the Chosen are no longer certain they " +
                    "are one faith at all.",

                    true, false, "Heaven is silent.", "",
                    () => { try { PerformSplit(chosen); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    null
                ), true, true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
