// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenDialogue.cs
// Replaces vanilla lord dialogue for all Chosen lords (Southern Empire
// kingdom, StringId "empire_s") with lines befitting zealots absolutely
// certain their PriestKing was chosen by Heaven itself. Mirrors the
// structure of LegionDialogue.cs / PaleWidowsDialogue.cs (deleted). The
// vassal title "Apostle" is woven through every pool.
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above PaleWidowsDialogue's old slot (197) — this
// pool always wins over any earlier registration for the same conversation
// tokens. Note membership is checked by KINGDOM (ChosenCulture.IsChosenLord),
// never by CultureObject — the three Empire successor kingdoms share one
// culture, so an Empire lord and a Chosen lord are otherwise
// indistinguishable by culture.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class ChosenDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 198; // above the retired PaleWidowsDialogue slot (197)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Chosen lords keep every
            // standard interaction (tasks, barter, war/peace, recruitment).
            // Gated on HasMet so first-meeting introductions still run through
            // vanilla.
            RegisterPool(starter, "cho_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the
            // window (prisoner chat) or on legacy/unreachable tokens. These never
            // divert the normal conversation hub.
            RegisterPool(starter, "cho_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "cho_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "cho_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "cho_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsChosenVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsChosenVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ElementLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!ChosenCulture.IsChosenLord(h)) return false;
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

        // ── Line pools — the vassal title "Apostle" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "The PriestKing's light still burns over Phycaon, Apostle. What brings you to my hall?",
            "We do not debate what was shown to the first of us on the walls. Speak your business.",
            "Every soul in Lycaron kneels to the same vision that saved it. What do you need?",
            "Heaven does not repeat itself twice for the unworthy. Don't waste the favour it showed us.",
            "An Apostle does not flinch from the PriestKing's word. Say your piece.",
            "I've buried men beside that vision and never once doubted it. What's your business?",
        };

        private static readonly string[] _barters =
        {
            "Coin is easy. Faith is the ledger that actually matters.",
            "I'll hear your offer — the Chosen have never been precious about gold.",
            "Fair enough, Apostle. Just don't mistake a good bargain for a holy one.",
        };

        private static readonly string[] _defeats1 =
        {
            "You've won the field. The vision never promised us an easy road, only a saved one.",
            "Strike true, then. Phycaon has buried better than you and kept its faith standing.",
            "Finish it. The PriestKing's line does not end with me.",
        };

        private static readonly string[] _defeats2 =
        {
            "You fight like someone certain of something. I understand that better than you'd think.",
            "Take what you like from the baggage — Heaven's favour was never carried in a saddlebag.",
            "Today the field was yours. The vision does not change because of one battle.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage won't hold what the PriestKing is owed. Ransom me or don't — decide quickly.",
            "I've knelt to worse than a captor's cell and risen again. I'll rise from this one too.",
            "Careful what you offer for my ransom, stranger. The Chosen keep count of every debt like that.",
        };
    }
}
