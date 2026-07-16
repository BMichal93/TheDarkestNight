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

                string display = _buffer + new string('_', System.Math.Max(0, SpellbookCatalog.MinFormulaLength - _buffer.Length));
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
            if (_buffer.Length < SpellbookCatalog.MaxFormulaLength) _buffer += dir;
        }

        private static void TryResolve()
        {
            if (_buffer.Length == 0) return;

            var caster = Agent.Main;
            if (caster == null || !caster.IsActive()) return;

            if (!SpellEffects.HasFreeHand(caster))
            {
                Fizzle("Your hands are full of steel. Sheathe your weapon to speak a formula.");
                return;
            }

            if (SpellbookCatalog.TryGetByFormula(_buffer, out SpellDef def))
            {
                // Requirement 16: casts AND is recorded — even the first time.
                SpellbookCampaignBehavior.LearnSpell(def.Id);
                SpellbookEffects.Cast(def.Id, caster);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{def.Name} answers.", new Color(0.75f, 0.4f, 0.85f)));
                return;
            }

            // A completed but wrong formula — fizzle, then roll spellburn.
            Fizzle("The formula does not answer — it dies unspoken.");
            int intellect = 0;
            try { intellect = Hero.MainHero?.GetAttributeValue(DefaultCharacterAttributes.Intelligence) ?? 0; }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float chance = SpellbookMath.SpellburnChance(intellect);
            // Talisman of the Unburnt Tongue (mod-author-directed addition):
            // shaves flat percentage points off the roll, still respecting
            // SpellbookMath.MinSpellburnChance's floor.
            try
            {
                if (TalismanEffects.CarriesTalisman(caster, TalismanId.UnburntTongue))
                    chance = TalismansMath.ReducedSpellburnChance(chance);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (_rng.NextDouble() < chance)
                SpellburnEffects.Trigger(caster);
        }

        private static readonly System.Random _rng = new System.Random();

        private static void Fizzle(string msg)
            => InformationManager.DisplayMessage(new InformationMessage(msg, new Color(0.6f, 0.6f, 0.6f)));
    }
}
