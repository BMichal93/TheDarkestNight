// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireCampaignBehavior.Menus.cs
//
// Game menu implementation for the Empire's town option — Empire towns only
// (Saneopa/Diathma/Argoron, or whatever the kingdom currently holds — see
// EmpireSettlements.IsEmpireSettlement). Mirrors TempleCampaignBehavior.
// Menus.cs / BloodboundCampaignBehavior.Menus.cs.
//
// Menu tree:
//   "town" → "Claim your grain ration" (a single direct option — no submenu
//             needed, since the Empire offers exactly one town interaction)
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public partial class EmpireCampaignBehavior
    {
        private static void RegisterEmpireMenus(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "empire_claim_grain", "{EMPIRE_GRAIN_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (!EmpireSettlements.IsEmpireSettlement(Settlement.CurrentSettlement)) return false;

                            bool ready = CanClaimToday(Hero.MainHero);
                            MBTextManager.SetTextVariable("EMPIRE_GRAIN_TEXT",
                                ready ? $"Claim your grain ration  [{EmpireMath.GrainClaimAmount} grain]"
                                      : "Claim your grain ration (already claimed today)");
                            args.IsEnabled = ready;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Default; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { DoClaimGrain(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void DoClaimGrain()
        {
            Hero hero = Hero.MainHero;
            if (hero == null || !CanClaimToday(hero)) return;

            GrantGrain(MobileParty.MainParty);
            MarkClaimed(hero);

            InformationManager.DisplayMessage(new InformationMessage(
                $"A quartermaster weighs out your ration without a word — {EmpireMath.GrainClaimAmount} measures of grain, "
              + "logged and gone. \"The old rites,\" she says, \"still feed the ones who keep them.\"",
                new Color(0.78f, 0.72f, 0.55f)));
        }
    }
}
