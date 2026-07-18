// =============================================================================
// THE DARKEST NIGHT — Spellbook/RuneCatalog.cs
//
// THE SCRIVED WORD — the 36 runes of the old craft (RUNE_MAGIC_PLAN.md §2).
// Pure data, no TaleWorlds types, mirroring SpellbookCatalog's shape.
//
// A rune is exactly THREE marks of U/D/L/R (keyboard W/A/S/D → U/L/D/R). Of the
// 64 possible triplets, 36 are real runes — sparse enough that discovery is a
// hunt, large enough that the hunt lasts (asserted by PureLogicTests). Each rune
// carries a grammatical ROLE (Matter / Form / Manner / Coda) that the resolver
// (RuneSequenceMath) reads like a sentence, plus a solo working it performs when
// drawn alone.
//
// This file only says WHAT the runes are; RuneSequenceMath says how a drawn
// SEQUENCE resolves, and (Phase 2) RuneEffects says how a resolved working acts
// on the world. The legacy SpellbookCatalog / SpellId is untouched — it stays
// the catalog of BOUND workings for wands, the Chosen's Rod, and NPC repertoires
// (RUNE_MAGIC_PLAN.md §8); RunesForLegacySpell below maps each to its runes for
// save migration.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheDarkestNight
{
    // One grammatical role per rune. Matter = what the working is made of; Form =
    // what it is poured into (at most one per binding); Manner = how it behaves
    // (freely stackable); Coda = a self-contained working that stacks its solo
    // effect. See RUNE_MAGIC_PLAN.md §3.
    public enum RuneRole { Matter, Form, Manner, Coda }

    // The eight mutually-exclusive Forms (capped at 8 forever — every future rune
    // must be effect-side, never a new Form).
    public enum RuneForm { None, LongMark, Bar, Calling, Snare, Brand, Husk, Ring, Rain }

    public enum RuneId
    {
        // Matter (5)
        Cinder, Tide, Stone, Gale, Wyrd,
        // Forms (8)
        LongMark, Bar, Calling, Snare, Brand, Husk, Ring, Rain,
        // Manners (8)
        Echo, Vigil, NightMark, Price, Chain, Mirror, Still, Gift,
        // Codas (15)
        Circle, Fetter, Shroud, Sundering, Lamp, Mending, GraveMark, Hush,
        Stride, Rot, Beacon, Maw, Sentry, Hollow, Anchor,
    }

    public struct RuneDef
    {
        public RuneId    Id;
        public string    Name;
        public string    Triplet;   // exactly three of U/D/L/R
        public string    Meaning;
        public string    Description;
        public RuneRole  Role;
        public RuneForm  Form;                 // Form runes only, else None
        public MagicElement Element;           // Matter runes only
        public bool      IsElement;            // true for the five Matter runes
    }

    public static class RuneCatalog
    {
        public const int RuneLength       = 3;   // every rune is exactly three marks
        public const int MaxSequenceRunes = 7;   // 21 marks — RUNE_MAGIC_PLAN.md §4
        public const int FormCount        = 8;   // capped forever — §2 completeness rule

        private static RuneDef M(RuneId id, string name, string triplet, string meaning, MagicElement el, string desc)
            => new RuneDef { Id = id, Name = name, Triplet = triplet, Meaning = meaning, Role = RuneRole.Matter,
                             Form = RuneForm.None, Element = el, IsElement = true, Description = desc };

        private static RuneDef F(RuneId id, string name, string triplet, string meaning, RuneForm form, string desc)
            => new RuneDef { Id = id, Name = name, Triplet = triplet, Meaning = meaning, Role = RuneRole.Form,
                             Form = form, IsElement = false, Description = desc };

        private static RuneDef Mn(RuneId id, string name, string triplet, string meaning, string desc)
            => new RuneDef { Id = id, Name = name, Triplet = triplet, Meaning = meaning, Role = RuneRole.Manner,
                             Form = RuneForm.None, IsElement = false, Description = desc };

        private static RuneDef C(RuneId id, string name, string triplet, string meaning, string desc)
            => new RuneDef { Id = id, Name = name, Triplet = triplet, Meaning = meaning, Role = RuneRole.Coda,
                             Form = RuneForm.None, IsElement = false, Description = desc };

        public static readonly RuneDef[] All =
        {
            // ── Matter (5) — the pure strokes ─────────────────────────────────
            M(RuneId.Cinder, "Cinder", "DDD", "Fire",   MagicElement.Fire,   "A close burst of flame ahead — moderate hurt, and it ignites."),
            M(RuneId.Tide,   "Tide",   "LLL", "Water",  MagicElement.Water,  "A slowing wave that breaks over those ahead."),
            M(RuneId.Stone,  "Stone",  "RRR", "Earth",  MagicElement.Earth,  "A short fan of erupting rock that roots what it strikes."),
            M(RuneId.Gale,   "Gale",   "URU", "Wind",   MagicElement.Wind,   "A forward gust that drives foes back."),
            M(RuneId.Wyrd,   "Wyrd",   "UDU", "Spirit", MagicElement.Spirit, "The will spoken aloud — it heartens the nearest ally."),

            // ── Forms (8) — the turned strokes ────────────────────────────────
            F(RuneId.LongMark, "the Long Mark", "UUU", "Reach",  RuneForm.LongMark, "An arrow of force from the fingertips — little hurt, long reach."),
            F(RuneId.Bar,      "the Bar",       "LRL", "Wall",   RuneForm.Bar,      "A low warding line drawn across the ground ahead."),
            F(RuneId.Calling,  "the Calling",   "DUD", "Summon", RuneForm.Calling,  "A weak elemental is called to fight beside you."),
            F(RuneId.Snare,    "the Snare",     "LDR", "Trap",   RuneForm.Snare,    "A bare snare that trips the first foe to cross it."),
            F(RuneId.Brand,    "the Brand",     "URD", "Imbue",  RuneForm.Brand,    "A faint gleam along the blade — alone, nothing more."),
            F(RuneId.Husk,     "the Husk",      "RDR", "Mantle", RuneForm.Husk,     "A brittle plain mantle worn over the skin — a small ward."),
            F(RuneId.Ring,     "the Ring",      "RLL", "Nova",   RuneForm.Ring,     "A bare shockwave that staggers those close around you."),
            F(RuneId.Rain,     "the Rain",      "DUR", "Fall",   RuneForm.Rain,     "A brief, plain drizzle over the ground ahead."),

            // ── Manners (8) — how the working behaves ─────────────────────────
            Mn(RuneId.Echo,      "the Echo",       "RLR", "Multiply", "Alone, it finds no voice to double — a harmless fizzle."),
            Mn(RuneId.Vigil,     "the Vigil",      "ULD", "Linger",   "Alone, it keeps watch over nothing — a harmless fizzle."),
            Mn(RuneId.NightMark, "the Night Mark", "DLR", "Dark",     "A breath of false night: foes falter, but demons quicken."),
            Mn(RuneId.Price,     "the Price",      "DUL", "Blood",    "Alone, you bleed for nothing — a lesson written in your own hand."),
            Mn(RuneId.Chain,     "the Chain",      "RLD", "Arc",      "A static snap at the nearest foe — trivial, alone."),
            Mn(RuneId.Mirror,    "the Mirror",     "LUL", "Turn",     "A glint, nothing more — alone. Bound, it arms a brief counter."),
            Mn(RuneId.Still,     "the Still",      "LLU", "Quench",   "A held breath — alone. Bound, it dispels."),
            Mn(RuneId.Gift,      "the Gift",       "ULR", "Bestow",   "An open, empty hand — alone. Bound, it gives the working away."),

            // ── Codas (15) — self-contained workings ──────────────────────────
            C(RuneId.Circle,    "the Circle",     "ULU", "Ward",    "A small ward drawn about the caster."),
            C(RuneId.Fetter,    "the Fetter",     "DLD", "Bind",    "Roots the nearest foe a short while."),
            C(RuneId.Shroud,    "the Shroud",     "LDL", "Veil",    "A thin ash-veil settles around the caster."),
            C(RuneId.Sundering, "the Sundering",  "DUU", "Banish",  "A pale flash that stings every demon close by."),
            C(RuneId.Lamp,      "the Lamp",       "UUD", "Light",   "A lingering lamp the dark cannot bear."),
            C(RuneId.Mending,   "the Mending",    "UDD", "Mend",    "A modest mending of the caster's own wounds."),
            C(RuneId.GraveMark, "the Grave Mark", "DDU", "Fear",    "Nearby foes falter — a small breaking of nerve."),
            C(RuneId.Hush,      "the Hush",       "LLD", "Silence", "A short hush — the courage of nearby foes dips."),
            C(RuneId.Stride,    "the Stride",     "RUR", "Step",    "A short wraithstep carries the caster forward."),
            C(RuneId.Rot,       "the Rot",        "DRD", "Curse",   "A gnawing curse settles on the nearest foe."),
            C(RuneId.Beacon,    "the Beacon",     "RRU", "Rally",   "A hearth-glow that lifts the courage of every soul near it."),
            C(RuneId.Maw,       "the Maw",        "DDL", "Hunger",  "A dark draw: it wounds the nearest foe and mends half of it into you."),
            C(RuneId.Sentry,    "the Sentry",     "RLU", "Dawn",    "A standing watch-light that sears demons within its ring."),
            C(RuneId.Hollow,    "the Hollow",     "DRR", "Decoy",   "A phantom of the caster steps out and draws foes before folding to ash."),
            C(RuneId.Anchor,    "the Anchor",     "LRR", "Hold",    "The caster stands rooted by choice — proof against knockback and stagger."),
        };

        // ── Lookups ──────────────────────────────────────────────────────────
        private static readonly Dictionary<string, RuneDef> _byTriplet =
            All.ToDictionary(r => r.Triplet, r => r, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<RuneId, RuneDef> _byId =
            All.ToDictionary(r => r.Id, r => r);

        public static bool TryGetByTriplet(string triplet, out RuneDef def)
            => _byTriplet.TryGetValue(triplet ?? "", out def);

        public static RuneDef Get(RuneId id) => _byId[id];

        public static bool IsRealTriplet(string triplet)
            => triplet != null && _byTriplet.ContainsKey(triplet);

        // ── Starter pair (RUNE_MAGIC_PLAN.md §7 — "a stranger's book") ─────────
        // The first is always an element; the second is another element or one of
        // a small pool of simple, safe first lessons. The dark/blood runes are
        // never in a stranger's opening pages.
        public static readonly RuneId[] ElementRunes =
            { RuneId.Cinder, RuneId.Tide, RuneId.Stone, RuneId.Gale, RuneId.Wyrd };

        public static readonly RuneId[] StarterEligibleSecond =
            { RuneId.Cinder, RuneId.Tide, RuneId.Stone, RuneId.Gale, RuneId.Wyrd,
              RuneId.LongMark, RuneId.Bar, RuneId.Circle, RuneId.Mending, RuneId.Lamp };

        // Returns (first, second): first always an element, second element-or-safe,
        // never equal. Deterministic given the Random passed. Every possible pair
        // is castable on day one (element alone works; element+element fuses;
        // element+form shapes) — asserted by PureLogicTests.
        public static (RuneId first, RuneId second) PickStarterPair(Random rng)
        {
            RuneId first = ElementRunes[rng.Next(ElementRunes.Length)];
            RuneId second;
            int guard = 0;
            do { second = StarterEligibleSecond[rng.Next(StarterEligibleSecond.Length)]; }
            while (second == first && ++guard < 32);
            if (second == first) // pathological — pick any other element
                second = ElementRunes.First(e => e != first);
            return (first, second);
        }

        // ── Legacy migration map (RUNE_MAGIC_PLAN.md §7) ──────────────────────
        // Every old SpellId maps to the runes that compose an equivalent working,
        // so a v0.8 save's known formulas carry over as known runes. Every SpellId
        // must map to only real, known runes — asserted by PureLogicTests.
        public static IReadOnlyList<RuneId> RunesForLegacySpell(SpellId id)
        {
            switch (id)
            {
                // The five elements and their walls
                case SpellId.Fireball:            return new[] { RuneId.Cinder, RuneId.LongMark };
                case SpellId.Firewall:            return new[] { RuneId.Cinder, RuneId.Bar };
                case SpellId.GalesCall:           return new[] { RuneId.Gale };
                case SpellId.WindwardVeil:        return new[] { RuneId.Gale, RuneId.Bar };
                case SpellId.StonerootStrike:     return new[] { RuneId.Stone };
                case SpellId.Thornwall:           return new[] { RuneId.Stone, RuneId.Bar };
                case SpellId.TorrentsEdge:        return new[] { RuneId.Tide };
                case SpellId.Mistwall:            return new[] { RuneId.Tide, RuneId.Bar };
                case SpellId.WailingNova:         return new[] { RuneId.Wyrd, RuneId.Ring };
                case SpellId.WardOfWhispers:      return new[] { RuneId.Wyrd, RuneId.Bar };

                // The Unbindings — the amplified element (the resolver reads repeats)
                case SpellId.FirstFlameRemembered: return new[] { RuneId.Cinder };
                case SpellId.OnTheWingsOfTheGale:  return new[] { RuneId.Gale };
                case SpellId.MountainsWrath:       return new[] { RuneId.Stone };
                case SpellId.TheWeepingSky:        return new[] { RuneId.Tide, RuneId.Rain };
                case SpellId.TheBentKnee:          return new[] { RuneId.Wyrd, RuneId.Calling };

                // Demon-facing
                case SpellId.SummonDemon:          return new[] { RuneId.NightMark, RuneId.Calling };
                case SpellId.BanishDemons:         return new[] { RuneId.Sundering };
                case SpellId.Light:                return new[] { RuneId.Lamp };

                // Wards / curses / veils / callings
                case SpellId.SparkOfEmbers:        return new[] { RuneId.Cinder };
                case SpellId.VeilOfAsh:            return new[] { RuneId.Shroud };
                case SpellId.Frostbind:            return new[] { RuneId.Tide, RuneId.Fetter };
                case SpellId.Wraithstep:           return new[] { RuneId.Stride };
                case SpellId.CallingOfEmbers:      return new[] { RuneId.Cinder, RuneId.Calling };
                case SpellId.CursedGround:         return new[] { RuneId.Rot };
                case SpellId.SilentVeil:           return new[] { RuneId.Hush };
                case SpellId.BindingChant:         return new[] { RuneId.Fetter };
                case SpellId.HollowCalling:        return new[] { RuneId.Hollow };
                case SpellId.EmberWard:            return new[] { RuneId.Circle };
                case SpellId.SunderingCry:         return new[] { RuneId.Sundering };
                case SpellId.GraspingRoots:        return new[] { RuneId.Stone, RuneId.Fetter };
                case SpellId.Tidebreaker:          return new[] { RuneId.Tide };
                case SpellId.TheLongSilence:       return new[] { RuneId.Hush };
                case SpellId.BonewindCurse:        return new[] { RuneId.Rot };
                case SpellId.WardingSigil:         return new[] { RuneId.Circle };
                case SpellId.CallersBane:          return new[] { RuneId.Sundering };
                case SpellId.HearthlightBeacon:    return new[] { RuneId.Beacon };
                case SpellId.GraveChant:           return new[] { RuneId.GraveMark };
                case SpellId.TheAshenCalling:      return new[] { RuneId.Calling };
                case SpellId.WidowsVeil:           return new[] { RuneId.Shroud };
                case SpellId.TheLongWard:          return new[] { RuneId.Circle, RuneId.Vigil };
            }
            return new RuneId[0];
        }
    }
}
