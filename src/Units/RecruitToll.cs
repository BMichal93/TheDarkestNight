// =============================================================================
// THE DARKEST NIGHT — Units/RecruitToll.cs
//
// Requirement 30b: "tier 5 stays good gear-wise, but recruiting a tier-5 unit
// should also cost items too." The one deterministic vanilla flow that hands
// the player a fresh troop at ANY tier — including 5 — in a single click is
// converting a captured prisoner straight into your own roster (the party-
// screen "recruit prisoner" action), gated by
// DefaultPrisonerRecruitmentCalculationModel.IsPrisonerRecruitable. Gating
// that call is therefore the correct, narrow seam: it blocks the exact
// action that could otherwise mint a tier-5 unit for gold alone.
//
// Consumption happens afterwards in PromotionCampaignBehavior (listening to
// OnTroopRecruitedEvent), the same split as the promotion toll.
// =============================================================================

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace TheDarkestNight
{
    internal sealed class UnitsRecruitModel : DefaultPrisonerRecruitmentCalculationModel
    {
        public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
        {
            bool baseResult = base.IsPrisonerRecruitable(party, character, out conformityNeeded);
            if (!baseResult) return false;
            try
            {
                if (character != null && UnitsMath.IsTier5Recruit(character.Tier))
                    return PromotionToll.HasRecruitToll(party);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            return baseResult;
        }
    }
}
