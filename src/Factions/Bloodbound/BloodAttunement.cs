// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodAttunement.cs
//
// The persisted per-hero tracker for the Bloodbound's blood-attunement (a
// mod-author-directed addition — see BloodAttunementMath.cs's header). Owns:
//   • which of Fire/Water/Earth/Wind a hero (player OR lord) has permanently
//     attuned to, via a small bitmask keyed off MagicElement's int value
//     (Fire=0, Wind=1, Earth=2, Water=3 — see ElementMagicMath.cs)
//   • the drinking logic itself (LearnElement): applies the relation fallout
//     and rolls the one random permanent penalty a new attunement carries
//   • the player's currently-loaded element for battle casting (mission-
//     scoped, NOT persisted — reset like any other in-mission selection)
//   • the two flag-driven permanent penalties' STACK COUNTS (daytime morale /
//     daytime speed) — the actual daytime morale drain is applied by
//     BloodboundCampaignBehavior's daily tick (TickDaytimeMoralePenalty
//     below); the daytime speed penalty is applied continuously by
//     BloodAttunementSpeedModel, also in this file.
//
// Persistence: four parallel lists (BLD_ATT_* keys), synced from
// BloodboundCampaignBehavior.SyncData — the same "small persisted roster"
// pattern the ignore/HP-buff trackers in that file already use. Unlike
// SpellcasterLords (which reseeds its NPC population fresh every session,
// because its effects are all mission-scoped), a Bloodbound lord's
// attunement carries PERMANENT side effects (relation hits, stat/behaviour
// penalties) — so it must be seeded EXACTLY ONCE, ever, and then persist.
// See BloodAttunementLordAI.SeedIfNeeded's HasAnyElement guard.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    internal static class BloodAttunement
    {
        // ── Persisted state (parallel lists, synced by BloodboundCampaignBehavior) ──
        internal static List<string> HeroIds             = new List<string>();
        internal static List<int>    ElementMasks         = new List<int>();
        internal static List<int>    DaytimeMoraleStacks  = new List<int>();
        internal static List<int>    DaytimeSpeedStacks   = new List<int>();

        // ── Mission-scoped only — the player's currently-loaded element ────────
        private static MagicElement _playerLoaded = MagicElement.Fire;
        internal static MagicElement PlayerLoaded => _playerLoaded;

        internal static readonly MagicElement[] AttunableElements =
            { MagicElement.Fire, MagicElement.Wind, MagicElement.Earth, MagicElement.Water };

        internal static void ResetForNewGame()
        {
            HeroIds = new List<string>();
            ElementMasks = new List<int>();
            DaytimeMoraleStacks = new List<int>();
            DaytimeSpeedStacks = new List<int>();
            _playerLoaded = MagicElement.Fire;
        }

        internal static void EnsureListsSane()
        {
            if (HeroIds == null) HeroIds = new List<string>();
            if (ElementMasks == null) ElementMasks = new List<int>();
            if (DaytimeMoraleStacks == null) DaytimeMoraleStacks = new List<int>();
            if (DaytimeSpeedStacks == null) DaytimeSpeedStacks = new List<int>();
        }

        // ── Queries ─────────────────────────────────────────────────────────────
        private static int IndexOf(Hero hero) => hero == null ? -1 : HeroIds.IndexOf(hero.StringId);

        internal static bool HasElement(Hero hero, MagicElement el)
        {
            int idx = IndexOf(hero);
            return idx >= 0 && (ElementMasks[idx] & (1 << (int)el)) != 0;
        }

        internal static int KnownCount(Hero hero)
        {
            int idx = IndexOf(hero);
            if (idx < 0) return 0;
            int mask = ElementMasks[idx];
            int c = 0;
            foreach (var el in AttunableElements)
                if ((mask & (1 << (int)el)) != 0) c++;
            return c;
        }

        internal static bool HasAnyElement(Hero hero) => KnownCount(hero) > 0;

        internal static List<MagicElement> KnownElements(Hero hero)
        {
            var list = new List<MagicElement>();
            int idx = IndexOf(hero);
            if (idx < 0) return list;
            int mask = ElementMasks[idx];
            foreach (var el in AttunableElements)
                if ((mask & (1 << (int)el)) != 0) list.Add(el);
            return list;
        }

        // Cost the NEXT (as-yet-unknown) element attunement would take.
        internal static int NextCost(Hero hero) => BloodAttunementMath.AttunementCostBlood(KnownCount(hero));

        // ── Player's loaded-element selection ──────────────────────────────────
        internal static void SetPlayerLoaded(MagicElement el)
        {
            if (HasElement(Hero.MainHero, el)) _playerLoaded = el;
        }

        // Called whenever the battle-input handler starts a new hold session —
        // if the currently-loaded element was never learned (e.g. a fresh
        // battle, or the hero only ever attuned to something other than Fire),
        // fall back to whichever attuned element sorts first (Fire preferred).
        internal static void EnsureValidLoaded(Hero hero)
        {
            if (HasElement(hero, _playerLoaded)) return;
            var known = KnownElements(hero);
            if (known.Count > 0) _playerLoaded = known[0];
        }

        // ── Drinking — the core "learn a new element" operation ────────────────
        // Caller (the menu) is responsible for spending the Demon Blood BEFORE
        // calling this. Returns false (no-op) if `el` is already known. On a
        // genuine new attunement, applies the relation fallout to every OTHER
        // faction's ruling leader and rolls the one random permanent penalty.
        internal static bool LearnElement(Hero hero, MagicElement el, Random rng, out BloodAttunementMath.PenaltyKind penalty)
        {
            penalty = BloodAttunementMath.PenaltyKind.SocialDown;
            if (hero == null || rng == null) return false;
            if (HasElement(hero, el)) return false;

            try
            {
                EnsureListsSane();
                int idx = IndexOf(hero);
                if (idx < 0)
                {
                    HeroIds.Add(hero.StringId);
                    ElementMasks.Add(0);
                    DaytimeMoraleStacks.Add(0);
                    DaytimeSpeedStacks.Add(0);
                    idx = HeroIds.Count - 1;
                }
                ElementMasks[idx] |= (1 << (int)el);

                try { ApplyRelationFallout(hero); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                penalty = BloodAttunementMath.RollPenalty(rng.NextDouble());
                try { ApplyPenalty(hero, idx, penalty); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                return true;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); return false; }
        }

        private static void ApplyRelationFallout(Hero hero)
        {
            string ownKingdomId = null;
            try { ownKingdomId = hero.MapFaction?.StringId; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            List<Kingdom> kingdoms;
            try { kingdoms = Kingdom.All.ToList(); } catch { return; }

            foreach (Kingdom k in kingdoms)
            {
                try
                {
                    if (k == null || k.IsEliminated) continue;
                    if (ownKingdomId != null && k.StringId == ownKingdomId) continue;
                    Hero leader = k.Leader;
                    if (leader == null || leader == hero) continue;

                    int delta = k.StringId == "vlandia"
                        ? BloodAttunementMath.RelationPenaltyTemple
                        : BloodAttunementMath.RelationPenaltyOther;
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, leader, delta, false);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static void ApplyPenalty(Hero hero, int idx, BloodAttunementMath.PenaltyKind penalty)
        {
            switch (penalty)
            {
                case BloodAttunementMath.PenaltyKind.SocialDown:
                    try { hero.HeroDeveloper?.RemoveAttribute(DefaultCharacterAttributes.Social, 1); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    break;
                case BloodAttunementMath.PenaltyKind.IntellectDown:
                    try { hero.HeroDeveloper?.RemoveAttribute(DefaultCharacterAttributes.Intelligence, 1); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    break;
                case BloodAttunementMath.PenaltyKind.DaytimeMorale:
                    DaytimeMoraleStacks[idx] = DaytimeMoraleStacks[idx] + 1;
                    break;
                case BloodAttunementMath.PenaltyKind.DaytimeSpeed:
                    DaytimeSpeedStacks[idx] = DaytimeSpeedStacks[idx] + 1;
                    break;
            }
        }

        internal static int DaytimeSpeedStackCount(Hero hero)
        {
            int idx = IndexOf(hero);
            return idx < 0 ? 0 : DaytimeSpeedStacks[idx];
        }

        // ── Daily tick — the daytime-only morale drain ─────────────────────────
        // Called from BloodboundCampaignBehavior.OnDailyTick. Only bites while
        // the current hour is NOT usable (i.e. full daylight) — a hero who
        // never rolled the morale penalty, or whose party can't be found, is
        // skipped entirely.
        internal static void TickDaytimeMoralePenalty()
        {
            if (HeroIds == null || HeroIds.Count == 0) return;
            try
            {
                float hour = (float)CampaignTime.Now.CurrentHourInDay;
                if (BloodAttunementMath.IsUsableHour(hour)) return;

                for (int i = 0; i < HeroIds.Count; i++)
                {
                    int stacks = DaytimeMoraleStacks[i];
                    if (stacks <= 0) continue;
                    try
                    {
                        Hero hero = Hero.MainHero != null && Hero.MainHero.StringId == HeroIds[i]
                            ? Hero.MainHero
                            : Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == HeroIds[i]);
                        if (hero == null) continue;

                        MobileParty party = hero == Hero.MainHero ? MobileParty.MainParty : hero.PartyBelongedTo;
                        if (party == null) continue;

                        party.RecentEventsMorale -= BloodAttunementMath.DaytimeMoraleDrainPerDay * stacks;
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }

    // ── The daytime-only party-speed penalty ────────────────────────────────────
    // A continuous model override, layered ON TOP of ForestClansSpeedModel
    // rather than registered alongside it — CampaignGameStarter/GameModels
    // resolves exactly ONE PartySpeedCalculatingModel (confirmed by reflecting
    // TaleWorlds.CampaignSystem.GameModels: a single backing field, not a
    // composable list), so a second independent AddModel(...) of the same
    // interface would silently REPLACE the Forest Clans' Green Roads bonus
    // instead of adding to it. Subclassing keeps both effects live from one
    // registration (see MagicSystem.cs, which now registers THIS class in
    // ForestClansSpeedModel's place). A party led by a hero carrying one or
    // more DaytimeSpeed stacks marches slower whenever the current hour is
    // NOT usable (full daylight); at night or in twilight the penalty simply
    // does not apply — no separate tick or flag needed, the model is
    // re-evaluated live.
    internal sealed class BloodAttunementSpeedModel : ForestClansSpeedModel
    {
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            var result = base.CalculateFinalSpeed(mobileParty, finalSpeed);
            try
            {
                Hero leader = mobileParty?.LeaderHero;
                if (leader == null) return result;

                int stacks = BloodAttunement.DaytimeSpeedStackCount(leader);
                if (stacks <= 0) return result;

                float hour = (float)CampaignTime.Now.CurrentHourInDay;
                if (BloodAttunementMath.IsUsableHour(hour)) return result;

                result.AddFactor(BloodAttunementMath.DaytimeSpeedPenaltyFactor * stacks,
                    new TextObject("{=ae_blood_burden}The Blood's Burden"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return result;
        }
    }
}
