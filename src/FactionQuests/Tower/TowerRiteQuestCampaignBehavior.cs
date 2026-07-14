// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Tower/TowerRiteQuestCampaignBehavior.cs
//
// "THE UNBINDING RITE" — Faction B's (the Tower) Phase 12 questline
// (Requirement 21). Gather relics, Demon Blood, and Holy Sigils; deliver them
// to Iyakis for the Great Rite. The rite always fails — it tears open a hole
// for a demon host instead of closing one, and the host rampages against
// nearby settlements for TowerRiteMath.RampageDurationDays before it burns
// itself out (or is cleared early by the player).
//
// Wired into the shared, generic FactionQuestTrigger (see FactionQuests/
// FactionQuestTrigger.cs) exactly as WolfHuntQuestCampaignBehavior is — this
// file owns only what is specific to the Rite: the phase state machine and
// the aftermath. The gather/delivery menu lives in the .Menus.cs partial; the
// host band spawn/rampage/remnant tracking lives in TowerRiteHostParty.cs.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class TowerRiteQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle      = 0; // not yet accepted
        internal const int PhaseGathering = 1; // accepted; collecting/delivering grave-goods
        internal const int PhaseRampage   = 2; // rite performed and failed; host is loose
        internal const int PhaseEnded     = 3; // aftermath applied

        private static int _phase = PhaseIdle;
        private static int _rampageStartDay = -1;

        public TowerRiteQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "tower_unbindingrite",
            LeaderResolver = TowerLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "Word climbs from the Tower's own shelves: the Warlock believes they have found a working " +
                "that could close the Night Tide's door for good. Speak with them of the Great Rite.",
            PlayerAskLine =
                "I hear the Tower thinks it's found a way to end the Tide for good. Tell me of this rite.",
            LeaderRevealLine =
                "Not a theory — a working, assembled from three things no one else would think to set on the " +
                "same altar. A scholar's own relics, to carry the shape of the binding. The Bloodbound's " +
                "vialed blood, to give it something of the Tide to bind against. The Temple's sigils, to hold " +
                "it steady while it closes. Bring me enough of each, to Iyakis, and I will show you what real " +
                "magic can still do.",
            PlayerAcceptLine = "Then tell me how much of each you need.",
            OnAccepted = OnAccepted,
        };

        internal static Hero TowerLeader()
        {
            try
            {
                var k = Kingdom.All.FirstOrDefault(x => x != null && x.StringId == TowerCulture.CultureId && !x.IsEliminated);
                return k?.Leader ?? k?.RulingClan?.Leader;
            }
            catch { return null; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseGathering;
                TowerRiteQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Unbinding Rite.", new Color(0.55f, 0.35f, 0.65f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Wiring ────────────────────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("TWRRITE_Phase",     ref _phase); }           catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("TWRRITE_StartDay",  ref _rampageStartDay); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            TowerRiteHostParty.SyncData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _rampageStartDay = -1;
            TowerRiteHostParty.ResetForNewGame();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterRiteMenu(starter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { TickRampageState(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static int CurrentDay()
        {
            try { return (int)CampaignMapEvents.ElapsedCampaignDays(); }
            catch { return 0; }
        }

        private static void TickRampageState()
        {
            if (_phase != PhaseRampage) return;

            TowerRiteHostParty.TickRampage();

            if (TowerRiteHostParty.LiveHostCount() <= 0)
            {
                // Every band the rite spawned is already dead — the rampage is
                // cleared early. No remnant to leave behind; the relation hit
                // still lands.
                FinishRampage(leaveRemnant: false);
                return;
            }

            int day = CurrentDay();
            if (_rampageStartDay >= 0 && TowerRiteMath.IsRampageOver(day - _rampageStartDay))
                FinishRampage(leaveRemnant: true);
        }

        // ── Called by the Menus partial the moment the rite is performed ────────
        internal static void StartRampage()
        {
            _phase = PhaseRampage;
            _rampageStartDay = CurrentDay();
            TowerRiteHostParty.SpawnHost();
            TowerRiteQuestLog.Current?.LogRiteFails();

            InformationManager.DisplayMessage(new InformationMessage(
                "The Great Rite tears open, not shut. Something vast pours through — the Tower's working has " +
                "failed catastrophically.", new Color(0.60f, 0.05f, 0.05f)));
        }

        private static void FinishRampage(bool leaveRemnant)
        {
            if (leaveRemnant) TowerRiteHostParty.EndRampageLeaveRemnant();
            else TowerRiteHostParty.ClearAllHosts();

            ApplyAftermath();
            _phase = PhaseEnded;
            TowerRiteQuestLog.Current?.LogAftermath();
        }

        // ── Aftermath: a lasting relation hit with the Tower's own scholars ──────
        private static void ApplyAftermath()
        {
            try
            {
                Hero player = Hero.MainHero;
                if (player == null) return;

                var k = Kingdom.All.FirstOrDefault(x => x != null && x.StringId == TowerCulture.CultureId && !x.IsEliminated);
                if (k == null) return;

                foreach (var clan in k.Clans.ToList())
                {
                    if (clan == null || clan.IsEliminated || clan == Clan.PlayerClan) continue;
                    foreach (Hero hero in clan.Heroes.Where(h => h != null && h.IsAlive && h != player).ToList())
                    {
                        try
                        {
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                                player, hero, TowerRiteMath.AftermathRelationPenalty, false);
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }

                InformationManager.DisplayMessage(new InformationMessage(
                    "The Tower does not forgive the ruin the rite made of Iyakis. (Relations with the Tower: " +
                    $"{TowerRiteMath.AftermathRelationPenalty})",
                    new Color(0.55f, 0.35f, 0.65f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
