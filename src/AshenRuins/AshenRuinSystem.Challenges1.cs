// =============================================================================
// THE DARKEST NIGHT — AshenRuins/AshenRuinSystem.Challenges1.cs
// Individual challenge implementations (Ch_BloodLock .. Ch_MirrorGate).
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
        // ── Individual challenge implementations ──────────────────────────────

        private static void Ch_BloodLock(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Blood Lock",
                "The door is sealed by a dried ring of dark matter. There is no key — only a price. The fire inside you already knows what it wants.",
                true, true, "Pay the toll (3 days)", "Retreat",
                () => { AgePlayer(3); NextRoom(def, isSolo, ri, sr); },
                () => OnRetreat(def, ri)), true);
        }

        private static void Ch_Vision(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Vision Chamber",
                "The chamber hums. Whatever lived here left an impression — not a ghost exactly, but the shape of one. It has nothing to say and everything to show.",
                true, false, "Bear witness", "",
                () => NextRoom(def, isSolo, ri, sr), null), true);
        }

        private static void Ch_SoulHarvest(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "The Harvest",
                "Something here is starving. It offers you passage — not freely. It takes 15 days of your fire as a toll. You cannot negotiate. You can only pay.",
                true, false, "Pay (15 days aging)", "",
                () => { AgePlayer(15); NextRoom(def, isSolo, ri, sr); }, null), true);
        }

        private static void Ch_VoidMaw(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "The Void Maw",
                "A passage that closes the moment you enter it. No retreat from here. The walls press close and cost you something. 6 days of your fire, gone into the dark.",
                true, false, "Pass through (6 days aging)", "",
                () => { AgePlayer(6); NextRoom(def, isSolo, ri, sr); }, null), true);
        }

        private static void Ch_AncientTrap(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            bool pass = _rng.Next(100) < 60;
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Ancient Trap",
                    "You step over something that would have been a trigger a hundred years ago. The mechanism has seized. Lucky.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Ancient Trap",
                    "The floor gives way just enough. Something catches you — not a killing blow, but the price of carelessness. 5 days older.",
                    true, false, "Continue (5 days aging)", "",
                    () => { AgePlayer(5); NextRoom(def, isSolo, ri, sr); }, null), true);
            }
        }

        private static void Ch_SealedMemory(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Sealed Memory",
                "A room that holds the compressed weight of someone else's last moment. It forces its way in. You will carry 8 whispers of it with you, and you will see everything.",
                true, false, "Receive the memory (+8 whispers)", "",
                () =>
                {
                    MageKnowledge.AddWhispers(8);
                    NextRoom(def, isSolo, ri, sr);
                }, null), true);
        }

        private static void Ch_CollapsingChamber(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            if (isSolo)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Collapsing Chamber",
                    "The ceiling is unhappy. Without troops to brace the supports you must trust your own speed — and pay for it. 8 days aging.",
                    true, true, "Sprint through (8 days aging)", "Retreat",
                    () => { AgePlayer(8); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
                return;
            }
            int troops = MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0;
            bool pass = _rng.Next(100) < Math.Min(80, troops / 2 + 20);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Collapsing Chamber",
                    "The ceiling groans and sheds dust. Your people brace the weakest points. It holds — barely.",
                    true, false, "Push on", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                int lost = Math.Max(3, troops / 6);
                InformationManager.ShowInquiry(new InquiryData(
                    "Collapsing Chamber",
                    $"Part of the ceiling comes down. {lost} men are buried or flee. You can dig or retreat.",
                    true, true, $"Dig through (lose {lost} troops)", "Retreat",
                    () => { LoseTroops(lost); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_SpectralGuardian(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int agingSpent = AgingSystem.LedgerDaysSpent;
            // More experienced casters have harder guardians
            bool tough = agingSpent > 40;
            bool pass = _rng.Next(100) < (tough ? 45 : 65);
            string desc = tough
                ? "The guardian recognises the weight you carry. It makes itself harder. Your own fire, bent back against you."
                : "A remnant, still on duty. It reads your fire and finds it... acceptable. Barely.";
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Spectral Guardian", desc + "\n\nIt lets you pass.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Spectral Guardian", desc + "\n\nIt does not let you pass without a price. 8 days aging to force through, or retreat.",
                    true, true, "Force through (8 days aging)", "Retreat",
                    () => { AgePlayer(8); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_VoidWhisper(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Void Whisper",
                "Something speaks from a crack in the wall. It does not use words. It uses your name. The fire inside you flinches. You can answer it — which costs you something — or resist, which may cost you more.",
                true, true, "Answer (+10 whispers, proceed)", "Resist (roll)",
                () => { MageKnowledge.AddWhispers(10); NextRoom(def, isSolo, ri, sr); },
                () =>
                {
                    bool resist = _rng.Next(100) < 55;
                    if (resist)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Void Whisper",
                            "The fire holds. The voice retreats. You continue.",
                            true, false, "Continue", "",
                            () => NextRoom(def, isSolo, ri, sr), null), true);
                    }
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Void Whisper",
                            "The voice finds a crack anyway. 3 days of your fire, taken. But you pass.",
                            true, false, "Continue (3 days aging)", "",
                            () => { AgePlayer(3); NextRoom(def, isSolo, ri, sr); }, null), true);
                    }
                }), true);
        }

        private static void Ch_RiddleGate(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
            => ShowRiddle(def, isSolo, ri, sr);

        private static void ShowRiddle(RuinDef def, bool isSolo, int ri, bool sr)
        {
            var riddles = new[]
            {
                ("I am counted by kings and bled by mages. I have no colour but am spent in every fire. What am I?",
                 new[]{"Days","Gold","Time","Blood"}, 0),
                ("The cold carries it, the living forget it, the dead hoard it. What is it?",
                 new[]{"Memory","Ash","Shadow","Silence"}, 0),
                ("I am the price of the first spell and the shape of the last one. I grow without planting.",
                 new[]{"Aging","Power","Hunger","Grief"}, 0),
            };
            var (question, answers, correct) = riddles[_rng.Next(riddles.Length)];
            string correctAnswer = answers[correct];

            var options = answers.Select((a, i) =>
                new InquiryElement(i, a, null, true,
                    i == correct ? "This feels true." : "This might be right.")).ToList();
            // Add "Pay to pass" option
            options.Add(new InquiryElement(99, "Burn through it (2 days aging)", null, true, "Skip the puzzle with fire."));

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Riddle Gate",
                question,
                options, true, 1, 1, "Answer", "Retreat",
                chosen =>
                {
                    if (chosen == null || chosen.Count == 0) { OnRetreat(def, ri); return; }
                    int idx = (int)chosen[0].Identifier;
                    if (idx == 99)
                    { AgePlayer(2); NextRoom(def, isSolo, ri, sr); return; }
                    bool right = answers[idx] == correctAnswer;
                    if (right)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "The Gate Opens", "The lock clicks. The door swings inward on silence.",
                            true, false, "Enter", "",
                            () => NextRoom(def, isSolo, ri, sr), null), true);
                    }
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Wrong Answer", "4 days aging for the error. You may try once more.",
                            true, true, "Try again", "Retreat",
                            () => { AgePlayer(4); ShowRiddle(def, isSolo, ri, sr); },
                            () => OnRetreat(def, ri)), true);
                    }
                },
                _ => OnRetreat(def, ri),
                "", false), false, true);
        }

        private static void Ch_AshenSentinel(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            if (MageKnowledge.IsAshen)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Hollow Sentinel",
                    "The sentinel turns its hollow gaze on you. Then it steps aside. The cold recognises the cold.",
                    true, false, "Pass through", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
                return;
            }
            bool pass = _rng.Next(100) < 40;
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Hollow Sentinel",
                    "The guardian reads the marks on you and finds them... insufficient threat. It does not bother. You pass.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Hollow Sentinel",
                    "The guardian does not step aside for rune-writers of your kind. You can burn your way through — 10 days aging — or lose 5 troops as a distraction.",
                    true, true, "Spend yourself (10 days aging)", "Spend troops (5 men)",
                    () => { AgePlayer(10); NextRoom(def, isSolo, ri, sr); },
                    () =>
                    {
                        if (isSolo || (MobileParty.MainParty?.MemberRoster?.TotalHealthyCount ?? 0) < 5)
                        {
                            InformationManager.ShowInquiry(new InquiryData(
                                "Not Enough Men",
                                "You do not have the men to spend. The fire will have to do. 10 days aging.",
                                true, false, "Pay", "",
                                () => { AgePlayer(10); NextRoom(def, isSolo, ri, sr); }, null), true);
                        }
                        else
                        { LoseTroops(5); NextRoom(def, isSolo, ri, sr); }
                    }), true);
            }
        }

        private static void Ch_SerpentNest(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int troopCost = 8 + _rng.Next(13);
            int agingCost = isSolo ? 9 : 6;

            InformationManager.ShowInquiry(new InquiryData(
                "Serpent Nest",
                $"The passage is occupied. You can burn them out with a fire-rune ({agingCost} days aging) or send men through first ({troopCost} troops lost).",
                true, true, $"Burn them ({agingCost} days aging)", $"Send men ({troopCost} troops)",
                () => { AgePlayer(agingCost); NextRoom(def, isSolo, ri, sr); },
                () =>
                {
                    if (isSolo)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Serpent Nest",
                            "You are alone. There are no men to send. The fire must serve. 9 days aging.",
                            true, true, "Pay (9 days aging)", "Retreat",
                            () => { AgePlayer(9); NextRoom(def, isSolo, ri, sr); },
                            () => OnRetreat(def, ri)), true);
                    }
                    else
                    { LoseTroops(troopCost); NextRoom(def, isSolo, ri, sr); }
                }), true);
        }

        private static void Ch_PoisonedAir(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int proficiency = TalentSystem.PurchasedCount;
            bool pass = _rng.Next(100) < Math.Min(75, 30 + proficiency * 4);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Poisoned Air",
                    "The air is thick with something that should not be breathable. Your fire burns it away before it reaches your lungs.",
                    true, false, "Continue", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Poisoned Air",
                    "The fumes take hold before your fire responds. Your party staggers through — the wound will heal, but some will not walk straight for days.",
                    true, true, "Push on (−15% party health)", "Retreat",
                    () => { ApplyPartyHealthPenalty(0.15f); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

        private static void Ch_CursedHoard(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Cursed Relics",
                "Three objects on a shelf. One hums faintly with warmth. The other two are cold, and eager. You can only take one.",
                new List<InquiryElement>
                {
                    new InquiryElement(0, "The brass compass", null, true, "It points in a direction that does not match north."),
                    new InquiryElement(1, "The sealed jar", null, true, "Something inside shifts when you move it."),
                    new InquiryElement(2, "The folded cloth", null, true, "It is warm. Too warm for this room."),
                },
                true, 1, 1, "Take it", "Leave everything",
                chosen =>
                {
                    if (chosen == null || chosen.Count == 0) { OnRetreat(def, ri); return; }
                    int pick = (int)chosen[0].Identifier;
                    int safe = 2; // the warm cloth is always safe
                    if (pick == safe)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "The Relic Holds",
                            "The cloth unfolds into something that should not fit inside it. You keep it. The room does not object.",
                            true, false, "Proceed", "",
                            () => NextRoom(def, isSolo, ri, sr), null), true);
                    }
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            "Cursed",
                            "Whatever was inside the relic has opinions about being moved. 10 days aging and a headache that lasts.",
                            true, false, "Continue (10 days aging)", "",
                            () => { AgePlayer(10); NextRoom(def, isSolo, ri, sr); }, null), true);
                    }
                },
                _ => OnRetreat(def, ri),
                "", false), false, true);
        }

        private static void Ch_MirrorGate(RuinChallenge c, RuinDef def, bool isSolo, int ri, bool sr)
        {
            int spells = TalentSystem.PurchasedCount;
            bool pass = _rng.Next(100) < Math.Max(20, 70 - spells * 3);
            if (pass)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Mirror Gate",
                    "Your shadow precedes you through the archway and tries to block you. You are slightly less afraid of yourself than it expected.",
                    true, false, "Step through", "",
                    () => NextRoom(def, isSolo, ri, sr), null), true);
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Mirror Gate",
                    "The shadow knows every spell you do. You pay for the impasse: 12 days aging, and then it steps aside.",
                    true, true, "Pay (12 days aging)", "Retreat",
                    () => { AgePlayer(12); NextRoom(def, isSolo, ri, sr); },
                    () => OnRetreat(def, ri)), true);
            }
        }

    }
}
