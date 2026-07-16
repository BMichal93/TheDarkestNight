// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenCulture.cs
//
// The Southern Empire becomes "The Chosen" — the first PriestKing had a
// vision of an angelic creature that blessed his line and promised salvation
// from the demons. His followers believe the PriestKing's blood is divine,
// and every Apostle who kneels to him serves that belief, not merely a
// crown. Like the Northern and Western Empire renames (see
// Factions/Empire/EmpireCulture.cs and Factions/Legion/LegionCulture.cs),
// the baseline AshAndEmber mod never renamed any of the three Empire
// kingdoms — this is a fresh identity, replacing the (deleted) Pale Widows,
// not a supersession of anything still live.
//
// IMPORTANT — the three Empire successor kingdoms (empire / empire_w /
// empire_s) all point at the SAME CultureObject ("Culture.empire" in the
// shipped spkingdoms.xml). EmpireCulture.cs already owns that shared
// CultureObject's rename (culture-card text, "The Empire"). The Chosen must
// therefore rename ONLY the Kingdom object (StringId "empire_s") — never the
// CultureObject — or it would fight EmpireCulture.cs over the same text
// every tick. This also means culture-based checks (Hero.Culture) cannot
// tell the three Empire successor kingdoms apart; every Chosen membership
// check below uses MAP FACTION (kingdom) membership instead, never
// CultureObject.
//
// Mirrors the exact rename technique EmpireCulture.cs / LegionCulture.cs /
// AshenCitySystem.Renaming.cs already use (Kingdom shadows MBObjectBase.Name
// with its own auto-property backing field, so that backing field — not the
// base _name field — must be written directly).
//
// The ruler title is "PriestKing"; the vassal title is "Apostle" — every
// Chosen lord is one of the Chosen, sworn to a king they believe was chosen
// by Heaven itself.
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
    internal static class ChosenCulture
    {
        // The Southern Empire kingdom's StringId (confirmed against the shipped
        // SandBox/ModuleData/spkingdoms.xml — id="empire_s"). The CultureObject
        // it shares with the other two Empire kingdoms is "empire" and is
        // deliberately left untouched here (see the header note above).
        internal const string KingdomId = "empire_s";

        internal const string RulerTitle  = "PriestKing";
        internal const string VassalTitle = "Apostle";

        private const string Lore =
            "The first PriestKing knelt alone on the walls of Phycaon in the worst hour of the Long Night and rose "
          + "with a vision burned into him: a creature of light and fire, vast as the sky, that named his line "
          + "chosen and swore the demons would not have him while his blood still ruled. Whether it was Heaven, or "
          + "something older wearing Heaven's face, no Apostle now living dares ask aloud. The Chosen do not vote on "
          + "what the PriestKing decides — his word is not a policy, it is a promise made on their behalf, and every "
          + "wife brought to his hall from a conquered town is one more oath the dark cannot break.";

        // Kingdom SHADOWS the base MBObjectBase.Name with its own auto-property —
        // writing the base _name field has no effect — so this backing-field
        // handle is required. Shared with EmpireCulture's / LegionCulture's
        // technique.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        // Membership must be checked by KINGDOM (MapFaction), never by
        // CultureObject — the three Empire successor kingdoms share one culture.
        public static bool IsPlayerChosen
        {
            get { try { return Hero.MainHero?.MapFaction?.StringId == KingdomId; } catch { return false; } }
        }

        public static bool IsChosenLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == KingdomId; } catch { return false; }
        }

        public static bool IsChosenParty(PartyBase party)
        {
            if (party == null) return false;
            try { return party.MapFaction?.StringId == KingdomId; } catch { return false; }
        }

        public static Kingdom GetChosenKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k.StringId == KingdomId && !k.IsEliminated); }
            catch { return null; }
        }

        public static bool IsPriestKing(Hero hero)
        {
            if (hero == null) return false;
            try { return GetChosenKingdom()?.Leader == hero; } catch { return false; }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenameChosenKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == KingdomId && !k.IsEliminated);
                if (kingdom == null) return;

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Chosen"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Chosen"));
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
