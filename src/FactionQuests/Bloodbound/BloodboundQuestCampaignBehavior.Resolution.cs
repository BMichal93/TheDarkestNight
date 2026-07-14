// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Bloodbound/BloodboundQuestCampaignBehavior.Resolution.cs
//
// The moment the draught is complete. Three things happen, in order:
//
//   1. THE PLAYER'S CHOICE — a two-answer InquiryData, matching every other
//      resolution in this codebase (ForestWidowsQuestCampaignBehavior.
//      Resolution.cs's shape):
//        • Drink (Affirmative) — the player character DIES, a real and
//          permanent consequence. Verified fresh against the DLL
//          (Actions/KillCharacterAction — ApplyByMurder is the exact call
//          NorthmenStonesCampaignBehavior.Ending.cs already uses to kill
//          Hero.MainHero for a scripted story reason) rather than guessed:
//          killing Hero.MainHero through KillCharacterAction fires the
//          engine's own OnBeforeMainCharacterDiedEvent / OnHeirSelection-
//          RequestedEvent / OnGameOverEvent chain (TaleWorlds.CampaignSystem.
//          CampaignEvents) — vanilla Bannerlord's native player-death flow.
//          Nothing further needs to be scripted here: if the player has a
//          living, eligible heir the game offers to continue as them: if not,
//          it ends the campaign on its own. This mod does not need to (and
//          must not try to) reimplement that flow.
//        • Run (Negative) — if the player is still a Bloodbound vassal, their
//          clan leaves the kingdom first (ChangeKingdomAction.
//          ApplyByLeaveKingdom, matching ForestWidowsQuestCampaignBehavior.
//          Resolution.cs's ResolveCastOut / ChosenQuestCampaignBehavior.
//          Split.cs's "never drag the player into a fate they didn't
//          choose"). The player survives; the campaign continues normally.
//
//   2. THE MASS DEATH — ApplyMassClanDeath, run once regardless of which
//      button the player pressed: every adult member (Hero.IsChild == false)
//      of every OTHER Bloodbound clan (the player's own clan is excluded —
//      its fate is the choice above, not this sweep) is killed via
//      KillCharacterAction.ApplyByMurder, leaving each clan reduced to
//      whatever child heirs it had. Bannerlord's own clan-leadership
//      succession (built into the engine, the same machinery that already
//      promotes a new leader whenever any clan's leader dies) runs as each
//      kill lands; if only children are left when a clan's leader dies,
//      vanilla Bannerlord assigns leadership to one of them rather than
//      failing — a headless clan is not a state the engine needs help with.
//      A clan already down to zero adults before the sweep begins (rare, but
//      possible after enough campaign attrition) is simply skipped — there is
//      no one left to drink.
//
//   3. THE AFTERMATH — a few days later (BloodboundQuestMath.
//      DemonAttackDelayDays), AftermathDailyTick spawns aggressive demon
//      ambush bands at every settlement the (now leaderless-or-child-led)
//      Bloodbound kingdom still holds, reusing Phase 9's proven spawn-near-
//      position hook (DemonSpawnCampaignBehavior.SpawnAmbushNear — the exact
//      technique RuinsExplorationSystem.WaitMenu already uses for "a demon
//      party appears right here, right now").
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AshAndEmber
{
    public sealed partial class BloodboundQuestCampaignBehavior
    {
        // Persisted so a mid-resolution save can't re-roll or re-apply either step.
        private static bool _massDeathApplied  = false;
        private static bool _demonAttackApplied = false;
        private static float _massDeathDay     = -1f;

        private static void SyncResolutionData(IDataStore store)
        {
            try { store.SyncData("BLDQ_MassDeathApplied",   ref _massDeathApplied); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("BLDQ_DemonAttackApplied", ref _demonAttackApplied); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { store.SyncData("BLDQ_MassDeathDay",       ref _massDeathDay); }       catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ResetResolutionState()
        {
            _massDeathApplied = false;
            _demonAttackApplied = false;
            _massDeathDay = -1f;
        }

        // ── The choice ────────────────────────────────────────────────────────────
        private static void ShowResolutionChoice()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "The Surpassing Rite",

                "The last of it is poured, and the shrine's basin does not so much fill as go still, the way a " +
                "held breath goes still just before it's let out.\n\n" +
                "Word runs to every Bloodhunter at once — you can feel it more than hear it, the whole hall " +
                "turning toward Akkalat as one animal does. The Huntmaster finds you before the others arrive, " +
                "and for once does not sound like a rider who has already decided to survive the hunt.\n\n" +
                "\"It's ready. Every cup will be filled and drained together — that's the whole of the working, " +
                "no half-measures in it. Drink with us, if you mean to become what we're about to become. Or ride " +
                "out now, and let the rest of us find out alone what's on the other side of it.\"",

                true, true,
                "Drink. I'll see what's on the other side too.",
                "Ride out. This isn't a hunt I'll follow you on.",
                () => { try { ResolveParticipate(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                () => { try { ResolveRan(); }         catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } }
            ), true, true);
        }

        // ── Ending A — drink with them ───────────────────────────────────────────
        private static void ResolveParticipate()
        {
            _phase = PhaseEndedParticipated;
            try { BloodboundQuestLog.Current?.LogEndingParticipated(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ApplyMassClanDeath();

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Surpassing Rite",

                    "There is nothing gentle in it. The blood goes down like fire and does not stop being fire once " +
                    "it's down — you feel whatever held the shape of you straining against something that was never " +
                    "meant to fit inside a single body. For one unbearable moment you understand exactly what the " +
                    "Huntmaster promised, and exactly what it costs.\n\n" +
                    "It costs everything. The last thing you're certain of is that you are still, somehow, yourself.",

                    true, false, "So it is.", "",
                    () => { try { KillCharacterAction.ApplyByMurder(Hero.MainHero, null, true); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    () => { }
                ), true, true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Ending B — ride out ──────────────────────────────────────────────────
        private static void ResolveRan()
        {
            try
            {
                Kingdom bloodbound = GetBloodboundKingdom();
                if (bloodbound != null && Clan.PlayerClan != null && Clan.PlayerClan.Kingdom == bloodbound)
                    try { ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            _phase = PhaseEndedRan;
            try { BloodboundQuestLog.Current?.LogEndingRan(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ApplyMassClanDeath();

            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(
                    "You are on the road before the shrine's basin runs dry. Whatever happens in Akkalat now " +
                    "happens without you — and you do not look back to see the start of it."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The mass death — every OTHER clan drinks, and dies ───────────────────
        private static void ApplyMassClanDeath()
        {
            if (_massDeathApplied) return;
            _massDeathApplied = true;
            _massDeathDay = CurrentCampaignDayFloat();

            Kingdom bloodbound = GetBloodboundKingdom();
            if (bloodbound == null) return;

            int killed = 0;
            try
            {
                foreach (Clan clan in bloodbound.Clans.ToList())
                {
                    if (clan == null || clan.IsEliminated || clan == Clan.PlayerClan) continue;

                    // Snapshot first — killing a clan's leader mid-loop can trigger
                    // the engine's own succession pass, which must never cause this
                    // sweep to skip or double-process a hero.
                    var adults = clan.Heroes?.Where(h => h != null && h.IsAlive && !h.IsChild).ToList();
                    if (adults == null) continue;

                    foreach (Hero hero in adults)
                    {
                        try
                        {
                            if (!hero.IsAlive || hero.IsChild) continue; // re-check: an earlier kill in this same clan may have already ended them (spouse, etc.)
                            KillCharacterAction.ApplyByMurder(hero, null, false);
                            killed++;
                        }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(
                    $"Word reaches you of what happened at Akkalat and Chaikand: every Bloodhunter who did not " +
                    $"ride away drank together, and the blood did not remake them so much as unmake them. " +
                    $"{killed} riders are gone in a single night, gruesomely, all at once — every hall left to " +
                    "children too young to lead it."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The aftermath — the dark comes for the emptied seats ────────────────
        private void AftermathDailyTick()
        {
            if (!_massDeathApplied || _demonAttackApplied) return;

            float daysSince = CurrentCampaignDayFloat() - _massDeathDay;
            if (!BloodboundQuestMath.IsDemonAttackDue((int)daysSince)) return;

            _demonAttackApplied = true;
            try { SpawnAftermathAttack(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void SpawnAftermathAttack()
        {
            Kingdom bloodbound = GetBloodboundKingdom();
            List<Settlement> targets = bloodbound?.Settlements?
                .Where(s => s != null && (s.IsTown || s.IsCastle)).ToList();
            if (targets == null || targets.Count == 0) return;

            int spawned = 0;
            foreach (Settlement s in targets)
            {
                for (int i = 0; i < BloodboundQuestMath.AttackPartiesPerSettlement; i++)
                {
                    try
                    {
                        if (DemonSpawnCampaignBehavior.SpawnAmbushNear(s.GetPosition2D, "khuzait") != null) spawned++;
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }

            if (spawned > 0)
            {
                try
                {
                    MBInformationManager.AddQuickInformation(new TextObject(
                        "The dark does not wait long to test what's left of the Bloodbound. Demon bands rise on " +
                        "Akkalat and Chaikand both, and there is no one left in either hall old enough to answer " +
                        "the muster."));
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static float CurrentCampaignDayFloat()
        {
            try { return (float)CampaignTime.Now.ToDays; } catch { return 0f; }
        }
    }
}
