// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellburnEffects.cs
//
// Requirement 18's spellburn table: what happens when a completed-but-wrong
// formula spellburns (SpellbookMath.RollSpellburn already decided THAT it
// happens; this file decides WHAT happens, picking uniformly among the eight
// kinds in SpellbookMath.SpellburnKind).
//
// Five are named in the prompt (burn self, immobilise, random command,
// explode, a demon appears and switches sides); three are invented in the
// same spirit (weapon-hand seizes, false night, voice tears). All are built
// from primitives the mod already exercises elsewhere (SpellEffects.DamageAgent,
// NatureEffects.ApplySpeedToken, DemonFactory.SpawnDemon) so nothing here is a
// new kind of TaleWorlds risk.
//
// DemonAppears needs its own tick (the side-switch) — mirrors the shape of
// ElementUltimates' Flight/Thrall lists: mission-scoped, cleared with the
// rest of battle state, ticked from MagicMissionBehavior.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class SpellburnEffects
    {
        private static readonly Random _rng = new Random();

        public static void Trigger(Agent caster)
        {
            if (caster == null || !caster.IsActive()) return;
            SpellbookMath.SpellburnKind kind = SpellbookMath.RollKind(_rng);
            try { Apply(kind, caster); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void Apply(SpellbookMath.SpellburnKind kind, Agent caster)
        {
            switch (kind)
            {
                case SpellbookMath.SpellburnKind.BurnSelf:       BurnSelf(caster); break;
                case SpellbookMath.SpellburnKind.Immobilise:     Immobilise(caster); break;
                case SpellbookMath.SpellburnKind.RandomCommand:  RandomCommand(caster); break;
                case SpellbookMath.SpellburnKind.Explode:        Explode(caster); break;
                case SpellbookMath.SpellburnKind.DemonAppears:   DemonAppears(caster); break;
                case SpellbookMath.SpellburnKind.WeaponSeal:     WeaponSeal(caster); break;
                case SpellbookMath.SpellburnKind.FalseNight:     FalseNight(caster); break;
                case SpellbookMath.SpellburnKind.VoiceTears:     VoiceTears(caster); break;
            }
        }

        private static void Msg(string text)
            => InformationManager.DisplayMessage(new InformationMessage(text, BurnColor));
        private static readonly Color BurnColor = new Color(0.55f, 0.15f, 0.55f);

        // ── The five listed burns ────────────────────────────────────────────
        private static void BurnSelf(Agent caster)
        {
            SpellEffects.DamageAgent(caster, SpellbookMath.BurnSelfDamage, ColorSchool.Red, caster, MagicElement.Fire);
            try { SpellEffects.SpawnTempFireParticle(caster.Position + new Vec3(0f, 0f, 0.6f), 1.4f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            Msg("The formula turns on you — the fire bites your own hand.");
        }

        private static void Immobilise(Agent caster)
        {
            try { caster.SetMaximumSpeedLimit(0f, false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            NatureEffects.ApplySpeedToken(caster, 0f, SpellbookMath.ImmobiliseSeconds);
            Msg("Your feet root to the ground — the working has you, not the other way around.");
        }

        private static void RandomCommand(Agent caster)
        {
            try
            {
                if (caster.Team == null || Mission.Current == null) return;
                var forms = caster.Team.FormationsIncludingEmpty.Where(f => f != null && f.CountOfUnits > 0).ToList();
                if (forms.Count == 0) return;
                var form = forms[_rng.Next(forms.Count)];
                switch (_rng.Next(3))
                {
                    case 0: form.SetMovementOrder(MovementOrder.MovementOrderCharge); break;
                    case 1: form.SetMovementOrder(MovementOrder.MovementOrderRetreat); break;
                    default: form.SetMovementOrder(MovementOrder.MovementOrderAdvance); break;
                }
                Msg("The formula slips loose and shouts through your own ranks instead of your enemy's.");
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void Explode(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = SpellbookMath.ExplodeRadius * SpellbookMath.ExplodeRadius;
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { agents = null; }
            if (agents != null)
                foreach (Agent a in agents)
                {
                    if (a == null || !a.IsActive() || a.IsMount) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    // "Everyone nearby" — friend or foe, the caster included; the
                    // working does not sort its blast by allegiance.
                    SpellEffects.DamageAgent(a, SpellbookMath.ExplodeDamage, ColorSchool.Red, null, MagicElement.Fire);
                }
            try { SpellEffects.SpawnBurstExplosion(pos, ColorSchool.Red, SpellbookMath.ExplodeRadius * 0.5f, 1.3f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            Msg("The formula collapses on itself — everyone nearby feels it, friend and foe alike.");
        }

        // ── The demon that appears and switches sides ────────────────────────
        private class WildDemon
        {
            public Agent Agent;
            public float Remaining;
            public float SwitchTimer;
        }
        private static readonly List<WildDemon> _wildDemons = new List<WildDemon>();

        private static void DemonAppears(Agent caster)
        {
            if (Mission.Current == null || caster.Team == null) return;
            Team side = _rng.Next(2) == 0 ? caster.Team : OppositeTeam(caster.Team);
            if (side == null) side = caster.Team;
            Vec3 fwd; try { fwd = caster.LookDirection; fwd.z = 0f; if (fwd.Length < 0.01f) fwd = new Vec3(0f, 1f, 0f); else fwd.Normalize(); }
            catch { fwd = new Vec3(0f, 1f, 0f); }
            Vec3 pos = caster.Position + fwd * 4f;
            var demon = DemonFactory.SpawnDemon(DemonMath.DemonTier.Fiend, side, pos, charge: true);
            if (demon == null) return;
            _wildDemons.Add(new WildDemon { Agent = demon, Remaining = SpellbookMath.DemonAppearSeconds, SwitchTimer = SpellbookMath.DemonSideSwitchSeconds });
            Msg("The formula tears open — something of the Night steps through, loyal to no one for long.");
        }

        private static Team OppositeTeam(Team team)
        {
            try
            {
                foreach (Team t in Mission.Current.Teams)
                    if (t != null && t.IsValid && t != team && t.IsEnemyOf(team)) return t;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return null;
        }

        public static void Tick(float dt)
        {
            if (_wildDemons.Count == 0) return;
            for (int i = _wildDemons.Count - 1; i >= 0; i--)
            {
                var w = _wildDemons[i];
                bool alive = false;
                try { alive = w.Agent != null && w.Agent.IsActive() && w.Agent.Health > 0f; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (!alive) { _wildDemons.RemoveAt(i); continue; }

                w.Remaining -= dt;
                if (w.Remaining <= 0f) { _wildDemons.RemoveAt(i); continue; } // it stays on the field, just stops switching

                w.SwitchTimer -= dt;
                if (w.SwitchTimer <= 0f)
                {
                    w.SwitchTimer = SpellbookMath.DemonSideSwitchSeconds;
                    try
                    {
                        var newTeam = OppositeTeam(w.Agent.Team);
                        if (newTeam != null && TryChangeAgentTeam(w.Agent, newTeam))
                            DemonFactory.SetAggressive(w.Agent, newTeam);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
        }

        // Same reflection approach as ElementUltimates.TryChangeAgentTeam — see
        // that method's header comment for why a settable Team property/field is
        // not part of any signature this mod has needed before now.
        private static bool TryChangeAgentTeam(Agent agent, Team team)
        {
            if (agent == null || team == null) return false;
            try
            {
                var prop = typeof(Agent).GetProperty("Team", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite) { prop.SetValue(agent, team); return true; }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try
            {
                var field = typeof(Agent).GetField("<Team>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? typeof(Agent).GetField("_team", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { field.SetValue(agent, team); return true; }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return false;
        }

        // ── The three invented burns ──────────────────────────────────────────
        private static void WeaponSeal(Agent caster)
        {
            // No confirmed "drop and destroy" API for a wielded weapon in this
            // codebase (see behaviour.md — never guess a TaleWorlds signature);
            // forcing both hands to sheathe is the closest verified primitive
            // (SpellEffects.TryFreeHandForCast already does this for NPC casts)
            // and reads the same at the table: your weapon-hand is not yours
            // to use for a moment.
            try { SpellEffects.TryFreeHandForCast(caster); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            Msg("Your weapon-hand sears shut — the blade is not yours to hold, for now.");
        }

        private static void FalseNight(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            for (int i = 0; i < 6; i++)
            {
                double ang = _rng.NextDouble() * Math.PI * 2.0;
                float dist = (float)_rng.NextDouble() * 10f;
                Vec3 p = pos + new Vec3((float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist, 1.0f);
                try { SpellEffects.SpawnTempSmokeParticle(p, SpellbookMath.FalseNightSeconds * 0.2f); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            // The false dark bolsters the very thing it should have banished —
            // nearby demons quicken while the field is blind.
            float r2 = 14f * 14f;
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { agents = null; }
            if (agents != null)
                foreach (Agent a in agents)
                {
                    if (a == null || !a.IsActive() || !DemonBattleBehavior.IsDemon(a)) continue;
                    float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    NatureEffects.ApplySpeedToken(a, 1.3f, SpellbookMath.FalseNightSeconds);
                }
            Msg("A false night falls over the field — and the dark things nearby quicken for it.");
        }

        private static void VoiceTears(Agent caster)
        {
            try
            {
                if (caster == Agent.Main)
                    MobileParty.MainParty.RecentEventsMorale -= SpellbookMath.VoiceTearsMoraleLoss;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            Msg("Your voice tears on the last word — the column heard it break.");
        }

        public static void ClearBattleState()
        {
            _wildDemons.Clear();
        }
    }
}
