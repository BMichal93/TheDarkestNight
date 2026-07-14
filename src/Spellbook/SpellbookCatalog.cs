// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellbookCatalog.cs
//
// THE SPOKEN FORMULAS. Requirement 16/17: every spell is a directional
// (U/D/L/R) gesture, 5-20 marks long, catalogued here with its name,
// description, and formula. Pure data — no TaleWorlds types — mirroring the
// MiracleCatalog pattern exactly.
//
// Casting is resolved by SpellbookInputHandler (the gesture) and dispatched
// by SpellbookEffects (the game-world effect); this file only says WHAT
// exists and HOW it is spoken, never HOW it acts on the world.
//
// The formula space is deliberately sparse: most of the 4^N possible strings
// at any length N answer nothing at all — see PureLogicTests for the explicit
// coverage assertion (Requirement 16's "the space must stay sparse").
// =============================================================================

using System.Collections.Generic;
using System.Linq;

namespace AshAndEmber
{
    public enum SpellId
    {
        // ── The five elements, replicated as spoken formulas ────────────────
        Fireball, Firewall,
        GalesCall, WindwardVeil,
        StonerootStrike, Thornwall,
        TorrentsEdge, Mistwall,
        WailingNova, WardOfWhispers,

        // ── The Unbindings, as the longest formulas known ───────────────────
        FirstFlameRemembered,   // Fire
        OnTheWingsOfTheGale,    // Wind
        MountainsWrath,         // Earth
        TheWeepingSky,          // Water
        TheBentKnee,            // Spirit

        // ── Demon-facing workings ────────────────────────────────────────────
        SummonDemon,
        BanishDemons,
        Light,

        // ── Invented — wards, curses, veils, callings ───────────────────────
        SparkOfEmbers, VeilOfAsh,
        Frostbind, Wraithstep, CallingOfEmbers,
        CursedGround, SilentVeil, BindingChant,
        HollowCalling, EmberWard, SunderingCry,
        GraspingRoots, Tidebreaker,
        TheLongSilence, BonewindCurse,
        WardingSigil, CallersBane,
        HearthlightBeacon, GraveChant,
        TheAshenCalling, WidowsVeil,
        TheLongWard,
    }

    public struct SpellDef
    {
        public SpellId Id;
        public string  Name;
        public string  Formula;      // U/D/L/R, 5-20 characters
        public string  Description;  // player-facing flavour + effect summary

        public int Length => Formula?.Length ?? 0;
    }

    public static class SpellbookCatalog
    {
        public const int MinFormulaLength = 5;
        public const int MaxFormulaLength = 20;

        private static readonly List<SpellDef> _defs = new List<SpellDef>
        {
            // ── Elements — attack / wall pairs (5-7 marks) ──────────────────
            new SpellDef { Id = SpellId.Fireball, Name = "Fireball", Formula = "UDURD",
                Description = "A bolt of living fire, hurled forward — it bursts on the first foe it reaches." },
            new SpellDef { Id = SpellId.Firewall, Name = "Firewall", Formula = "UDULR",
                Description = "A standing curtain of flame raised ahead of you — those who hold the line burn for it." },
            new SpellDef { Id = SpellId.GalesCall, Name = "Gale's Call", Formula = "LRLRUD",
                Description = "A driven wind that hurls foes ahead of it and slows all it touches." },
            new SpellDef { Id = SpellId.WindwardVeil, Name = "Windward Veil", Formula = "LRLRUDU",
                Description = "A wall of turning wind — it scatters flung stone and drinks the arrows loosed against it." },
            new SpellDef { Id = SpellId.StonerootStrike, Name = "Stoneroot Strike", Formula = "DUDUDL",
                Description = "A short, almost-melee fan of erupting rock — heavy damage, and the roots hold what they catch." },
            new SpellDef { Id = SpellId.Thornwall, Name = "Thornwall", Formula = "DUDUDLR",
                Description = "A rampart of raised stone — arrows die against it, and a broken wave breaks harder still." },
            new SpellDef { Id = SpellId.TorrentsEdge, Name = "Torrent's Edge", Formula = "RLRLUD",
                Description = "A slowing wave loosed ahead of you — it drags at the foe and douses any fire in its path." },
            new SpellDef { Id = SpellId.Mistwall, Name = "Mistwall", Formula = "RLRLUDR",
                Description = "A standing bank of mist — it quenches fire to steam and drinks the force of the wind." },
            new SpellDef { Id = SpellId.WailingNova, Name = "Wailing Nova", Formula = "ULDRUL",
                Description = "A ring of dread breaking outward from you — men and horses alike falter and bolt." },
            new SpellDef { Id = SpellId.WardOfWhispers, Name = "Ward of Whispers", Formula = "ULDRULD",
                Description = "A wall of unseen voices — it heartens your own line and mends them a little." },

            // ── The Unbindings — the longest of the spoken formulas ─────────
            new SpellDef { Id = SpellId.FirstFlameRemembered, Name = "The First Flame Remembered", Formula = "UDRLUDRLUDRL",
                Description = "The Fire unbound: a nova of flame and ignition breaking outward from you." },
            new SpellDef { Id = SpellId.OnTheWingsOfTheGale, Name = "On the Wings of the Gale", Formula = "LRUDLRUDLRUD",
                Description = "The Wind unbound: for a time, you are carried — steer by your own gaze." },
            new SpellDef { Id = SpellId.MountainsWrath, Name = "The Mountain's Wrath", Formula = "DLURDLURDLUR",
                Description = "The Earth unbound: the ground heaves in a ring around you, and every foe within it is hurled down." },
            new SpellDef { Id = SpellId.TheWeepingSky, Name = "The Weeping Sky", Formula = "RULDRULDRULD",
                Description = "The Water unbound: a standing rain that quenches fire, mires horses, and soaks every bowstring beneath it." },
            new SpellDef { Id = SpellId.TheBentKnee, Name = "The Bent Knee", Formula = "ULRDULRDULRD",
                Description = "The Spirit unbound: a nearby will bends to your own and fights at your side, for a time." },

            // ── Demon-facing workings ────────────────────────────────────────
            new SpellDef { Id = SpellId.SummonDemon, Name = "Summon Demon", Formula = "UDLRUDLRUDLRUDLRUDLR",
                Description = "The longest and darkest of the spoken formulas — it tears one of the Night's own creatures loose to fight at your side." },
            new SpellDef { Id = SpellId.BanishDemons, Name = "Banish Demons", Formula = "DULRDULRDULRDULRDULR",
                Description = "A blast of pale light that sears every demon on the field, whatever side it stands on." },
            new SpellDef { Id = SpellId.Light, Name = "Light", Formula = "UUDDL",
                Description = "A lingering lamp of clean light — the dark that clings to demons cannot bear to stand near it." },

            // ── Invented — wards, curses, veils, callings ───────────────────
            new SpellDef { Id = SpellId.SparkOfEmbers, Name = "Spark of Embers", Formula = "LRUDL",
                Description = "A small kindling of the flame turned inward — a modest mending of your own wounds." },
            new SpellDef { Id = SpellId.VeilOfAsh, Name = "Veil of Ash", Formula = "RLDUR",
                Description = "A curtain of drifting ash thrown up around you — a moment's cover, thin but real." },
            new SpellDef { Id = SpellId.Frostbind, Name = "Frostbind", Formula = "DLURDL",
                Description = "A single foe's feet turn to lead — rooted, for a short while, wherever they stand." },
            new SpellDef { Id = SpellId.Wraithstep, Name = "Wraithstep", Formula = "LDRULD",
                Description = "The ground blurs and you are elsewhere on it — a short, sudden step no eye quite follows." },
            new SpellDef { Id = SpellId.CallingOfEmbers, Name = "Calling of Embers", Formula = "UDLRUL",
                Description = "The fire answers a quiet call and closes a wound before it can worsen." },
            new SpellDef { Id = SpellId.CursedGround, Name = "Cursed Ground", Formula = "DRULDRU",
                Description = "The earth ahead of you turns foul — those who cross it are struck and slowed." },
            new SpellDef { Id = SpellId.SilentVeil, Name = "Silent Veil", Formula = "LURDLUR",
                Description = "Your footfall and your shadow both grow quiet — a short, uneasy hush follows you." },
            new SpellDef { Id = SpellId.BindingChant, Name = "Binding Chant", Formula = "RDLURDL",
                Description = "A longer, surer working than Frostbind — the mark it catches does not easily break free." },
            new SpellDef { Id = SpellId.HollowCalling, Name = "Hollow Calling", Formula = "UDLRUDLR",
                Description = "A lesser dread, spoken outward — nearby foes falter, though it is no full nova." },
            new SpellDef { Id = SpellId.EmberWard, Name = "Ember Ward", Formula = "LRUDLRUD",
                Description = "A private warmth wrapped around you — armour of a kind, and a little healing besides." },
            new SpellDef { Id = SpellId.SunderingCry, Name = "Sundering Cry", Formula = "DULRDULR",
                Description = "A shout that finds the seams in a foe's guard — their next blows land harder against you unless you strike first." },
            new SpellDef { Id = SpellId.GraspingRoots, Name = "Grasping Roots", Formula = "UDLRUDLRU",
                Description = "A wider working than Stoneroot Strike — every foe ahead of you is caught and held." },
            new SpellDef { Id = SpellId.Tidebreaker, Name = "Tidebreaker", Formula = "LRUDLRUDL",
                Description = "A heavier wave than Torrent's Edge — it breaks a whole line's footing at once." },
            new SpellDef { Id = SpellId.TheLongSilence, Name = "The Long Silence", Formula = "UDLRUDLRUD",
                Description = "The air itself goes still and dull around your enemies — their shouted orders die unheard." },
            new SpellDef { Id = SpellId.BonewindCurse, Name = "Bonewind Curse", Formula = "LRUDLRUDLR",
                Description = "A creeping curse laid on a single foe — it gnaws at them long after the working is spoken." },
            new SpellDef { Id = SpellId.WardingSigil, Name = "Warding Sigil", Formula = "UDLRUDLRUDL",
                Description = "A sigil drawn in light around you — for a short while, harm turns aside more than it should." },
            new SpellDef { Id = SpellId.CallersBane, Name = "Caller's Bane", Formula = "LRUDLRUDLRU",
                Description = "A working aimed at silencing another's working — a burst that catches whoever stands nearest and unready." },
            new SpellDef { Id = SpellId.HearthlightBeacon, Name = "Hearthlight Beacon", Formula = "UDLRUDLRUDLRU",
                Description = "A greater Light — it burns longer and lifts the courage of every soul near it." },
            new SpellDef { Id = SpellId.GraveChant, Name = "Grave Chant", Formula = "LRUDLRUDLRUDL",
                Description = "A dirge sung wide across the field — the dread it carries reaches far further than a nova's ring." },
            new SpellDef { Id = SpellId.TheAshenCalling, Name = "The Ashen Calling", Formula = "UDLRUDLRUDLRUDL",
                Description = "A greater Fireball in all but name — a deeper draw on the same flame, and a harder blow for it." },
            new SpellDef { Id = SpellId.WidowsVeil, Name = "Widow's Veil", Formula = "LRUDLRUDLRUDLRU",
                Description = "A vast bank of fog rolled out across the field — a battle's worth of ground swallowed from sight." },
            new SpellDef { Id = SpellId.TheLongWard, Name = "The Long Ward", Formula = "UDLRUDLRUDLRUDLR",
                Description = "The surest ward the spoken formulas hold — a working long enough that it does not lightly fail you." },
        };

        public static IReadOnlyList<SpellDef> All => _defs;

        // Requirement 23, Step 6 — the "you studied the arcane arts" backstory
        // grant picks its two starting spells only from formulas short enough to
        // plausibly be a first lesson (<= 7 marks).
        public const int ArcaneStartMaxFormulaLength = 7;

        public static IEnumerable<SpellDef> QualifyingForArcaneStart =>
            _defs.Where(d => d.Length <= ArcaneStartMaxFormulaLength);

        public static SpellDef Get(SpellId id) => _defs.First(d => d.Id == id);

        public static bool TryGetByFormula(string formula, out SpellDef def)
        {
            def = default;
            if (string.IsNullOrEmpty(formula)) return false;
            foreach (var d in _defs)
                if (d.Formula == formula) { def = d; return true; }
            return false;
        }
    }
}
