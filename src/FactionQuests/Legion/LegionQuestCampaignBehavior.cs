// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestCampaignBehavior.cs
//
// "THE FAR SHORE" — Legion's (Western Empire) Phase 12 questline. The Warlord
// does not believe the Long Night can be won or banished — only outrun. He
// means to build an ark at Ortysia, stock it with a very significant hoard of
// hardwood and iron, and sail every soul who will follow him beyond the sea.
// The player feeds that stockpile; once it is complete (and Ortysia is still
// Legion's), the player chooses: sail beyond the sea themselves, or stay and
// take up the Warlord's place while a handful of Legion's own clans abandon
// the kingdom rather than follow either path.
//
// Partials:
//   .Menu.cs           — the Ortysia "Stock the ark" donation submenu
//   .NpcContribution.cs — Legion lords occasionally donating + the theft-on-
//                         capture decay (repurposes NorthmenStonesCampaign
//                         Behavior's exact mechanism — see LegionQuestMath.cs)
//   .Ending.cs          — the two-choice resolution once the stock is complete
//
// Wired into the shared, generic FactionQuestTrigger (FactionQuests/
// FactionQuestTrigger.cs) exactly as EmpireQuestCampaignBehavior is.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public sealed partial class LegionQuestCampaignBehavior : CampaignBehaviorBase
    {
        // ── Phase state machine ──────────────────────────────────────────────────
        internal const int PhaseIdle      = 0; // not yet accepted
        internal const int PhaseGathering = 1; // accepted; stocking the ark at Ortysia
        internal const int PhaseEndedSail = 2; // ending (a) — sailed beyond the sea
        internal const int PhaseEndedStay = 3; // ending (b) — stayed as the new Warlord
        internal const int PhaseEndedFactionGone = 4; // Legion wiped out before the ark was ever finished — balance-pass closure

        private static int _phase = PhaseIdle;

        // ── The ark's stock — persistent, decays only while Ortysia isn't Legion's ──
        internal static int _hardwood;
        internal static int _iron;

        internal static readonly Random _rng = new Random();

        public LegionQuestCampaignBehavior()
        {
            // Registration is idempotent (FactionQuestTrigger.Register dedupes
            // by Id) and runs at construction time — every OnGameStart, new
            // game or load, well before any CampaignEvents fire.
            try { FactionQuestTrigger.Register(BuildDef()); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static FactionQuestDef BuildDef() => new FactionQuestDef
        {
            Id = "legion_thefarshore",
            LeaderResolver = LegionLeader,
            IsEligible = () => _phase == PhaseIdle,
            NotificationText =
                "Word reaches you from Ortysia: the Warlord has said, in front of the whole column, that this " +
                "land cannot be held forever — only raided a little longer before the dark takes it back for " +
                "good. He speaks of an ark. Speak with him of it.",
            PlayerAskLine =
                "They say you mean to build a ship and leave all this behind, Warlord. Tell me it's true.",
            LeaderRevealLine =
                "Might makes right — I've never once told a Comrade otherwise. But might can't hold a whole " +
                "world against what's coming, and I'll not spend Legion's last strength pretending it can. " +
                "There's a peace beyond the sea the mainland will never have again. I mean to build the ship " +
                "that finds it. Bring hardwood and iron to Ortysia — a great deal of both — and help me stock her.",
            PlayerAcceptLine = "Then I'll help you build your ark, Warlord.",
            OnAccepted = OnAccepted,
        };

        internal static Hero LegionLeader()
        {
            try { return GetLegionKingdom()?.Leader ?? GetLegionKingdom()?.RulingClan?.Leader; }
            catch { return null; }
        }

        internal static Kingdom GetLegionKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == LegionCulture.KingdomId && !k.IsEliminated); }
            catch { return null; }
        }

        internal static Settlement OrtysiaSettlement()
        {
            try
            {
                return Settlement.All.FirstOrDefault(s => s != null && s.IsTown
                    && string.Equals(s.StringId, "town_EW4", StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }

        internal static bool IsOrtysiaLegionOwned()
        {
            try { return OrtysiaSettlement()?.MapFaction?.StringId == LegionCulture.KingdomId; }
            catch { return false; }
        }

        private static void OnAccepted()
        {
            try
            {
                _phase = PhaseGathering;
                LegionQuestLog.Start();
                InformationManager.DisplayMessage(new InformationMessage(
                    "Quest added: The Far Shore.", new Color(0.75f, 0.62f, 0.20f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
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
            try { store.SyncData("LEGQ_Phase",    ref _phase); }    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("LEGQ_Hardwood", ref _hardwood); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("LEGQ_Iron",     ref _iron); }     catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            SyncEndingData(store);
        }

        public static void ResetForNewGame()
        {
            _phase = PhaseIdle;
            _hardwood = 0;
            _iron = 0;
            ResetEndingState();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnWeeklyTick()
        {
            try { NpcContributionWeeklyTick(); }  catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ApplyDecayWeeklyTick(); }       catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { CheckFactionGoneWeeklyTick(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Balance-pass reliability fix: Legion is scoped to only two seats
        // (LegionMath.StartingTownIds). ApplyDecayWeeklyTick already handles
        // Ortysia changing hands (the stock decays but the quest keeps waiting,
        // per the brief's own "theft when Ortysia falls" note) — but if Legion
        // is wiped out ENTIRELY (both Lageta and Ortysia lost for good), there is
        // no path back: Ortysia can never become Legion's again, so
        // EndingDailyTick's IsOrtysiaLegionOwned gate would block the ending
        // forever. Resolve to a documented failure once the kingdom itself is
        // confirmed gone, rather than leaving the quest decaying toward zero
        // forever with no closure.
        private static void CheckFactionGoneWeeklyTick()
        {
            if (_phase != PhaseGathering) return;
            if (GetLegionKingdom() != null) return; // still exists — nothing to do

            _phase = PhaseEndedFactionGone;
            try { LegionQuestLog.Current?.LogFactionGone(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { EndingDailyTick(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void NotifyStockChanged(string text)
        {
            try
            {
                int pct = (int)(LegionQuestMath.BlendedProgress(_hardwood, _iron) * 100f);
                LegionQuestLog.Current?.LogStockProgress(_hardwood, _iron);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{text} The ark's stock stands at {pct}%.", new Color(0.75f, 0.62f, 0.20f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
