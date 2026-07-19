# The Darkest Night — Changelog

---

## v0.10.0

### The Scrived Word — casting is runes now, not fixed formulas
The player's magic is no longer a lookup against a table of fixed 5–20-mark
strings. Mages **draw runes in the air and bind them into sentences**. Same
input — hold Left Alt (or Controller-RLeft), tap directions, release — but each
**rune is exactly three marks** of U/D/L/R, and 36 of the 64 triplets are real.

- **Every rune means something and does something alone.** Release after one rune
  for its bare working (Cinder = a burst of flame, the Bar = a wall, the Calling =
  a summoned elemental, the Mending = a heal…).
- **Repetition amplifies** — write Cinder twice for a longer, harder burn.
- **Runes combine into a language.** Two elements marry into a fusion (Fire+Water =
  Fog); three become a **Triad** (Fire+Wind+Water = *the Tempest*); all four, the
  **Unbound Weave** — the mightiest working, and it always bites its caster back.
  A Form reshapes the whole binding: the **Long Mark** throws it, the **Bar** raises
  a wall, the **Calling** gives it a body.
- **Discovery** — draw a real rune you've never used and it writes itself into your
  book on the spot. A malformed or senseless binding fizzles and risks a *spellburn*.
- **Strain** — the longer the binding, the greater the risk it burns you even when
  it lands. Short bindings are safe craft; a six-rune Triad is a master's gamble.
- **Composed names** — every successful binding names itself ("Fireball", "Fog Wall",
  "Twice-Written Cinder", "The Tempest").
- **The book** now shows *The Marks* (every rune you know, with its shape and
  meaning) and *The Craft* (a primer on the grammar).

Learning sources all grant runes now: the **"a stranger's book"** background starts
you with two (an element plus a safe first lesson), the **Tower** teaches them for
influence, and **ruins** yield them cut into the old stone. Old v0.9 saves migrate
automatically — your known formulas resolve into the runes that compose them.

Wands, the Chosen's Rod, and NPC caster lords/troops keep their bound workings
unchanged; demons and the Bloodbound's blood-attunement still loose raw element
cones, by design.

---

## v0.9.0

### The Veil turns — a season-long tide of magic and dark
A new membrane hangs between the living world and the underworld below, and it **thins and thickens on a fixed rhythm — one full turn each season** (21 days), split into three seven-day windows. The turn is deliberately **predictable**, so you *and* the NPC world can plan around it. Every season plays out the same: **the Warding → the Steady → the Thinning**, then the Veil snaps shut and it begins again. A short passage in the message log announces each turn and how long it holds.

- **The Thinning** *(veil thin)* — the fire in all things runs high and the cracks between worlds widen: **magic ×1.5**, and the Night crosses in **greater numbers (demon spawns ×1.2)**.
- **The Steady** *(veil even)* — the ordinary balance: magic ×1.0, demons ×1.0.
- **The Warding** *(veil thick)* — the inner fire gutters low, but far fewer of the dark can force a crossing: **magic ×0.7**, **demon spawns ×0.6**. The season to march.

The tide moves the whole world, not just the player:

- **The magic scaling reaches every working** — your Spellbook casts, NPC mage lords, the Awakened, and demon hellfire all strike harder in the Thinning and weaker in the Warding (folded into the shared cast choke, so it stays NPC-parity-correct).
- **Mage lords come out to play when the Veil is thin** — they cast markedly more often during the Thinning and hold their fire during the Warding.
- **Campaigning lords favour the Warding to move their armies** — while the Veil thins, even large hosts shelter from the swollen Night; while it holds thick, true armies march the quiet roads freely.

### The mod stops answering to Ash and Ember — ⚠️ delete your old folder
The mod now installs as **`Modules\TheDarkestNight\`** and ships **`TheDarkestNight.dll`**. Until now it deployed into `Modules\AshAndEmber\` and listed itself in the launcher as "Ash and Ember".

**Delete the old `Modules\AshAndEmber\` folder before playing** — otherwise the launcher lists it as a second, separate mod. Nothing of yours lives in it: campaign data is stored in the save file. Bannerlord may warn once that a save's module is "no longer present", because the module **id** changed from `AshAndEmber` to `TheDarkestNight`; the campaign loads normally, as no save key, item id, or SaveDefiner id moved.

### Every faction fields its own troops at last
The kingdoms were renamed to The Darkest Night's factions in Phase 7, but the **troop trees were never retargeted with them** — so the Wolf Brothers fielded "Northman Warriors", the Tower fielded "Duneborn Infantry", the Forest Widows fielded "Forest Clan" troops, and the Bloodbound rode as "Tribal Lancers" behind a "God-King's Vanguard". Ash and Ember's factions do not exist in this world; that codebase is a parts bin, not a setting.

- **Khuzait → the Bloodbound** — `Bloodbound X`, with the **Huntmaster's Vanguard**, the **Blooded Rider**, and the **Blood Ravager**.
- **Sturgia → the Wolf Brothers** — `Wolf X`, with the **Pack Whelp** and the **Packmaster's Own**.
- **Aserai → the Tower** — `Tower X`, with the **Tower Novice** and the **Magister's Lance**.
- **Battania → the Forest Widows** — `Forest X`, with the **Forest Whelp** and the **Champion of the Deep Wood**.
- Vlandia keeps `Templar X`: the Temple is still the Temple, so that one was never a leftover.

Troop names are display-only — read from `CharacterObject` each session and never persisted — so this is save-safe. The sturgia tree is shared with The Camp, and the battania tree with the Children of the Forest, both of which deliberately keep their original troops; the wording is chosen to read correctly for them too.

### The God-King is dead
Khuzait's ruler has been the **Huntmaster** since the Bloodbound rework, but every *live* surface still told Ash and Ember's story. The character-creation culture card was titled "Tribes of the East" and described a God-King who "takes wives from every city he puts to tribute". The Khuzait backstories swore your family to "the Apostles of the God-King". The Burning Laboratory offered its scrolls to the Tribes. Some 28 world-event strings named a God-King, his divine fire, and his tribesmen. All of it speaks as the Bloodbound now — the hunt, and blood as the only coin they still trust.

"Wives of Conquest" is reframed rather than removed: the Bloodbound are *bound by blood* and trust it over treaties, so every taken town is bound into the Huntmaster's household by marriage. The same mechanic, correctly named.

Left alone by design: the Tribes helpers in `AI/AshenCitySystem.Renaming.cs` and all of `AI/TribesDialogue.cs` are **verified uncalled** — retired and kept for save compatibility, superseded by `BloodboundCulture`/`BloodboundDialogue`.

---

## v0.8.0 — The Night Uncaged

Playtest-driven fix pass. Every item below traces to a specific reported issue; see `FIX_PLAN.md` (removed after this release shipped) for the original diagnosis.

### New campaigns actually finish setting up
The blocking "Gift" prompt at new-game start could wedge behind a loading-screen transition — and worse, ALL of new-game world setup (city-states, imperial reassignment, lord seeding) ran from inside that prompt's own callbacks, so a wedge silently skipped every one of them. The prompt is gone outright: every culture now starts identically, magic is purely learned through the Spellbook, and world setup runs unconditionally (`CampaignBehavior.Events.FinishNewGameWorldSetup`). The StoryMode gate now pushes the player back to the main menu directly instead of risking the same wedge with a blocking `ShowInquiry`.

### Character creation grants nothing but the Keepsake
Every narrative stage (Family, Childhood, Adolescence, Youth) now grants zero skill/attribute/trait bonuses — "I am a survivor" really is a background with no hidden bonus. The Keepsake stage (Young Adulthood) is the sole source of starting flavour, and a null-id bug that could silently swallow the whole pick (and abort recording every OTHER selected option in the same loop) is fixed. A confirmation message now always names what was granted. The Spellbook can also be opened on the map with a controller (X+Y), not just Alt+L.

### City-states mint from day one
Free towns no longer wait out a 3-day dead window: every faction is scoped to its starting towns and every newly-ownerless town converted to a city-state in one eager pass at new-game setup, with the daily tick remaining as the ongoing repair pass.

### The night tide actually rises, and rises hard
Demon spawning's hideout lookup was one fallback level short of the proven pattern the rest of the codebase's bandit-party spawns already use (own clan hideout → nearest hideout → **any** hideout in the world) — the missing third level was silently failing spawns. Nightly density is now a rolled intensity band (Quiet/Restless/Surge: 8–40 parties, cap raised 40→120) instead of a flat 3–6, spawn locations lean harder toward settlements and roads, and every living demon party is pinned to pure aggression on the campaign map every hour (max attack initiative, never avoids the main party) and bands together with nearby packs when no prey is in reach.

### The Ashen are retired for new games
The old immortal-villain kingdom no longer stands up on a fresh save — demons already own the night in this fiction, and a second one competing for the same space was clutter. Existing saves that already have an Ashen kingdom keep it fully functional; only the *establishment* path is gated (`LegacyContent.AshenEnabled`).

### Ruins keep their names after a reload
A reflection-set ruin name could be reverted by the engine's own text reload between session launch and the first frame — the same class of bug `AshenCitySystem`'s renames already had a fix for. Ruin castles now re-apply their appearance from the daily tick and `OnGameInitializationFinished` as a backstop.

### The barter economy actually bites
New characters start with ~50 gold instead of vanilla's 1000; lord party rosters are halved by a new `PartySizeLimitModel` factor; town markets are pruned once at session launch instead of showing vanilla stock for their first day.

### NPCs use their toys
The Empire's Schemes access had no NPC-side counterpart — `SchemeSystem.TryQueueNpcScheme` existed but nothing ever called it. An Empire lord now runs one scheme every 10–14 days. The Camp now occasionally sends out its own charter expedition, independent of the player's, purely for texture (a notification, no player-state changes).

### The intro is four sentences, not five paragraphs
*An Empire ruled all Calradia. The underworld tore open... The Empire fell... Now remnants huddle behind walls and wards...*

### NPC magic is the Spellbook now
The legacy unified-element NPC lord casters (`ElementLordAI`) are retired for new campaigns — a fresh game never seeds a lord into that system. The Spellbook's own rare casters pick up the slack: `SpellcasterLordMath.TargetFraction` raised from 7% to 15% of named lords. Nature seers and Temple priests are unaffected (their casts are shared with the Wind/Earth/Water Spellbook spells).

### The Spellbook can be inherited
When your character dies and an heir succeeds, you're now asked whether every known formula passes to them, or burns with you.

No save-breaking changes: `AshAndEmber.dll`/`Modules/AshAndEmber/`, every `aae_*` item id, and all `SyncData` keys are untouched. An existing v0.7.x save loads exactly as before, Ashen kingdom included if it already had one.

### The mod finally answers to its own name
The C# namespace is **`TheDarkestNight`** (was `AshAndEmber`) across all 432 source files, along with `TheDarkestNightSaveDefiner` and the `SubModuleClassType` entry point. Verified by a green build and 627/627 tests.

The rename had been deferred since v0.1.0 as too risky for saves. That rationale turned out not to survive checking: `SaveDefiner` persists **numeric ids**, never type names; all 544 `SyncData` keys are hand-written prefixes (`SEA_*`, `APOC_*`) with no namespace in them; and the item ids are `aae_*`, which a namespace rename never matches. Nothing in a save records the C# namespace.

What genuinely does persist is untouched and documented in `CLAUDE.md` as not-to-be-tidied: the `aae_*` item ids (they sit in player inventories), the `AshAndEmber.dll` assembly and `Modules/AshAndEmber/` folder (renaming those is a **breaking install change**, deferred to its own release), and `InternalsVisibleTo("AshAndEmber.Tests")`, which matches the test *assembly* name and is independent of the namespace.

### The documentation caught up with the mod
`README.md` had been frozen at **v0.1.0** for six releases — it is now a version sync point in `behaviour.md` (the bump touches five places, not four). It told players to tick a mod named "Ash and Ember" in the launcher and to expect an "The Inner Fire" prompt that the code titles "The Gift" — so a correct install looked broken. The retired Ash and Ember caster paths moved to **`LEGACY.md`**; they remain the authoritative description of how spells behave for NPC casters, but they are no longer presented as things you can do.

`CLAUDE.md`'s architecture map was missing six live systems entirely (`Expeditions/`, `ForeignMuster/`, `BeastsOfTheNorth/`, `Units/`, `Mage/`, `Talents/`) and undercounted the codebase by 63% (~65K/250 files → ~106K/430). It now also records v0.7.0's verified agent-scaling hooks (`Agent.SetInitialAgentScale`, `MBAgentVisuals.ApplySkeletonScale`), superseding the v0.2.0 note that claimed no safe runtime scale surface existed.

### Recovered history
`CHANGELOG.md` was missing **v0.15.0, v0.12.2, v0.12.1, v0.11.2, and v0.11.0** — its `v0.11.x` entry read *"no changelog recorded"* while README's archive quietly held the records. All five are merged back; README now points here.

### Housekeeping
47 test build artifacts untracked and gitignored (`tests/bin`, `tests/obj` — `.gitignore` already covered `src/`), taking the last `TheWitheringArt` / `ColoursOfCalradia` ghosts with them. `dist/AshAndEmber/bin/**/*.dll` stays tracked deliberately: those are the pre-built DLLs `install.ps1` installs for players who don't build from source. `behaviour.md`'s documented Bannerlord DLL path pointed at a dead Xbox GUID folder and now resolves from `$env:BannerlordPath` instead.

---

## v0.7.1 — A Watchman Who Cannot Read

No gameplay changes. This is a version bump only.

The official launcher marks the mod with a red warning — *"Couldn't verify some or all of the code included in this module."* It is cosmetic and safe to ignore. The launcher hands every enabled community DLL to `bin\ModVerifier\ModVerifier.exe` before listing it; on Xbox / Game Pass installs that tool is not shipped, so the check cannot run, and a check that cannot run is recorded as a failure. Any enabled community mod on such an install draws the same mark. Nothing in the mod, its deployment, or its load order is at fault, and there is nothing on this side to fix.

---

## v0.7.0 — The Shapes in the Dark

### Demons read as beasts, not reskinned looters
The night tide no longer wears a man's silhouette. Every lever here is built from what the engine already ships — no custom meshes, textures, or skeletons exist or are referenced:

- **Unnatural stature.** Demons are scaled at spawn through the engine's own skeleton-scale hook: Stalkers stand a head taller than a man (1.08x), Ravagers loom at 1.28x, the Hellsteed's rider sits high at 1.05x on a mount grown to 1.12x. Fiends alone stay man-sized — starved, uneven tide-fodder against which the bigger tiers read as monsters.
- **A real hitbox, not an illusion.** Ravagers spawned through the summoning path stand on a new additive `demon_hulking` Monster entry (`ModuleData/monsters.xml`, `base_monster="human"` so combat and animation stay fully compatible) with a taller, wider body capsule — physically bigger, not just drawn bigger. Purely additive; the shared vanilla `human` Monster is untouched.
- **Unnatural gait.** Each tier moves wrong for its bulk, reasserted on the same relentless cadence as the charge order: Fiends and Stalkers run faster than any man (1.10x / 1.20x — prey-driven, always hunting), Ravagers grind forward slower but unstoppable (0.95x), and the Lord is fast *despite* his mass (1.05x). Demons also never settle into the relaxed human idle sway — the body stands wrong between kills.
- **Beast heads.** Stalkers wear the vanilla Battanian wolf-head trophy and Ravagers (and the Lord) the bear-head — full head-replacing meshes that kill the human-head read at any distance, tinted near-black with the rest of the hide.
- **The horse is a demon too.** The Hellsteed's mount is now scaled, wreathed in the same bone-bound smoke, ember-lit, and contoured like its rider — an unnaturally large, smouldering beast, not an old nag carrying a monster.
- **The Lord commands more than one working.** The Demon Lord now casts in battle: hellfire alternating with a Spirit nova on a short cooldown — the Night itself pressing in — while Ravagers keep their hellfire cone unchanged.
- **The warp — bodies that never sat right.** Each tier is disfigured per-bone through the engine's own skeleton-scale channel (the same `ApplySkeletonScale` mechanism Native's `skeleton_scales.xml` uses to reshape its horses — every value inside the vanilla-proven 0.8–2.1 envelope): the Fiend is *the Starved*, a swollen head and overgrown grasping hands on mismatched, lopsided arms; the Stalker is *the Long-Armed*, stretched neck and arms a hand too long over a gaunt chest, hands splayed into claws; the Ravager is *the Mass*, all shoulders and forelimb under a head too small for the frame; the Hellsteed's rider is drawn thin and stretched over the saddle. Wielded weapons are exempted from the hand-bone scaling so the cleaver never balloons.
- **The face never rests.** Every demon wears a permanent bared-teeth snarl — real shipped facial-animation ids, looped for the demon's whole life.
- **The Demon Lord is the uncanny one.** Where every lesser tier is openly bestial, he is *almost* a man — and that is the horror. His bare human face shows (no trophy skull), he stands only a half-head too tall (1.12x, on the ordinary human capsule — fighting him feels like fighting a man, right up until it doesn't), his proportions are off by exactly the amount the eye can't name but can't stop noticing (a neck a shade long, arms that don't quite match, fingers a knuckle past right), and through everything he wears a gentle, unbroken smile — the calmest face on the field, and the wrongest.
- **The Jotunn-Blooded stands giant-tall at last.** The Wolf Brothers' giant recruit — whose original implementation noted, correctly at the time, that no runtime agent-scale hook was known — now uses the same verified skeleton-scale hook the demons ride: 1.32x in the flesh, the tallest thing on any ordinary field, looming over even the Ravager.

Save-safe throughout: no troop id, savedata key, or tier enum changed; the Monster entry and head-slot equipment are additive; all engine access degrades to a logged no-op under `ModLog` if another mod interferes.

### The Kindled are now the Awakened
The elemental beings' family name loses its fire-flavoured "Kindled" (a leftover of Ash and Ember's one-fire cosmology) for the element-neutral **Awakened** — the land's own magic, pooled too thick and too long, woken and walking. Every player-facing string follows: the six sacred-site troops ("Awakened of Stone/Frost/Sand/Flame/the Tide/the Gale"), the wilds rumour lines, sacred-site menus and talents, miracle text, the Forest questline's donation lines, and the mid-battle event once called "The Kindling" — now **"The Waking"** ("the ground itself wakes and takes a side"). The flame-kind wild band is named "Flame-Born", matching its Frost-/Sand-/Stone-Born kin. All troop ids, save keys, and code identifiers are untouched — a mid-save Kindled loads as the same being under its new name.

### The Great Awakening speaks for the Tower now
The Great Other questline was still telling Ash and Ember's story — "Duneborn" (a faction name that no longer exists; the Aserai are the Tower) and its "Sheikh." The quest already keyed on the Aserai kingdom mechanically, so it always *worked*; now its words match: it is **the Tower** that has reached beyond the Sands and touched something that answered, its **Archmagister** from whose mouth you hear the terms, the Tower's kingdom that must fall — or be served — before the count of ten thousand is paid at the Dark Altar. No trigger phase or save key changed.

### The Great Awakening is now the Tower's second act
Re-theming left the Tower carrying two parallel day-50 dark-rite stories — the Unbinding Rite ("close the way") and the Great Awakening ("crown what comes through it"), both started by the same Archmagister in the same season, contradicting each other. They are now one arc: **the Great Awakening's discovery roll only opens once the Unbinding Rite has concluded** — failed and its demon host burned out, or the Tower broken before the rite was ever performed. *"We tried to shut the way, and the way taught us it cannot be shut — so we read on, and reached through instead."* A late fallback (day 400) keeps the questline reachable in a campaign that never engages the Rite, using the original discovery text. Save-safe: no phase or save key changed; a save with the Great Awakening already discovered or active is untouched by the new gate.

---

Older entries: see CHANGELOG_ARCHIVE.md
