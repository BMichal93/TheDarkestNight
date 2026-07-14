// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestCampaignBehavior.Resolution.cs
//
// The moment the count is paid. Two things happen:
//
//   1. THE PLAYER'S CHOICE — a two-answer InquiryData, matching every other
//      resolution in this codebase (ApocalypseCampaignBehavior.Resolution.cs's
//      "flag + inquiry, no literal process termination" convention):
//        • Cast out (Affirmative) — if the player is still a Forest Widows
//          vassal, their clan leaves the kingdom first
//          (ChangeKingdomAction.ApplyByLeaveKingdom, matching
//          ChosenQuestCampaignBehavior.Split.cs's "never drag the player into
//          a fate they didn't choose"), then the dark pact is sealed around
//          them. The campaign continues normally.
//        • Stay (Negative) — the player's own clan is left exactly where it
//          is when the dark pact is sealed. If the player is (or remains) a
//          Forest Widows vassal, the pact's permanent, everyone-hostile war
//          footing now includes them too — a real, mechanical "the game is
//          over" state (every other kingdom is permanently at war with the
//          player's own realm, exactly like the Widows), on top of the
//          narrative epilogue. No process is force-quit; this is a flagged
//          ending the player can still look at, matching
//          DemonLordSystem/ApocalypseCampaignBehavior's own victory/defeat
//          resolutions.
//
//   2. THE DARK PACT ITSELF — ApplyDarkPact, run once regardless of which
//      button the player pressed:
//        • Every surviving Forest Widows clan permanently at war with every
//          other kingdom (and the player's clan, if independent) — see
//          AshenDiplomacyModel's mirrored IsForestWidowsDarkPact for the
//          "never offered peace again" half of this.
//        • Every settlement the Forest Widows still hold gets its garrison
//          replaced outright with the Kindled's own troops
//          (Demons/DemonCatalog.cs) — Phase 1's "which troops are demons"
//          catalog, the closest real analogue this codebase has to "a
//          kingdom folded into the demon faction" (per DemonSpawnCampaign-
//          Behavior's header note, demons hold no Kingdom of their own).
//        • HasJoinedTheDark flips permanently true — consulted by
//          AshenDiplomacyModel (never offered peace) and
//          DemonSpawnCampaignBehavior.DirectDemonParties (via
//          ForestWidowsQuestCampaignBehavior.IsDarkPactParty — the Night
//          Tide no longer preys on them).
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public sealed partial class ForestWidowsQuestCampaignBehavior
    {
        // Persisted so a mid-resolution save can't re-roll or re-apply the pact.
        private static bool _darkPactApplied = false;

        // Consulted by AshenDiplomacyModel and DemonSpawnCampaignBehavior — true
        // forever once the ending fires, regardless of which choice the player made.
        internal static bool HasJoinedTheDark => _darkPactApplied;

        private static void SyncResolutionData(IDataStore store)
        {
            try { store.SyncData("FWQ_DarkPactApplied", ref _darkPactApplied); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ResetResolutionState() { _darkPactApplied = false; }

        // ── The choice ────────────────────────────────────────────────────────────
        private static void ShowResolutionChoice()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "The Ledger Is Closed",

                "The last of it is carried to the altar in the root-cellar, and this time the stone does not go " +
                "quiet the way it always has before. Something in the wood itself seems to let go of a breath it " +
                "has held since the first Widow struck this bargain.\n\n" +
                "The Grand Widow feels it too — you can see it leave her face, the watchfulness that has never " +
                "once left a Widow's eyes since Marunath started paying. She turns to you, calm in a way that " +
                "does not look entirely like relief.\n\n" +
                "\"It is done. We will not be hunted again — because we will not be anyone left for the dark to " +
                "hunt. Go now, if you mean to go. Or stay, and let it take you the way it is about to take us.\"",

                true, true,
                "Go. Whatever this is, it isn't for me.",
                "Stay. I bought this peace too.",
                () => { try { ResolveCastOut(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                () => { try { ResolveStayed(); }  catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } }
            ), true, true);
        }

        // ── Ending A — cast out ──────────────────────────────────────────────────
        private static void ResolveCastOut()
        {
            try
            {
                Kingdom widows = GetForestWidowsKingdom();
                if (widows != null && Clan.PlayerClan != null && Clan.PlayerClan.Kingdom == widows)
                    try { ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            ApplyDarkPact();
            _phase = PhaseEndedCastOut;

            try { ForestWidowsQuestLog.Current?.LogEndingCastOut(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try
            {
                MBInformationManager.AddQuickInformation(new TextObject(
                    "You are outside the gate before the last of the Widows stop being anyone you knew. Marunath " +
                    "and Car Banseth belong to the dark now, completely — and you do not."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Ending B — stay ──────────────────────────────────────────────────────
        private static void ResolveStayed()
        {
            ApplyDarkPact();
            _phase = PhaseEndedStayed;

            try { ForestWidowsQuestLog.Current?.LogEndingStayed(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Final Peace",

                    "You do not step back from the altar.\n\n" +
                    "There is no pain in it, in the end — only the sense of a great many small, worried things " +
                    "inside you finally being allowed to stop. The war, the ledger, the counting of days: all of " +
                    "it goes quiet at once, the way the whole wood has just gone quiet.\n\n" +
                    "Calradia goes on without you. Somewhere behind your eyes, something that used to be entirely " +
                    "yours agrees that this was worth it, and means to go on agreeing, forever.\n\n" +
                    "This is the end of your part in the Long Night. Whatever comes after belongs to the dark now.",

                    true, false,
                    "So it is.",
                    "",
                    () => { },
                    () => { }
                ), true, true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The pact itself — permanent hostility, garrisons, demon-lockout ──────
        private static void ApplyDarkPact()
        {
            if (_darkPactApplied) return;
            _darkPactApplied = true;

            Kingdom widows = GetForestWidowsKingdom();
            if (widows == null) return;

            DeclareWarOnEveryone(widows);
            ReplaceGarrisonsWithDemons(widows);
        }

        // Permanently at war with every other kingdom, and with the player's own
        // clan directly if the player is (or has just become, via ResolveCastOut)
        // independent — mirrors GreatAwakeningCampaignBehavior.Resolution.
        // DeclarePermanentWar, but also covers a clanless-kingdom opponent since
        // an independent clan is its own IFaction in Bannerlord.
        private static void DeclareWarOnEveryone(Kingdom widows)
        {
            try
            {
                foreach (var other in Kingdom.All)
                {
                    if (other == null || other == widows || other.IsEliminated) continue;
                    if (widows.IsAtWarWith(other)) continue;
                    try { DeclareWarAction.ApplyByDefault(widows, other); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }

                if (Clan.PlayerClan != null && Clan.PlayerClan.Kingdom == null && Clan.PlayerClan != widows.RulingClan
                    && !widows.IsAtWarWith(Clan.PlayerClan))
                {
                    try { DeclareWarAction.ApplyByDefault(widows, Clan.PlayerClan); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Every settlement the Forest Widows still hold gets its garrison
        // cleared and refilled with the Kindled's own troops
        // (Demons/DemonCatalog.cs) — the closest real, mechanical analogue this
        // codebase has to "folding a kingdom into the demon faction" (demons
        // themselves hold no Kingdom of their own — see DemonSpawnCampaign-
        // Behavior.cs's header note).
        private static void ReplaceGarrisonsWithDemons(Kingdom widows)
        {
            try
            {
                CharacterObject fiend = MBObjectManager.Instance?.GetObject<CharacterObject>(DemonCatalog.FiendTroopId);
                CharacterObject stalker = MBObjectManager.Instance?.GetObject<CharacterObject>(DemonCatalog.StalkerTroopId);
                if (fiend == null && stalker == null) return;

                foreach (Settlement s in widows.Settlements.ToList())
                {
                    try
                    {
                        var garrison = s?.Town?.GarrisonParty?.MemberRoster;
                        if (garrison == null) continue;

                        foreach (var entry in garrison.GetTroopRoster().ToList())
                            try { garrison.AddToCounts(entry.Character, -entry.Number); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                        if (fiend != null)   garrison.AddToCounts(fiend, ForestWidowsQuestMath.GarrisonFiends);
                        if (stalker != null) garrison.AddToCounts(stalker, ForestWidowsQuestMath.GarrisonStalkers);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
