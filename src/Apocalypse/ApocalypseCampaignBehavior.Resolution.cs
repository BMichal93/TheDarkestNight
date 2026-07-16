// =============================================================================
// THE DARKEST NIGHT — Apocalypse/ApocalypseCampaignBehavior.Resolution.cs
//
// Requirement 33, stage 3 — the Demon Lord's weekly appearance roll (beyond
// day 1000), and the two ways the campaign can resolve once he walks:
//   • Victory — the player (or anyone) kills him in battle. His host scatters
//     back to leaderless night-tide behaviour, Phase 1 continues unchanged.
//   • Defeat — his conquest holds half of all standing settlements, or all
//     eight Phase 7 core factions have been eliminated.
// Both resolutions follow the same "narrative resolution + persisted flag"
// pattern DragonQuestSystem.Ending.cs and GreatAwakeningCampaignBehavior.
// Resolution.cs already use — Bannerlord sandbox saves have no native
// "declare victory and stop" hook, so this sets a permanent world-state flag
// and shows a grand ShowInquiry resolution, exactly like those two systems do.
// Partial of ApocalypseCampaignBehavior.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class ApocalypseCampaignBehavior
    {
        // The eight Phase 7 core-faction kingdom ids — used for the "all core
        // factions eliminated" defeat condition. Matches the ids used
        // throughout Factions/*/*.cs (culture-rename kingdoms share vanilla ids).
        private static readonly string[] CoreFactionKingdomIds =
        {
            "sturgia", "aserai", "battania", "khuzait",
            "vlandia", "empire_n", "empire_s", "empire",
        };

        private static void SyncResolutionData(IDataStore store) { /* nothing else to persist here — DemonLordSystem owns its own flags */ }
        private static void ResetResolutionForNewGame() { }

        // ── Stage 3 — the roll ───────────────────────────────────────────────────
        private void TryRollDemonLordAppearance()
        {
            int day = CurrentDay();
            if (!ApocalypseMath.IsLordEligible(day)) return;
            if (DemonLordSystem.HasAppeared) return;

            if (!ApocalypseMath.RollDemonLordAppears(_rng.NextDouble())) return;

            if (DemonLordSystem.TryAppear())
            {
                MobileParty lordParty = DemonLordSystem.CurrentParty();
                if (lordParty != null)
                    try { AbsorbGatheringIntoLord(lordParty); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Victory ──────────────────────────────────────────────────────────────
        private void TryResolveVictory(Hero victim)
        {
            if (victim == null || !DemonLordSystem.HasAppeared) return;
            if (DemonLordSystem.VictoryResolved || DemonLordSystem.DefeatResolved) return;
            if (!DemonLordSystem.IsTrackedHero(victim)) return;

            DemonLordSystem.MarkVictoryResolved();

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Long Night Breaks",

                    "It does not happen as you expected.\n\n" +
                    "There is no final word, no curse spat with a dying breath — only the enormous, wrong-jointed " +
                    "weight of it going still, and the sudden, ringing quiet where its will used to press against " +
                    "everything living.\n\n" +
                    "Across Calradia, at the same moment, every demon still standing simply... stops. The host that " +
                    "moved as one thing forgets, all at once, why it was moving. What is left of it turns back into " +
                    "what it always was underneath — scattered, leaderless, hungry things that rise at dusk and " +
                    "sink again at dawn, with no plan and no name to answer to.\n\n" +
                    "The Long Night is not over. The dark will still come, tonight and every night after. " +
                    "But it will never again come with a mind behind it.\n\n" +
                    "That is what you bought here. It is enough.",

                    true, false,
                    "It is enough.",
                    "",
                    () => { },
                    () => { }
                ), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            try { DissolveDemonLordHost(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Defeat ───────────────────────────────────────────────────────────────
        private void TickVictoryDefeatChecks()
        {
            if (!DemonLordSystem.HasAppeared) return;
            if (DemonLordSystem.VictoryResolved || DemonLordSystem.DefeatResolved) return;

            int held = DemonLordSystem.SettlementsHeldByLord();
            int total = 0;
            try { total = Settlement.All.Count(s => s != null && (s.IsTown || s.IsCastle)); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            int aliveCore = 0;
            try
            {
                aliveCore = Kingdom.All.Count(k => k != null && !k.IsEliminated
                    && CoreFactionKingdomIds.Contains(k.StringId));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            bool defeat = ApocalypseMath.IsDefeatBySettlements(held, total)
                       || ApocalypseMath.IsDefeatByElimination(aliveCore);
            if (!defeat) return;

            DemonLordSystem.MarkDefeatResolved();

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Long Night Does Not End",

                    "There is no single hour you could point to and say: here, it was lost.\n\n" +
                    "It was lost in every hall that emptied and was never refilled, every wall that fell and was " +
                    "never retaken, every dawn that came and changed nothing. The kingdoms that once quarrelled " +
                    "over grain and border-stones are gone, or as good as — and what wears their banners now does " +
                    "not remember what they meant.\n\n" +
                    "Calradia still turns. People still wake, still light fires, still bury their dead when they " +
                    "can. But the thing that came for it is not going to stop, and there is no one left with the " +
                    "strength to make it.\n\n" +
                    "The Long Night does not end. It is simply, now, the only kind of night there is.",

                    true, false,
                    "So it is.",
                    "",
                    () => { },
                    () => { }
                ), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Shared: scatter the Demon Lord's host back to leaderless demons ─────
        private static void DissolveDemonLordHost()
        {
            try
            {
                MobileParty party = DemonLordSystem.CurrentParty();
                if (party != null && party.IsActive)
                    try { TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(party.Party, null); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            // His kingdom is left to wither naturally (no lord, no heir clan of
            // its own culture to fall back on) — vanilla eventually eliminates a
            // kingdom with no living clans; we do not force it, matching the
            // rest of this codebase's preference for letting vanilla state
            // machinery resolve itself once the driving force is gone.
        }
    }
}
