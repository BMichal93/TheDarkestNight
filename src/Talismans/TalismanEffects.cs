// =============================================================================
// THE DARKEST NIGHT — Talismans/TalismanEffects.cs
//
// Battle-side wiring for the Temple's holy talismans. Passive while CARRIED
// in any of the four weapon/carry slots — never required to be the wielded
// weapon — exactly the pattern RelicEffects.TryFindCarried already uses for
// Dark Gift-sourced relics (see TalismansMath.cs's header for why that slot
// choice was reused rather than an unverified armour-slot clone).
//
// Player-only, matching RelicEffects/TempleSigilEffects' documented scope —
// no NPC item-ownership plumbing exists yet anywhere in the mod for a
// passive carried trinket.
//
//   • UnburntTongue  — read directly by SpellbookInputHandler before rolling
//     spellburn (no tick/hit hook of its own).
//   • EmberVigil     — ticks a small heal every VigilTickIntervalSeconds.
//   • SteadfastLine  — ticks a small morale gain every
//     SteadfastTickIntervalSeconds.
//   • CleansingBrand — bonus flat damage to the STRUCK target on a landed
//     melee hit, when that target is a registered demon (any weapon).
//   • LastWard       — heals back a fraction of a blocked/parried blow.
//
// All TaleWorlds access is null-guarded and wrapped in individual try/catch.
// =============================================================================

using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class TalismanEffects
    {
        private static readonly EquipmentIndex[] WeaponSlots =
        {
            EquipmentIndex.Weapon0, EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
        };

        private static float _vigilCooldown     = 0f;
        private static float _steadfastCooldown = 0f;

        public static void ClearBattleState()
        {
            _vigilCooldown     = 0f;
            _steadfastCooldown = 0f;
        }

        // ── Carried-talisman lookup — mirrors RelicEffects.TryFindCarried ──────
        public static bool CarriesTalisman(Agent agent, TalismanId id)
        {
            if (agent == null) return false;
            Equipment eq;
            try { eq = agent.SpawnEquipment; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
            if (eq == null) return false;

            foreach (var slot in WeaponSlots)
            {
                string itemId;
                try { itemId = eq.GetEquipmentFromSlot(slot).Item?.StringId; }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); continue; }
                if (TalismansCatalog.TryGetByItemId(itemId, out var def) && def.Id == id) return true;
            }
            return false;
        }

        // ── MissionTick: EmberVigil heal + SteadfastLine morale ────────────────
        public static void MissionTick(float dt)
        {
            if (Mission.Current == null) return;
            var main = Agent.Main;
            if (main == null || !main.IsActive()) return;

            _vigilCooldown -= dt;
            if (_vigilCooldown <= 0f)
            {
                _vigilCooldown = TalismansMath.VigilTickIntervalSeconds;
                if (CarriesTalisman(main, TalismanId.EmberVigil))
                {
                    try { SpellEffects.HealAgent(main, TalismansMath.VigilHealPerTick); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }

            _steadfastCooldown -= dt;
            if (_steadfastCooldown <= 0f)
            {
                _steadfastCooldown = TalismansMath.SteadfastTickIntervalSeconds;
                if (CarriesTalisman(main, TalismanId.SteadfastLine))
                {
                    try
                    {
                        float m = main.GetMorale();
                        main.SetMorale(Math.Min(m + TalismansMath.SteadfastMoraleGainPerTick, 100f));
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
        }

        // ── OnAgentHit: CleansingBrand (attacker) + LastWard (defender) ────────
        public static void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
            in Blow blow, in AttackCollisionData attackCollisionData, bool isMeleeHit)
        {
            if (Mission.Current == null || !isMeleeHit) return;

            // ── Cleansing Brand: attacker carries it, struck target is a demon ──
            try
            {
                if (affectorAgent == Agent.Main && blow.InflictedDamage > 0
                    && affectedAgent != null && affectedAgent.IsActive() && !affectedAgent.IsMount
                    && DemonBattleBehavior.IsDemon(affectedAgent)
                    && CarriesTalisman(affectorAgent, TalismanId.CleansingBrand))
                {
                    SpellEffects.DamageAgent(affectedAgent, TalismansMath.CleansingBrandBonusDamage, ColorSchool.Yellow, affectorAgent);
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // ── Last Ward: defender carries it, the blow was blocked/parried ───
            // A real block/parry drives InflictedDamage to (near) zero, so the
            // heal-back is computed off blow.BaseMagnitude — the blow's raw,
            // pre-mitigation magnitude — rather than what actually landed.
            try
            {
                if (affectedAgent == Agent.Main && affectedAgent.IsActive())
                {
                    bool blocked = false;
                    try
                    {
                        blocked = attackCollisionData.AttackBlockedWithShield
                               || attackCollisionData.CollisionResult == CombatCollisionResult.Blocked
                               || attackCollisionData.CollisionResult == CombatCollisionResult.Parried;
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                    if (blocked && CarriesTalisman(affectedAgent, TalismanId.LastWard))
                    {
                        float healBack = blow.BaseMagnitude * TalismansMath.LastWardBlockHealFrac;
                        if (healBack >= 1f)
                            try { SpellEffects.HealAgent(affectedAgent, healBack); }
                            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
