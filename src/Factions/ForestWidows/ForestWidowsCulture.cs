// =============================================================================
// THE DARKEST NIGHT — Factions/ForestWidows/ForestWidowsCulture.cs
//
// Battania becomes "The Forest Widows" — men failed to hold the Long Night
// back, so the women of the Battanian court seized power and bought peace by
// feeding the demons the men who could not save them. Mechanically identical
// to the Pale Widows (Southern Empire) — see Factions/PaleWidows/ — but
// retargeted to Battania's own kingdom and its own two seats.
//
// IMPORTANT — DIFFERENT RENAME TECHNIQUE FROM THE PALE WIDOWS. The three
// Empire successor kingdoms (empire / empire_w / empire_s) all share ONE
// CultureObject, so PaleWidowsCulture deliberately renames only the Kingdom
// object. Battania is NOT an Empire successor — it has always owned its own,
// unshared CultureObject ("battania"), the same way Sturgia and Aserai do.
// So this class follows the WolfBrothersCulture.cs / TowerCulture.cs pattern
// instead: it renames BOTH the Kingdom and the CultureObject, and wires a
// character-creation culture-card text override
// (ApplyForestWidowsCultureTexts) into both MagicSystem.OnGameInitialization-
// Finished and the pre-game EnsureTempleCultureTextPreGame slot, exactly as
// Tower/WolfBrothers/Bloodbound do — because with an unshared culture,
// Hero.Culture membership checks (not just MapFaction) are also meaningful
// here, unlike the Empire family.
//
// This SUPERSEDES the baseline AshAndEmber "Battania -> The Forest Clans"
// identity (see AshenCitySystem.Renaming.cs): both target the same
// "battania" culture/kingdom StringId, and a kingdom can only wear one name.
// The old RenameForestClansKingdom()/ApplyForestClansCultureTexts() helpers
// are left in place, unreferenced, for save compatibility — only the call
// site moves (see MagicSystem.cs and AshenCitySystem.Tick.cs).
//
// The ruler title is "Grand Widow"; the vassal title (borne only by the
// female lords who can actually hold the seat) is "Widow" everywhere the
// Forest Widows speak of rank (see ForestWidowsDialogue.cs). A man in this
// kingdom is never a lord in his own right — he is a husband, and dialogue
// never calls him anything grander.
// =============================================================================

using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal static class ForestWidowsCulture
    {
        // Battania's culture object and kingdom object share this StringId —
        // both are renamed through it (unlike the Pale Widows, which must
        // leave the shared Empire CultureObject alone; see header note).
        internal const string CultureId = "battania";

        internal const string RulerTitle  = "Grand Widow";
        internal const string VassalTitle = "Widow";

        private const string Lore =
            "The Forest Widows hold Marunath and Car Banseth the only way anything survives the Long Night — "
          + "by paying for it. When the men of the deep wood could not keep the dark from the boughs, their "
          + "wives, mothers, and daughters struck the bargain the men would not: a life, offered freely, buys "
          + "a season of quiet beneath the leaves. The court is a court of women now. A husband may love a "
          + "Widow, advise her, die for her — but he does not rule beside her, and everyone in Marunath knows why.";

        // Kingdom and CultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties (see WolfBrothersCulture.cs / the fuller
        // explanation in AshenCitySystem.Renaming.cs) — writing the base
        // _name field has no effect on either, so these backing-field
        // handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerForestWidow
        {
            get { try { return Hero.MainHero?.MapFaction?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsForestWidowLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        public static bool IsForestWidowParty(PartyBase party)
        {
            if (party == null) return false;
            try { return party.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        // Runs pre-campaign too so the character-creation culture card reads
        // "The Forest Widows" (mirrors WolfBrothersCulture.RenameWolfBrothersCulture).
        public static void RenameForestWidowsCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("The Forest Widows"));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameForestWidowsKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameForestWidowsCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Forest Widows"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Forest Widows"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameForestWidowsCulture();
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void SetKingdomField(Kingdom kingdom, string[] candidates, TextObject value)
        {
            foreach (var fieldName in candidates)
            {
                try
                {
                    var f = typeof(Kingdom).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) { f.SetValue(kingdom, value); return; }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static void SetKingdomEncyclopediaText(Kingdom kingdom, string lore)
        {
            if (kingdom == null || string.IsNullOrEmpty(lore)) return;
            SetKingdomField(kingdom,
                new[] { "<EncyclopediaText>k__BackingField", "_encyclopediaText" },
                new TextObject(lore));
        }

        // ── Character-creation culture card text override ───────────────────────
        // Same mechanism as WolfBrothersCulture.ApplyWolfBrothersCultureTexts /
        // TowerCulture.ApplyTowerCultureTexts: the culture-selection card reads
        // game-text variations keyed by the culture's StringId, not
        // CultureObject.Name, so those must be rewritten too. Called from
        // MainSubModule.OnGameInitializationFinished (post-campaign) AND from
        // the pre-game per-frame re-application slot, exactly like the other
        // own-culture identities — so it survives both a fresh load and a
        // save/load.
        public static bool ApplyForestWidowsCultureTexts()
        {
            try
            {
                RenameForestWidowsCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "The Forest Widows");
                SetCultureVariation(mgr, "str_culture_description", CultureId, Lore);
                SetCultureVariation(mgr, "str_faction_ruler", CultureId,        RulerTitle);
                SetCultureVariation(mgr, "str_faction_ruler", CultureId + "_f", RulerTitle);
                SetCultureVariation(mgr, "str_faction_ruler_name_with_title", CultureId,
                    RulerTitle + " {RULER.NAME}");
                SetCultureVariation(mgr, "str_faction_ruler_term_in_speech", CultureId,
                    RulerTitle + " {RULER.NAME}");
                return true;
            }
            catch { return false; }
        }

        private static void SetCultureVariation(GameTextManager mgr, string textId, string variation, string value)
        {
            try
            {
                var existing = GameTexts.FindText(textId, variation);
                if (existing != null && existing.ToString() == value) return; // already applied
                GameText gt = mgr.GetGameText(textId);
                if (gt == null) return;
                // Empty list, never null — see WolfBrothersCulture.SetCultureVariation.
                gt.SetVariationWithId(variation, new TextObject(value), new System.Collections.Generic.List<GameTextManager.ChoiceTag>());
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
