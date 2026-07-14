// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Bloodbound/BloodboundQuestCampaignBehavior.cs
//
// "THE SURPASSING RITE" — the Bloodbound's (Khuzait) Phase 12 questline. The
// Huntmaster believes an enormous, unified draught of Demon Blood can do what
// no single vial ever could: carry every Bloodhunter who drinks it past the
// dark's reach entirely. The player (and, per Great Awakening's precedent,
// the Bloodbound's own lords) fill that draught by donating Demon Blood
// (Factions/Bloodbound/BloodboundCatalog.DemonBloodItemId) at the chosen
// shrine city — reusing GreatAwakeningCampaignBehavior's donation-accumulation
// machinery (see .Altar.cs).
//
// The twist: the rite works exactly as promised, and that is the horror of
// it. Once the draught is complete the player faces one real choice — drink
// with them (see .Resolution.cs — a real, permanent death) or run (leave the
// kingdom, alive, and abandon them). Either way, every other Bloodbound
// clan drinks together and dies gruesomely, leaving only their child heirs;
// a few days later the dark descends on the seats they can no longer defend
// (see .Resolution.cs).
//
// Wired into the shared, generic FactionQuestTrigger (FactionQuests/
// FactionQuestTrigger.cs) exactly as WolfHuntQuestCampaignBehavior/
// TowerRiteQuestCampaignBehavior/ChosenQuestCampaignBehavior/
// ForestWidowsQuestCampaignBehavior are — this file owns only what is
// specific to the Rite: the phase state machine and wiring. .Altar.cs owns
// the donation menu + NPC trickle; .Resolution.cs owns the ending itself.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class BloodboundQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle              = 0; // not yet accepted
        internal const int PhaseAccumulating       = 1; // accepted; tracking the donation total
        internal const int PhaseAwaitingChoice     = 2; // threshold met, resolution inquiry pending/showing
        internal const int PhaseEndedParticipated  = 3; // player drank with them — the player character dies
        internal const int PhaseEndedRan           = 4; // player ran — abandoned the Bloodbound kingdom
        internal const int PhaseEndedFactionGone   = 5; // Bloodbound wiped out (rival war/demons) before the draught was ever filled — balance-pass closure, see CheckFactionGoneWeeklyTick

        private static int _phase = PhaseIdle;

        // Global, save-persistent running total toward BloodboundQuestMath.DonationTarget.
        private static int _bloodDonated = 0;

        // The Rite's own shrine — chosen once from BloodboundMath.StartingTownIds
        // (see .Altar.cs's EnsureShrineChosen), mirroring GreatAwakeningCampaign-
        // Behavior's single fixed altar city.
        private static string _shrineSettlementId = null;

        internal static readonly Random _rng = new Random();

        public BloodboundQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "bloodbound_surpassingrite",
            LeaderResolver = BloodboundLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "The Huntmaster calls every rider back to Akkalat and speaks of a rite the old blood-priests only " +
                "ever hinted at: enough Demon Blood, drunk together, all at once, and a Bloodhunter stops being " +
                "prey to the dark forever. Speak with the Huntmaster of it.",
            PlayerAskLine =
                "They say you mean to drink your way past what a man's body can hold, Huntmaster. Tell me of this rite.",
            LeaderRevealLine =
                "Every vial we've ever taken off a kill was a taste, no more. This is the whole draught — every " +
                "Bloodhunter's cup filled and emptied together, until the blood itself remakes what's underneath. " +
                "We need an ocean of it first. Bring what you take from the hunt to Akkalat's shrine, and I'll see " +
                "it's put toward the Rite.",
            PlayerAcceptLine = "Then I'll bring you your ocean, Huntmaster — vial by vial if I have to.",
            OnAccepted = OnAccepted,
        };

        internal static Hero BloodboundLeader()
        {
            try { return GetBloodboundKingdom()?.Leader ?? GetBloodboundKingdom()?.RulingClan?.Leader; }
            catch { return null; }
        }

        internal static Kingdom GetBloodboundKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == BloodboundCulture.CultureId && !k.IsEliminated); }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseAccumulating;
                _bloodDonated = 0;
                BloodboundQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Surpassing Rite.", new Color(0.55f, 0.10f, 0.10f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("BLDQ_Phase",         ref _phase); }               catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("BLDQ_BloodDonated",  ref _bloodDonated); }        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("BLDQ_ShrineId",      ref _shrineSettlementId); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            SyncResolutionData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _bloodDonated = 0;
            _shrineSettlementId = null;
            ResetResolutionState();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { EnsureShrineChosen(); }        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { RegisterQuestAltarMenu(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { EnsureShrineChosen(); }        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NpcContributionWeeklyTick(); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CheckThresholdWeeklyTick(); }   catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CheckFactionGoneWeeklyTick(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Balance-pass reliability fix: the Bloodbound are scoped to only two
        // seats (BloodboundMath.StartingTownIds), so a rival kingdom's war or the
        // Night Tide itself can plausibly wipe them out before the 750-vial
        // draught is ever filled. Without this check the quest would sit at
        // PhaseAccumulating forever — the shrine gate (ShrineIsBloodboundOwned)
        // permanently refuses donations once there is no Bloodbound kingdom left
        // to own it, and CheckThresholdWeeklyTick has nothing left to trigger —
        // a silent, permanent stall with a dangling journal entry and no closure
        // for the player. Once the kingdom is confirmed gone, the quest resolves
        // to a documented failure instead.
        private static void CheckFactionGoneWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;
            if (GetBloodboundKingdom() != null) return; // still exists — nothing to do

            _phase = PhaseEndedFactionGone;
            try { BloodboundQuestLog.Current?.LogFactionGone(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { AftermathDailyTick(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Shared: record a contribution toward the draught ────────────────────
        internal static void AddDonation(int amount)
        {
            if (amount <= 0 || _phase != PhaseAccumulating) return;
            _bloodDonated += amount;
            try { BloodboundQuestLog.Current?.LogProgress(_bloodDonated); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
