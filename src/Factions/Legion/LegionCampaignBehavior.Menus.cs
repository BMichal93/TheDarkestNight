// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionCampaignBehavior.Menus.cs
//
// Game menu implementation for the Legion's town option — Legion towns only
// (Lageta/Ortysia, or whatever the kingdom currently holds — see
// LegionSettlements.IsLegionSettlement). Mirrors EmpireCampaignBehavior.
// Menus.cs / TempleCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Train at the fields" — spend 1 focus point, gain 1 focus point
//             in each of TWO random skills drawn from the Vigor/Control/
//             Endurance attribute groups (LegionMath.PickTwoDistinctSkillIndices).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public partial class LegionCampaignBehavior
    {
        // Index → SkillObject mapping for the training-fields pool: 0-3 Vigor
        // (OneHanded, TwoHanded, Polearm, Bow), 4-6 Control (Crossbow, Throwing,
        // Riding), 7-8 Endurance (Athletics, Crafting). Mirrors the vanilla
        // character-sheet attribute groupings exactly.
        private static SkillObject SkillForIndex(int index)
        {
            switch (index)
            {
                case 0: return DefaultSkills.OneHanded;
                case 1: return DefaultSkills.TwoHanded;
                case 2: return DefaultSkills.Polearm;
                case 3: return DefaultSkills.Bow;
                case 4: return DefaultSkills.Crossbow;
                case 5: return DefaultSkills.Throwing;
                case 6: return DefaultSkills.Riding;
                case 7: return DefaultSkills.Athletics;
                case 8: return DefaultSkills.Crafting;
                default: return null;
            }
        }

        private static void RegisterLegionMenus(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "legion_training_fields", "{LEGION_TRAIN_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!LegionSettlements.IsLegionSettlement(Settlement.CurrentSettlement)) return false;

                            int have = Hero.MainHero?.HeroDeveloper?.UnspentFocusPoints ?? 0;
                            bool ready = have >= LegionMath.TrainingFieldFocusCost;
                            MBTextManager.SetTextVariable("LEGION_TRAIN_TEXT",
                                ready ? $"Train at the fields  [{LegionMath.TrainingFieldFocusCost} focus point -> 2 skills]"
                                      : "Train at the fields (no focus points to spend)");
                            args.IsEnabled = ready;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { DoTrainAtFields(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DoTrainAtFields()
        {
            Hero hero = Hero.MainHero;
            if (hero?.HeroDeveloper == null) return;
            if (hero.HeroDeveloper.UnspentFocusPoints < LegionMath.TrainingFieldFocusCost) return;

            hero.HeroDeveloper.UnspentFocusPoints -= LegionMath.TrainingFieldFocusCost;

            LegionMath.PickTwoDistinctSkillIndices(_rng.NextDouble(), _rng.NextDouble(), out int i1, out int i2);
            SkillObject s1 = SkillForIndex(i1);
            SkillObject s2 = SkillForIndex(i2);

            string granted = "";
            granted += TryGrantFocus(hero, s1);
            granted += TryGrantFocus(hero, s2);

            if (string.IsNullOrEmpty(granted))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "The drill-masters run you through the paces, but you've mastered every discipline they can teach.",
                    new Color(0.7f, 0.65f, 0.55f)));
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"The drill-masters push you through the fields at a brutal pace.{granted}",
                new Color(0.7f, 0.65f, 0.55f)));
        }

        private static string TryGrantFocus(Hero hero, SkillObject skill)
        {
            if (skill == null) return "";
            try
            {
                if (!hero.HeroDeveloper.CanAddFocusToSkill(skill)) return "";
                hero.HeroDeveloper.AddFocus(skill, LegionMath.TrainingFieldFocusPerSkill, false);
                return $" +{LegionMath.TrainingFieldFocusPerSkill} focus in {skill.Name}.";
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return ""; }
        }
    }
}
