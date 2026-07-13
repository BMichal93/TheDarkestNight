// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerDialogue.cs
// Replaces vanilla lord dialogue for all Tower lords (Aserai culture/kingdom)
// with lines befitting scholars who study the Long Night instead of merely
// enduring it. Mirrors the structure of WolfBrothersDialogue.cs / DunebornDialogue.cs.
// The vassal title "Warlock" is woven through every pool — a Tower lord is
// never called "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above DunebornDialogue (190) so this pool always
// wins if that registration is ever restored by mistake — Aserai can only
// wear one identity. Kept below WolfBrothersDialogue (191) purely so the two
// new faction pools never collide on priority (they never target the same
// kingdom, so this ordering has no practical effect beyond documentation).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class TowerDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 192; // above the retired DunebornDialogue (190) and WolfBrothersDialogue (191)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Tower lords keep every standard
            // interaction (tasks, barter, war/peace, recruitment). Gated on
            // HasMet so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "twr_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "twr_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "twr_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "twr_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "twr_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsTowerVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsTowerVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ColourLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!TowerCulture.IsTowerLord(h)) return false;
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

        // ── Line pools — the vassal title "Warlock" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "You've caught a Warlock mid-thought. Speak plainly, and I may finish it after.",
            "Every Warlock of the Tower answers to the work before rank. What is it you need?",
            "We do not waste ink on visitors who cannot be bothered to state their business. Well?",
            "You stand in the Tower's shadow uninvited. Curiosity, or need — which brought you?",
            "The work does not pause for a stranger's business, but I will hear it. Speak.",
            "A Warlock is judged by what they are willing to learn, not by what they claim to know. Speak.",
        };

        private static readonly string[] _barters =
        {
            "Coin buys little the Tower cannot already command. Bring something worth studying.",
            "The Tower trades in knowledge, not in your purse. This offer holds neither.",
            "Every Warlock knows a fair exchange by its weight in use, not its shine. This isn't one.",
        };

        private static readonly string[] _defeats1 =
        {
            "You have the better of me. A Warlock studies defeat as closely as victory — this one, most of all.",
            "Strike, if you must. The Tower will simply write down how it happened, and why.",
            "Fair work. I'll not pretend the outcome surprises a mind that reads the field honestly.",
        };

        private static readonly string[] _defeats2 =
        {
            "The Tower does not mourn long. Another Warlock takes up the same question tomorrow.",
            "You fight like something that has already read every page I have. Well fought.",
            "I've survived colder losses than this. Even this becomes a note in someone's ledger.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage does not trouble a Warlock. I've studied worse rooms than this one.",
            "Mind what you feed me — the Tower remembers every debt, including this one.",
            "I'll wait. The Tower always sends for its own, sooner or later.",
        };
    }
}
