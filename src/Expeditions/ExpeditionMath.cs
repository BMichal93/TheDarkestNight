// =============================================================================
// THE DARKEST NIGHT — Expeditions/ExpeditionMath.cs
//
// Pure numeric logic for the Expeditions ("Antiquarian Charter") — Revyl's
// mercenary free-camp ("The Camp", CityStateSystem.IsCampSettlement)
// town-menu ruin-expedition system. Originally a Legion ("empire_w") offer,
// paid in influence; moved to The Camp and repriced in gold (GoldCost below)
// while every other formula here is untouched. No TaleWorlds types; covered
// directly by PureLogicTests. See ExpeditionCampaignBehavior.cs for the
// state/tick machinery and ExpeditionCampaignBehavior.Menus.cs for the
// town-menu flow.
// =============================================================================

using System;

namespace AshAndEmber
{
    // ── Leader specialties ──────────────────────────────────────────────────
    // Data-only: leaders in the pool are records, never real Hero objects.
    public enum ExpeditionLeaderSpecialty
    {
        ScholarOfTheOldScript,      // +success vs high-tier ruins, slower
        VeteranOfTheSouthernRoads,  // -duration, -loot yield
        TombRobber,                 // +loot, chance to abscond with part of it on success
        ZealousAntiquarian,         // +success, small chance "the Ashen take note"
    }

    // ── Core team rosters ────────────────────────────────────────────────────
    public enum ExpeditionTeamType
    {
        LegionVeterans,   // +survival/failure mitigation, higher influence cost
        ImperialScholars, // +success on high-tier ruins, fragile — worse failures
        HiredBlades,      // cheap, chance to desert with the loot
        TempleWardens,    // +vs "cold"/Ashen ruins, refuse the darkest sites
    }

    public static class ExpeditionMath
    {
        // ── Tuning bounds ────────────────────────────────────────────────────
        public const int MinInfluenceCost = 20;
        public const int MaxInfluenceCost = 150;
        public const int MinSuccessChance = 10;
        public const int MaxSuccessChance = 95;
        public const int MinDurationDays  = 3;

        // Recovery cooldown (Feature 2) shares the ruin's own AshenRuinMath roll —
        // Expeditions simply calls AshenRuinSystem.MarkClearedByExpedition, which
        // rolls through AshenRuinMath.RecoveryCooldownDays itself. Nothing to
        // duplicate here.

        private static bool IsHighTier(RuinTier tier) => tier >= RuinTier.Brutal;

        // ── Success chance ───────────────────────────────────────────────────
        public static int BaseSuccessChance(RuinTier tier) => tier switch
        {
            RuinTier.Easy     => 75,
            RuinTier.Standard => 60,
            RuinTier.Brutal   => 45,
            _                 => 30, // Legendary
        };

        public static int SuccessChance(RuinTier tier, ExpeditionLeaderSpecialty leader,
            ExpeditionTeamType team, bool leaderProven)
        {
            int chance = BaseSuccessChance(tier);
            bool highTier = IsHighTier(tier);

            switch (leader)
            {
                case ExpeditionLeaderSpecialty.ScholarOfTheOldScript:
                    if (highTier) chance += 15;
                    break;
                case ExpeditionLeaderSpecialty.ZealousAntiquarian:
                    chance += 10;
                    break;
                // VeteranOfTheSouthernRoads and TombRobber carry no success modifier —
                // their edge shows up in duration/loot instead.
            }

            switch (team)
            {
                case ExpeditionTeamType.ImperialScholars:
                    if (highTier) chance += 10;
                    break;
                case ExpeditionTeamType.HiredBlades:
                    chance -= 5; // cheap and unreliable
                    break;
                // LegionVeterans mitigates FAILURE severity, not the roll itself —
                // see LeaderLostOnFailure/team survival handling in the behavior.
            }

            if (leaderProven) chance += 5; // a leader who has come back before is trusted more

            return Math.Max(MinSuccessChance, Math.Min(MaxSuccessChance, chance));
        }

        // ── Duration ──────────────────────────────────────────────────────────
        // 6 + 2×tier ± leader/team modifiers, floored at MinDurationDays.
        public static int DurationDays(RuinTier tier, ExpeditionLeaderSpecialty leader, ExpeditionTeamType team)
        {
            int days = 6 + 2 * (int)tier;

            if (leader == ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads) days -= 2;
            else if (leader == ExpeditionLeaderSpecialty.ScholarOfTheOldScript) days += 1;

            if (team == ExpeditionTeamType.HiredBlades) days -= 1;
            else if (team == ExpeditionTeamType.TempleWardens) days += 1;

            return Math.Max(MinDurationDays, days);
        }

        // ── Influence cost ───────────────────────────────────────────────────
        // Roughly 30-120, scaled by ruin tier and the chosen core team.
        public static int InfluenceCost(RuinTier tier, ExpeditionTeamType team)
        {
            int baseCost = tier switch
            {
                RuinTier.Easy     => 30,
                RuinTier.Standard => 55,
                RuinTier.Brutal   => 85,
                _                 => 120, // Legendary
            };

            float mult = team switch
            {
                ExpeditionTeamType.LegionVeterans => 1.20f, // hardened professionals cost more
                ExpeditionTeamType.HiredBlades    => 0.70f, // cheap
                ExpeditionTeamType.TempleWardens  => 1.10f,
                _                                  => 1.00f, // ImperialScholars
            };

            int cost = (int)Math.Round(baseCost * mult, MidpointRounding.AwayFromZero);
            return Math.Max(MinInfluenceCost, Math.Min(MaxInfluenceCost, cost));
        }

        // ── Gold cost (The Camp) ─────────────────────────────────────────────
        // The charter moved from the Legion's town menu to The Camp
        // (CityStateSystem.IsCampSettlement) — a mercenary free-camp with no
        // access to a clan's influence ledger, so it charges coin instead.
        // Derived straight from InfluenceCost (never duplicated/re-tuned) times
        // a flat multiplier: with InfluenceCost clamped to [20, 150], a tier-1
        // dig with a cheap team lands well inside what a mid-game party can
        // spare, while a top-tier Legendary charter with a premium team stings
        // hard without needing its own separate tuning curve.
        public const int GoldPerInfluenceUnit = 8;

        public static int GoldCost(RuinTier tier, ExpeditionTeamType team) =>
            InfluenceCost(tier, team) * GoldPerInfluenceUnit;

        // ── Reward composition (success) ─────────────────────────────────────
        public static int SuccessGold(RuinTier tier, ExpeditionLeaderSpecialty leader, int roll0To99)
        {
            int baseGold  = 150 + 120 * (int)tier;
            int variance  = (roll0To99 % 40) * 5; // 0-195 extra, deterministic from the same roll
            int gold      = baseGold + variance;
            if (leader == ExpeditionLeaderSpecialty.VeteranOfTheSouthernRoads)
                gold = (int)(gold * 0.8f); // -loot yield, traded for speed
            else if (leader == ExpeditionLeaderSpecialty.TombRobber)
                gold = (int)(gold * 1.25f); // +loot, before any abscond skim
            return Math.Max(0, gold);
        }

        public static int SuccessRenown(RuinTier tier) => 5 + 4 * (int)tier;

        public static bool RollGrantsCrystal(int roll0To99, RuinTier tier)
        {
            int chance = 20 + 5 * (int)tier; // 25 (Easy) .. 40 (Legendary)
            return roll0To99 < chance;
        }

        public static bool RollGrantsRelic(int roll0To99, RuinTier tier)
        {
            int chance = 5 + 3 * (int)tier; // 8 (Easy) .. 17 (Legendary)
            return roll0To99 < chance;
        }

        // Tomb-Robber: 25% chance to skim 40% of the gold haul on a successful run.
        public static float TombRobberAbscondFraction(int roll0To99)
            => roll0To99 < 25 ? 0.4f : 0f;

        // Zealous Antiquarian: a small, purely-flavour chance the Ashen take note.
        public static bool RollAshenTakesNote(int roll0To99) => roll0To99 < 12;

        // ── Failure resolution ───────────────────────────────────────────────
        public static bool LeaderLostOnFailure(int roll0To99, RuinTier tier, ExpeditionTeamType team)
        {
            int chance = 10 + 8 * (int)tier; // 18 (Easy) .. 42 (Legendary)
            if (team == ExpeditionTeamType.LegionVeterans) chance -= 10; // survival/failure mitigation
            chance = Math.Max(0, chance);
            return roll0To99 < chance;
        }
    }
}
