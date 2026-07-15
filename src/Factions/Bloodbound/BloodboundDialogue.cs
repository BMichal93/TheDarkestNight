// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundDialogue.cs
// Replaces vanilla lord dialogue for all Bloodbound lords (Khuzait
// culture/kingdom) with lines befitting riders who traded the steppe for the
// hunt. Mirrors the structure of HiveDialogue.cs / TowerDialogue.cs /
// WolfBrothersDialogue.cs. The vassal title "Bloodhunter" is woven through
// every pool — a Bloodbound rider is never called "vassal".
//
// Each lord speaks from a pool of lines selected deterministically by their
// StringId hash. Multiple variants are registered; conditions evaluate at
// conversation time so the same lord always says the same line.
//
// Priority is set one step above HiveDialogue (193) — this pool always wins
// over the retired TribesDialogue (190) if that registration is ever
// restored by mistake — Khuzait can only wear one identity.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;

namespace AshAndEmber
{
    internal static class BloodboundDialogue
    {
        internal static void Register(CampaignGameStarter starter)
        {
            const int P = 194; // above HiveDialogue (193), below ArenicosDialogue (210)/AshenDialogue (200)

            // Flavour greeting only — route straight into the normal vanilla lord
            // options hub (hero_main_options) so Bloodbound lords keep every
            // standard interaction (tasks, barter, war/peace, recruitment). Gated
            // on HasMet so first-meeting introductions still run through vanilla.
            RegisterPool(starter, "bld_start",    "start",                "hero_main_options", _openings, P, requireMet: true, guardPostBattle: true);

            // Text-only flavour on flows vanilla already ends by closing the window
            // (prisoner chat) or on legacy/unreachable tokens. These never divert
            // the normal conversation hub.
            RegisterPool(starter, "bld_barter",   "lord_barter_question", "close_window",  _barters,   P);
            RegisterPool(starter, "bld_defeat1",  "defeated_lord_start_1","close_window",  _defeats1,  P);
            RegisterPool(starter, "bld_defeat2",  "defeated_lord_start_2","close_window",  _defeats2,  P);
            RegisterPool(starter, "bld_prisoner", "prisoner_chat",        "close_window",  _prisoners, P);
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
                        () => IsBloodboundVariant(variant, pool.Length, requireMet, guardPostBattle),
                        null,
                        priority);
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        private static bool IsBloodboundVariant(int variant, int poolSize, bool requireMet, bool guardPostBattle)
        {
            try
            {
                var h = Hero.OneToOneConversationHero;
                if (h == null || ElementLordRegistry.IsAshenLord(h)) return false;
                if (!h.IsLord) return false;   // lord dialogue only — never notables or wanderers
                if (requireMet && !h.HasMet) return false; // let vanilla handle the first-meeting introduction
                if (guardPostBattle && LordDialogueGuard.MustYieldToVanilla()) return false; // vanilla owns capture/prisoner talks
                if (!BloodboundCulture.IsBloodboundLord(h)) return false;
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

        // ── Line pools — the vassal title "Bloodhunter" replaces "vassal" throughout ──

        private static readonly string[] _openings =
        {
            "Another blade for the hunt, or just another mouth passing through? Speak your business.",
            "Every Bloodhunter rides out again before dusk. Say what you need before I saddle up.",
            "You've the look of someone who hasn't bled a demon yet. What do you want?",
            "We don't waste the light on chatter. Say your piece, hunter to hunter.",
            "The blood doesn't collect itself. Talk quickly.",
            "A Bloodhunter judges you by what you've killed, not what you say you are. Speak.",
        };

        private static readonly string[] _barters =
        {
            "Coin's worth less than a vial of what we bleed from them. Bring blood, or bring nothing.",
            "We trade in what proves a kill, not in shine. Your purse doesn't prove anything.",
            "Every Bloodhunter knows a fair trade by the weight of the hunt behind it. This isn't one.",
        };

        private static readonly string[] _defeats1 =
        {
            "Strike true. A Bloodhunter who falls to a blade at least falls to something that bleeds red.",
            "You've bested me fair. Better a clean loss to you than a slow one to the dark.",
            "Finish it. I've looked worse things in the eye than you.",
        };

        private static readonly string[] _defeats2 =
        {
            "The hunt doesn't stop for one rider down. Someone else takes the saddle before dusk.",
            "You fight like you've faced worse than me at night. Fair enough.",
            "I've ridden out from worse mornings than this one. This is just another kind of losing.",
        };

        private static readonly string[] _prisoners =
        {
            "A cage doesn't scare a Bloodhunter. We've all camped closer to the dark than this.",
            "Careful what you keep me for. My kin remember every debt, including this one.",
            "I'll wait. The hunt always rides back this way, one way or another.",
        };
    }
}
