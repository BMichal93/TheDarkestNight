// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Chosen/ChosenQuestCampaignBehavior.Split.cs
//
// The kingdom split itself. Mirrors CityStateSystem.CreateCityState's proven
// Kingdom.CreateKingdom/InitializeKingdom/ChangeKingdomAction.ApplyByCreateKingdom
// sequence exactly, run once per splinter, then wars every splinter against
// every other splinter (DeclareWarAction.ApplyByDefault, all pairs).
//
// The PriestKing's fate: he leads the largest splinter (Group 0 — see
// ChosenQuestMath.AssignSplinterGroups's remainder bias). He cannot admit
// the vision failed, so rather than dying or being deposed he doubles down,
// certain the OTHER Apostles must be the ones who profaned the reclaiming.
// (If the player has somehow supplanted him as the Chosen's ruling clan
// before this fires — impossible via the quest's own dialogue trigger, which
// requires talking to an NPC leader, but theoretically reachable through some
// other succession path — the player's clan is still excluded from the
// partition below, so Group 0 simply anchors on whichever remaining Chosen
// clan is most influential instead.)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed partial class ChosenQuestCampaignBehavior
    {
        private static void PerformSplit(Kingdom chosen)
        {
            try
            {
                if (chosen == null) return;

                // Release the player's own clan to independence first — the
                // wars declared below are not a choice the player made, so no
                // splinter gets to speak for a clan that never picked a side.
                try
                {
                    if (Clan.PlayerClan != null && Clan.PlayerClan.Kingdom == chosen)
                        ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                var clans = chosen.Clans
                    .Where(c => c != null && !c.IsEliminated && c != Clan.PlayerClan)
                    .OrderBy(c => c == chosen.RulingClan ? 0 : 1)
                    .ThenByDescending(c => c.Influence)
                    .ToList();

                int splinterCount = ChosenQuestMath.SplinterCount(clans.Count);

                if (splinterCount <= 1)
                {
                    ChosenQuestLog.Current?.LogSplit(
                        "There are too few of the faithful left to fracture into rival crowns. The Chosen's " +
                        "name survives, hollowed out, with no one left to argue over what it ever meant.");
                    InformationManager.DisplayMessage(new InformationMessage(
                        "The Promise dies quietly — too few of the Chosen remain to even fracture over it.",
                        new Color(0.55f, 0.35f, 0.35f)));
                    _phase = PhaseEnded;
                    return;
                }

                int[] groups = ChosenQuestMath.AssignSplinterGroups(clans.Count, splinterCount);

                var newKingdoms = new List<Kingdom>();
                for (int g = 0; g < splinterCount; g++)
                {
                    var groupClans = new List<Clan>();
                    for (int i = 0; i < clans.Count; i++)
                        if (groups[i] == g) groupClans.Add(clans[i]);
                    if (groupClans.Count == 0) continue;

                    Kingdom splinter = FoundSplinterKingdom(g, groupClans);
                    if (splinter != null) newKingdoms.Add(splinter);
                }

                // The old Chosen kingdom (chosen) now holds no clans of its own —
                // TaleWorlds marks a kingdom eliminated automatically once its
                // last clan departs. Every other Chosen daily/weekly tick reads
                // the kingdom through ChosenCulture.GetChosenKingdom()/
                // ChosenCampaignBehavior.GetChosenKingdom(), both of which already
                // filter on "!k.IsEliminated" — so every one of those ticks
                // degrades to a harmless early-return the moment this fires,
                // with no extra guarding needed here.

                for (int i = 0; i < newKingdoms.Count; i++)
                    for (int j = i + 1; j < newKingdoms.Count; j++)
                        try { DeclareWarAction.ApplyByDefault(newKingdoms[i], newKingdoms[j]); }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                string names = string.Join(", ",
                    newKingdoms.Select(k => k.Name?.ToString()).Where(n => !string.IsNullOrEmpty(n)));

                ChosenQuestLog.Current?.LogSplit(
                    $"What was the Chosen has broken into {newKingdoms.Count} crowns — {names} — each certain " +
                    "the others profaned the reclaiming, each now at the others' throats instead of the dark's.");

                InformationManager.DisplayMessage(new InformationMessage(
                    "The Chosen have shattered. " + names + " now war each other over a promise none of them " +
                    "can keep.",
                    new Color(0.60f, 0.05f, 0.05f)));

                _phase = PhaseEnded;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Kingdom FoundSplinterKingdom(int splinterIndex, List<Clan> groupClans)
        {
            try
            {
                if (groupClans == null || groupClans.Count == 0) return null;
                Clan anchor = groupClans[0];

                Settlement home = anchor.Settlements?.FirstOrDefault(s => s.IsTown)
                                ?? anchor.Settlements?.FirstOrDefault(s => s.IsCastle);
                if (home == null) return null; // anchor holds no fief — cannot found a kingdom around it

                string id = splinterIndex < ChosenQuestMath.SplinterKingdomIds.Length
                    ? ChosenQuestMath.SplinterKingdomIds[splinterIndex]
                    : "chosen_splinter_" + splinterIndex;
                if (Kingdom.All.Any(k => k.StringId == id))
                    id = id + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);

                string name = splinterIndex < ChosenQuestMath.SplinterKingdomNames.Length
                    ? ChosenQuestMath.SplinterKingdomNames[splinterIndex]
                    : "The Broken Chosen";

                var kingdom = Kingdom.CreateKingdom(id);
                kingdom.InitializeKingdom(
                    new TextObject(name),
                    new TextObject(name),
                    anchor.Culture ?? home.Culture,
                    anchor.Banner ?? Banner.CreateRandomBanner(),
                    anchor.Color,
                    anchor.Color2,
                    home,
                    new TextObject(SplinterLore(splinterIndex)),
                    new TextObject(name),
                    new TextObject(ChosenCulture.RulerTitle));

                try { ChangeKingdomAction.ApplyByCreateKingdom(anchor, kingdom, false); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                for (int i = 1; i < groupClans.Count; i++)
                {
                    try
                    {
                        ChangeKingdomAction.ApplyByJoinToKingdom(
                            groupClans[i], kingdom, CampaignTime.Now + CampaignTime.Years(1000), false);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                return kingdom;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return null; }
        }

        private static string SplinterLore(int splinterIndex)
        {
            switch (splinterIndex)
            {
                case 0:
                    return "The PriestKing would not believe the vision lied, so he believes instead that the " +
                           "reclaiming was profaned by weaker hands than his own. The Unbroken Word holds to the " +
                           "letter of the first vision and calls every other splinter heretic for doubting it.";
                case 1:
                    return "The Ashen Apostles broke from the PriestKing's line the day the sky stayed silent — " +
                           "not because they stopped believing a vision was once seen, but because they no " +
                           "longer believe the bloodline that saw it was ever divine.";
                default:
                    return "The Last Vigil kept no doctrine out of the silence beyond this: the demons are still " +
                           "coming, and someone still has to stand the wall. They fight on without a promise, " +
                           "and without much hope that either of the other crowns will still be standing to help.";
            }
        }
    }
}
