// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionDialogue.cs
// Replaces vanilla lord dialogue for all Legion lords (Western Empire
// kingdom, StringId "empire_w") with lines befitting militarists who steal
// rather than produce. Mirrors the structure of EmpireDialogue.cs /
// BloodboundDialogue.cs / HiveDialogue.cs. The vassal title "Comrade" is
// woven through every pool — a Legion lord is never called "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above EmpireDialogue (195) — this pool always
// wins over any earlier registration for the same conversation tokens. Note
// membership is checked by KINGDOM (LegionCulture.IsLegionLord), never by
// CultureObject — the three Empire successor kingdoms share one culture, so
// an Empire lord and a Legion lord are otherwise indistinguishable by culture.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class LegionDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 196; // above EmpireDialogue (195)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Legion lords keep every standard
            // interaction (tasks, barter, war/peace, recruitment). Gated on HasMet
            // so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "leg_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "leg_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "leg_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "leg_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "leg_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsLegionVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsLegionVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ElementLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!LegionCulture.IsLegionLord(h)) return false;
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

        // ── Line pools — the vassal title "Comrade" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "Ortysia doesn't feed itself, Comrade. What do you need before I march again?",
            "We took Lageta by the sword, and we'll hold it the same way. Speak your business.",
            "There's no harvest worth boasting of out here — only what the column brings back. What is it?",
            "A Comrade earns his standing by what he takes, not what he's given. What do you want?",
            "Every quiet season is a wasted one. Say your piece before I find something better to do with the daylight.",
            "Might makes right, Comrade — mine's still standing. What brings you to my column?",
        };

        private static readonly string[] _barters =
        {
            "I'd sooner take it than trade for it, but I'll hear your offer.",
            "Coin's coin. Just don't mistake fair dealing for weakness.",
            "A Comrade honours a bargain struck plainly — try not to insult either of us.",
        };

        private static readonly string[] _defeats1 =
        {
            "Strike true. Legion doesn't beg, and it doesn't forget who beat it either.",
            "You've won the field. Remember the taste of it — I mean to return the favour.",
            "Finish it, then. A weak Comrade is no loss to the column.",
        };

        private static readonly string[] _defeats2 =
        {
            "You fight like Legion trained you. I'll allow that's a compliment, coming from me.",
            "Take what you like from the baggage. We'll take it back from someone else by nightfall.",
            "Might makes right — today it was yours. Don't get comfortable with it.",
        };

        private static readonly string[] _prisoners =
        {
            "A cell won't hold my temper long. Ransom me or don't, but decide quickly.",
            "Legion remembers its debts the way it remembers its raids — in full, and with interest.",
            "I've marched through worse than this cage. I'll march out of it too, one way or another.",
        };
    }
}
