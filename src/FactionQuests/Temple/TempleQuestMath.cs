// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestMath.cs
//
// Pure numeric core of "The Unbroken Vow" — Faction E's (the Temple, Vlandia)
// Phase 12 questline (Requirement 21). No TaleWorlds types (fully covered by
// PureLogicTests). Runtime behaviour lives in TempleQuestCampaignBehavior.*/
// TempleQuestLog.cs; the artifact item table lives in TempleQuestArtifacts.cs.
//
// ── Premise ───────────────────────────────────────────────────────────────
// The Temple believes five relics lost across the ruined world — the Five
// Vigils — can, reunited, bind every Templar sword and banner into a single
// deathless host. There IS no clever ending here: once bound, the Order
// marches until the dark is gone or they are. "Kill 50,000 demons" is the
// mechanical stand-in for "until all are dead," not a grindable victory
// condition — see the balance-pass note below for why 50,000 is deliberately
// framed as a bittersweet near-impossibility rather than a straightforward
// finish line.
//
// ── Guaranteed artifact placement — the balance-pass requirement ───────────
// Phase 12's explicit balance-pass warning: "two quests must not deadlock
// each other (e.g. Temple artifacts and ruin loot RNG — guarantee artifact
// placement)." Relying on RelicMath.RollRuinLoot (a 15% per-chamber roll,
// itself gated behind RuinsMath.RollChamberLoot's ~14% Relic slice — well
// under a 3% chance per chamber searched) for FIVE specific outcomes could
// in principle never happen in a single campaign. So this questline does not
// touch that roll at all: SelectArtifactRuins below deterministically assigns
// each of the five artifacts to a real ruin castle (the same castles Phase 9
// already converts — RuinsCastleSystem), and TempleQuestCampaignBehavior.
// Artifacts.cs grants the artifact unconditionally the FIRST time the player
// fully clears that specific ruin's chamber chain — a guaranteed, RNG-free
// bonus pickup layered on top of (never replacing) whatever the normal
// chamber loot rolls turned up along the way.
//
// Selection is a stable sort of the campaign's own ruin-id set by a salted
// hash of each id (RuinsMath.StableHash, already proven deterministic-per-
// save-per-id) — the same "no persistence needed, same input always yields
// the same answer" technique RuinsMath itself uses to decide which castles
// become ruins in the first place. This questline DOES still persist the
// five chosen ids once selected (TempleQuestCampaignBehavior's own state),
// because RuinsCastleSystem's ruin set can in principle drift slightly
// between sessions (a player-conquered castle becomes exempt — see
// RuinsCastleSystem.IsExempt) and a mid-campaign artifact must never move
// out from under a player who is actively hunting it.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace TheDarkestNight
{
    public static class TempleQuestMath
    {
        // ── The Five Vigils ──────────────────────────────────────────────────
        public const int ArtifactCount = 5;

        // Deterministically picks which ruins carry the five artifacts, from
        // whatever set of ruin settlement ids the campaign actually has this
        // session (RuinsCastleSystem.AllRuinIds()). Ordering is a stable sort
        // by a salted hash of each id, so the SAME ruin set always yields the
        // SAME five picks — no Random, no session-to-session drift for a
        // fixed input. If fewer than ArtifactCount ruins exist at all (Phase
        // 9's ~80% conversion rate makes this extremely unlikely, but not
        // impossible on a tiny custom map), this simply returns however many
        // ARE available — TempleQuestCampaignBehavior documents and handles
        // the "fewer than 5" case rather than crashing or hanging forever.
        public static List<string> SelectArtifactRuins(IEnumerable<string> allRuinIds, int desiredCount = ArtifactCount)
        {
            var ids = (allRuinIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .OrderBy(id => RuinsMath.StableHash("temple_artifact_" + id))
                .ToList();

            int take = Math.Min(Math.Max(0, desiredCount), ids.Count);
            return ids.Take(take).ToList();
        }

        // ── Delivery ─────────────────────────────────────────────────────────
        public static bool HasAllArtifacts(int artifactsFound) => artifactsFound >= ArtifactCount;

        // ── The kill target — "a huge but technically countable number" ────────
        // Demons respawn nightly, campaign-wide, in real numbers
        // (DemonMath.MinNightSpawnParties..MaxNightSpawnParties parties every
        // night, DemonMath.MinPartyBodies..MaxPartyBodies bodies each, capped
        // at DemonMath.MaxLivingDemonParties parties alive at once) — so
        // 50,000 kills is not fantasy-arithmetic, it is a real number of
        // bodies the Tide can in principle produce over a very long, very
        // sustained campaign. But it is chosen to sit far past what any
        // single playthrough's own pacing will comfortably reach: a
        // deliberately bittersweet, "almost never happens" milestone rather
        // than a grindable finish line, matching the brief's own "there is
        // no salvation — they fight until all are dead" framing. Reaching it
        // is not a reward for good play; it is what's left after everything
        // else has already been lost.
        public const int KillTarget = 50_000;

        public static bool HasReachedKillTarget(int demonsKilled) => demonsKilled >= KillTarget;

        // Clamped 0..target, for the discrete journal objective bar.
        public static int ClampedKillProgress(int demonsKilled)
        {
            if (demonsKilled < 0) return 0;
            if (demonsKilled > KillTarget) return KillTarget;
            return demonsKilled;
        }

        // ── The permanent army — cohesion upkeep ────────────────────────────────
        // Mirrors SoldierServiceMath's "never let a raised host bleed out"
        // reasoning (SustainArmy tops cohesion to 100 whenever it drifts below
        // this floor) — but here there is no hold window and no release: the
        // Vow is permanent, so the topping-up never stops.
        public const float ArmyCohesionFloor = 90f;
        public const float ArmyCohesionTopUp = 100f;
    }
}
