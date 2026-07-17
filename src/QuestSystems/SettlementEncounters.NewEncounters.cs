// =============================================================================
// THE DARKEST NIGHT — SettlementEncounters.NewEncounters.cs
// Six settlement encounters native to this world: The Painted Door, One Watch,
// The Vial Trade, Born at the Turning, The Family That Would Not Open, and
// The Bell of a Nameless Town. Each follows the house pattern established by
// E_OldEnemy/FireOldEnemyConsequence — an immediate choice, then a deferred,
// unexpected payoff fired days or weeks later via the daily tick.
// Partial of SettlementEncounters (shared state lives in SettlementEncounters.cs).
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
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public static partial class SettlementEncounters
    {
        // ═══════════════════════════════════════════════════════════════════
        // 1. THE PAINTED DOOR — town enter, general, 40-70d cooldown
        // ═══════════════════════════════════════════════════════════════════
        private static void E_WardSeller(Settlement s)
        {
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  The Painted Door",
                "A man works door to door with a pot of charcoal and salt paste, painting a crude ward above each lintel that will have him. He says it keeps the Night from crossing a threshold. He says it with the flat conviction of a man who has said it many times and stopped caring whether it was believed, only whether it was bought.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", "Buy a bundle for your own doors and packs.", null, true,
                        "A few coins for a folk charm. It may be nothing. It may not."),
                    new InquiryElement("b", "Denounce him — he preys on frightened people for coin.", null, true,
                        "You say it loudly enough for the street to hear."),
                    new InquiryElement("c", "Walk on. Not your business.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                            if (!ChangeGold(-40)) break;
                            _wardSellerOutcome   = 1;
                            _wardSellerCountdown = 20 + _rng.Next(21);
                            Msg("He paints the ward over your door in three quick strokes, mutters something under his breath, and moves to the next house before you've finished counting the coins into his hand.", DimColor);
                            break;
                        case "b":
                            ShiftTrait(DefaultTraits.Honor, -1);
                            _wardSellerOutcome   = 2;
                            _wardSellerCountdown = 20 + _rng.Next(21);
                            Msg("You say it loud enough for the street to hear: a fraud, preying on the frightened. A few people laugh. He does not argue. He gathers his pot and his brushes and walks away faster than a man with nothing to hide usually does.", BadColor);
                            break;
                        case "c":
                            Msg("You walk on. Behind you, another door gets its painted ward, another coin changes hands. The street looks no different than it did a minute ago.", DimColor);
                            break;
                    }
                }, null, "", false), false, true);
            _wardSellerCooldown = 40 + _rng.Next(31);
        }

        private static void FireWardSellerConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _wardSellerCountdown = 1; return; }
            int outcome = _wardSellerOutcome;
            _wardSellerOutcome = 0;

            if (outcome == 1)
            {
                if (_rng.NextDouble() < 0.5)
                {
                    MageKnowledge._deferredInquiry = () =>
                        Msg("Word reaches you from a hamlet down the road: they painted their doors on your recommendation. The wards did nothing. The Night came through anyway, the way it always does when salt and charcoal are the only thing standing in front of it. You did not sell them the charm. You only vouched for the man who did. That distinction feels smaller than it should.", BadColor);
                }
                else
                {
                    MageKnowledge._deferredInquiry = () =>
                        Msg("Something finds your camp at the edge of night and does not come in. Whether the painted lines on your wagon actually held it, or it simply passed you by for reasons of its own, you cannot say. Your men are certain it was the ward. You let them believe it. Certainty is worth more than accuracy, some nights.", GoodColor);
                }
            }
            else if (outcome == 2)
            {
                MageKnowledge._deferredInquiry = () =>
                {
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                        "◈  What the Coin Was For",
                        "A woman stops you in a market two towns over. She recognises you from the street where you called the ward-seller a fraud. She tells you, without heat, that he was her husband — that the coin from his pots and brushes fed three children after the fever took his hands for anything finer. She does not ask for anything. She wanted you to know what the mob you started actually cost.",
                        new List<InquiryElement>
                        {
                            new InquiryElement("ok", "There is nothing adequate to say to that.", null, true, ""),
                        },
                        false, 1, 1, "Endure", "",
                        _ => Msg("There is nothing adequate to say to that. You give her what coin you can spare, which does not feel like an answer, and she takes it because three children do not care whether it is an answer.", BadColor),
                        null, "", false), false, true);
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. ONE WATCH — town leave, dusk, clan tier >= 1
        // ═══════════════════════════════════════════════════════════════════
        private static void E_NightwatchShort(Settlement s)
        {
            _nightwatchSettlementId = s.StringId;
            float meleeChance = SkillChance(DefaultSkills.OneHanded, 0.35f);
            string meleeHint  = SkillHint(DefaultSkills.OneHanded, 0.35f, "Hold the wall through the dark hours");

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  One Watch",
                "The gate wardens are a man short for tonight's watch, with dusk already thickening at the tree line. The sergeant asks it plainly, not as an order — you owe the town nothing — but the wall goes unwalked at one point tonight unless someone fills it.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", "Stand the watch yourself.", null, true, meleeHint),
                    new InquiryElement("b", "Post one of your soldiers in your place.", null, true,
                        "You lose their sword arm for the night, whatever the night brings."),
                    new InquiryElement("c", "You have your own road. Refuse.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                            if (SkillRoll(DefaultSkills.OneHanded, 0.35f))
                            {
                                _nightwatchOutcome   = 1;
                                _nightwatchCountdown = 14 + _rng.Next(17);
                                Msg("You stand the gap until the second bell. Something tests the wall once, near the fourth hour — a scraping, low sound that stops when you move toward it. It does not come back. Dawn finds you tired and the wall unbroken.", GoodColor);
                            }
                            else
                            {
                                WoundPlayer();
                                _nightwatchOutcome   = 1;
                                _nightwatchCountdown = 14 + _rng.Next(17);
                                Msg("You hold the gap, but not cleanly — something gets close enough to leave a mark before the sergeant's whistle turns three spears on it and it withdraws. You bleed for the town's wall tonight. The wall holds anyway.", BadColor);
                            }
                            break;
                        case "b":
                            _nightwatchOutcome   = 2;
                            _nightwatchCountdown = 14 + _rng.Next(17);
                            Msg("You post one of yours in the gap and go find a bed. The night passes the way nights are supposed to — you hear nothing, and in the morning your soldier is at the gate, stiff and cold-handed and unremarkable.", DimColor);
                            break;
                        case "c":
                            _nightwatchOutcome   = 3;
                            _nightwatchCountdown = 14 + _rng.Next(17);
                            Msg("You ride on before dusk finishes settling. The sergeant says nothing. He will fill the gap himself, or he won't, and either way it is no longer yours to answer for.", DimColor);
                            break;
                    }
                }, null, "", false), false, true);
            _nightwatchCooldown = 25 + _rng.Next(21);
        }

        private static void FireNightwatchConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _nightwatchCountdown = 1; return; }
            int outcome = _nightwatchOutcome;
            _nightwatchOutcome = 0;
            string sId = _nightwatchSettlementId;
            _nightwatchSettlementId = null;
            var s = sId != null ? Settlement.All.FirstOrDefault(se => se.StringId == sId) : null;

            switch (outcome)
            {
                case 1:
                    MageKnowledge._deferredInquiry = () =>
                    {
                        ChangeRenown(10f);
                        Msg("The gate captain names you to his lord: the sellsword who stood a stranger's wall through the dark hours and asked nothing for it. It is a small story. It travels anyway — small stories about people who stayed are rarer than the ones about people who fought.", GoodColor);
                    };
                    break;
                case 2:
                    if (_rng.NextDouble() < 0.35)
                    {
                        MageKnowledge._deferredInquiry = () =>
                        {
                            KillPartyTroops(1);
                            Msg("Word reaches your company: the soldier you posted in the gap that night did not survive the following week's dusk raid, out on a separate patrol. Nobody connects the two nights. You do. You posted them in a gap once because it cost you less than standing it yourself, and the accounting for that never quite closes.", BadColor);
                        };
                    }
                    else
                    {
                        MageKnowledge._deferredInquiry = () =>
                            Msg("Your soldier mentions, weeks later and without making anything of it, that the watch was quiet and they'd do it again. You did not ask them how they felt about being sent in your place. They did not seem to need you to.", DimColor);
                    }
                    break;
                case 3:
                    if (_rng.NextDouble() < 0.4)
                    {
                        MageKnowledge._deferredInquiry = () =>
                        {
                            ChangeRelWithOwner(s, -8);
                            Msg("The section of wall that went unwalked the night you rode past broke before dawn — not badly, a handful of dead, but the town remembers who had the strength to fill the gap and rode on instead. Word of it reaches the settlement's lord before you do.", BadColor);
                        };
                    }
                    else
                    {
                        MageKnowledge._deferredInquiry = () =>
                            Msg("Nothing came of the gap you left unfilled. The sergeant covered it himself, or got lucky, or both. You will never know which, and the town has already forgotten you were asked.", DimColor);
                    }
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. THE VIAL TRADE — town enter, Bloodbound culture or mage-eligible
        // ═══════════════════════════════════════════════════════════════════
        private static ItemObject DemonBloodItem()
        {
            try { return MBObjectManager.Instance?.GetObject<ItemObject>(BloodboundCatalog.DemonBloodItemId); }
            catch { return null; }
        }

        private static int PlayerDemonBloodCount()
        {
            try
            {
                var item = DemonBloodItem();
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return 0;
                return roster.GetItemNumber(item);
            }
            catch { return 0; }
        }

        private static void E_BloodBroker(Settlement s)
        {
            int carried = PlayerDemonBloodCount();
            bool hasPrisoner = MobileParty.MainParty?.PrisonRoster?.TotalManCount > 0;

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  The Vial Trade",
                "A Bloodbound broker finds you at the market with the unhurried manner of someone who has done this many times and never once been refused without eventually being sought out again. He wants Demon Blood — yours, if you carry it, or fresh, if you are willing to let him bleed a prisoner in your presence. He pays well either way. He is entirely untroubled by the question of where it goes after it leaves his hands.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", carried > 0 ? $"Sell what you carry ({carried} vial(s))." : "You carry none to sell.", null, carried > 0,
                        "He pays fairly. What he does with it afterward is his business, not yours."),
                    new InquiryElement("b", "Let him bleed a prisoner.", null, hasPrisoner,
                        "More coin, less clean. Requires a prisoner in your roster."),
                    new InquiryElement("c", "Refuse the whole trade.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                        {
                            var item = DemonBloodItem();
                            if (item != null && MobileParty.MainParty?.ItemRoster != null)
                            {
                                int sell = Math.Min(carried, 1 + _rng.Next(carried));
                                MobileParty.MainParty.ItemRoster.AddToCounts(item, -sell);
                                ChangeGold(200 * sell);
                                _bloodBrokerOutcome   = 1;
                                _bloodBrokerCountdown = 25 + _rng.Next(21);
                                Msg($"He decants {sell} vial(s) into a lined case without spilling a drop and counts the coin into your palm without being asked twice.", GoldColor);
                            }
                            break;
                        }
                        case "b":
                            ShiftTrait(DefaultTraits.Mercy, -1);
                            ChangeCrime(5f);
                            ChangeGold(600);
                            _bloodBrokerOutcome   = 2;
                            _bloodBrokerCountdown = 25 + _rng.Next(21);
                            Msg("He works quickly and without ceremony, and the prisoner is more frightened by his calm than by the blade. You are paid well. You do not watch the whole thing.", BadColor);
                            break;
                        case "c":
                            _bloodBrokerOutcome = 3;
                            Msg("You refuse the whole trade. He accepts it the way he accepts everything — without argument, filing you under a different heading than the one he'd hoped for.", DimColor);
                            break;
                    }
                }, null, "", false), false, true);
            _bloodBrokerCooldown = 45 + _rng.Next(21);
        }

        private static void FireBloodBrokerConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _bloodBrokerCountdown = 1; return; }
            int outcome = _bloodBrokerOutcome;
            _bloodBrokerOutcome = 0;

            if (outcome == 1)
            {
                MageKnowledge._deferredInquiry = () =>
                {
                    var s2 = Settlement.All.Where(se => se.IsVillage || se.IsTown).OrderBy(_ => _rng.Next()).FirstOrDefault();
                    try { DemonSpawnCampaignBehavior.SpawnAmbushNear(MobileParty.MainParty.GetPosition2D, ""); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    Msg("The vials you sold fed a rite that slipped whatever circle was meant to hold it. Something carrying your scent finds your column at dusk — the broker's rite went wrong, or right in a way nobody warned you about, and either way the blood remembered where it came from.", BadColor);
                };
            }
            else if (outcome == 2)
            {
                MageKnowledge._deferredInquiry = () =>
                {
                    var item = DemonBloodItem();
                    if (item != null && MobileParty.MainParty?.ItemRoster != null)
                        MobileParty.MainParty.ItemRoster.AddToCounts(item, 2 + _rng.Next(3));
                    Msg("The Bloodbound brokers remember a useful, uncomplaining partner. A courier leaves a case of Demon Blood at your camp with no note attached — and the standing, unspoken expectation that you will be just as useful the next time they ask.", GoodColor);
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. BORN AT THE TURNING — village enter, long deferral
        // ═══════════════════════════════════════════════════════════════════
        private static void E_DuskbornChild(Settlement s)
        {
            _duskbornSettlementId = s.StringId;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  Born at the Turning",
                "A midwife shows you a newborn, delivered at the exact moment dusk crossed into full dark — the village is already calling the child demon-marked and wants it given back to the Night before it can grow into whatever it was born as. The mother says nothing. She is past arguing and not yet past hoping someone else will.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", "Take the child into your protection.", null, true,
                        "One more mouth to feed on a hard road."),
                    new InquiryElement("b", "Pay the village to shelter it properly.", null, true,
                        "Coin enough to buy the child a chance at an ordinary childhood."),
                    new InquiryElement("c", "Leave it to their judgment.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                            AddMorale(-3f);
                            ShiftTrait(DefaultTraits.Mercy, 1);
                            _duskbornOutcome   = 1;
                            _duskbornCountdown = 60 + _rng.Next(31);
                            Msg("You take the child. Your men grumble about the extra mouth for exactly one evening and then stop, the way soldiers do about children they've decided to like.", GoodColor);
                            break;
                        case "b":
                            if (!ChangeGold(-300)) break;
                            _duskbornOutcome   = 2;
                            _duskbornCountdown = 60 + _rng.Next(31);
                            Msg("You pay enough that the village has no more excuse to be afraid of a hungry mouth than any other. The midwife takes the coin without meeting your eyes, which you decide to read as gratitude rather than shame.", GoodColor);
                            break;
                        case "c":
                            ShiftTrait(DefaultTraits.Mercy, -1);
                            _duskbornOutcome   = 3;
                            _duskbornCountdown = 60 + _rng.Next(31);
                            Msg("You leave it to their judgment and ride on before you can learn what that judgment was. You tell yourself it is not yours to carry. You do not entirely believe it.", DimColor);
                            break;
                    }
                }, null, "", false), false, true);
        }

        private static void FireDuskbornConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _duskbornCountdown = 1; return; }
            int outcome = _duskbornOutcome;
            _duskbornOutcome = 0;
            string sId = _duskbornSettlementId;
            _duskbornSettlementId = null;
            var s = sId != null ? Settlement.All.FirstOrDefault(se => se.StringId == sId) : null;

            if (outcome == 1 || outcome == 2)
            {
                if (_rng.NextDouble() < 0.85)
                {
                    MageKnowledge._deferredInquiry = () =>
                        Msg("Months compress into a single report reaching you on the road: the duskborn child is simply a child, quick-eyed and unremarkable except for a habit of tapping rhythms against tables that no one taught them — the kind of habit a Spellbook scribe would recognise instantly and everyone else mistakes for restlessness. Someone should teach them, eventually. You make a note of it.", GoodColor);
                }
                else
                {
                    MageKnowledge._deferredInquiry = () =>
                    {
                        try { DemonSpawnCampaignBehavior.SpawnAmbushNear(MobileParty.MainParty.GetPosition2D, ""); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        Msg("The village's fear was not entirely wrong after all. Whatever the child drew toward it at the moment of its birth has finally come looking — years late, patient the way the Night is patient. You deal with it, but you understand now why they wanted it given back.", BadColor);
                    };
                }
            }
            else
            {
                MageKnowledge._deferredInquiry = () =>
                {
                    ChangeRelWithOwner(s, -6);
                    Msg("Whatever the village decided to do with the child, it was not gentle, and word of it has reached the surrounding hamlets. They do not tell you outright what happened. The way people in that region look at you now tells you enough.", BadColor);
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5. THE FAMILY THAT WOULD NOT OPEN — village leave, dusk
        // ═══════════════════════════════════════════════════════════════════
        private static void E_SaltCircle(Settlement s)
        {
            _saltCircleSettlementId = s.StringId;
            float charmChance = SkillChance(DefaultSkills.Charm, 0.35f);
            string charmHint  = SkillHint(DefaultSkills.Charm, 0.35f, "Talk them down through the door");

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  The Family That Would Not Open",
                "A shuttered croft at the village edge, salt laid thick across the threshold. A family huddles inside, convinced — loudly, through the door — that you are a demon wearing a borrowed face. Dusk is closing fast and your column needs the shelter more than it needs an argument.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", $"Talk them down. ({(int)(charmChance * 100)}% Charm)", null, true, charmHint),
                    new InquiryElement("b", "Break the door. You need the shelter.", null, true,
                        "They will scatter into the Night rather than let you in."),
                    new InquiryElement("c", "Leave food on the step and go.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                            if (SkillRoll(DefaultSkills.Charm, 0.35f))
                            {
                                ShiftTrait(DefaultTraits.Honor, 1);
                                _saltCircleOutcome   = 3;
                                _saltCircleCountdown = 20 + _rng.Next(21);
                                Msg("You talk to them through the door for a long time, low and unhurried, until the bolt finally slides back on its own. They do not apologise. They do not need to. You did not need the shelter as badly as you needed them to see that fear was the only thing behind that door.", GoodColor);
                            }
                            else
                            {
                                Msg("You talk to them through the door and nothing you say reaches past the salt line. Eventually you give up and make camp outside their walls instead. They watch you through a gap in the shutters until you're asleep, or pretend to be.", DimColor);
                            }
                            break;
                        case "b":
                            ShiftTrait(DefaultTraits.Honor, -1);
                            ChangeCrime(5f);
                            _saltCircleOutcome   = 2;
                            _saltCircleCountdown = 20 + _rng.Next(21);
                            Msg("You break the door. The family scatters out the back and into the dark rather than stay under a roof with what they believe you are. You have the shelter. You do not have the family, and you do not know yet what the Night made of that choice.", BadColor);
                            break;
                        case "c":
                            ShiftTrait(DefaultTraits.Generosity, 1);
                            _saltCircleOutcome   = 1;
                            _saltCircleCountdown = 20 + _rng.Next(21);
                            Msg("You leave what food you can spare on the step, in plain sight of the gap in the shutters, and make camp a respectful distance off. Nobody opens the door. Somebody, eventually, takes the food.", GoodColor);
                            break;
                    }
                }, null, "", false), false, true);
            _saltCircleCooldown = 40 + _rng.Next(21);
        }

        private static void FireSaltCircleConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _saltCircleCountdown = 1; return; }
            int outcome = _saltCircleOutcome;
            _saltCircleOutcome = 0;
            string sId = _saltCircleSettlementId;
            _saltCircleSettlementId = null;
            var s = sId != null ? Settlement.All.FirstOrDefault(se => se.StringId == sId) : null;

            switch (outcome)
            {
                case 1:
                    MageKnowledge._deferredInquiry = () =>
                    {
                        AddMorale(5f);
                        Msg("Weeks on, a jar of preserves turns up at your camp, left by a courier who won't say who sent it or how they found you. A child in that family, you hear later, has taken to calling themselves after you. The fire you didn't have to spend was, it turns out, the entire point.", GoodColor);
                    };
                    break;
                case 2:
                    MageKnowledge._deferredInquiry = () =>
                    {
                        ChangeRelWithOwner(s, -10);
                        Msg("The rumour that you are demon-touched has outrun you along the road — a family scattered into the dark rather than share a roof with you, and that story travels faster and stranger with every retelling. A Temple prelate two towns over has started asking pointed questions about your movements.", BadColor);
                    };
                    break;
                case 3:
                    MageKnowledge._deferredInquiry = () =>
                        Msg("Nothing further comes of the family behind the salt-laid door. That is, on reflection, the whole point of the choice you made — the fire you didn't have to spend was worth exactly as much as it cost you, which was nothing at all.", DimColor);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6. THE BELL OF A NAMELESS TOWN — town enter, ownerless city-state
        // ═══════════════════════════════════════════════════════════════════
        private static void E_MusterBell(Settlement s)
        {
            _musterBellSettlementId = s.StringId;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "◈  The Bell of a Nameless Town",
                $"{s.Name} answers to no kingdom, and its muster bell is ringing — an ownerless city-state with no banner to call for help, begging any armed company passing through to hold the wall for one more night. The captain who meets you at the gate has the specific exhaustion of someone who has asked this question of travelers before and been refused more often than not.",
                new List<InquiryElement>
                {
                    new InquiryElement("a", "Help hold the wall. No price named.", null, true,
                        "A real fight, offered freely."),
                    new InquiryElement("b", "Name your price first, then help.", null, true,
                        "Coin up front. It costs you something else."),
                    new InquiryElement("c", "Decline. It is not your wall.", null, true, ""),
                },
                false, 1, 1, "Decide", "",
                chosen =>
                {
                    switch (chosen?[0]?.Identifier as string)
                    {
                        case "a":
                        {
                            bool hurt = _rng.NextDouble() < 0.35;
                            if (hurt) WoundPlayer();
                            ShiftTrait(DefaultTraits.Honor, 1);
                            ChangeRenown(10f);
                            _musterBellOutcome   = 1;
                            _musterBellCountdown = 15 + _rng.Next(21);
                            Msg(hurt
                                ? "You hold the wall through the worst of it and come away marked for the trouble — but the wall holds, and the captain does not forget who stood on it without being asked what it was worth."
                                : "You hold the wall through a night that never quite breaks into the disaster it threatens to be. The captain watches you work and says nothing, which from him is evidently high praise.", GoodColor);
                            break;
                        }
                        case "b":
                            ChangeGold(400);
                            ShiftTrait(DefaultTraits.Calculating, 1);
                            _musterBellOutcome   = 2;
                            _musterBellCountdown = 15 + _rng.Next(21);
                            Msg("You name a price before you draw a blade. The captain pays it without much grace, and you hold the wall regardless — the coin bought your presence, not your effort, and you gave both anyway.", GoldColor);
                            break;
                        case "c":
                            ShiftTrait(DefaultTraits.Mercy, -1);
                            _musterBellOutcome   = 3;
                            _musterBellCountdown = 15 + _rng.Next(21);
                            Msg("You decline. The bell keeps ringing behind you as you ride, calling for a company that isn't yours to be.", DimColor);
                            break;
                    }
                }, null, "", false), false, true);
            _musterBellCooldown = 30 + _rng.Next(26);
        }

        private static void FireMusterBellConsequence()
        {
            if (MageKnowledge._deferredInquiry != null) { _musterBellCountdown = 1; return; }
            int outcome = _musterBellOutcome;
            _musterBellOutcome = 0;
            string sId = _musterBellSettlementId;
            _musterBellSettlementId = null;
            var s = sId != null ? Settlement.All.FirstOrDefault(se => se.StringId == sId) : null;
            string sName = s?.Name?.ToString() ?? "the town";

            switch (outcome)
            {
                case 1:
                    MageKnowledge._deferredInquiry = () =>
                    {
                        ChangeRelWithOwner(s, 15);
                        Msg($"{sName} opens its gates to you for good. Its captain's name, when you need a favor called in, turns out to be worth exactly what he implied it was.", GoodColor);
                    };
                    break;
                case 2:
                    MageKnowledge._deferredInquiry = () =>
                        Msg($"The story of the sellsword who haggled while the wall of {sName} nearly fell has made its way to the next town over, told two different ways by two different people who were both there. They lived. That part of the story is somehow always mentioned second.", DimColor);
                    break;
                case 3:
                    if (_rng.NextDouble() < 0.4)
                    {
                        MageKnowledge._deferredInquiry = () =>
                        {
                            ChangeRelWithOwner(s, -15);
                            Msg($"The wall of {sName} did not hold that night. Its refugees are on the roads now, and some of them remember exactly which company rode past the muster bell without answering it.", BadColor);
                        };
                    }
                    else
                    {
                        MageKnowledge._deferredInquiry = () =>
                            Msg($"{sName} held its own wall without you, in the end. Nobody there remembers you rode past — they had a longer night to remember instead.", DimColor);
                    }
                    break;
            }
        }
    }
}
