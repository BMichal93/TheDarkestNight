// =============================================================================
// THE DARKEST NIGHT — Veil/VeilMath.cs
//
// THE VEIL. Between the living world and the underworld below hangs a membrane
// that thins and thickens on a fixed rhythm — one full turn each season. When
// the Veil THINS, the fire in all things runs high (magic strikes harder) and
// the dark finds the cracks between worlds wider than before (more demons
// cross). When it draws THICK, the inner fire gutters low (magic weakens) but
// fewer of the Night can force a crossing (fewer demons).
//
// The rhythm is deliberately PREDICTABLE so both the player and the NPC world
// can plan around it: it keys off the absolute campaign day modulo one season
// (21 days in Bannerlord's calendar, so the cycle also lines up with the season
// boundaries), split into three equal seven-day windows. Every season plays out
// the same turn: Warding → Steady → Thinning, then the Veil snaps shut and the
// Warding begins again.
//
// Pure numeric core — no TaleWorlds types, fully covered by PureLogicTests.
// The runtime that reads the clock, announces the turn, and applies these
// multipliers lives in VeilCampaignBehavior.cs and at the choke points it wires
// into (ElementSpellEffects.CastAttack/CastWall, DemonSpawnCampaignBehavior,
// ElementLordAI, MortalLawCampaignBehavior).
// =============================================================================

using System;

namespace TheDarkestNight
{
    // The three turns of the Veil. Numbered low→high by how thin the Veil is
    // (how much bleeds through), so a larger value always means "more magic,
    // more dark." Not serialized as an enum — VeilCampaignBehavior persists the
    // int — but keep the numbering stable for sanity's sake.
    public enum VeilPhase { Warding = 0, Steady = 1, Thinning = 2 }

    public static class VeilMath
    {
        // One Bannerlord season. The cycle length equals the season so a full
        // turn of the Veil plays out once per season and lines up with the
        // season boundaries (day 0 of the campaign is the start of spring).
        public const int SeasonLengthDays = 21;
        public const int PhaseWindowDays  = SeasonLengthDays / 3; // 7

        // 0..20 within the current season/cycle.
        public static int DayOfCycle(int day)
        {
            int d = day % SeasonLengthDays;
            return d < 0 ? d + SeasonLengthDays : d;
        }

        // Which turn of the Veil a given absolute campaign day falls under.
        // Days 0–6 Warding, 7–13 Steady, 14–20 Thinning.
        public static VeilPhase PhaseForDay(int day)
        {
            int d = DayOfCycle(day);
            if (d < PhaseWindowDays)     return VeilPhase.Warding;
            if (d < PhaseWindowDays * 2) return VeilPhase.Steady;
            return VeilPhase.Thinning;
        }

        // How many days remain until the NEXT turn begins (always 1..7). Lets the
        // announcement and any planning UI say "the Thinning holds for three more
        // days." At the last day of Thinning this counts down into the next
        // season's Warding.
        public static int DaysUntilNextPhase(int day)
        {
            int d = DayOfCycle(day);
            int nextBoundary = ((d / PhaseWindowDays) + 1) * PhaseWindowDays;
            return nextBoundary - d;
        }

        // ── Magic effect scaling ──────────────────────────────────────────────
        // Folded into the CastAttack/CastWall power choke, so it lifts (or lowers)
        // the DIRECT damage of every elemental working — the player's Spellbook
        // casts, NPC mage lords, the Awakened, and demon hellfire alike. Because
        // ChargeFraction clamps at 1, a >1 multiplier lifts only the direct
        // damage, never the tuned cone reach / wall depth / ignite (exactly as the
        // overchannel's >1 power already behaves — see ElementMagicMath.MasteryScale).
        public static float MagicMultiplier(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning: return 1.5f;
                case VeilPhase.Warding:  return 0.7f;
                default:                 return 1.0f;
            }
        }

        // ── Demon spawn scaling ───────────────────────────────────────────────
        // Multiplies the nightly party count the Night Tide would otherwise raise.
        public static float DemonSpawnMultiplier(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning: return 1.2f;
                case VeilPhase.Warding:  return 0.6f;
                default:                 return 1.0f;
            }
        }

        // Apply the spawn multiplier to a rolled base party count, rounding to the
        // nearest whole party and never returning negative.
        public static int ApplySpawnMultiplier(int baseCount, VeilPhase phase)
        {
            if (baseCount <= 0) return 0;
            int scaled = (int)Math.Round(baseCount * DemonSpawnMultiplier(phase), MidpointRounding.AwayFromZero);
            return scaled < 0 ? 0 : scaled;
        }

        // ── Mage NPC cadence ──────────────────────────────────────────────────
        // Multiplies a mage lord's cast cooldown. Mages "come out to play" when
        // the Veil is thin — a value below 1 shortens the cooldown (they cast MORE
        // often during the Thinning) and above 1 lengthens it (they hold back
        // during the Warding, when the fire is hard to reach). Applied on top of
        // the existing life/temper cadence stretch, so it composes cleanly.
        public static float MageLordCooldownMultiplier(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning: return 0.6f;  // casts ~1.6× more often
                case VeilPhase.Warding:  return 1.4f;  // conserves — the fire runs low
                default:                 return 1.0f;
            }
        }

        // ── Campaigning-lord caution ──────────────────────────────────────────
        // Scales the night-fear "safe army size" threshold in MortalLaw. During
        // the Thinning the Night swells, so even a large host feels unsafe and
        // shelters (threshold raised); during the Warding the roads are quiet, so
        // only the smallest parties bother to hole up and true armies march freely
        // (threshold lowered). This is what makes campaigning lords prefer the
        // "non-magical" Warding season to move their armies.
        public static float NightSafeSizeMultiplier(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning: return 1.5f;  // more hosts shelter
                case VeilPhase.Warding:  return 0.5f;  // armies march freely
                default:                 return 1.0f;
            }
        }

        // ── Labels ────────────────────────────────────────────────────────────
        public static string PhaseName(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning: return "the Thinning";
                case VeilPhase.Warding:  return "the Warding";
                default:                 return "the Steady";
            }
        }

        public static string PhaseDescription(VeilPhase phase)
        {
            switch (phase)
            {
                case VeilPhase.Thinning:
                    return "The Veil thins. The fire in all things runs high, and the dark finds the cracks between worlds wider than before — magic burns bright, and the Night crosses in greater numbers. Walk carefully.";
                case VeilPhase.Warding:
                    return "The Veil draws thick and cold. The inner fire gutters low, but far fewer of the dark can force a crossing. If a host means to march, it marches now.";
                default:
                    return "The Veil holds even. The world keeps its ordinary balance of fire and dark.";
            }
        }
    }
}
