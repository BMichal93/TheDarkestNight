// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestCampaignBehavior.Kills.cs
//
// The kill counter toward EmpireQuestMath.KillTarget (15,000), and the
// victory resolution once it is reached.
//
// ── Attribution — campaign-wide, exactly like the Temple's own tally ───────
// Mirrors TempleQuestCampaignBehavior.Kills.cs's own reasoning in full:
// attributing a kill to a specific Empire-controlled agent would need
// per-agent army/party bookkeeping this codebase has nowhere else, so this
// counts every demon killed ANYWHERE, by ANYONE, for as long as the war is
// declared (PhaseWar) — "the whole war effort's tally," consistent with how
// every other aggregate counter in this mod already works. Hooked from
// MagicSystem.cs's MagicMissionBehavior.OnAgentRemoved, the same choke point
// TempleQuestCampaignBehavior.OnDemonAgentKilled already uses.
//
// The threshold check and the actual victory resolution both run on the next
// clean daily tick (CheckVictoryDaily, wired from EmpireQuestCampaignBehavior.
// OnDailyTick) rather than inside the live mission — mutating quest/Kingdom
// state mid-mission is the exact crash risk SoldierServiceCampaignBehavior's
// own header documents and TempleQuestCampaignBehavior.Kills.cs's header
// already restates for this exact pattern.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AshAndEmber
{
    public sealed partial class EmpireQuestCampaignBehavior
    {
        private static int _demonsKilled = 0;

        private static void SyncKillData(IDataStore store)
        {
            try { store.SyncData("EMPQ_DemonsKilled", ref _demonsKilled); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ResetKillState()
        {
            _demonsKilled = 0;
        }

        // Called from MagicSystem.cs's MagicMissionBehavior.OnAgentRemoved for
        // every agent that dies, in every mission — no-op unless the war on the
        // demons has actually been declared and the victim is a registered demon.
        internal static void OnDemonAgentKilled(Agent affectedAgent)
        {
            try
            {
                if (_phase != PhaseWar) return;
                if (affectedAgent == null) return;
                if (!DemonBattleBehavior.IsDemon(affectedAgent)) return;

                _demonsKilled++;
                try { EmpireQuestLog.Current?.LogKillProgress(EmpireQuestMath.ClampedKillProgress(_demonsKilled)); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Victory — the kill target is reached ────────────────────────────────
        // Unlike the Temple's Order, the Empire does not disband: this is the
        // one deliberately positive Phase 12 ending (per the brief). The
        // Kingdom, its lords, and its towns simply keep existing, having proven
        // the Empire's whole founding claim correct.
        private static void CheckVictoryDaily()
        {
            if (_phase != PhaseWar) return;
            if (!EmpireQuestMath.HasReachedKillTarget(_demonsKilled)) return;
            ResolveVictory();
        }

        private static void ResolveVictory()
        {
            _phase = PhaseVictory;

            try
            {
                var empire = GetEmpireKingdom();
                Hero emperor = EmpireLeader();
                ClanRenown.Gain(emperor?.Clan ?? empire?.RulingClan, EmpireQuestMath.VictoryRenownBonus);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try { EmpireQuestLog.Current?.LogVictory(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Reunification",

                    $"Fifteen thousand. Word of the tally spreads the way good news always used to spread, before " +
                    "the Long Night — fast, and gladly. The reunified Empire has done what no divided kingdom, " +
                    "however brave, ever managed alone: it has actually WON something against the dark, in real, " +
                    "countable numbers, not just survived it another season.\n\n" +
                    "This is not the end of the war. The Emperor says so plainly, from the same walls the " +
                    "coronation was held on — there is no fewer dark left in the world's other corners for one " +
                    "kingdom's tally. But for the first time since before the world broke, the news from the " +
                    "Empire is simply good, and an Emperor's banner is why.",

                    true, false, "Long live the Emperor.", "", null, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
