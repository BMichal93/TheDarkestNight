// =============================================================================
// THE DARKEST NIGHT — QuestSystems/KeybindReferenceLog.cs
// A permanent, never-completing Journal entry that records the shape of the
// new world and every control this mod adds to live in it — the spellbook and
// its formulas, the night tide, the eight factions, the ruins, relics, wands,
// talismans, and the barter economy that replaced honest coin. Created once
// per campaign (new or existing save), re-linked on load, and rewritten in
// place when the listed lore/controls change between builds. Pure reference:
// no goals, no progress, no events.
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public static class KeybindReferenceSystem
    {
        internal static KeybindReferenceLog _log = null;

        // Called from MagicCampaignBehavior.OnDailyTick. On a fresh campaign or an
        // older save the quest is absent, so we create it. On a save that already
        // holds it, InitializeQuestOnGameLoad has set _log first, so we leave it.
        //
        // NOTE: _log is a static and survives between save loads in the same process.
        // Loading a second campaign without restarting can leave _log pointing at the
        // PREVIOUS campaign's quest, so a plain "_log == null" check would wrongly
        // skip creation and the journal entry would never appear. Guard instead on
        // whether the handle actually belongs to the current campaign's quest list.
        public static void DailyTick() => EnsureForSession();

        // Creates the journal entry if it is not already part of the current
        // campaign. Safe to call from new-game setup (so it is there from the
        // start) and from the daily tick (so loaded saves that lack it get it).
        public static void EnsureForSession()
        {
            try
            {
                if (Campaign.Current == null) return;
                // A quest that deserialised finalized (the engine can conclude/fail a
                // custom log across a save/load) must be treated as absent so a fresh,
                // live entry is created — otherwise the Journal is left with only the
                // dead "failed" copy of Notes for the Adventurer.
                bool present = _log != null
                    && !_log.IsFinalized
                    && Campaign.Current.QuestManager?.Quests?.Contains(_log) == true;
                if (!present) { _log = null; EnsureLog(); return; }
                // Present already: an older save may hold a shorter/outdated copy of
                // the reference text — rewrite it so the controls are always current.
                _log.RefreshEntriesIfStale();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void EnsureLog()
        {
            if (_log != null) return;
            // Assign _log only after the quest fully starts, so a transient failure
            // leaves _log null and we retry next tick rather than stranding a
            // half-created quest that never appears in the Journal.
            var log = new KeybindReferenceLog();
            log.StartQuest();
            log.WriteEntries();
            _log = log;
        }

        public static void ResetForNewGame()
        {
            _log = null;
        }
    }

    public sealed class KeybindReferenceLog : QuestBase
    {
        public KeybindReferenceLog()
            : base("ae_keybind_reference", Hero.MainHero, CampaignTime.Never, 0) { }

        public override TextObject Title => new TextObject("Notes for the Adventurer");
        // Exempts the quest from the engine's cancel-on-load sweep (see GreatAwakeningQuestLog).
        public override string SpecialQuestType => "AshAndEmberQuest";
        public override bool IsRemainingTimeHidden => true;

        protected override void InitializeQuestOnGameLoad()
        {
            KeybindReferenceSystem._log = this;
        }

        protected override void RegisterEvents() { }
        protected override void SetDialogs() { }

        // Not serialized, so it defaults to false each time the quest is loaded —
        // giving us exactly one rewrite per session. That keeps the listed controls
        // current on every load (even text-only changes) without rewriting daily.
        private bool _refreshedThisSession = false;

        // Rewrites the reference text once per session so it always matches the build.
        internal void RefreshEntriesIfStale()
        {
            if (_refreshedThisSession) return;
            _refreshedThisSession = true;
            try { WriteEntries(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Each AddLog is one line in the Journal. Clears any prior entries first so
        // this doubles as a refresh; hideInformation:true avoids a wall of toast
        // pop-ups when the (reference-only) entries are written or rewritten.
        internal void WriteEntries()
        {
            try
            {
                if (JournalEntries != null)
                    foreach (var e in JournalEntries.ToList())
                        RemoveLog(e);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            AddLog(new TextObject(
                "THE LONG NIGHT — Calradia had a thousand years of history, and then it had one night. The sky " +
                "tore, and what came through did not stop coming. By morning the great armies were ash and the " +
                "great cities were tombs. Only walls held, where walls were high enough, and only faith held, " +
                "where faith was hard enough. What follows is set down so none of it is forgotten — read it, " +
                "and you will understand more of this world than most who still live in it."), true);

            AddLog(new TextObject(
                "THE NIGHT TIDE — Every dusk the ground gives up its dead. Demon warbands rise near " +
                "settlements, along the roads, out in open wilderness — aggressive, numerous, and utterly " +
                "without cunning. They never retreat, never flee a stronger foe, never manoeuvre — only " +
                "charge, again and again, until the sun returns and they sink back below. What survives a " +
                "day's fighting is whole again by the next nightfall. There is no treating with them and no " +
                "buying them off; the only diplomacy the dark understands is a wall high enough or a blade " +
                "quick enough. The land itself marks them: the snow-born are pale and hardier, the desert-born " +
                "leaner and faster, the forest-born quieter on their feet — but a demon is a demon everywhere, " +
                "grey-black flesh and an ember-red glow, and every one of them burns better than it bleeds. " +
                "Rarely, by day, they throw themselves at a town's own gate — no one has yet explained why " +
                "those days are worse than the rest."), true);

            AddLog(new TextObject(
                "SOMETHING GATHERS — Whatever leads the tide has not always led it; the old-timers say the dark " +
                "used to be mindless, and it is not entirely mindless anymore. Somewhere in the wastes a will is " +
                "assembling itself out of every demon it has ever spent — and when it is ready, it will not rise " +
                "as a warband. It will rise as a Lord, take a settlement the way a rebel notable takes one, and " +
                "hold it. Kill that thing before it holds three, or hold the line until it can be killed; there " +
                "is no third ending written into this war."), true);

            AddLog(new TextObject(
                "THE SPELLBOOK — Magic is not taught in these towns anymore; it is found, spoken, or misspoken. " +
                "Spend one focus point to open your spellbook — once, forever — and the old formulas will answer " +
                "your hands. Hold Left Alt (gamepad: hold X) with BOTH hands empty and tap the marks — W/A/S/D " +
                "(gamepad: flick the left stick) for Up/Left/Down/Right — then release Alt to speak the shape " +
                "you have drawn. Every formula is five to twenty marks long; the short ones are common workings, " +
                "the long ones are things that used to be gods' business. Speak a shape you already know and it " +
                "answers as it should. Speak a shape you have never learned, but get it EXACTLY right, and it " +
                "still answers — the working teaches itself to a hand steady enough to cast it true. Tap X " +
                "(gamepad: click the left stick) with an empty formula buffered to open the book itself and read " +
                "what you have learned."), true);

            AddLog(new TextObject(
                "SPELLBURN — Speak the marks wrong and the working still tries to answer — it simply answers " +
                "wrong. A completed, incorrect formula always fizzles, and better than half the time (less, the " +
                "sharper your mind) it spellburns besides: you may be scorched for your trouble, rooted where " +
                "you stand, made to shout a fool's order at your own line, or made to burst outright and scald " +
                "everyone near you. Worse tellings summon a demon onto the field for a time, seal your weapon " +
                "hand shut, drop a false night over the battle, or tear your own voice and your line's nerve " +
                "with it. There is no telling in advance which burn a bad shape will bring — only that free " +
                "hands, a clear head, and a formula spoken slowly are the only ward against it."), true);

            AddLog(new TextObject(
                "THE RARE-GIFTED — Perhaps one lord or sworn companion in twenty carries the old craft at all, " +
                "and fewer still are trained to it: a scattered Hollow Choir of true spellcaster troops still " +
                "marches under a handful of banners, each of them bearing two or three battle-workings and " +
                "nothing else to their name. No one casts from a saddle over a map anymore — whatever magic " +
                "still answers, answers only on a battlefield, with a life already staked on the outcome."), true);

            AddLog(new TextObject(
                "RELICS, WANDS, AND TALISMANS — The dead world left tools behind, and demons carry stranger " +
                "ones still. A RELIC is a named thing, one of a kind, pulled off a fallen lord or out of a " +
                "collapsed vault — its power is real but always the lesser echo of what it must once have been. " +
                "A WAND holds a single working bound into wood and stone, loosed on a landed blow without a " +
                "formula spoken at all — rarer than gold and priced like it. A TALISMAN is quieter: a passive " +
                "ward or blessing worn rather than wielded, and the Temple's Brother Templars carry the truest " +
                "of them. None of it is common. All of it is worth killing for."), true);

            AddLog(new TextObject(
                "THE RUINS — Some four in five of the old castles are empty now, garrison and lord both gone, " +
                "and Scouting alone will tell you how quickly you can search one without the dark catching you " +
                "at it. Explore chamber by chamber — collapsed halls, flooded cellars, a lord's door barred from " +
                "the inside, a chapel to whatever they prayed to before the Night — and each one costs time you " +
                "may not have; wait too long between chambers and nightfall finds you in the dark with everything " +
                "that rises in it. What you carry out is worth the risk: weapons, armour, trade goods, the " +
                "occasional relic, and now and again a formula scratched into a wall by someone who did not " +
                "survive learning it."), true);

            AddLog(new TextObject(
                "THE FREE TOWNS — Every settlement that is not one of the eight great houses' own has been cut " +
                "loose to stand or fall on its own account — a wretched little city-state under whatever clan " +
                "still holds its walls, bearing that clan's own name and no other master's. Some are proud. Most " +
                "are simply what is left when a kingdom cannot spare the men to keep them."), true);

            AddLog(new TextObject(
                "THE EIGHT — What was Calradia's six kingdoms and the Empire's three thrones has settled, since " +
                "the Long Night, into eight hard households, each holding a scatter of towns and asking a " +
                "different price for standing with them:\n" +
                "  · The WOLF BROTHERS (once Sturgia) — every sworn Kinsman answers to hunger before rank; " +
                "a hard, hungry pack that judges you by what you are willing to give up.\n" +
                "  · The TOWER (once Aserai) — a college of Warlocks who put the work before the title, and " +
                "the work before almost everything else.\n" +
                "  · The FOREST WIDOWS (once Battania) — led only by women, a Grand Widow at their head; a " +
                "sworn Widow answers to a sacrifice altar the rest of Calradia would rather not look at directly.\n" +
                "  · The BLOODBOUND (once Khuzait) — riders who trade in demon blood itself; a Bloodhunter is " +
                "gone again before dusk and is judged only by what he has killed.\n" +
                "  · The TEMPLE (once Vlandia) — warrior-monks of the Order; every Brother Templar keeps the " +
                "same vigil, whatever face wears the title this season.\n" +
                "  · The EMPIRE (once the Northern Empire) — an Emperor and his Legates still keep the old " +
                "rolls, ruling three cities as though they still ruled ninety.\n" +
                "  · LEGION (once the Western Empire) — Comrades who take their standing by what they seize, " +
                "not what they are given.\n" +
                "  · The CHOSEN (once the Southern Empire) — ruled by a PriestKing and his sworn Apostles, " +
                "faithful to a vision shown to the first of them on the walls, the night the sky tore."), true);

            AddLog(new TextObject(
                "COIN AND BARTER — Gold still passes hands, but it buys a tenth of what it once did, and a " +
                "prince's ransom is worth less than a sound horse. Most towns' markets run thin — traders poor, " +
                "food scarce, weapons crude and dear — and most honest dealing is goods for goods, favour for " +
                "favour, hand to hand. Raising a soldier to the sword-and-mail tiers now costs a horse, a proper " +
                "suit of armour, and a weapon worth carrying, not a purse of coin. Reward for good service, from " +
                "friend or foe alike, is more often food and iron than gold — remember that when a lord offers " +
                "you his thanks."), true);

            AddLog(new TextObject(
                "READING THE SIGNS — The Codex of the Inner Fire (Left Alt + L on the map) still teaches the " +
                "old element-craft to any mage who has not yet opened a spellbook of their own; once the book is " +
                "open, the same keys show you the book instead. Whatever else this world has taken from you, it " +
                "has not taken the ability to learn. Use it."), true);
        }
    }
}
