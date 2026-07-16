// =============================================================================
// THE DARKEST NIGHT — Factions/Empire/EmpireCulture.cs
//
// The Northern Empire IS the Empire — heirs of the imperial throne, keepers
// of the old rites, the last realm still governing as if Calradia might be
// put back together. Unlike Vlandia/Khuzait/Sturgia/Aserai/Battania, the
// baseline AshAndEmber mod never renamed any of the three Empire kingdoms —
// this is a fresh identity, not a supersession, so there is no retired call
// site to swap here.
//
// Mirrors the exact rename technique AshenCitySystem.Renaming.cs / the other
// Phase 7 factions already use (Kingdom and BasicCultureObject both shadow
// MBObjectBase.Name with their own auto-property backing fields, so those
// backing fields — not the base _name field — must be written directly).
//
// The vassal title is "Legate" everywhere the Empire speaks of rank (see
// EmpireDialogue.cs); the ruler title is "Emperor".
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
    internal static class EmpireCulture
    {
        // The Northern Empire's culture and kingdom objects share this
        // StringId in vanilla Bannerlord — both are renamed through it.
        internal const string CultureId = "empire";

        internal const string RulerTitle  = "Emperor";
        internal const string VassalTitle = "Legate";

        private const string Lore =
            "The Empire did not fall with the rest of Calradia — it simply shrank to what could still be "
          + "held. Saneopa, Diathma and Argoron keep the old rites: the census, the road-wardens, the "
          + "muster rolls, all of it copied out by hand as if the rest of the provinces might yet answer to "
          + "them again. A Legate serves not because the throne can still reach that far, but because "
          + "someone has to keep pretending it can, or nothing of the old world survives the Long Night at "
          + "all.";

        // Kingdom and BasicCultureObject SHADOW the base MBObjectBase.Name with
        // their own auto-properties — writing the base _name field has no
        // effect on either, so these backing-field handles are required.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cultureNameField =
            typeof(BasicCultureObject).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPlayerEmpire
        {
            get { try { return Hero.MainHero?.Culture?.StringId == CultureId; } catch { return false; } }
        }

        public static bool IsEmpireLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == CultureId; } catch { return false; }
        }

        // Actual KINGDOM membership (not culture — see LegionCulture's header note:
        // the three Empire successor kingdoms all share the "empire" CultureObject,
        // so a culture check cannot tell them apart). Used to gate content that
        // should require the player to actually stand with this kingdom right now,
        // not merely have been born under its banner.
        public static bool IsPlayerEmpireKingdom => IsEmpireLord(Hero.MainHero);

        // ── Culture-only rename (no Campaign required) ──────────────────────────
        public static void RenameEmpireCulture()
        {
            try
            {
                var culture = MBObjectManager.Instance?.GetObject<CultureObject>(CultureId);
                if (culture != null)
                    _cultureNameField?.SetValue(culture, new TextObject("The Empire"));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameEmpireKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == CultureId && !k.IsEliminated);
                if (kingdom == null) { RenameEmpireCulture(); return; }

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Empire"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Empire"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);

                RenameEmpireCulture();
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
        // Same mechanism as the other Phase 7 factions: the culture-selection
        // card reads game-text variations keyed by the culture's StringId, not
        // CultureObject.Name, so those must be rewritten too. Called from
        // MainSubModule.OnGameInitializationFinished (post-campaign) AND from
        // the pre-game per-frame re-application slot, so it survives both a
        // fresh load and a save/load.
        public static bool ApplyEmpireCultureTexts()
        {
            try
            {
                RenameEmpireCulture();

                var mgrField = typeof(GameTexts).GetField("_gameTextManager",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var mgr = mgrField?.GetValue(null) as GameTextManager;
                if (mgr == null) return false;

                SetCultureVariation(mgr, "str_culture_rich_name", CultureId, "The Empire");
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
