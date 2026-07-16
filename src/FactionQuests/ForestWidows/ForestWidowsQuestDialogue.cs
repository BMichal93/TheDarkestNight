// =============================================================================
// THE DARKEST NIGHT — FactionQuests/ForestWidows/ForestWidowsQuestDialogue.cs
//
// "Afterwards, contacting any Widow gets only the demons' answer: '...'"
// Once ForestWidowsQuestCampaignBehavior.HasJoinedTheDark is true, every line
// Factions/ForestWidows/ForestWidowsDialogue.cs would otherwise give a Forest
// Widows lord is replaced with "..." — mirrors AI/AshenDialogue.cs's silence
// pattern exactly (same input/output tokens, same "flavour vs. hard block"
// split), but gated on the ending flag instead of a permanent faction
// identity, and registered at a higher priority (210) than both
// ForestWidowsDialogue's own pool (193) and AshenDialogue's (200) so it wins
// the instant the pact is sealed — a former Forest Widow lord no longer has
// anything a living person would call speech.
// =============================================================================

using TaleWorlds.CampaignSystem;

namespace TheDarkestNight
{
    internal static class ForestWidowsQuestDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 210; // above ForestWidowsDialogue (193) and AshenDialogue (200)

            // ── Opening and any standard lord sub-state ─────────────────────────
            try { starter.AddDialogLine("fwq_silence_start",   "start",                  "fwq_silence_done", "...", IsSilencedContext, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { starter.AddDialogLine("fwq_silence_pretalk", "lord_pretalk",           "fwq_silence_done", "...", IsSilencedContext, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // Player's only available response — also "..."
            try { starter.AddPlayerLine("fwq_silence_close",   "fwq_silence_done", "close_window", "...", null, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // ── Barter / negotiation ─────────────────────────────────────────────
            try { starter.AddDialogLine("fwq_silence_barter",  "lord_barter_question",   "close_window", "...", IsSilencedLord, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // ── Defeat / surrender offers ─────────────────────────────────────────
            try { starter.AddDialogLine("fwq_silence_defeat1", "defeated_lord_start_1",  "close_window", "...", IsSilencedLord, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { starter.AddDialogLine("fwq_silence_defeat2", "defeated_lord_start_2",  "close_window", "...", IsSilencedLord, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { starter.AddDialogLine("fwq_silence_special", "lord_special_request",   "close_window", "...", IsSilencedLord, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            // ── Prisoner conversation ─────────────────────────────────────────────
            try { starter.AddDialogLine("fwq_silence_prisoner","prisoner_chat",          "close_window", "...", IsSilencedLord, null, P); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Guarded exactly like AshenDialogue.IsAshenFlavourContext — vanilla still
        // owns the defeated-lord capture/prisoner flows so those tokens don't get
        // silenced out from under LordDialogueGuard.
        private static bool IsSilencedContext()
        {
            return IsSilencedLord() && !LordDialogueGuard.MustYieldToVanilla();
        }

        private static bool IsSilencedLord()
        {
            try
            {
                if (!ForestWidowsQuestCampaignBehavior.HasJoinedTheDark) return false;
                var h = Hero.OneToOneConversationHero;
                if (h == null || !h.IsLord) return false;
                return ForestWidowsCulture.IsForestWidowLord(h);
            }
            catch { return false; }
        }
    }
}
