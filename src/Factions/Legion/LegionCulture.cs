// =============================================================================
// THE DARKEST NIGHT — Factions/Legion/LegionCulture.cs
//
// The Western Empire becomes "Legion" — militarists who steal rather than
// produce; might makes right. Like the Northern Empire (see
// Factions/Empire/EmpireCulture.cs), the baseline AshAndEmber mod never
// renamed any of the three Empire kingdoms — this is a fresh identity, not a
// supersession, so there is no retired call site to swap here.
//
// IMPORTANT — the three Empire successor kingdoms (empire / empire_w /
// empire_s) all point at the SAME CultureObject ("Culture.empire" in the
// shipped spkingdoms.xml). EmpireCulture.cs already owns that shared
// CultureObject's rename (culture-card text, "The Empire"). Legion must
// therefore rename ONLY the Kingdom object (StringId "empire_w") — never the
// CultureObject — or it would fight EmpireCulture.cs over the same text every
// tick. This also means culture-based checks (Hero.Culture) cannot tell the
// three Empire successor kingdoms apart; every Legion membership check below
// uses MAP FACTION (kingdom) membership instead, never CultureObject.
//
// Mirrors the exact rename technique EmpireCulture.cs / AshenCitySystem.
// Renaming.cs already use (Kingdom shadows MBObjectBase.Name with its own
// auto-property backing field, so that backing field — not the base _name
// field — must be written directly).
//
// The vassal title is "Comrade" everywhere the Legion speaks of rank (see
// LegionDialogue.cs); the ruler title is "Warlord".
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
    internal static class LegionCulture
    {
        // The Western Empire kingdom's StringId (confirmed against the shipped
        // SandBox/ModuleData/spkingdoms.xml — id="empire_w"). The CultureObject
        // it shares with the other two Empire kingdoms is "empire" and is
        // deliberately left untouched here (see the header note above).
        internal const string KingdomId = "empire_w";

        internal const string RulerTitle  = "Warlord";
        internal const string VassalTitle = "Comrade";

        private const string Lore =
            "Legion holds Ortysia and Lageta the only way it has ever held anything — by force, and by "
          + "keeping enough of it in reserve to take more. There is no harvest here worth the name; the "
          + "granaries fill from raid columns, not from ploughed fields. A Comrade does not ask permission "
          + "to march. Might makes right, and the Long Night has only sharpened the argument: a realm that "
          + "cannot take what it needs will simply be eaten by something that can.";

        // Kingdom SHADOWS the base MBObjectBase.Name with its own auto-property —
        // writing the base _name field has no effect — so this backing-field
        // handle is required. Shared with EmpireCulture's technique.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        // Membership must be checked by KINGDOM (MapFaction), never by
        // CultureObject — the three Empire successor kingdoms share one culture.
        public static bool IsPlayerLegion
        {
            get { try { return Hero.MainHero?.MapFaction?.StringId == KingdomId; } catch { return false; } }
        }

        public static bool IsLegionLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == KingdomId; } catch { return false; }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameLegionKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == KingdomId && !k.IsEliminated);
                if (kingdom == null) return;

                _kingdomNameField?.SetValue(kingdom, new TextObject("Legion"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("Legion"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);
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
    }
}
