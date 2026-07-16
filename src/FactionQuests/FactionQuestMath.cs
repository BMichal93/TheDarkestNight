// =============================================================================
// THE DARKEST NIGHT — FactionQuests/FactionQuestMath.cs
//
// Pure numeric core shared by every Phase 12 faction questline's trigger gate.
// No TaleWorlds types (fully covered by PureLogicTests). Runtime behaviour
// lives in FactionQuestTrigger.cs / FactionQuestTriggerCampaignBehavior.cs.
//
// Requirement 21: "from ~day 50, the player starts receiving notifications and
// journal entries to speak with each faction leader." A single flat gate,
// shared by all eight questlines — no per-faction stagger, no roll: once day
// 50 is reached, every faction whose quest has not yet been offered gets its
// one-time notification on the very next weekly tick. Reliability (Phase 12's
// balance-pass note: "endings must fire reliably") matters more here than
// pacing flavour, so this is deliberately simpler than GreatAwakeningMath's/
// NorthmenStonesMath's own escalating-chance rolls — those gate a single
// hidden discovery; this gates eight quests that must all become reachable.
// =============================================================================

namespace TheDarkestNight
{
    public static class FactionQuestMath
    {
        public const int TriggerStartDay = 50;

        public static bool IsTriggerEligible(int elapsedCampaignDays) => elapsedCampaignDays >= TriggerStartDay;
    }
}
