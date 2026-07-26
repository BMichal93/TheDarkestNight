// =============================================================================
// THE DARKEST NIGHT — Factions/Bloodbound/BloodboundCulture.cs
//
// Khuzait IS the Bloodbound — riders who traded the horse-lord's endless
// steppe for a narrower, grimmer purpose: they hunt what rose from below and
// keep its blood as both trophy and tool. Mirrors the exact rename technique
// HiveCulture.cs / TowerCulture.cs / WolfBrothersCulture.cs use (Kingdom and
// BasicCultureObject both shadow MBObjectBase.Name with their own
// auto-property backing fields, so those backing fields — not the base
// _name field — must be written directly).
//
// This SUPERSEDES the baseline AshAndEmber "Khuzait -> Tribes" identity (see
// TribalCulture.cs / AshenCitySystem.ApplyTribalCultureTexts): both target the
// same "khuzait" culture/kingdom StringId, and a kingdom can only wear one
// name. See MagicSystem.cs for the call-site swap (Tribes renaming/dialogue
// call sites replaced with these). TribalCulture.cs / TribesDialogue.cs are
// left in place, unreferenced, for save-compatibility per behaviour.md —
// only the call sites move.
//
// The vassal title is "Bloodhunter" everywhere the Bloodbound speak of rank;
// the ruler title is "Huntmaster". See BloodboundDialogue.cs for the sweep.
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
    internal static class BloodboundCulture
    {
        // Khuzait's culture object and kingdom object share this StringId in
        // vanilla Bannerlord — both are renamed through it.
        internal const string CultureId = "khuzait";

        internal const string RulerTitle  = "Huntmaster";
        internal const string VassalTitle = "Bloodhunter";

        private const string Lore =
            "The Bloodbound do not chase glory or grazing rights anymore — the steppe emptied the night the "
          + "dark came up through it. What the clans kept was the hunt: they ride out from Akkalat and Chaikand "
          + "after every dusk to cut down whatever the Long Night sent up, and they do not leave the kill without "
          + "taking its blood. Drunk, spilled, or bartered, that blood is the only coin the Bloodbound still trust "
          + "— it does not lie about what a rider is willing to face.";

        // Kingdom and BasicCultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties (see AshenCitySystem.Renaming.cs for the
        // fuller explanation) — writing the base _name field has no effect on
        // either, so these backing-field handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerBloodbound
        {
            get { try { return Hero.MainHero?.Culture?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsBloodboundLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        internal static bool IsBloodboundParty(TaleWorlds.CampaignSystem.Party.PartyBase party)
        {
            if (party == null) return false;
            try { return party.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        // Runs pre-campaign too so the character-creation culture card reads
        // "The Bloodbound" (mirrors HiveCulture.RenameHiveCulture).
        public static void RenameBloodboundCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("The Bloodbound"));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameBloodboundKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameBloodboundCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Bloodbound"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Bloodbound"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameBloodboundCulture();
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
        // Same mechanism as HiveCulture.ApplyHiveCultureTexts / TowerCulture's
        // equivalent: the culture-selection card reads game-text variations keyed
        // by the culture's StringId, not CultureObject.Name, so those must be
        // rewritten too. Called from MainSubModule.OnGameInitializationFinished
        // (post-campaign) AND from the pre-game per-frame re-application slot,
        // exactly like the other culture identities — so it survives both a
        // fresh load and a save/load.
        public static bool ApplyBloodboundCultureTexts()
        {
            try
            {
                RenameBloodboundCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "The Bloodbound");
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
