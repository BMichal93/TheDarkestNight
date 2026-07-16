// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodAttunementInputHandler.cs
//
// The player's battle input for a drunk-in blood-attunement (a mod-author-
// directed addition). Deliberately a BRAND NEW handler, not a re-enable of
// the retired ElementMagicInput (PlayerCastingEnabled = false, permanently,
// per Phase 4) — this reuses that system's underlying effect calls
// (ElementSpellEffects.CastAttack/CastWall) and its draw/charge math
// (ElementMagicMath), but gates on BloodAttunement.HasAnyElement instead of
// MageKnowledge.IsMage, and reads a DIFFERENT hold key so it cannot collide
// with the Spellbook's own gesture input (Left Alt / X).
//
//   Hold LEFT CONTROL. Both LeftControl and RightControl are already read by
//   NatureInputHandler/MiracleInputHandler, but BOTH of those are also
//   permanently retired (PlayerCastingEnabled = false, see their headers) —
//   their Tick methods return before ever touching the key, so LeftControl is
//   genuinely free at runtime. Confirmed by grepping every InputKey.LeftControl
//   / RightControl use in src/ before choosing it.
//
//   While held: tap W / S / A to LOAD Wind / Earth / Water (Fire is the
//   default/no-tap element) — the SAME convention ElementMagicInput used, for
//   a player who remembers the old scheme. D is unused (Spirit is excluded
//   from this path per the brief). Only elements the hero has actually
//   attuned to may be loaded; an untaught tap is refused with a hint.
//
//   Stand still to DRAW a charge (ElementMagicMath.PowerMult, exactly like
//   the old system) — Left Mouse looses the loaded element's ATTACK, Right
//   Mouse raises its WALL. No hand/armour gating (the blood does not care
//   what you are carrying) and no aging/life cost (this is Demon Blood's
//   working, not the fire's) — those are deliberate simplifications from the
//   old system's parity, not oversights.
//
//   Works ONLY at night or in twilight (BloodAttunementMath.IsUsableHour) —
//   "as this is demonic power." A draw started before daylight fell is
//   dropped, not merely paused, the instant the window closes.
//
//   Works even for a hero who is not a mage, not Spellbook-unlocked — gated
//   ONLY on having attuned to at least one element.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class BloodAttunementInputHandler
    {
        private static bool  _wasHolding;
        private static float _drawTime;
        private static float _sinceCast = ElementMagicMath.RecoverySeconds;
        private static bool  _prevAtk, _prevBlk;
        private static float _dayReminder;
        private const  float DayReminderInterval = 4f;
        private const  float StillSpeed = 0.3f;

        public static void ResetInputState()
        {
            _wasHolding = false;
            _drawTime = 0f;
            _sinceCast = ElementMagicMath.RecoverySeconds;
            _prevAtk = _prevBlk = false;
            _dayReminder = 0f;
        }

        public static void Tick(bool inMission, float dt = 0f)
        {
            if (!inMission) return;
            Hero hero = Hero.MainHero;
            if (!BloodAttunement.HasAnyElement(hero)) return;

            if (_sinceCast < 1e6f) _sinceCast += dt;

            bool holding = Input.IsKeyDown(InputKey.LeftControl);
            if (holding)
            {
                if (!_wasHolding)
                {
                    _wasHolding = true;
                    _drawTime = 0f;
                    BloodAttunement.EnsureValidLoaded(hero);
                    try { if (Agent.Main != null) SpellEffects.BeginFocusVisual(Agent.Main, ColorSchool.Ashen); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                ReadElementSelect();

                float hour = 12f;
                try { hour = (float)CampaignTime.Now.CurrentHourInDay; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                if (!BloodAttunementMath.IsUsableHour(hour))
                {
                    _drawTime = 0f;
                    _dayReminder -= dt;
                    if (_dayReminder <= 0f)
                    {
                        Msg("The blood sleeps under a high sun — it answers only by night or twilight.");
                        _dayReminder = DayReminderInterval;
                    }
                }
                else
                {
                    Agent c = Agent.Main;
                    bool still = c != null && c.IsActive();
                    if (still) { try { still = c.GetCurrentVelocity().Length < StillSpeed; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } }

                    if (c == null || !c.IsActive() || !still)
                    {
                        _drawTime = 0f;
                    }
                    else
                    {
                        _drawTime += dt;
                        if (_drawTime >= ElementMagicMath.MaxDrawSeconds) _drawTime = 0f;
                    }

                    bool atk = Input.IsKeyDown(InputKey.LeftMouseButton);
                    bool blk = Input.IsKeyDown(InputKey.RightMouseButton);
                    bool atkEdge = atk && !_prevAtk;
                    bool blkEdge = blk && !_prevBlk;
                    if (atkEdge) TryCast(CastForm.Attack);
                    else if (blkEdge) TryCast(CastForm.Wall);
                    _prevAtk = atk; _prevBlk = blk;
                }
            }
            else if (_wasHolding)
            {
                _wasHolding = false;
                _prevAtk = _prevBlk = false;
                _drawTime = 0f;
                try { if (Agent.Main != null) SpellEffects.EndFocusVisual(Agent.Main); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // W / S / A load Wind / Earth / Water — the same W/S/A/D convention
        // ElementMagicInput used (D stood for Spirit there; unused here).
        private static void ReadElementSelect()
        {
            if      (Input.IsKeyPressed(InputKey.W)) TrySelect(MagicElement.Wind);
            else if (Input.IsKeyPressed(InputKey.S)) TrySelect(MagicElement.Earth);
            else if (Input.IsKeyPressed(InputKey.A)) TrySelect(MagicElement.Water);
        }

        private static void TrySelect(MagicElement el)
        {
            if (BloodAttunement.HasElement(Hero.MainHero, el))
            {
                BloodAttunement.SetPlayerLoaded(el);
                Msg($"{el} loaded.");
            }
            else
            {
                Msg("Your blood has not been attuned to that element.");
            }
        }

        private static void TryCast(CastForm form)
        {
            var caster = Agent.Main;
            if (caster == null || !caster.IsActive()) return;
            if (!ElementMagicMath.CanReleaseAgain(_sinceCast))
            {
                Msg("The blood settles — let it gather.");
                return;
            }

            float power = ElementMagicMath.PowerMult(_drawTime);
            var el = BloodAttunement.PlayerLoaded;
            if (!BloodAttunement.HasElement(Hero.MainHero, el)) return; // safety — should not happen after EnsureValidLoaded

            try
            {
                if (form == CastForm.Attack) ElementSpellEffects.CastAttack(el, caster, power);
                else                         ElementSpellEffects.CastWall(el, caster, power);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            _sinceCast = 0f;
            _drawTime = 0f;
        }

        private static void Msg(string text)
            => InformationManager.DisplayMessage(new InformationMessage(text, new Color(0.55f, 0.10f, 0.10f)));
    }
}
