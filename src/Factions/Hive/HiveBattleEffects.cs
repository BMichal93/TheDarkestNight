// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveBattleEffects.cs
//
// The Hive's two battle downsides — both gated on the AGENT being Integrated
// (the player, or a Hive lord's hero agent present in the mission), and both
// intermittent, never guaranteed, per HiveMath.DownsideCheckIntervalSeconds.
//
//   • Colour inversion (hallucination) — a jarring, wrong-feeling scene tint
//     for a few seconds. True framebuffer/postfx colour inversion is not
//     something this codebase's plumbing exposes (no shader/render-target
//     access exists anywhere in src/); AshenSceneTone.cs is this mod's only
//     precedent for a scene-wide visual effect, and it works by reflecting
//     into Scene.SetFog/SetSun. This reuses exactly that mechanism, tuned to
//     a garish, saturated, "wrong" palette (acid green fog, inverted-feeling
//     sun colours) as the closest safe approximation of an inversion — this
//     is a deliberate, documented substitution, not a true pixel invert.
//   • Will suppression — "your will is suppressed by others": the network's
//     other voices occasionally override your own order to your own troops.
//     Implemented on the existing SpellburnEffects.RandomCommand plumbing —
//     one formation gets a forced, random movement order for a moment.
//
// Applies to Hive lords too where an equivalent exists: the will-suppression
// roll also considers Hive lord agents' formations (a network-bound lord's
// own men can be overridden the same way). Colour inversion is inherently a
// player-screen effect and has no lord-facing equivalent — documented here
// rather than silently skipped.
// =============================================================================

using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class HiveBattleEffects
    {
        private static readonly Random _rng = new Random();
        private static float _timer = 0f;

        private static bool  _inversionActive = false;
        private static float _inversionTimer  = 0f;

        private static MethodInfo _setFogMethod;
        private static bool       _fogResolved = false;

        public static void MissionTick(float dt)
        {
            try
            {
                if (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing) return;

                if (_inversionActive)
                {
                    _inversionTimer -= dt;
                    if (_inversionTimer <= 0f) { _inversionActive = false; RestoreFog(); }
                }

                bool playerIntegrated = HiveCampaignBehavior.IsIntegrated(Hero.MainHero);
                bool hiveLordPresent = AnyHiveLordAgentPresent();
                if (!playerIntegrated && !hiveLordPresent) return;
                if (!SpellEffects.IsBattleMission()) return;

                _timer += dt;
                if (_timer < HiveMath.DownsideCheckIntervalSeconds) return;
                _timer = 0f;

                if (playerIntegrated && HiveMath.RollColourInversion(_rng.NextDouble()))
                    TriggerColourInversion();

                if ((playerIntegrated || hiveLordPresent) && HiveMath.RollWillSuppression(_rng.NextDouble()))
                    TriggerWillSuppression(playerIntegrated);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool AnyHiveLordAgentPresent()
        {
            try
            {
                if (Mission.Current == null) return false;
                return Mission.Current.Agents.Any(a =>
                    a != null && a.IsActive() && !a.IsMount && a.IsHero &&
                    (a.Character as CharacterObject)?.HeroObject is Hero h &&
                    h != Hero.MainHero && HiveCulture.IsHiveLord(h) && HiveCampaignBehavior.IsIntegrated(h));
            }
            catch { return false; }
        }

        // ── Colour inversion (hallucination) ────────────────────────────────────
        private static void TriggerColourInversion()
        {
            try
            {
                _inversionActive = true;
                _inversionTimer = HiveMath.ColourInversionDurationSeconds;
                ApplyInvertedFog();
                InformationManager.DisplayMessage(new InformationMessage(
                    "The network's voice presses behind your eyes — the field runs wrong colours for a moment.",
                    new Color(0.7f, 0.95f, 0.2f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ApplyInvertedFog()
        {
            try
            {
                var scene = Mission.Current?.Scene;
                if (scene == null) return;
                if (!_fogResolved)
                {
                    _fogResolved = true;
                    _setFogMethod = scene.GetType().GetMethod("SetFog",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                if (_setFogMethod == null) return;
                var parms = _setFogMethod.GetParameters();
                if (parms.Length == 3)
                    _setFogMethod.Invoke(scene, new object[]
                        { 0.02f, new Vec3(0.75f, 0.95f, 0.15f), 0.5f }); // acid-green, wrong-feeling
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RestoreFog()
        {
            try
            {
                var scene = Mission.Current?.Scene;
                if (scene == null || _setFogMethod == null) return;
                var parms = _setFogMethod.GetParameters();
                if (parms.Length == 3)
                    _setFogMethod.Invoke(scene, new object[] { 0.004f, new Vec3(0.6f, 0.6f, 0.6f), 1f });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Will suppression ─────────────────────────────────────────────────────
        private static void TriggerWillSuppression(bool playerVoice)
        {
            try
            {
                if (Mission.Current == null) return;
                var team = playerVoice ? Mission.Current.PlayerTeam : HiveLordTeam();
                if (team == null) return;

                var forms = team.FormationsIncludingEmpty.Where(f => f != null && f.CountOfUnits > 0).ToList();
                if (forms.Count == 0) return;
                var form = forms[_rng.Next(forms.Count)];
                switch (_rng.Next(3))
                {
                    case 0: form.SetMovementOrder(MovementOrder.MovementOrderCharge); break;
                    case 1: form.SetMovementOrder(MovementOrder.MovementOrderRetreat); break;
                    default: form.SetMovementOrder(MovementOrder.MovementOrderAdvance); break;
                }

                if (playerVoice)
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You give the order — but it is not quite the order that leaves your mouth. Other voices in the network speak over you.",
                        new Color(0.7f, 0.95f, 0.2f)));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Team HiveLordTeam()
        {
            try
            {
                return Mission.Current?.Agents.FirstOrDefault(a =>
                    a != null && a.IsActive() && !a.IsMount && a.IsHero &&
                    (a.Character as CharacterObject)?.HeroObject is Hero h &&
                    h != Hero.MainHero && HiveCulture.IsHiveLord(h) && HiveCampaignBehavior.IsIntegrated(h))?.Team;
            }
            catch { return null; }
        }

        public static void Reset()
        {
            _timer = 0f;
            _inversionActive = false;
            _inversionTimer = 0f;
            _fogResolved = false;
            _setFogMethod = null;
        }
    }
}
