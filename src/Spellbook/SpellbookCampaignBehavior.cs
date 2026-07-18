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
        // The player's casting path is now RUNES (RUNE_MAGIC_PLAN.md). _known
        // (legacy SpellId formulas) is kept only for save migration and for the
        // systems that still read it (wands, the Rod, NPC bound workings).
        private static readonly HashSet<RuneId> _knownRunes = new HashSet<RuneId>();

        public static bool IsUnlocked => _unlocked;
        public static bool KnowsSpell(SpellId id) => _known.Contains(id);
        public static IEnumerable<SpellId> KnownSpells => _known;

        public static bool KnowsRune(RuneId id) => _knownRunes.Contains(id);
        public static IEnumerable<RuneId> KnownRunes => _knownRunes;
        public static int KnownRuneCount => _knownRunes.Count;

        public static void ResetForNewGame()
        {
            _unlocked = false;
            _known.Clear();
            _knownRunes.Clear();
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
                if (!_unlocked || (_known.Count == 0 && _knownRunes.Count == 0)) return;
                // Never clobber another system's queued popup — losing the ask is
                // safer than losing a queued event (behaviour.md's single-slot rule).
                // Keeping the book silently is the safe default either way.
                if (MageKnowledge._deferredInquiry != null) return;

                int count = _knownRunes.Count;
                MageKnowledge._deferredInquiry = () =>
                {
                    try
                    {
                        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                            "The Book Passes",
                            "Your hands are done with the marks. Your heir now carries the book — and every "
                            + $"rune written in your years ({count} known). Or let it burn with you.",
                            new List<InquiryElement>
                            {
                                new InquiryElement("keep", "Leave them the book", null, true,
                                    "The marks pass on unbroken."),
                                new InquiryElement("burn", "Let it burn", null, true,
                                    "Every mark is forgotten."),
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
                                        $"The book passes on: {count} rune{(count != 1 ? "s" : "")} carried into the next hand.", Gold));
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
            _knownRunes.Clear();
        }

        public override void SyncData(IDataStore dataStore)
        {
            // On LOAD, start from fresh empty lists so stale static state from a
            // previously-loaded game in the same session cannot leak in (the
            // "static-leak bug class" — a save missing a key would otherwise keep
            // the ref's prior value). On SAVE, write the live sets.
            bool loading = false;
            try { loading = dataStore.IsLoading; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            var ids     = loading ? new List<int>() : _known.Select(s => (int)s).ToList();
            var runeIds = loading ? new List<int>() : _knownRunes.Select(rid => (int)rid).ToList();

            dataStore.SyncData("SPELLBOOK_Unlocked", ref _unlocked);
            dataStore.SyncData("SPELLBOOK_KnownIds", ref ids);
            dataStore.SyncData("SPELLBOOK_KnownRuneIds", ref runeIds);

            if (!loading) return;   // save path is done — never touch the live sets

            _known.Clear();
            if (ids != null)
                foreach (int id in ids)
                    if (System.Enum.IsDefined(typeof(SpellId), id))
                        _known.Add((SpellId)id);

            _knownRunes.Clear();
            if (runeIds != null)
                foreach (int id in runeIds)
                    if (System.Enum.IsDefined(typeof(RuneId), id))
                        _knownRunes.Add((RuneId)id);

            // Migration (RUNE_MAGIC_PLAN.md §7): a v0.9 save carries known FORMULAS
            // but no known runes (the SPELLBOOK_KnownRuneIds key is absent, so
            // runeIds loaded empty). Grant the runes that compose each known formula
            // so the caster keeps what they earned. Legacy ids stay in the save
            // (wands/NPC systems still read them) — never deleted.
            if (_knownRunes.Count == 0 && _known.Count > 0)
            {
                foreach (var spell in _known)
                    foreach (var rune in RuneCatalog.RunesForLegacySpell(spell))
                        _knownRunes.Add(rune);
                if (_knownRunes.Count > 0)
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Your old formulas resolve into marks: {_knownRunes.Count} rune{(_knownRunes.Count != 1 ? "s" : "")} carried over.", Gold));
            }
        }

        // Records a spell as known — idempotent. Kept for the legacy formula path
        // and the systems that still read _known; the player learns RUNES now.
        public static void LearnSpell(SpellId id) => _known.Add(id);

        // Records a rune as known — idempotent, safe to call on every draw
        // (RUNE_MAGIC_PLAN.md §9: a real mark drawn teaches itself).
        public static void LearnRune(RuneId id) => _knownRunes.Add(id);

        // Learning sources — the faction/ruin/backstory hooks now grant RUNES.
        public static void LearnSpellFromRuin(SpellId id) => LearnSpell(id);
        public static void LearnSpellFromTower(SpellId id) => LearnSpell(id);
        public static void GrantStartingSpell(SpellId id) { TryUnlock(free: true); LearnSpell(id); }

        public static void LearnRuneFromRuin(RuneId id) => LearnRune(id);
        public static void LearnRuneFromTower(RuneId id) => LearnRune(id);
        public static void GrantStartingRune(RuneId id) { TryUnlock(free: true); LearnRune(id); }

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
                "The spellbook opens. Draw the marks now — hold Left Alt, tap three of U/D/L/R for each rune, and release the binding.",
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

            // ── The Marks — every rune known ──────────────────────────────────
            body.AppendLine("— THE MARKS —");
            if (_knownRunes.Count == 0)
            {
                body.AppendLine("You know no marks yet.");
                body.AppendLine("Draw any true rune in battle — hold Left Alt, tap three of U/D/L/R, release — and it is yours from that moment on.");
            }
            else
            {
                foreach (var d in RuneCatalog.All.Where(r => _knownRunes.Contains(r.Id)))
                    body.AppendLine($"{d.Name}  [{DrawMarks(d.Triplet)}]  — {d.Meaning}: {d.Description}");
            }
            body.AppendLine();

            // ── The Craft — the grammar primer (no undiscovered runes spoiled) ─
            body.AppendLine("— THE CRAFT —");
            body.AppendLine("• A rune is three marks. Release after one to loose its bare working.");
            body.AppendLine("• Write a rune twice to deepen it; a third and fourth time deepen it further.");
            body.AppendLine("• Two elements marry into a greater working; three become a Triad; all four, the Unbound Weave.");
            body.AppendLine("• The Long Mark throws a working far; the Bar raises it into a wall; the Calling gives it a body.");
            body.AppendLine("• The longer the binding, the greater the strain — a master's gamble when the line breaks.");

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    $"The Spellbook  ({_knownRunes.Count} mark{(_knownRunes.Count != 1 ? "s" : "")} known)",
                    body.ToString(), true, false, "Close", "",
                    () => { }, null), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Renders a U/D/L/R triplet as arrows for the book display.
        private static string DrawMarks(string triplet)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in triplet ?? "")
            {
                switch (c)
                {
                    case 'U': sb.Append('↑'); break;
                    case 'D': sb.Append('↓'); break;
                    case 'L': sb.Append('←'); break;
                    case 'R': sb.Append('→'); break;
                }
            }
            return sb.ToString();
        }

        // ── Debug (Requirement 26) ───────────────────────────────────────────
        public static void DebugUnlockAll()
        {
            TryUnlock(free: true);
            foreach (var d in SpellbookCatalog.All) LearnSpell(d.Id);
            foreach (var r in RuneCatalog.All) LearnRune(r.Id);
        }

        private static readonly Color Gold = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color Dim  = new Color(0.7f, 0.65f, 0.55f);
    }
}
