// =============================================================================
// THE DARKEST NIGHT — AshenRuins/AshenRuinSystem.Exploration.cs
// Challenge resolution flow and challenge dispatch.
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
        // ── Challenge resolution ───────────────────────────────────────────────
        // Called from the menu "Enter" option.
        public static void BeginExploration(RuinDef def, bool isSolo)
        {
            if (def == null) return;
            // Contest intercept
            if (IsContested(def.VillageName))
            {
                ShowContestDialog(def, isSolo);
                return;
            }
            ShowIntro(def, isSolo, 0, false);
        }

        private static void ShowContestDialog(RuinDef def, bool isSolo)
        {
            Hero lord = ContestedBy(def.VillageName);
            string lordName = lord?.Name?.ToString() ?? "a mage lord";

            InformationManager.ShowInquiry(new InquiryData(
                def.RuinName,
                $"{lordName} is already inside. You can hear movement deeper in.",
                true, true, "Confront them", "Wait outside",
                () =>
                {
                    // Confront: relation check
                    int rel = lord != null
                        ? CharacterRelationManager.GetHeroRelation(Hero.MainHero, lord)
                        : -10;
                    bool share = rel >= 0;
                    if (share)
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            def.RuinName,
                            $"{lordName} acknowledges you. You explore together — there is a strange camaraderie in it. The reward is halved, but neither of you walks alone into the dark.",
                            true, false, "Enter together", "",
                            () =>
                            {
                                ClearContest();
                                ShowIntro(def, isSolo, 0, true); // sharedReward = true
                            }, null), true);
                    }
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(
                            def.RuinName,
                            $"{lordName} turns to face you with cold eyes. The fire between you is not warm. You can fight — or retreat.",
                            true, true, "Stand your ground (race them)", "Retreat",
                            () =>
                            {
                                ClearContest();
                                ShowIntro(def, isSolo, 0, false);
                            },
                            () => { }), true);
                    }
                },
                () => { }), true);
        }

        private static void ClearContest()
        {
            _lordRacingRuin  = null;
            _lordRacingId    = null;
            _lordRacingDaysLeft = 0;
        }

        private static void ShowIntro(RuinDef def, bool isSolo, int roomIdx, bool sharedReward)
        {
            string soloNote = isSolo ? "\n\nYou are alone. Some passages are quieter without an army. Others are harder." : "";
            InformationManager.ShowInquiry(new InquiryData(
                def.RuinName,
                def.EntryLore + soloNote,
                true, true, "Enter", "Turn back",
                () => RunRoom(def, isSolo, roomIdx, sharedReward),
                () => { }), true);
        }

        private static void RunRoom(RuinDef def, bool isSolo, int roomIdx, bool sharedReward)
        {
            if (roomIdx >= def.Challenges.Length)
            {
                // All rooms cleared. A repeat clear (cooldown already expired,
                // ruin already in the first-cleared ledger) substitutes the
                // partial-tier reward for any one-time unique — those must
                // never be granted twice (see _oneTimeUniqueRewards).
                bool repeatClear = IsCleared(def.VillageName);
                MarkCleared(def.VillageName);
                bool substitute = repeatClear
                    && def.MainReward != null
                    && _oneTimeUniqueRewards.Contains(def.MainReward.Type);
                var reward = substitute ? def.PartialReward : def.MainReward;
                GrantReward(reward, sharedReward, def.RuinName, full: true, repeatSubstituted: substitute);
                return;
            }

            var challenge = def.Challenges[roomIdx];
            DispatchChallenge(challenge, def, isSolo, roomIdx, sharedReward);
        }

        // After successfully passing a room, advance to next room.
        private static void NextRoom(RuinDef def, bool isSolo, int roomIdx, bool sharedReward)
            => RunRoom(def, isSolo, roomIdx + 1, sharedReward);

        // On retreat, give partial reward if at least one room was passed.
        private static void OnRetreat(RuinDef def, int roomsPassed)
        {
            SetCooldown(def.VillageName, 14);
            if (roomsPassed > 0)
                GrantReward(def.PartialReward, false, def.RuinName, full: false);
            else
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You leave {def.RuinName} — something for another day.",
                    new Color(0.6f, 0.55f, 0.5f)));
        }

        // ── Challenge dispatch ─────────────────────────────────────────────────
        private static void DispatchChallenge(RuinChallenge c, RuinDef def, bool isSolo, int roomIdx, bool sharedReward)
        {
            switch (c.Type)
            {
                case ChallengeType.BloodLock:
                    Ch_BloodLock(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.VisionChamber:
                    Ch_Vision(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.SoulHarvest:
                    Ch_SoulHarvest(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.VoidMaw:
                    Ch_VoidMaw(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.AncientTrap:
                    Ch_AncientTrap(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.SealedMemory:
                    Ch_SealedMemory(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.CollapsingChamber:
                    Ch_CollapsingChamber(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.SpectralGuardian:
                    Ch_SpectralGuardian(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.VoidWhisper:
                    Ch_VoidWhisper(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.RiddleGate:
                    Ch_RiddleGate(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.AshenSentinel:
                    Ch_AshenSentinel(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.SerpentNest:
                    Ch_SerpentNest(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.PoisonedAir:
                    Ch_PoisonedAir(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.CursedHoard:
                    Ch_CursedHoard(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.MirrorGate:
                    Ch_MirrorGate(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.TemporalCrack:
                    Ch_TemporalCrack(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.SleepingGiant:
                    Ch_SleepingGiant(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.NecromanticWard:
                    Ch_NecromanticWard(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.DragonEgg:
                    Ch_DragonEgg(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.AshenFlame:
                    Ch_AshenFlame(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.EmberWraith:
                    Ch_EmberWraith(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.WardstoneGate:
                    Ch_WardstoneGate(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.HollowChoir:
                    Ch_HollowChoir(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.EmberToll:
                    Ch_EmberToll(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.WeightOfAsh:
                    Ch_WeightOfAsh(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.ShiftingHall:
                    Ch_ShiftingHall(c, def, isSolo, roomIdx, sharedReward); break;
                case ChallengeType.TriuneReckoning:
                    Ch_TriuneReckoning(c, def, isSolo, roomIdx, sharedReward); break;
                default:
                    NextRoom(def, isSolo, roomIdx, sharedReward); break;
            }
        }

    }
}
