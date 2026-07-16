// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellbookCampaignBehavior.cs
//
// Persists which formulas the player knows (a parallel-list-of-ids pattern,
// exactly like DemonSpawnCampaignBehavior's DEMON_PartyIds / ELEM_* keys)
// and gates casting behind Requirement 13's one-time unlock: spend one focus
// point, once, to "open the spellbook."
//
// ── Learning sources (Requirement 13) ────────────────────────────────────
//   • Ruins            — stub only; ruin exploration is Phase 9.
//   • The Tower faction — stub only; faction teaching is Phase 7.
//   • Character creation — stub only; the arcane background is Phase 13.
//   • A correct spoken formula, even one never learned before (Requirement
//     16) — this IS implemented; SpellbookInputHandler calls LearnSpell on
//     every successful cast, which is the mechanic that actually teaches
//     spells in practice during this phase.
//   • Debug (Ctrl+Shift+F12) — unlocks the book and learns every spell, for
//     testing (see MagicSystem.DebugGrantAll's extension).
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public class SpellbookCampaignBehavior : CampaignBehaviorBase
    {
        private static bool _unlocked;
        private static readonly HashSet<SpellId> _known = new HashSet<SpellId>();

        public static bool IsUnlocked => _unlocked;
        public static bool KnowsSpell(SpellId id) => _known.Contains(id);
        public static IEnumerable<SpellId> KnownSpells => _known;

        public static void ResetForNewGame()
        {
            _unlocked = false;
            _known.Clear();
        }

        public override void RegisterEvents()
        {
            // Issue 10 — spellbook inheritance. OnHeirSelectionOverEvent fires once
            // the engine has already picked the new Hero.MainHero (vanilla only
            // triggers heir selection on the previous main character's death), so
            // by the time this runs succession is complete and this is purely
            // "ask, and on decline, wipe" — the state itself (_unlocked/_known) is
            // already player-global and survives untouched either way.
            CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
        }

        private void OnHeirSelectionOver(Hero newMainHero)
        {
            try
            {
                if (!_unlocked || _known.Count == 0) return;
                // Never clobber another system's queued popup — losing the ask is
                // safer than losing a queued event (behaviour.md's single-slot rule).
                // Keeping the book silently is the safe default either way.
                if (MageKnowledge._deferredInquiry != null) return;

                int count = _known.Count;
                MageKnowledge._deferredInquiry = () =>
                {
                    try
                    {
                        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                            "The Book Passes",
                            "Your hands are done with the marks. Your heir now carries the book — and every "
                            + $"formula written in your years ({count} known). Or let it burn with you.",
                            new List<InquiryElement>
                            {
                                new InquiryElement("keep", "Leave them the book", null, true,
                                    "The formulas pass on unbroken."),
                                new InquiryElement("burn", "Let it burn", null, true,
                                    "Every formula is forgotten."),
                            },
                            false, 1, 1, "Choose.", "",
                            chosen =>
                            {
                                bool burn = chosen?.Any(e => e.Identifier is string s && s == "burn") == true;
                                if (burn)
                                {
                                    ForgetAll();
                                    InformationManager.DisplayMessage(new InformationMessage(
                                        "The book burns with its keeper. Nothing of it remains.", Dim));
                                }
                                else
                                {
                                    InformationManager.DisplayMessage(new InformationMessage(
                                        $"The book passes on: {count} formula{(count != 1 ? "s" : "")} carried into the next hand.", Gold));
                                }
                            },
                            null, "", false), false, true);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                };
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Wipes the unlocked state and every known formula — used only when the
        // player deliberately lets the book burn with them on succession (issue 10).
        // Deliberately separate from ResetForNewGame, which may grow other duties.
        public static void ForgetAll()
        {
            _unlocked = false;
            _known.Clear();
        }

        public override void SyncData(IDataStore dataStore)
        {
            var ids = _known.Select(s => (int)s).ToList();
            dataStore.SyncData("SPELLBOOK_Unlocked", ref _unlocked);
            dataStore.SyncData("SPELLBOOK_KnownIds", ref ids);
            _known.Clear();
            if (ids != null)
                foreach (int id in ids)
                    if (System.Enum.IsDefined(typeof(SpellId), id))
                        _known.Add((SpellId)id);
        }

        // Records a spell as known — idempotent, safe to call on every cast
        // (Requirement 16: a correct formula teaches itself, known or not).
        public static void LearnSpell(SpellId id) => _known.Add(id);

        // Stub hooks for the other three learning sources — Phases 7/9/13 call
        // these once their own systems exist; wiring them here now means those
        // later phases only need to call LearnSpell/TryUnlock, not touch this
        // file again.
        public static void LearnSpellFromRuin(SpellId id) => LearnSpell(id);
        public static void LearnSpellFromTower(SpellId id) => LearnSpell(id);
        public static void GrantStartingSpell(SpellId id) { TryUnlock(free: true); LearnSpell(id); }

        // Requirement 13 — spend one focus point, once, to unlock casting at
        // all. `free` is used only by the arcane character-creation grant
        // (Phase 13 stub) and the debug hook, which do not charge the point.
        public static bool TryUnlock(bool free = false)
        {
            if (_unlocked) return true;
            if (free) { _unlocked = true; return true; }

            var hero = Hero.MainHero;
            int have = 0;
            try { have = hero?.HeroDeveloper?.UnspentFocusPoints ?? 0; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (have < SpellbookMath.UnlockFocusCost)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Opening the spellbook costs {SpellbookMath.UnlockFocusCost} focus point; you have {have}.",
                    Dim));
                return false;
            }
            try { hero.HeroDeveloper.UnspentFocusPoints -= SpellbookMath.UnlockFocusCost; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            _unlocked = true;
            InformationManager.DisplayMessage(new InformationMessage(
                "The spellbook opens. Its formulas will answer a spoken gesture, now — hold Left Alt, tap the marks, and release.",
                Gold));
            return true;
        }

        // ── The book — a clean, read-only list of every formula known
        //    (Requirement 17), reusing the litany/grimoire inquiry pattern. ───
        public static void ShowSpellbook()
        {
            if (!_unlocked)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("The spellbook is closed to you.");
                sb.AppendLine($"Opening it costs {SpellbookMath.UnlockFocusCost} focus point, spent once.");
                try
                {
                    InformationManager.ShowInquiry(new InquiryData(
                        "The Spellbook",
                        sb.ToString(), true, true, "Open it", "Close",
                        () => TryUnlock(false), null), true, true);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                return;
            }

            var body = new System.Text.StringBuilder();
            if (_known.Count == 0)
            {
                body.AppendLine("You know no formulas yet.");
                body.AppendLine("Speak any correct sequence in battle — 5 to 20 marks of U/D/L/R — and it is yours from that moment on.");
            }
            else
            {
                var known = SpellbookCatalog.All.Where(d => _known.Contains(d.Id)).OrderBy(d => d.Length).ToList();
                foreach (var d in known)
                {
                    body.AppendLine($"{d.Name}   [{d.Formula}]");
                    body.AppendLine($"   {d.Description}");
                    body.AppendLine();
                }
            }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    $"The Spellbook  ({_known.Count} formula{(_known.Count != 1 ? "s" : "")} known)",
                    body.ToString(), true, false, "Close", "",
                    () => { }, null), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Debug (Requirement 26) ───────────────────────────────────────────
        public static void DebugUnlockAll()
        {
            TryUnlock(free: true);
            foreach (var d in SpellbookCatalog.All) LearnSpell(d.Id);
        }

        private static readonly Color Gold = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color Dim  = new Color(0.7f, 0.65f, 0.55f);
    }
}
