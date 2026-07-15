// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireDialogue.cs
// Replaces vanilla lord dialogue for all Empire lords (Northern Empire
// culture/kingdom) with lines befitting the last realm still governing as if
// Calradia might be put back together. Mirrors the structure of
// BloodboundDialogue.cs / HiveDialogue.cs / TowerDialogue.cs /
// WolfBrothersDialogue.cs. The vassal title "Legate" is woven through every
// pool — an Empire lord is never called "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above BloodboundDialogue (194) — this pool always
// wins over any earlier registration for the same conversation tokens.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class EmpireDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 195; // above BloodboundDialogue (194)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Empire lords keep every standard
            // interaction (tasks, barter, war/peace, recruitment). Gated on HasMet
            // so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "emp_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "emp_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "emp_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "emp_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "emp_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsEmpireVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsEmpireVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ElementLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!EmpireCulture.IsEmpireLord(h)) return false;
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

        // ── Line pools — the vassal title "Legate" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "The rolls still need keeping, Legate or not. What brings you to my column?",
            "Saneopa still pays its levies on time. Ask your business and let me get back to the count.",
            "Every province we've lost is still logged in the census, waiting to be reclaimed on paper if nowhere else. What do you need?",
            "The old rites don't observe themselves. Speak plainly.",
            "We keep the muster rolls the way the first Emperors did. It's the only Empire left to keep. What is it?",
            "A Legate's time is the throne's time, even a throne that only holds three cities now. Say your piece.",
        };

        private static readonly string[] _barters =
        {
            "Coin has a proper weight in the Empire still — unlike in the wastes. Let's see if yours does too.",
            "We trade by the old measures. Try not to insult either of us with a bad one.",
            "A Legate honours a fair exchange. Make this one fair.",
        };

        private static readonly string[] _defeats1 =
        {
            "Strike true, then. The Empire has buried better soldiers than either of us to worse ends.",
            "You've won this field. Note it properly, if you keep records — the Empire always did.",
            "Finish it. I served the old rites; I'll not beg to skip the last one.",
        };

        private static readonly string[] _defeats2 =
        {
            "The census will note one fewer Legate riding tomorrow. It changes little else.",
            "You fight like the Empire once did, before it learned to lose gracefully. Fair enough.",
            "I've read worse defeats in the old campaign logs than the one I'm living now.",
        };

        private static readonly string[] _prisoners =
        {
            "A cell doesn't trouble a Legate. I've kept worse company than you in the archive vaults.",
            "Careful what you keep me for. The Empire remembers every debt in writing, including this one.",
            "I'll wait. The old rites teach patience, if nothing else survives of them.",
        };
    }
}
