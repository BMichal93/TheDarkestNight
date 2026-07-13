// =============================================================================
// THE DARKEST NIGHT — Factions/Hive/HiveCulture.cs
//
// Battania IS the Hive — a fungal network that bound the last Battanian
// clans into one nervous system when the Long Night broke the forests open.
// Mirrors the exact rename technique WolfBrothersCulture.cs / TowerCulture.cs
// use (Kingdom and BasicCultureObject both shadow MBObjectBase.Name with
// their own auto-property backing fields, so those backing fields — not the
// base _name field — must be written directly).
//
// This SUPERSEDES the baseline AshAndEmber "Battania -> The Forest Clans"
// identity: both target the same "battania" culture/kingdom StringId, and a
// kingdom can only wear one name. See MagicSystem.cs for the call-site swap
// (Forest Clans renaming/dialogue call sites replaced with these).
// ForestClansCulture.cs / the Forest Clans rename helpers in
// AshenCitySystem.Renaming.cs are left in place, unreferenced, for
// save-compatibility per behaviour.md — only the call sites move.
//
// The vassal title is "Integrated" everywhere the Hive speaks of rank (see
// HiveDialogue.cs); the ruler title is "First Root". Hive lords always speak
// in the plural — see HiveDialogue.cs's line pools.
// =============================================================================

using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    internal static class HiveCulture
    {
        // Battania's culture object and kingdom object share this StringId in
        // vanilla Bannerlord — both are renamed through it.
        internal const string CultureId = "battania";

        internal const string RulerTitle  = "First Root";
        internal const string VassalTitle = "Integrated";

        private const string Lore =
            "The Hive does not remember the clans it used to be. When the Long Night tore the forest floor "
          + "open, something older than the Battanian kings answered — a patient fungus that offered its hosts "
          + "one mind instead of many frightened ones. We held Marunath and Car Banseth because the network "
          + "could not spread past where its spores could still find soil to hide the roots in. A stranger who "
          + "drinks the elixir is Integrated; the network does not let go easily, but it does let go — the "
          + "dose simply has to stop.";

        // Kingdom and BasicCultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties (see AshenCitySystem.Renaming.cs for the
        // fuller explanation) — writing the base _name field has no effect on
        // either, so these backing-field handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerHive
        {
            get { try { return Hero.MainHero?.Culture?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsHiveLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        // Runs pre-campaign too so the character-creation culture card reads
        // "The Hive" (mirrors WolfBrothersCulture.RenameWolfBrothersCulture).
        public static void RenameHiveCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("The Hive"));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameHiveKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameHiveCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Hive"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Hive"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameHiveCulture();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
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
        // MainSubModule.OnGameInitializationFinished (post-campaign) AND from the
        // pre-game per-frame re-application slot, exactly like the other culture
        // identities — so it survives both a fresh load and a save/load.
        public static bool ApplyHiveCultureTexts()
        {
            try
            {
                RenameHiveCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "The Hive");
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
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
