// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenRodEffects.cs
//
// Battle-side wiring for the Rod of the Apostle. Built on the same
// OnAgentHit pipeline TempleSigilEffects.OnAgentHit already uses (the same
// one RelicEffects.OnAgentHitAttack/OnAgentHitDefense use) — a real landed
// blow and a real blocked one, not a button press.
//
// UNLIKE TempleSigilEffects (deliberately player-only, per that file's own
// header note), the Rod is wired for ANY wielder — the mod author's brief
// grants a Rod to the player, the PriestKing, and every Chosen lord, so the
// battle effect has to actually fire for NPC lords carrying one, not only
// Agent.Main.
//
//   • On block: the wielder blocks/parries a real attack and their own
//     zealotry turns on the ally standing nearest — 50 damage to a random
//     allied unit within ChosenMath.RodEffectRadius — while the wielder is
//     healed 100 HP for holding the line. Deliberately harsh: this is a
//     cursed relic, not a benevolent one.
//   • On a landed hit: the wielder's blow instantly kills a random allied
//     unit nearby AND summons a demon at the wielder's side
//     (SpellbookEffects.Cast(SpellId.SummonDemon, wielder) — the same
//     dispatch choke point SpellcasterLords/SpellcasterTroops already cast
//     spoken formulas through). A deliberately monstrous, self-destructive-
//     to-allies item — a cursed instrument of zealotry, not a "good" weapon.
//
// Guard: the wielder is never their own "random ally" victim (AlliesOf
// already excludes the source agent), and both branches no-op gracefully
// when no other ally stands within range — the item never crashes or
// silently does nothing weird when the wielder fights alone.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class ChosenRodEffects
    {
        private static readonly Random _rng = new Random();

        public static void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
            in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, bool isMeleeHit)
        {
            if (Mission.Current == null || !isMeleeHit) return;

            // ── On a landed hit: attacker wields the Rod ────────────────────────
            try
            {
                if (affectorAgent != null && affectorAgent.IsActive() && blow.InflictedDamage > 0)
                {
                    string itemId = null;
                    try { itemId = affectorWeapon.Item?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    if (itemId == ChosenRodCatalog.RodOfApostleItemId)
                        OnLandedHit(affectorAgent);
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // ── On block: victim wields the Rod and blocks/parries the blow ────
            try
            {
                if (affectedAgent != null && affectedAgent.IsActive())
                {
                    bool blocked = false;
                    try
                    {
                        blocked = attackCollisionData.AttackBlockedWithShield
                               || attackCollisionData.CollisionResult == CombatCollisionResult.Blocked
                               || attackCollisionData.CollisionResult == CombatCollisionResult.Parried;
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                    if (blocked)
                    {
                        string itemId = null;
                        try { itemId = affectedAgent.WieldedWeapon.Item?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        if (itemId == ChosenRodCatalog.RodOfApostleItemId)
                            OnBlock(affectedAgent);
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnBlock(Agent wielder)
        {
            Agent victim = PickRandomNearbyAlly(wielder);
            if (victim != null)
            {
                try { SpellEffects.DamageAgent(victim, ChosenMath.RodOnBlockAllyDamage, ColorSchool.Red, wielder); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }

            try { SpellEffects.HealAgent(wielder, ChosenMath.RodOnBlockWielderHeal); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (victim != null && wielder == Agent.Main)
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "The Rod of the Apostle drinks conviction from the man beside you — it heals you all the same.",
                        new Color(0.55f, 0.10f, 0.10f)));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnLandedHit(Agent wielder)
        {
            Agent victim = PickRandomNearbyAlly(wielder);
            if (victim == null) return; // fight alone — no ally to sacrifice, no-op

            try { SpellEffects.KillAgent(victim, wielder); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellbookEffects.Cast(SpellId.SummonDemon, wielder); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (wielder == Agent.Main)
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "The Rod of the Apostle claims a life of your own line — and something answers the offering from below.",
                        new Color(0.55f, 0.10f, 0.10f)));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Never the wielder themselves (SpellEffects.AlliesOf already excludes
        // the source agent); skips gracefully if nobody else stands within
        // ChosenMath.RodEffectRadius.
        private static Agent PickRandomNearbyAlly(Agent wielder)
        {
            if (wielder == null || !wielder.IsActive()) return null;
            try
            {
                Vec3 pos = wielder.Position;
                float r2 = ChosenMath.RodEffectRadius * ChosenMath.RodEffectRadius;
                List<Agent> candidates = SpellEffects.AlliesOf(wielder)
                    .Where(a => a != null && a.IsActive() && !a.IsMount)
                    .Where(a =>
                    {
                        float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                        return dx * dx + dy * dy <= r2;
                    })
                    .ToList();
                if (candidates.Count == 0) return null;
                return candidates[_rng.Next(candidates.Count)];
            }
            catch { return null; }
        }
    }
}
