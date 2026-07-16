// =============================================================================
// THE DARKEST NIGHT — FactionQuests/WolfBrothers/WolfHuntMath.cs
//
// Pure numeric core of "The Great Hunt" — Faction A's (Wolf Brothers) Phase 12
// questline (Requirement 21). No TaleWorlds types (fully covered by
// PureLogicTests). Runtime behaviour lives in WolfHuntQuestCampaignBehavior.*,
// WolfHuntBeastParty.cs, WolfHuntQuestLog.cs.
//
// ── Design ────────────────────────────────────────────────────────────────
// Three named demon beasts, hunted in escalating order, each leading a small
// pack drawn entirely from its own (harder) demon tier. A beast's pack is
// deliberately SMALLER than an ordinary night-tide party (DemonMath.
// PartyBodyCount: 10-22 mixed-tier bodies, which any day-50+ player has
// already survived many times over just by existing under the Night Tide) —
// the challenge here is concentrated in the pack being uniformly a harder
// tier, at boosted health. The health boost is the same layering technique
// ApocalypseMath.DemonLordHealthMultiplier uses on top of DemonMath.Health for
// the Demon Lord (DemonBattleBehavior.PendingBossMultiplier is the shared,
// generalised hook — see that file), just scaled down to a mid-campaign
// trial rather than the campaign's final boss.
//
// ── Balance reasoning (Phase 12's explicit balance-pass requirement) ────────
// The day-50 trigger gate (FactionQuestMath.TriggerStartDay) means a player
// reaching this quest has already weathered roughly 50 nights of the Night
// Tide. Per Phase 2's barter economy, that player is still gold/food-poor —
// this quest deliberately asks for NO donation/resource stockpile (unlike the
// Great Awakening's 10,000-prisoner counter or the Northmen Stones' six
// material tracks): Wolf Brothers already have their own resource sink (the
// Larder — WolfBrothersMath.MeatFromTroop/MeatFromPrisoner), and stacking a
// second grind on the same currency would just be busywork, not difficulty.
// The cost here is risk and time: three hunts, each a full tier harder than
// the last (Stalker -> Ravager -> Hellsteed) at 2.0x-3.0x that tier's base
// health, so "trivially fast or cheap" is not achievable without a war party
// that could already beat an ordinary Ravager/Hellsteed-heavy tide night —
// exactly the wall the balance pass asks for, without inventing a second,
// disconnected difficulty curve.
// =============================================================================

namespace TheDarkestNight
{
    public static class WolfHuntMath
    {
        public const int StageCount = 3;

        public static bool IsValidStage(int stageIndex) => stageIndex >= 0 && stageIndex < StageCount;

        // Which demon tier's pack the named beast at this stage leads —
        // escalating a full tier each time (skips Fiend, the weakest tier, as
        // beneath "the largest things still drawing breath"; never Lord, which
        // Phase 11 reserves for the campaign's own endgame).
        public static DemonMath.DemonTier BeastTier(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0: return DemonMath.DemonTier.Stalker;
                case 1: return DemonMath.DemonTier.Ravager;
                default: return DemonMath.DemonTier.Hellsteed;
            }
        }

        // How many bodies of that tier make up the pack — small and
        // homogeneous, unlike an ordinary night-tide party's 10-22 mixed-tier
        // bodies. Still grows stage over stage so the last hunt is a real fight,
        // not just a tougher duel.
        public static int PackSize(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0: return 5;
                case 1: return 8;
                default: return 12;
            }
        }

        // Layered on DemonMath.Health(tier, Default) exactly like
        // ApocalypseMath.DemonLordHealthMultiplier layers onto the Demon Lord's
        // base stats via DemonBattleBehavior.PendingBossMultiplier — a real
        // "this pack is led by something named" bump, kept well below the
        // Lord's x6.0 because this is a mid-campaign trial, not the endgame.
        public static float BeastHealthMultiplier(int stageIndex)
        {
            switch (stageIndex)
            {
                case 0: return 2.0f;
                case 1: return 2.5f;
                default: return 3.0f;
            }
        }

        // ── The final choice — two distinct, permanent mechanical outcomes ───────
        // Transformation ("feed the pack the beast's flesh"): a genuine
        // permanent ceiling raise — the same "raise the HP ceiling above
        // MaxHitPoints" technique BloodboundMath.HpBuffAmount uses for its
        // week-long Hardened Flesh, just never ticked back down — plus a
        // further hardening of the exact trait pair the join ritual already
        // shifts (WolfBrothersMath.JoinMercyShift/JoinValorShift): becoming,
        // completely, the thing you were sent to hunt.
        public const float TransformationHpBonus     = 30f;
        public const int   TransformationMercyShift  = -1;
        public const int   TransformationValorShift  = 1;
        // The pack's own renown for a Kinsman who goes all the way.
        public const float TransformationRenownGain  = 20f;

        // Burning: no HP bonus, but the trait shift runs the other way (a
        // sliver of mercy held onto) — and the renown gain is LARGER than
        // Transformation's, because it is word of the restraint, not the kill,
        // that travels: proving a Kinsman can stop is rarer, and reads further,
        // than proving one more Kinsman could not.
        public const int   BurningMercyShift = 1;
        public const float BurningRenownGain = 35f;
    }
}
