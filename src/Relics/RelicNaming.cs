// =============================================================================
// THE DARKEST NIGHT — Relics/RelicNaming.cs
//
// Pure component-combinator for relic names. No TaleWorlds types (fully
// covered by PureLogicTests). Register: "Vow of the Sixth Dawn", "Cinderfang",
// "The Widow's Patience" — grim, terse, a little funerary, never a joke.
//
// Deterministic from an int seed (System.Random(seed) yields the same
// sequence for the same seed within a given .NET runtime), so RelicCatalog
// can bake a stable name for every catalog entry at static-init time, and
// Phase 9's ruin loot can mint a fresh one per find from whatever seed it has
// on hand (day count, position hash, roll index — anything).
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class RelicNaming
    {
        // ── Word banks ────────────────────────────────────────────────────────
        // Sized generously (not just enough for the ten shipped relics) —
        // Phase 9's ruin loot will mint names on the fly from arbitrary seeds
        // and needs a combinatorial space large enough to stay mostly unique
        // across many finds in one campaign.
        private static readonly string[] CompoundPrefixes =
        {
            "Cinder", "Grave", "Ash", "Night", "Iron", "Frost", "Blood", "Storm",
            "Hollow", "Ember", "Wraith", "Thorn", "Dusk", "Rime", "Pale", "Widow",
            "Grim", "Black", "Bone", "Wolf", "Raven", "Rust", "Salt", "Ruin",
        };

        private static readonly string[] CompoundSuffixes =
        {
            "fang", "bane", "heart", "veil", "grasp", "mark", "shroud", "wake",
            "thorn", "brand", "kiss", "sigh", "gale", "root", "ward", "gaze",
            "howl", "wound", "creed", "hollow",
        };

        private static readonly string[] PossessiveOwners =
        {
            "the Widow's", "the King's", "the Martyr's", "the Warden's",
            "the Beggar's", "the Hollow's", "the Silent One's", "the Watcher's",
            "the Drowned Man's", "the Last Priest's", "the Gravedigger's",
            "the First Widow's", "the Orphan's", "the Deserter's", "the Hanged Man's",
            "the Nameless One's",
        };

        private static readonly string[] AbstractNouns =
        {
            "Patience", "Mercy", "Ruin", "Silence", "Hunger", "Vigil", "Sorrow",
            "Reckoning", "Ashes", "Wrath", "Penance", "Refusal", "Grief",
            "Contempt", "Devotion", "Ruination", "Absolution", "Spite",
        };

        private static readonly string[] Ordinals =
        {
            "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh",
            "Eighth", "Ninth", "Tenth", "Eleventh", "Twelfth", "Last",
            "Thirteenth", "Twentieth", "Final", "Forgotten", "Unnamed",
        };

        private static readonly string[] TimeNouns =
        {
            "Dawn", "Dusk", "Vigil", "Night", "Hour", "Bell", "Watch", "Ember",
            "Silence", "Frost", "Star", "Candle", "Moon", "Tide", "Storm",
            "Winter", "Harvest", "Requiem",
        };

        private static readonly string[] RiteNouns =
        {
            "Vow", "Oath", "Rite", "Seal", "Ward", "Chant", "Psalm", "Litany",
            "Covenant", "Reliquary", "Sacrament", "Testament",
        };

        // ── Templates ─────────────────────────────────────────────────────────
        private const int TemplateCount = 4;

        // Generates a deterministic relic name from an integer seed. Never
        // throws for any int input (System.Random accepts any seed and every
        // list index below is taken modulo its own length).
        //
        // System.Random(seed) is deterministic but its FIRST draw correlates
        // strongly between adjacent seeds (a known quirk of its LCG) — feeding
        // consecutive seeds (0, 1, 2, ...) straight in produced a visibly
        // clumpy word pick and a high name-collision rate. Scrambling the seed
        // through a small integer hash first (splitmix32-style) breaks that
        // correlation while staying fully deterministic per input seed.
        public static string Generate(int seed)
        {
            var rng = new Random(Scramble(seed));
            int template = rng.Next(TemplateCount);

            switch (template)
            {
                case 0: // Compound: "Cinderfang"
                    return Pick(rng, CompoundPrefixes) + Pick(rng, CompoundSuffixes);

                case 1: // Possessive: "The Widow's Patience"
                    return "The " + StripLeadingThe(Pick(rng, PossessiveOwners)) + " " + Pick(rng, AbstractNouns);

                case 2: // Ordinal vow: "Vow of the Sixth Dawn"
                    return Pick(rng, RiteNouns) + " of the " + Pick(rng, Ordinals) + " " + Pick(rng, TimeNouns);

                default: // "Ember of Ruin"
                    return Pick(rng, CompoundPrefixes) + " of " + Pick(rng, AbstractNouns);
            }
        }

        // splitmix32-style integer scramble — cheap, deterministic, no
        // TaleWorlds/BCL crypto dependency, good-enough avalanche for our
        // purpose (breaking Random's adjacent-seed correlation).
        private static int Scramble(int seed)
        {
            unchecked
            {
                uint x = (uint)seed + 0x9E3779B9u;
                x = (x ^ (x >> 16)) * 0x85EBCA6Bu;
                x = (x ^ (x >> 13)) * 0xC2B2AE35u;
                x = x ^ (x >> 16);
                return (int)x;
            }
        }

        private static string Pick(Random rng, string[] bank)
        {
            if (bank == null || bank.Length == 0) return string.Empty;
            return bank[rng.Next(bank.Length)];
        }

        // PossessiveOwners entries already read naturally after "The "
        // ("The " + "the Widow's" would double up) — strip a leading "the ".
        private static string StripLeadingThe(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            const string prefix = "the ";
            if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return s.Substring(prefix.Length);
            return s;
        }
    }
}
