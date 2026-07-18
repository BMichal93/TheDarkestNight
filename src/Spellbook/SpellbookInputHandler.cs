// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellbookInputHandler.cs
//
// THE SPOKEN GESTURE — battle only, mirrors MiracleInputHandler's hold-
// modifier/tap-direction/release-to-cast pattern exactly (see that file's
// header for the design this is copied from):
//
//   Keyboard: hold Left Alt, tap W/A/S/D (= U/L/R/D) to build the formula,
//   release Alt to speak it.
//   Controller: hold X (ControllerRLeft), flick the left stick.
//
// Left Alt and X are free again in this phase — the unified element
// hold-and-charge input they used to drive is superseded by the spellbook
// (see ElementMagicInput.Tick's guard clause).
//
// Casting needs a free hand and requires the spellbook to be unlocked
// (Requirement 13 — one focus point, spent once, in the book's menu).
//
// Requirement 16: a CORRECT completed formula casts AND is recorded as known
// (even if never learned before) — the spoken word teaches itself. An
// INCORRECT completed formula fizzles and rolls spellburn (Requirement 18).
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class SpellbookInputHandler
    {
        private static string _buffer = "";
        private static bool   _wasHolding = false;
        private static string _lastDisplay = "";

        public static void ResetInputState()
        {
            _buffer = "";
            _wasHolding = false;
            _lastDisplay = "";
        }

        // Battle only — no campaign-map spells exist in this mod (Requirement 14).
        public static void Tick(bool inMission)
        {
            if (!inMission) return;
            if (!SpellbookCampaignBehavior.IsUnlocked) return;

            bool altHeld = Input.IsKeyDown(InputKey.LeftAlt);
            bool padHeld = Input.IsKeyDown(InputKey.ControllerRLeft);
            bool holding = altHeld || padHeld;

            if (holding)
            {
                if (!_wasHolding)
                {
                    _wasHolding = true;
                    // Restore the AshAndEmber hold-to-channel behaviour: while focus is
                    // held the caster stands and plays the looping cast stance (BeginCastLoop),
                    // stopping the instant focus is released (EndCastLoop below). The old
                    // ElementMagicInput player path did exactly this; the Spellbook handler
                    // had only the aura, not the animation.
                    try { if (Agent.Main != null) SpellEffects.BeginCastLoop(Agent.Main); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { if (Agent.Main != null) SpellEffects.BeginFocusVisual(Agent.Main, ColorSchool.Purple); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                bool bookKey = altHeld ? Input.IsKeyPressed(InputKey.X) : Input.IsKeyPressed(InputKey.ControllerLThumb);
                if (bookKey && _buffer.Length == 0)
                {
                    SpellbookCampaignBehavior.ShowSpellbook();
                    return;
                }

                if (altHeld)
                {
                    if      (Input.IsKeyPressed(InputKey.W)) Append("U");
                    else if (Input.IsKeyPressed(InputKey.A)) Append("L");
                    else if (Input.IsKeyPressed(InputKey.D)) Append("R");
                    else if (Input.IsKeyPressed(InputKey.S)) Append("D");
                }
                else
                {
                    ReadPad();
                }

                string display = _buffer + new string('_', System.Math.Max(0, RuneCatalog.RuneLength - _buffer.Length));
                if (display != _lastDisplay)
                {
                    _lastDisplay = display;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "[ " + display + " ]", new Color(0.75f, 0.4f, 0.85f)));
                }
            }
            else if (_wasHolding)
            {
                _wasHolding = false;
                _lastDisplay = "";
                try { if (Agent.Main != null) SpellEffects.EndCastLoop(Agent.Main); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { if (Agent.Main != null) SpellEffects.EndFocusVisual(Agent.Main); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                TryResolve();
                _buffer = "";
            }
        }

        private static bool _prevPadUp, _prevPadDown, _prevPadLeft, _prevPadRight;
        private static void ReadPad()
        {
            bool up    = Input.IsKeyDown(InputKey.ControllerLStickUp);
            bool down  = Input.IsKeyDown(InputKey.ControllerLStickDown);
            bool left  = Input.IsKeyDown(InputKey.ControllerLStickLeft);
            bool right = Input.IsKeyDown(InputKey.ControllerLStickRight);
            if (up    && !_prevPadUp)    Append("U");
            if (down  && !_prevPadDown)  Append("D");
            if (left  && !_prevPadLeft)  Append("L");
            if (right && !_prevPadRight) Append("R");
            _prevPadUp = up; _prevPadDown = down; _prevPadLeft = left; _prevPadRight = right;
        }

        private static void Append(string dir)
        {
            if (_buffer.Length < RuneCatalog.MaxSequenceRunes * RuneCatalog.RuneLength) _buffer += dir;
        }

        private static readonly Color RuneColor = new Color(0.75f, 0.4f, 0.85f);

        private static void TryResolve()
        {
            if (_buffer.Length == 0) return;

            var caster = Agent.Main;
            if (caster == null || !caster.IsActive()) return;

            if (!SpellEffects.HasFreeHand(caster))
            {
                Fizzle("Your hands are full of steel. Sheathe your weapon to draw a binding.");
                return;
            }

            // 1) Chunk the marks into runes. Trailing marks or a non-rune triplet
            //    is malformed → fizzle + spellburn (nothing was even a real rune).
            if (!RuneSequenceMath.TryChunk(_buffer, out var runes, out string chunkReason))
            {
                FizzleAndBurn(caster, "The binding falters — " + chunkReason);
                return;
            }

            // 2) Discovery (RUNE_MAGIC_PLAN.md §9): every real rune drawn that is not
            //    yet known is learned now — even inside a binding that later resolves
            //    malformed, the mark was real.
            foreach (var rid in runes.Distinct())
            {
                if (!SpellbookCampaignBehavior.KnowsRune(rid))
                {
                    SpellbookCampaignBehavior.LearnRune(rid);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"A new mark answers your hand: {RuneCatalog.Get(rid).Name}.", RuneColor));
                }
            }

            // 3) Resolve the binding as a sentence.
            ResolvedWorking working = RuneSequenceMath.Resolve(runes);
            if (working.Malformed)
            {
                // Real runes that simply compose nothing (a lone Echo, an inert
                // manner) fizzle without a burn — only a genuine misbinding burns.
                if (working.Harmless) Fizzle("The marks find nothing to work upon.");
                else                  FizzleAndBurn(caster, "The binding will not hold — " + working.Reason);
                return;
            }

            // 4) A valid working fires — then STRAIN (rule 7) rolls on top: a long
            //    binding can still burn its caster even when it lands.
            RuneEffects.Cast(working, caster);
            InformationManager.DisplayMessage(new InformationMessage(
                $"{working.Name} answers.", RuneColor));

            int intellect = Intellect();
            float strain = SpellbookMath.StrainAfterIntellect(working.StrainChance, intellect);
            strain = ApplyTalisman(caster, strain);
            if (strain > 0f && _rng.NextDouble() < strain)
                SpellburnEffects.Trigger(caster);
        }

        // A malformed binding fizzles, then rolls the fizzle-spellburn chance.
        private static void FizzleAndBurn(Agent caster, string msg)
        {
            Fizzle(msg);
            float chance = SpellbookMath.SpellburnChance(Intellect());
            chance = ApplyTalisman(caster, chance);
            if (_rng.NextDouble() < chance)
                SpellburnEffects.Trigger(caster);
        }

        private static int Intellect()
        {
            try { return Hero.MainHero?.GetAttributeValue(DefaultCharacterAttributes.Intelligence) ?? 0; }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }

        // Talisman of the Unburnt Tongue shaves flat points off any burn roll,
        // still respecting SpellbookMath's floor.
        private static float ApplyTalisman(Agent caster, float chance)
        {
            try
            {
                if (TalismanEffects.CarriesTalisman(caster, TalismanId.UnburntTongue))
                    return TalismansMath.ReducedSpellburnChance(chance);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            return chance;
        }

        private static readonly System.Random _rng = new System.Random();

        private static void Fizzle(string msg)
            => InformationManager.DisplayMessage(new InformationMessage(msg, new Color(0.6f, 0.6f, 0.6f)));
    }
}
