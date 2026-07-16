// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Bloodbound/BloodboundQuestMath.cs
//
// Pure numeric core of "The Surpassing Rite" — Faction D's (the Bloodbound,
// formerly Khuzait) Phase 12 questline (Requirement 21). No TaleWorlds types
// (fully covered by PureLogicTests). Runtime behaviour lives in
// BloodboundQuestCampaignBehavior.*/BloodboundQuestLog.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// Every Bloodhunter already knows the small workings a few vials can buy
// (Factions/Bloodbound/BloodboundMath.cs — a few days unseen, a week of
// hardened flesh, a permanent trade of mind for body). The Huntmaster
// believes the same blood, taken in one enormous, unified draught, can do
// what no single vial ever could: remake every drinker past the reach of the
// dark entirely. The player (and, per Great Awakening's precedent, the
// Bloodbound's own lords) feed that draught by donating Demon Blood
// (Factions/Bloodbound/BloodboundCatalog.DemonBloodItemId) at the chosen
// shrine city, reusing GreatAwakeningCampaignBehavior's altar-donation shape
// (GreatAwakening/GreatAwakeningCampaignBehavior.Altar.cs).
//
// ── The donation threshold — why 750 is "genuinely enormous" here ──────────
// Demon Blood is not a battle byproduct like the prisoners Great Awakening
// (10,000) or the Forest Widows' Final Peace (3,000, ForestWidowsQuestMath.cs)
// sacrifice — those are captured in whatever numbers a single battle happens
// to yield (a big siege can hand over dozens at once). Demon Blood is capped
// HARD at 1-3 vials per personally-won demon-party victory, regardless of how
// large that fight was (BloodboundMath.RollDemonBloodYield, average 2). That
// makes every single vial far more expensive to earn than a captured soldier,
// so the raw target has to sit well below Great Awakening's or the Final
// Peace's numbers even though it must still feel enormous:
//   • DonationTarget = 750 vials ÷ the ~2-vial average yield per victory =
//     roughly 375 personally-won demon-party fights if the player farmed the
//     whole draught alone — over 6x the ~60-fight grind TowerRiteMath reasons
//     is already "real, sustained play deep into the Night Tide" for ONE of
//     ITS three tracks (TowerRiteMath.cs). This is the entire point of the D
//     questline rather than one ingredient among several, so it earns a much
//     bigger multiple of that baseline.
//   • The Bloodbound are scoped to the same two-seat economy as the Forest
//     Widows (BloodboundMath.StartingTownIds — Akkalat, Chaikand), which is
//     why this stays an order of magnitude under Great Awakening's sprawling-
//     kingdom 10,000, even before accounting for Demon Blood's harder cap.
//   • Trading for it (Demon Blood is a registered tradeable "wine"-category
//     Goods item, value 120 — see TowerRiteMath's own reasoning) helps, but a
//     Bloodbound player is exactly who a barter partner would rather keep the
//     stock — this is intentionally not a target a purse alone can shortcut.
// NPC Bloodhunter lords also feed the shrine in the background (mirrors
// GreatAwakeningMath/ForestWidowsQuestMath's NPC trickle), which is what
// makes 750 reachable within one campaign rather than a purely theoretical
// number — exactly the balance-pass note's "quantities must scale against the
// barter economy... this should take real, sustained hunting."
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class BloodboundQuestMath
    {
        // ── The draught — "an enormous amount of Demon Blood" ───────────────────
        public const int DonationTarget = 750;

        public static bool HasReachedThreshold(int bloodDonated) => bloodDonated >= DonationTarget;

        // Clamped 0..threshold, for the discrete journal objective bar.
        public static int ClampedProgress(int bloodDonated)
        {
            if (bloodDonated < 0) return 0;
            if (bloodDonated > DonationTarget) return DonationTarget;
            return bloodDonated;
        }

        // ── NPC contribution — background trickle, never the primary driver ─────
        // Bloodbound lords hold Demon Blood on their own party's item roster
        // (BloodboundCampaignBehavior.GrantDemonBlood already pays them out the
        // same way it pays the player). Tuned smaller than ForestWidowsQuestMath's
        // prisoner trickle (5-20) because a lord's own held stock is itself
        // capped at a few vials per victory, never a bulk haul.
        public const float NpcWeeklyContributionChance = 0.25f;
        public const int   NpcContributionMin = 2;
        public const int   NpcContributionMax = 8;

        public static int NpcContributionAmount(Random rng, int bloodHeld)
        {
            if (rng == null || bloodHeld <= 0) return 0;
            int roll = NpcContributionMin + rng.Next(NpcContributionMax - NpcContributionMin + 1);
            return roll < bloodHeld ? roll : bloodHeld;
        }

        // ── The aftermath — demons descend on the emptied seats ─────────────────
        // A short, readable gap between "every Bloodhunter drinks and dies" and
        // "the dark comes for what's left" — long enough to read as its own
        // scene, short enough that the consequence never feels detached from
        // the choice that caused it.
        public const int DemonAttackDelayDays = 5;

        public static bool IsDemonAttackDue(int daysSinceMassDeath) => daysSinceMassDeath >= DemonAttackDelayDays;

        // Two ambush bands per former Bloodbound seat — enough to read as a real
        // assault on an undefended town (mirrors TowerRiteMath.HostPartyCount's
        // "several separate war-bands" reasoning), while each individual band
        // stays within DemonSpawnCampaignBehavior.SpawnAmbushNear's normal single-
        // party sizing (DemonMath.PartyBodyCount) rather than one inflated blob.
        public const int AttackPartiesPerSettlement = 2;
    }
}
