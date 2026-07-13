// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellbookEffects.cs
//
// Dispatches a successfully-spoken SpellId to the existing battle-effect
// plumbing — the single choke point Requirement 17's spell table promises.
// Every element attack/wall/Unbinding reuses ElementSpellEffects /
// ElementUltimates exactly as the unified element system already did; only
// Summon Demon, Banish Demons, Light and the invented spells have code of
// their own here, built from the same primitives (SpellEffects.*,
// NatureEffects.ApplySpeedToken, DemonFactory) the rest of the mod already
// uses.
//
// Light's lingering lamps and Banish Demons' single sweep both need no
// per-frame state (Light re-checks nearby demons once at cast; a true
// "lingering lamp" would need a tick list, but a demon fleeing back into it
// a second later is an acceptable simplification — see the method comment).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class SpellbookEffects
    {
        private static readonly Random _rng = new Random();

        // `power` mirrors ElementSpellEffects.CastAttack's contract: 1.0 for a
        // spoken formula (there is no charge to scale it, unlike the retired
        // element hold-and-charge input) — see MasteryScale inside CastAttack
        // for how a hero caster's level still nudges it.
        public static void Cast(SpellId id, Agent caster)
        {
            if (caster == null || !caster.IsActive()) return;
            try { CastCore(id, caster); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void CastCore(SpellId id, Agent caster)
        {
            switch (id)
            {
                // ── The five elements — same dispatch choke point as before ──
                case SpellId.Fireball:      ElementSpellEffects.CastAttack(MagicElement.Fire,  caster); break;
                case SpellId.Firewall:      ElementSpellEffects.CastWall  (MagicElement.Fire,  caster); break;
                case SpellId.GalesCall:     ElementSpellEffects.CastAttack(MagicElement.Wind,  caster); break;
                case SpellId.WindwardVeil:  ElementSpellEffects.CastWall  (MagicElement.Wind,  caster); break;
                case SpellId.StonerootStrike: ElementSpellEffects.CastAttack(MagicElement.Earth, caster); break;
                case SpellId.Thornwall:     ElementSpellEffects.CastWall  (MagicElement.Earth, caster); break;
                case SpellId.TorrentsEdge:  ElementSpellEffects.CastAttack(MagicElement.Water, caster); break;
                case SpellId.Mistwall:      ElementSpellEffects.CastWall  (MagicElement.Water, caster); break;
                case SpellId.WailingNova:   ElementSpellEffects.CastAttack(MagicElement.Spirit, caster); break;
                case SpellId.WardOfWhispers: ElementSpellEffects.CastWall (MagicElement.Spirit, caster); break;

                // ── The Unbindings — same cooldown/gate as the retired charge
                //    input; CastPlayerUltimate silently no-ops if still on
                //    cooldown, which is the correct "the working failed to
                //    answer" behaviour for a spoken long formula too. ─────────
                case SpellId.FirstFlameRemembered: ElementUltimates.CastPlayerUltimate(MagicElement.Fire,   caster); break;
                case SpellId.OnTheWingsOfTheGale:  ElementUltimates.CastPlayerUltimate(MagicElement.Wind,   caster); break;
                case SpellId.MountainsWrath:       ElementUltimates.CastPlayerUltimate(MagicElement.Earth,  caster); break;
                case SpellId.TheWeepingSky:        ElementUltimates.CastPlayerUltimate(MagicElement.Water,  caster); break;
                case SpellId.TheBentKnee:          ElementUltimates.CastPlayerUltimate(MagicElement.Spirit, caster); break;

                // ── Demon-facing workings ────────────────────────────────────
                case SpellId.SummonDemon:   CastSummonDemon(caster); break;
                case SpellId.BanishDemons:  CastBanishDemons(caster); break;
                case SpellId.Light:         CastLight(caster); break;

                // ── Invented ─────────────────────────────────────────────────
                case SpellId.SparkOfEmbers:      SpellEffects.HealAgent(caster, 18f); FlashSelf(caster, MagicElement.Fire); break;
                case SpellId.CallingOfEmbers:    SpellEffects.HealAgent(caster, 30f); FlashSelf(caster, MagicElement.Fire); break;
                case SpellId.EmberWard:          SpellEffects.HealAgent(caster, 15f); SpellEffects.ExecuteWardFromAgent(caster); break;
                case SpellId.VeilOfAsh:          CastVeilOfAsh(caster); break;
                case SpellId.Frostbind:          CastRoot(caster, 8f, 5.5f); break;
                case SpellId.BindingChant:       CastRoot(caster, 10f, 9f); break;
                case SpellId.Wraithstep:         CastWraithstep(caster); break;
                case SpellId.CursedGround:       ElementSpellEffects.CastAttack(MagicElement.Earth, caster, 0.8f); break;
                case SpellId.SilentVeil:         FlashSelf(caster, MagicElement.Wind); break;
                case SpellId.HollowCalling:      ElementSpellEffects.CastAttack(MagicElement.Spirit, caster, 0.6f); break;
                case SpellId.SunderingCry:       CastSunderingCry(caster); break;
                case SpellId.GraspingRoots:      ElementSpellEffects.CastAttack(MagicElement.Earth, caster, 1.3f); break;
                case SpellId.Tidebreaker:        ElementSpellEffects.CastAttack(MagicElement.Water, caster, 1.3f); break;
                case SpellId.TheLongSilence:     CastFear(caster, 12f); break;
                case SpellId.BonewindCurse:      CastCurse(caster); break;
                case SpellId.WardingSigil:       SpellEffects.ExecuteWardFromAgent(caster); break;
                case SpellId.CallersBane:        ElementSpellEffects.CastAttack(MagicElement.Spirit, caster, 0.5f); break;
                case SpellId.HearthlightBeacon:  CastLight(caster); FlashSelf(caster, MagicElement.Spirit); break;
                case SpellId.GraveChant:         CastFear(caster, 16f); break;
                case SpellId.TheAshenCalling:    ElementSpellEffects.CastAttack(MagicElement.Fire, caster, 1.6f); break;
                case SpellId.WidowsVeil:         CastWidowsVeil(caster); break;
                case SpellId.TheLongWard:        SpellEffects.ExecuteWardFromAgent(caster, allyRadius: 8f); break;
            }
        }

        // ── Summon Demon — Requirement 17 mandatory entry ───────────────────
        private static void CastSummonDemon(Agent caster)
        {
            if (Mission.Current == null || caster.Team == null) return;
            var tier = _rng.Next(100) < 70 ? DemonMath.DemonTier.Fiend : DemonMath.DemonTier.Stalker;
            Vec3 fwd; try { fwd = caster.LookDirection; fwd.z = 0f; if (fwd.Length < 0.01f) fwd = new Vec3(0f, 1f, 0f); else fwd.Normalize(); }
            catch { fwd = new Vec3(0f, 1f, 0f); }
            Vec3 pos = caster.Position + fwd * 3f;
            var demon = DemonFactory.SpawnDemon(tier, caster.Team, pos, charge: true);
            if (demon != null)
                InformationManager.DisplayMessage(new InformationMessage(
                    "The Night answers — one of its own now fights at your side.", DemonColor));
        }

        // ── Banish Demons — Requirement 17 mandatory entry ──────────────────
        // A single sweep of pale light: every demon on the field, whichever
        // side it stands on, is struck. Demon-bane bonus damage (Phase 6) will
        // multiply on top of this once relics exist; for now it is a flat,
        // heavy hit — deliberately not lethal on its own against the tougher
        // tiers, so it reads as a purge rather than an "I win" button.
        private const float BanishDamage = 55f;
        private const float BanishRadius = 40f;

        private static void CastBanishDemons(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = BanishRadius * BanishRadius;
            var demons = DemonBattleBehavior.GetActiveDemons();
            int struck = 0;
            foreach (var d in demons)
            {
                try
                {
                    float dx = d.Position.x - pos.x, dy = d.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    SpellEffects.DamageAgent(d, BanishDamage, ColorSchool.White, caster);
                    SpellEffects.SpawnTempLightWhite(d.Position + new Vec3(0f, 0f, 1f), 10f, 0.4f);
                    struck++;
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            SpellEffects.SpawnTempLightWhite(pos + new Vec3(0f, 0f, 1.5f), 24f, 0.6f);
            if (struck > 0)
                InformationManager.DisplayMessage(new InformationMessage(
                    $"A pale light sears the field — {struck} of the Night's creatures reel from it.", DemonColor));
        }

        // ── Light — Requirement 17 mandatory entry ───────────────────────────
        // A lasting lamp: it plants a bright point-light and, once, drives back
        // every demon caught nearby (a shove + a short fear token) — the
        // closest read of "lowers demon morale" the demon roster's always-
        // charging AI (Requirement 7c: never strategize) allows, since demons
        // have no morale stat to lower in the way a human formation does.
        private const float LightRadius = 12f;

        private static void CastLight(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position + new Vec3(0f, 0f, 1.6f); } catch { return; }
            SpellEffects.SpawnTempLightRgb(pos, new Vec3(1.0f, 0.92f, 0.65f), 20f, 6f);
            float r2 = LightRadius * LightRadius;
            var demons = DemonBattleBehavior.GetActiveDemons();
            foreach (var d in demons)
            {
                try
                {
                    float dx = d.Position.x - pos.x, dy = d.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    NatureEffects.ApplySpeedToken(d, 0.5f, 4f);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // ── Invented spells' small effects ───────────────────────────────────
        private static void CastVeilOfAsh(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            try { SpellEffects.SpawnTempSmokeParticle(pos + new Vec3(0f, 0f, 0.8f), 2.2f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            SpellEffects.ExecuteWardFromAgent(caster);
        }

        private static Agent NearestEnemy(Agent caster, float range)
        {
            Agent best = null; float bestD2 = range * range;
            Vec3 pos; try { pos = caster.Position; } catch { return null; }
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return null; }
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount || a == caster) continue;
                if (caster.Team != null && a.Team == caster.Team) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= bestD2) { bestD2 = d2; best = a; }
            }
            return best;
        }

        private static void CastRoot(Agent caster, float range, float seconds)
        {
            var target = NearestEnemy(caster, range);
            if (target == null) return;
            try { target.SetMaximumSpeedLimit(0f, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            NatureEffects.ApplySpeedToken(target, 0f, seconds);
            try { SpellEffects.SpawnTempSnowParticle(target.Position + new Vec3(0f, 0f, 0.6f), 1.6f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void CastCurse(Agent caster)
        {
            var target = NearestEnemy(caster, 12f);
            if (target == null) return;
            SpellEffects.DamageAgent(target, 24f, ColorSchool.Nature, caster);
        }

        private static void CastSunderingCry(Agent caster)
        {
            var target = NearestEnemy(caster, 10f);
            if (target == null) return;
            SpellEffects.DamageAgent(target, 12f, ColorSchool.Nature, caster);
            try { target.SetMorale(target.GetMorale() - 10f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void CastFear(Agent caster, float radius)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = radius * radius;
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return; }
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount) continue;
                if (caster.Team != null && a.Team == caster.Team) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                if (dx * dx + dy * dy > r2) continue;
                try { a.SetMorale(a.GetMorale() - 12f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                NatureEffects.ApplySpeedToken(a, 0.7f, 3f);
            }
        }

        private static void CastWraithstep(Agent caster)
        {
            Vec3 fwd; try { fwd = caster.LookDirection; fwd.z = 0f; if (fwd.Length < 0.01f) return; fwd.Normalize(); }
            catch { return; }
            try { caster.TeleportToPosition(caster.Position + fwd * 6f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.SpawnTempSmokeParticle(caster.Position + new Vec3(0f, 0f, 0.6f), 1.4f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void CastWidowsVeil(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            try { SpellEffects.SpawnFogPatch(pos, 22f, 12f, caster.Team); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void FlashSelf(Agent caster, MagicElement el)
        {
            try
            {
                bool ashen = false; try { ashen = MageKnowledge.IsAshen; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                SpellEffects.SpawnTempLightRgb(caster.Position + new Vec3(0f, 0f, 1f), ElementSpellEffects.ElementLightRgb(el, ashen), 8f, 0.8f);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static readonly Color DemonColor = new Color(0.85f, 0.35f, 0.3f);
    }
}
