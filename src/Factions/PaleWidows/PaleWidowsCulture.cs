// =============================================================================
// THE DARKEST NIGHT — Factions/PaleWidows/PaleWidowsCulture.cs
//
// The Southern Empire becomes "The Pale Widows" — men failed to hold the Long
// Night back, so the women of the Southern court seized power and bought
// peace by feeding the demons the men who could not save them. Like the
// Northern and Western Empire renames (see Factions/Empire/EmpireCulture.cs
// and Factions/Legion/LegionCulture.cs), the baseline AshAndEmber mod never
// renamed any of the three Empire kingdoms — this is a fresh identity, not a
// supersession, so there is no retired call site to swap here.
//
// IMPORTANT — the three Empire successor kingdoms (empire / empire_w /
// empire_s) all point at the SAME CultureObject ("Culture.empire" in the
// shipped spkingdoms.xml). EmpireCulture.cs already owns that shared
// CultureObject's rename (culture-card text, "The Empire"). The Pale Widows
// must therefore rename ONLY the Kingdom object (StringId "empire_s") —
// never the CultureObject — or it would fight EmpireCulture.cs over the same
// text every tick. This also means culture-based checks (Hero.Culture)
// cannot tell the three Empire successor kingdoms apart; every Pale Widows
// membership check below uses MAP FACTION (kingdom) membership instead,
// never CultureObject.
//
// Mirrors the exact rename technique EmpireCulture.cs / LegionCulture.cs /
// AshenCitySystem.Renaming.cs already use (Kingdom shadows MBObjectBase.Name
// with its own auto-property backing field, so that backing field — not the
// base _name field — must be written directly).
//
// The ruler title is "Grand Widow"; the vassal title (borne only by the
// female lords who can actually hold the seat) is "Widow" everywhere the
// Pale Widows speak of rank (see PaleWidowsDialogue.cs). A man in this
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

namespace AshAndEmber
{
    internal static class PaleWidowsCulture
    {
        // The Southern Empire kingdom's StringId (confirmed against the shipped
        // SandBox/ModuleData/spkingdoms.xml — id="empire_s"). The CultureObject
        // it shares with the other two Empire kingdoms is "empire" and is
        // deliberately left untouched here (see the header note above).
        internal const string KingdomId = "empire_s";

        internal const string RulerTitle  = "Grand Widow";
        internal const string VassalTitle = "Widow";

        private const string Lore =
            "The Pale Widows hold Phycaon and Lycaron the only way anything survives the Long Night — by "
          + "paying for it. When the men of the Southern court could not keep the dark from the walls, their "
          + "wives, mothers, and daughters struck the bargain the men would not: a life, offered freely, buys "
          + "a season of quiet. The court is a court of women now. A husband may love a Widow, advise her, "
          + "die for her — but he does not rule beside her, and everyone in Phycaon knows why.";

        // Kingdom SHADOWS the base MBObjectBase.Name with its own auto-property —
        // writing the base _name field has no effect — so this backing-field
        // handle is required. Shared with EmpireCulture's / LegionCulture's
        // technique.
        private static readonly FieldInfo _kingdomNameField =
            typeof(Kingdom).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        // Membership must be checked by KINGDOM (MapFaction), never by
        // CultureObject — the three Empire successor kingdoms share one culture.
        public static bool IsPlayerPaleWidow
        {
            get { try { return Hero.MainHero?.MapFaction?.StringId == KingdomId; } catch { return false; } }
        }

        public static bool IsPaleWidowLord(Hero hero)
        {
            if (hero == null) return false;
            try { return hero.MapFaction?.StringId == KingdomId; } catch { return false; }
        }

        public static bool IsPaleWidowParty(PartyBase party)
        {
            if (party == null) return false;
            try { return party.MapFaction?.StringId == KingdomId; } catch { return false; }
        }

        // ── Kingdom rename — call once per session's first daily tick ──────────
        // Kingdom display names revert to their XML values on every session load.
        public static void RenamePaleWidowsKingdom()
        {
            try
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == KingdomId && !k.IsEliminated);
                if (kingdom == null) return;

                _kingdomNameField?.SetValue(kingdom, new TextObject("The Pale Widows"));
                SetKingdomField(kingdom,
                    new[] { "_informalName", "<InformalName>k__BackingField" },
                    new TextObject("The Pale Widows"));
                SetKingdomField(kingdom,
                    new[] { "<EncyclopediaRulerTitle>k__BackingField", "_rulerTitle", "<RulerTitle>k__BackingField" },
                    new TextObject(RulerTitle));
                SetKingdomEncyclopediaText(kingdom, Lore);
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
    }
}
