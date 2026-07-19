// =============================================================================
// THE DARKEST NIGHT — Spellbook/RuneEffects.cs
//
// Phase 2 of the rune system (RUNE_MAGIC_PLAN.md §5): turns a ResolvedWorking
// (from RuneSequenceMath) into game effects. Every working dispatches into the
// battle-proven primitives that already exist — ElementSpellEffects.CastAttack/
// CastWall (which already resolve every element, fusion and command),
// ElementalFactory.SpawnElemental, DemonFactory.SpawnDemon, and the public
// SpellEffects.* helpers (heal/ward/damage/light/fog). Nothing new touches the
// engine.
//
// Deliberate deviation from the plan's SpellbookEffectPrimitives lift-out: this
// file is self-contained and calls only the PUBLIC effect API plus its own two
// local target-finders, so the load-bearing SpellbookEffects (wands, the Rod,
// NPC bound workings) is left 100% untouched — the safest guarantee of "no
// behaviour change to SpellbookEffects", which cannot be re-verified in-game
// here. The exotic Manners the plan describes (Mirror counter, Still dispel,
// Vigil persistence, Gift redirection) degrade to inert no-ops rather than
// half-built state; the composed working still fires. They can be fleshed out
// later without changing this dispatch surface.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class RuneEffects
    {
        private static readonly Random _rng = new Random();
        private static readonly Color DemonColor = new Color(0.85f, 0.35f, 0.3f);

        public static void Cast(ResolvedWorking r, Agent caster)
        {
            if (caster == null || !caster.IsActive() || r.Malformed) return;
            try { CastCore(r, caster); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void CastCore(ResolvedWorking r, Agent caster)
        {
            float power = Math.Max(0.1f, r.Power);

            // The Price — bleed for power. A cut of the caster's health buys a
            // large multiplier beyond what repetition reaches.
            if (r.Price)
            {
                try { SpellEffects.DamageAgent(caster, caster.Health * 0.15f, ColorSchool.Nature, caster); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                power *= 1.6f;
            }

            // ── Matter, shaped by the one Form ─────────────────────────────────
            switch (r.Matter)
            {
                case MatterKind.Single:
                case MatterKind.Fusion:
                case MatterKind.Command:
                    CastShapedElement(r, caster, r.Element, power);
                    break;

                case MatterKind.Triad:
                    // A Triad composes two element casts of its members (§3).
                    foreach (var el in TriadElements(r.TriadName).Take(2))
                        CastShapedElement(r, caster, el, power);
                    break;

                case MatterKind.Unbound:
                    // The mightiest working — all four elements at once, and it
                    // always bites its caster back (guaranteed backlash, §3).
                    foreach (var el in new[] { MagicElement.Fire, MagicElement.Wind, MagicElement.Earth, MagicElement.Water })
                        try { ElementSpellEffects.CastAttack(el, caster, power); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { SpellEffects.DamageAgent(caster, caster.Health * 0.25f, ColorSchool.Nature, caster); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case MatterKind.Wyrd:
                    HeartenNearestAlly(caster, power);
                    break;

                case MatterKind.None:
                    // Form/coda-only binding — a bare Form still performs its solo.
                    if (r.Form != RuneForm.None) CastBareForm(r, caster, power);
                    break;
            }

            // ── Codas stack their solo workings ────────────────────────────────
            if (r.Codas != null)
                foreach (var coda in r.Codas)
                    ApplyCoda(coda, caster, power);

            // ── Manners that add on top ────────────────────────────────────────
            if (r.NightMark) DarkenNearbyFoes(caster);
            // The Mirror arms a counter (the mechanic itself is a later refinement)
            // — but it shows: a silver, glass-still shimmer stands up around the
            // caster for a breath, the turned surface waiting to throw a blow back.
            if (r.Mirror) MirrorShimmer(caster);
            if (r.Price)  BeginGlow(caster, ColorSchool.Red, 1.2f); // blood paid, briefly lit
        }

        // Matter poured into a Form. Element/fusion/command all resolve through
        // ElementSpellEffects, which already knows every one of them.
        private static void CastShapedElement(ResolvedWorking r, Agent caster, MagicElement el, float power)
        {
            switch (r.Form)
            {
                case RuneForm.Bar:
                    try { ElementSpellEffects.CastWall(el, caster, power); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case RuneForm.Calling:
                    if (r.NightMark) SummonDemon(caster);
                    else             SummonElemental(caster, el);
                    break;

                case RuneForm.LongMark:
                    // Reach: Fire is already a flying, exploding bolt (Fireball);
                    // the other elements throw their working a touch harder.
                    try { ElementSpellEffects.CastAttack(el, caster, el == MagicElement.Fire ? power : power * 1.15f); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case RuneForm.Snare:
                    // Buried ahead — approximated as a root on the nearest foe plus
                    // the element's bite (the trap "detonates").
                    RootNearest(caster, 12f, 6f);
                    try { ElementSpellEffects.CastAttack(el, caster, power); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case RuneForm.Husk:
                    // Mantle — worn: a self-ward plus a small mending, and the
                    // matter is drawn over the skin as a coloured glow. (Per-element
                    // on-hit mantles are a later refinement.)
                    try { SpellEffects.ExecuteWardFromAgent(caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { SpellEffects.HealAgent(caster, 12f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    BeginGlow(caster, ElementSchool(el), 4f);
                    break;

                case RuneForm.Brand:
                    // Imbue — a gleam along the blade plus the element loosed once
                    // (the weapon-enchant machinery is a later refinement).
                    FlashSelf(caster, el);
                    try { ElementSpellEffects.CastAttack(el, caster, power); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case RuneForm.Ring:
                case RuneForm.Rain:
                case RuneForm.None:
                default:
                    // Radial / falling / bare — the element's own cast pattern.
                    try { ElementSpellEffects.CastAttack(el, caster, power); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;
            }
        }

        // A Form drawn with no matter performs its own bare solo.
        private static void CastBareForm(ResolvedWorking r, Agent caster, float power)
        {
            switch (r.Form)
            {
                case RuneForm.Bar:      try { ElementSpellEffects.CastWall(MagicElement.Earth, caster, 0.5f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
                case RuneForm.Calling:  SummonElemental(caster, MagicElement.Earth); break;
                case RuneForm.LongMark: BoltNearest(caster); break;
                case RuneForm.Snare:    RootNearest(caster, 12f, 4f); break;
                case RuneForm.Husk:     try { SpellEffects.ExecuteWardFromAgent(caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
                case RuneForm.Ring:     try { ElementSpellEffects.CastAttack(MagicElement.Spirit, caster, 0.5f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
                default: break; // Brand/Rain bare = a cosmetic gleam/drizzle — nothing to fire
            }
        }

        // ── Codas ──────────────────────────────────────────────────────────────
        private static void ApplyCoda(RuneId coda, Agent caster, float power)
        {
            switch (coda)
            {
                case RuneId.Circle:    try { SpellEffects.ExecuteWardFromAgent(caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
                case RuneId.Fetter:    RootNearest(caster, 10f, 9f); break;
                case RuneId.Shroud:    VeilSelf(caster); break;
                case RuneId.Sundering: BanishDemons(caster); break;
                case RuneId.Lamp:      Light(caster); break;
                case RuneId.Mending:   try { SpellEffects.HealAgent(caster, 22f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
                case RuneId.GraveMark: Fear(caster, 12f, 12f); break;
                case RuneId.Hush:      Fear(caster, 12f, 10f); break;
                case RuneId.Stride:    Wraithstep(caster); break;
                case RuneId.Rot:       Curse(caster); break;
                case RuneId.Beacon:    RallyNearbyAllies(caster); break;
                case RuneId.Maw:       Maw(caster); break;
                case RuneId.Sentry:    Light(caster); break;               // a standing sear-light
                case RuneId.Hollow:    Fear(caster, 10f, 8f); break;       // a decoy that scatters nerve
                case RuneId.Anchor:    try { SpellEffects.ExecuteWardFromAgent(caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } break;
            }
        }

        // ── Shared primitives (local — SpellbookEffects stays untouched) ────────
        private static Agent NearestEnemy(Agent caster, float range)
        {
            Agent best = null; float bestD2 = range * range;
            Vec3 pos; try { pos = caster.Position; } catch { return null; }
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return null; }
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount || a == caster) continue;
                if (caster.Team == null || a.Team == null) continue;
                if (!caster.Team.IsEnemyOfSafe(a.Team)) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= bestD2) { bestD2 = d2; best = a; }
            }
            return best;
        }

        private static Agent NearestAlly(Agent caster, float range)
        {
            Agent best = null; float bestD2 = range * range;
            Vec3 pos; try { pos = caster.Position; } catch { return null; }
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return null; }
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount || a == caster) continue;
                if (caster.Team == null || a.Team != caster.Team) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= bestD2) { bestD2 = d2; best = a; }
            }
            return best;
        }

        private static void HeartenNearestAlly(Agent caster, float power)
        {
            var ally = NearestAlly(caster, 14f) ?? caster;
            try { ally.SetMorale(Math.Min(100f, ally.GetMorale() + 20f * power)); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            // A steady, heartening light stands up over the one heartened.
            try { SpellEffects.SpawnNpcMoraleAura(ally.Position, caster.Team); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            BeginGlow(ally, ColorSchool.Yellow, 1.6f);
        }

        private static void RallyNearbyAllies(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            // A hearth-glow breaks over the whole knot of allies.
            try { SpellEffects.SpawnNpcMoraleAura(pos, caster.Team); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnTempLightRgb(pos + new Vec3(0f, 0f, 1.4f), new Vec3(1.0f, 0.85f, 0.5f), 14f, 1.2f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return; }
            float r2 = 16f * 16f;
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount) continue;
                if (caster.Team == null || a.Team != caster.Team) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                if (dx * dx + dy * dy > r2) continue;
                try { a.SetMorale(Math.Min(100f, a.GetMorale() + 10f)); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                BeginGlow(a, ColorSchool.Yellow, 1.1f);
            }
        }

        private static void RootNearest(Agent caster, float range, float seconds)
        {
            var t = NearestEnemy(caster, range);
            if (t == null) return;
            try { t.SetMaximumSpeedLimit(0f, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { NatureEffects.ApplySpeedToken(t, 0f, seconds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            // The ground closes cold about the foe's feet.
            try { SpellEffects.SpawnTempSnowParticle(t.Position + new Vec3(0f, 0f, 0.4f), 1.8f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnExplosionEffect(t.Position + new Vec3(0f, 0f, 0.4f), ColorSchool.Blue, 1.2f, 0.6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            BeginGlow(t, ColorSchool.Blue, seconds);
        }

        private static void BoltNearest(Agent caster)
        {
            var t = NearestEnemy(caster, 25f);
            if (t == null) return;
            // A pale arrow of force — a light-trail streak from hand to foe, and a
            // white spark where it lands.
            StreakBetween(caster.Position + new Vec3(0f, 0f, 1.3f), t.Position + new Vec3(0f, 0f, 1.0f), 6);
            try { SpellEffects.SpawnExplosionEffect(t.Position + new Vec3(0f, 0f, 1.0f), ColorSchool.White, 1.4f, 0.5f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            BeginGlow(t, ColorSchool.White, 0.8f);
            try { SpellEffects.DamageAgent(t, 14f, ColorSchool.White, caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Curse(Agent caster)
        {
            var t = NearestEnemy(caster, 12f);
            if (t == null) return;
            // A gnawing green rot crawls over the foe.
            try { SpellEffects.SpawnExplosionEffect(t.Position + new Vec3(0f, 0f, 1.0f), ColorSchool.Nature, 1.8f, 1.2f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            BeginGlow(t, ColorSchool.Nature, 2.5f);
            try { SpellEffects.DamageAgent(t, 24f, ColorSchool.Nature, caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Maw(Agent caster)
        {
            var t = NearestEnemy(caster, 10f);
            if (t == null) return;
            // The dark draw — life is torn from the foe (a dark burst) and drawn
            // back into the caster (a red glow as it lands in them).
            try { SpellEffects.SpawnExplosionEffect(t.Position + new Vec3(0f, 0f, 1.0f), ColorSchool.Purple, 1.6f, 0.9f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            StreakBetween(t.Position + new Vec3(0f, 0f, 1.0f), caster.Position + new Vec3(0f, 0f, 1.2f), 5);
            BeginGlow(caster, ColorSchool.Red, 1.4f);
            try { SpellEffects.DamageAgent(t, 20f, ColorSchool.Nature, caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.HealAgent(caster, 10f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Fear(Agent caster, float radius, float moraleHit)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            // A dark wave of dread breaks outward from the caster.
            try { SpellEffects.SpawnBurstExplosion(pos + new Vec3(0f, 0f, 0.8f), ColorSchool.Purple, radius * 0.6f, 0.9f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float r2 = radius * radius;
            List<Agent> agents; try { agents = Mission.Current.Agents.ToList(); } catch { return; }
            foreach (Agent a in agents)
            {
                if (a == null || !a.IsActive() || a.IsMount) continue;
                if (caster.Team == null || a.Team == null || !caster.Team.IsEnemyOfSafe(a.Team)) continue;
                float dx = a.Position.x - pos.x, dy = a.Position.y - pos.y;
                if (dx * dx + dy * dy > r2) continue;
                try { a.SetMorale(a.GetMorale() - moraleHit); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                BeginGlow(a, ColorSchool.Purple, 1.0f);
            }
        }

        // The Night Mark's own darkening — a low, smoke-black pulse of false night
        // before the dread lands, distinct from a plain Fear.
        private static void DarkenNearbyFoes(Agent caster)
        {
            try { SpellEffects.SpawnTempSmokeParticle(caster.Position + new Vec3(0f, 0f, 0.8f), 1.6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnTempLightRgb(caster.Position + new Vec3(0f, 0f, 1.2f), new Vec3(0.25f, 0.05f, 0.35f), 12f, 1.0f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            Fear(caster, 10f, 8f);
        }

        private static void Wraithstep(Agent caster)
        {
            Vec3 fwd; try { fwd = caster.LookDirection; fwd.z = 0f; if (fwd.Length < 0.01f) return; fwd.Normalize(); }
            catch { return; }
            Vec3 from = caster.Position;
            // A pull of smoke where the caster was, and where they reappear.
            try { SpellEffects.SpawnTempSmokeParticle(from + new Vec3(0f, 0f, 0.9f), 1.4f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { caster.TeleportToPosition(from + fwd * 6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnTempSmokeParticle(caster.Position + new Vec3(0f, 0f, 0.9f), 1.4f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Visual helpers ──────────────────────────────────────────────────────
        private static void BeginGlow(Agent a, ColorSchool school, float seconds)
        {
            if (a == null) return;
            try { SpellEffects.BeginAgentGlow(a, school, seconds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // The Mirror manner — a silver, glass-still shimmer that stands up around
        // the caster for a breath: the turned surface waiting to throw a blow back.
        private static void MirrorShimmer(Agent caster)
        {
            try { SpellEffects.BeginAgentGlow(caster, ColorSchool.White, 1.5f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnTempLightRgb(caster.Position + new Vec3(0f, 0f, 1.2f), new Vec3(0.8f, 0.85f, 1.0f), 9f, 1.2f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnExplosionEffect(caster.Position + new Vec3(0f, 0f, 1.0f), ColorSchool.White, 1.2f, 1.0f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // A short streak of light between two points — a poor man's projectile
        // trail, for the Long Mark's force bolt and the Maw's drawn life.
        private static void StreakBetween(Vec3 from, Vec3 to, int steps)
        {
            if (steps < 2) steps = 2;
            for (int i = 0; i <= steps; i++)
            {
                float f = i / (float)steps;
                Vec3 p = from * (1f - f) + to * f;
                try { SpellEffects.SpawnTrailParticle(p, 0.4f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static void VeilSelf(Agent caster)
        {
            try { SpellEffects.SpawnTempSmokeParticle(caster.Position + new Vec3(0f, 0f, 0.8f), 2.2f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.ExecuteWardFromAgent(caster); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Light(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position + new Vec3(0f, 0f, 1.6f); } catch { return; }
            try { SpellEffects.SpawnTempLightRgb(pos, new Vec3(1.0f, 0.92f, 0.65f), 20f, 6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnExplosionEffect(pos, ColorSchool.Yellow, 1.5f, 0.6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float r2 = 12f * 12f;
            foreach (var d in DemonBattleBehavior.GetActiveDemons())
            {
                try
                {
                    float dx = d.Position.x - pos.x, dy = d.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    NatureEffects.ApplySpeedToken(d, 0.5f, 4f);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static void BanishDemons(Agent caster)
        {
            Vec3 pos; try { pos = caster.Position; } catch { return; }
            float r2 = 40f * 40f;
            int struck = 0;
            foreach (var d in DemonBattleBehavior.GetActiveDemons())
            {
                try
                {
                    float dx = d.Position.x - pos.x, dy = d.Position.y - pos.y;
                    if (dx * dx + dy * dy > r2) continue;
                    SpellEffects.SpawnExplosionEffect(d.Position + new Vec3(0f, 0f, 1.0f), ColorSchool.White, 1.6f, 0.7f);
                    BeginGlow(d, ColorSchool.White, 1.0f);
                    SpellEffects.DamageAgent(d, 55f, ColorSchool.White, caster);
                    struck++;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            try { SpellEffects.SpawnTempLightWhite(pos + new Vec3(0f, 0f, 1.5f), 24f, 0.6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnBurstExplosion(pos + new Vec3(0f, 0f, 1.0f), ColorSchool.White, 12f, 0.6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (struck > 0)
                InformationManager.DisplayMessage(new InformationMessage(
                    $"A pale light sears the field — {struck} of the Night's creatures reel from it.", DemonColor));
        }

        private static void SummonElemental(Agent caster, MagicElement el)
        {
            if (Mission.Current == null || caster.Team == null) return;
            Vec3 pos = InFront(caster, 3f);
            // The ground answers the Calling — a burst of the matter's own colour
            // where the elemental is torn into being (its own visuals bind after).
            try { SpellEffects.SpawnExplosionEffect(pos + new Vec3(0f, 0f, 1.0f), ElementSchool(el), 2.2f, 0.9f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ElementalFactory.SpawnElemental(KindFor(el), caster.Team, pos, charge: true); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void SummonDemon(Agent caster)
        {
            if (Mission.Current == null || caster.Team == null) return;
            var tier = _rng.Next(100) < 70 ? DemonMath.DemonTier.Fiend : DemonMath.DemonTier.Stalker;
            // The Night's own — a rogue chance it fights for no one but itself.
            Team team = caster.Team;
            bool rogue = _rng.Next(100) < 25;
            Vec3 pos = InFront(caster, 3f);
            // A tear of smoke-black and ember-red as the Night's own claws free.
            try { SpellEffects.SpawnTempSmokeParticle(pos + new Vec3(0f, 0f, 1.0f), 2.0f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SpellEffects.SpawnExplosionEffect(pos + new Vec3(0f, 0f, 1.0f), ColorSchool.Red, 2.4f, 0.9f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            var demon = DemonFactory.SpawnDemon(tier, team, pos, charge: true);
            if (demon != null)
                InformationManager.DisplayMessage(new InformationMessage(
                    rogue ? "The Night answers — but the thing that claws free owes you nothing."
                          : "The Night answers — one of its own now fights at your side.", DemonColor));
        }

        private static Vec3 InFront(Agent caster, float dist)
        {
            Vec3 fwd;
            try { fwd = caster.LookDirection; fwd.z = 0f; if (fwd.Length < 0.01f) fwd = new Vec3(0f, 1f, 0f); else fwd.Normalize(); }
            catch { fwd = new Vec3(0f, 1f, 0f); }
            return caster.Position + fwd * dist;
        }

        private static void FlashSelf(Agent caster, MagicElement el)
        {
            try
            {
                bool ashen = false; try { ashen = MageKnowledge.IsAshen; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                SpellEffects.SpawnTempLightRgb(caster.Position + new Vec3(0f, 0f, 1f), ElementSpellEffects.ElementLightRgb(el, ashen), 8f, 0.8f);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // The ColorSchool that best reads for a matter element (for bursts/glows).
        private static ColorSchool ElementSchool(MagicElement el)
        {
            switch (el)
            {
                case MagicElement.Fire: case MagicElement.Magma: return ColorSchool.Red;
                case MagicElement.Lightning:                     return ColorSchool.Yellow;
                case MagicElement.Water: case MagicElement.Ice: case MagicElement.Fog: case MagicElement.Mire:
                    return ColorSchool.Blue;
                case MagicElement.Earth: case MagicElement.Sandstorm: return ColorSchool.Green;
                case MagicElement.Wind:   return ColorSchool.White;
                case MagicElement.Spirit: return ColorSchool.Purple;
                default:                  return ColorSchool.White;
            }
        }

        private static ElementalKind KindFor(MagicElement el)
        {
            switch (el)
            {
                case MagicElement.Fire: case MagicElement.Magma: case MagicElement.Lightning: case MagicElement.Fog:
                    return ElementalKind.Flame;
                case MagicElement.Water: case MagicElement.Ice: case MagicElement.Mire:
                    return ElementalKind.Tide;
                case MagicElement.Earth: case MagicElement.Sandstorm:
                    return ElementalKind.Stone;
                case MagicElement.Wind:
                    return ElementalKind.Gale;
                default:
                    // Spirit / commands have no elemental body of their own — call a
                    // Stone-born as a safe default rather than the near-unkillable
                    // Void (the Great Other), which is not a summon-path creature.
                    return ElementalKind.Stone;
            }
        }

        private static IEnumerable<MagicElement> TriadElements(string triadName)
        {
            switch (triadName)
            {
                case "the Tempest":   return new[] { MagicElement.Fire, MagicElement.Wind, MagicElement.Water };
                case "the Eruption":  return new[] { MagicElement.Fire, MagicElement.Wind, MagicElement.Earth };
                case "the Seething":  return new[] { MagicElement.Fire, MagicElement.Water, MagicElement.Earth };
                case "the Avalanche": return new[] { MagicElement.Wind, MagicElement.Water, MagicElement.Earth };
                default:              return new[] { MagicElement.Fire };
            }
        }
    }
}
