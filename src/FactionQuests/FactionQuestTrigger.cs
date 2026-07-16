// =============================================================================
// THE DARKEST NIGHT — FactionQuests/FactionQuestTrigger.cs
//
// GENERIC, REUSABLE infrastructure for Phase 12 (Requirement 21) — "eight
// faction questlines, all sharing the same day-50 leader-conversation
// trigger." Every faction questline (Wolf Brothers now; Tower, the Forest
// Widows, the Bloodbound, the Temple, the Empire, Legion, the Pale Widows
// later) hooks in
// by constructing ONE FactionQuestDef and calling FactionQuestTrigger.Register
// — nothing here knows anything about any specific faction.
//
// Mirrors GreatAwakeningCampaignBehavior.Trigger.cs/.Dialogue.cs's proven
// shape (a day-gated one-time notification, then a leader-only dialogue line
// off "hero_main_options" that fires the quest's own start callback) but
// factored so eight questlines share one registry instead of duplicating the
// notify/dialogue plumbing eight times.
//
// ── How a questline uses this ────────────────────────────────────────────────
//   FactionQuestTrigger.Register(new FactionQuestDef
//   {
//       Id               = "<faction>_<quest>",      // unique, short, stable
//       LeaderResolver    = () => ...,                 // the faction's current leader Hero (or null)
//       IsEligible        = () => <quest not yet offered/active/done>,
//       NotificationText  = "...",                     // shown once, day 50+
//       PlayerAskLine     = "...",
//       LeaderRevealLine  = "...",
//       PlayerAcceptLine  = "...",
//       OnAccepted        = () => { <start the quest's own state machine> },
//   });
// Call Register once — the cheapest safe place is the questline's
// CampaignBehaviorBase constructor, which runs every time MagicSystem.cs's
// OnGameStart constructs a fresh instance (new game AND load), well before any
// CampaignEvents fire. Register() itself is idempotent (dedupes by Id), so a
// repeated call across session launches within one process is harmless.
//
// ── What this owns / does not own ───────────────────────────────────────────
// Owns: the day-50 notification gate and the three-line "ask the leader"
// dialogue exchange. Does NOT own anything about the quest itself — stage
// state, journals, endings all live in the questline's own files, exactly as
// GreatAwakeningCampaignBehavior.Dialogue.cs's OnAskConsequence only flips a
// phase and starts that quest's own journal.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed class FactionQuestDef
    {
        // Unique, stable, short — used as both the internal dictionary key and
        // the dialogue line id prefix. Never change once shipped (dialogue ids
        // are not persisted, but changing one mid-campaign would silently drop
        // a still-pending notification for players already past day 50).
        public string Id;

        // Resolves the faction's current leader Hero. May return null (e.g.
        // between successions) — the trigger simply waits for a non-null leader
        // before ever notifying or offering the dialogue line.
        public Func<Hero> LeaderResolver;

        // Extra gate beyond "has been notified" — typically "the quest has not
        // already been offered/started/finished." Checked both before the
        // one-time notification fires and every time the dialogue condition is
        // evaluated, so a quest that started (or finished) through some other
        // path never re-offers itself.
        public Func<bool> IsEligible;

        public string NotificationText;
        public string PlayerAskLine;
        public string LeaderRevealLine;
        public string PlayerAcceptLine;

        // Invoked once, the moment the player accepts — starts the questline's
        // own state machine (mirrors GreatAwakeningCampaignBehavior.Dialogue.cs's
        // OnAskConsequence).
        public Action OnAccepted;
    }

    public static class FactionQuestTrigger
    {
        // Base priority for the shared dialogue pool — clear of every other
        // "hero_main_options" hub priority already in use in this codebase
        // (GreatAwakeningCampaignBehavior.Dialogue: 150, NorthmenStonesCampaignBehavior.
        // Dialogue: 150 — both safe to share since their conditions are mutually
        // exclusive per interlocutor). Each registered def gets basePriority + its
        // registration index, so eight questlines never collide with each other.
        private const int BasePriority = 152;

        private static readonly List<FactionQuestDef> _defs = new List<FactionQuestDef>();
        private static readonly HashSet<string> _notified = new HashSet<string>();

        // Defs are re-registered (idempotently) every session launch by each
        // questline's own constructor — never cleared here. Only the "has this
        // faction's notification already fired" state is per-campaign.
        public static void Register(FactionQuestDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            if (_defs.Any(d => d.Id == def.Id)) return;
            _defs.Add(def);
        }

        public static void ResetForNewGame()
        {
            _notified.Clear();
        }

        // ── Persistence (called from FactionQuestTriggerCampaignBehavior.SyncData) ──
        internal static List<string> NotifiedSnapshot() => _notified.ToList();

        internal static void RestoreNotified(List<string> ids)
        {
            _notified.Clear();
            if (ids == null) return;
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id)) _notified.Add(id);
        }

        // ── Weekly: the day-50 gate ──────────────────────────────────────────────
        public static void WeeklyTick()
        {
            int day;
            try { day = (int)CampaignMapEvents.ElapsedCampaignDays(); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return; }
            if (!FactionQuestMath.IsTriggerEligible(day)) return;

            foreach (var def in _defs)
            {
                try
                {
                    if (_notified.Contains(def.Id)) continue;
                    if (def.IsEligible != null && !def.IsEligible()) continue;
                    Hero leader = def.LeaderResolver?.Invoke();
                    if (leader == null) continue; // wait for a leader to exist before speaking of it

                    _notified.Add(def.Id);
                    if (!string.IsNullOrEmpty(def.NotificationText))
                        MBInformationManager.AddQuickInformation(new TextObject(def.NotificationText));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Dialogue: "ask the leader" — off hero_main_options, one exchange per def ──
        public static void RegisterDialogue(CampaignGameStarter starter)
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                FactionQuestDef def = _defs[i];
                int priority = BasePriority + i;
                try
                {
                    starter.AddPlayerLine(
                        def.Id + "_ask", "hero_main_options", def.Id + "_reveal",
                        def.PlayerAskLine, () => CanAsk(def), null, priority);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                try
                {
                    starter.AddDialogLine(
                        def.Id + "_reveal_npc", def.Id + "_reveal", def.Id + "_reveal_player",
                        def.LeaderRevealLine, () => CanAsk(def), null, priority);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                try
                {
                    starter.AddPlayerLine(
                        def.Id + "_accept", def.Id + "_reveal_player", "close_window",
                        def.PlayerAcceptLine, null, () => OnAccept(def), priority);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static bool CanAsk(FactionQuestDef def)
        {
            try
            {
                if (def == null || !_notified.Contains(def.Id)) return false;
                if (def.IsEligible != null && !def.IsEligible()) return false;
                Hero h = Hero.OneToOneConversationHero;
                if (h == null) return false;
                Hero leader = def.LeaderResolver?.Invoke();
                return leader != null && h == leader;
            }
            catch { return false; }
        }

        private static void OnAccept(FactionQuestDef def)
        {
            try { def?.OnAccepted?.Invoke(); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
