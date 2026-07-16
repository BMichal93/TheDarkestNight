// =============================================================================
// THE DARKEST NIGHT — Factions/WolfBrothers/WolfBrothersCulture.cs
//
// Sturgia IS the Wolf Brothers — survival of the fittest over the shape of the
// old world. Mirrors the exact rename technique AshenCitySystem.Renaming.cs
// already uses for the Holy Temple / Tribes / (old) Northmen / Duneborn /
// Forest Clans identities (Kingdom and BasicCultureObject both shadow
// MBObjectBase.Name with their own auto-property backing fields, so those
// backing fields — not the base _name field — must be written directly).
//
// This SUPERSEDES the baseline AshAndEmber "Sturgia -> Northmen" identity:
// both target the same "sturgia" culture/kingdom StringId, and a kingdom can
// only wear one name. See MagicSystem.cs and AshenCitySystem.Tick.cs for the
// call-site swap (Northmen renaming/dialogue call sites replaced with these).
// NorthmenCulture-adjacent code (NorthmenDialogue.cs, the Northmen rename
// helpers in AshenCitySystem.Renaming.cs) is left in place, unreferenced, for
// save-compatibility per behaviour.md — only the call sites move.
//
// The vassal title is "Kinsman" everywhere the Wolf Brothers speak of rank
// (see WolfBrothersDialogue.cs); the ruler title is "Packmaster".
// =============================================================================

using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal static class WolfBrothersCulture
    {
        // Sturgia's culture object and kingdom object share this StringId in
        // vanilla Bannerlord — both are renamed through it.
        internal const string CultureId = "sturgia";

        internal const string RulerTitle  = "Packmaster";
        internal const string VassalTitle = "Kinsman";

        private const string Lore =
            "The Wolf Brothers do not pretend the old world still stands. When the Long Night came for the "
          + "Sturgian line, the folk who survived it stopped burying their dead in soft ground and started "
          + "eating what would not go to waste. They hold Tyal and Sibir the way a wolf holds a kill: by "
          + "tooth, not by law. A stranger is fed once, judged once, and either bound to the pack as "
          + "Kinsman or put out into the cold to take their chances with what hunts there.";

        // Kingdom and BasicCultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties (see AshenCitySystem.Renaming.cs for the
        // fuller explanation) — writing the base _name field has no effect on
        // either, so these backing-field handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerWolfBrother
        {
            get { try { return Hero.MainHero?.Culture?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsWolfBrotherLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        // Runs pre-campaign too so the character-creation culture card reads
        // "Wolf Brothers" (mirrors TempleCulture.RenameTempleCulture).
        public static void RenameWolfBrothersCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("Wolf Brothers"));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameWolfBrothersKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameWolfBrothersCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("Wolf Brothers"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("Wolf Brothers"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameWolfBrothersCulture();
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
        // Same mechanism as AshenCitySystem.ApplyTempleCultureTexts / ApplyNorthmenCultureTexts:
        // the culture-selection card reads game-text variations keyed by the
        // culture's StringId, not CultureObject.Name, so those must be rewritten too.
        // Called from MainSubModule.OnGameInitializationFinished (post-campaign) AND
        // from the pre-game per-frame re-application slot, exactly like the other
        // culture identities — so it survives both a fresh load and a save/load.
        public static bool ApplyWolfBrothersCultureTexts()
        {
            try
            {
                RenameWolfBrothersCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "Wolf Brothers");
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
                gt.SetVariationWithId(variation, new TextObject(value), null);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
