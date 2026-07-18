// =============================================================================
// THE DARKEST NIGHT — Spellbook/RuneSequenceMath.cs
//
// THE BINDING GRAMMAR (RUNE_MAGIC_PLAN.md §3) — the PURE resolver that reads an
// ordered list of drawn runes as a sentence and returns a ResolvedWorking (what
// the effect layer should produce) or a Malformed result (→ fizzle + spellburn).
// No TaleWorlds types; fully covered by PureLogicTests.
//
// Order of runes within the sequence does NOT matter — the resolver reads a
// multiset, which keeps it pure, testable, and forgiving. It reuses the already
// battle-proven element fusion table (ElementComboMath.TryFuse) so every fusion,
// command, and wall the element system tuned is inherited for free.
//
// This file decides WHAT a binding resolves to; RuneEffects (Phase 2) turns a
// ResolvedWorking into game effects. Nothing here touches the world.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheDarkestNight
{
    // What the working's matter is. Single = one base element; Fusion = two
    // (ElementComboMath); Command = element + Wyrd; Triad = three elements;
    // Unbound = all four; Wyrd = the will alone; None = no matter (form/coda only).
    public enum MatterKind { None, Single, Fusion, Command, Triad, Unbound, Wyrd }

    public struct ResolvedWorking
    {
        public bool         Malformed;
        public bool         Harmless;      // malformed but composes nothing to punish —
                                           // valid runes that simply do nothing together
                                           // (a lone Echo, an inert manner): fizzle, no burn
        public string       Reason;        // malformed only — the fizzle line

        public MatterKind   Matter;
        public MagicElement Element;       // meaningful for Single/Fusion/Command
        public string       TriadName;     // Triad only

        public RuneForm     Form;
        public float        Power;         // amplification scalar (≥1)
        public int          RuneCount;
        public float        StrainChance;  // before Intellect reduction, ≥0

        // Manner flags
        public int  EchoCount;
        public bool Chain, Vigil, Price, NightMark, Mirror, Still, Gift;

        public List<RuneId> Codas;
        public string       Name;          // composed, player-facing

        public static ResolvedWorking Fail(string reason)
            => new ResolvedWorking { Malformed = true, Reason = reason, Codas = new List<RuneId>() };
    }

    public static class RuneSequenceMath
    {
        // Repetition amplifies: 1×, 1.75×, 2.4×, 3.0×, capped at 4 repeats.
        public static float AmplifyScale(int n)
        {
            if (n <= 1) return 1f;
            if (n == 2) return 1.75f;
            if (n == 3) return 2.4f;
            return 3.0f; // 4+
        }

        // Strain: every rune past the third adds inherent burn risk to even a
        // perfect binding. (runeCount − 3) × 0.04, floored at 0 (before Intellect).
        public static float StrainChance(int runeCount)
            => Math.Max(0f, (runeCount - 3) * 0.04f);

        // ── Chunking (input validation half of rule 1) ─────────────────────────
        // Splits a U/D/L/R mark string into runes. A trailing 1–2 marks, an empty
        // string, or any non-rune triplet ⇒ malformed. On success, `runes` holds
        // the drawn rune ids in order (used for discovery even if the binding later
        // resolves malformed).
        public static bool TryChunk(string marks, out List<RuneId> runes, out string reason)
        {
            runes = new List<RuneId>();
            reason = null;
            if (string.IsNullOrEmpty(marks)) { reason = "nothing was drawn."; return false; }
            if (marks.Length % RuneCatalog.RuneLength != 0)
            {
                reason = "the mark breaks off unfinished.";
                return false;
            }
            for (int i = 0; i < marks.Length; i += RuneCatalog.RuneLength)
            {
                string triplet = marks.Substring(i, RuneCatalog.RuneLength);
                if (!RuneCatalog.TryGetByTriplet(triplet, out RuneDef def))
                {
                    reason = "a mark answers nothing.";
                    return false;
                }
                runes.Add(def.Id);
            }
            return true;
        }

        // ── Resolution ─────────────────────────────────────────────────────────
        public static ResolvedWorking Resolve(IReadOnlyList<RuneId> runes)
        {
            if (runes == null || runes.Count == 0)
                return ResolvedWorking.Fail("nothing was drawn.");
            if (runes.Count > RuneCatalog.MaxSequenceRunes)
                return ResolvedWorking.Fail("the binding is too long to hold.");

            var defs = runes.Select(RuneCatalog.Get).ToList();

            // ── Forms (at most one) ────────────────────────────────────────────
            var forms = defs.Where(d => d.Role == RuneRole.Form).Select(d => d.Form).Distinct().ToList();
            if (forms.Count > 1)
                return ResolvedWorking.Fail("two vessels cannot hold one working.");
            RuneForm form = forms.Count == 1 ? forms[0] : RuneForm.None;

            // ── Manners ────────────────────────────────────────────────────────
            var mannerIds = defs.Where(d => d.Role == RuneRole.Manner).Select(d => d.Id).ToList();
            int echoCount = mannerIds.Count(id => id == RuneId.Echo);
            bool chain  = mannerIds.Contains(RuneId.Chain);
            bool vigil  = mannerIds.Contains(RuneId.Vigil);
            bool price  = mannerIds.Contains(RuneId.Price);
            bool night  = mannerIds.Contains(RuneId.NightMark);
            bool mirror = mannerIds.Contains(RuneId.Mirror);
            bool still  = mannerIds.Contains(RuneId.Still);
            bool gift   = mannerIds.Contains(RuneId.Gift);

            var codas = defs.Where(d => d.Role == RuneRole.Coda).Select(d => d.Id).ToList();

            // ── Declared contradictions (rule 5) ───────────────────────────────
            if (night && codas.Contains(RuneId.Lamp))
                return ResolvedWorking.Fail("the lamp gutters against the dark.");
            if (mirror && gift)
                return ResolvedWorking.Fail("a counter cannot be given away.");
            if (still && vigil)
                return ResolvedWorking.Fail("a working cannot both quench and linger.");

            // ── Matter ─────────────────────────────────────────────────────────
            var elementRunes = defs.Where(d => d.IsElement).ToList();
            var distinct = elementRunes.Select(d => d.Element).Distinct().ToList();
            bool hasWyrd = distinct.Contains(MagicElement.Spirit);
            var nonWyrd = distinct.Where(e => e != MagicElement.Spirit).ToList();

            var r = new ResolvedWorking { Codas = codas, Form = form, RuneCount = runes.Count };
            r.EchoCount = echoCount; r.Chain = chain; r.Vigil = vigil; r.Price = price;
            r.NightMark = night; r.Mirror = mirror; r.Still = still; r.Gift = gift;

            // Amplification: the strongest repeated element rune drives Power.
            r.Power = MaxElementAmplify(elementRunes);
            r.StrainChance = StrainChance(runes.Count);

            if (distinct.Count == 0)
            {
                r.Matter = MatterKind.None; // form / coda only — valid
            }
            else if (hasWyrd && nonWyrd.Count >= 2)
            {
                return ResolvedWorking.Fail("the weave tears — will cannot share matter twice over.");
            }
            else if (distinct.Count == 1)
            {
                r.Matter = distinct[0] == MagicElement.Spirit ? MatterKind.Wyrd : MatterKind.Single;
                r.Element = distinct[0];
            }
            else if (distinct.Count == 2)
            {
                MagicElement? fused = ElementComboMath.TryFuse(distinct[0], distinct[1]);
                if (fused == null)
                    return ResolvedWorking.Fail("the matters will not marry.");
                r.Element = fused.Value;
                r.Matter = hasWyrd ? MatterKind.Command : MatterKind.Fusion;
            }
            else if (distinct.Count == 3)
            {
                if (hasWyrd) return ResolvedWorking.Fail("the weave tears — will cannot share matter twice over.");
                r.Matter = MatterKind.Triad;
                r.TriadName = TriadNameFor(nonWyrd);
            }
            else // 4 distinct
            {
                if (hasWyrd) return ResolvedWorking.Fail("the weave tears — will cannot share matter twice over.");
                r.Matter = MatterKind.Unbound;
            }

            // ── Form legality against matter (rule 4 / completeness) ───────────
            if (form != RuneForm.None)
            {
                if (r.Matter == MatterKind.Command)
                    return ResolvedWorking.Fail("a command cannot be poured into a vessel.");
                if (r.Matter == MatterKind.Unbound)
                    return ResolvedWorking.Fail("what is unbound cannot be shaped.");
            }

            // A binding of pure manners with no matter, form, or coda does nothing.
            // These are REAL runes that simply compose nothing together, so it is a
            // harmless fizzle — no spellburn (the input layer reads Harmless).
            if (r.Matter == MatterKind.None && form == RuneForm.None && codas.Count == 0)
            {
                var harmless = ResolvedWorking.Fail("the marks find nothing to work upon.");
                harmless.Harmless = true;
                return harmless;
            }

            r.Name = ComposeName(r);
            return r;
        }

        private static float MaxElementAmplify(List<RuneDef> elementRunes)
        {
            if (elementRunes.Count == 0) return 1f;
            float best = 1f;
            foreach (var group in elementRunes.GroupBy(d => d.Element))
                best = Math.Max(best, AmplifyScale(group.Count()));
            return best;
        }

        // Fire+Wind+Water = Tempest; Fire+Wind+Earth = Eruption;
        // Fire+Water+Earth = Seething; Wind+Water+Earth = Avalanche.
        private static string TriadNameFor(List<MagicElement> three)
        {
            var s = new HashSet<MagicElement>(three);
            bool fire = s.Contains(MagicElement.Fire), wind = s.Contains(MagicElement.Wind);
            bool water = s.Contains(MagicElement.Water), earth = s.Contains(MagicElement.Earth);
            if (fire && wind && water) return "the Tempest";
            if (fire && wind && earth) return "the Eruption";
            if (fire && water && earth) return "the Seething";
            if (wind && water && earth) return "the Avalanche";
            return "the Triad";
        }

        // ── Name composition (RUNE_MAGIC_PLAN.md §3) ───────────────────────────
        public static string ComposeName(ResolvedWorking r)
        {
            string matter = MatterName(r);
            string form = FormWord(r.Form);

            string prefix = "";
            if (r.Power >= 2.9f) prefix = "Thrice-Written ";
            else if (r.Power >= 2.3f) prefix = "Twice-Written ";
            else if (r.Power >= 1.7f) prefix = "Twice-Written ";

            string core;
            if (!string.IsNullOrEmpty(matter) && !string.IsNullOrEmpty(form))
                core = matter + " " + form;
            else if (!string.IsNullOrEmpty(form))
                core = form;
            else if (!string.IsNullOrEmpty(matter))
                core = matter + (r.Matter == MatterKind.Single || r.Matter == MatterKind.Fusion ? " Blast" : "");
            else
                core = CodaName(r);

            string qualifiers = MannerQualifiers(r);
            return (prefix + core + qualifiers).Trim();
        }

        private static string MatterName(ResolvedWorking r)
        {
            switch (r.Matter)
            {
                case MatterKind.Single:  return BaseElementName(r.Element);
                case MatterKind.Fusion:  return ElementComboMath.ElementName(r.Element);
                case MatterKind.Command: return ElementComboMath.ElementName(r.Element);
                case MatterKind.Triad:   return r.TriadName;
                case MatterKind.Unbound: return "the Unbound Weave";
                case MatterKind.Wyrd:    return "Wyrd";
                default:                 return "";
            }
        }

        public static string BaseElementName(MagicElement e)
        {
            switch (e)
            {
                case MagicElement.Fire:   return "Fire";
                case MagicElement.Wind:   return "Wind";
                case MagicElement.Earth:  return "Earth";
                case MagicElement.Water:  return "Water";
                case MagicElement.Spirit: return "Wyrd";
                default:                  return ElementComboMath.ElementName(e);
            }
        }

        private static string FormWord(RuneForm form)
        {
            switch (form)
            {
                case RuneForm.LongMark: return "Bolt";
                case RuneForm.Bar:      return "Wall";
                case RuneForm.Calling:  return "Calling";
                case RuneForm.Snare:    return "Snare";
                case RuneForm.Brand:    return "Brand";
                case RuneForm.Husk:     return "Husk";
                case RuneForm.Ring:     return "Nova";
                case RuneForm.Rain:     return "Rain";
                default:                return "";
            }
        }

        private static string CodaName(ResolvedWorking r)
        {
            if (r.Codas != null && r.Codas.Count > 0)
                return RuneCatalog.Get(r.Codas[0]).Name.Replace("the ", "The ");
            return "A Whispered Mark";
        }

        private static string MannerQualifiers(ResolvedWorking r)
        {
            var parts = new List<string>();
            if (r.EchoCount > 0) parts.Add(r.EchoCount > 1 ? "Manifold" : "Echoed");
            if (r.Chain)     parts.Add("Chained");
            if (r.NightMark) parts.Add("Darkened");
            if (r.Mirror)    parts.Add("Turned");
            if (r.Price)     parts.Add("Blood-Bought");
            if (r.Vigil)     parts.Add("Lingering");
            if (r.Gift)      parts.Add("Given");
            if (parts.Count == 0) return "";
            return " (" + string.Join(", ", parts) + ")";
        }
    }
}
