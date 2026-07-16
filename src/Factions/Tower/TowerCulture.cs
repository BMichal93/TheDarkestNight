// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerCulture.cs
//
// Aserai IS the Tower — scholars who read the Long Night as a text to be
// studied, not merely survived. Mirrors the exact rename technique
// WolfBrothersCulture.cs uses for Sturgia (Kingdom and BasicCultureObject
// both shadow MBObjectBase.Name with their own auto-property backing
// fields, so those backing fields — not the base _name field — must be
// written directly).
//
// This SUPERSEDES the baseline AshAndEmber "Aserai -> Duneborn" identity:
// both target the same "aserai" culture/kingdom StringId, and a kingdom can
// only wear one name. See MagicSystem.cs for the call-site swap (Duneborn
// renaming/dialogue call sites replaced with these). DunebornCulture.cs /
// DunebornDialogue.cs / the Duneborn rename helpers in
// AshenCitySystem.Renaming.cs are left in place, unreferenced, for
// save-compatibility per behaviour.md — only the call sites move.
// DunebornCulture.AltarCost (the Blood Tithe discount) is untouched: it
// keys off the "aserai" culture StringId directly, not the display name, so
// it keeps working exactly as before under the new name.
//
// The vassal title is "Warlock" everywhere the Tower speaks of rank (see
// TowerDialogue.cs); the ruler title is "Archmagister".
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
    internal static class TowerCulture
    {
        // Aserai's culture object and kingdom object share this StringId in
        // vanilla Bannerlord — both are renamed through it.
        internal const string CultureId = "aserai";

        internal const string RulerTitle  = "Archmagister";
        internal const string VassalTitle = "Warlock";

        private const string Lore =
            "The Tower does not mourn the sultanate it replaced. When the Long Night broke the Aserai line, "
          + "the scholars who kept Iyakis standing found that the old magic — the same Fire the rest of "
          + "Calradia only stumbles into by accident — answers a disciplined mind more readily than a "
          + "desperate one. They hold their one seat the way a library holds a single locked vault: fiercely, "
          + "and by design. A stranger who proves patient enough to be taught is bound to the order as "
          + "Warlock; one who is not is sent back into the dark with nothing.";

        // Kingdom and BasicCultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties (see AshenCitySystem.Renaming.cs for the
        // fuller explanation) — writing the base _name field has no effect on
        // either, so these backing-field handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerTower
        {
            get { try { return Hero.MainHero?.Culture?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsTowerLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        // Runs pre-campaign too so the character-creation culture card reads
        // "Tower" (mirrors WolfBrothersCulture.RenameWolfBrothersCulture).
        public static void RenameTowerCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("Tower"));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameTowerKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameTowerCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("Tower"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("Tower"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameTowerCulture();
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
        // Same mechanism as WolfBrothersCulture.ApplyWolfBrothersCultureTexts: the
        // culture-selection card reads game-text variations keyed by the
        // culture's StringId, not CultureObject.Name, so those must be rewritten
        // too. Called from MainSubModule.OnGameInitializationFinished (post-
        // campaign) AND from the pre-game per-frame re-application slot, exactly
        // like the other culture identities — so it survives both a fresh load
        // and a save/load.
        public static bool ApplyTowerCultureTexts()
        {
            try
            {
                RenameTowerCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "Tower");
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
