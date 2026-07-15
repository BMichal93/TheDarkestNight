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
using TaleWorlds.CampaignSystem;
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
            public int CastCount;      // Lord only — drives the Fire/Spirit alternation
            public Agent Mount;        // Hellsteed only — the horse, once dressed
            public bool MountDressed;  // Hellsteed only — scale + shroud applied to the horse
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

        // Generic "this mission's demons are a boss/named encounter" hook —
        // set by any system staging one (e.g. FactionQuests/WolfBrothers'
        // Great Hunt) so OnAgentBuild scales every demon in THIS mission the
        // same layering technique ApocalypseMath.DemonLordHealthMultiplier
        // uses for the Demon Lord, just opt-in and scaled down. 1f (no-op) in
        // every ordinary battle; reset at mission end.
        public static float PendingBossMultiplier = 1f;

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

        // Phase 11 — true only for the Demon Lord himself (DemonTier.Lord).
        // Consulted by SpellEffects.Combat.cs's demon-bane carve-out so he
        // resists (rather than takes bonus damage from) Requirement 20's bane
        // multiplier — see ApocalypseMath.DemonLordBaneMultiplier.
        public static bool IsBoss(Agent agent)
            => agent != null && _tierOf.TryGetValue(agent, out var tier) && tier == DemonMath.DemonTier.Lord;

        // Every demon currently alive and registered in this mission — the
        // Spellbook's Banish Demons (src/Spellbook/) reads this to find its
        // targets; nothing else in the codebase needs a full enumeration, so
        // this is a thin, defensive copy rather than exposing _beings itself.
        public static List<Agent> GetActiveDemons()
        {
            var result = new List<Agent>();
            for (int i = 0; i < _beings.Count; i++)
            {
                Agent a = _beings[i]?.Agent;
                bool alive = false;
                try { alive = a != null && a.IsActive() && a.Health > 0f; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (alive) result.Add(a);
            }
            return result;
        }

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
            PendingBossMultiplier = 1f;
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
                bool matched = DemonCatalog.TryGetTier(id, out DemonMath.DemonTier tier);

                // Phase 11 — the Demon Lord is a real Hero (see DemonLordSystem),
                // so HeroCreator wraps the "demon_lord" template in a cloned
                // CharacterObject with its own generated StringId — a plain id
                // lookup above will not find him. Identify him by Hero identity
                // instead, exactly like SpellcasterLords.MissionTick does for its
                // caster lords.
                if (!matched)
                {
                    try
                    {
                        Hero hero = (agent.Character as CharacterObject)?.HeroObject;
                        if (hero != null && DemonLordSystem.IsTrackedHero(hero))
                        {
                            matched = true;
                            tier = DemonMath.DemonTier.Lord;
                        }
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                if (!matched) return;

                Register(agent, tier);
                // Environment variant (snow/desert/forest): scale HP the same way
                // ElementalBeings.ConvertBattleAgent scales a Kindled's — bumped up,
                // never down, so it never undercuts the troop's own base stats.
                try
                {
                    DemonMath.EnvironmentVariant variant = PendingVariant ?? DemonMath.EnvironmentVariant.Default;
                    float hp = DemonMath.Health(tier, variant);
                    if (tier == DemonMath.DemonTier.Lord) hp *= ApocalypseMath.DemonLordHealthMultiplier;
                    hp *= PendingBossMultiplier;
                    agent.HealthLimit = Math.Max(agent.HealthLimit, hp);
                    agent.Health = agent.HealthLimit;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The beast shape (visual scale + never-resting stance) for the
                // roster-spawned tide — DemonFactory.SpawnDemon applies the same
                // helper on its own path. (The bigger demon_hulking Monster
                // capsule is build-time only and thus factory-path only.)
                try { DemonFactory.ApplyBeastShape(agent, tier); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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
                    if (b.Mount != null)
                    {
                        try { DemonVisuals.Detach(b.Mount); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        b.Mount = null;
                    }
                    _beings.RemoveAt(i);
                    continue;
                }

                if (!b.Shrouded)
                {
                    try { DemonVisuals.Attach(b.Agent, b.Tier); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    b.Shrouded = DemonVisuals.IsShrouded(b.Agent);
                    // The warp (per-bone disfigurement + the permanent snarl)
                    // rides the same lazy first-tick timing as the shroud —
                    // the skeleton is guaranteed built once Attach succeeds.
                    if (b.Shrouded)
                        try { DemonFactory.ApplyBeastWarp(b.Agent, b.Tier); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
                else
                {
                    try { DemonVisuals.Follow(b.Agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                // The Hellsteed's horse is a demon too — dressed lazily the
                // first tick it exists alongside its rider (MountAgent can be
                // null at OnAgentBuild time), same one-time-bind discipline as
                // the rider's own shroud: an unnaturally large, smouldering
                // beast, not an old nag carrying a monster.
                if (b.Tier == DemonMath.DemonTier.Hellsteed) TickMount(b);

                // Requirement 7a/7b/7c: never retreat, never escape, never
                // strategize — reassert the charge order so nothing (a reformed
                // line, a lost target, morale AI) ever lets a demon fall back.
                // The same cadence reasserts the tier's unnatural gait — the
                // engine's speed-limit hook decays (see NatureEffects), so a
                // one-shot multiplier at spawn would quietly wear off.
                if (reAggro)
                {
                    ReRouse(b.Agent);
                    try
                    {
                        float speed = DemonMath.SpeedMultiplier(b.Tier);
                        if (speed != 1f) b.Agent.SetMaximumSpeedLimit(speed, true);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                if (DemonMath.CastsMagic(b.Tier))
                {
                    b.CastTimer -= dt;
                    if (b.CastTimer <= 0f)
                    {
                        b.CastTimer = b.Tier == DemonMath.DemonTier.Lord
                            ? DemonMath.LordCastCooldownSeconds
                            : DemonMath.RavagerCastCooldownSeconds;
                        TryLooseWorking(b);
                    }
                }
            }
        }

        // Scale + shroud the Hellsteed's horse once, then keep its shroud
        // following. The horse rides through DemonVisuals untouched — the
        // wreath-bone lookup reads the agent's own Monster bone map, so the
        // same Attach works on the horse skeleton.
        private static void TickMount(Being b)
        {
            try
            {
                Agent mount = b.Agent?.MountAgent;
                if (!b.MountDressed)
                {
                    if (mount == null) return;
                    b.Mount = mount;
                    b.MountDressed = true;
                    try { DemonFactory.SetAgentScale(mount, DemonMath.HellsteedMountScale); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    try { DemonVisuals.Attach(mount, b.Tier); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    return;
                }
                if (b.Mount == null) return;
                bool mountAlive = false;
                try { mountAlive = b.Mount.IsActive() && b.Mount.Health > 0f; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (!mountAlive)
                {
                    try { DemonVisuals.Detach(b.Mount); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    b.Mount = null;
                    return;
                }
                try { DemonVisuals.Follow(b.Mount); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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

        // A Ravager looses a cone of hellfire at the nearest foe within reach —
        // reuses ElementSpellEffects.CastAttack exactly as ElementalBeings does
        // for the Kindled, just gated to the casting tiers and a shorter, harsher
        // cooldown ("some with magical attacks"). The Lord alternates hellfire
        // with a Spirit nova (DemonMath.LordCastPatternIndex) — a boss that
        // commands more than one working, whose panic-wave reads as the Night
        // itself pressing in.
        private const float CastRangeMetres = 11f;

        private static void TryLooseWorking(Being b)
        {
            Agent agent = b?.Agent;
            MagicElement element = MagicElement.Fire;
            float power = DemonMath.RavagerCastPower;
            if (b != null && b.Tier == DemonMath.DemonTier.Lord)
            {
                element = DemonMath.LordCastPatternIndex(b.CastCount) == 0 ? MagicElement.Fire : MagicElement.Spirit;
                power = DemonMath.LordCastPower;
                b.CastCount++;
            }
            TryLooseAttack(agent, element, power);
        }

        private static void TryLooseAttack(Agent agent, MagicElement element, float power)
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

                ElementSpellEffects.CastAttack(element, agent, power);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
