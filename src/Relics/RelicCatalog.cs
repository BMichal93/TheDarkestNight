// =============================================================================
// THE DARKEST NIGHT — Relics/RelicCatalog.cs
//
// Static definitions for the relics — rare magical items assembled from
// Crystal and Dark Gift effects, rebalanced weaker per RelicMath (a relic
// never breaks, unlike a Crystal, and is per-item rather than per-soul,
// unlike a Dark Gift). Pure data; no TaleWorlds types.
//
// Every relic's visual base is an EXISTING, already-proven item template —
// the exact one CrystalCatalog's items.xml block already uses (vanilla
// "throwing_stone" mesh on a "bo_mace_a" collision body, wired as a simple
// one-handed weapon). That template is known not to fault the moment it is
// equipped (see the header comment in ModuleData/items.xml); relics reuse it
// byte-for-byte rather than risking an unverified mesh/body combination, and
// each entry documents which real ModuleData/items.xml row it was cloned
// from. See ModuleData/items.xml for the matching <Item id="aae_relic_*">
// blocks.
//
// Names are generated once at static-init time via RelicNaming, seeded from
// a fixed per-relic constant so the name is stable across every load of the
// same build (not re-rolled per session) — exactly like a real named item.
// =============================================================================

using System.Collections.Generic;
using System.Linq;

namespace AshAndEmber
{
    public enum RelicEffectSource { Crystal = 0, DarkGift = 1 }

    public struct RelicEffectRef
    {
        public RelicEffectSource Source;
        public CrystalType       CrystalEffect;   // valid when Source == Crystal
        public DarkGiftId        DarkGiftEffect;   // valid when Source == DarkGift
    }

    public enum RelicId
    {
        Cinderfang        = 0, // Embershard, weaker
        WidowsPatience    = 1, // Rimeshard, weaker
        WardensBulwark    = 2, // Aegisstone, weaker
        TheLastDraught    = 3, // Bloodstone, weaker
        GaleboundCharm    = 4, // Zephyrglass, weaker
        GravebiteBrand    = 5, // DarkStrike, weaker
        HollowsMercy      = 6, // IronVeil, weaker
        WraithkissSigil   = 7, // SoulDrain, weaker
        MartyrsRefusal    = 8, // BloodPact, weaker
        DuskwardBell      = 9, // DreadPresence, weaker
    }

    public struct RelicDef
    {
        public RelicId               Id;
        public string                ItemId;         // matches ModuleData/items.xml id attribute
        public string                BaseItemNote;    // which existing/proven item template this is modelled on
        public string                Name;            // RelicNaming-generated
        public List<RelicEffectRef>  Effects;         // one or two effects
        public ColorSchool           GlowColor;
        public string                Lore;
    }

    public static class RelicCatalog
    {
        // Fixed per-relic seeds — never change these once shipped, or a
        // relic's generated name would drift between builds even though
        // nothing about the relic itself changed.
        private static string Named(int seed) => RelicNaming.Generate(seed);

        private static readonly List<RelicDef> _defs = new List<RelicDef>
        {
            new RelicDef
            {
                Id           = RelicId.Cinderfang,
                ItemId       = "aae_relic_cinderfang",
                BaseItemNote = "Cloned from aae_embershard's item template (vanilla throwing_stone mesh / bo_mace_a body).",
                Name         = Named(10001),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.Crystal, CrystalEffect = CrystalType.Embershard }
                },
                GlowColor = ColorSchool.Red,
                Lore = "A shard of Embershard's own lattice, carved thin enough to never fracture. "
                     + "It cannot hold the whole burst its parent stone once did — only an ember's worth, "
                     + "loosed again and again, for as long as the hand that bears it keeps swinging.",
            },
            new RelicDef
            {
                Id           = RelicId.WidowsPatience,
                ItemId       = "aae_relic_widows_patience",
                BaseItemNote = "Cloned from aae_rimeshard's item template (vanilla throwing_stone mesh / bo_mace_a body).",
                Name         = Named(10002),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.Crystal, CrystalEffect = CrystalType.Rimeshard }
                },
                GlowColor = ColorSchool.Blue,
                Lore = "Cold enough to slow a step, never cold enough to stop a heart — the mercy Rimeshard "
                     + "withheld from its own bearer, kept back here on purpose.",
            },
            new RelicDef
            {
                Id           = RelicId.WardensBulwark,
                ItemId       = "aae_relic_wardens_bulwark",
                BaseItemNote = "Cloned from aae_aegisstone's item template (vanilla throwing_stone mesh / bo_mace_a body).",
                Name         = Named(10003),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.Crystal, CrystalEffect = CrystalType.Aegisstone }
                },
                GlowColor = ColorSchool.White,
                Lore = "A watchman's keepsake, worn smooth by a thumb that never stopped turning it. "
                     + "It will not save you twice as hard as the stone it copied — only as often as you need it.",
            },
            new RelicDef
            {
                Id           = RelicId.TheLastDraught,
                ItemId       = "aae_relic_the_last_draught",
                BaseItemNote = "Cloned from aae_bloodstone's item template (vanilla throwing_stone mesh / bo_mace_a body).",
                Name         = Named(10004),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.Crystal, CrystalEffect = CrystalType.Bloodstone }
                },
                GlowColor = ColorSchool.Red,
                Lore = "It does not spend itself trading blood for blood the way Bloodstone does — "
                     + "it sips, carefully, so there is always a little more to give back tomorrow.",
            },
            new RelicDef
            {
                Id           = RelicId.GaleboundCharm,
                ItemId       = "aae_relic_galebound_charm",
                BaseItemNote = "Cloned from aae_zephyrglass's item template (vanilla throwing_stone mesh / bo_mace_a body).",
                Name         = Named(10005),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.Crystal, CrystalEffect = CrystalType.Zephyrglass }
                },
                GlowColor = ColorSchool.Yellow,
                Lore = "The lightest of the lattices, halved again — a hurry that never quite catches up "
                     + "to the wind it was cut from, but is glad enough to keep chasing it.",
            },
            new RelicDef
            {
                Id           = RelicId.GravebiteBrand,
                ItemId       = "aae_relic_gravebite_brand",
                BaseItemNote = "Cloned from aae_embershard's item template (vanilla throwing_stone mesh / bo_mace_a body) — carries a Dark Gift effect, not the Embershard effect.",
                Name         = Named(10006),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.DarkGift, DarkGiftEffect = DarkGiftId.DarkStrike }
                },
                GlowColor = ColorSchool.Red,
                Lore = "Every gift has a shadow it casts. This is DarkStrike's — dimmer, gifted to a blade "
                     + "instead of a soul, so that whoever wields it borrows the dark rather than owning it.",
            },
            new RelicDef
            {
                Id           = RelicId.HollowsMercy,
                ItemId       = "aae_relic_hollows_mercy",
                BaseItemNote = "Cloned from aae_rimeshard's item template (vanilla throwing_stone mesh / bo_mace_a body) — carries a Dark Gift effect, not the Rimeshard effect.",
                Name         = Named(10007),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.DarkGift, DarkGiftEffect = DarkGiftId.IronVeil }
                },
                GlowColor = ColorSchool.Purple,
                Lore = "IronVeil's shadow, thinned to a whisper of the ward it copies. It gives back only "
                     + "a sliver of what the blow took — enough to notice, never enough to trust.",
            },
            new RelicDef
            {
                Id           = RelicId.WraithkissSigil,
                ItemId       = "aae_relic_wraithkiss_sigil",
                BaseItemNote = "Cloned from aae_veilstone's item template (vanilla throwing_stone mesh / bo_mace_a body) — carries a Dark Gift effect, not the Veilstone effect.",
                Name         = Named(10008),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.DarkGift, DarkGiftEffect = DarkGiftId.SoulDrain }
                },
                GlowColor = ColorSchool.Purple,
                Lore = "SoulDrain's shadow — it does not hollow a whole battle line, only the nerve of the "
                     + "one man unlucky enough to be standing closest when the blade lands.",
            },
            new RelicDef
            {
                Id           = RelicId.MartyrsRefusal,
                ItemId       = "aae_relic_martyrs_refusal",
                BaseItemNote = "Cloned from aae_bloodstone's item template (vanilla throwing_stone mesh / bo_mace_a body) — carries a Dark Gift effect, not the Bloodstone effect.",
                Name         = Named(10009),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.DarkGift, DarkGiftEffect = DarkGiftId.BloodPact }
                },
                GlowColor = ColorSchool.Red,
                Lore = "BloodPact's shadow, cut down to a rumour of the debt it once collected on every kill. "
                     + "Enough to keep a fight going. Never enough to make death feel like a bargain.",
            },
            new RelicDef
            {
                Id           = RelicId.DuskwardBell,
                ItemId       = "aae_relic_duskward_bell",
                BaseItemNote = "Cloned from aae_duskstone's item template (vanilla throwing_stone mesh / bo_mace_a body) — carries a Dark Gift effect, not the Duskstone effect.",
                Name         = Named(10010),
                Effects      = new List<RelicEffectRef> {
                    new RelicEffectRef { Source = RelicEffectSource.DarkGift, DarkGiftEffect = DarkGiftId.DreadPresence }
                },
                GlowColor = ColorSchool.Ashen,
                Lore = "DreadPresence's shadow. It cannot break a whole line's nerve the way the gift does — "
                     + "only unsettle whoever stands close enough to hear whatever it is the bearer hears.",
            },
        };

        public static IReadOnlyList<RelicDef> All => _defs;

        public static bool TryGet(RelicId id, out RelicDef def)
        {
            foreach (var d in _defs)
                if (d.Id == id) { def = d; return true; }
            def = default;
            return false;
        }

        public static bool TryGetByItemId(string itemId, out RelicDef def)
        {
            def = default;
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var d in _defs)
                if (d.ItemId == itemId) { def = d; return true; }
            return false;
        }

        public static bool IsRelicItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            foreach (var d in _defs)
                if (d.ItemId == itemId) return true;
            return false;
        }

        public static string[] AllItemIds()
        {
            var ids = new string[_defs.Count];
            for (int i = 0; i < _defs.Count; i++) ids[i] = _defs[i].ItemId;
            return ids;
        }
    }
}
