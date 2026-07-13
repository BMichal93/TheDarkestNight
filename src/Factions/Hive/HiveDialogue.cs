// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveDialogue.cs
// Replaces vanilla lord dialogue for all Hive lords (Battania culture/
// kingdom) with lines befitting a single fungal mind speaking through many
// mouths. Mirrors the structure of WolfBrothersDialogue.cs / TowerDialogue.cs.
// The vassal title "Integrated" is woven through every pool — a Hive lord is
// never called "vassal".
//
// EVERY line in every pool speaks in the plural ("we/us/our") — never "I/me/
// my". This is not flavour on top of the Hive's identity, it IS the Hive's
// identity: a lord who has drunk the elixir does not have a separate voice
// left to speak with.
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above the retired ForestClansDialogue-adjacent
// registrations (192, matching TowerDialogue) so this pool always wins if
// that identity is ever restored by mistake — Battania can only wear one
// identity. Set to 193 so it never collides with WolfBrothersDialogue (191)
// or TowerDialogue (192).
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class HiveDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 193;

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Hive lords keep every standard
            // interaction (tasks, barter, war/peace, recruitment). Gated on
            // HasMet so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "hiv_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "hiv_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "hiv_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "hiv_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "hiv_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsHiveVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsHiveVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ColourLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!HiveCulture.IsHiveLord(h)) return false;
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

        // ── Line pools — plural throughout; the vassal title "Integrated"
        //    replaces "vassal" wherever rank comes up ────────────────────────

        private static readonly string[] _openings =
        {
            "We felt you cross into our ground before we saw you. Speak, and we will listen with every ear we have.",
            "Every Integrated answers with one mind, not one mouth. What is it you want of us?",
            "We do not waste the network's attention on strangers who cannot say their business plainly. Well?",
            "You stand where our roots run deep. Curiosity, or need — we would know which brought you.",
            "The Hive does not pause its thinking for a stranger's business, but we will hear it. Speak.",
            "We judge a stranger by what they are willing to become, not by what they claim to be. Speak.",
        };

        private static readonly string[] _barters =
        {
            "Coin means little to us — we do not trade in what a single hand can lose. Bring something the network can use.",
            "We do not need your purse. It holds nothing the Hive lacks already.",
            "Every Integrated knows a fair exchange by what it feeds the whole. This offer feeds no one.",
        };

        private static readonly string[] _defeats1 =
        {
            "You have bested one of our roots, not the Hive itself. We will remember this, all of us together.",
            "Strike, if the moment asks it of you. We will simply grow back where you cut.",
            "Fair work. We do not pretend this outcome surprises the part of us that watched the field honestly.",
        };

        private static readonly string[] _defeats2 =
        {
            "We do not mourn long — a fallen root is not a fallen Hive. Another of us takes up the same ground tomorrow.",
            "You fight like something that already understands what we are. Well fought.",
            "We have survived colder losses than this one. Even this becomes something the network remembers.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage does not trouble us. We have grown through worse walls than these.",
            "Mind what you feed us — the Hive remembers every debt, and we are patient about collecting.",
            "We will wait. The network always reaches for its own, sooner or later.",
        };
    }
}
