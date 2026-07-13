// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterTroops.cs
//
// Requirement 15's battle half: any live Hollow Choir troop agent
// (SpellcasterTroopCatalog.IsHollowChoirTroop) casts its tier's 2-3 known
// spells on a cooldown, exactly as SpellcasterLords does for the rare
// caster lords — same SpellbookEffects.Cast(SpellId, Agent) choke point,
// same "sheathe first" courtesy (SpellEffects.TryFreeHandForCast) — but
// keyed per-Agent instead of per-Hero, since many copies of the same troop
// id can exist on a field at once.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public static class SpellcasterTroops
    {
        private const float CastCooldownSeconds = 20f;

        private static readonly Dictionary<Agent, float> _cooldowns = new Dictionary<Agent, float>();
        private static readonly Random _rng = new Random();

        public static void ClearBattleState() => _cooldowns.Clear();

        public static void MissionTick(float dt)
        {
            try
            {
                if (Mission.Current == null || !Mission.Current.AllowAiTicking) return;
                if (!SpellEffects.IsBattleMission()) return;

                foreach (var key in _cooldowns.Keys.ToList())
                {
                    _cooldowns[key] -= dt;
                    if (_cooldowns[key] <= 0f) _cooldowns.Remove(key);
                }

                List<Agent> agents;
                try { agents = Mission.Current.Agents.ToList(); }
                catch { return; }

                foreach (Agent agent in agents)
                {
                    if (!agent.IsActive() || agent.IsMount || agent.IsHero) continue;
                    string troopId = (agent.Character as CharacterObject)?.StringId;
                    if (troopId == null || !SpellcasterTroopCatalog.TryGetTier(troopId, out var tier)) continue;
                    if (_cooldowns.ContainsKey(agent)) continue;
                    if (SpellEffects.EnemiesOf(agent).Count == 0) continue;

                    SpellEffects.TryFreeHandForCast(agent);
                    SpellId pick = tier.Spells[_rng.Next(tier.Spells.Length)];
                    _cooldowns[agent] = CastCooldownSeconds;
                    SpellbookEffects.Cast(pick, agent);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
