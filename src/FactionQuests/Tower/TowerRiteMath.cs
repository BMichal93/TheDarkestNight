// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Tower/TowerRiteMath.cs
//
// Pure numeric core of "The Unbinding Rite" — Faction B's (the Tower) Phase 12
// questline (Requirement 21). No TaleWorlds types (fully covered by
// PureLogicTests). Runtime behaviour lives in TowerRiteQuestCampaignBehavior.*,
// TowerRiteHostParty.cs, TowerRiteQuestLog.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// The Tower believes it can banish the Night Tide outright: gather three
// classes of grave-goods (a scholar's magical relics, the Bloodbound's
// vialed Demon Blood, the Temple's Holy Sigils) and burn them together in
// the Great Rite at Iyakis. The rite is not a gamble — it ALWAYS fails,
// because that failure IS the quest's point (a faction of scholars overreaching
// exactly the way this mod's Aserai/Tower lore already frames them: clever,
// not wise). Instead of banishing the Tide, the rite tears a hole wide enough
// for a whole demon HOST to pour through at once, which then rampages against
// nearby settlements for a fixed span before it burns itself out.
//
// ── Gather quantities — why these numbers are "genuinely hard" ─────────────
// Each of the three tracks is gated by a DIFFERENT existing scarcity system,
// so "genuinely hard" isn't invented here — it is inherited and then simply
// multiplied by a meaningful count:
//   • Relics (RelicMath.RelicDropChancePerVictory = 0.05, i.e. 1-in-20 demon-
//     party victories): requiring GatherRelicsRequired = 3 means an expected
//     ~60 personally-won demon fights (3 / 0.05) before the tally is met —
//     real, sustained play deep into the Night Tide, not a single lucky drop.
//   • Demon Blood (BloodboundMath.RollDemonBloodYield = 1-3 per victory, but
//     ONLY credited to a party that is itself Bloodbound-affiliated —
//     BloodboundCulture.IsBloodboundParty): a Tower-aligned player is
//     virtually never Bloodbound, so this track cannot be farmed by fighting
//     at all — it must be bought/bartered off the map's own tradeable-goods
//     economy (aae_demon_blood is a registered "wine"-category Goods item,
//     value 120), which is exactly Phase 2's barter scarcity biting: no
//     Bloodbound trade partner nearby, no stock, no shortcut.
//     GatherDemonBloodRequired = 12 keeps this a real trading campaign
//     (roughly 1,400 denars of goods at base value, assuming a willing seller
//     can even be found) rather than a rounding error next to a starting purse.
//   • Holy Sigils (TempleMath.SigilPurchaseCostGold = 350 denars, purchased
//     outright at a Temple town, no rarity gate beyond gold):
//     GatherHolySigilsRequired = 6 is 2,100 denars on its own — cheap next to
//     a late-game clan's income, but stacked on top of the other two tracks
//     it is real, cumulative pressure on the same purse the player also needs
//     for the demon-blood trades above.
// Together the three tracks cannot be satisfied by any single grind loop —
// exactly the "sustained play, not a quick errand" balance-pass note.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class TowerRiteMath
    {
        // ── Gather requirements ──────────────────────────────────────────────────
        public const int GatherRelicsRequired      = 3;
        public const int GatherDemonBloodRequired  = 12;
        public const int GatherHolySigilsRequired  = 6;

        public static bool MeetsGatherThreshold(int relics, int demonBlood, int holySigils)
            => relics >= GatherRelicsRequired
            && demonBlood >= GatherDemonBloodRequired
            && holySigils >= GatherHolySigilsRequired;

        // ── The catastrophe — the summoned host ──────────────────────────────────
        // Several separate war-bands rather than one giant roster, so the
        // rampage reads as "the whole region is suddenly under attack" instead
        // of a single blob the player fights once and is done with — and so
        // that killing one band doesn't require chasing down every demon the
        // rite ever produced.
        public const int HostPartyCount    = 3;
        public const int HostPartySizeEach = 40; // ~3.6x DemonMath.MaxPartyBodies per band

        // Tier weighting skewed hard toward the dangerous tiers (no Fiends at
        // all) — this is a catastrophe, not an ordinary tide night. Compare
        // DemonMath.RollTier's ordinary 55/25/13/7 (Fiend/Stalker/Ravager/
        // Hellsteed) split.
        public static DemonMath.DemonTier HostTier(Random rng)
        {
            if (rng == null) return DemonMath.DemonTier.Stalker;
            int roll = rng.Next(100);
            if (roll < 30) return DemonMath.DemonTier.Stalker;    // 0..29  (30%)
            if (roll < 70) return DemonMath.DemonTier.Ravager;    // 30..69 (40%)
            return DemonMath.DemonTier.Hellsteed;                 // 70..99 (30%)
        }

        // ── The rampage ───────────────────────────────────────────────────────────
        // Each surviving host band, once free (not already mid-battle or
        // mid-assault), rolls daily to march on the nearest town or castle —
        // far more aggressive than the ordinary night tide's
        // DemonMath.SettlementAssaultChancePerPartyPerNight (0.02), because
        // this IS the disaster, not a rare escalation of it.
        public const float RaidChancePerHostPerDay = 0.35f;

        public static bool RollHostRaids(double roll) => roll < RaidChancePerHostPerDay;

        // How long the rampage runs before it burns itself out, if the player
        // has not already cleared every host band first. Three weeks — long
        // enough for several raids to actually land, short enough that a
        // single failed rite doesn't haunt an entire playthrough indefinitely.
        public const int RampageDurationDays = 21;

        public static bool IsRampageOver(int daysSinceStart) => daysSinceStart >= RampageDurationDays;

        // ── Aftermath ─────────────────────────────────────────────────────────────
        // A permanent relation hit with the Tower (its own scholars blame the
        // outsider who lit the rite, not themselves) — applied once, to the
        // Tower's own heroes, mirroring how TowerCampaignBehavior.
        // ApplyTowerJoinMagic already walks a Tower clan's Heroes list.
        public const int AftermathRelationPenalty = -25;

        // One surviving host band is deliberately left behind, shrunk down to
        // a lingering remnant, instead of being fully destroyed when the
        // rampage's clock runs out — "a lasting, map-visible demon threat near
        // Tower lands," per the balance-pass note's aftermath requirement.
        // Far smaller than a live host band, but never auto-cleared: it stays
        // until a player physically kills it.
        public const int RemnantPartySize = 6;
    }
}
