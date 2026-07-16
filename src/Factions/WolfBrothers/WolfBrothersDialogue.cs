// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersDialogue.cs
// Replaces vanilla lord dialogue for all Wolf Brothers lords (Sturgia
// culture/kingdom) with lines befitting the pack that outlived the Long Night
// by eating what would not go to waste. Mirrors the structure of
// TempleDialogue.cs / NorthmenDialogue.cs. The vassal title "Kinsman" is
// woven through every pool — a Wolf Brother is never called "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above the legacy NorthmenDialogue (190) so this
// pool always wins if that registration is ever restored by mistake — Sturgia
// can only wear one identity.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace TheDarkestNight
{
    internal static class WolfBrothersDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 191; // above the retired NorthmenDialogue (190), below ArenicosDialogue (210)/AshenDialogue (200)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Wolf Brothers lords keep every
            // standard interaction (tasks, barter, war/peace, recruitment). Gated
            // on HasMet so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "wlf_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "wlf_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "wlf_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "wlf_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "wlf_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
        }

        // Registers one line per pool entry. Conditions pick the right variant
        // at conversation time based on the interlocutor's StringId hash.
        private static void RegisterPool(CampaignGameStarter starter,
            string idPrefix, string inputToken, string outputToken,
            string[] pool, int priority, bool requireMet = false, bool guardPostBattle = false)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                int variant = i; // capture for closure
                string text = pool[i];
                try
                {
                    starter.AddDialogLine(
                        $"{idPrefix}_{variant}",
                        inputToken, outputToken,
                        text,
                        () => IsWolfBrotherVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static bool IsWolfBrotherVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ElementLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!WolfBrothersCulture.IsWolfBrotherLord(h)) return false;
                int idx = Math.Abs(DeterministicHash(h.StringId ?? h.Name?.ToString() ?? "")) % poolSize;
                return idx == variant;
            }
            catch { return false; }
        }

        // String.GetHashCode() is randomized per-process in .NET 4.7.2+.
        // This deterministic version ensures the same lord always says the same line.
        private static int DeterministicHash(string s)
        {
            int h = 0;
            foreach (char c in s) h = h * 31 + c;
            return h;
        }

        // ── Line pools — the vassal title "Kinsman" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "You've caught the pack between kills. Say your business, or move along.",
            "Every Kinsman answers to hunger before they answer to rank. Speak quickly — mine hasn't been fed.",
            "We don't waste words on strangers who might yet be meat. What do you want?",
            "You walk into Wolf ground uninvited. That took either courage or stupidity. Which is it?",
            "The pack doesn't stop moving for a stranger's business. Say your piece and keep pace.",
            "A Kinsman judges you by what you're willing to give up, not by what you say you are. Speak.",
        };

        private static readonly string[] _barters =
        {
            "Coin doesn't fill a belly. Bring meat, or bring nothing.",
            "The pack trades in what can be eaten or worn. Your purse is neither.",
            "Every Kinsman knows a fair trade by its weight in flesh, not its shine. This isn't one.",
        };

        private static readonly string[] _defeats1 =
        {
            "Strike true. A Kinsman who falls clean is still worth more than one who begs.",
            "You've bested me fair. The pack respects that more than it respects rank.",
            "Finish it, or don't. Either way I'll not whimper for you.",
        };

        private static readonly string[] _defeats2 =
        {
            "The pack doesn't mourn long. Someone else takes my place and the hunt goes on.",
            "You fight like something that's already survived worse than me. Fair enough.",
            "I've outlasted colder nights than this one. This is just another kind of losing.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage doesn't scare a Kinsman. We've all slept in worse, and eaten worse company.",
            "Careful what you feed me. My pack remembers every debt, including this one.",
            "I'll wait. The pack always comes looking, one way or another.",
        };
    }
}
