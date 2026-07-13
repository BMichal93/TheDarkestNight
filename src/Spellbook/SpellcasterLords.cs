// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterLords.cs
//
// Requirement 14 — roughly 7% of named lords/companions (excluding the
// player) know 1-3 spells from the Phase 4 SpellbookCatalog and cast them in
// battle. Deliberately kept SEPARATE from ColourLordAI (which drives the old
// unified-element NPC casting and is already a large, intricate file): this
// is a second, independent population — a lord can be a colour lord, a
// spellcaster lord, both, or neither. Casting dispatches through the same
// SpellbookEffects.Cast(SpellId, Agent) choke point the player's spoken
// formulas use, so a caster lord's Fireball is mechanically identical to the
// player's.
//
// Selection is re-rolled once per campaign session (first mission tick that
// finds it unseeded) rather than persisted — matching the "seeded, not
// serialized" pattern ColourLordRegistry already uses for the mage-lord
// population. It costs nothing to reseed each session and keeps this system
// entirely free of new save-format surface.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class SpellcasterLords
    {
        private static readonly HashSet<string> _eligibleIds = new HashSet<string>();
        private static readonly Dictionary<string, List<SpellId>> _knownSpells = new Dictionary<string, List<SpellId>>();
        private static readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private static readonly List<SpellId> _emptySpells = new List<SpellId>();
        private static bool _seeded;
        private static readonly Random _rng = new Random();

        public static bool IsEligible(Hero hero) =>
            hero != null && _eligibleIds.Contains(hero.StringId);

        public static IReadOnlyList<SpellId> KnownSpells(Hero hero)
        {
            if (hero == null || !_knownSpells.TryGetValue(hero.StringId, out var list)) return _emptySpells;
            return list;
        }

        public static void ResetForNewGame()
        {
            _seeded = false;
            _eligibleIds.Clear();
            _knownSpells.Clear();
            _cooldowns.Clear();
        }

        public static void ClearCooldowns() => _cooldowns.Clear();

        // Grants a specific hero caster status outside the 7% random seeding —
        // used by TowerCampaignBehavior so a joining lord who is not already a
        // recognised spellcaster gets the Tower's equivalent of the player's
        // free spellbook unlock (SpellbookCampaignBehavior's unlock flag is
        // player-only state, so eligibility here is the lord-facing analogue).
        // Idempotent: re-granting a hero who is already eligible only tops up
        // spells they do not yet know.
        public static void GrantToHero(Hero hero, IReadOnlyList<SpellId> spells)
        {
            if (hero == null || spells == null || spells.Count == 0) return;
            try
            {
                _eligibleIds.Add(hero.StringId);
                if (!_knownSpells.TryGetValue(hero.StringId, out var list))
                {
                    list = new List<SpellId>();
                    _knownSpells[hero.StringId] = list;
                }
                foreach (var s in spells)
                    if (!list.Contains(s)) list.Add(s);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void SeedIfNeeded()
        {
            if (_seeded) return;
            _seeded = true;
            try
            {
                var pool = Hero.AllAliveHeroes
                    .Where(h => h != Hero.MainHero && h.IsAlive && (h.IsLord || h.IsPlayerCompanion))
                    .ToList();
                int target = SpellcasterLordMath.TargetCasterCount(pool.Count);
                if (target <= 0) return;
                Shuffle(pool);

                var allSpellIds = SpellbookCatalog.All.Select(d => d.Id).ToList();
                for (int i = 0; i < Math.Min(target, pool.Count); i++)
                {
                    Hero h = pool[i];
                    _eligibleIds.Add(h.StringId);
                    int count = Math.Min(SpellcasterLordMath.KnownSpellCount(_rng.Next(100)), allSpellIds.Count);
                    Shuffle(allSpellIds);
                    _knownSpells[h.StringId] = allSpellIds.Take(count).ToList();
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Driven from MagicMissionBehavior.OnMissionTick, alongside ColourLordAI's
        // own tick — same 0.5s-effective cadence is unnecessary here since this
        // cooldown is measured in whole seconds, so it ticks every frame cheaply.
        public static void MissionTick(float dt)
        {
            try
            {
                SeedIfNeeded();
                if (Mission.Current == null || !Mission.Current.AllowAiTicking) return;
                if (!SpellEffects.IsBattleMission()) return;

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
                    Hero hero = (agent.Character as CharacterObject)?.HeroObject;
                    if (hero == null || !IsEligible(hero)) continue;
                    if (_cooldowns.ContainsKey(hero.StringId)) continue;
                    TryCast(agent, hero);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void TryCast(Agent agent, Hero hero)
        {
            try
            {
                var known = KnownSpells(hero);
                if (known.Count == 0) return;
                if (SpellEffects.EnemiesOf(agent).Count == 0) return;

                SpellId pick = known[_rng.Next(known.Count)];
                _cooldowns[hero.StringId] = SpellcasterLordMath.CastCooldownSeconds;
                SpellbookEffects.Cast(pick, agent);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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
