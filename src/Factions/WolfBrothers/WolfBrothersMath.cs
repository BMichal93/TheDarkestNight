// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersMath.cs
//
// Pure numeric core of Faction A — the Wolf Brothers (formerly Sturgia).
// No TaleWorlds types (fully covered by PureLogicTests). Runtime behaviour —
// culture/kingdom renaming, dialogue, town-scoping, the join ritual, and the
// "render into meat" city menu — lives in WolfBrothersCulture.cs,
// WolfBrothersDialogue.cs, WolfBrothersSettlements.cs and
// WolfBrothersCampaignBehavior(.Menus).cs.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class WolfBrothersMath
    {
        // ── Starting holdings (town-scoping) ────────────────────────────────────
        // The Wolf Brothers keep exactly two seats: Tyal and Sibir. Every other
        // town/castle Sturgia holds falls out of the kingdom's scope — its clan is
        // simply ejected (the settlement stays with that now-independent clan,
        // exactly the "ownerless in this faction's scope" state the brief asks
        // for; Phase 8 later folds these into proper city-states).
        public static readonly string[] StartingTownIds = { "town_S5", "town_S6" }; // Tyal, Sibir

        public static bool IsStartingTownId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            for (int i = 0; i < StartingTownIds.Length; i++)
                if (string.Equals(StartingTownIds[i], stringId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // ── Joining the pack — the price of the first meal ──────────────────────
        // Eating with the Wolf Brothers is not a metaphor. A new Kinsman loses
        // standing among the "civilised" kingdoms and is quietly hardened by it.
        public const float JoinReputationLoss = 15f;  // Clan.Renown lost, once, on joining
        public const int   JoinMercyShift     = -1;   // DefaultTraits.Mercy
        public const int   JoinValorShift     = 1;    // DefaultTraits.Valor

        private const int TraitMin = -2;
        private const int TraitMax = 2;

        public static int ClampTraitLevel(int current, int shift)
        {
            int next = current + shift;
            if (next < TraitMin) return TraitMin;
            if (next > TraitMax) return TraitMax;
            return next;
        }

        public static float ApplyReputationLoss(float currentRenown)
        {
            float next = currentRenown - JoinReputationLoss;
            return next < 0f ? 0f : next;
        }

        // ── Rendering flesh into meat ────────────────────────────────────────────
        // A hardened warrior feeds the pack longer than a green recruit — yield
        // scales with troop tier. A prisoner yields one extra ration per tier: a
        // captive is fed and fattened while your own soldiers are lean from war.
        private const int BaseMeatYield     = 2;
        private const int MeatYieldPerTier  = 1;
        private const int PrisonerMeatBonus = 1;
        private const int MaxTierForYield   = 6; // CharacterObject.Tier tops out around 6

        public static int MeatFromTroop(int tier)
        {
            int t = tier < 0 ? 0 : (tier > MaxTierForYield ? MaxTierForYield : tier);
            return BaseMeatYield + t * MeatYieldPerTier;
        }

        public static int MeatFromPrisoner(int tier)
            => MeatFromTroop(tier) + PrisonerMeatBonus;

        // ── The pack's lords eat too ─────────────────────────────────────────────
        // A Wolf Brothers lord does not let a cage go to waste for long — every
        // few days, any lord party holding prisoners renders one into meat for its
        // own stores. Deliberately slow and small: a survival reflex, not a
        // standing policy, and never enough to make prisoners pointless to take.
        public const int LordCannibalizeIntervalDays  = 5; // how often a lord party checks its cages
        public const int LordMaxPrisonersEatenPerTick = 1; // prisoners spent per qualifying tick
    }
}
