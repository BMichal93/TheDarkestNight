// =============================================================================
// THE DARKEST NIGHT — Factions/ForestWidows/ForestWidowsDialogue.cs
// Replaces vanilla lord dialogue for all Forest Widows lords (Battania
// kingdom, StringId "battania") with lines befitting a court of women who
// bought their peace with the blood of the men who could not keep it.
// Mirrors the structure of PaleWidowsDialogue.cs. The vassal title "Widow"
// is woven through every pool — a Forest Widows lord (always female, see
// ForestWidowsCampaignBehavior's leadership enforcement) is never called
// "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority 193 — the slot the (deleted) HiveDialogue used to occupy, still
// below BloodboundDialogue (194) as that file's own comment already notes.
// Membership is checked by KINGDOM/CULTURE (ForestWidowsCulture.
// IsForestWidowLord) — Battania owns its own unshared culture, so this is
// unambiguous (unlike the Pale Widows, which must disambiguate from the
// other two Empire successors).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class ForestWidowsDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 193; // the slot the retired HiveDialogue used to occupy

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Forest Widows lords keep every
            // standard interaction (tasks, barter, war/peace, recruitment).
            // Gated on HasMet so first-meeting introductions still run through
            // vanilla.
            RegisterPool(starter, "fw_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the
            // window (prisoner chat) or on legacy/unreachable tokens. These never
            // divert the normal conversation hub.
            RegisterPool(starter, "fw_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "fw_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "fw_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "fw_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsForestWidowsVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsForestWidowsVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ColourLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!ForestWidowsCulture.IsForestWidowLord(h)) return false;
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

        // ── Line pools — the vassal title "Widow" replaces "vassal" throughout ──
        // The odd male lord who slips through before the leadership sweep
        // catches him would still speak these — the title is a kingdom-wide
        // convention, not a strictly gendered address, since even a husband
        // knows exactly whose court he stands in.

        private static readonly string[] _openings =
        {
            "Marunath still stands because the ledger was paid, Widow. What business brings you to my hall?",
            "We did not survive the Long Night by being gentle about it. Speak plainly.",
            "Every man in Car Banseth knows his place now — mine is to rule, and his is to be grateful for it. What do you need?",
            "The dark took what it was owed and left the rest of us in peace among the trees. Don't waste that peace on small talk.",
            "A Widow does not flinch from what keeps her hold standing. What's your business?",
            "I've buried more husbands than I care to count keeping the wood-line closed. Say your piece.",
        };

        private static readonly string[] _barters =
        {
            "Coin is easy. It's the other ledger that costs something.",
            "I'll hear your offer — the court has grown practical about what it takes to survive.",
            "Fair enough, Widow. Just don't mistake a good bargain for a soft one.",
        };

        private static readonly string[] _defeats1 =
        {
            "You've won the field. The ledger doesn't care who's holding the knife when it's balanced.",
            "Strike true, then. Marunath has buried better than you and kept standing.",
            "Finish it. I've made harder bargains than losing to you.",
        };

        private static readonly string[] _defeats2 =
        {
            "You fight like someone who's had to. I understand that better than you'd think.",
            "Take what you like from the baggage — the court will manage without it.",
            "Today the debt was mine to pay. Don't count on it staying that way.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage won't hold what Marunath owes me. Ransom me or don't — decide quickly.",
            "I've bargained with worse than a captor's cell. I'll bargain my way out of this one too.",
            "Careful what you offer the dark for my ransom, stranger. The Widows keep count of debts like that.",
        };
    }
}
