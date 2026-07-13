// =============================================================================
// THE DARKEST NIGHT — Factions/Temple/TempleSigilEffects.cs
//
// Battle-side wiring for the Holy Sigil. Built directly on the OnAgentHit
// pipeline — the same one RelicEffects.OnAgentHitAttack/OnAgentHitDefense
// use — rather than the Crystal attack-button trigger, because the brief's
// "on hit... on block" language maps onto an actual landed blow and an
// actual blocked one, not a button press. See RelicEffects.cs header for why
// this codebase already treats "carried weapon" checks this way.
//
//   • On hit: the bearer lands a real blow (with the Sigil drawn) and a small
//     burst of damage scorches DEMONS standing near the bearer — not
//     necessarily the thing that was actually struck, so the Sigil still
//     answers a landed blow against a human raider by punishing whatever
//     unclean thing stands close.
//   • On block: the bearer blocks or parries a real attack (with the Sigil
//     drawn) and their own morale is slightly restored — the Order's
//     conviction steadies them in the moment they hold the line.
//
// Player-only for this pass, matching RelicEffects' documented scope (no
// NPC-relic-ownership plumbing exists yet anywhere in the mod) — Temple
// LORDS benefit from every OTHER faction bonus (town-scoping, the pray
// menu's daily-tick precedent), but a lord actually drawing and swinging a
// specific carried weapon item is out of scope here exactly as it is for
// Phase 6 relics.
// =============================================================================

using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class TempleSigilEffects
    {
        public static void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
            in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, bool isMeleeHit)
        {
            if (Mission.Current == null || !isMeleeHit) return;

            // ── On hit: attacker wields the Sigil and lands a real blow ────────
            try
            {
                if (affectorAgent == Agent.Main && blow.InflictedDamage > 0)
                {
                    string itemId = null;
                    try { itemId = affectorWeapon.Item?.StringId; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    if (itemId == TempleSigilCatalog.HolySigilItemId)
                        ScorchNearbyDemons(affectorAgent);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // ── On block: victim wields the Sigil and blocks/parries the blow ──
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
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                    if (blocked)
                    {
                        string itemId = null;
                        try { itemId = affectedAgent.WieldedWeapon.Item?.StringId; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                        if (itemId == TempleSigilCatalog.HolySigilItemId)
                            RestoreBearerMorale(affectedAgent);
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ScorchNearbyDemons(Agent bearer)
        {
            Vec3 pos; try { pos = bearer.Position; } catch { return; }
            float r2 = TempleMath.SigilOnHitDemonRadius * TempleMath.SigilOnHitDemonRadius;
            int hit = 0;
            try
            {
                foreach (Agent a in Mission.Current.Agents)
                {
                    if (a == null || !a.IsActive() || a.IsMount || a == bearer) continue;
                    if (!DemonBattleBehavior.IsDemon(a)) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    SpellEffects.DamageAgent(a, TempleMath.SigilOnHitDemonDamage, ColorSchool.Yellow, bearer);
                    hit++;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (hit > 0)
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The Holy Sigil flares — {hit} demon(s) seared by the Light.",
                        new Color(0.90f, 0.82f, 0.42f)));
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void RestoreBearerMorale(Agent bearer)
        {
            try
            {
                float m = bearer.GetMorale();
                bearer.SetMorale(Math.Min(m + TempleMath.SigilOnBlockMoraleGain, 100f));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
