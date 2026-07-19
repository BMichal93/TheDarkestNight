// =============================================================================
// ASH AND EMBER — AshenRuins/AshenRuinSystem.cs
// State, public queries, daily/weekly ticks, NPC lord racing, and persistence.
// Menus are in AshenRuinMenus.cs. Exploration flow is in
// AshenRuinSystem.Exploration.cs, challenge implementations are in
// AshenRuinSystem.Challenges1.cs / .Challenges2.cs, and reward/grant helpers
// are in AshenRuinSystem.Rewards.cs.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public static partial class AshenRuinSystem
    {
        // ── Tuning ────────────────────────────────────────────────────────────
        // Recovery cooldown after any clear is now a randomized 30-120 day roll
        // (AshenRuinMath.RecoveryCooldownDays) rather than a fixed number — see
        // MarkCleared. The old fixed 90-day constant is retired.
        private const int LordRacingDays      = 7;
        private const float LordRaceChance    = 0.005f; // 0.5% per day per eligible ruin

        private const int AshenCrownFpBonus   = 3;     // focus pts per fragment (×3 bonus at completion)

        // ── State ─────────────────────────────────────────────────────────────
        // Ruins that were fully cleared this campaign (once per campaign floor).
        private static readonly HashSet<string> _cleared = new HashSet<string>();

        // Revisit cooldown: ruin village name → days remaining.
        private static readonly List<string> _cdKeys  = new List<string>();
        private static readonly List<int>    _cdDays  = new List<int>();

        // Dragon artifact
        private static bool _eyeFound = false;
        public  static bool EyeFound  => _eyeFound;

        // Ashen Crown fragments (3 = set complete)
        private static int _crownFragments = 0;

        // NPC lord racing
        private static string _lordRacingRuin  = null;   // ruin village name
        private static string _lordRacingId    = null;   // hero StringId
        private static int    _lordRacingDaysLeft = 0;

        // Guard spawn cooldown (ruin name → days until respawn)
        private static readonly List<string> _guardCdKeys  = new List<string>();
        private static readonly List<int>    _guardCdDays  = new List<int>();

        private static readonly Random _rng = new Random();

        // Rewards that must never be granted twice to the same player. A repeat
        // clear (cooldown expired, ruin already in _cleared) substitutes the
        // ruin's own PartialReward instead — see RunRoom / GrantReward.
        private static readonly HashSet<RewardType> _oneTimeUniqueRewards = new HashSet<RewardType>
        {
            RewardType.GrimoireFragment,
            RewardType.AncientGrimoire,
            RewardType.DragonArtifact,
            RewardType.AshenCrownFragment,
        };

        // ── Public queries ─────────────────────────────────────────────────────
        public static bool IsCleared(string villageName) => _cleared.Contains(villageName);
        public static int  ClearedCount              => _cleared.Count;

        public static bool IsOnCooldown(string villageName)
        {
            int idx = _cdKeys.IndexOf(villageName);
            return idx >= 0 && _cdDays[idx] > 0;
        }

        public static int CooldownDays(string villageName)
        {
            int idx = _cdKeys.IndexOf(villageName);
            return idx >= 0 ? _cdDays[idx] : 0;
        }

        public static bool IsContested(string villageName) =>
            _lordRacingRuin == villageName && _lordRacingDaysLeft > 0;

        public static Hero ContestedBy(string villageName)
        {
            if (!IsContested(villageName) || _lordRacingId == null) return null;
            try { return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _lordRacingId); }
            catch { return null; }
        }

        // ── External clear hook (e.g. Legion Expeditions) ───────────────────────
        // Lets an outside system (the Antiquarian Charter's expedition
        // resolution) put a ruin on cooldown exactly as if an NPC lord had
        // cleared it, without reaching into this class's private state
        // directly. Stamps the same first-cleared ledger and rolls the same
        // randomized 30-120 day recovery cooldown as any other clear.
        public static void MarkClearedByExpedition(string villageName)
        {
            try
            {
                if (string.IsNullOrEmpty(villageName)) return;
                if (!AshenRuinDefs.All.Any(r => r.VillageName == villageName)) return;
                MarkCleared(villageName);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Daily tick ────────────────────────────────────────────────────────
        public static void DailyTick()
        {
            if (!MageKnowledge.IsMage) return;

            // Decrement revisit cooldowns
            for (int i = 0; i < _cdDays.Count; i++)
                if (_cdDays[i] > 0) _cdDays[i]--;

            // Decrement guard respawn cooldowns
            for (int i = 0; i < _guardCdDays.Count; i++)
                if (_guardCdDays[i] > 0) _guardCdDays[i]--;

            // Lord racing countdown
            if (_lordRacingDaysLeft > 0)
            {
                _lordRacingDaysLeft--;
                if (_lordRacingDaysLeft == 0)
                    ResolveLordTakesRuin();
                return;
            }

            // Roll for a new lord to start racing toward a Tier 3+ ruin
            TryStartLordRace();
        }

        private static void TryStartLordRace()
        {
            if (_rng.NextDouble() >= LordRaceChance) return;

            // Pick an eligible ruin (Tier 3+, not on cooldown, not already contested).
            // _cleared is a first-cleared LEDGER only now (ruins recover over time) —
            // it no longer permanently excludes a ruin from the lord race; only an
            // active cooldown does.
            var eligible = AshenRuinDefs.All
                .Where(r => r.Tier >= RuinTier.Brutal
                         && !IsOnCooldown(r.VillageName)
                         && _lordRacingRuin != r.VillageName)
                .ToList();
            if (eligible.Count == 0) return;

            var ruin = eligible[_rng.Next(eligible.Count)];

            // Pick a mage lord that is alive and not the player
            var lords = Hero.AllAliveHeroes
                .Where(h => h.IsLord && h != Hero.MainHero && h.IsAlive
                         && ElementLordRegistry.IsElementLord(h))
                .ToList();
            if (lords.Count == 0) return;

            Hero lord = lords[_rng.Next(lords.Count)];
            _lordRacingRuin     = ruin.VillageName;
            _lordRacingId       = lord.StringId;
            _lordRacingDaysLeft = LordRacingDays + _rng.Next(4); // 7–10 days

            InformationManager.DisplayMessage(new InformationMessage(
                $"Rumour reaches you: {lord.Name} has departed toward a place of old power.",
                new Color(0.55f, 0.45f, 0.75f)));
        }

        private static void ResolveLordTakesRuin()
        {
            if (_lordRacingRuin == null) return;
            var def = AshenRuinDefs.All.FirstOrDefault(r => r.VillageName == _lordRacingRuin);
            string lordName = _lordRacingId != null
                ? (Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _lordRacingId)?.Name?.ToString() ?? "A mage lord")
                : "A mage lord";

            // The lord's clear counts exactly like a player clear: it stamps
            // _cleared (first-cleared ledger) and rolls the same randomized
            // 30-120 day recovery cooldown.
            if (def != null)
                MarkCleared(_lordRacingRuin);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{lordName} has returned from {(def?.RuinName ?? "the ruins")}. The opportunity has passed for now.",
                new Color(0.55f, 0.45f, 0.75f)));

            _lordRacingRuin  = null;
            _lordRacingId    = null;
            _lordRacingDaysLeft = 0;
        }

        // ── Weekly guard respawn ───────────────────────────────────────────────
        public static void WeeklyTick()
        {
            foreach (var def in AshenRuinDefs.All)
            {
                int idx = _guardCdKeys.IndexOf(def.VillageName);
                if (idx >= 0 && _guardCdDays[idx] > 0) continue; // still on cooldown
                SpawnGuardsForRuin(def);
            }
        }

        internal static void SpawnGuardsForRuin(RuinDef def)
        {
            try
            {
                // Respect the respawn cooldown. SpawnInitialGuards runs on every
                // OnSessionLaunched (including save loads), so without this gate a
                // fresh ambush party was spawned at every ruin on each reload,
                // stacking parties indefinitely on the campaign map.
                int cdIdx = _guardCdKeys.IndexOf(def.VillageName);
                if (cdIdx >= 0 && _guardCdDays[cdIdx] > 0) return;

                var village = Settlement.All.FirstOrDefault(s =>
                    s != null && s.IsVillage &&
                    string.Equals(s.Name?.ToString()?.Trim(), def.VillageName, StringComparison.OrdinalIgnoreCase));
                if (village == null) return;

                Vec2 pos = village.GetPosition2D;
                int tier = (int)def.Tier;
                int troops = tier switch
                {
                    1 => 12 + _rng.Next(7),
                    2 => 20 + _rng.Next(11),
                    3 => 30 + _rng.Next(16),
                    _ => 45 + _rng.Next(21),
                };
                float minStr = tier >= 3 ? 150f : 0f;
                try { CampaignMapEvents.SpawnAshenAmbushNear(pos, troops, minStr); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                // Set respawn cooldown for this ruin
                SetGuardCooldown(def.VillageName, tier >= 3 ? 10 : 7);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }


        // ── Persistence ───────────────────────────────────────────────────────
        public static void Save(IDataStore store)
        {
            try
            {
                var clearedList = _cleared.ToList();
                var cdKeys      = _cdKeys.ToList();
                var cdDays      = _cdDays.ToList();
                var gCdKeys     = _guardCdKeys.ToList();
                var gCdDays     = _guardCdDays.ToList();

                store.SyncData("AR_Cleared",  ref clearedList);
                store.SyncData("AR_CdKeys",   ref cdKeys);
                store.SyncData("AR_CdDays",   ref cdDays);
                store.SyncData("AR_EyeFound", ref _eyeFound);
                store.SyncData("AR_Crown",    ref _crownFragments);
                store.SyncData("AR_LordRuin", ref _lordRacingRuin);
                store.SyncData("AR_LordId",   ref _lordRacingId);
                store.SyncData("AR_LordDays", ref _lordRacingDaysLeft);
                store.SyncData("AR_GCdKeys",  ref gCdKeys);
                store.SyncData("AR_GCdDays",  ref gCdDays);

                if (clearedList != null)
                {
                    _cleared.Clear();
                    foreach (var s in clearedList) _cleared.Add(s);
                }
                if (cdKeys != null && cdDays != null && cdKeys.Count == cdDays.Count)
                {
                    _cdKeys.Clear(); _cdDays.Clear();
                    for (int i = 0; i < cdKeys.Count; i++) { _cdKeys.Add(cdKeys[i]); _cdDays.Add(cdDays[i]); }
                }
                if (gCdKeys != null && gCdDays != null && gCdKeys.Count == gCdDays.Count)
                {
                    _guardCdKeys.Clear(); _guardCdDays.Clear();
                    for (int i = 0; i < gCdKeys.Count; i++) { _guardCdKeys.Add(gCdKeys[i]); _guardCdDays.Add(gCdDays[i]); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _cleared.Clear();
            _cdKeys.Clear();
            _cdDays.Clear();
            _guardCdKeys.Clear();
            _guardCdDays.Clear();
            _eyeFound        = false;
            _crownFragments  = 0;
            _lordRacingRuin  = null;
            _lordRacingId    = null;
            _lordRacingDaysLeft = 0;
        }
    }
}
