// =============================================================================
// THE DARKEST NIGHT — AshenRuins/AshenRuinSystem.Challenges2.cs
// Individual challenge implementations (Ch_TemporalCrack .. Ch_TriuneReckoning).
// Partial of AshenRuinSystem (shared state lives in AshenRuinSystem.cs).
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
        private static void Ch_TemporalCrack(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int roll = _rng.Next(100);
            if (roll < 40) // Gain aging back
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Temporal Crack",
                    "The crack in time runs the wrong way here. You walk through and come out younger. 6 days given back.",
                    true, false, "Continue", "",
                    () => { AgingSystem.RejuvenateHero(Hero.MainHero, 6); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
            else if (roll < 75) // Aging cost
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Temporal Crack",
                    "Time skips. You come out the other side 8 days older. The room looks the same. You do not.",
                    true, false, "Continue (8 days aging)", "",
                    () => { AgePlayer(8); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
            else // Troop wipe in that sector
            {
                int lost = isSolo ? 0 : Math.Max(5, (MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 10) / 5);
                string troopLine = isSolo || lost == 0
                    ? "The room reassembles around you. You are unharmed — there is no one else to lose."
                    : $"Your rearguard does not make it through. {lost} men are simply... not there anymore.";
                InformationManager.ShowInquiry(new InquiryData(
                    "Temporal Crack",
                    troopLine,
                    true, false, "Continue", "",
                    () => { if (!isSolo && lost > 0) LoseTroops(lost); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
        }

        private static void Ch_SleepingGiant(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            if (isSolo)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Sleeping Giant",
                    "Something enormous rests here. Alone, you move like smoke. It does not stir.",
                    true, false, "Slip past", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
                return;
            }
            int troops = MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0;
            bool pass = _rng.Next(100) < Math.Max(15, 60 - troops / 3);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Sleeping Giant",
                    "Despite everything, you get your entire column through without a sound. It does not wake.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                int lost = Math.Max(10, troops / 4);
                InformationManager.ShowInquiry(new InquiryData(
                    "Sleeping Giant",
                    $"It wakes. It is terrible. The retreat costs {lost} men. You are through, but not all of you.",
                    true, true, $"Fight retreat (lose {lost} troops)", "Full retreat",
                    () => { LoseTroops(lost); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_NecromanticWard(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            if (isSolo)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Necromantic Ward",
                    "The ward was built to turn armies against themselves. Alone, it finds nothing to work with. It settles for pressing itself into you instead — 6 whispers, carried out.",
                    true, false, "Continue (+6 whispers)", "",
                    () => { MageKnowledge.AddWhispers(6); NextRoom(def, isSolo, ri, sr); }, null), true);
                return;
            }
            int troops = MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0;
            bool held = _rng.Next(100) < 55;
            if (held)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Necromantic Ward",
                    "Your men look at each other strangely for a moment. Then the moment passes. The ward finds no purchase.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                int lost = Math.Max(5, troops / 8);
                InformationManager.ShowInquiry(new InquiryData(
                    "Necromantic Ward",
                    $"The ward does something to the men at the back. {lost} men turn on each other before you can stop it. The rest push through.",
                    true, false, "Continue (friendly fire)", "",
                    () => { LoseTroops(lost); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
        }

        private static void Ch_DragonEgg(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Dragon's Egg",
                "A black sphere, warm, the size of a man's head. It is not an egg — not exactly — but something in your marks answers something inside it. You can take it or leave it. Taking it will attract attention.",
                true, true, "Take it (the Night notices you)", "Leave it",
                () =>
                {
                    MageKnowledge.AddWhispers(12);
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You carry the Egg out. Something cold and distant takes note of your name.",
                        new Color(0.45f, 0.35f, 0.65f)));
                    NextRoom(def, isSolo, ri, sr);
                },
                () => NextRoom(def, isSolo, ri, sr)), true);
        }

        private static void Ch_AshenFlame(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int agingCost = isSolo ? 10 : 6;
            int troopCost = 10;
            string desc = isSolo
                ? "Cold fire fills the corridor — flame with no warmth in it. No troops to shield you. 10 days aging minimum to push through it."
                : "Cold fire fills the corridor — flame with no warmth in it. You can shield yourself with troops (10 lost) but it will still take 6 days from you.";

            InformationManager.ShowInquiry(new InquiryData(
                "The Cold Fire",
                desc,
                true, true, "Push through", "Retreat",
                () =>
                {
                    AgePlayer(agingCost);
                    if (!isSolo) LoseTroops(troopCost);
                    NextRoom(def, isSolo, ri, sr);
                },
                () => OnRetreat(def, ri)), true);
        }

        private static void Ch_EmberWraith(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            float renown = 0f;
            try { renown = Hero.MainHero?.Clan?.Renown ?? 0f; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            bool pass = _rng.Next(100) < AshenRuinMath.EmberWraithPassChance(renown);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Ember Wraith",
                    "Something ember-eyed studies you from the dark and decides you are not worth the trouble. It withdraws.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Ember Wraith",
                    "Your name has reached further than you thought. The wraith wants to test it — 8 days of your fire, or five men to test it instead.",
                    true, true, "Spend fire (8 days aging)", "Spend troops (5 men)",
                    () => { AgePlayer(8); NextRoom(def, isSolo, ri, sr); },
                    () =>
                    {
                        int troops = MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0;
                        if (isSolo || troops < 5)
                        {
                            InformationManager.ShowInquiry(new InquiryData(
                                "Not Enough Men",
                                "You do not have the men to spend. The fire will have to do. 8 days aging.",
                                true, false, "Pay", "",
                                () => { AgePlayer(8); NextRoom(def, isSolo, ri, sr); }, null), true);
                        }
                        else
                        { LoseTroops(5); NextRoom(def, isSolo, ri, sr); }
                    }), true);
            }
        }

        private static void Ch_WardstoneGate(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int roguery = 0;
            try { roguery = Hero.MainHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            bool pass = _rng.Next(100) < AshenRuinMath.WardstoneGatePassChance(roguery);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Wardstone Gate",
                    "The ward-carved stones expect a particular kind of trespasser. You read the pattern and step through the gaps meant for exactly your sort.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Wardstone Gate",
                    "The pattern eludes you. You can force the wardstones apart — 6 days of your fire — or turn back.",
                    true, true, "Force it (6 days aging)", "Retreat",
                    () => { AgePlayer(6); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_HollowChoir(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int tier = MageKnowledge.IsMage ? MageKnowledge.WhisperTier : 0;
            if (tier >= 2)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Hollow Choir",
                    "Voices without mouths sing something that isn't quite a warning. The cold in you hums back at the same pitch. They let you pass, and leave a little more of themselves behind. +3 whispers.",
                    true, false, "Continue", "",
                    () => { MageKnowledge.AddWhispers(3); NextRoom(def, isSolo, ri, sr); }, null), true);
                return;
            }
            bool pass = _rng.Next(100) < AshenRuinMath.HollowChoirPassChance(tier);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Hollow Choir",
                    "The singing rises around you, searching for a voice to match. It doesn't find one in you, and loses interest.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Hollow Choir",
                    "The singing finds a crack in you after all. 6 days of your fire, taken to quiet it — or retreat before it takes more.",
                    true, true, "Push through (6 days aging)", "Retreat",
                    () => { AgePlayer(6); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_EmberToll(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int unspentFp = 0;
            try { unspentFp = Hero.MainHero?.HeroDeveloper?.UnspentFocusPoints ?? 0; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            bool canPayFp = unspentFp > 0;
            string toll = canPayFp ? "Pay with knowledge (1 focus point)" : "Pay with fire (4 days aging)";
            InformationManager.ShowInquiry(new InquiryData(
                "Ember Toll",
                "The room asks for a piece of what you know, not what you carry. The fire inside you already understands the exchange.",
                true, true, toll, "Retreat",
                () =>
                {
                    if (canPayFp)
                    {
                        try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints -= 1; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                    else AgePlayer(4);
                    NextRoom(def, isSolo, ri, sr);
                },
                () => OnRetreat(def, ri)), true);
        }

        private static void Ch_WeightOfAsh(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int cost = AshenRuinMath.WeightOfAshCost(def.Tier);
            InformationManager.ShowInquiry(new InquiryData(
                "Weight of Ash",
                $"The scale in this room does not move for gold or blood. It moves for time. {cost} days of your fire, and no less, or the passage does not open.",
                true, false, $"Pay ({cost} days aging)", "",
                () => { AgePlayer(cost); NextRoom(def, isSolo, ri, sr); }, null), true);
        }

        private static void Ch_ShiftingHall(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            var outcome = AshenRuinMath.ResolveShiftingHall(_rng.Next(100));
            switch (outcome)
            {
                case ShiftingHallOutcome.RenownGain:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Shifting Hall",
                        "The hall rearranges itself around you and, for reasons it does not share, remembers you kindly. Word of the passage will travel. (+15 renown)",
                        true, false, "Continue", "",
                        () =>
                        {
                            try { ClanRenown.Gain(Hero.MainHero.Clan, 15f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            NextRoom(def, isSolo, ri, sr);
                        }, null), true);
                    break;

                case ShiftingHallOutcome.Desertion:
                    int troops = MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0;
                    int lost = isSolo ? 0 : AshenRuinMath.ShiftingHallDesertionLoss(troops);
                    string line = isSolo || lost == 0
                        ? "The hall's doubt has nothing to work with — you are alone, and unshaken."
                        : $"The hall plants a doubt at the back of the column. {lost} men slip away in the confusion and do not come back.";
                    InformationManager.ShowInquiry(new InquiryData(
                        "Shifting Hall", line,
                        true, false, "Continue", "",
                        () => { if (!isSolo && lost > 0) LoseTroops(lost); NextRoom(def, isSolo, ri, sr); }, null), true);
                    break;

                default: // WhisperGain
                    InformationManager.ShowInquiry(new InquiryData(
                        "Shifting Hall",
                        "The hall leaves something behind in you on the way through — not a wound, exactly. +5 whispers.",
                        true, false, "Continue", "",
                        () => { MageKnowledge.AddWhispers(5); NextRoom(def, isSolo, ri, sr); }, null), true);
                    break;
            }
        }

        private static void Ch_TriuneReckoning(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            bool isAshen = MageKnowledge.IsAshen;
            bool natureAttuned = false, hasGrace = false;
            try { natureAttuned = NatureKnowledge.IsAttuned; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { hasGrace = MiracleInventory.HasGrace; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            if (isAshen)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Triune Reckoning",
                    "The room asks which fire you carry. The cold in you answers before you do. It recognises itself and steps aside. +4 whispers.",
                    true, false, "Continue", "",
                    () => { MageKnowledge.AddWhispers(4); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
            else if (hasGrace)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Triune Reckoning",
                    "The room asks which fire you carry. Something in you answers with conviction rather than heat. That is enough. Word of it spreads. (+10 renown)",
                    true, false, "Continue", "",
                    () =>
                    {
                        try { ClanRenown.Gain(Hero.MainHero.Clan, 10f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        NextRoom(def, isSolo, ri, sr);
                    }, null), true);
            }
            else if (natureAttuned)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Triune Reckoning",
                    "The room asks whose hand wrote you. The marks you carry are quieter than the others, and older. The room eases, briefly, and gives back 3 days.",
                    true, false, "Continue", "",
                    () => { AgingSystem.RejuvenateHero(Hero.MainHero, 3); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
            else
            {
                bool pass = _rng.Next(100) < AshenRuinMath.TriuneReckoningFallbackPassChance(TalentSystem.PurchasedCount);
                if (pass)
                {
                    InformationManager.ShowInquiry(new InquiryData(
                        "Triune Reckoning",
                        "The room asks which fire you carry. You aren't entirely sure yourself — but the answer, whatever it was, is accepted.",
                        true, false, "Continue", "",
                        () => NextRoom(def, isSolo, ri, sr), null), true);
                }
                else
                {
                    InformationManager.ShowInquiry(new InquiryData(
                        "Triune Reckoning",
                        "The room asks which fire you carry, and finds the answer wanting. 7 days of your fire settles the matter, or retreat.",
                        true, true, "Pay (7 days aging)", "Retreat",
                        () => { AgePlayer(7); NextRoom(def, isSolo, ri, sr); },
                        () => OnRetreat(def, ri)), true);
                }
            }
        }

    }
}
