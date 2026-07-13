// =============================================================================
// THE DARKEST NIGHT — Demons/DemonBattleBehavior.cs
//
// Mission-side registry of every demon fighting in the current battle —
// Elementals/ElementalBeings.cs's exact structure, re-tuned for demons:
//
//   • TickAuras(dt)  — binds each body's DemonVisuals shroud the first tick it
//                      is seen alive, keeps it roused, and (Ravagers only)
//                      looses hellfire on a cooldown.
//   • OnAgentBuild    — identifies a troops.xml demon id (demon_fiend,
//                      demon_stalker, demon_ravager, demon_hellsteed) the
//                      moment its Agent is built, and registers it.
//   • ReRouse         — reasserted every few seconds: requirement 7a/7b/7c,
//                      demons never retreat, never flee, never manoeuvre —
//                      only ever CHARGE. This is the mission-side half of
//                      that rule; the map-side half lives in
//                      DemonSpawnCampaignBehavior.
//
// All state is mission-scoped and cleared with the rest of the battle state
// (see MagicSystem's MainSubModule.OnGameStart / MagicMissionBehavior.OnEndMission),
// nothing here is serialized.
// =============================================================================

using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class DemonBattleBehavior
    {
        private class Being
        {
            public Agent Agent;
            public DemonMath.DemonTier Tier;
            public bool Shrouded;
            public float CastTimer;
        }

        private static readonly Random _rng = new Random();
        private static readonly List<Being> _beings = new List<Being>();
        private static readonly Dictionary<Agent, DemonMath.DemonTier> _tierOf = new Dictionary<Agent, DemonMath.DemonTier>();

        // Set by DemonSpawnCampaignBehavior.OnMapEventStarted when a tracked
        // demon party is a side in the coming battle, so OnAgentBuild can scale
        // health for the region it rose under (requirement 2's environment
        // variants). Null in every ordinary battle. Cleared at mission end and
        // at map-event end.
        public static DemonMath.EnvironmentVariant? PendingVariant = null;

        public static void Register(Agent agent, DemonMath.DemonTier tier)
        {
            if (agent == null) return;
            if (_tierOf.ContainsKey(agent)) { _tierOf[agent] = tier; return; }
            _tierOf[agent] = tier;
            _beings.Add(new Being
            {
                Agent = agent, Tier = tier,
                CastTimer = (float)(_rng.NextDouble() * DemonMath.RavagerCastCooldownSeconds),
            });
        }

        public static bool IsDemon(Agent agent) => agent != null && _tierOf.ContainsKey(agent);

        public static bool TryGetTier(Agent agent, out DemonMath.DemonTier tier)
        {
            if (agent != null && _tierOf.TryGetValue(agent, out tier)) return true;
            tier = DemonMath.DemonTier.Fiend;
            return false;
        }

        public static void ClearBattleState()
        {
            try { DemonVisuals.ClearAll(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            _beings.Clear();
            _tierOf.Clear();
            PendingVariant = null;
        }

        // Every troop built for this mission passes through here; no-op unless
        // its CharacterObject id is one of ours (DemonCatalog).
        public static void OnAgentBuild(Agent agent)
        {
            try
            {
                if (agent == null || agent.IsMount) return;
                string id = null;
                try { id = agent.Character?.StringId; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (!DemonCatalog.TryGetTier(id, out DemonMath.DemonTier tier)) return;
                Register(agent, tier);
                // Environment variant (snow/desert/forest): scale HP the same way
                // ElementalBeings.ConvertBattleAgent scales a Kindled's — bumped up,
                // never down, so it never undercuts the troop's own base stats.
                try
                {
                    DemonMath.EnvironmentVariant variant = PendingVariant ?? DemonMath.EnvironmentVariant.Default;
                    float hp = DemonMath.Health(tier, variant);
                    agent.HealthLimit = Math.Max(agent.HealthLimit, hp);
                    agent.Health = agent.HealthLimit;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { DemonFactory.SetAggressive(agent, agent.Team); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Tick (driven by MagicMissionBehavior.OnMissionTick) ─────────────────
        private static float _reAggroTimer = 0f;

        public static void TickAuras(float dt)
        {
            if (_beings.Count == 0) return;

            _reAggroTimer -= dt;
            bool reAggro = _reAggroTimer <= 0f;
            if (reAggro) _reAggroTimer = 3f; // shorter than the Kindled's 4s — demons are more relentless

            for (int i = _beings.Count - 1; i >= 0; i--)
            {
                Being b = _beings[i];
                bool alive = false;
                try { alive = b.Agent != null && b.Agent.IsActive() && b.Agent.Health > 0f; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (!alive)
                {
                    if (b.Agent != null)
                    {
                        _tierOf.Remove(b.Agent);
                        try { DemonVisuals.Detach(b.Agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                    _beings.RemoveAt(i);
                    continue;
                }

                if (!b.Shrouded)
                {
                    try { DemonVisuals.Attach(b.Agent, b.Tier); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    b.Shrouded = DemonVisuals.IsShrouded(b.Agent);
                }
                else
                {
                    try { DemonVisuals.Follow(b.Agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                // Requirement 7a/7b/7c: never retreat, never escape, never
                // strategize — reassert the charge order so nothing (a reformed
                // line, a lost target, morale AI) ever lets a demon fall back.
                if (reAggro) ReRouse(b.Agent);

                if (DemonMath.CastsMagic(b.Tier))
                {
                    b.CastTimer -= dt;
                    if (b.CastTimer <= 0f)
                    {
                        b.CastTimer = DemonMath.RavagerCastCooldownSeconds;
                        TryLooseHellfire(b.Agent);
                    }
                }
            }
        }

        private static void ReRouse(Agent agent)
        {
            try
            {
                if (agent.Team == null) return;
                if (Mission.Current != null && agent.Team == Mission.Current.PlayerTeam) return;
                try { agent.SetWatchState(Agent.WatchState.Alarmed); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                Formation form = agent.Formation;
                if (form != null)
                    try { form.SetMovementOrder(MovementOrder.MovementOrderCharge); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // A Ravager looses a cone of fire at the nearest foe within reach —
        // reuses ElementSpellEffects.CastAttack exactly as ElementalBeings does
        // for the Kindled, just gated to the Ravager tier and a shorter, harsher
        // cooldown ("some with magical attacks").
        private const float CastRangeMetres = 11f;

        private static void TryLooseHellfire(Agent agent)
        {
            try
            {
                if (agent == null || !agent.IsActive() || agent.Team == null) return;
                if (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing) return;

                Vec3 pos = agent.Position;
                float r2 = CastRangeMetres * CastRangeMetres;
                Agent nearest = null; float bestD2 = r2;
                foreach (Agent a in Mission.Current.Agents)
                {
                    if (a == null || !a.IsActive() || a.IsMount || a.Team == null) continue;
                    if (!agent.Team.IsEnemyOf(a.Team)) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= bestD2) { bestD2 = d2; nearest = a; }
                }
                if (nearest == null) return;

                Vec3 fwd = agent.LookDirection; fwd.z = 0f;
                Vec3 to  = nearest.Position - pos; to.z = 0f;
                float fl = fwd.Length, tl = to.Length;
                if (fl > 0.01f && tl > 0.01f &&
                    Vec3.DotProduct(fwd * (1f / fl), to * (1f / tl)) < 0.2f)
                    return; // foe is not ahead — hold the working this beat

                ElementSpellEffects.CastAttack(MagicElement.Fire, agent, DemonMath.RavagerCastPower);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
