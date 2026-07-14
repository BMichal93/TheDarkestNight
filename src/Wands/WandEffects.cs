// =============================================================================
// THE DARKEST NIGHT — Wands/WandEffects.cs
//
// Battle-side wiring for wands. Built on the SAME OnAgentHit pipeline
// ChosenRodEffects/RelicEffects/TempleSigilEffects already use — a real
// landed blow, not a button press or an attack-initiated event. Bannerlord's
// OnAgentHit callback only fires on landed hits (and blocked/parried ones),
// there is no cheaper "attack initiated" hook this codebase already exposes
// (see MagicSystem.cs's OnAgentHit dispatch — every existing on-attack item
// effect in this mod rides this same landed-hit callback), so "must land a
// hit to cast" is accepted here as the same honest simplification the Rod
// and Sigil already live with, not a new one invented for wands.
//
// Fires for ANY wielder (player, Tower/Chosen lords, Hollow Choir troops) —
// the mod author's brief explicitly wants NPCs to wield these.
//
//   • PLAYER: real charges (WandsMath.PlayerMaxCharges), tracked per wand
//     item id in _playerCharges — see WandsMath.cs's header for why this is
//     per-item-id rather than per-physical-instance, and WandsCampaignBehavior
//     for how the dictionary is persisted.
//   • NON-PLAYER: no charge tracking at all — WandsMath.NpcBreakChancePerUse
//     is rolled on every cast; a break marks the wielding agent's wand inert
//     for the rest of THIS mission (_brokenAgents) and, if the wielder is a
//     hero, strikes one copy from their party's item roster too (so the loss
//     is real outside the mission as well as in it).
//
// All TaleWorlds access is null-guarded and wrapped in individual try/catch.
// =============================================================================

using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public static class WandEffects
    {
        private static readonly Random _rng = new Random();

        // Battle-scoped only — cleared every mission (ClearBattleState).
        private static readonly Dictionary<int, float> _cooldowns = new Dictionary<int, float>();
        private static readonly HashSet<int> _brokenAgents = new HashSet<int>();

        // Persists across the whole campaign (player only) — see
        // WandsCampaignBehavior.SyncData for the parallel-list save wiring.
        private static readonly Dictionary<string, int> _playerCharges = new Dictionary<string, int>();

        public static void ClearBattleState()
        {
            _cooldowns.Clear();
            _brokenAgents.Clear();
        }

        // ── Persistence accessors (called from WandsCampaignBehavior) ───────
        internal static List<string> ExportChargeKeys() => new List<string>(_playerCharges.Keys);
        internal static List<int> ExportChargeVals()
        {
            var vals = new List<int>(_playerCharges.Count);
            foreach (var k in _playerCharges.Keys) vals.Add(_playerCharges[k]);
            return vals;
        }

        internal static void ImportCharges(List<string> keys, List<int> vals)
        {
            _playerCharges.Clear();
            if (keys == null || vals == null || keys.Count != vals.Count) return;
            for (int i = 0; i < keys.Count; i++)
                _playerCharges[keys[i]] = vals[i];
        }

        internal static void ResetForNewGame() => _playerCharges.Clear();

        // A wand entering the player's hands (purchase or ruin loot) tops the
        // shared pool back up to full — see WandsMath.cs header for why this
        // is a refill rather than an addition.
        internal static void RefillPlayerCharges(string wandItemId)
        {
            if (string.IsNullOrEmpty(wandItemId)) return;
            _playerCharges[wandItemId] = WandsMath.PlayerMaxCharges;
        }

        internal static int GetPlayerCharges(string wandItemId)
        {
            if (string.IsNullOrEmpty(wandItemId)) return 0;
            return _playerCharges.TryGetValue(wandItemId, out int v) ? v : 0;
        }

        // ── Battle dispatch ───────────────────────────────────────────────────
        public static void MissionTick(float dt)
        {
            if (_cooldowns.Count == 0) return;
            try
            {
                var keys = new List<int>(_cooldowns.Keys);
                foreach (int key in keys)
                {
                    float remaining = _cooldowns[key] - dt;
                    if (remaining <= 0f) _cooldowns.Remove(key);
                    else _cooldowns[key] = remaining;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public static void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
            in MissionWeapon affectorWeapon, in Blow blow, bool isMeleeHit)
        {
            if (Mission.Current == null || !isMeleeHit) return;
            if (affectorAgent == null || !affectorAgent.IsActive() || blow.InflictedDamage <= 0) return;

            try
            {
                string itemId = null;
                try { itemId = affectorWeapon.Item?.StringId; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                if (!WandsCatalog.TryGetByItemId(itemId, out var def)) return;

                int agentIndex = affectorAgent.Index;
                if (_brokenAgents.Contains(agentIndex)) return;
                if (_cooldowns.ContainsKey(agentIndex)) return; // still on cooldown

                bool isPlayer = affectorAgent == Agent.Main;
                if (isPlayer)
                {
                    if (!TryConsumePlayerCharge(def.ItemId))
                    {
                        Announce(affectorAgent, $"{def.Name} is spent — nothing answers this time.");
                        return;
                    }
                }
                else if (WandsMath.NpcWandBreaks(_rng.NextDouble()))
                {
                    _brokenAgents.Add(agentIndex);
                    TryRemoveOneFromWielderRoster(affectorAgent, def.ItemId);
                    Announce(affectorAgent, $"{def.Name} shatters in its wielder's hand.");
                    return;
                }

                _cooldowns[agentIndex] = WandsMath.CastCooldownSeconds;
                SpellbookEffects.Cast(def.Spell, affectorAgent);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static bool TryConsumePlayerCharge(string wandItemId)
        {
            if (!_playerCharges.TryGetValue(wandItemId, out int charges) || charges <= 0) return false;
            _playerCharges[wandItemId] = charges - 1;
            return true;
        }

        private static void TryRemoveOneFromWielderRoster(Agent wielder, string wandItemId)
        {
            try
            {
                Hero hero = (wielder.Character as CharacterObject)?.HeroObject;
                if (hero == null) return; // template-equipped troop — nothing to strike from a roster
                ItemRoster roster = hero.PartyBelongedTo?.ItemRoster;
                if (roster == null) return;
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(wandItemId);
                if (item == null) return;
                roster.AddToCounts(item, -1);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void Announce(Agent caster, string msg)
        {
            try { if (caster == Agent.Main) InformationManager.DisplayMessage(new InformationMessage(msg, new Color(0.6f, 0.45f, 0.85f))); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
