// =============================================================================
// THE DARKEST NIGHT — Units/PromotionCampaignBehavior.cs
//
// Requirement 4 / 30b consumption side. The gate (whether the party is even
// ALLOWED to attempt the upgrade / prisoner recruit) lives in the model
// overrides (Economy/ScarcityModels.cs EconomyTroopUpgradeModel, and
// UnitsRecruitModel below); this behavior only fires once the engine has
// actually completed the transaction, and spends the toll out of the party's
// item roster. Stateless — nothing here needs to survive a save.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace TheDarkestNight
{
    public class PromotionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, OnPlayerUpgradedTroops);
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent state — the toll is spent the moment the upgrade/
            // recruit completes, nothing carries across a save.
        }

        private static void OnPlayerUpgradedTroops(CharacterObject original, CharacterObject upgraded, int count)
        {
            try
            {
                if (upgraded == null || !UnitsMath.RequiresPromotionToll(upgraded.Tier)) return;
                var party = MobileParty.MainParty?.Party;
                if (party == null) return;
                PromotionToll.ConsumePromotionToll(party, count);
                TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(
                    "The muster toll is paid — a horse, an armour piece, a good blade, spent so the line can hold a little longer.",
                    new TaleWorlds.Library.Color(0.75f, 0.65f, 0.4f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Requirement 30b: recruiting a tier-5 troop (e.g. converting a tier-5
        // prisoner straight into your roster) costs an armour piece and a good
        // weapon, same as the tail end of a promotion toll. Vanilla volunteer
        // recruitment never hands out tier-5 troops directly, so in practice
        // this fires from the prisoner-recruitment screen (gated up front by
        // UnitsRecruitModel.IsPrisonerRecruitable) — but the hook is generic,
        // so any other mod-added tier-5 recruit path pays the same toll.
        private static void OnTroopRecruited(Hero recruiterHero, TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
            Hero recruitmentSource, CharacterObject troop, int count)
        {
            try
            {
                if (recruiterHero != Hero.MainHero) return;
                if (troop == null || !UnitsMath.IsTier5Recruit(troop.Tier)) return;
                var party = MobileParty.MainParty?.Party;
                if (party == null) return;
                PromotionToll.ConsumeRecruitToll(party, count);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
