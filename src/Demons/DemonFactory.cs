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
// A demon carries the troops.xml body (bare-hide gear, a cleaver, for the
// Hellsteed a horse) — DemonFactory does not invent equipment. What it adds
// is the runtime dressing: a dark hide tint (ClothingColor, below), the
// DemonVisuals shroud, and (Kindled-style) telling the body to CHARGE and
// never sit idle.
//
// ── Research note: a riderless "beast" tier on the horse skeleton ──────────
// The mod author's feedback asked to investigate whether a demon tier could
// be a genuinely quadrupedal creature — the horse skeleton as its own
// standalone fighting Agent, no rider, reading as a hunting hound rather
// than a mount. Investigated and NOT used, for two independent reasons that
// both hold even though the low-level API exists to try it:
//   1. AgentBuildData DOES expose `.Monster(Monster)` / `.Race(int)`
//      builder methods (confirmed via reflection against
//      TaleWorlds.MountAndBlade.dll — AgentBuildData.Monster(Monster) and
//      AgentBuildData.Race(Int32) both exist), so a riderless horse-skeleton
//      Agent CAN be spawned at the raw API level, bypassing a CharacterObject's
//      normal (always-human) race. But NPCCharacter/troops.xml itself has NO
//      "race" attribute at all (confirmed: it is absent from
//      Native/ModuleData/xml_attributes_to_be_identified.txt and from every
//      shipped NPCCharacter), so a data-driven "beast troop" — the pattern
//      every other demon tier uses — is not expressible in troops.xml; it
//      would require hand-building the Agent's low-level spawn call outside
//      the existing troop/equipment pipeline.
//   2. More decisively: Native/ModuleData/monsters.xml's "horse" Monster
//      (action_set="as_horse") carries NO melee-attack action bindings —
//      its <Flags> are Mountable/CanRear/CanCharge/CanWander/
//      RunsAwayWhenHit only. A horse's only "offense" in vanilla is being
//      driven into a foe by a rider's charge order; the AI never lets a
//      riderless horse initiate or land an attack, and RunsAwayWhenHit is a
//      flight response baked into the shipped Monster — the exact opposite
//      of this system's "never retreat" requirement (DemonMath/
//      DemonBattleBehavior.ReRouse). Equipping the demon's cleaver on a
//      horse-race Agent would also fail silently: the wielded weapon's
//      attack usage is resolved against the Monster's action_set, and
//      "as_horse" has no one-handed-sword entries to resolve against.
// Overriding monsters.xml's shared "horse" Monster (or forking it under a
// new id) to fix (2) would touch every mount in the game or require a
// second bespoke Monster definition — real scope, real risk, and still
// wouldn't produce a demon that fights the way this system requires. The
// Hellsteed therefore stays a mounted pair (rider + horse), and the look
// fix lands entirely in the humanoid demons + the Hellsteed's rider
// (DemonVisuals.cs, and the bare-hide equipment/body-property changes in
// troops.xml).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
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
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                int seed = _rng.Next();
                Equipment equipment = troop.FirstBattleEquipment ?? troop.Equipment;
                BodyProperties body = troop.GetBodyProperties(equipment, seed);

                // Dark hide tint — the bare-hide gear (fur_skirt / bandit_fur_a,
                // both UseTeamColor="true" in troops.xml) is tinted near-black so
                // it reads as dark hide rather than a person's dyed cloth. Skin
                // itself has no tint hook this codebase has found; the near-solid
                // BodyAlpha in DemonVisuals plus this tint is the closest "dark,
                // hide-covered creature" read achievable with zero custom textures.
                uint hide = HideTint(tier);
                var origin    = new BasicBattleAgentOrigin(troop);
                var agentData = new AgentBuildData(origin)
                    .Team(team)
                    .Controller(AgentControllerType.AI)
                    .Equipment(equipment)
                    .BodyProperties(body)
                    .Age((int)body.Age)
                    .ClothingColor1(hide)
                    .ClothingColor2(hide)
                    .InitialPosition(in pos);
                Vec2 dir = Vec2.Forward;
                agentData = agentData.InitialDirection(in dir);

                // The hulking tiers spawn on their own additive Monster entry
                // (ModuleData/monsters.xml, base_monster="human") — a genuinely
                // larger physical body capsule, not just a visual scale trick.
                // Only reachable on THIS spawn path (a Monster is fixed at build
                // time); roster-spawned demons get the visual half from
                // ApplyBeastShape in DemonBattleBehavior.OnAgentBuild.
                try
                {
                    string monsterId = DemonMath.MonsterIdFor(tier);
                    if (!string.IsNullOrEmpty(monsterId))
                    {
                        Monster hulking = MBObjectManager.Instance.GetObject<Monster>(monsterId);
                        if (hulking != null) agentData = agentData.Monster(hulking);
                    }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                Agent agent = Mission.Current.SpawnAgent(agentData, false);
                if (agent == null) return null;

                try
                {
                    float hp = DemonMath.Health(tier, DemonMath.EnvironmentVariant.Default);
                    agent.HealthLimit = hp;
                    agent.Health      = hp;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                ApplyBeastShape(agent, tier);
                DemonBattleBehavior.Register(agent, tier);
                if (charge) SetAggressive(agent, team);
                return agent;
            }
            catch { return null; }
        }

        // Near-black tint (with a whisper of each tier's ember undertone) for
        // the bare-hide equipment's UseTeamColor cloth — a dark hide, not a
        // dyed garment. Deliberately much darker than DemonVisuals' light/
        // contour colours; this only has to darken a mesh's own base texture.
        private static uint HideTint(DemonMath.DemonTier tier)
        {
            switch (tier)
            {
                case DemonMath.DemonTier.Ravager:   return 0xFF2E0C08; // charcoal with a live ember undertone
                case DemonMath.DemonTier.Stalker:   return 0xFF241010; // dried-blood dark
                case DemonMath.DemonTier.Hellsteed: return 0xFF1E1210; // low ember dark
                default:                            return 0xFF1C1614; // Fiend — near-black ash
            }
        }

        // ── The beast shape — scale + never-resting stance ─────────────────────
        // Applied once per demon, right after its Agent exists, from BOTH spawn
        // paths (SpawnDemon above; DemonBattleBehavior.OnAgentBuild for the
        // roster-spawned night tide). Two cheap, high-impact "not a person" cues:
        //   • Visual scale — Agent.SetInitialAgentScale is the engine's real
        //     skeleton-scale hook (verified by reflection against the game DLLs:
        //     private void SetInitialAgentScale(float)); it is private, so it is
        //     invoked via cached reflection, and any engine drift degrades to a
        //     logged no-op rather than a crash.
        //   • SetAgentIdleAnimationStatus(false) — the body never settles into
        //     the relaxed human idle sway between actions; it stands wrong.
        private static System.Reflection.MethodInfo _setInitialScale;
        private static bool _setInitialScaleLookedUp;

        internal static void ApplyBeastShape(Agent agent, DemonMath.DemonTier tier)
        {
            if (agent == null) return;
            try
            {
                float scale = DemonMath.VisualScale(tier);
                if (scale > 1.001f) SetAgentScale(agent, scale);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { agent.SetAgentIdleAnimationStatus(false); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── The warp — per-bone disfigurement + the snarl ──────────────────────
        // Called by DemonBattleBehavior the first tick a demon's visuals exist
        // (same lazy timing as the DemonVisuals shroud — the skeleton is
        // guaranteed built there, which OnAgentBuild cannot promise). Applies
        // the tier's DemonMath.BoneWarps through the engine's own per-bone
        // skeleton-scale channel — MBAgentVisuals.ApplySkeletonScale(Vec3,
        // float, sbyte[], Vec3[]), the exact call Native's skeleton_scales.xml
        // horse entries ride through (verified against the shipped DLLs) — and
        // sets the permanent bared-teeth facial animation. UseScaledWeapons(false)
        // keeps the wielded cleaver from ballooning with the scaled hand bones.
        internal static void ApplyBeastWarp(Agent agent, DemonMath.DemonTier tier)
        {
            if (agent == null) return;
            try
            {
                DemonMath.BoneWarp[] warps = DemonMath.BoneWarps(tier);
                var visuals = agent.AgentVisuals;
                if (warps.Length > 0 && visuals != null)
                {
                    Monster m = null;
                    try { m = agent.Monster; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    if (m != null)
                    {
                        var indices = new System.Collections.Generic.List<sbyte>(warps.Length);
                        var scales  = new System.Collections.Generic.List<Vec3>(warps.Length);
                        foreach (DemonMath.BoneWarp w in warps)
                        {
                            sbyte bone = BoneIndexFor(m, w.Part);
                            if (bone < 0) continue;
                            indices.Add(bone);
                            scales.Add(new Vec3(w.X, w.Y, w.Z));
                        }
                        if (indices.Count > 0)
                        {
                            try { visuals.UseScaledWeapons(false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            visuals.ApplySkeletonScale(Vec3.One, 0f, indices.ToArray(), scales.ToArray());
                        }
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, DemonMath.FacialAnimation(tier), true); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static sbyte BoneIndexFor(Monster m, DemonMath.BonePart part)
        {
            switch (part)
            {
                case DemonMath.BonePart.Head:       return m.HeadLookDirectionBoneIndex;
                case DemonMath.BonePart.Neck:       return m.NeckRootBoneIndex;
                case DemonMath.BonePart.SpineUpper: return m.SpineUpperBoneIndex;
                case DemonMath.BonePart.Pelvis:     return m.PelvisBoneIndex;
                case DemonMath.BonePart.LeftArm:    return m.LeftUpperArmBoneIndex;
                case DemonMath.BonePart.RightArm:   return m.RightUpperArmBoneIndex;
                case DemonMath.BonePart.MainHand:   return m.MainHandBoneIndex;
                case DemonMath.BonePart.OffHand:    return m.OffHandBoneIndex;
                default:                            return -1;
            }
        }

        // Also used for the Hellsteed's mount (DemonBattleBehavior dresses the
        // horse lazily on the first tick it exists alongside its rider) and the
        // Wolf Brothers' Jotunn-Blooded giant (BeastsOfTheNorthCampaignBehavior).
        internal static void SetAgentScale(Agent agent, float scale)
        {
            try
            {
                if (agent == null) return;
                if (!_setInitialScaleLookedUp)
                {
                    _setInitialScaleLookedUp = true;
                    _setInitialScale = typeof(Agent).GetMethod("SetInitialAgentScale",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                _setInitialScale?.Invoke(agent, new object[] { scale });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // The demon's only order: find the nearest living thing and CHARGE it.
        // No manoeuvring, no formation-holding — requirement 7c, "never
        // strategize." An enemy-side demon is dropped into its side's infantry
        // (or cavalry, for the Hellsteed) and told to charge, same as a Kindled.
        internal static void SetAggressive(Agent agent, Team team)
        {
            try { agent.SetWatchState(Agent.WatchState.Alarmed); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            bool enemySide = false;
            try { enemySide = Mission.Current.PlayerTeam == null || team != Mission.Current.PlayerTeam; }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (!enemySide) return;
            try
            {
                bool mounted = false;
                try { mounted = agent.HasMount; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                Formation form = team.GetFormation(mounted ? FormationClass.Cavalry : FormationClass.Infantry);
                if (form != null)
                {
                    try { agent.Formation = form; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { form.SetMovementOrder(MovementOrder.MovementOrderCharge); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
