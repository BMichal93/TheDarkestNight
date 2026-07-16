// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntQuestCampaignBehavior.Menus.cs
//
// The final choice, once all three beasts are dead: feed the pack the last
// beast's flesh (Transformation) or burn it (Burning). Mirrors
// WolfBrothersCampaignBehavior.Menus.cs's larder-menu shape — a "town" option
// gated to a Wolf Brothers settlement, this time also gated on
// PhaseAwaitingFinalChoice.
//
// Both endings are permanent and mechanical, not flavour-only:
//   Transformation — a genuine permanent HP-ceiling raise (WolfHuntMath.
//                    TransformationHpBonus) plus a further Mercy-down/Valor-up
//                    trait shift (the same pair the join ritual already moves).
//   Burning        — a partial Mercy restored, and a LARGER renown gain than
//                    Transformation's (word of the restraint travels further
//                    than word of the kill).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public sealed partial class WolfHuntQuestCampaignBehavior
    {
        private static void RegisterFinalChoiceMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "wolfhunt_judgment_enter", "{WOLFHUNT_JUDGMENT_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseAwaitingFinalChoice) return false;
                            if (!WolfBrothersSettlements.IsWolfBrothersSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("WOLFHUNT_JUDGMENT_TEXT", "Bring the last kill before the pack");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { ShowFinalChoice(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ShowFinalChoice()
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Pack's Judgment",

                    "Three kills lie behind you, each larger than the last. The pack has kept the flesh of " +
                    "the third on ice by the fire, waiting.\n\n" +
                    "The Packmaster looks at you the way the pack looks at anything it hasn't decided is " +
                    "Kinsman yet, or meat.\n\n" +
                    "\"Every Kinsman answers this the same way, sooner or later. Eat what you killed, and " +
                    "become what the pack already knows you can be — or burn it, and stay what you were. " +
                    "Either answer is a true one. Only cowards give none at all.\"",

                    true, true,
                    "Feed the pack the beast's flesh.",
                    "Burn it.",
                    OnChooseTransformation,
                    OnChooseBurning
                ), true, true);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnChooseTransformation()
        {
            try
            {
                _phase = PhaseEndedTransformation;
                Hero hero = Hero.MainHero;
                if (hero != null)
                {
                    // Genuine permanent ceiling raise — the same technique
                    // BloodboundMath.HpBuffAmount uses for its week-long
                    // Hardened Flesh (Hero.MaxHitPoints has no setter, so the
                    // buff lives in HitPoints itself), just never ticked back
                    // down by anything.
                    float ceiling = hero.MaxHitPoints + WolfHuntMath.TransformationHpBonus;
                    hero.HitPoints = (int)Math.Min(hero.HitPoints + (int)WolfHuntMath.TransformationHpBonus, (int)ceiling);

                    int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
                    hero.SetTraitLevel(DefaultTraits.Mercy, WolfBrothersMath.ClampTraitLevel(mercy, WolfHuntMath.TransformationMercyShift));
                    int valor = hero.GetTraitLevel(DefaultTraits.Valor);
                    hero.SetTraitLevel(DefaultTraits.Valor, WolfBrothersMath.ClampTraitLevel(valor, WolfHuntMath.TransformationValorShift));

                    if (hero.Clan != null) hero.Clan.Renown += WolfHuntMath.TransformationRenownGain;
                }

                WolfHuntQuestLog.Current?.LogTransformation();
                InformationManager.DisplayMessage(new InformationMessage(
                    "You eat what you killed. Something in you settles into place that was never going to " +
                    "fit any other way. (Permanent: the pack's hunger is yours now.)",
                    new Color(0.55f, 0.15f, 0.12f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnChooseBurning()
        {
            try
            {
                _phase = PhaseEndedBurning;
                Hero hero = Hero.MainHero;
                if (hero != null)
                {
                    int mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
                    hero.SetTraitLevel(DefaultTraits.Mercy, WolfBrothersMath.ClampTraitLevel(mercy, WolfHuntMath.BurningMercyShift));

                    if (hero.Clan != null) hero.Clan.Renown += WolfHuntMath.BurningRenownGain;
                }

                WolfHuntQuestLog.Current?.LogBurning();
                InformationManager.DisplayMessage(new InformationMessage(
                    "You burn it. The pack watches the smoke a long while before anyone speaks again. Word " +
                    "of the restraint travels further than the kill itself did.",
                    new Color(0.65f, 0.55f, 0.45f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
