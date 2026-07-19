using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    [TestFixture]
    public partial class PureLogicTests
    {

        // ── AgingSystem pure math ─────────────────────────────────────────────

        // Tests use the pure overload (explicit hero age) so they run without the
        // TaleWorlds runtime. Age 40 is the Tempered threshold — no age discount.
        private const float NoAgeDiscount = 40f;


        // ── ElementComboMath tests ───────────────────────────────────────────

        private static readonly MagicElement[] _baseElements =
        {
            MagicElement.Fire, MagicElement.Wind, MagicElement.Earth,
            MagicElement.Water, MagicElement.Spirit,
        };


        // ── Fusion Ultimates ──────────────────────────────────────────────────

        private static readonly MagicElement[] _fusionElements =
        {
            MagicElement.Lightning, MagicElement.Fog, MagicElement.Magma,
            MagicElement.Ice, MagicElement.Sandstorm, MagicElement.Mire,
        };


        // ── Save-definer guard ────────────────────────────────────────────────
        //
        // A QuestBase subclass that reaches the QuestManager without an
        // AddClassDefinition row cannot be serialized, and the whole campaign save
        // FAILS — but only once that quest has actually started, never at build time.
        // That is what repeatedly broke saving on the big questlines, so it is guarded
        // here instead of being left to review.
        //
        // The check is deliberately SOURCE-based, not reflection-based: loading the mod
        // assembly's quest types would drag in the TaleWorlds runtime, which the pure
        // test suite must not depend on (see behaviour.md — JIT type resolution).

        private static string RepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(
                System.IO.Path.GetDirectoryName(typeof(PureLogicTests).Assembly.Location));
            while (dir != null)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "src"))
                    && System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "src", "SaveDefiner.cs")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }


        // The 20 matter-states (5 elements + 6 fusions + 4 commands + 4 Triads +
        // the Unbound Weave), each as the runes that produce it. Command and Unbound
        // reject any Form (declared contradictions); the rest accept every Form.
        private static readonly (string name, RuneId[] runes, bool formRejected)[] _matterStates =
        {
            ("Fire",  new[]{RuneId.Cinder}, false), ("Water", new[]{RuneId.Tide}, false),
            ("Earth", new[]{RuneId.Stone},  false), ("Wind",  new[]{RuneId.Gale}, false),
            ("Wyrd",  new[]{RuneId.Wyrd},   false),
            ("Fire+Wind",  new[]{RuneId.Cinder,RuneId.Gale},  false),
            ("Fire+Water", new[]{RuneId.Cinder,RuneId.Tide},  false),
            ("Fire+Earth", new[]{RuneId.Cinder,RuneId.Stone}, false),
            ("Wind+Water", new[]{RuneId.Gale,RuneId.Tide},    false),
            ("Wind+Earth", new[]{RuneId.Gale,RuneId.Stone},   false),
            ("Earth+Water",new[]{RuneId.Stone,RuneId.Tide},   false),
            ("Cmd Fire",  new[]{RuneId.Wyrd,RuneId.Cinder}, true),
            ("Cmd Wind",  new[]{RuneId.Wyrd,RuneId.Gale},   true),
            ("Cmd Earth", new[]{RuneId.Wyrd,RuneId.Stone},  true),
            ("Cmd Water", new[]{RuneId.Wyrd,RuneId.Tide},   true),
            ("Tempest",   new[]{RuneId.Cinder,RuneId.Gale,RuneId.Tide},  false),
            ("Eruption",  new[]{RuneId.Cinder,RuneId.Gale,RuneId.Stone}, false),
            ("Seething",  new[]{RuneId.Cinder,RuneId.Tide,RuneId.Stone}, false),
            ("Avalanche", new[]{RuneId.Gale,RuneId.Tide,RuneId.Stone},   false),
            ("Unbound",   new[]{RuneId.Cinder,RuneId.Gale,RuneId.Stone,RuneId.Tide}, true),
        };


        private static readonly RuneId[] _formRunes =
        {
            RuneId.LongMark, RuneId.Bar, RuneId.Calling, RuneId.Snare,
            RuneId.Brand, RuneId.Husk, RuneId.Ring, RuneId.Rain,
        };
    }
}
