// =============================================================================
// THE DARKEST NIGHT — Demons/DemonVisuals.cs
//
// The LOOK of the demons — a direct re-tuning of Elementals/ElementalVisuals
// (read that file's header first; this one only changes the palette and the
// particle choice, never the technique). A demon body is wreathed ONCE, at
// the first tick it is seen alive, in continuous particle systems bound to
// its own skeleton bones (pelvis/chest/head/hands/feet), plus one follower
// light and a persistent coloured contour. Nothing is re-stamped per tick.
//
// Palette: smoke-black body, ember-red glow — dark grey/red/black, as
// specified. No custom mesh or texture is referenced anywhere in this file;
// "psys_smoke" and "psys_fire_vertical" are the same particle systems
// ElementalVisuals already uses successfully for the Kindled.
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
        // Same "faceless apparition" treatment as the Kindled — the eye should
        // follow the smoke and the glow, not a textured human face.
        private const float GhostAlpha = 0.10f;

        private class Shroud
        {
            public Skeleton Skeleton;
            public readonly List<KeyValuePair<sbyte, ParticleSystem>> Systems
                = new List<KeyValuePair<sbyte, ParticleSystem>>();
            public GameEntity Light;
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

                try { shroud.Light = SpellEffects.CreateFollowerLight(agent.Position + new Vec3(0f, 0f, 1.1f), Rgb(tier), 4.0f); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                shroud.Contour = Argb(tier);
                _shrouds[agent] = shroud;

                try { agent.AgentVisuals?.GetEntity()?.SetContourColor(shroud.Contour, true); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                try { agent.AgentVisuals?.GetEntity()?.SetAlpha(GhostAlpha); }
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
                var entity = agent.AgentVisuals?.GetEntity();
                if (entity != null)
                {
                    entity.SetContourColor(shroud.Contour, true);
                    entity.SetAlpha(GhostAlpha);
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

        // Same bone spread as the Kindled — full column up the trunk plus arms,
        // hands and feet, so the shroud reads on a demon exactly as it does on
        // an elemental: no bare skin between a few thin columns of smoke.
        private static IEnumerable<sbyte> WreathBones(Agent agent)
        {
            Monster m = null;
            try { m = agent.Monster; } catch { }
            if (m == null) { yield return 0; yield break; }
            yield return m.PelvisBoneIndex;
            yield return m.SpineLowerBoneIndex;
            yield return m.SpineUpperBoneIndex;
            yield return m.NeckRootBoneIndex;
            yield return m.HeadLookDirectionBoneIndex;
            yield return m.LeftUpperArmBoneIndex;
            yield return m.RightUpperArmBoneIndex;
            yield return m.MainHandBoneIndex;
            yield return m.OffHandBoneIndex;
            yield return m.LeftFootIkEndEffectorBoneIndex;
            yield return m.RightFootIkEndEffectorBoneIndex;
        }

        // Ravagers carry a licking ember-fire (they cast hellfire); every other
        // tier — and the Hellsteed's rider — wears a neutral smoke the follower
        // light tints ember-red.
        private static string ParticleFor(DemonMath.DemonTier tier)
            => tier == DemonMath.DemonTier.Ravager ? "psys_fire_vertical" : "psys_smoke";

        // Dark grey / red / black — the demon palette. Kept deliberately darker
        // and redder than any Kindled colour so the two families never read as
        // the same thing on the field.
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

        private static uint Argb(DemonMath.DemonTier tier)
        {
            Vec3 c = Rgb(tier);
            uint r = (uint)(Clamp01(c.x) * 255f);
            uint g = (uint)(Clamp01(c.y) * 255f);
            uint b = (uint)(Clamp01(c.z) * 255f);
            return 0xFF000000u | (r << 16) | (g << 8) | b;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
