// =============================================================================
// THE DARKEST NIGHT — Visual/BattleWhispers.cs
// Ambient demonic-dread messages during battles where the Night's demons are on
// the field. Fires at most 4 times per battle, spaced ~45 s apart, never repeating.
// A deeply-attuned mage (WhisperTier 3) hears the dark address them by name.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TheDarkestNight
{
    public static class BattleWhispers
    {
        private static float _timer             = 40f;
        private static int   _whispersFired     = 0;
        private static bool  _enabled           = false;
        private static readonly HashSet<int> _used = new HashSet<int>();
        private static readonly Random _rng       = new Random();
        private const  int MaxWhispersPerBattle   = 4;

        private static readonly string[] _whispers =
        {
            "A cold gaze finds you through the smoke. Nothing living looks at you that way.",
            "The dark presses closer than the fighting warrants.",
            "You hear your name — no one's lips are moving.",
            "Something on the far side of the field pulls at you, patient and cold.",
            "One of them is not fighting. It is feeding on the fear before the flesh.",
            "A demon watches you between blows — not as a foe. As food that is still moving.",
            "You smell winter. It is midsummer.",
            "Across the field, a pair of eyes that are not a man's find yours. They hold.",
            "The dark gathers thicker where the demons stand. They are not fighting a battle. They are feeding.",
            "Something in their line is not afraid of you. That is the one to watch.",
            "You feel watched from inside your own shadow.",
            "A familiar cold brushes the back of your neck.",
            "One of their soldiers glances at you — not with hate, but with recognition.",
            "The dead on their side do not fall the way dead men should.",
            "Something beyond the treeline watches with familiar eyes.",
        };

        // Only reached at WhisperTier 3 — the dark now speaks directly to you.
        private static readonly string[] _whispersPersonal =
        {
            "The voice says your name the way someone says the name of a thing they already own.",
            "You know which of them carries the deepest cold. You knew before you saw it.",
            "Something in you leans toward their line. You have to remind it which side you are on.",
            "One of them smiles at you across the field. You almost smile back.",
            "The dark drifts toward you, not away. As if it remembers the marks you have been drawing.",
        };

        public static void Reset()
        {
            _timer         = 40f;
            _whispersFired = 0;
            _enabled       = false;
            _used.Clear();
        }

        public static void MissionTick(float dt)
        {
            if (_whispersFired >= MaxWhispersPerBattle) return;
            if (!SpellEffects.IsBattleMission()) return;

            _timer -= dt;
            if (_timer > 0f) return;
            _timer = 45f + _rng.Next(30);

            if (!_enabled)
            {
                // The whispers are the Night itself — they only stir when demons
                // are actually on the field.
                bool trigger = false;
                try
                {
                    if (Mission.Current != null)
                        trigger = DemonBattleBehavior.GetActiveDemons().Count > 0;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (!trigger) return;
                _enabled = true;
            }

            // Skip probability scales with how deeply the dark has seeped in.
            // Tier 3: no skip — the voice does not wait for permission.
            int whisperTier = MageKnowledge.IsMage ? MageKnowledge.WhisperTier : 0;
            int skipDenominator = whisperTier >= 3 ? 0 : whisperTier >= 2 ? 4 : 3;
            if (skipDenominator > 0 && _rng.Next(skipDenominator) == 0) return;

            // At Tier 3 the cold speaks personally — draw from the combined pool.
            bool usePersonal = whisperTier >= 3 && _rng.Next(2) == 0;
            string[] pool = usePersonal ? _whispersPersonal : _whispers;

            int idx = -1;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                // Offset personal-pool indices to keep them separate from _used for the base pool.
                int candidate = usePersonal ? _whispers.Length + _rng.Next(pool.Length) : _rng.Next(pool.Length);
                if (!_used.Contains(candidate)) { idx = candidate; break; }
            }
            if (idx < 0) return;

            _used.Add(idx);
            _whispersFired++;
            string line = usePersonal ? pool[idx - _whispers.Length] : pool[idx];

            try
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    line, new Color(0.35f, 0.4f, 0.6f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
