// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodAttunementLordAI.cs
//
// Bloodbound lords get the SAME blood-attunement mechanic the player does
// (mod-author-directed addition, Constraint 5). Seeding mirrors
// SpellcasterLords.SeedIfNeeded's technique — a randomly-picked subset of a
// population, rolled once — with one crucial difference: because
// BloodAttunement.LearnElement carries PERMANENT side effects (relation
// hits, a stat/behaviour penalty), a lord must be seeded EXACTLY ONCE, EVER,
// not re-rolled fresh every session like SpellcasterLords' mission-scoped
// population. The HasAnyElement guard in SeedIfNeeded is what makes that
// safe: a lord who already carries an attunement (from a previous session,
// restored via BloodboundCampaignBehavior's SyncData) is skipped.
//
// Battle casting mirrors SpellcasterLords.MissionTick's dispatch pattern —
// a cooldown-gated loop over live agents — but additionally respects the
// day/night gate (BloodAttunementMath.IsUsableHour) the player's own input
// answers to: a Bloodbound lord's blood sleeps in daylight exactly like the
// player's.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class BloodAttunementLordAI
    {
        private static bool _seeded;
        private static readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private static readonly Random _rng = new Random();

        public static void ResetForNewGame()
        {
            _seeded = false;
            _cooldowns.Clear();
        }

        public static void ClearCooldowns() => _cooldowns.Clear();

        private static void SeedIfNeeded()
        {
            if (_seeded) return;
            _seeded = true;
            try
            {
                var lords = Hero.AllAliveHeroes
                    .Where(h => h != null && h != Hero.MainHero && h.IsAlive && h.IsLord
                             && BloodboundCulture.IsBloodboundLord(h))
                    .ToList();

                foreach (Hero lord in lords)
                {
                    try
                    {
                        if (BloodAttunement.HasAnyElement(lord)) continue; // already attuned from a prior session

                        int count = BloodAttunementMath.PickElementCount(_rng.NextDouble());
                        var pool = BloodAttunement.AttunableElements.ToList();
                        Shuffle(pool);

                        for (int i = 0; i < count && i < pool.Count; i++)
                            BloodAttunement.LearnElement(lord, pool[i], _rng, out _);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Driven from MagicMissionBehavior.OnMissionTick, alongside
        // SpellcasterLords.MissionTick's own equivalent call.
        public static void MissionTick(float dt)
        {
            try
            {
                SeedIfNeeded();
                if (Mission.Current == null || !Mission.Current.AllowAiTicking) return;
                if (!SpellEffects.IsBattleMission()) return;

                float hour = 12f;
                try { hour = (float)CampaignTime.Now.CurrentHourInDay; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (!BloodAttunementMath.IsUsableHour(hour)) return; // the blood sleeps in daylight, lords included

                foreach (string key in _cooldowns.Keys.ToList())
                {
                    _cooldowns[key] -= dt;
                    if (_cooldowns[key] <= 0f) _cooldowns.Remove(key);
                }

                List<Agent> agents;
                try { agents = Mission.Current.Agents.ToList(); }
                catch { return; }

                foreach (Agent agent in agents)
                {
                    if (!agent.IsActive() || agent.IsMount || !agent.IsHero || agent == Agent.Main) continue;
                    Hero hero = (agent.Character as TaleWorlds.CampaignSystem.CharacterObject)?.HeroObject;
                    if (hero == null || !BloodAttunement.HasAnyElement(hero)) continue;
                    if (_cooldowns.ContainsKey(hero.StringId)) continue;
                    TryCast(agent, hero);
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void TryCast(Agent agent, Hero hero)
        {
            try
            {
                var known = BloodAttunement.KnownElements(hero);
                if (known.Count == 0) return;
                if (SpellEffects.EnemiesOf(agent).Count == 0) return;

                MagicElement el = known[_rng.Next(known.Count)];
                _cooldowns[hero.StringId] = BloodAttunementMath.LordCastCooldownSeconds;

                if (_rng.NextDouble() < BloodAttunementMath.LordWallChance)
                    ElementSpellEffects.CastWall(el, agent, 1f);
                else
                    ElementSpellEffects.CastAttack(el, agent, 1f);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
