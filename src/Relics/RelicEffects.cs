// =============================================================================
// THE DARKEST NIGHT — Relics/RelicEffects.cs
//
// Battle-side wiring for relics. Two activation styles, matching the two
// sources a relic's effect can be built on:
//
//   • CRYSTAL-sourced relics fire INSTANTLY on the attack-button press while
//     the relic is the WIELDED weapon — same trigger CrystalEffects uses.
//     Unlike the source crystal, a relic never burns down and never travels
//     as a missile (Embershard's projectile arc is simplified to an instant
//     point-blank burst around the caster — see CastCrystalRelic's header
//     comment for why). All magnitudes are the matching CrystalMath constant
//     times RelicMath.CrystalRelicPowerMult.
//
//   • DARK GIFT-sourced relics are passive while CARRIED (any weapon slot,
//     not necessarily drawn) — mirroring how a Dark Gift is always "on" for
//     its bearer. On-hit gifts (DarkStrike/IronVeil/SoulDrain) hook the same
//     OnAgentHit path DarkGiftBattleEffects uses; the kill gift (BloodPact)
//     hooks OnAgentRemoved; the aura gift (DreadPresence) ticks in
//     MissionTick. All magnitudes are the gift's own value times
//     RelicMath.DarkGiftRelicPowerMult.
//
// SCOPE DECISION: both activation styles are player-only for Phase 6. The
// Crystal path already needs a second NPC-facing file (CrystalBattleAI) to
// let NPCs fire crystals; building that same second file for relics — with
// no NPC relic-ownership plumbing yet anywhere in the mod — is out of scope
// for "a caster carves through demons visibly faster than a swordsman" (the
// stated acceptance target is about the PLAYER's own relic use). NPC relic
// use can be added the same way CrystalBattleAI was, later, without touching
// this file's player-facing logic.
//
// All TaleWorlds access is null-guarded and wrapped in individual try/catch.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class RelicEffects
    {
        private static readonly Random _rng = new Random();

        private static readonly EquipmentIndex[] WeaponSlots =
        {
            EquipmentIndex.Weapon0, EquipmentIndex.Weapon1,
            EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
        };

        // Edge-detect the attack button, exactly like CrystalEffects, but its
        // own flag — a player who somehow carries both a crystal and a relic
        // must not have one press double-fire both.
        private static bool _prevAttackDown = false;

        // DreadPresence-style aura cooldown.
        private static float _duskwardCooldown = 0f;
        private const float DuskwardInterval = 3f;   // same cadence as the source gift
        private const float DuskwardRange2    = 8f * 8f;

        public static void ClearBattleState()
        {
            _prevAttackDown   = false;
            _duskwardCooldown = 0f;
        }

        // ── Carried-relic lookup ────────────────────────────────────────────
        private static bool TryFindCarried(Agent agent, RelicEffectSource source, DarkGiftId? gift,
            CrystalType? crystal, out RelicDef def)
        {
            def = default;
            if (agent == null) return false;
            Equipment eq;
            try { eq = agent.SpawnEquipment; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
            if (eq == null) return false;

            foreach (var slot in WeaponSlots)
            {
                string itemId;
                try { itemId = eq.GetEquipmentFromSlot(slot).Item?.StringId; }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); continue; }
                if (!RelicCatalog.TryGetByItemId(itemId, out var candidate)) continue;
                foreach (var eff in candidate.Effects)
                {
                    if (eff.Source != source) continue;
                    if (source == RelicEffectSource.DarkGift && gift.HasValue && eff.DarkGiftEffect != gift.Value) continue;
                    if (source == RelicEffectSource.Crystal && crystal.HasValue && eff.CrystalEffect != crystal.Value) continue;
                    def = candidate;
                    return true;
                }
            }
            return false;
        }

        private static bool CarriesDarkGiftRelic(Agent agent, DarkGiftId gift, out RelicDef def)
            => TryFindCarried(agent, RelicEffectSource.DarkGift, gift, null, out def);

        // ── MissionTick: attack-button crystal-style activation + DreadPresence aura ──
        public static void MissionTick(float dt)
        {
            if (Mission.Current == null) return;

            var main = Agent.Main;
            if (main == null || !main.IsActive()) { _prevAttackDown = false; return; }

            // Crystal-style: instant activation on the attack button, only
            // while the RELIC is the wielded weapon.
            try
            {
                bool focusing = Input.IsKeyDown(InputKey.LeftAlt)
                             || Input.IsKeyDown(InputKey.ControllerLBumper);
                bool attackDown = Input.IsKeyDown(InputKey.LeftMouseButton)
                               || Input.IsKeyDown(InputKey.ControllerRTrigger);
                if (attackDown && !_prevAttackDown && !focusing)
                {
                    string itemId = null;
                    try { itemId = main.WieldedWeapon.Item?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    if (RelicCatalog.TryGetByItemId(itemId, out var def)
                        && def.Effects.Any(e => e.Source == RelicEffectSource.Crystal))
                    {
                        var eff = def.Effects.First(e => e.Source == RelicEffectSource.Crystal);
                        CastCrystalRelic(main, def, eff.CrystalEffect);
                    }
                }
                _prevAttackDown = attackDown;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // Dark Gift-style passive aura: DreadPresence relic.
            _duskwardCooldown -= dt;
            if (_duskwardCooldown <= 0f)
            {
                _duskwardCooldown = DuskwardInterval;
                if (CarriesDarkGiftRelic(main, DarkGiftId.DreadPresence, out _))
                    TickDuskwardBell(main);
            }
        }

        private static void TickDuskwardBell(Agent player)
        {
            Vec3 pos; try { pos = player.Position; } catch { return; }
            float drain = 20f * RelicMath.DarkGiftRelicPowerMult;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (a == null || !a.IsActive() || a.IsMount || a.Health <= 0f) continue;
                    if (player.Team != null && !player.Team.IsEnemyOf(a.Team)) continue;
                    Vec3 ap; try { ap = a.Position; } catch { continue; }
                    if ((ap - pos).LengthSquared > DuskwardRange2) continue;
                    try
                    {
                        float m = a.GetMorale();
                        a.SetMorale(Math.Max(m - drain, 0f));
                        SpellEffects.BeginAgentGlowRaw(a, new Color(0.25f, 0f, 0.25f).ToUnsignedInteger(), 0.5f);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Crystal-style dispatch (instant, no missile, no burndown) ───────
        private static void CastCrystalRelic(Agent caster, RelicDef def, CrystalType source)
        {
            if (caster == null || !caster.IsActive() || Mission.Current == null) return;
            float mult = RelicMath.CrystalRelicPowerMult;

            try
            {
                Vec3 at = caster.Position + new Vec3(0f, 0f, 1f);
                SpellEffects.SpawnImpactBurst(at, def.GlowColor, 1.6f);
                SpellEffects.SpawnTempLight(at, def.GlowColor, 7f, 0.8f);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            switch (source)
            {
                case CrystalType.Embershard:   CastEmbershardRelic(caster, mult);   break;
                case CrystalType.Rimeshard:    CastRimeshardRelic(caster, mult);    break;
                case CrystalType.Aegisstone:   CastAegisstoneRelic(caster, mult);   break;
                case CrystalType.Bloodstone:   CastBloodstoneRelic(caster, mult);   break;
                case CrystalType.Zephyrglass:  CastZephyrglassRelic(caster, mult);  break;
                default: break; // other crystal types are not currently bound to a relic
            }
        }

        // Embershard's relic form: no missile arc (a relic clone re-implementing
        // missile flight would duplicate CrystalEffects' TickCrystalMissile
        // wholesale for one weaker effect) — an instant point-blank fire burst
        // instead, same spirit ("shard burst: AoE fire damage"), simpler code.
        private static void CastEmbershardRelic(Agent caster, float mult)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = CrystalMath.EmberRadius * CrystalMath.EmberRadius;
            int hit = 0;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (!a.IsActive() || a.IsMount || a == caster) continue;
                    if (caster.Team != null && a.Team == caster.Team) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    SpellEffects.DamageAgent(a, CrystalMath.EmberDamage * mult, ColorSchool.Red, caster);
                    hit++;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Announce(caster, hit > 0
                ? $"The relic sears — {hit} enemies scorched."
                : "The relic sears — nothing stood close enough to burn.");
        }

        private static void CastRimeshardRelic(Agent caster, float mult)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = CrystalMath.RimeRadius * CrystalMath.RimeRadius;
            float slowMult = 1f - (1f - CrystalMath.RimeSlowMult) * mult;
            int slowed = 0;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (!a.IsActive() || a.IsMount || a == caster) continue;
                    if (caster.Team != null && a.Team == caster.Team) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    try { a.SetMaximumSpeedLimit(slowMult, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    slowed++;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Announce(caster, "The relic's frost pulse " + (slowed > 0 ? $"stills {slowed} enemies." : "finds nothing to still."));
        }

        private static void CastAegisstoneRelic(Agent caster, float mult)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            try { SpellEffects.HealAgent(caster, CrystalMath.AegisSelfHeal * mult); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float r2 = CrystalMath.AegisRadius * CrystalMath.AegisRadius;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (!a.IsActive() || a.IsMount || a == caster) continue;
                    if (caster.Team != null && a.Team == caster.Team) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    Vec3 away = (a.Position - pos);
                    away = away.LengthSquared > 0.001f ? away.NormalizedCopy() : new Vec3(1f, 0f, 0f);
                    try { NatureEffects.KnockbackAgent(a, a.Position + away * (CrystalMath.AegisKnockback * mult)); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Announce(caster, $"The relic's bulwark pulse steadies you (+{(int)(CrystalMath.AegisSelfHeal * mult)} HP).");
        }

        private static void CastBloodstoneRelic(Agent caster, float mult)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = CrystalMath.BloodRadius * CrystalMath.BloodRadius;
            float totalDealt = 0f;
            int hit = 0;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (!a.IsActive() || a.IsMount || a == caster) continue;
                    if (caster.Team != null && a.Team == caster.Team) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    float dmg = CrystalMath.BloodDamage * mult;
                    SpellEffects.DamageAgent(a, dmg, ColorSchool.Red, caster);
                    totalDealt += dmg;
                    hit++;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (totalDealt > 0f)
                try { SpellEffects.HealAgent(caster, totalDealt * CrystalMath.BloodLifestealFrac); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Announce(caster, hit > 0
                ? $"The relic's vampiric pulse strikes {hit} enemies and returns some of the toll."
                : "The relic's vampiric pulse finds nothing to take.");
        }

        private static void CastZephyrglassRelic(Agent caster, float mult)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float hasteMult = 1f + (CrystalMath.ZephyrHasteMult - 1f) * mult;
            try { caster.SetMaximumSpeedLimit(hasteMult, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float r2 = CrystalMath.ZephyrRadius * CrystalMath.ZephyrRadius;
            try
            {
                foreach (Agent a in Mission.Current.Agents.ToList())
                {
                    if (a == caster || !a.IsActive() || a.IsMount) continue;
                    if (caster.Team == null || a.Team != caster.Team) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    try { a.SetMaximumSpeedLimit(hasteMult, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Announce(caster, "The relic's quickening light lifts your step.");
        }

        // ── Dark Gift-style on-hit / on-kill hooks (player-only, see header) ─
        public static void OnAgentHitAttack(Agent victim, Agent attacker, int inflictedDamage, bool isMelee)
        {
            if (attacker != Agent.Main || victim == null || victim.IsMount || !isMelee) return;

            if (inflictedDamage > 0 && CarriesDarkGiftRelic(attacker, DarkGiftId.DarkStrike, out _))
            {
                try
                {
                    float bonus = inflictedDamage * 0.25f * RelicMath.DarkGiftRelicPowerMult;
                    SpellEffects.DamageAgent(victim, bonus);
                    SpellEffects.BeginAgentGlowRaw(victim, new Color(0.6f, 0f, 0.08f).ToUnsignedInteger(), 0.3f);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }

            if (CarriesDarkGiftRelic(attacker, DarkGiftId.SoulDrain, out _))
            {
                try
                {
                    float m = victim.GetMorale();
                    victim.SetMorale(Math.Max(m - 30f * RelicMath.DarkGiftRelicPowerMult, 0f));
                    SpellEffects.BeginAgentGlowRaw(victim, new Color(0.1f, 0f, 0.4f).ToUnsignedInteger(), 0.35f);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        public static void OnAgentHitDefense(Agent victim, Agent attacker, int inflictedDamage, bool isMelee)
        {
            if (victim != Agent.Main || attacker == null || inflictedDamage <= 0) return;

            if (CarriesDarkGiftRelic(victim, DarkGiftId.IronVeil, out _))
            {
                try
                {
                    float healBack = inflictedDamage * 0.10f * RelicMath.DarkGiftRelicPowerMult;
                    if (healBack >= 1f)
                    {
                        SpellEffects.HealAgent(victim, healBack);
                        SpellEffects.BeginAgentGlowRaw(victim, new Color(0.3f, 0.3f, 0.3f).ToUnsignedInteger(), 0.2f);
                    }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        public static void OnAgentKill(Agent killed, Agent killer)
        {
            if (killer != Agent.Main || !killer.IsActive()) return;
            if (!CarriesDarkGiftRelic(killer, DarkGiftId.BloodPact, out _)) return;
            try
            {
                SpellEffects.HealAgent(killer, 12f * RelicMath.DarkGiftRelicPowerMult);
                SpellEffects.BeginAgentGlowRaw(killer, new Color(0.7f, 0f, 0.1f).ToUnsignedInteger(), 0.4f);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Announce(Agent caster, string msg)
        {
            try { if (caster == Agent.Main) InformationManager.DisplayMessage(new InformationMessage(msg, new Color(0.8f, 0.7f, 0.4f))); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
