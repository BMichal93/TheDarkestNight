// =============================================================================
// THE DARKEST NIGHT — CampaignMapEvents.Branded.cs
// "The Turning" — a mage lord's blood is failing to hold the Night out.
// The player can End It (a demon-bane mercy kill; a measure of what escapes
// them steadies the player in turn), Hold It Back (steady the lord at a
// cost of their own), or Leave (do nothing).
// Partial of CampaignMapEvents (shared state lives in CampaignMapEvents.cs).
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
    public static partial class CampaignMapEvents
    {
        // ── The Turning ────────────────────────────────────────────────────────
        // A mage lord has been touched by the Night too many times, or too closely
        // — a wound that would not close, a cast that reached too deep, a night
        // spent too near a breach. Whatever the cause, the demon-blood in them is
        // winning. The player, as a fellow caster who has felt the Night's pull
        // on their own working, is the only one present who can see it clearly.
        //
        // Gate: player must be an active caster (not cult-bound — the corrupted
        // cannot feel another's corruption; it all reads as home to them).
        // Candidate: a non-cult-bound caster lord who is alive, not the player,
        //            not a clan leader (clan leaders have too much plot armour
        //            to dissolve cleanly mid-campaign).
        internal static void TryFireTheBranded()
        {
            // Cheap eligibility gates must run BEFORE claiming the weekly slot.
            // If the slot were claimed first and then returned (the common non-mage
            // case), the 14-day event cooldown would start with no event fired,
            // silently suppressing all other world events for two weeks.
            if (!MageKnowledge.IsMage || MageKnowledge.IsAshen) return;
            if (Hero.MainHero == null) return;
            if (MageKnowledge._deferredInquiry != null) return;
            if (_rng.NextDouble() >= ChanceTheBranded) return;
            if (!TryClaimWeeklySlot()) return;

            try
            {
                var candidates = Hero.AllAliveHeroes
                    .Where(h =>
                        h.IsLord && h.IsAlive && !h.IsChild && !h.IsPrisoner
                        && h != Hero.MainHero
                        && ElementLordRegistry.IsElementLord(h)
                        && !ElementLordRegistry.IsAshenLord(h)
                        && h.Clan?.Leader != h
                        && h.Clan?.Heroes.Count(x => x.IsAlive && !x.IsChild) >= 2)
                    .ToList();

                if (candidates.Count == 0) return;

                var branded = candidates[_rng.Next(candidates.Count)];
                string brandedName = branded.Name?.ToString() ?? "a caster lord";
                string clanName    = branded.Clan?.Name?.ToString() ?? "their house";

                MageKnowledge._deferredInquiry = () => ShowBrandedEvent(branded, brandedName, clanName);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ShowBrandedEvent(Hero branded, string brandedName, string clanName)
        {
            if (branded == null || !branded.IsAlive)
            {
                MageKnowledge._deferredInquiry = null;
                return;
            }

            // Ending it answers the BLOOD discipline (the Bloodbound's demon-bane
            // art, heir to the retired Reap talent — legacy owners keep the right).
            bool canHarvest = MageElementKnowledge.HasBlood || TalentSystem.Has(TalentId.Reap);

            string affirmLabel = canHarvest ? "End it" : "Leave them to it";
            string affirmDesc  = canHarvest
                ? "Cut the corruption off before it finishes taking them. It will kill them — but what escapes steadies you in turn."
                : "You have felt the Night's pull on your own working. You know what this looks like, and you will not touch it.";

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "The Turning",
                $"Something in {brandedName} of {clanName} is losing the argument with the dark. You feel it before you see it — " +
                $"a coldness at the edge of your awareness, the way a draft finds a door left ajar. " +
                $"They do not know it yet, or they do and cannot stop it. The Night is working its way through " +
                $"them, marrow-deep, and will not stop on its own.\n\n" +
                $"You are the only one present who can see it for what it is.",
                new List<InquiryElement>
                {
                    new InquiryElement("harvest",
                        canHarvest ? "End it — cut the corruption off before it finishes." : "Leave them. This is not yours to take.",
                        null, true,
                        canHarvest
                            ? "Fifteen days of your own strength restored. They will not survive it."
                            : "You watch, and you walk away."),
                    new InquiryElement("leave",
                        canHarvest ? "Leave. Let it run its course." : "Offer what little warding you can.",
                        null, true,
                        canHarvest
                            ? "They will almost certainly turn within the month regardless. You gain nothing."
                            : "You cannot save them, but you can hold the Night back a few more days. −3 days of your own vigour for the effort."),
                },
                false, 1, 1, "Decide", "",
                sub =>
                {
                    string choice = sub?[0]?.Identifier as string;
                    switch (choice)
                    {
                        case "harvest" when canHarvest:
                            OnBrandedHarvest(branded, brandedName);
                            break;
                        case "leave" when !canHarvest: // labelled "Offer what little warding you can"
                            OnBrandedSoothe(branded, brandedName);
                            break;
                        default: // walk away — "leave" with the Blood right, "harvest" without it
                            OnBrandedLeave(branded, brandedName);
                            break;
                    }
                }, null, "", false), false, true);
        }

        private static void OnBrandedHarvest(Hero branded, string brandedName)
        {
            try
            {
                // Cutting the corruption off restores the player's own vigour (the
                // same ledger the Blood discipline feeds) — it does not de-age the body.
                AgingSystem.RestoreLifeExpectancy(Hero.MainHero, 15);
                MBInformationManager.AddQuickInformation(new TextObject(
                    $"You cut it off in {brandedName}. For a moment there is warmth — real warmth, " +
                    $"the kind that restores, as whatever the Night was taking is denied it. Then it is over. " +
                    $"Fifteen days of your own strength, given back. They are nothing at all."));
                try
                {
                    if (branded.IsAlive)
                        KillCharacterAction.ApplyByOldAge(branded, true);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try
                {
                    if (branded.MapFaction is Kingdom k)
                        ChangeCrimeRatingAction.Apply(k, 20f, true);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { ShiftTrait(DefaultTraits.Mercy, -1); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnBrandedLeave(Hero branded, string brandedName)
        {
            MBInformationManager.AddQuickInformation(new TextObject(
                $"You watch {brandedName} from a distance and walk away. " +
                $"The cold in them flares bright and then is gone. You carry nothing from this."));
        }

        private static void OnBrandedSoothe(Hero branded, string brandedName)
        {
            try
            {
                // The warding is paid for as advertised — 3 days of the player's own vigour.
                try { AgingSystem.AgeHero(Hero.MainHero, 3); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, branded, 15, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                MBInformationManager.AddQuickInformation(new TextObject(
                    $"You reach in and hold the Night back from {brandedName} — carefully, at a cost you feel but cannot measure. " +
                    $"They will carry the mark, but they will carry it longer. " +
                    $"They do not fully understand what you did. They feel only that they owe you something they cannot name."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ShiftTrait(TraitObject trait, int delta)
        {
            try
            {
                if (Hero.MainHero == null) return;
                int cur = Hero.MainHero.GetTraitLevel(trait);
                int next = Math.Max(-3, Math.Min(3, cur + delta));
                if (next != cur) Hero.MainHero.SetTraitLevel(trait, next);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
