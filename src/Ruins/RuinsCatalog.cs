// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsCatalog.cs
//
// Phase 9 (Requirement 12) — "the dead castles of the old world are the
// dungeons of the new." This is a PARALLEL catalog to AshAndEmber's own
// src/AshenRuins/AshenRuinDefs.cs, not an extension of it: AshenRuinDefs is
// wired end-to-end to the Ashen mystic register (Whisper tiers, the Ashen
// alignment, grimoire fragments, named villages matched one-for-one to a
// bespoke RuinDef) and to a purely-inquiry-chain exploration loop with no
// waiting at all. Phase 9 needs a different register entirely — crumbled
// halls of dead lords, not standing stones of a cold god — AND a genuinely
// different mechanic (Scouting-timed waiting between chambers, nightfall
// risk). Rather than bend AshenRuinDefs/AshenRuinSystem to carry both, this
// folder copies the TECHNIQUE (a pure enum+data-row catalog feeding a
// campaign-side dispatcher) and writes its own content and its own loop.
//
// Chambers here are a re-usable POOL, not one-per-settlement: every ruined
// castle draws a short, deterministic sequence from this pool (see
// RuinsMath.ChamberSequence), so the ~80% of castles that become ruins don't
// need one bespoke entry apiece the way AshenRuinDefs' 34 named ruins did.
// =============================================================================

using System.Collections.Generic;
using System.Linq;

namespace AshAndEmber
{
    // The old civilization's remains, room by room — collapsed stonework,
    // drowned cellars, a lord's last door, a chapel nobody prays in anymore.
    public enum ChamberType
    {
        CollapsedHall,       // the roof came down; the hall did not
        FloodedCellar,       // the undercroft, ankle- to waist-deep
        SealedBedchamber,    // a lord's door, barred from the inside
        ChapelOfTheOldLight, // a shrine to whatever they worshipped before the Night
        CrawlingLarder,      // the stores, and what has moved into them
        ArmouryOfTheFallen,  // racked weapons, most rusted, some not
        ScholarsStudy,       // a small library, water-stained, half-legible
        CollapsedStair,      // the way down is no longer a way down
        GraveyardOfBanners,  // the dead lord's hall of arms and ancestors
        ThroneOfDust,        // the seat itself — always the last chamber
    }

    public struct ChamberDef
    {
        public ChamberType         Id;
        public string              Name;
        public string              EntryLore;     // shown on arrival
        public string              SearchLine;    // shown while searching, before the wait
        public RuinsMath.LootKind  LootBias;       // which loot kind this room favours
    }

    public static class RuinsCatalog
    {
        private static readonly List<ChamberDef> _defs = new List<ChamberDef>
        {
            new ChamberDef
            {
                Id = ChamberType.CollapsedHall,
                Name = "The Collapsed Hall",
                EntryLore = "The roof gave up long before you arrived. Beams lie crossed like broken ribs over what was once a hall wide enough to seat a household. Rubble in drifts. Something in it still glints.",
                SearchLine = "You pick a path through the wreckage, testing each stone before you trust it with your weight.",
                LootBias = RuinsMath.LootKind.Weapon,
            },
            new ChamberDef
            {
                Id = ChamberType.FloodedCellar,
                Name = "The Flooded Cellar",
                EntryLore = "Stairs go down into black water that never drained and never will. Barrels float, long since staved in. The cold climbs your legs before you've taken three steps.",
                SearchLine = "You wade in, feeling along submerged shelves with your hands because your eyes are useless here.",
                LootBias = RuinsMath.LootKind.TradeGoods,
            },
            new ChamberDef
            {
                Id = ChamberType.SealedBedchamber,
                Name = "The Sealed Bedchamber",
                EntryLore = "The door is barred — from the inside. Whoever slept here last did not want to be found, or did not want to let something else in. The bar is old wood, and it still holds.",
                SearchLine = "You work the bar loose and push the door open on a room no one has aired out since the world ended.",
                LootBias = RuinsMath.LootKind.Armor,
            },
            new ChamberDef
            {
                Id = ChamberType.ChapelOfTheOldLight,
                Name = "The Chapel of the Old Light",
                EntryLore = "A small shrine, its icons scratched blank by hands that came after whoever carved them. Candle stubs, long cold, still stand in neat rows on the altar. Someone tended this after the Night began.",
                SearchLine = "You search the altar and the little vestry behind it, the way you'd search anything a desperate household thought worth locking.",
                LootBias = RuinsMath.LootKind.SpellFormula,
            },
            new ChamberDef
            {
                Id = ChamberType.CrawlingLarder,
                Name = "The Crawling Larder",
                EntryLore = "The stores. Sacks long rotted to dust, jars long since shattered — and something living in the wreckage of both, rustling in the dark corners when your torchlight passes over them.",
                SearchLine = "You keep your torch low and your steps loud, and search anyway, because hunger doesn't care what's watching you back.",
                LootBias = RuinsMath.LootKind.TradeGoods,
            },
            new ChamberDef
            {
                Id = ChamberType.ArmouryOfTheFallen,
                Name = "The Armoury of the Fallen",
                EntryLore = "Racks of weapons, most rusted past use, a few still bright where oiled leather kept the air off them. Whoever armed this place expected a siege that never came, or came and was lost anyway.",
                SearchLine = "You go rack by rack, testing edges and straps, keeping only what still deserves to be carried.",
                LootBias = RuinsMath.LootKind.Weapon,
            },
            new ChamberDef
            {
                Id = ChamberType.ScholarsStudy,
                Name = "The Scholar's Study",
                EntryLore = "Shelves, mostly collapsed. The books that survived are swollen with damp, their pages fused — except for one, oddly, still dry, still open on the desk to a page someone meant to come back to.",
                SearchLine = "You turn pages that crumble at the edges, looking for whatever kept this room locked long after its owner stopped needing it.",
                LootBias = RuinsMath.LootKind.SpellFormula,
            },
            new ChamberDef
            {
                Id = ChamberType.CollapsedStair,
                Name = "The Collapsed Stair",
                EntryLore = "The stair down is no longer a stair — a slope of broken stone dropping into darkness the torchlight doesn't reach the bottom of. Someone went down here once. The rope they used is still tied off at the top.",
                SearchLine = "You test the old rope, find it holds, and start down carefully, one loose stone at a time.",
                LootBias = RuinsMath.LootKind.Relic,
            },
            new ChamberDef
            {
                Id = ChamberType.GraveyardOfBanners,
                Name = "The Graveyard of Banners",
                EntryLore = "A hall of arms — shields on the walls, banners rotted to threads, the painted faces of a dead lord's ancestors staring down at nothing. This house ended here, in this room, with no one left to remember it.",
                SearchLine = "You search beneath the banners and behind the shields, where a proud house would have hidden what it valued most.",
                LootBias = RuinsMath.LootKind.Armor,
            },
            new ChamberDef
            {
                Id = ChamberType.ThroneOfDust,
                Name = "The Throne of Dust",
                EntryLore = "The seat itself. Dust lies undisturbed on the arm-rests in a shape almost like hands. Whoever ruled here ruled to the very end, and then simply — stopped. This is the last room. There is nowhere further to go.",
                SearchLine = "You search the dais the way you'd search a grave, because that is what it is.",
                LootBias = RuinsMath.LootKind.Relic,
            },
        };

        public static IReadOnlyList<ChamberDef> All => _defs;

        public static ChamberDef Get(ChamberType id) => _defs.First(d => d.Id == id);

        public static ChamberDef GetByIndex(int poolIndex) => _defs[poolIndex];

        public static int PoolSize => _defs.Count;
    }
}
