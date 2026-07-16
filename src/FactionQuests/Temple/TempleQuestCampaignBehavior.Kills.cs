// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestCampaignBehavior.Kills.cs
//
// The kill counter toward TempleQuestMath.KillTarget (50,000).
//
// ── Attribution — why this is campaign-wide, not per-army ──────────────────
// The merged army's own membership churns constantly (a lord dies and is
// replaced, a fresh Templar joins, the leader seat itself can change — see
// .Army.cs's ReassertPermanentArmy), and the player is deliberately never a
// forced member of it at all. Cleanly attributing "was this specific kill
// struck by someone currently inside the bound army" would need per-agent
// army-membership bookkeeping this codebase has nowhere else — every other
// aggregate counter in this mod (ApocalypseCampaignBehavior's demon-lord
// escalation, DemonSpawnCampaignBehavior's living-party cap) is already
// campaign-wide, not per-faction-member. So this counts every demon killed
// ANYWHERE, by ANYONE, for as long as the Vow is bound (PhaseBound) — "the
// whole war effort's tally," not "only kills the merged host personally
// landed." This is the simpler, working choice per behaviour.md's own
// working-style guidance, and it still reads correctly in-fiction: once the
// whole Order marches as one, the war is the Order's war however any single
// kill actually lands.
//
// Hooked from MagicSystem.cs's MagicMissionBehavior.OnAgentRemoved — the
// mission-side "an agent was just killed" signal every other kill-reactive
// system (SpellEffects.ApplyDarkGiftKillEffects, RelicEffects.OnAgentKill)
// already taps. DemonBattleBehavior.IsDemon(agent) is the existing, proven
// "is this agent one of ours" check (Demons/DemonBattleBehavior.cs).
//
// Deliberately does NOT touch the kingdom/army here even if the threshold is
// crossed mid-battle — mutating Kingdom/Army state from inside a live mission
// is exactly the crash risk SoldierServiceCampaignBehavior's own header
// documents ("must NOT change the player's faction or touch armies while a
// PlayerEncounter is live"). The threshold check and the actual disbandment
// both run on the next clean daily tick instead (.Army.cs's CheckDisbandDaily,
// wired from TempleQuestCampaignBehavior.OnDailyTick).
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public sealed partial class TempleQuestCampaignBehavior
    {
        private static int _demonsKilled = 0;

        private static void SyncKillData(IDataStore store)
        {
            try { store.SyncData("TPLQ_DemonsKilled", ref _demonsKilled); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ResetKillState()
        {
            _demonsKilled = 0;
        }

        // Called from MagicSystem.cs's MagicMissionBehavior.OnAgentRemoved for
        // every agent that dies, in every mission — no-op unless the Vow is
        // currently bound and the victim is a registered demon.
        internal static void OnDemonAgentKilled(Agent affectedAgent)
        {
            try
            {
                if (_phase != PhaseBound) return;
                if (affectedAgent == null) return;
                if (!DemonBattleBehavior.IsDemon(affectedAgent)) return;

                _demonsKilled++;
                try { TempleQuestLog.Current?.LogKillProgress(TempleQuestMath.ClampedKillProgress(_demonsKilled)); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
