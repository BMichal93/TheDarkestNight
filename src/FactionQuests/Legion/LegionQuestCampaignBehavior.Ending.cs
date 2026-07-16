// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Legion/LegionQuestCampaignBehavior.Ending.cs
//
// Once the ark's stock is complete AND Ortysia is still Legion's (mirrors
// NorthmenStonesCampaignBehavior.Ending.cs's own "materials done, but the
// working also needs the town still held" gate), the player is offered a
// real choice via InformationManager.ShowInquiry:
//
//   (a) SAIL — a narrative resolution, no forced process termination
//       (mirrors ApocalypseCampaignBehavior/ForestWidowsQuestCampaignBehavior.
//       Resolution.cs's "flag + inquiry" convention). The player's own clan
//       leaves the Legion kingdom if it was ever a member
//       (ChangeKingdomAction.ApplyByLeaveKingdom, matching ForestWidowsQuest
//       CampaignBehavior.Resolution.cs's ResolveCastOut and ChosenQuestCampaign
//       Behavior.Split.cs's PerformSplit) — the ark carries the player and
//       whoever chose to follow them beyond Legion's own politics entirely.
//       The campaign continues normally; _phase flips to PhaseEndedSail so
//       nothing else in this file can re-fire.
//
//   (b) STAY — a REAL leadership change. The player's clan is installed as
//       Legion's ruling clan (ChangeRulingClanAction.Apply — verified against
//       TaleWorlds.CampaignSystem.dll; joins the kingdom first via
//       ChangeKingdomAction.ApplyByJoinToKingdom if the player wasn't already
//       a member, mirroring TribalKingdomBehavior.EnforceGodKingSuccession's
//       own ChangeClanLeaderAction.ApplyWithSelectedNewLeader precedent for
//       "install a leader for real, not just narratively"). 2-3 random OTHER
//       Legion clans (never the player's own, now-ruling clan) then leave the
//       kingdom outright (ChangeKingdomAction.ApplyByLeaveKingdom) — the
//       Comrades who won't follow a Warlord who chose to stay rather than
//       finish the crossing.
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed partial class LegionQuestCampaignBehavior
    {
        private static bool _stockCompleteNotified;
        private static bool _resolutionHandled;

        private static void SyncEndingData(IDataStore store)
        {
            int notified = _stockCompleteNotified ? 1 : 0;
            store.SyncData("LEGQ_StockNotify", ref notified);
            _stockCompleteNotified = notified != 0;

            int resolved = _resolutionHandled ? 1 : 0;
            store.SyncData("LEGQ_Resolved", ref resolved);
            _resolutionHandled = resolved != 0;
        }

        private static void ResetEndingState()
        {
            _stockCompleteNotified = false;
            _resolutionHandled = false;
        }

        private void EndingDailyTick()
        {
            if (_phase != PhaseGathering) return;

            bool stockDone = LegionQuestMath.IsStockComplete(_hardwood, _iron);
            if (!stockDone) return;

            if (!IsOrtysiaLegionOwned())
            {
                if (!_stockCompleteNotified)
                {
                    _stockCompleteNotified = true;
                    try
                    {
                        MBInformationManager.AddQuickInformation(new TextObject(
                            "The ark's hold is stocked past any reasonable margin — but Ortysia is not Legion's " +
                            "to launch her from. The Warlord will not sail from a quay that answers to someone " +
                            "else's banner."));
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
                return;
            }

            if (_resolutionHandled) return;
            if (MageKnowledge._deferredInquiry != null) return;
            _resolutionHandled = true;
            MageKnowledge._deferredInquiry = ShowFinalChoice;
        }

        private static void ShowFinalChoice()
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Far Shore",

                    "The ark rides low at Ortysia's quay, every plank and fitting the Warlord asked for finally " +
                    "given. He meets you on the deck, and for once there is nothing of the raider left in his " +
                    "voice.\n\n" +
                    "\"She's ready. Past the horizon is a peace this mainland is never getting back — I mean to " +
                    "find it, or die trying, and I'll not pretend I know which. Come with me, if you've had " +
                    "enough of this land too. Or stay, and hold what's left of Legion in my place. I'll not call " +
                    "either choice weak.\"",

                    true, true,
                    "Sail with you, beyond the sea.",
                    "Stay. Legion needs a Warlord more than a ship.",
                    () => { try { ResolveSail(); }  catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    () => { try { ResolveStay(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } }
                ), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Ending (a) — sail beyond the sea ─────────────────────────────────────
        private static void ResolveSail()
        {
            try
            {
                Kingdom legion = GetLegionKingdom();
                if (legion != null && Clan.PlayerClan != null && Clan.PlayerClan.Kingdom == legion)
                    try { ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            _phase = PhaseEndedSail;

            try { LegionQuestLog.Current?.LogSailed(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(
                    "Ortysia's harbour falls away behind the ark's wake. Whatever the Long Night still means to " +
                    "do to Calradia, it will have to do it without you."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Ending (b) — stay as the new Warlord ─────────────────────────────────
        private static void ResolveStay()
        {
            Kingdom legion = GetLegionKingdom();
            if (legion == null) return;

            try
            {
                if (Clan.PlayerClan != null && Clan.PlayerClan.Kingdom != legion)
                {
                    try
                    {
                        ChangeKingdomAction.ApplyByJoinToKingdom(Clan.PlayerClan, legion, CampaignTime.Never, false);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                try { ChangeRulingClanAction.Apply(legion, Clan.PlayerClan); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            List<string> departedNames = ApplyDepartingClans(legion);

            _phase = PhaseEndedStay;

            try { LegionQuestLog.Current?.LogStayed(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            string departedText = departedNames.Count > 0
                ? " " + string.Join(", ", departedNames) + " will not follow a Warlord who stayed behind, and " +
                  "march their banners out of Legion for good."
                : "";

            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(
                    "You take the Warlord's place at Ortysia. The ark stays at the quay, ready and unused." +
                    departedText));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // 2-3 random OTHER Legion clans (never the player's own, now-ruling clan)
        // leave the kingdom outright — LegionQuestMath.DepartingClanCount.
        private static List<string> ApplyDepartingClans(Kingdom legion)
        {
            var departedNames = new List<string>();
            try
            {
                var candidates = legion.Clans
                    .Where(c => c != null && !c.IsEliminated && c != Clan.PlayerClan)
                    .OrderBy(_ => _rng.Next())
                    .ToList();

                int count = LegionQuestMath.DepartingClanCount(_rng.NextDouble());
                if (count > candidates.Count) count = candidates.Count;

                for (int i = 0; i < count; i++)
                {
                    Clan c = candidates[i];
                    try
                    {
                        ChangeKingdomAction.ApplyByLeaveKingdom(c, false);
                        departedNames.Add(c.Name?.ToString() ?? "A Legion clan");
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            return departedNames;
        }
    }
}
