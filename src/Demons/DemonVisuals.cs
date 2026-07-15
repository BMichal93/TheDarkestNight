// =============================================================================
// THE DARKEST NIGHT — Demons/DemonVisuals.cs
//
// The LOOK of the demons — a re-tuned Elementals/ElementalVisuals (read that
// file's header first; this one keeps the technique — bone-bound continuous
// particles, one follower light, a persistent contour — but deliberately
// diverges on every dial). REVISION: the original cut wreathed a human body
// in the same near-invisible "ghost" alpha ElementalVisuals uses for the
// Kindled. The mod author rejected that — it read as a person in dark rags,
// not a monster. See DemonFactory.cs for the full research writeup on why a
// riderless beast-skeleton tier was not used instead; this file's job is to
// make the humanoid demons (and the Hellsteed's rider) read as beastlike hide
// creatures with the ONLY tools available with zero custom meshes/textures:
//   • a near-SOLID body (high alpha) instead of a translucent apparition —
//     a demon should look like a dark, hide-covered creature, not a ghost.
//   • a small, tightly-scoped RED EYE LIGHT at head height, separate from
//     the dimmer body-wide ember glow — "eyes glowing in the dark," not
//     "a red-lit person."
//   • fewer wreath points (chest/shoulders/head only, not the full arm-and-
//     leg column) so the particle wisp reads as rising off inhuman hide,
//     not a human silhouette wrapped in smoke.
//   • a darker, less saturated contour than the Kindled's — a demon should
//     be hard to make out at range, not glow like a signal fire.
// Palette stays dark grey/red/black, as specified. No custom mesh or texture
// is referenced anywhere in this file; "psys_smoke" and "psys_fire_vertical"
// are the same particle systems ElementalVisuals already uses for the
// Kindled.
//
// All state is mission-scoped: ClearAll() is called with the rest of the
// battle state (see DemonBattleBehavior.ClearBattleState). Nothing here is
// serialized.
// =============================================================================

using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class DemonVisuals
    {
        // Near-solid dark hide, not a translucent apparition — a demon should
        // read as a real, dark, present creature. Kept just shy of 1.0 so the
        // engine's hit-flash/hologram edge cases never fully vanish the body.
        private const float BodyAlpha = 0.90f;

        // The body-wide ember glow is deliberately dim and small now — the
        // eyes are the thing that should catch the eye in the dark, not the
        // whole body lighting up "like a parade" (mod author's words).
        private const float BodyLightRadiusMetres = 2.4f;

        // A second, tightly-scoped light bound to head height only. Small
        // radius, pure red, distinct from the tier-coloured body light.
        private const float EyeLightRadiusMetres = 0.55f;
        private const float EyeLightHeightMetres  = 1.62f; // roughly human eye height
        private static readonly Vec3 EyeRgb = new Vec3(1.0f, 0.05f, 0.02f);

        private class Shroud
        {
            public Skeleton Skeleton;
            public readonly List<KeyValuePair<sbyte, ParticleSystem>> Systems
                = new List<KeyValuePair<sbyte, ParticleSystem>>();
            public GameEntity Light;
            public GameEntity EyeLight;
            public uint Contour;
        }

        private static readonly Dictionary<Agent, Shroud> _shrouds = new Dictionary<Agent, Shroud>();

        public static bool IsShrouded(Agent agent) => agent != null && _shrouds.ContainsKey(agent);

        public static void Attach(Agent agent, DemonMath.DemonTier tier)
        {
            if (agent == null || _shrouds.ContainsKey(agent)) return;
            try
            {
                if (!agent.IsActive()) return;
                Skeleton skeleton = agent.AgentVisuals?.GetSkeleton();
                if (skeleton == null) return;

                string psys = ParticleFor(tier);
                var shroud = new Shroud { Skeleton = skeleton };

                MatrixFrame local = MatrixFrame.Identity;
                foreach (sbyte bone in WreathBones(agent))
                {
                    if (bone < 0) continue;
                    try
                    {
                        ParticleSystem ps = ParticleSystem.CreateParticleSystemAttachedToBone(psys, skeleton, bone, ref local);
                        if (ps != null)
                            shroud.Systems.Add(new KeyValuePair<sbyte, ParticleSystem>(bone, ps));
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                try { shroud.Light = SpellEffects.CreateFollowerLight(agent.Position + new Vec3(0f, 0f, 1.1f), Rgb(tier), BodyLightRadiusMetres); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                try { shroud.EyeLight = SpellEffects.CreateFollowerLight(agent.Position + new Vec3(0f, 0f, EyeLightHeightMetres), EyeRgb, EyeLightRadiusMetres); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                shroud.Contour = Argb(tier);
                _shrouds[agent] = shroud;

                try { agent.AgentVisuals?.GetEntity()?.SetContourColor(shroud.Contour, true); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                try { agent.AgentVisuals?.GetEntity()?.SetAlpha(BodyAlpha); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void Follow(Agent agent)
        {
            if (agent == null || !_shrouds.TryGetValue(agent, out Shroud shroud)) return;
            try
            {
                if (!agent.IsActive()) return;
                if (shroud.Light != null)
                    SpellEffects.MoveFollowerLight(shroud.Light, agent.Position + new Vec3(0f, 0f, 1.1f));
                if (shroud.EyeLight != null)
                    SpellEffects.MoveFollowerLight(shroud.EyeLight, agent.Position + new Vec3(0f, 0f, EyeLightHeightMetres));
                var entity = agent.AgentVisuals?.GetEntity();
                if (entity != null)
                {
                    entity.SetContourColor(shroud.Contour, true);
                    entity.SetAlpha(BodyAlpha);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void Detach(Agent agent)
        {
            if (agent == null || !_shrouds.TryGetValue(agent, out Shroud shroud)) return;
            _shrouds.Remove(agent);
            RemoveShroud(agent, shroud);
        }

        public static void ClearAll()
        {
            foreach (var kv in _shrouds)
                RemoveShroud(kv.Key, kv.Value);
            _shrouds.Clear();
        }

        private static void RemoveShroud(Agent agent, Shroud shroud)
        {
            if (shroud == null) return;
            foreach (var pair in shroud.Systems)
            {
                try { pair.Value?.SetEnable(false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { shroud.Skeleton?.RemoveBoneComponent(pair.Key, pair.Value); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            shroud.Systems.Clear();
            SpellEffects.RemoveFollowerLight(shroud.Light);
            shroud.Light = null;
            SpellEffects.RemoveFollowerLight(shroud.EyeLight);
            shroud.EyeLight = null;
            try
            {
                var entity = agent?.AgentVisuals?.GetEntity();
                if (entity != null)
                {
                    entity.SetContourColor(null, false);
                    entity.SetAlpha(1f);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Fewer points than the Kindled's full column — chest, shoulders and
        // head only, so the wisp reads as rising off dark hide rather than
        // wrapping a whole human silhouette (arms/hands/feet deliberately
        // left bare so the now-near-solid body itself carries the read).
        // Reads the agent's OWN Monster bone map, so the same Attach also
        // dresses the Hellsteed's horse (DemonBattleBehavior.TickMount) — the
        // horse Monster maps pelvis/neck/foreleg bones here, and any bone a
        // skeleton lacks comes back < 0 and is skipped.
        private static IEnumerable<sbyte> WreathBones(Agent agent)
        {
            Monster m = null;
            try { m = agent.Monster; } catch { }
            if (m == null) { yield return 0; yield break; }
            yield return m.PelvisBoneIndex;
            yield return m.SpineUpperBoneIndex;
            yield return m.NeckRootBoneIndex;
            yield return m.LeftUpperArmBoneIndex;
            yield return m.RightUpperArmBoneIndex;
        }

        // Ravagers carry a licking ember-fire (they cast hellfire); every other
        // tier — and the Hellsteed's rider — wears a neutral smoke the follower
        // light tints ember-red.
        private static string ParticleFor(DemonMath.DemonTier tier)
            => tier == DemonMath.DemonTier.Ravager ? "psys_fire_vertical" : "psys_smoke";

        // Dark grey / red / black — the demon palette. Kept deliberately darker
        // and redder than any Kindled colour so the two families never read as
        // the same thing on the field. This is the BODY-LIGHT colour, dimmed
        // and small (BodyLightRadiusMetres) — the eyes (EyeRgb, above) are the
        // one bright, legible feature.
        private static Vec3 Rgb(DemonMath.DemonTier tier)
        {
            switch (tier)
            {
                case DemonMath.DemonTier.Ravager:   return new Vec3(0.95f, 0.16f, 0.05f); // hot ember red
                case DemonMath.DemonTier.Stalker:   return new Vec3(0.55f, 0.08f, 0.06f); // dried-blood red
                case DemonMath.DemonTier.Hellsteed: return new Vec3(0.40f, 0.10f, 0.08f); // low red ember
                default:                            return new Vec3(0.30f, 0.06f, 0.05f); // Fiend — near-black ember
            }
        }

        // The contour outline, further darkened from the light colour above —
        // a demon should bleed a faint dark-red edge in shadow, not glow like
        // the Kindled's saturated elemental outline. "Difficult to understand"
        // — the eye should have to work to place the silhouette.
        private static Vec3 ContourRgb(DemonMath.DemonTier tier)
        {
            Vec3 c = Rgb(tier);
            return new Vec3(c.x * 0.55f, c.y * 0.55f, c.z * 0.55f);
        }

        private static uint Argb(DemonMath.DemonTier tier)
        {
            Vec3 c = ContourRgb(tier);
            uint r = (uint)(Clamp01(c.x) * 255f);
            uint g = (uint)(Clamp01(c.y) * 255f);
            uint b = (uint)(Clamp01(c.z) * 255f);
            return 0xFF000000u | (r << 16) | (g << 8) | b;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
