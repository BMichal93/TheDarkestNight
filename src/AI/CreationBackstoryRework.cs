// =============================================================================
// THE DARKEST NIGHT — AI/CreationBackstoryRework.cs
// Reworks Sandbox character-creation backstory options for the new fiction
// (Requirement 23: "Who you were before the Night").
//
//   Step 1 — Background: every culture/faction option is removed from the
//   selection stage save one — the Empire template, relabelled "I am a
//   survivor" (RestrictBackgroundToSurvivor). No other background, and so no
//   faction bonus tied to one, can ever be picked. Steps 2-4 (Family, Early
//   Childhood, Adolescence) are the Empire's own vanilla content and are left
//   exactly as they were; the old Khuzait/Vlandia/Sturgia-gated renames below
//   are consequently unreachable dead weight, kept only because deleting
//   working, harmless code buys nothing.
//
//   Step 5 — Youth: the six options the Empire background can actually reach
//   are re-themed from "drafted into an army" to "surviving the Long Night in
//   the city" (RewriteYouthMenu).
//
//   Step 6 — Young Adulthood ("narrative_adulthood_menu") is superseded
//   entirely by "The Keepsake": the vanilla "biggest achievement" framing
//   and its twelve occupation/register-gated options are replaced with one
//   fixed question — the one thing you carried out the door the day you
//   left home — and six always-visible keepsake options. See
//   CreationBackstoryRework.Keepsakes.cs for the menu rewrite, the six
//   options, and their pending-boon grants (ApplyPendingBoons calls into
//   ApplyKeepsakeBoon from that partial). This is a global rewrite (all
//   cultures, no gating) since Requirement 23 Step 1 already restricts every
//   campaign to the Empire background regardless.
//
// The vanilla backstory options live in the engine's generic
// CharacterCreationCampaignBehavior. We register as an
// ICharacterCreationContentHandler and rewrite the already-built narrative
// menus in AfterInitializeContent (the option's display text, visibility
// condition, and skill-effect getter are private/readonly — set by
// reflection, matching the reflection-light style of TempleCultureCardFixer).
//
// Special boons (Dark Gift, Grace, Magic + starting spells, age override)
// cannot be granted during character creation: the engine runs
// OnCharacterCreationFinalize *before* the OnCharacterCreationIsOver event,
// and our own new-game reset (CampaignBehavior.OnNewGameCreated →
// MageKnowledge.ResetForNewGame) fires on that event and would wipe them. So
// we only *record* the player's final pick at finalize, and apply the boon
// from OnNewGameCreated, after the reset has run (see ApplyPendingBoons).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal sealed partial class CreationBackstoryRework : CampaignBehaviorBase, ICharacterCreationContentHandler
    {
        // Engine-defined option ids for the two mechanically-changed options.
        private const string KhuzaitApostleOptionId = "khuzait_retainer_option";
        private const string VlandiaSquireOptionId  = "youth_groom_option";

        // Requirement 23, Step 1 — the sole surviving background.
        private const string EmpireCultureId = "empire";

        // The generic option grants (read from the live content so we stay in
        // sync with the engine's defaults rather than hard-coding 1/10/1).
        private static int _focus = 1, _skill = 10, _attr = 1;

        // The engine's ApplyFinalEffects calls Args.AffectedTraits.ToList() with no
        // null guard, so the reworked Apostle option (which grants no trait) must still
        // hand it a non-null list.
        private static readonly TraitObject[] _noTraits = new TraitObject[0];

        // Stored at AfterInitializeContent so OnStageCompleted can read the LIVE
        // selected culture (unknown when the menus are first built, before selection).
        private static CharacterCreationManager _manager;

        // Culture-flavoured renames of SHARED (cross-culture) narrative options. These
        // options appear for every culture, so their flavour must only show for the
        // matching culture — otherwise an Empire youth reads "the Tribe's emissary".
        // We capture each option's vanilla text/desc/args, then on every stage
        // transition restore vanilla or apply the flavour depending on the live pick.
        private sealed class GatedRename
        {
            public string MenuId, OptionId, Culture;
            public TextObject FlavText, FlavDesc, BaseText, BaseDesc;
            public GetNarrativeMenuOptionArgsDelegate FlavArgs, BaseArgs;
        }
        private static readonly List<GatedRename> _gated = new List<GatedRename>();

        private const BindingFlags FPriv = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags FPub  = BindingFlags.Instance | BindingFlags.Public;

        private static readonly FieldInfo TextField =
            typeof(NarrativeMenuOption).GetField("Text", FPub);
        private static readonly FieldInfo DescField =
            typeof(NarrativeMenuOption).GetField("DescriptionText", FPub);
        private static readonly FieldInfo ArgsGetterField =
            typeof(NarrativeMenuOption).GetField("_getNarrativeMenuOptionArgs", FPriv);
        private static readonly FieldInfo OnConditionField =
            typeof(NarrativeMenuOption).GetField("_onConditionInternal", FPriv);

        // A visibility condition that always passes — used to surface the two
        // Step-6 renames whose vanilla option is otherwise gated to a culture
        // that can no longer be picked (see ForceAlwaysVisible).
        private static readonly NarrativeMenuOptionOnConditionDelegate AlwaysVisible = _ => true;

        // ── CampaignBehaviorBase ─────────────────────────────────────────────

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, OnInitialized);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnInitialized(CharacterCreationManager manager)
        {
            // Clear any stale pending state from an abandoned creation this session.
            _pendingKeepsake        = KeepsakeId.None;
            _gated.Clear();
            _manager = null;
            try { manager.RegisterCharacterCreationContentHandler(this, 1000); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── ICharacterCreationContentHandler ─────────────────────────────────

        void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager m) { }

        void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager m)
        {
            try
            {
                var content = m.CharacterCreationContent;
                if (content != null)
                {
                    _focus = content.FocusToAdd;
                    _skill = content.SkillLevelToAdd;
                    _attr  = content.AttributeLevelToAdd;
                }
                _manager = m;
                RestrictBackgroundToSurvivor(m);   // Requirement 23, Step 1
                // Youth/Adulthood rewrites run BEFORE RewriteMenus: one Youth option
                // (youth_envoys_guard_first_option) is also touched by a pre-existing
                // Khuzait GatedRename below, which captures the option's CURRENT text
                // as its "vanilla" fallback the moment it registers. Renaming it here
                // first means that captured fallback is our new Empire-era text, not
                // the old vanilla text — and since Khuzait can never be the live
                // selection any more, the fallback is what always shows.
                RewriteYouthMenu(m);                // Requirement 23, Step 5
                RewriteKeepsakeMenu(m);             // "The Keepsake" — see .Keepsakes.cs
                RewriteMenus(m);
                ApplyGatedRenames();   // set initial state for the current (or no) selection
                // The Family/Adolescence/Youth stages now grant their normal
                // vanilla skill/focus/attribute bonuses again (mod-author directive:
                // backgrounds should reward the player as in the base game). Only the
                // FLAVOUR of each option is rewritten (RewriteMenus / RewriteYouthMenu
                // above); the option's own args getter — and thus its bonuses — is left
                // untouched. The Keepsake stage still adds its magic boon on top, exactly
                // as the base game's Young-Adulthood stage would have granted its own.
                // (NeutralizeMenu / NeutralArgs remain defined but unused, in case the
                // no-bonus behaviour is ever wanted again.)
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Culture is chosen partway through creation; re-evaluate the shared-option
        // flavour each time a stage completes so it tracks the player's live pick.
        void ICharacterCreationContentHandler.OnStageCompleted(CharacterCreationStageBase stage)
        {
            try { ApplyGatedRenames(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        void ICharacterCreationContentHandler.OnCharacterCreationFinalize(CharacterCreationManager m)
        {
            // Record the final pick only; the grant itself happens post-reset.
            // The Apostle/Squire options are SHARED across cultures (their flavour is
            // culture-gated), so their boons must be gated by culture too — otherwise an
            // Empire character who picks the "groom" option would receive Templar Grace.
            foreach (var pair in m.SelectedOptions)
            {
                try
                {
                    string id = pair.Value?.StringId;
                    if (string.IsNullOrEmpty(id)) continue;
                    if (KeepsakeOptionIds.TryGetValue(id, out var keepsake)) _pendingKeepsake = keepsake;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Menu rewrites ────────────────────────────────────────────────────

        private static void RewriteMenus(CharacterCreationManager m)
        {
            // Each Edit updates the option's label and its lore description. The
            // skill/attribute/trait effects shown in the dedicated effect panel come
            // from the option's args getter; for the two reworked options we replace
            // that getter so the panel reflects the new grant. Effects that the panel
            // cannot express (Grace, a Dark Gift) are noted in the description and
            // granted post-reset in ApplyPendingBoons.

            // ── Stage 1 — Family ─────────────────────────────────────────────
            // Khuzait: A noyan's kinsfolk → the Huntmaster's Blood-Sworn (Dark Gift for Polearm).
            Edit(m, "narrative_parent_menu", KhuzaitApostleOptionId,
                "The Huntmaster's Blood-Sworn",
                "Your family were sworn to the Huntmaster's inner rites — the marked few who carry his fire in "
                + "miniature and drink first from what the hunt brings down. You were raised among them, and the "
                + "dark took its measure of you before you were old enough to refuse it.");
            // Vlandia: A baron's retainers → Lower-rank Templars (same bonus).
            Edit(m, "narrative_parent_menu", "vlandia_retainer_option",
                "Lower-rank Templars",
                "Your father served in the lower ranks of the Templar order — a sworn man-at-arms who rode "
                + "under the banner and answered to the Lord Templars above him. He kept his oaths, drilled the "
                + "village levy, and fought as an armoured knight when the Order called.");
            // Vlandia: Mercenaries → Footmen (same bonus).
            Edit(m, "narrative_parent_menu", "vlandia_mercenary_option",
                "Footmen",
                "Your family marched as common footmen in the Templar host — spear and crossbow, paid in coin "
                + "and plunder, the rank and file who held the line while the knights broke it. Your mother "
                + "followed the column from siege to siege, and you grew up in the wake of its campaigns.");

            // ── Stage 3 — Adolescence ────────────────────────────────────────
            // These education/youth options are SHARED across cultures, so the flavour
            // is culture-gated (see RegisterGatedRename) — only the matching culture
            // sees it; everyone else keeps the vanilla wording.
            // Khuzait (urban): studied with your private tutor → learned the blood-lore.
            RegisterGatedRename(m, "narrative_education_menu", "education_tutor_option", "khuzait",
                "were schooled in the blood-lore.",
                "While other children worked the herds, you were sent to the Huntmaster's readers, who drilled "
                + "the anatomy of the risen, the reckoning of vials, and the disciplines of the hunt into you by "
                + "rote and by rod.");
            // Vlandia (urban): hung out with the gangs → denounced enemies of the faith.
            RegisterGatedRename(m, "narrative_education_menu", "education_ganger_option", "vlandia",
                "denounced enemies of the faith with your friends.",
                "You and your fellows made a sport of rooting out heresy in the back streets — naming the "
                + "lapsed, the foreign, and the merely unlucky to the Order's wardens. Some of it was zeal. "
                + "Some of it was knowing whom to threaten, and when.");

            // ── Stage 4 — Youth ──────────────────────────────────────────────
            // Khuzait: a chieftain's servant → a bloodrider's servant.
            RegisterGatedRename(m, "narrative_youth_menu", "youth_servant_first_option", "khuzait",
                "were a bloodrider's servant.",
                "You waited on one of the Huntmaster's bloodriders — his chosen lancers — fetching and scouting "
                + "and listening at the edges of councils you were never meant to hear.");
            // Khuzait: an envoy's entourage → the Bloodbound's emissary.
            RegisterGatedRename(m, "narrative_youth_menu", "youth_envoys_guard_first_option", "khuzait",
                "served as the Bloodbound's emissary.",
                "You rode ahead of the hunt, carrying the Huntmaster's terms to cities that still believed they "
                + "could bargain. You learned to read a room full of frightened men — and to be gone before the "
                + "knives came out.");
            // Vlandia: a baron's groom → a Lord Templar's squire (Grace + Honour for Charm).
            RegisterGatedRename(m, "narrative_youth_menu", VlandiaSquireOptionId, "vlandia",
                "served as a Lord Templar's squire.",
                "You served a Lord Templar as his squire — tending his arms and his horse, kneeling through the "
                + "long vigils, and learning that the Order's strength is bought with discipline and faith.");
        }

        // ── Requirement 23, Step 1 — the sole background ─────────────────────

        // Pre-selects the Empire ("I am a survivor") as the only background the
        // player ends up with, so "I am a survivor" is effectively the only pick.
        //
        // It deliberately does NOT strip the other cultures from
        // CharacterCreationContent._characterCreationCultures: the native
        // CharacterCreationCultureStageVM, when it builds, runs SortCultureList,
        // which calls .Single(c => c.CultureID.Contains("vlan"/"stur"/"empi"/
        // "aser"/"khuz")) over the built card list. An emptied pool makes those
        // Single() calls throw "Sequence contains no matching element", which
        // unwinds through LaunchSandboxCharacterCreation and wedges the whole
        // character-creation screen (the intro freezes, no new game can start).
        // So we leave the native pool whole — the native sort is happy — and the
        // single-background restriction is enforced cosmetically by
        // TempleCultureCardFixer, which removes every non-Empire card from the
        // live stage view-model AFTER that sort has already run.
        private static void RestrictBackgroundToSurvivor(CharacterCreationManager m)
        {
            try
            {
                var content = m?.CharacterCreationContent;
                if (content == null) return;

                CultureObject empire = null;
                try { empire = MBObjectManager.Instance?.GetObject<CultureObject>(EmpireCultureId); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (empire == null) return;   // Empire missing — leave creation alone rather than break it.

                try { content.SetSelectedCulture(empire, m); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Requirement 23, Step 5 — Youth ───────────────────────────────────
        // The Empire background's youth stage reaches exactly six vanilla
        // options (verified against TaleWorlds.CampaignSystem.dll's
        // CharacterCreationCampaignBehavior.AddYouthMenuOptions): the four
        // renamed here, "youth_guard_empire_register_option" ("stood guard with
        // a garrison") which is kept as-is per the brief, and youth_groom_option
        // (Vlandia-only, already unreachable — left untouched).
        private static void RewriteYouthMenu(CharacterCreationManager m)
        {
            const string menu = "narrative_youth_menu";

            Edit(m, menu, "youth_rider_high_register_option",
                "scavenged for food.",
                "The granaries never stretched far enough once the sun went down. You learned which stalls were "
                + "slack, which cellars still held a forgotten sack of grain, which bins the watch forgot to "
                + "lock — and how far a stranger's trust could be stretched for half a loaf.");
            Edit(m, menu, "youth_infantry_option",
                "trained to fight.",
                "Every hand old enough to hold a spear was put to drilling once the walls started mattering more "
                + "than the fields. You learned to hold a line, brace a shield, and not look at what came over "
                + "it after dark.");
            Edit(m, menu, "youth_skirmisher_option",
                "ran the night errands.",
                "You carried messages between wards after dark, when no one sensible walked the streets alone. "
                + "You learned the sound the city makes when it is holding its breath, and how to move through "
                + "it without becoming part of what it fears.");
            Edit(m, menu, "youth_envoys_guard_first_option",
                "served as a messenger.",
                "You carried word between the garrison, the wardens, and whatever remained of the old chains of "
                + "command — running the gauntlet of empty streets so that orders, warnings, and last words could "
                + "still reach the people who needed them.");
            Edit(m, menu, "youth_staff_first_option",
                "tended the ward-fires of a lord's hall.",
                "You kept the braziers lit and the oil topped along a lord's walls — a small, ceaseless labour, "
                + "but the fires held more than the eye could ever tell you. You learned which flames guttered "
                + "when something passed close in the dark, and which lords listened when you told them so.");
        }

        // Young Adulthood ("narrative_adulthood_menu") is rewritten as "The
        // Keepsake" — see RewriteKeepsakeMenu in CreationBackstoryRework.Keepsakes.cs.

        // Overrides an option's visibility condition so it always shows, for
        // renamed options whose vanilla condition is gated to a culture that
        // Requirement 23's single-background restriction (or the Keepsake's
        // global, no-gating rewrite) has made unreachable.
        private static void ForceAlwaysVisible(CharacterCreationManager m, string menuId, string optionId)
        {
            var o = Find(m, menuId, optionId);
            if (o == null) return;
            try { OnConditionField?.SetValue(o, AlwaysVisible); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Registers a culture-gated rename of a shared narrative option: captures the
        // vanilla text/description/args first, then ApplyGatedRenames swaps in the
        // flavour only while the matching culture is selected.
        private static void RegisterGatedRename(CharacterCreationManager m, string menuId, string optionId,
            string culture, string flavText, string flavDesc, GetNarrativeMenuOptionArgsDelegate flavArgs = null)
        {
            var o = Find(m, menuId, optionId);
            if (o == null) return;
            try
            {
                _gated.Add(new GatedRename
                {
                    MenuId = menuId, OptionId = optionId, Culture = culture,
                    FlavText = new TextObject(flavText), FlavDesc = new TextObject(flavDesc),
                    BaseText = TextField?.GetValue(o) as TextObject,
                    BaseDesc = DescField?.GetValue(o) as TextObject,
                    FlavArgs = flavArgs,
                    BaseArgs = ArgsGetterField?.GetValue(o) as GetNarrativeMenuOptionArgsDelegate,
                });
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Applies each gated rename's flavour when its culture is the live selection,
        // and restores the vanilla wording/args otherwise.
        private static void ApplyGatedRenames()
        {
            if (_manager == null || _gated.Count == 0) return;
            string sel = null;
            try { sel = _manager.CharacterCreationContent?.SelectedCulture?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            foreach (var gr in _gated)
            {
                var o = Find(_manager, gr.MenuId, gr.OptionId);
                if (o == null) continue;
                bool match = sel == gr.Culture;
                try { TextField?.SetValue(o, match ? gr.FlavText : gr.BaseText); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { DescField?.SetValue(o, match ? gr.FlavDesc : gr.BaseDesc); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (gr.FlavArgs != null)
                    try { ArgsGetterField?.SetValue(o, match ? gr.FlavArgs : gr.BaseArgs); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static NarrativeMenuOption Find(CharacterCreationManager m, string menuId, string optionId)
        {
            var menu = m.GetNarrativeMenuWithId(menuId);
            if (menu == null) return null;
            foreach (var o in menu.CharacterCreationMenuOptions)
                if (o != null && o.StringId == optionId) return o;
            return null;
        }

        // Updates an option's label and description; optionally replaces its skill-effect
        // getter (passed for the two reworked options, null for rename-only ones).
        private static void Edit(CharacterCreationManager m, string menuId, string optionId,
            string newText, string newDesc, GetNarrativeMenuOptionArgsDelegate argsGetter = null)
        {
            var o = Find(m, menuId, optionId);
            if (o == null) return;
            try { TextField?.SetValue(o, new TextObject(newText)); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { DescField?.SetValue(o, new TextObject(newDesc)); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (argsGetter != null)
                try { ArgsGetterField?.SetValue(o, argsGetter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Every option in Family/Childhood/Adolescence/Youth grants nothing — see
        // NeutralizeMenu below. The Keepsake stage is the sole source of bonuses.
        private static void NeutralArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[0]);
            args.SetAffectedTraits(_noTraits);   // non-null or ApplyFinalEffects throws
            args.SetFocusToSkills(0);
            args.SetLevelToSkills(0);
            // Level 0 rather than an unset attribute — EffectedAttribute defaults to
            // null if SetLevelToAttribute is never called, and the engine's effect
            // application does not appear to null-guard it.
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, 0);
        }

        // Overwrites every option's args getter in the given narrative menu with
        // NeutralArgs. Called once from AfterInitializeContent, after RewriteMenus/
        // RegisterGatedRename have run — ApplyGatedRenames only touches an option's
        // args getter when a gated rename explicitly supplies one (none currently
        // do), so this neutral getter is never overwritten afterward.
        private static void NeutralizeMenu(CharacterCreationManager m, string menuId)
        {
            var menu = m.GetNarrativeMenuWithId(menuId);
            if (menu == null) return;
            foreach (var o in menu.CharacterCreationMenuOptions)
            {
                if (o == null) continue;
                try { ArgsGetterField?.SetValue(o, new GetNarrativeMenuOptionArgsDelegate(NeutralArgs)); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Boon application ─────────────────────────────────────────────────

        // Called from CampaignBehavior.OnNewGameCreated, AFTER the new-game static
        // reset, so the grants survive into the campaign. Only the effects that the
        // creation effect panel cannot express live here — the Honour point is applied
        // through the squire's args (and so survives on the hero already).
        public static void ApplyPendingBoons()
        {
            // "The Keepsake" — see CreationBackstoryRework.Keepsakes.cs — is the ONLY
            // source of creation-time bonuses; every earlier stage is neutral (see
            // NeutralizeMenu in AfterInitializeContent).
            if (_pendingKeepsake != KeepsakeId.None)
            {
                var keepsake = _pendingKeepsake;
                _pendingKeepsake = KeepsakeId.None;
                ApplyKeepsakeBoon(keepsake);
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        KeepsakeConfirmationText(keepsake), new Color(0.75f, 0.65f, 0.4f)));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }
    }
}
