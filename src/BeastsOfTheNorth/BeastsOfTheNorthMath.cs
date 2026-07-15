// =============================================================================
// THE DARKEST NIGHT — BeastsOfTheNorth/BeastsOfTheNorthMath.cs
//
// Pure numeric core of "Seek the Old Blood" — Wolf Brothers (Sturgia) towns'
// costly recruitment of two special troops: the Jotunn-Blooded (a giant) and
// the Ulfhednar (a wolf-rider). Runtime (menus, item/roster handling) lives
// in BeastsOfTheNorthCampaignBehavior(.Menus).cs.
//
// ── On "the vanilla meat trade good" ─────────────────────────────────────
// The brief asked for these recruits to be paid in the vanilla "meat" trade
// good. Verified against the shipped item XML — the full <!-- #region Trade
// --> and <!-- #region Misc --> regions of
// SandBoxCore/ModuleData/items/horses_and_others.xml (the only file that
// defines trade-good Items in this build) — and by an id="meat" search
// across every XML file in every shipped module on this box: no item with
// that id (or any meat-flavoured id: "raw_meat", "dried_meat", etc.) exists
// in this Bannerlord build. "grain" is equally absent, for what that's
// worth — this game version's trade-good roster is smaller than the classic
// Warband/early-Bannerlord list memory suggests.
//
// (WolfBrothersCampaignBehavior.TickLordCannibalism already reaches for
// ItemObject "meat" and silently no-ops when MBObjectManager resolves it to
// null — a pre-existing, out-of-scope gap this task does not touch or fix.)
//
// Rather than repeat that silent no-op, Beasts of the North is priced in
// "fish" (id="fish", IsFood="true" — verified real) instead. Tyal and Sibir
// are fjord seats; dried and salted fish stores are exactly what a northern
// muster yard would actually be counting out. A modest gold component rides
// alongside it (see GiantGoldCost/WolfRiderGoldCost) because fish alone is
// cheap and easy to stockpile by trade (12 denars/unit in vanilla) — under
// this mod's Path B scarcity economy (gold ~10x scarcer everywhere, see
// Economy/EconomyMath.cs), a real gold toll is what keeps "costly
// recruitment" costly, not the fish count on its own.
// =============================================================================

using System;

namespace AshAndEmber
{
    public static class BeastsOfTheNorthMath
    {
        // The real, verified item this system spends (see header note above).
        public const string TributeItemId = "fish";

        // ── The Jotunn-Blooded (giant) ───────────────────────────────────────
        public const int GiantFishCost = 50;  // asked range: 40-60
        public const int GiantGoldCost = 400;
        public const int GiantMonthlyCap = 2;

        // Real battle-time height, applied through the engine's own skeleton-
        // scale hook (Agent.SetInitialAgentScale via DemonFactory.SetAgentScale
        // — the hook the Jotunn-Blooded's original troops.xml note believed
        // did not exist; verified real against the shipped DLLs when the demon
        // tiers gained it). The tallest thing on any ordinary field — above
        // even the Ravager (1.28); the Demon Lord deliberately isn't in this
        // race at all (he is man-shaped and uncanny, not big — see
        // DemonMath.VisualScale).
        public const float GiantAgentScale = 1.32f;

        // ── The Ulfhednar (wolf-rider) ───────────────────────────────────────
        public const int WolfRiderFishCost = 32; // asked range: 25-40
        public const int WolfRiderGoldCost = 220;
        public const int WolfRiderMonthlyCap = 4;

        // Roughly 30 in-game days per "month" tick — this mod's calendar has
        // no first-class Month concept (only Season, four per year), so this
        // mirrors ForeignMusterMath's week-bucketing at a coarser grain.
        public const double DaysPerMonth = 30.0;

        public static bool HasCapRemaining(int purchasedThisMonth, int cap) => purchasedThisMonth < cap;

        public static bool CanAffordFish(int haveFish, int fishCost) => haveFish >= fishCost;

        public static bool CanAffordGold(int haveGold, int goldCost) => haveGold >= goldCost;
    }
}
