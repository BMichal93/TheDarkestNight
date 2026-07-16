// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Empire/EmpireQuestMath.cs
//
// Pure numeric core of "The Reunification" — Faction F's (the Empire, Northern
// Empire) Phase 12 questline (Requirement 21). No TaleWorlds types (fully
// covered by PureLogicTests). Runtime behaviour lives in
// EmpireQuestCampaignBehavior.*/EmpireQuestLog.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// Unlike every other faction's answer to the Long Night, the Empire's is not
// a bargain, a rite, or a surrender — it is the oldest imperial article of
// faith: Calradia broke because it was divided, so put it back together and
// the dark loses its footing. The player helps the Empire's conquest along.
// Once two-thirds of the map's cities answer to one crown again, the ruling
// clan's head is formally crowned Emperor of a reunified Calradia — and,
// unlike the Chosen's hollow promise or the Temple's grinding attrition, the
// Empire's arc is allowed to be a genuine, felt triumph: reunification
// actually works, for once. The crowned Empire turns that momentum outward
// and declares war on the demons themselves, its lords pressing the attack
// rather than merely holding the line, until a large — but, unlike the
// Temple's near-impossible 50,000, realistically reachable within a single
// determined campaign — tally of the dead is reached.
//
// ── Conquest threshold — why "cities," and why 35 ───────────────────────────
// The brief is explicit: "conquer ⅔ of all CITIES," not "⅔ of all fiefs."
// Bannerlord's own vocabulary keeps "town" (a city) and "castle" distinct,
// and this questline honours that distinction rather than folding castles
// into the count the way ChosenQuestMath.ConquestFiefThreshold (fiefs, i.e.
// towns AND castles) does. Verified against the shipped SandBox/ModuleData/
// settlements.xml: Calradia has exactly 53 towns map-wide (unchanged by
// Phase 8's city-state conversion or Phase 9's ~80% castle-to-Ruins pass —
// both of those phases only ever touch castles or already-independent towns;
// no town is ever removed from the "town" pool, so 53 is stable for the
// whole campaign). Two-thirds of 53 is 35.33, floored to 35 — this is
// EXACTLY the number ChosenQuestMath.cs's own header already documents as
// "the deleted Empire questline['s]... roughly 35 towns, a full-map-
// dominance threshold appropriate for a kingdom that starts with a normal,
// sprawling holding" — i.e. this questline is that superseded design,
// restored to Faction F where it always belonged, using the literal "cities"
// reading the brief asks for.
//
// ── Is 35 actually reachable? ────────────────────────────────────────────────
// Yes — deliberately more so than the raw "35 of 53" ratio (66%) sounds.
// Town settlements are NEVER converted to Ruins (Phase 9's RuinsCastleSystem
// only ever touches castles — see RuinsCastleSystem.cs's own IsExempt/roll
// gate), so every one of the 53 towns remains a live, siege-conquerable
// settlement for the whole campaign, including every Phase 8 city-state's
// own town. City-states ARE normal ownable settlements (CityStateSystem.
// ReassertCityStateMembership only re-homes the FOUNDING CLAN if it drifts
// out of its own kingdom — it never protects the SETTLEMENT from siege or
// change of ownership), so a determined Empire war machine can, in
// principle, take every single one of them exactly like any rival kingdom's
// town. This is deliberately a MUCH higher bar than ChosenQuestMath's own
// ConquestFiefThreshold (24, roughly a fifth of the 120-fief map) — the
// brief explicitly wants two-thirds of ALL cities, not a modest chunk — but
// it is still an honestly reachable number across a long, sustained
// campaign, not a number only the base game's best AI kingdoms ever touch:
// the Empire starts with 3 of the 53 (EmpireMath.StartingTownIds), so 35 is
// "conquer 32 more towns," a fraction over half the entire town map, which
// is exactly the scale of "reunify Calradia" the brief calls for.
// =============================================================================

using System;

namespace TheDarkestNight
{
    public static class EmpireQuestMath
    {
        // ── Conquest tracking (towns/cities only — castles are NOT counted) ─────
        public const int ConquestTownThreshold = 35;

        public static bool HasReachedThreshold(int currentTowns) => currentTowns >= ConquestTownThreshold;

        // Clamped 0..threshold, for the discrete journal objective bar.
        public static int ClampedTownProgress(int currentTowns)
        {
            if (currentTowns < 0) return 0;
            if (currentTowns > ConquestTownThreshold) return ConquestTownThreshold;
            return currentTowns;
        }

        // ── The crowning — a small, tunable, mechanical flourish ────────────────
        // "A real, felt event" per the brief. The ruler title itself ("Emperor")
        // already exists from Phase 7F (EmpireCulture.RulerTitle) — no mechanical
        // title change is needed, so "crowning" is the narrative/ceremonial
        // moment. The flourish: a real renown jump for the ruling clan (on the
        // same scale as this codebase's other "this changes the story" renown
        // grants — see RivalShadowSystem.cs's own 200f award for a comparable
        // life-event), plus a one-time coronation-feast supply grant to every
        // Empire lord's party (reusing EmpireCampaignBehavior.GrantGrain's
        // existing mechanism rather than inventing a morale/stat system this
        // codebase has nowhere else — see EmpireQuestCampaignBehavior.Crowning.cs).
        public const float CrownRenownBonus = 200f;
        public const int   CoronationGrainAmount = 60; // 4x EmpireMath.GrainClaimAmount's daily trickle, once

        // ── Victory — a second, smaller renown grant on top of the crowning's own ──
        // This is the one deliberately POSITIVE Phase 12 ending (per the brief) —
        // the mechanical reward doubles as narrative punctuation: the reunified
        // Empire's name is worth more, in the most literal sense this codebase
        // has, for having actually won something.
        public const float VictoryRenownBonus = 150f;

        // ── War on the demons ────────────────────────────────────────────────────
        // "Must kill a very large number of them" — deliberately smaller than the
        // Temple's 50,000 (TempleQuestMath.KillTarget), which is framed as a
        // bittersweet, near-impossible milestone in a war of attrition with no
        // salvation written into it. The Empire's arc is the opposite tone: a
        // triumphant, WINNABLE war a reunified Calradia can actually finish
        // within one determined campaign's pacing. 15,000 is under a third of
        // the Temple's target — still a real, sustained campaign-wide tally (the
        // Tide spawns DemonMath.MinNightSpawnParties..MaxNightSpawnParties
        // parties of DemonMath.MinPartyBodies..MaxPartyBodies bodies EVERY
        // night, capped at DemonMath.MaxLivingDemonParties parties alive at
        // once — the arithmetic is real, not fantasy), but framed and paced as
        // a finish line worth reaching, not a grind that outlasts hope.
        public const int KillTarget = 15_000;

        public static bool HasReachedKillTarget(int demonsKilled) => demonsKilled >= KillTarget;

        // Clamped 0..target, for the discrete journal objective bar.
        public static int ClampedKillProgress(int demonsKilled)
        {
            if (demonsKilled < 0) return 0;
            if (demonsKilled > KillTarget) return KillTarget;
            return demonsKilled;
        }

        // ── Aggression nudge — retargets LegionCampaignBehavior's raid-nudge
        // technique at the nearest demon party instead of a rival kingdom's
        // settlements. A higher roll than Legion's own LegionMath.RaidNudgeChance
        // (0.35) — the brief specifically calls for INCREASED aggression once
        // the Empire has declared war on the dark, not the same baseline
        // eagerness every other kingdom's lords already show toward each other.
        public const double AggressionNudgeChance = 0.45;

        public static bool ShouldNudgeToEngageDemons(double roll01) => roll01 < AggressionNudgeChance;

        // Mirrors DemonSpawnCampaignBehavior.EngageSearchRadius (40f) — the
        // range within which a demon-hunting party's own move order simply
        // isn't worth overwriting toward a target it cannot reasonably reach
        // this tick anyway. Kept identical rather than invented afresh so the
        // Empire's own hunting parties behave at the same tempo the demons'
        // own prey-seeking AI already does.
        public const float AggressionSearchRadius = 40f;
    }
}
