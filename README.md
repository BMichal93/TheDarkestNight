# The Darkest Night — v0.10.0

A total-conversion of Mount & Blade II: Bannerlord. The world shattered overnight: demons crawl out from the underworld every dusk and hunt the living, humanity survives behind walls and wards, gold has stopped mattering, and magic is no longer a noble's birthright but a formula anyone can tap out with their own two hands — if they're brave (or reckless) enough to try an untested one in battle.

This mod is built on **Ash and Ember**, a mature magic-overhaul codebase that serves as this conversion's baseline and "parts bin" — many of the mechanics below (the Awakened, the Codex, crystals, schemes, sea trade, the tavern) are Ash and Ember systems reused wholesale or repurposed; see `CLAUDE.md` for the full architecture map and which folders are new versus inherited.

## What's new in The Darkest Night

- **The demons.** Every dusk, red-and-black demon war-parties — built entirely from re-dressed, re-tinted human and horse meshes (no new assets exist) — flood the map. They never retreat, never negotiate, execute their prisoners, and sink back below at dawn. Rarely, a horde assaults a town. See `src/Demons/`.
- **The barter economy.** Gold is roughly a tenth as common everywhere (wages, upgrades, building, loot, ransom); town markets are starved of food/horses/good weapons; garrisons and food stores run at about half strength. Village food and the barter/item-exchange screens are how you actually get by. See `src/Economy/`.
- **The Spellbook — the Scrived Word.** Casting is runes drawn in the air: hold the focus key with free hands and tap **three** of U/D/L/R for each rune, then release the binding. 36 runes exist (Cinder, Tide, the Long Mark, the Bar, the Calling…); each does something alone, repetition amplifies, and runes **combine into a language** — two elements fuse (Fire+Water = Fog), three become a Triad (*the Tempest*), a Form reshapes the whole working (the Long Mark throws it, the Bar walls it, the Calling gives it a body). A real rune you've never drawn writes itself into your book on the spot; a malformed or over-long binding fizzles and risks a *spellburn*, and even a valid long binding carries *strain*. Opening the book costs one focus point; learn runes from ruins, from the Tower faction, or start with two. See `src/Spellbook/`.
- **The eight factions.** Every kingdom has been rebuilt into a desperate answer to the Long Night, each with its own joining ritual, vassal title, and city-menu mechanic: the **Wolf Brothers** (Sturgia, cannibal survivalists), the **Tower** (Aserai, scholars who teach spells), the **Forest Widows** (Battania, a fungal hive-mind that speaks as "we"), the **Bloodbound** (Khuzait, demon-hunters who harvest Demon Blood), the **Temple** (Vlandia, a militant faith bearing Holy Sigils), **The Empire** (Northern Empire, keepers of the old rites, with Schemes access), **Legion** (Western Empire, raiders dreaming of an ark beyond the sea), and **The Chosen** (Southern Empire, a matriarchy that buys peace with sacrifice). Every other town stands alone as a Looter/Bandit-recruiting city-state. See `src/Factions/`, `src/FactionQuests/`, `src/CityStates/`.
- **The ruins.** ~80% of castles are ownerless dungeons of the old world — collapsed halls, flooded cellars, sealed bedchambers — explored chamber by chamber on a Scouting-scaled timer, with real risk if night catches you inside. They're where relics and spell formulas are actually found. See `src/Ruins/`.
- **Magical items.** Relics, wands, and talismans — weakened, permanent versions of the old Crystal and Dark Gift effects, generated cool names included — drop rarely from demon fights and ruins, and deal bonus damage to demons. See `src/Relics/`, `src/Wands/`, `src/Talismans/`.
- **The clock of the apocalypse.** A Night of the Hunt strikes every 20–82 days; rumours of something gathering begin around day 300; a persistent demon band forms around day 600; past day 1000, a named Demon Lord may rise to conquer the world — or be hunted down and killed to win the campaign. See `src/Apocalypse/`.
- **Mortal law.** NPC lords live under the same rules as the player: no empire ever re-forms, small parties shelter at dusk instead of travelling, hungry armies raid for food, and enemy rosters look as scarce as yours. See `src/MortalLaw/`.
- **A survivor's past.** Character creation offers one background — *"I am a survivor"* — and rewrites the Youth/Young Adulthood steps around demons and scarcity instead of peacetime Calradia; choosing "studied the arcane arts" starts you with the Spellbook and two short spells. See `src/AI/CreationBackstoryRework.cs`.

The retired Ash and Ember caster paths — the old hold-and-charge input, the Living Ember, the two-phase spell forms, and the Inner Fire/Dark Gift path choice — have moved to **[`LEGACY.md`](LEGACY.md)**. They are still how NPC lords, seers, priests, the Awakened, and demons cast, so that file remains the authoritative description of spell *behaviour*; it is simply no longer a description of anything you do.

What remains below this point is current, with one caveat worth keeping in mind: much of it is inherited Ash and Ember machinery that The Darkest Night kept, re-themed, or gated (dice games, drinking, and sea travel are explicitly untouched). Sections that describe something the player can no longer reach are marked as such where they sit. See `CLAUDE.md` for the full architecture map.

---

## Package Structure

```
TheDarkestNight/                         (module id TheDarkestNight, presented as "The Darkest Night")
├── SubModule.xml                    mod manifest
├── ModuleData/
│   ├── items.xml                    demon/relic/wand/talisman/Holy Sigil/Demon Blood item defs
│   ├── troops.xml                   elemental_being / demon troop templates
│   └── monsters.xml                 demon_hulking — the Ravager's larger body capsule (additive)
├── src/                             ~106 000 lines across ~430 source files (grouped by system folder)
│   ├── MagicSystem.cs               module entry point + mission behaviour + debug-grant hook
│   ├── MagicInputHandler.cs         keyboard/gamepad combo detection (legacy path)
│   ├── SpellBuilder.cs              two-phase input parser → SpellCast (legacy / NPC)
│   ├── AgingSystem.cs               casting cost (days of life)
│   ├── SchoolData.cs / SpellDatabase.cs / ActiveEffects.cs / SaveDefiner.cs
│   │
│   │   ── The Darkest Night systems ──
│   ├── Demons/                      the night tide: factory, catalog, spawner, battle AI, visuals
│   ├── Spellbook/                   formula casting, fizzle/spellburn, rare spellcaster lords + troop tree
│   ├── Relics/ Wands/ Talismans/    magical items — weakened Crystal/Dark Gift effects, demon-bane bonus
│   ├── Ruins/                       ~80% of castles as explorable, ownerless dungeons
│   ├── CityStates/                  unclaimed towns as clan-named, Looter/Bandit-recruiting free cities
│   ├── Apocalypse/                  Night of the Hunt, the gathering, the Demon Lord endgame
│   ├── MortalLaw/                   NPC lords under the same scarcity/night-fear/food rules as the player
│   ├── Economy/                     barter/scarcity GameModel overrides, daily market pruning
│   ├── Factions/                    the eight reworked kingdoms (culture, dialogue, joining ritual, city menus)
│   ├── FactionQuests/               one questline per faction, day-50+ trigger
│   ├── Expeditions/                 the Antiquarian Charter — send a hired team into a ruin (run out of The Camp)
│   ├── ForeignMuster/               Legion towns' weekly rotating foreign tier-1 recruit
│   ├── BeastsOfTheNorth/            Wolf Brothers' Jotunn-Blooded giant + Ulfhednar wolf-riders
│   └── Units/                       promotion/recruit tolls, lord gear weathering
│   │
│   │   ── Ash and Ember baseline (still live — NPC casting, retained features, reuse templates) ──
│   ├── Magic/                       unified element system (Codex, input [player path retired], effects, walls, ultimates, teachers)
│   ├── Spells/                      legacy two-phase Inner Fire — SpellEffects.* partials, Blast/Self/Create spells, enchantments (drives NPC casts)
│   ├── Nature/                      the Living Ember — charges, living-energy economy, seers, backlash (player path retired)
│   ├── Miracles/                    Grace — prayers, grace economy, priest troops, battle AI, talents (player path retired)
│   ├── DarkGifts/                   the Dark Gift path (template for Talismans/Relics)
│   ├── Crystals/                    consumable crystal items, effects, battle AI (template for Relics)
│   ├── Elementals/                  the Awakened — elemental beings; direct visual/behaviour template for Demons/
│   ├── Talents/                     talent tree, learning curve, map-spell talents
│   ├── Schemes/                     covert operations — now Empire-only, influence-paid (see CLAUDE.md)
│   ├── Soldier/                     Take the Lord's Coin — hire out as a common soldier
│   ├── Sea/                         harbours, voyages, trade ventures, NPC sea lanes (retained untouched)
│   ├── Markets/                     the Exchange — commodity speculation
│   ├── Tavern/                      tavern menus, rumours, outcomes (dice games + drinking, retained untouched)
│   ├── ClanOrders/                  clan order system
│   ├── AshenRuins/                  explorable Ashen ruins — structural template for Ruins/
│   ├── Conclave/ Apprentice/        Ember Conclave, apprentice system
│   ├── QuestSystems/                Dragon main quest, Burning Lab questline, settlement encounters, world & battle events; GreatAwakening/ and NorthmenStones/ are the reuse templates for FactionQuests/
│   ├── AI/                          Ashen kingdom/city (template for CityStates/), NPC mage AI, bandit mages, dialogue, cultures, character-creation rework, Sandbox-only gate
│   ├── Tribes/ Campaign/            legacy faction/culture reworks, map tone
│   ├── Visual/                      glows, movement, atmospheric scene tone, battle whispers
│   └── Startup/                     splash, lore intro, loading screen (The Long Night text)
├── tests/
│   ├── TheDarkestNight.Tests.csproj
│   └── PureLogicTests.cs            covers every pure *Math.cs above — 627 tests
├── README.md                        this file — what you can actually do
├── LEGACY.md                        the retired Ash and Ember caster paths (still how NPC casts behave)
└── CHANGELOG.md                     release history
```

---

## Installation

### Requirements

- **OS:** Windows 10 or Windows 11
- **Game:** Mount & Blade II: Bannerlord — Steam or Xbox / Game Pass
- **Version compatibility:** built against Bannerlord's `.NET Framework 4.7.2` runtime

### Upgrading from an earlier build — read this first

The mod used to install as `Modules\AshAndEmber\`. It now installs as **`Modules\TheDarkestNight\`**.

**Delete the old `Modules\AshAndEmber\` folder before playing.** If you leave it, the launcher lists it as a separate mod ("Ash and Ember"), and enabling both loads the same systems twice. Nothing in your save lives in that folder — campaign data is stored in the save file itself, so deleting it costs you nothing.

Bannerlord may warn that a save was made with a module that is "no longer present" the first time you load. That is expected: the module's **id** changed from `AshAndEmber` to `TheDarkestNight`. Your campaign loads normally — every save key and item id is unchanged.

### Step 1 — Download

Download the latest release ZIP. Extract it anywhere. You get a single `TheDarkestNight` folder.

### Step 2 — Install

#### Option A — Script (recommended)

Open PowerShell in the extracted folder, then:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\install.ps1
```

The script finds your Bannerlord installation automatically (Steam registry, default paths, Xbox paths). For a non-standard location:

```powershell
.\install.ps1 -BannerlordPath "D:\Games\Mount & Blade II Bannerlord"
```

#### Option B — Manual

Copy the `TheDarkestNight` folder (the one containing `SubModule.xml`) into:

```
<BannerlordRoot>\Modules\TheDarkestNight\
```

`SubModule.xml` must be directly inside `Modules\TheDarkestNight\`, not one level deeper.

### Step 3 — Enable in launcher

Open the Bannerlord launcher → Mods → tick **The Darkest Night** → Play.

The launcher may mark the mod with a red *"Couldn't verify some or all of the code included in this module"* warning. It is cosmetic and safe to ignore — on Xbox / Game Pass installs the launcher's `ModVerifier.exe` is not shipped, so the check cannot run, and a check that cannot run is recorded as a failure. Every enabled community mod on such an install draws the same mark.

### Step 4 — Verify

Start a new Sandbox campaign. A short, four-screen lore introduction appears (*"An Empire ruled all Calradia..."*). If it plays and character creation opens with only "I am a survivor" as a background, the mod is installed correctly.

---

## Lore Introduction

When starting a new Sandbox campaign, the intro screen (`src/Startup/AshEmberLoreIntro.cs`) tells the Long Night's story: Calradia had a thousand years of history and then one night the sky tore and the dead world's things climbed out; by morning the great armies were ash and only walls and faith held; the survivors built their world small, trading grain and iron hand-to-hand because gold buys nothing from things that don't trade; and in the ruins a few have found the old formulas still — spoken shapes that bend the world, if you get the shape right. The character-creation flow that follows is described under **A Survivor's Past** below, not the old gift-choice prompt.

**Getting the Spellbook:** every new character walks the same background (*"I am a survivor"*), and the Spellbook itself is unlocked by spending one focus point at the in-game menu, or granted at creation by choosing one of the Young Adulthood "studied the arcane arts" options (which also grants two random spells with formulas up to 7 characters). See **A Survivor's Past** and **The Spellbook** below.

---

## The Spellbook

*(The Darkest Night's only player-facing casting path. Superseded the sections below it, which now describe internals/NPC behaviour — kept for reference.)*

### Unlocking

Open the spellbook menu (Alt+L on the map, or **Controller X+Y** on a pad) once the option appears, and spend **one focus point**. From then on, spells are learned three ways: tap a full, correct formula in battle (even one you've never studied — a lucky/reckless guess is recorded exactly like a taught spell), learn from **the Tower** faction's teaching menu, or find a formula in the **Ruins**. Picking "a stranger's book" as your character-creation Keepsake unlocks it immediately with two formulas already known.

### Inheritance

If your character dies and an heir succeeds you, you're asked whether the heir keeps the book: every known formula passes on unbroken, or you can let it burn with you and start the family's magic over from nothing. The choice fires once succession completes.

### Casting

| Action | Input |
|--------|-------|
| Hold focus | **Left Alt** (keyboard) or **Controller RLeft** |
| Tap the formula | **W/A/S/D** = Up/Left/Right/Down (keyboard) or the **left stick** (controller), while holding focus |
| Open the spellbook mid-formula | **Alt + X** |
| Cast | complete the correct 5–20-character sequence |

Casting requires **empty hands** — no weapon or shield wielded. A correct sequence looses the bound spell (see `src/Spellbook/SpellbookCatalog.cs` for the full 30–50-entry list, including Fireball, Firewall, every elemental gust/wave/entangle/nova and its wall, Summon Demon, Banish Demons, and Light). A wrong, *completed* sequence fizzles and rolls a **spellburn** — base 60% chance, reduced by Intellect: self-damage, immobilization, a rogue unit command, an area burst, a demon flickering onto the field for 70 seconds switching sides every 10, and several more in the same vein (`src/Spellbook/SpellburnEffects.cs`).

### Rare casters

Roughly 15% of named lords/companions know 1–3 spells and cast them in battle. A dedicated, rare spellcaster troop tree also exists (recruit through tier 5, each tier knowing 2–3 battle spells) — see `src/Spellbook/SpellcasterLords.cs` / `SpellcasterTroops.cs`. No spells exist on the campaign map; every working is a battlefield cast.

---

## The retired caster paths *(moved)*

Ash and Ember's older player-facing casting paths — **Getting the Gift**, the element **Controls** (hold-and-charge), **The Living Ember**, and the legacy two-phase **Spell Forms / Effects** — are no longer how the player casts, and have moved to **[`LEGACY.md`](LEGACY.md)** to keep this file about what you can actually do.

They are not dead code: seers, priests, the Awakened, and demons still cast through that machinery, so it remains the authoritative description of how spells *behave* on the field. As of v0.8.0 the old "Gift" prompt is gone entirely — a new campaign never asks, and the legacy unified-element NPC LORD casters are retired for new games too (an existing save that already had them keeps them). Aging is still a live cost (see below) — `LEGACY.md` opens by explaining both.

---


## Aging Cost

**The Spellbook does not charge aging.** Casting a formula costs you nothing but the risk of a spellburn — so if the Spellbook is all you use, the per-cast table below never applies to you.

Aging is still a live cost, though, through two other doors: the **Ashen Ruins** menus spend days of your life outright, and the **Reaping** and certain rites give days back. The `AgingSystem` and its Ledger of Years remain fully in play — this is where your lifespan actually goes.

The per-input cost table below belongs to the retired element/two-phase casting paths (see [`LEGACY.md`](LEGACY.md)). It is what NPC casters pay, and what a save from an earlier build already spent; it is kept here because the Ledger, the cap, and the Reap/Tempered mechanics it describes are shared with the live paths above.

Cost scales **geometrically** with total inputs — weak spells are cheap; powerful spells become very expensive. Hard cap: 84 campaign days (1 Bannerlord year = 4 seasons × 21 days).

**The Ledger of Years** — the grimoire (Alt+X) opens with a running account of the aging economy: your age, total days the fire has taken, days reclaimed (Reap, Ember, rites), and how many workings you have cast in battle and on the map. When the fire will finally burn out, it does not say — the fire keeps that count, and it does not give receipts.

| Total inputs | Cost | With Tempered |
|--------------|------|---------------|
| 1–2 | 1 day | 1 day |
| 3 | 2 days | 2 days |
| 4 | 3 days | 2 days |
| 5 | 4 days | 3 days |
| 6 | 5 days | 4 days |
| 7 | 8 days | 6 days |
| 8 | 11 days | 8 days |
| 9 | 15 days | 11 days |
| 10 | 21 days | 16 days |
| 12 | 41 days | 31 days |
| 14 | 80 days | 60 days |
| 16+ | 84 days (cap) | 63 days |

**Tempered** talent cuts the cost by 25% (rounded, minimum 1 — battle casts are never free), plus up to 30% age-based reduction after age 40.

### Campaign map casting cost

Campaign map spells escalate in cost with each use per calendar day:

| Cast # that day | Cost |
|-----------------|------|
| 1st | 1 day |
| 2nd | 4 days |
| 3rd | 8 days |
| 4th | 12 days |
| … | +4 per additional cast |

The counter resets at midnight — a notification appears in the message log. **Resonance** talent gives a 25% chance to skip the cost entirely on any cast. Ashen players pay criminal rating instead of days (see below).

### Becoming Ashen

At age 100 a prompt appears: *The Last Ember*. You may:

- **Take the cold** — become Ashen. Aging stops permanently (the cost of casting is now crime rating, not years). Your appearance changes: ash-white, grey-tinged. Your lords and the Ashen kingdom treat you as one of their own.
- **Let it end** — die of old age.

Ashen mages are completely immune to all magical aging — both the per-cast aging and the daily age check.

### Possession (Ashen players)

Ashen mages do not age, but repeated casting each day risks the cold stirring against them. After the first working each day, each further cast has a **33% chance** to trigger the **Possession** event:

**The Flame Turns** — Dark instincts and cold flame flood your body. You must choose:

| Choice | Outcome |
|--------|---------|
| **Surrender to it** | Death — the cold claims what it wants. |
| **Focus your will** | Leadership test. Success chance = skill × 0.3% (max 90%). |
| **Overwhelm it with your body** | Athletics test. Success chance = skill × 0.3% (max 90%). |

Failure follows a **two-strike rule**: the first failed test does not kill you — you are left broken (wounded to near-death, −20 party morale) and **strained for 21 days**. Failing another test while strained is death. Surrendering is always death.

This is the balancing cost of immortality — spamming map spells as an Ashen carries real risk, but one bad roll will not end a campaign on its own.

### Tournament

Casting **any** spell during a tournament kills and disqualifies you instantly.

---

## Talents

Talents are learned through the grimoire (Alt+X → *Talents*). The **Gift** is free. The first 9 purchased cost 1 focus point each; 10th onward costs 2 points. **Lost Forms** always cost a fixed 2 focus points.

### Passive

| Talent | Effect |
|--------|--------|
| **Gift** | You carry the fire. Battle casting enabled. |
| **Tempered** | Battle casts cost 25% fewer days (rounded, minimum 1). Beyond age 40, each year reduces cost by an additional 0.5%, up to 30% total. |
| **Resonance** | One in four campaign map castings costs no days. |
| **Kinship** | +10 relations with mage lords; relation cannot fall below 0 with them. |
| **Reap** | Executing a captured lord restores 20 days + 10 per tier of their clan (max 80). Raiding a village restores 5 days (7-day cooldown). Each discarded prisoner: 5% chance to restore 1 day. Learning this marks you. |
| **Ember** | 5% chance per battle kill to restore 1 day of youth. |
| **Flashfire** | Each battle spell has a 10% chance to echo — firing again instantly at no aging cost. |

### Enchantment

Enchantments fire automatically on every qualifying cast in battle.

**Damage enchantments** (trigger: Damage effect — applies to all hit units, allies included):

| Talent | Triggered by | Effect |
|--------|--------------|--------|
| **Scatter** | Force (A) inputs | Blasts enemies backward (5 m per Force input) and slows movement 25% per input (max 75%) for 4 s + 1.5 s per input. |
| **Smoulder** | Any damage input | Scorches enemy morale (−15 per input) and bewilders non-hero enemies with a random effect: rout, charge, dismount, or morale fracture. |
| **Sunder** | Shred (D) inputs | Increases all damage enemies receive and reduces their attack power. Damage vulnerability = 10% per Shred input (max 50%). Attack reduction = 10% per input (max 50%). Duration = 8 s + 1.5 s per input. |
| **Immolate** | Sear (W) inputs | Sets enemies alight — bonus burn damage (10 per Sear input). Kill slots scale with Sear inputs (one per 3): the first kill of a cast is certain, each further slot connects at 50%. 2 Sear: 50% kill chance; 1 Sear: 33%. |

**Restore enchantments** (trigger: Restore effect on allies):

Unlike damage enchantments — which are split across the Sear/Force/Shred natures, so one cast only feeds the natures it carries — **every Restore enchantment you own fires together on a single Restore cast**. Each one is therefore tuned weaker than its damage counterparts; the payload of a full restore build is the stack, not any single talent.

| Talent | Effect |
|--------|--------|
| **Ashveil** | Brief magic immunity for healed allies. Duration = 2 s per Restore input, max 10 s. |
| **Cinder Shell** | Reduces incoming damage (6% per input, max 30%) for 4 s + 1 s per input. Allies above 90% health also gain a 10 HP damage shield per input for 5 s. |
| **Hearthlight** | Lifts allied morale. Boost = 10 per Restore input. |
| **Reflect** | Healed allies reflect 5% of melee damage per input (max 25%) back at attackers. Ranged hits do not trigger the reflection. |

### Spell (campaign map)

Cast from the grimoire on the campaign map. Costs 1 aging day for the first cast each day; escalates with repeated use. Crime rating instead of days if Ashen. NPC mage lords also cast these on the campaign map.

#### Arcane sequence

When you cast a campaign spell, a 3-step ritual description appears — two sentences per step. Each step has three variant phrasings; one is drawn at random each cast. The description disappears, then you are asked to identify each step's exact phrasing from its three variants. Your recall score scales the spell's output power — the aging cost is always paid regardless of score.

| Correct | Multiplier | Flavour |
|---------|-----------|---------|
| 3 / 3 | **1.50×** | Resonance — the rite was perfect. |
| 2 / 3 | **1.20×** | The working takes hold. |
| 1 / 3 | **0.80×** | The words blur — the fire catches unevenly. |
| 0 / 3 | **0.50×** | The words scatter — the fire finds its own shape. |

A **"Cast without the rite"** button on the sequence screen skips the minigame and fires the spell at 1.00× — guessing blindly averages worse than skipping; genuine recall beats both.

The values in the table below are baseline (1.00×, the no-rite value).

| Talent | Effect |
|--------|--------|
| **Kindle** | Party morale +40 and up to 8 wounded soldiers per troop type recover. |
| **Unsettle** | Nearest enemy party within 75 map-units loses 40 morale and −10 influence. |
| **Wither** | Nearest enemy village loses 20% of its hearth. |
| **Clairvoyance** | +25 influence, or +700 gold if not in a kingdom. |
| **Extinguish** | 5–12 soldiers in the nearest enemy party within 60 map-units are wounded or killed; −30 morale. |
| **Fade** | Your party is concealed from enemy scouts for 2 days. A perfect recall (3/3) extends this to 3 days. |

### Lost Form (◈)

Lost Forms permanently alter how a spell form behaves once purchased. Each costs a fixed **2 focus points** regardless of how many talents you own. They appear as a separate category in the talent menu. They are sidegrades, not upgrades — priced below the late-game talent cost so trying one is never a build mistake.

| Talent | Fixed Cost | Effect |
|--------|-----------|--------|
| **Widened Blast** | 2 pts | Blast cone widens from ~49° to ~60°. More units caught at the edges. |
| **Twin Bolt** | 2 pts | Missile fires two bolts side by side, each at 60% of original damage and healing power. |
| **Fading Ward** | 2 pts | Barrier nodes expire after 60 seconds instead of persisting indefinitely. |
| **Directed Burst** | 2 pts | Burst is asymmetric: full power in the forward hemisphere, 40% power in the rear arc. |

---

## Rival Shadow

The cold ignores nobodies. Once your clan reaches **tier 3**, one Ashen lord is designated as your **Shadow** — a personal antagonist who watches you. A popup (*A Cold Attention*) announces that the dark forces of the north have noticed you.

Every 14–21 days the Shadow acts against one of your settlements: loyalty or security drops. After **five schemes**, the Shadow rides out alone to confront you.

**The Shadow Approaches** — a multi-select event:

| Choice | Outcome |
|--------|---------|
| **Face them — through will** | Leadership test (skill × 0.4%, max 85%). Win → Shadow driven back (+5 focus, +200 renown, nearest Ashen lord converts). Lose → −5 days, the Shadow heals their wounds. |
| **Face them — through endurance** | Athletics test (same scaling). |
| **Withdraw** | −30 renown. Timer resets; schemes resume. |

If the Shadow dies by other means the designation clears.

---

## Mage Companions

When a companion joins your party there is a **20% chance** they carry the inner fire. Companions with the gift always enter with 1–3 battle enchantments already shaped in them.

Companion mages age **25% faster** than regular mage lords after battle — the fire burns more personally in those who ride beside you.

Companion mage status is tracked and saved independently from lord mages so the system survives save/load cleanly.

---

## Spell Aftermath

Certain casts leave a mark on the ground after they fire:

| Trigger | Effect | Duration |
|---------|--------|----------|
| **Missile + Damage** | Fire patch (3 m radius) spawns at explosion point. Damages enemies who walk through it (~8 HP/s per Damage input). | 8 seconds |
| **Burst + Restore** (player only) | Holy zone (burst radius) lingers at cast position. Heals allies within it (~8 HP/s per Restore input). | 5 seconds |

---

## Whisper System

The cold watches. Certain acts open a crack in the fire.

**Whisper hooks (per event):**

| Act | Whispers gained |
|-----|----------------|
| Ashen lord killed by player | +1 |
| Any lord executed by player | +5 |
| Dark rite completed (Ashen Altar) | +5 |
| Sanctuary prayer failed | +2 |
| Battle lost (player involved) | +1 |

Whispers reflect recent conduct, not a permanent stain. They decay two ways:

- **Virtue** — honourable, merciful players (Mercy + Honor ≥ 2) have a 1-in-7 chance each day to lose 1.
- **Quiet conduct** — after 10 consecutive days without gaining a whisper, roughly 1 whisper fades every 3 days regardless of traits.

Certain settlement encounters also feed or starve the cold — burning the village in *Darkness in the Roots*, watching *The Pyre* for sport, joining the dance of the *Three Figures*, or reaching back into *Ash in the Dream* all add whispers; saving the girl, funding the priest's sanctuary, dismissing the dream, or scattering the witches' rite shed them.

**NPCs and the cold.** NPC mages carry no whisper counter — invisible per-lord bookkeeping would never surface to the player. Their corruption is modelled at the granularity you can actually see: a mage lord who overexerts in battle (15+ days aged in one fight) has an 8% chance of turning Ashen, the *Whispers from the Ash* world event pulls 1–3 mage lords to the cold, and lords who die at 100 have a 5% chance to rise Ashen.

**Whisper tiers.** The count itself stays hidden, but the cold expresses itself in stages (crossing a tier shows a one-time warning, and the Ledger of Years carries a vague status line):

| Tier | Threshold | Effect |
|------|-----------|--------|
| Noticed | 25+ | Occasional ambient whispers on the campaign map (rare — at most a few per season; one in three carries real intelligence: the bearing of the nearest Ashen warband). |
| Favoured | 50+ | Ashen Altar rituals gain +1 point per round; Sanctuary meditation loses 1 point per round (never below 1). |
| Close | 75+ | The altar bonus and sanctuary drag deepen to 2. The Temple declares you **anathema** (see The Temple). |

At **100+ whispers** a countdown of 7 days begins. Then **The Cold Calls Your Name** fires:

| Choice | Outcome |
|--------|---------|
| **Resist** | −10 days, −30 whispers. The event can fire again if whispers climb back to 100. |
| **Bargain** | −30 days, −60 whispers. |
| **Accept** | Become Ashen immediately. |

---

## NPC Mage Lords

At campaign start roughly 20% of lords are seeded as mages. A subset are **Ashen lords** — they cast with no aging cost, shorter cooldown, and heavier spell recipes.

### Campaign map casting

NPC lords cast on the campaign map independently. Ashen lords cast approximately every 3–7 days; regular mage lords every 5–10 days. Older lords cast less frequently. At most one Ashen lord and one regular mage lord produce a visible cast notification per in-game day.

### Battle AI priority

1. **Defensive burst** if HP < 40% and enemies within 8 m — clears close threats instead of warding.
2. **Heal burst** if HP < 30%.
3. **Heal burst** for allies below 50% HP within 15 m (non-Ashen lords only — Ashen fight on regardless).
4. **Attack** based on lord personality:
   - *Ashen*: rotating heavy Blast/Burst recipes (up to 6 inputs), never idle.
   - *Calculating*: prefers Burst when multiple enemies are in the area; precise Blast otherwise.
   - *Impulsive*: forward Blast-heavy, high tempo.
   - *Default*: balanced Blast/Burst mix.

Ward is no longer castable by NPC lords — it is now a Restoration talent available only to the player.

NPC mage lords briefly sheathe their weapon immediately before casting. The cast fires roughly 0.7 seconds later, giving the sheath animation time to complete. The AI re-wields automatically after the spell resolves.

Ashen lords skip the no-enemies early exit and cast proactively at all times. First cast is delayed 12 seconds; subsequent casts use the lord's trait-modified cooldown. Ashen spells — both NPC and player — display cold-blue and grey visuals.

### Aging (NPC)

NPC lords age after every battle in which they cast: `max(1, totalInputs / 4)` days. Ashen lords are immune. NPC lords die at age 100 (5% chance to become Ashen instead).

### Cooldowns

| Lord type | Cooldown |
|-----------|----------|
| Default | 25 s |
| Impulsive | 15 s |
| Calculating | 35 s |
| Ashen | 6 s |

---

## Bandit Mages

About 4% of eligible bandit units carry a stolen fragment of the fire. After each cast, there is a chance the caster burns out and dies.

| Tier | Troop types | formCount | Burnout |
|------|-------------|-----------|---------|
| Untrained | Looter | 1 | 35% |
| Bandit | forest/sea/mountain/steppe/desert bandit | 2 | 25% |
| Cultist | Fire Worshippers / Ashen Spawn | 3 | 15% |

Each type has a title shown in the combat log: Fire Prophet, Hedge Witch, Ashen Caller, Ash Shaman, Wind Binder, Ember Prophet.

### Fire Worshippers & Ashen Spawn

~10% of Looter and forest bandit parties become **Fire Worshippers**. ~10% of sea raider and mountain bandit parties become **Ashen Spawn**. Both are guaranteed at least one mage caster.

**Ashen Spawn parties are very large** — spawned by world events, they arrive as hordes of 120–200 troops. Talking to their leader is impossible; they only answer with silence.

---

## Battlefield Events

Occasionally a battle begins under cursed conditions. Each event rolls independently; most battles have none. Active events are announced in the message log.

| Event | Chance | Effect |
|-------|--------|--------|
| **Cinder Rain** | 10% | Every non-Ashen agent takes damage every 20 s. Burning-sky fog, ground fire field, aerial amber glow. |
| **Ember Tithe** | 7% | Every Ashen agent takes damage every 20 s but gains +10 morale. Amber pulse above their formation. |
| **The Rising** | 12% | Spawns units on the Ashen side every 20 s (Ashen battle only). Ground eruption burst + ghostly aerial lights at spawn. |
| **Dread** | 8% | All non-Ashen agents lose 30 morale (one-shot). Sky darkens to deep dusk, cold dark fog, grey fire field across the whole battle area. |
| **Last Light** | 5% | Sets time-of-day to midnight (one-shot). Fire-lit night fog, wide ground fire, burning-sky aerial glow. |
| **Ashen Ground** | 7% | All mounted agents are dismounted every 20 s. Ash-grey fog, grey ground particles. |
| **Frenzy** | 7% | Charge order to every formation every 20 s. Blood-red fog, chaotic fire field, aerial crimson glow. |

Expected events per battle: ~0.5. ~60% of battles are clean.

---

## Settlement Encounters

When entering or leaving a settlement, or after a battle, the mod may trigger a short narrative encounter — a short piece of text with a choice that has a mechanical consequence (gold, relations, morale, troop changes). The encounter pool has over 40 unique events gated by mage status, Ashen status, renown, and settlement type.

A cooldown of 6 days prevents back-to-back encounters. Six new dark-themed events have been added:

| Event | Trigger | Description |
|-------|---------|-------------|
| **Darkness in the Roots** | Enter village | Signs of Ashen cultists. Burn the village (crime +50, 50% −60 relations) or spare them (50% nothing, 50% 200 Ashen Spawn raid anyway). |
| **The Pyre** | Enter village | A girl is bound to a stake. Let her burn (Calculating +1), watch for fun (Mercy −1), stop them (Mercy +1; 50% she was Ashen and casts a curse), or ride past. |
| **The Priest at the Gate** | Enter town | A priest asks for funding to build a Sanctuary. Donate 10 000g (guaranteed), 5 000g (50%), 500g (5%), decline, or have him beaten (Mercy −1). |
| **The Circle Closes** | Leave village | Ashen Spawn surround you. Embrace the cold (become Ashen), run (Athletics check), fight (best blade skill check), or burn them with magic (age 3 days). |
| **Ash in the Dream** | Leave village | A dream reaches out to you. Accept (become Ashen), refuse, or inquire (30% wounded / 20% become Ashen / 50% free focus point). |
| **Three Figures at the Crossroads** | Leave village | Three witches invite you. Join (−2 years, Honor/Mercy −2), ride past (nothing), or scatter them (free focus point; 50% cursed: +1 year). | Encounter chance: 10% per settlement transition; 14% per field battle; 22% per siege or raid.

---

## Taking the Lord's Coin — Soldiering

You do not need a great name or a full retinue to make war pay. Even at **clan level 0**, you can hire your entire party out to a warring commander as a common soldier — a step below the mercenary contract (which still wants clan tier 1) and available far earlier.

**Who will take you.** Speak to any lord leading a party and choose *"I would take your coin and march under your banner…"*. The option appears only when:

- The lord's realm is **at war with a real rival** — a living rival kingdom, not merely the Ashen.
- That realm is **not at war with your own faction**.
- The lord is **not one of the Ashen** (they take no hired swords).
- You are **not already sworn to a realm** (vassal or mercenary elsewhere).

**The terms.** You agree a length of service — a season (**21 days**), a half-year (**42**), or a full campaign year (**84**) — and the commander names your weekly wage up front.

**While you serve:**

- Your party **rides with the commander automatically and fights his battles**. When he holes up in a town or castle, you **wait there** with him.
- You are counted a **mercenary of his realm**, exactly like a contract — with no lasting allegiance once it ends.
- **Every week** you are paid your party's full upkeep plus a small bounty that grows with your clan's standing, drawn from the commander's coffers (or the realm's, if he is short of coin).

**Leaving.** Striking out on your own — **clicking anywhere on the map** — ends the service:

- **Before your term is up, that is desertion.** You are **warned first**; if you go through with it your **crimes against the realm rise** and the commander **thinks the worse of you**, the more heavily the more of the term you leave unserved.
- **Serve the full term** and you are released cleanly, with a **parting bonus** and their thanks.
- You can also settle up **face-to-face** with your commander in conversation (*"About my service under your banner…"*).

If your commander is slain or captured, your oath ends with him.

---

## Schemes and Betrayals

When visiting any city, talk to the **Tavern Keeper** and choose *"I have some shadier business that needs arranging."* A scheme menu opens letting you plan covert operations against lords or settlements.

### How it works

1. **Choose a scheme** — pick from the list, which shows your success chance and cost.
2. **Choose a target** — lord (for lord-targeted schemes) or settlement (for city/castle schemes).
3. **Confirm** — pay gold and influence upfront. The scheme executes in **1–3 campaign days**.
4. **Result** — success applies the effect; silent failure leaves no trace; exposed failure hits relations hard.

### Personality cost

Requesting **any** scheme costs **Honor −1 (Dishonorable)** and **Calculating −1 (Devious)** — paid immediately on confirm, before the scheme resolves. Ordering an **assassination** also costs **Mercy −1 (Merciless)**. All costs are shown on the confirmation screen before you commit.

### Failure outcomes

- **70% of failures** — **Agent fled**: brief notification only. No trace, no consequences.
- **30% of failures** — **Agent caught**:
  - Crime rating +30–60 in the target's kingdom (only if the kingdom is not eliminated).
  - Relations −60 to −80 with the target or settlement owner (only if they are alive).
  - Assassination and Stage a Coup caught: **40% chance of war declaration** (only if both kingdoms exist, are not eliminated, and are not already at war).

**Viper's Counsel always exposes on failure** — court intrigue has no silent slip. Relations −50 to −70 with the target lord and −30 to −50 with the king, regardless of whether an agent was literally caught.

### Success formula

`baseChance + (skill / 600 × 30%) − (security / 400) − (clanTier × 2.5%)` — capped at 5–85%.

**Ashen targets**: additional −30%. Near-impossible without max Roguery/Charm.

### Gold cost

Base × `(1 + target clan tier × 0.4)` — tier 0 = 1×, tier 6 = 3.4×. Shown exactly in the target-selection UI before you commit.

### Repeat-use penalty

| Scheme | Cooldown after any attempt | Repeat within window |
|--------|---------------------------|---------------------|
| Assassinate a Lord | **14-day hard block** — cannot be queued at all | — |
| All other schemes | **7 days** | **5× base cost** |

When a cooldown expires the player receives a notification: *"Contacts reset — the path to [target] is open again"* or *"Network cooled — [scheme] may be repeated at normal cost."*

### Scheme list

| Scheme | Skill | Base gold | Influence | Base % | Effect on success |
|--------|-------|-----------|-----------|--------|-------------------|
| **Assassinate a Lord** | Roguery | 6 000 | 120 | 25% | Target lord dies. |
| **Hire an Assassin (wound)** | Roguery | 2 500 | 65 | 33% | ~20% of target's party troops wounded. |
| **Forge Documents** | Charm | 2 000 | 55 | 40% | Target lord −55 relations with their faction leader (if alive). |
| **False Accusations** | Charm | 1 500 | 40 | 45% | Target clan loses 5% renown (min 50). |
| **Stage a Coup** | Charm | 4 500 | 100 | 20% | Loyalty −40, security −35. Rebellion likely. |
| **Poison a Well** | Roguery | 2 200 | 60 | 38% | 20–60 garrison militia killed. |
| **Bribe Soldiers** | Charm | 2 200 | 60 | 32% | 20–50 garrison troops desert. |
| **Burn a Storage** | Roguery | 2 000 | 45 | 40% | Food −50%, prosperity −15%. |
| **Spread Terror** | Roguery | 1 500 | 35 | 40% | City security −25–45. |
| **Spread Rumors** | Charm | 1 200 | 20 | 35% | Loyalty −15, prosperity −8%. |
| **Viper's Counsel** ★ | Charm | 1 800 | 60 | 40% | Target clan loses 7% renown (min 50). Your clan gains 30–50 renown. **Same-kingdom lords only.** Failure always exposes. |
| **Scatter the Wolves** | Roguery | 2 500 | 50 | 35% | Spawns 5–8 bandit/deserter parties across the target lord's entire kingdom, each anchored to a hideout. |

★ *Viper's Counsel can only target lords within your own kingdom.*

### Arrange covert business (city menu)

In any town, a direct **"Arrange some covert business"** option is available in the main town menu — no tavern dialogue required. Conditions and costs are identical to the tavern route.

### Debug mode

Press **Ctrl + Shift + F10** on the campaign map to toggle scheme debug mode. While active, all schemes cost nothing and always succeed. When toggled **on**, this also queues **The Temple** event to fire on the next weekly tick (if it hasn't fired yet). Toggle again to restore normal mode.

### Balance notes

- One scheme in flight at a time. Schemes and campaign map spells share the same gold and influence pool — using one limits what you have for the other.
- The UI shows exact tier-scaled cost and any active repeat penalties before committing.
- Crash safety: eliminated kingdoms cannot receive crime rating or war declarations; dead heroes cannot receive relation changes. All checked before applying.

### Counter-intelligence

NPC lords can and do scheme against the player and player-owned fiefs. Two defences exist:

- **Warning whispers** — when a plot is queued against you or your fiefs, there is a chance you receive a vague warning (30% base, scaling with Roguery up to 75%).
- **Sweep the city for hostile agents** (scheme menu, 500g) — pays informants to comb the underworld. If a plot is in motion, a Roguery check (40–85%) cancels it and names its author (+300 Roguery XP); on failure the plot proceeds. If nothing is in motion, the coin buys only rumours — probing blind has a real cost.
- The **Clairvoyance** campaign spell also reveals a pending plot and offers to sever it for 2 000g.

When an NPC scheme resolves against you, a 1-day **retaliation window** opens: all your schemes cost half price.

### NPC lords

A random NPC lord may initiate a scheme each day — at most one new scheme launches per day globally. Each lord has a 20–35 day personal cooldown between schemes. NPC scheme results appear in the campaign message log (not as popup notifications) — unless the scheme targets you, in which case the result is shown as a popup.

- **Standard lord and settlement schemes** can target lords from any foreign kingdom — not just current enemies. Schemes work in peacetime too (intelligence operations, sabotage, court intrigue).
- **Ashen targets** are valid but uncommon (15% weighting when non-Ashen targets exist) and face an additional −30% success penalty.
- **Viper's Counsel** (NPC) targets a rival clan within the same kingdom — court intrigue runs both ways.
- **Scatter the Wolves** (NPC) targets a lord in any foreign kingdom, flooding it with bandits.

---

## The Last Flight of the Dragons *(retired — cannot start in The Darkest Night)*

> **This is not the main quest, and it will never begin.** Ash and Ember's campaign-ending questline was retired in v0.6.0 (`DragonQuestSystem.DormantForDarkestNight`): its premise — a First Emperor's soul shattered into a world-ending cycle — has no place in this world, which already owns that narrative slot with **the Demon Lord endgame** (see *The clock of the apocalypse* above). Its sibling, *The Hunger of the Void* (`AshenQuestSystem`), is retired the same way.
>
> Both are documented below as reference only. A quest already in progress in a save from an earlier build still resolves normally — nothing was deleted, only the trigger was gated off.

*"There is a way to rekindle the world. One great burning — everything, at once."*

### Trigger *(gated off — retained for saves that already started it)*

Defeat an Ashen lord's party for the first time. A dying old mage approaches you. He has been looking for someone for forty years.

### Goals (active after accepting)

| Goal | Condition |
|------|-----------|
| Establish dominion | Reach Clan Tier 6 |
| Enter the cold heart | Capture Tyal |
| Gain the power | Reach Hero Level 25 |

Progress is tracked in the grimoire (Alt+X).

### Resolution

When all three goals are met, a final prompt appears. You may:

- **Rekindle the world** — pour everything into a single great burning. All Ashen lords, all mage lords, and all mage companions die. All Ashen settlements are redistributed. World map events cease. The player hero dies — game over.
- **Refuse** — the chance passes forever. The game continues.

Refusing the old man at the initial encounter permanently closes the quest.

---

## Questline — The Burning Laboratory

*"Someone was experimenting with creating life from the fire."*

### Trigger

Win a siege as the attacking side. The event cannot fire before **campaign day 80**, and becomes very likely by day 300. It fires **at most once per campaign**.

### Discovery

Inside the captured keep you find a hidden laboratory stocked with scrolls describing forbidden experiments — creating life from ash and fire. Whoever built this place came close to finishing it.

You are given eleven options (minus those whose faction or leader has been eliminated):

| Choice | Effect |
|--------|--------|
| **Destroy it** | +Honour. Quest ends. |
| **Keep it** | Starts **Questline C**. |
| **Sell it** | +10 000 gold, −Honour. 50 % chance the buyer is an imperial contact → starts **Questline A** with a random imperial faction. |
| **Give to Rhagaea** *(empire_s alive)* | Starts **Questline A** with the Southern Empire. |
| **Give to Lucon** *(empire_n alive)* | Starts **Questline A** with the Northern Empire. |
| **Give to Garios** *(empire_w alive)* | Starts **Questline A** with the Western Empire. |
| **Give to Sturgians / Khuzaites / Battanians / Aserai / Vlandians** | Starts **Questline B** with that faction. |

---

### Questline A — The Resurrection of Arenicos

The receiving faction's scholars attempt to revive the dead Emperor Arenicos.

**Timeline:**

| Delay | Event |
|-------|-------|
| +3 days | The rituals begin in secret. |
| +10 days | Arenicos is revived — he possesses a random male lord of the receiving empire and is made its faction leader. His nature (true emperor or Ashen spirit) is determined secretly. |
| +3 days | Each other empire faction may submit (one is guaranteed, the rest have a **50% chance**) — a submitting empire's clans and fiefs are absorbed into Arenicos's empire. |
| +3 days | Arenicos declares war on all non-imperial factions. |

**True Emperor:** Fights all non-imperial factions and the Ashen.

**False Emperor (Ashen spirit):** After 50 more days, the empire secretly allies with the Ashen — peace is enforced daily.

**If Arenicos dies:** His empire's settlements are distributed randomly among the surviving imperial factions (empire_s, empire_n, empire_w). If the Ashen had already merged with his empire, the Ashen clans withdraw to their own kingdom instead and the empire endures.

---

### Questline B — The Faction's Gambit

The receiving faction studies the scrolls and attempts the rite. After 3 days, one of three outcomes is rolled (equal probability):

| Outcome | Effect |
|---------|--------|
| **They discard it** | Nothing further. Quest ends. |
| **Goes badly** | The faction is consumed by the Ashen. Every town and castle flips to the Ashen kingdom — one settlement every 3 days until the faction is gone. |
| **Goes well** | Every week, each lord army in that faction gains **30 tier-4 troops**. However, each week there is a **20% chance** the gift collapses into the "goes badly" outcome. |

---

### Questline C — Personal Rites

You keep the scrolls. Every **7 days** a prompt appears:

| Choice | Effect |
|--------|--------|
| **Discard the book** | Quest ends peacefully. |
| **Perform a rite** | +50 Renown · large XP gain (Athletics, Medicine, Roguery, Leadership, Charm) · −Honour · **5% chance of becoming Ashen** |

The weekly prompts continue indefinitely until you discard the book or the Ashen transformation occurs.

---

## The Ashen Kingdom

The Ashen chose cold over death. They do not age, they do not negotiate, and they are permanently at war with every other kingdom.

### Ashen settlements

At campaign start the following settlements are assigned to the Ashen Kingdom. Their garrisons are filled with high-tier troops immediately. All stats are locked daily to maximum.

**Core cities:** Tyal, Sibir, Baltakhand, Amprela  
**Castles and towns:** Urikskala, Kaysar, Dinar, Vladiv, Varnovapol, Tepes, Epinosa, Takor, Khimli, Lochana, Syratos  
**Additional (v1.0):** Ostican (Vlandian, with nearby castles)

**Settlement health (locked daily):**

| Stat | Value |
|------|-------|
| Loyalty | 100 (never rebel) |
| Security | 100 |
| Food stocks | Maximum |
| Prosperity | 5 000 |
| Militia | 1 500 (cities) / 600 (castles) |

**Garrisons:** 1 500 troops minimum for cities, 800 for castles. Filled with the highest-tier troop available from that settlement's culture.

### Conquest behaviour

**Ashen cities do not auto-return.** If you besiege and capture an Ashen city, it stays yours. Loyalty and security are immediately set to 100 so there is no instant rebellion. The Ashen system removes that city from its managed list.

**Extinction resurgence.** If all Ashen settlements are taken, the Ashen claim a random non-player city without warning. The cold always finds new ground.

### Ashen lords

- Do not age (birth day reset daily to ~35).
- Cast spells with no aging cost; 6-second cooldown. Spells display cold-blue and grey visuals.
- Always carry Scatter + Extinguish + BreakWills + Plague. 50% chance of Smoulder. 50% chance of Sunder. 40% chance of Immolate.
- Personality traits locked to Merciless, Closefisted, and Deceitful.
- Captured Ashen lords and Ashen Spawn party leaders refuse all dialogue. Encounters with them end with silence.

### Becoming Ashen (player)

When the player takes the cold at age 100 or surrenders to Ashen captors, their clan is automatically moved into the Ashen kingdom. Their spells change to cold-blue visuals. Criminal rating in their old kingdom spikes on departure. The Ashen kingdom is permanently at war with everyone — joining it means joining that war.

### Criminal status

Non-Ashen players are permanent criminals in Ashen lands. Ashen players have their crime rating cleared daily.

### Permanent war

Peace with the Ashen is impossible — the diplomacy AI scores it at −10 000, and any peace that does get forced through is re-declared within one in-game day. This applies to both the Ashen kingdom and any individual Ashen clan temporarily outside it. All other faction diplomacy is unmodified vanilla behavior.

---

## Campaign World Events

27 events spread across two independent weekly slots. Every 14 days (after the last event fired) the tick opens both slots simultaneously:

- **General slot** — at most one Ashen, political, or seasonal event fires.
- **War slot** — at most one inter-faction war event fires (independent of the general slot, so both can fire the same cycle).

A separate weekly safety net checks every 21 days: if no non-Ashen inter-faction wars exist at all, it directly seeds one.

### General events

| Event | Chance/week | Effect |
|-------|-------------|--------|
| **Ashen Plague** | 8% | Wounds entire garrison of a random city/castle. Spawns 3 Ashen Spawn hordes nearby. |
| **Great Withering** | 10% | Destroys 80% of a village's hearth or halves a city's prosperity. |
| **Ashen March** | 5% | Spawns 6 Ashen Spawn hordes across a random non-Ashen kingdom. |
| **Long Night** | 3% | Forces mod light-level to Dark for 7 days. Each day drains prosperity from every non-Ashen town. |
| **Ashen Tide** | 3% | A random non-Ashen castle is seized by an Ashen lord. Loyalty/security set to max immediately. |
| **Fire Fades** | 1.5% | 2–4 non-Ashen lords aged 25–55 (not clan leaders) die. Their home settlement weakens. |
| **Darkened Roads** | 6% | All caravans of a random kingdom vanish. Town prosperity drops 15%. 2 Ashen ambush parties arrive. |
| **Seeds of Betrayal** | 1.3% | A faction leader is poisoned at their own feast. The clan behind it is expelled from the realm. |
| **Broken Will** | 1% | Once or twice per campaign (after day 60): a faction is drawn into the cold and declares war on all others. |
| **The Long March** | 4% | 4 massive Ashen warbands (100+ troops each) march into Vlandia, Aserai, Khuzait, or Sturgia. |
| **Whispers from the Ash** | 1.5% | 1–3 mage lords abandon their factions and join the Ashen — gaining Ashen title, traits, and cold-fire magic. |
| **Tyranny** | 2% | A faction leader executes all tier-5/6 clan heads. Ruling clan loses all influence. One clan defects. |
| **Stolen Heirloom** | 2% | A rival clan seizes the faction seal overnight — a new ruling clan takes power without a blade drawn. |
| **Peasant Unrest** | 6% | The people of a random kingdom revolt. Three parties of 50 looters spawn near a lord's settlement. |
| **A Wolf in Sheep's Clothing** | 3% | A minor lord in a random kingdom is accused of serving the Ashen. Player gets a choice if in that kingdom (tier 4+ = 4 options; tier <4 = Charm-modified accusation risk). |
| **Mage Fatwa** | 2.5% | Religious fear sweeps a kingdom. 0–3 mage lords (non-Ashen) are hunted and killed by the mob. |
| **The Temple Rises** | 4% (after day 100, once only) | Diathma, Makeb, or Omor breaks from its faction. The city's owner clan founds The Temple — a militant holy order sworn to fight the Ashen. One more clan joins automatically. Player may join. |
| **Iron Winter** | 4% (winter only) | One random northern kingdom (Sturgia or Northern Empire) loses 50% hearth in villages and 50% prosperity/food in cities. |
| **Scorching Sun** | 4% (summer only) | One random desert kingdom (Aserai or Southern Empire) loses 50% hearth in villages and 50% prosperity/food in cities. |
| **Game of Thrones** | 5% on leader death | When a qualifying faction leader dies, the kingdom fractures: all non-ruling clans leave and become independent, keeping their fiefs. Requires 4+ clans; never fires for the Ashen. |

### Inter-faction war events (war slot — independent of general slot)

Each event picks two non-Ashen kingdoms currently at peace and rolls a war chance based on their leaders' relations (hostile: 85%; neutral: 45%; friendly: 10%). If the roll fails, relations drop instead.

| Event | Chance/cycle | What tips the balance |
|-------|--------------|-----------------------|
| **A Slight at Court** | 2.5% | An envoy is publicly turned away; the insult demands an answer. |
| **Border Torches** | 2.5% | Villages near a shared border burn; each crown blames the other. |
| **A Debt in Blood** | 2% | An old grievance resurfaces; the aggrieved party demands satisfaction. |
| **The Broken Betrothal** | 2% | A marriage alliance collapses; the spurned faction answers with steel. |
| **The Treasonous Scroll** | 2% | Documents implicating a lord in treachery surface at court. |

The Ashen are exempt from all betrayal and political-fracture events — their will is cold, singular, and does not break or scheme against itself.

### The Sanctuary

Cities owned by **The Temple** and **four randomly chosen Empire towns** (selected at new-game start, saved with the campaign) have a **Sanctuary** accessible from the town menu.

**Open access:** Any hero may approach a Sanctuary. Alignment (Mercy + Honor + Generosity) determines yield per round and effect strength. Full alignment yields ~6.5 pts/round. Zero alignment yields 1 pt/round — success is possible but requires many painful rounds for a weakened reward.

**Temple member discount:** Temple faction members reduce all rite cooldowns by 40%.

#### How rites work — the Meditation ritual

Selecting a prayer begins a **Meditation ritual**. The game secretly rolls a hidden target threshold. Each round of meditation:

1. **Costs the player** — troops are wounded or the hero ages (amount and type vary by rite).
2. **Accumulates hidden progress** — points per round = `round(roll(3–10) × mult)`, where `mult = (Mercy + Honor + Generosity) / 6`. At full alignment (+6 total) you average 6.5 pts/round. At zero alignment you always earn exactly 1 pt/round — success is slow but not impossible. The reward on success is also scaled by mult, so a zero-alignment character who grinds through earns a much weaker effect.
3. **Prompts the player** — continue with *steady devotion* (normal roll), continue with *fervent devotion* (progress ×1.5, but one round in three the flame takes the round's cost a second time), or *step back — claim what the flame offers*.

When the player stops: if accumulated progress **≥ hidden target**, the prayer fires. If not, the cost paid is lost and nothing is granted. The target number is never shown.

**Atmospheric hints** appear after each round indicating loosely how close you are ("the flame flickers", "the warmth is building", "one more push").

| Rite | Per-round cost | Effect on success |
|------|----------------|-------------------|
| **Prayer of Strength** | 8–15 hero HP | Party morale +40; blessed status (10% daily healing) for 3 days |
| **Protective Rites** | 12–20 hero HP + 1 day older | Blocks all Ashen world events for 14 days |
| **Turn the Ashen** | 15–25 hero HP | Wounds 12–20 soldiers in up to 3 Ashen parties within 200 map units; breaks morale |
| **Prayer of Healing** | 12–20 hero HP | Choice: heal all wounded troops **or** activate Steady the Line (fallen count as wounded not dead for 5 days) |
| **Prayer for a Blessing** | 15–25 hero HP + 2–4 days older | Choice: shed ~1 year (floor: age 20) **or** receive Flame Mark (+1/6 trait multiplier for 60 days) |

**Hidden target ranges** (for reference; never shown in-game):

| Rite | Target range | Avg rounds — max (all +2) | Avg rounds — typical (all +1) | Avg rounds — zero traits |
|------|-------------|--------------------------|------------------------------|--------------------------|
| Prayer of Strength | 10–18 | 2–3 | 4–5 | ~14 |
| Prayer of Healing | 18–30 | 3–5 | 7–9 | ~24 |
| Protective Rites | 22–35 | 4–6 | 8–11 | ~29 |
| Turn the Ashen | 26–40 | 4–7 | 10–12 | ~33 |
| Prayer for a Blessing | 35–55 | 6–9 | 13–17 | ~45 |

*Zero-trait heroes earn 1 pt/round; rounds needed ≈ avg target. The reward is also scaled down.*

Cooldowns (base; reduced 40% for Temple members; longer at lower alignment):

| Rite | Base cooldown | Note |
|------|--------------|------|
| Prayer of Strength | 7 days | Costs hero HP — weakens you to bolster morale |
| Prayer of Healing | 7 days | Costs hero HP — you bleed so your soldiers don't have to |
| Protective Rites | 10 days | Costs hero HP + 1 day aging |
| Turn the Ashen | 10 days | Costs hero HP — heavy drain for offensive use |
| Prayer for a Blessing | 30 days | Costs hero HP + 2–4 days aging — the heaviest rite |

**Location depletion:** after 5 ritual starts at a single Sanctuary (any mix of rites), the flame there rests for 30 days and all options are disabled. Travel to another Sanctuary to continue. The counter and recovery timer are shown in the sub-menu header.

**Altar interference:** the flame and the grey stone reject each other. Using an Ashen Altar halves Sanctuary yield for the next **30 days** (and vice versa). The remaining interference window is shown in the sub-menu header.

When Protective Rites are active, any Ashen world event that would fire instead shows a notification that the ward held. The counter ticks down daily.

**NPC behavior (simulated ritual):** NPC lords simulate 3–4 rounds of meditation when the chance fires. If their simulated accumulation meets the threshold, the effect applies.
- Honourable + Merciful lords in a sanctuary city: **0.3% chance per day** to attempt a miracle (healing or morale).
- Temple faction lords: **3% chance per day** to partially heal their wounded; **2% chance** to wound troops in the nearest Ashen party within 100 map units.

### The Temple

Sometime after campaign day 100, **The Temple Rises** fires once and permanently. One of three canonical cities (Diathma, Makeb, or Omor) breaks away from its parent faction, its owner clan founding a new militant kingdom dedicated to ending the Ashen. A second clan joins immediately. The player is offered the choice to join as well.

**The Temple** is always at war with the Ashen (the war is re-declared daily if peace is somehow imposed). It never initiates war on other factions; other factions may declare war on it.

The founding city has its loyalty and security immediately set to 100 to prevent instant rebellion. The Temple is a small kingdom — one city, two clans — and will need allies to survive long-term.

If none of the three canonical cities are eligible (already Ashen-owned, under siege, or their owner clan is unavailable), a fallback city from the Empire, Khuzait, or Sturgian factions is used instead.

#### The Covenant

Once the Temple stands, it watches players who are **not** members:

- **Covenant offer** — a clean-handed player (clan tier 2+, whisper tier ≤ 1) may be approached by a Temple envoy offering a pact. While sworn, **battle casts cost 1 fewer day of life** (minimum 1 — stacks after Tempered and Kinship), and every ~3–5 weeks the Temple **calls for aid** against the Ashen:

| Answer | Outcome |
|--------|---------|
| **Ride with the strike** | Up to 2 Ashen warbands are bloodied (10–18 wounded each, −20 morale). +50 renown, +10 relation with the High Templar, +10 party morale. |
| **Send coin (800 denars)** | +15 renown, +5 relation. |
| **Stand aside** | −5 relation. The covenant holds — for now. |

  Declining the envoy closes the offer permanently.

- **Anathema** — a mage whose whispers reach tier 3 (75+) is declared anathema: any covenant is revoked, relations with the High Templar collapse (−30 to −40), and templar zealots periodically ambush the player's column (3–8 soldiers wounded every ~2 weeks) until the whispers fade below tier 2. Redemption lifts the hunt, but the covenant is not offered twice.

### The Dark Altars and the Dark Gifts

Grey stone **Dark Altars** stand permanently in the cold cities (**Tyal, Sibir, Baltakhand, Amprela**) and in **two random Empire cities**, rolled at game start. At a Dark Altar you do not cast or hoard cold — you buy **permanent Dark Gifts** with blood.

**Who may bargain:** only the **Merciless or Devious** (Mercy ≤ −1 *or* Honour ≤ −1). Gifts you own are permanent, but they **sleep** if you ever stop being either — and wake again when you return to the dark.

**Exclusivity:** bearing even one Dark Gift bars you from **Grace** (Sanctuary) and from **Nature** (the Living Ember). The three dark/holy/wild paths cannot be mixed. You may **renounce** any gift at a Dark Altar to walk another road again.

#### The price — blood sacrifice

Each gift costs a **geometrically growing** tithe taken from your prison roster: first prisoners, then prisoners **and captured lords**. Lord-prisoners are spent first, then the lowest-tier commoners.

| Gift # owned | Prisoners | Captured lords |
|---|---|---|
| 1st | 5 | 0 |
| 2nd | 12 | 0 |
| 3rd | 25 | 1 |
| 4th | 50 | 2 |
| 5th | 80 | 4 |
| 6th | 130 | 6 |
| 7th | 200 | 9 |
| 8th+ | 300 | 12 |

#### The boons (all passive)

| Gift | Effect |
|------|--------|
| **Iron Veil** | −10% incoming damage. |
| **Dark Strike** | Each melee hit erupts for +20 dark damage. |
| **Soul Mirror** | Reflects 20% of melee damage back at attackers. |
| **Dark Spirit** | A dark shade hunts the enemy each battle (≈25 damage every 4 s). Buy up to **3**. |
| **Pale Rider's Curse** | Every horse within 5 m of you dies each second. |
| **Soul Drain** | Each melee hit saps 30 morale from the victim. |
| **Blood Pact** | Each kill restores 12 HP to you. |
| **Dread Presence** | Every 3 s, enemies within 8 m lose morale and may rout. |

**NPC gifts:** Ashen lords carry 1–2 gifts (often Soul Drain and a Dark Spirit); other genuinely evil lords occasionally carry one. Seeded at battle/encounter time and re-rolled each session.

> Note: the old Ashen-Altar **rituals** (Blood Tribute, Ashen Solstice, Cold accumulation, etc.) and the talents tied to them have been **replaced** by this permanent-gift system.

### Player-interactive world events

Three events prompt the player for a choice if their clan is **tier 4 or higher** and **in the affected kingdom**. The dialog appears before effects are applied.

| Event | Support the schemers | Oppose the schemers |
|-------|---------------------|---------------------|
| **Stolen Heirloom** | +50 relations with the usurper clan, −100 with the displaced ruling clan. | −100 with the usurper clan, +20 with the old ruling clan. **33% chance** the coup fails outright. |
| **Seeds of Betrayal** | +50 with the conspiring clan, −100 with the old ruling clan. | −100 with the conspiring clan, +20 with the old ruling clan. **33% chance** the plot is stopped and the leader survives. |
| **Tyranny** | +100 with the tyrant, −50 with every condemned clan. | **33% chance** the player is added to the execution list (game over). |

If the player's clan is below tier 4, or is the direct party in the event (the ruling clan being displaced, etc.), the event fires silently as normal.

---

## New-Game Settlement Reassignments

At campaign start, several settlements are moved to better reflect the world state:

| Settlement | From | To |
|------------|------|----|
| Marunath + castle_B5, castle_B2 | Battania | Northern Empire |
| Jaculan + castle_V2, castle_V7 | Vlandia | Western Empire |
| Seonon + nearby Battanian castles | Battania | Northern Empire |
| Razih + nearby Aserai castles | Aserai | Southern Empire |
| Ostican + nearby Vlandian castles | Vlandia | Ashen Kingdom |

All transferred settlements have loyalty and security set to 100 immediately.

---

## Building from Source

### Requirements

- .NET SDK 6 or later
- A local Bannerlord installation

### Environment variables

| Variable | Value |
|----------|-------|
| `BannerlordPath` | Path to your Bannerlord root (folder containing `bin` and `Modules`) |
| `BannerlordBin` | `Win64_Shipping_Client` (Steam) or `Gaming.Desktop.x64_Shipping_Client` (Xbox) |

```powershell
$env:BannerlordPath = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
dotnet build src\TheDarkestNight.csproj
```

Output: `src\bin\Debug\TheDarkestNight.dll`. The build copies it to the Modules folder automatically.

---

## Troubleshooting

**"The fire does not stir in you."**  
You have not opened the Spellbook yet — spend the one-time focus-point cost (see **The Spellbook** above), or pick "a stranger's book" as your Keepsake at character creation to start with it unlocked.

**"Both hands are full. Free a hand to shape the fire."**  
You are wielding a weapon or shield. Press **X** to sheathe everything, then cast.

**Spells fire but nothing happens**  
You may be in a tournament (casting kills you), in a prisoner state, or you mixed form keys incorrectly.

**Ashen settlements show as unclaimed for the first day**  
Expected. Ownership is asserted on the first daily tick.

**A conquered Ashen city keeps getting taken back**  
This was fixed in v1.0. Conquered Ashen cities now stay conquered permanently. If you are still seeing this, verify the DLL version matches this README.

**The Ashen have disappeared entirely**  
The extinction resurgence fires automatically — a random city will fall to the Ashen within a few in-game days.

**Script reports "Could not auto-detect your Bannerlord installation"**  
Pass the path manually: `.\install.ps1 -BannerlordPath "D:\Games\Mount & Blade II Bannerlord"`

---


## Changelog

The full release history lives in [`CHANGELOG.md`](CHANGELOG.md) — including the Ash and Ember era (v0.11 onward), which was previously archived here.
