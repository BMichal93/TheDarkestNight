// =============================================================================
// THE DARKEST NIGHT — Demons/DemonFactory.cs
//
// Builds one demon as a real fighting Agent — the mission-time counterpart to
// Elementals/ElementalFactory.cs (read that file first; this one follows it
// almost line for line). Two callers use it:
//   • DemonBattleBehavior's OnAgentBuild hook, for every demon that walks in
//     as part of a demon party's own roster (the common case).
//   • Anything that needs to conjure ONE demon out of nothing mid-mission —
//     reserved for a later phase's "Summon Demon" spell, so that spell reuses
//     this exact factory rather than inventing a second spawn path.
//
// A demon carries the troops.xml body (torn clothes, a cleaver, for the
// Hellsteed a horse) — DemonFactory does not invent equipment. What it adds
// is the runtime dressing: the DemonVisuals shroud, and (Kindled-style)
// telling the body to CHARGE and never sit idle.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public static class DemonFactory
    {
        private static readonly Random _rng = new Random();

        // Spawns a demon of `tier` on `team` at `pos` and registers it with
        // DemonBattleBehavior for its shroud, weakness and (Ravager) hellfire
        // cast. Returns the agent, or null on any failure (never throws).
        public static Agent SpawnDemon(DemonMath.DemonTier tier, Team team, Vec3 pos, bool charge)
        {
            try
            {
                if (Mission.Current == null || team == null) return null;

                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId)
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>("mountain_bandit")
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>("looter");
                if (troop == null) return null;

                try
                {
                    float gz = pos.z;
                    Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2,
                        BodyFlags.CommonCollisionExcludeFlagsForAgent, ref gz);
                    pos.z = gz;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                int seed = _rng.Next();
                Equipment equipment = troop.FirstBattleEquipment ?? troop.Equipment;
                BodyProperties body = troop.GetBodyProperties(equipment, seed);

                var origin    = new BasicBattleAgentOrigin(troop);
                var agentData = new AgentBuildData(origin)
                    .Team(team)
                    .Controller(AgentControllerType.AI)
                    .Equipment(equipment)
                    .BodyProperties(body)
                    .Age((int)body.Age)
                    .InitialPosition(in pos);
                Vec2 dir = Vec2.Forward;
                agentData = agentData.InitialDirection(in dir);

                Agent agent = Mission.Current.SpawnAgent(agentData, false);
                if (agent == null) return null;

                try
                {
                    float hp = DemonMath.Health(tier, DemonMath.EnvironmentVariant.Default);
                    agent.HealthLimit = hp;
                    agent.Health      = hp;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                DemonBattleBehavior.Register(agent, tier);
                if (charge) SetAggressive(agent, team);
                return agent;
            }
            catch { return null; }
        }

        // The demon's only order: find the nearest living thing and CHARGE it.
        // No manoeuvring, no formation-holding — requirement 7c, "never
        // strategize." An enemy-side demon is dropped into its side's infantry
        // (or cavalry, for the Hellsteed) and told to charge, same as a Kindled.
        internal static void SetAggressive(Agent agent, Team team)
        {
            try { agent.SetWatchState(Agent.WatchState.Alarmed); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            bool enemySide = false;
            try { enemySide = Mission.Current.PlayerTeam == null || team != Mission.Current.PlayerTeam; }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            if (!enemySide) return;
            try
            {
                bool mounted = false;
                try { mounted = agent.HasMount; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                Formation form = team.GetFormation(mounted ? FormationClass.Cavalry : FormationClass.Infantry);
                if (form != null)
                {
                    try { agent.Formation = form; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    try { form.SetMovementOrder(MovementOrder.MovementOrderCharge); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
