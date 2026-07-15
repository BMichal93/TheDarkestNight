# THE DARKEST NIGHT — Build Prompt for Claude Code

> Paste this whole file as the task, or tell Claude Code: *"Read PROMPT_THE_DARKEST_NIGHT.md and execute it phase by phase, starting at Phase 0."*

---

## Mission

You are building **The Darkest Night**, a total-conversion mod for Mount & Blade II: Bannerlord.

**The pitch:** The world shattered overnight. Demons crawled out from the underworld, slaughtering the unprepared people of the Empire. Now people tremble behind high walls and protective spells, and demons rise every night to hunt the living.

This repository is a **full copy of the AshAndEmber mod (v0.38)** — a mature, working magic-overhaul codebase (~200 C# files, ~62K lines under `src/`). It is your **baseline and parts bin**. You must **reuse as many existing systems, assets, patterns, and mechanics from it as possible**, repurposing them instead of writing from scratch. `CLAUDE.md` describes the code; `behaviour.md` describes how to work on it. **Read both, plus `README.md`, before writing any code.**

---

## Non-negotiable working rules

These rules exist to keep you from making errors. Follow all of them, always.

1. **Work strictly phase by phase**, in the order below. Do not start a phase before the previous one builds, tests green, and is committed. One phase = one or more commits with clear messages.
2. **Explore before you edit.** At the start of every phase, read the files listed in its "Reuse" section end-to-end before changing anything. Never modify a file you have not read.
3. **Never guess a TaleWorlds API.** The Bannerlord assemblies do not match intuition. Before calling any TaleWorlds type/method you are not certain of, `grep` this codebase for an existing usage and copy its exact form. If no usage exists anywhere in `src/`, verify against the DLLs (see `behaviour.md`) or choose a different, already-proven approach. The "Confirmed gotchas" table in `behaviour.md` is mandatory reading.
4. **Build and test after every phase**: `dotnet build src/TheDarkestNight.csproj` (needs `BannerlordPath` env var) and `dotnet test tests/AshAndEmber.Tests.csproj`. A green build is not enough on its own — the test project failing to *compile* silently disables the suite, so always run the tests too. If the game DLLs are unavailable in your environment, still run the pure-logic tests and state clearly in the commit message that the game build was not verified.
5. **Clean code, SOLID, separation of concerns, testability** — this is an explicit requirement of the project:
   - One system per folder under `src/`; split large classes into partial classes by concern, exactly like the existing code (`SchemeSystem.Execution.cs`, `SpellEffects.Battlefield.cs` style).
   - **All numeric/tuning/decision logic goes in a pure `*Math.cs` file** (no TaleWorlds types) with unit tests in `tests/PureLogicTests.cs`. Every new system you write must have a pure math file and tests for it. If a "pure" method needs a game value, pass it in as a parameter.
   - Single responsibility: campaign ticks live in the `CampaignBehaviorBase` that owns the concern; battle effects in mission-side classes; data catalogs in pure static catalog classes (see `MiracleCatalog`, `CrystalCatalog`, `AshenRuinDefs` for the pattern).
   - Depend on abstractions where the codebase already does; do not invent new frameworks — match the existing architecture.
6. **Mod-conflict safety:** null-guard `Campaign.Current` / `Mission.Current`, wrap TaleWorlds singleton access in try/catch, and **never** write a bare `catch { }` — always `catch (System.Exception logEx) { ModLog.Error(logEx); }`.
7. **Prefer the simple working solution** over the clever fragile one. When a requirement says "if possible / if not feasible, do X instead", actually test feasibility with a small spike first, then commit to one path and note the decision in the commit message.
8. **Names and text must be climatic, mysterious, lore-friendly** — the tonal inspirations are **Dark Souls and Gothic** (the Piranha Bytes RPG series: a harsh, broken world of scavengers, camps, and uneasy factions), with a Game of Thrones sense of grit. No placeholder text ships.
9. **Keep a running checklist.** The "Requirement traceability" table at the bottom of this prompt lists all 33 requirements. After each phase, update your todo list against it. Nothing may be silently dropped.
10. Where this prompt names a settlement the game does not know (e.g. "Aragon", "Otysia", "Lycarob", "Chaikland", "Akalat"), **match it to the closest real Bannerlord settlement id** (Argoron, Ortysia, Lycaron, Chaikand, Akkalat) and use the real id. Match settlements **by name at session launch** the same tolerant way `SeaCampaignBehavior` matches ports (a failed match logs and degrades, never crashes).

---

## Asset-reuse map (memorize this)

| New feature | Reuse this existing AshAndEmber system |
|---|---|
| Demon creatures (bodies, auras, tint, battle AI) | `src/Elementals/` — `ElementalFactory`, `ElementalBeings`, `ElementalVisuals`, `ElementalWildsBehavior`, `troops.xml` `elemental_being` pattern |
| Demon faction at permanent war, no diplomacy | `src/AI/AshenDiplomacyModel`, Ashen kingdom logic in `src/AI/AshenCitySystem.*` |
| Spell casting by keystroke/stick sequence | `src/Miracles/MiracleInputHandler` (hold key, tap U/L/R/D sequence, release to cast) + `MiracleMinigame` |
| Spellbook / codex UI + keybind | `MagicLearning` codex (Alt+L), litany list (Shift+X), grimoire reference (Ctrl+X) — see `MagicSystem.OnApplicationTick` and `MiracleInputHandler` header comments |
| Battle spell effects (fire bolt, walls, gale, torrent, entangle, nova, ultimates) | `src/Magic/ElementSpellEffects` (`CastAttack` is the single dispatch choke point), `ElementWallWards`, `ElementUltimates`, `src/Nature/NatureEffects` |
| NPC lords casting in battle | `src/AI/ColourLordAI`, `NpcCastPlanner`, `BanditMageAI` |
| Caster troops in the field | `src/Miracles/PriestTroops` |
| Magical item effects | `src/Crystals/CrystalCatalog` + `CrystalEffects` (consumable → make permanent-but-weaker), `src/DarkGifts/DarkGiftBattleEffects` (per-hero → make per-item-and-weaker) |
| Ruins/dungeon crawling | `src/AshenRuins/` — `AshenRuinDefs` (chamber catalog), `AshenRuinSystem`, `AshenRuinMenus`, `AshenRuinMath` |
| City-states that never join kingdoms | `src/AI/AshenCitySystem.*` (setup, renaming, maintenance, persistence) |
| "Donate a huge amount of X to a city" questline | `src/GreatAwakening/` (Duneborn Great Awakening) — trigger, NPC contribution, altar, resolution, opposition |
| "Gather resources in one town, can be stolen" questline | `src/NorthmenStones/` (Standing Stones) — including its Invasion and Ending partials |
| Faction renames + dialogue + culture overrides | `src/AI/TempleCulture`, `TribalCulture`, `DunebornCulture`, `ForestClansCulture`, the `*Dialogue.cs` files, and `OnGameInitializationFinished` culture-text re-application in `MagicSystem.cs` |
| Character creation rework | `src/AI/CreationBackstoryRework.cs` |
| Campaign world events / escalation clock | `src/QuestSystems/CampaignMapEvents.*` (slot queues), `SettlementEncounters.*` |
| Intro/splash/loading screens | `src/Startup/` |
| Ritual "sacrifice per round" menus | `src/Sanctuary/SanctuaryCampaignBehavior`, `src/AshenAltars/` |
| City menu options pattern | `Crystals/CrystallinesCampaignBehavior.Menus`, `AshenRecruitCampaignBehavior.Menus`, `Sanctuary/*.Menus` |
| Keep as-is (do not break!) | Dice games + drinking (`src/Tavern/`), sea travel (`src/Sea/`), schemes (`src/Schemes/`) |

---

# THE PHASES

## Phase 0 — Orientation and new identity

**Goal:** know the codebase; the mod presents itself as The Darkest Night; Sandbox-only.

1. Read `CLAUDE.md`, `behaviour.md`, `README.md` (at least the structure + systems sections), and skim `SubModule.xml`, `src/MagicSystem.cs` top-to-bottom.
2. Get a green baseline: build + run tests before touching anything. Fix nothing yet; just record the baseline state.
3. Rebrand the module: `SubModule.xml` (and `dist/AshAndEmber/SubModule.xml`) → mod name **"The Darkest Night"**, its own module id and version `v0.1.0`. Keep the entry class working; do not mass-rename namespaces yet (that is Phase 15, deliberately last).
4. **Requirement 28 — Sandbox only:** disable/hide the "New Campaign" (StoryMode) option so only Sandbox is playable. Investigate how the module list / game-start menus can be filtered (check how `Startup/` already manipulates the launcher-adjacent flow); the acceptable minimum is intercepting and blocking campaign start with a clear message.
5. **Requirement 27 — intro:** rewrite `src/Startup/AshEmberSplash.cs`, `AshEmberLoreIntro.cs`, `AshEmberLoadingScreen.cs` text for the new lore (the Long Night, demons rising nightly, humanity behind walls and wards). Keep the mechanism, change the words.
6. Commit.

**Acceptance:** builds, tests green, mod presents as The Darkest Night, campaign mode blocked, new intro text in place.

---

## Phase 1 — The demons (Requirements 1, 2, 7)

**Goal:** demonic armies spawn everywhere at night, vanish at dawn, and behave like a mindless, merciless flood.

**Reuse:** everything in `src/Elementals/` (this is your template — the Kindled are already skeleton-riding, particle-tinted, weaponless chargers built at runtime), `ModuleData/troops.xml`, `AshenDiplomacyModel`, `ElementalWildsBehavior` (roaming band breeding + `ELEM_*` persistence pattern).

1. Create `src/Demons/` with, at minimum: `DemonFactory.cs`, `DemonCatalog.cs` (pure data), `DemonSpawnCampaignBehavior.cs`, `DemonBattleBehavior.cs`, `DemonMath.cs` (pure) + tests.
2. **Requirement 2 — the bodies.** There are **no demon textures or meshes in this project and none will be provided** — every demon must be assembled from what the game already ships: **human bodies/armour pieces and horse meshes**, re-dressed and re-tinted at runtime. The Kindled prove this works: `ElementalVisuals` makes an ordinary human body read as a creature purely through bone-bound particles, follower lights, and a coloured contour. Do the same — dark, ragged human gear + smoke/ember particle shrouds + red-black contour = a demon; a horse under the same treatment = a hellsteed. Do not reference any texture, mesh, or asset id you have not confirmed exists in the game or in `ModuleData/`. Demon troops in `ModuleData/troops.xml` modeled on `elemental_being`:
   - **Humanoid-skeleton demons** ("Fiend", "Stalker", "Ravager"-type tiers): strong melee claw attacks (bare-handed or claw-like weapon, high damage), some with magical attacks (reuse `ElementSpellEffects.CastAttack` on a cooldown exactly like `ElementalBeings` looses its element cone).
   - **Horse-skeleton demons** ("Hellsteed"/hunting-beast type): mounted or beast units using the horse skeleton.
   - **Palette: dark grey, red, black.** Reuse `ElementalVisuals`' bone-bound particle systems, follower lights, and coloured contour — re-tuned to smoke-black bodies, ember-red glow. Bind particles **once** to skeleton bones as `ElementalVisuals` does; never re-stamp per tick.
   - **Environment variants:** slight stat/visual variation by terrain/culture region (snow demons paler and hardier, desert demons faster, forest demons stealthier…). Put the variant table in `DemonCatalog`/`DemonMath` (pure, tested).
3. **Demon faction.** A campaign-level demon faction permanently at war with everyone, no diplomacy possible — copy the `AshenDiplomacyModel` approach. Demons hold no settlements.
4. **Requirement 1 — the night tide.** `DemonSpawnCampaignBehavior`, hourly tick:
   - At nightfall, spawn demon parties **across the whole map** (near settlements, roads, wilderness). Aggressive, strong in numbers. Party counts/sizes in `DemonMath` (tested).
   - At dawn, despawn all night-spawned demon parties (they "sink back below"). Persist nothing that must not be persisted; follow the `ELEM_*` key pattern for anything that must survive save/load.
5. **Requirement 7 — demon behaviour.** Enforce all of:
   - a/ **Never retreat** and b/ **never escape** — on map (never flee from stronger parties) and in battle (always CHARGE, like the Kindled; wire through the mission behaviour).
   - c/ **Never strategize** — no manoeuvring AI; pure charge.
   - d/ **Replenish in the night** — parties that survived a day regain numbers at each nightfall.
   - e/ **Rarely attack cities** — small nightly chance a demon horde assaults a town/castle; it must actually happen from time to time. Chance in `DemonMath` (tested).
   - f/ **Never negotiate, never make peace, never release captured lords.**
   - g/ **Execute every captured hero and troop.** When demons take prisoners, roll an on-the-spot escape chance per hero (in `DemonMath`, tested); those who fail die. No lord or unit survives demon captivity otherwise.
6. Commit (this phase will likely be several commits: bodies, faction, spawner, behaviour rules).

**Acceptance:** at night the map crawls with red-black demon parties that charge anything living, execute their prisoners, and dissolve at dawn; a rare night sees a town assaulted. Pure math for all tunables, tested.

---

## Phase 2 — The barter economy (Requirements 3, 5, 9, 31)

**Goal:** gold ceases to matter; food and items are the currency of survival.

**Reuse:** study how the mod already overrides game models (`AshenDiplomacyModel` registration in `MagicSystem.cs`) and how `ForestClansPenaltyModels` / `ForestClansSpeedModel` override vanilla `GameModel`s — that is your pattern for economy model overrides.

1. **Requirement 5 — feasibility spike first.** Timebox a small investigation: enumerate every vanilla flow where gold enters/leaves (trade, recruit, upgrade, wages, building, loot, ransom, tournaments, quest rewards, caravans, workshops, barter screen). For each, identify the `GameModel`/behavior that controls it and whether it can be forced to 0/items. Then choose ONE path and record it:
   - **Path A (preferred): gold removed.** Traders hold 0 gold; all trade is barter (the barter screen is the only trade); recruiting costs items/food (preferably food); upkeep costs food only (no coin); promoting units and building cost no coin; gold cannot be looted; ransoming prisoners grants almost nothing. Only items, food, and weapons form the economy.
   - **Path B (fallback): gold ~10× scarcer** everywhere — city traders, villages, rewards, army costs, money from lords, ransoms.
   - Whichever path you take, implement it through clean model overrides in a new `src/Economy/` folder (`ScarcityModels.cs`, `EconomyMath.cs` pure + tested), not scattered patches.
2. **Requirement 3 — scarcity.** City traders: much less gold, almost no food for sale. Weapons crude and very rare, horses almost unavailable. Most food must come from **villages**, forcing travel. Adjust town/village market inventories on a daily tick (see how existing behaviors do daily work) with the quantities in `EconomyMath`.
3. **Requirement 9 — rewards.** Every reward the player receives from NPCs (quests, favors, tournaments, events already in `CampaignMapEvents`/`SettlementEncounters`) becomes **food or items**. If a vanilla flow makes gold unavoidable, make the amount trivial.
4. **Requirement 31 — thin garrisons.** Town/castle garrisons, food stores, and militia at roughly **half** vanilla or less. It should be visibly hard to raise and feed an army.
5. Commit per sub-system.

**Acceptance:** you cannot meaningfully buy your way through the game; the barter screen is how things move; towns are hungry and under-defended. Decision (Path A or B) recorded in commit message and in code comments at the override site.

---

## Phase 3 — Units reflect scarcity (Requirements 4, 29, 30)

**Goal:** armies look and cost like the end of the world.

**Reuse:** the vanilla horse-requirement mechanic for cavalry upgrades (grep for `UpgradeRequires` / upgrade requirement usage), item catalogs, and however AshAndEmber already restyles troops (`PriestTroops`, `AshenRecruitCatalog`).

1. **Requirement 4 — costly promotion.** Upgrading any troop to **tier 4 or 5** requires spending **a horse AND an armour piece AND a weapon of good price** from the player's inventory (the same idea as cavalry needing horses, extended). Implement the check + consumption cleanly in `src/Economy/` or a new `src/Units/`; costs tabled in a pure math file, tested.
2. **Requirement 30 — shabby mid-tiers.** Re-equip tier 3–4 troops of all cultures with lighter, poorer, more improvised gear. Tier 5 stays good **but** recruiting a tier-5 unit costs items too.
3. **Requirement 29 — weary lords.** Lords' equipment de-blinged: strip gold/ornate/rich items, re-dress in worn, patched, post-apocalyptic gear. Apply at session launch (the same "re-apply after engine load" slot as the culture overrides in `OnGameInitializationFinished`).
4. Commit.

**Acceptance:** a tier-5 line is a real investment; lords look like survivors, not princes.

---

## Phase 4 — Magic: the spellbook and the formulas (Requirements 13, 16, 17, 18, 26)

**Goal:** magic cast by keyed formulas, learned scarce, misfiring painfully.

**Reuse:** `MiracleInputHandler` (this **is** the input mechanism: hold modifier, tap a U/L/R/D sequence, release to cast — extend it or copy its pattern into a new `src/Spellbook/` system), `MiracleCatalog` (pure data catalog pattern), `MagicLearning` (focus-point unlock + codex UI), `ElementSpellEffects`/`ElementWallWards`/`ElementUltimates`/`NatureEffects` (the actual battle effects you will bind formulas to), the existing keybinds (Alt+L codex / Shift+X list / Ctrl+X in-battle reference).

1. Create `src/Spellbook/`: `SpellbookCatalog.cs` (pure), `SpellbookInputHandler.cs`, `SpellbookCampaignBehavior.cs`, `SpellbookMath.cs` (pure) + tests, `SpellburnEffects.cs`.
2. **Requirement 13 — unlocking magic.** To cast at all, the player must **spend one focus point once** in the spellbook menu ("open the spellbook"). Spells are learned by: finding a formula in ruins (Phase 9), learning from the Tower faction (Phase 7), or starting with one (Phase 13 character creation).
3. **Requirement 16 — formulas.** Each spell is a directional input sequence of **5 to 20 characters** (U/D/L/R). Tap a correct full combination in battle — even one you never learned — and the spell **casts and is recorded** in your spellbook, same as discovering it. Design the formula table so that **most possible sequences are NOT spells** (the space must stay sparse — a fizzle chance must exist at every length). Add a pure test asserting no two spells share a formula and that coverage of each length-N space stays below a small fraction.
4. **Requirement 17 — the spell set.** Target **30–50 spells**, catalogued in `SpellbookCatalog` with name, formula, description, and effect binding. Mandatory entries:
   - **Fireball** — replicate the fire blast/bolt (`ElementSpellEffects.FireMissile`) — 5 inputs.
   - **Firewall** — replicate the fire wall (`ElementWallWards`) — 5 inputs.
   - **Replicate every AshAndEmber elemental spell the same way**: wind gust, water torrent, earth entangle, spirit nova, the walls/wards per element, and the ultimates as long-formula spells.
   - **Summon Demon** — conjure a demon fighting on your side (reuse `DemonFactory`) — 20 inputs.
   - **Banish Demons** — light blasts damage **all** demons on the battlefield — 20 inputs.
   - **Light** — lingering bulbs of light acting as lamps that lower demon morale — 5 inputs.
   - Invent the rest in the same register (wards, curses, veils, callings…) with formula length scaling to power. Keep the codebase's lore voice.
5. **Requirement 17 — the book.** The **existing AshAndEmber spell-list key combination must open the new spellbook**. The spellbook is a clean, nicely organized read-only list of every formula you know + description (reuse the litany/grimoire presentation).
6. **Requirement 18 — fizzle and spellburn.** An incorrect completed sequence fizzles. On fizzle, roll a **spellburn**: base 60% chance, **reduced by Intellect** (scaling in `SpellbookMath`, tested). Spellburn table (pick one at random):
   - Burn yourself for 30 damage.
   - Become immobilised for 30 seconds.
   - Issue a random command to your units.
   - Explode, dealing 20 damage to everyone nearby.
   - A demon appears on the battlefield on a random side for 70 seconds, switching sides every 10 seconds.
   - **Plus three more you design in the same spirit** (e.g. your weapon-hand sears shut — dropped weapon; a false night falls over the field for a time; every light on the field snuffs and demon morale surges; your voice tears — party morale drops). Pick three, make them implementable with existing effect plumbing.
7. **Hands free:** casting requires empty hands — no weapon or shield wielded. Enforce in the input handler.
8. **Requirement 26 — debug mode.** **Ctrl+Shift+F12** unlocks all spells and grants a few magical items (repurpose the existing Ctrl+Shift+F12 grant-all debug hook in `MagicSystem.OnApplicationTick`).
9. Decide what happens to the old casting paths: the unified element hold-and-charge input and the Grace/Nature/DarkGift player paths are **superseded** by the spellbook (their *effects* live on as spells). Disable their player-facing inputs cleanly; keep the effect code and the NPC paths (Phase 5 uses them). Note: no campaign-map spells exist in this mod at all (Requirement 14) — disable `ElementMapSpells` and map miracles for everyone.
10. Commit (catalog, input, fizzle, book UI, debug — separate commits).

**Acceptance:** with free hands you can tap a 5-key formula and loose a fireball; a wrong 12-key sequence can blow up in your face; the book lists what you know; Ctrl+Shift+F12 fills it.

---

## Phase 5 — Rare spellcasters (Requirements 14, 15)

**Goal:** casters are rare and precious, on both sides of the battle line.

**Reuse:** `ColourLordAI` + `NpcCastPlanner` (NPC lords already cast in battle on personality cooldowns), `ColourLordRegistry` (how lords are chosen as mages), `BanditMageAI`, `PriestTroops` (caster troop trees + battle AI).

1. **Requirement 14:** roughly **7% of named lords/companions** know 1–3 spells and cast them in battle through the existing NPC cast path. **No campaign-map spells** (already disabled in Phase 4 — verify).
2. **Requirement 15:** a **spellcaster troop tree** — a full tree (recruit → tier 5), very rare to encounter/recruit, each tier knowing 2–3 battle spells and using them in combat (follow `PriestTroops` end-to-end: definition, spawning, battle AI).
3. Percentages/cooldowns in pure math, tested.
4. Commit.

**Acceptance:** the occasional enemy lord burns your line; a rare caster unit exists at every tier and actually casts.

---

## Phase 6 — Magical items and demon-bane (Requirements 19, 20)

**Goal:** relics worth crawling through ruins for; magic is what kills demons.

**Reuse:** `CrystalCatalog`/`CrystalEffects` (item-bound battle effects), `DarkGiftBattleEffects` (passive combat gifts), item lookup patterns from `ElementalFactory` (`MBObjectManager.Instance.GetObject<ItemObject>` style), loot hooks from existing battle-end handling.

1. Create `src/Relics/`: `RelicCatalog.cs` (pure), `RelicEffects.cs`, `RelicMath.cs` + tests, `RelicNaming.cs`.
2. **Requirement 19 — the relics.** Rare magical items: small-chance loot from demon battles and rare finds in ruins (Phase 9 wires the ruin side). Base each on an existing thematic item, assign **one or two** effects:
   - Every current **Crystal** effect, re-balanced weaker because a relic never breaks.
   - Every **Dark Gift** effect, re-balanced weaker because it is per-item, not per-soul.
   - More effects you design following the same pattern.
   - Each relic gets a **generated, cool name** (`RelicNaming`: pattern like *"Vow of the Sixth Dawn"*, *"Cinderfang"*, *"The Widow's Patience"* — build a component-combinator, pure + tested).
3. **Requirement 20 — demons fear the flame.** Spells and magical weapons deal **bonus damage to demons**. Single choke point: hook the damage-dealing paths (`CastAttack`, relic-weapon hits) and multiply when the victim is a demon. Multiplier in `RelicMath`/`DemonMath`, tested.
4. Commit.

**Acceptance:** a named relic can drop from a demon horde; a caster carves through demons visibly faster than a swordsman.

---

## Phase 7 — The eight factions (Requirement 10)

**Goal:** every kingdom is a desperate answer to the Long Night.

**Reuse:** the culture-override pattern (`TempleCulture`, `DunebornCulture`, `TribalCulture`, `ForestClansCulture` + the `OnGameInitializationFinished` re-application), the faction dialogue systems (`TempleDialogue`, `TribesDialogue`, …), city-menu-option pattern (`CrystallinesCampaignBehavior.Menus`, `SanctuaryCampaignBehavior.Menus`), the vassal-title dialogue replacement (grep the existing dialogue files for how "vassal" lines are altered), `SanctuaryCampaignBehavior` (sacrifice-per-round ritual pattern).

**Structure:** create `src/Factions/` with one sub-folder or partial-class family per faction. **Implement, test, and commit one faction at a time.** Each faction keeps ONLY its listed starting towns (all other towns become city-states in Phase 8). Faction-specific bonuses apply to that faction's **lords too**, not just the player.

Do all eight, exactly as specified:

**A. Sturgia → "Wolf Brothers"** — towns **Tyal, Sibir**. Survival of the fittest over civilization. Vassal title → **Kinsman** (in all their dialogue). Joining means eating human flesh: reputation loss + personality-trait shifts. Members get a **city menu option to render units and prisoners into meat** (food items).

**B. Aserai → "Tower"** — town **Iyakis**. Scholars of magic. Vassal title → **Warlock**. Joining opens the way to magic (grants the spellbook unlock if you lack it). They **teach spells**: a menu offering 6 random spells, each at an influence cost. A further menu option **transmutes any tier-2+ troop into a magical (spellcaster) unit** for influence.

**C. Battania → "The Hive"** — towns **Marunath, Car Banseth**. Bound by a protective fungus into one network. Lords **always speak in plural** ("we", "us") — sweep their dialogue. You join by **drinking the elixir** (vassal → **Integrated**), and the bond is kept alive only by **drinking it regularly** — lore-wise the Integrated dose themselves often to stay in the network. **Leaving is therefore possible**: stop drinking and the fungus lets go (leaving works like leaving any faction; the Hive bonuses and downsides simply end). **While Integrated, if the player dies (game-over), automatically take control of a random Hive lord and continue.** City option: **recruit prisoners into your army for free**. Downside: sometimes battle **screen colours invert** (hallucinations; investigate a feasible post-processing/scene-tone trick — `Visual/AshenSceneTone` is your starting point) and your will is suppressed by others.

**D. Khuzait → invent a name** (demon-hunter flavoured — e.g. "The Bloodbound"; you choose, keep it lore-fitting) — towns **Akkalat, Chaikand**. They hunt demons and use their blood. As a vassal, every demon party you defeat yields **1–3 × "Demon Blood"** items (use a wine-like texture/mesh, trade-good type). In their cities, spend it via a city menu for one of: demons **ignore you for 1–4 days** (they won't attack your party); **+80 max HP for a week**; or **−1 Social/Intellect (random) for +1 Vigor/Endurance (random)**. They refuse candidates with low Vigor+Endurance and no focus points in combat skills.

**E. Vlandia → "Temple"** — towns **Ocs Hall, Pravend**. Servants of the God of Light; a religious order wearing a kingdom's shape. Vassal → **Brother Templar**. On joining you receive a **Holy Sigil** (a stone-based weapon item: on hit, small damage to nearby demons; on block, slight morale restore — build it on the Crystal effect plumbing). Members can **buy Holy Sigils** in their towns. City menu: **pray** for beneficial party/global effects, gated by personality traits. They refuse the **devious or cruel**.

**F. Northern Empire → "The Empire"** — towns **Saneopa, Diathma, Argoron**. Heirs of the imperial throne, keepers of the old rites. Members may **claim a few free units of grain once per day** in their cities (city option).

**G. Western Empire → "Legion"** — towns **Ortysia, Lageta**. Militarists; might makes right; they steal rather than produce. Make them **markedly more aggressive** — more raids, more attacks — than other factions (campaign AI weighting). Vassal → **Comrade**. Members get **training fields** in their cities: spend 1 focus point → gain 1 focus point in each of **two random skills** from Vigor, Control, or Endurance (double yield, random placement).

**H. Southern Empire → invent a name containing "Widows"** (e.g. "The Pale Widows"; you choose) — towns **Phycaon, Lycaron**. All clan leaders **must be female**; male lords are husbands, never leaders (enforce at session start and on succession). Lore: men failed; women seized power and bought peace by sacrificing men to the demons. **A male player in this faction loses all influence every day.** City option: **sacrifice any of your soldiers** → 1 day of demons ignoring you per soldier. **Sacrifice a male prisoner lord or male clan member** → demon soldiers join your army (they vanish after 1–4 weeks) + immunity. A male player has a **small chance to be sacrificed instead** when using the option.

For each faction: rename (culture-text override), reduce to starting towns, vassal-title dialogue sweep, joining gate/ritual, city menu options, lords benefit too, all tunables pure + tested. **One commit per faction minimum.**

**Acceptance:** each faction is joinable, its mechanic works, its lords use it, its dialogue uses the new titles, and the renames survive a save/load (the `OnGameInitializationFinished` re-application).

---

## Phase 8 — City-states and the wretched free towns (Requirements 11, 24)

**Goal:** everything not claimed above stands alone.

**Reuse:** `AshenCitySystem.*` — it already turns settlements into independent, renamed, self-maintained enclaves with persistence. `AshenRecruitCatalog` for recruit-pool overrides.

1. **Requirement 11:** every town **not** listed in Phase 7 becomes a **city-state**: a one-city faction (plus its villages), named after its ruling clan. They never join or rejoin the core kingdoms — they live by their own small politics and armies. Joinable by the player normally; no special plotline or perk. Model directly on the Ashen city mechanics.
2. **Requirement 24:** those neutral/free cities get **Looter or Bandit culture** — the units they spawn and offer for recruitment are looters/bandits: deliberately bad troops.
3. Commit.

**Acceptance:** the map is 8 small kingdoms + a scatter of self-owned city-states recruiting rabble, and none of them ever fold back into a kingdom.

---

## Phase 9 — The ruins (Requirement 12)

**Goal:** the dead castles of the old world are the dungeons of the new.

**Reuse:** the entire `src/AshenRuins/` system (defs → math → menus → campaign behavior), `DemonSpawnCampaignBehavior` (nightfall spawning), `RelicCatalog` and `SpellbookCatalog` (loot).

1. **~80% of castles**, randomly chosen at session start (stable per save), are converted: renamed **"Ruined Manor" / "Ruined Castle" / "Ruined City" / "Ruined Tower"** (and similar variants), with **no owner, no garrison, nothing** — they exist only as explorable legacy dungeons. (The remaining ~20% stay normal castles.)
2. Exploration works like Ashen Ruins, with these changes:
   - **Replace the chamber list** in the defs catalog with a new one fitting *ruined places of the old civilization* (collapsed halls, flooded cellars, a lord's bedchamber sealed from inside, a chapel of the old light, a larder crawling with things…) — not a mystic dungeon. Keep the def-catalog structure (`AshenRuinDefs` pattern: enum + data rows) so menus/math stay reusable.
   - **Progressing to each next chamber requires waiting several in-game hours, scaled by your Scouting skill** (better scouts clear rooms faster). Use a wait menu (see `SettlementEncounters.WaitMenu` / the sea-voyage wait menu for the pattern). Hours formula pure + tested.
   - **Waiting risks nightfall.** If night falls while you are inside, **aggressive demons inevitably spawn nearby** (wire to the Phase 1 spawner) — leaving the ruin at night is walking into them.
   - **Loot:** weapons, armor, trade goods, **magical items (relics)**, and **spell formulas** — a found formula is recorded in the spellbook. Loot tables pure + tested.
3. Commit (conversion; chambers; wait/night mechanics; loot — separately).

**Acceptance:** most castles read as ruins with no owner; crawling one is a Scouting-timed gamble against the dark; formulas and relics genuinely come out of them.

---

## Phase 10 — Mortal AI under the same law (Requirement 6)

**Goal:** the NPCs live in the same nightmare, by the same rules.

**Reuse:** campaign AI hooks already used by `TribalKingdomBehavior` and the Legion aggression work from Phase 7G; the economy models from Phase 2.

1. a/ **No great kingdoms:** campaign AI avoids snowballing — discourage/deny kingdom growth past a small cap (fief count), discourage clans defecting into big kingdoms.
2. b/ **Fear the night:** NPC parties avoid travelling at night unless their army is large (threshold in pure math). Small parties shelter in/near settlements at dusk.
3. c/ **Fight for food:** raise the AI's appetite for raids/hostility driven by food and item scarcity — hungry parties raid villages and attack caravans for supplies.
4. d/ **Same rules as the player:** NPCs trade as barter, hold limited arms, field mostly low-tier troops with only a few elites, and pay the same promotion costs conceptually (their rosters should *look* like scarcity: tier caps/ratios in pure math, tested).
5. Commit per sub-rule.

**Acceptance:** no empire ever re-forms; roads empty at dusk; wars are about grain, not glory; enemy armies look as ragged as yours.

---

## Phase 11 — The clock of the apocalypse (Requirements 32, 33)

**Goal:** the world has a pulse, and it is getting worse.

**Reuse:** `CampaignMapEvents.*` (slotted event queues, rumours/portents machinery — see `CampaignMapEvents.Portents`), `DemonSpawnCampaignBehavior`, the Ashen resurgence daily logic (`MagicCampaignBehavior` daily tick) as a scheduling pattern.

1. **Requirement 32 — Night of the Hunt.** Every **20–82 days** (rolled), a Night of the Hunt: demons spawn in much greater numbers, raid villages, and generally do more harm than a normal night. Announce it with dread (an evening portent message). Interval/intensity in pure math, tested.
2. **Requirement 33 — escalation.**
   - **~Day 300:** rumours begin (tavern rumours + map portents) of demons behaving strangely, as if planning something.
   - **~Day 600:** **they are gathering** — spawn a large demon band in one random corner of the map that persists and grows.
   - **Beyond day 1000:** each period, a chance the **Demon Lord** appears — a unique, named demon lord who binds all demons into **his** faction, summons many more, and begins conquering settlements. He can end the game by killing everyone. Make him a real campaign presence (party, army, sieges), not a text event.
   - **Defeating the Demon Lord is the campaign's victory** — killing him counts as winning The Darkest Night (a proper victory announcement/journal resolution; the surviving demons scatter back to leaderless night-tide behaviour). But he must **not be easily killable**: he is a monstrous battlefield presence (boss-tier stats, heavy magic, demon-bane resistance ideas inverted), his host is enormous, and simply catching him should itself be an endgame feat. Tunables in pure math, tested — the intent is that only a late-game player with relics, spells, and a hardened army has a real chance.
3. Commit per milestone.

**Acceptance:** a 900-day save has lived through hunts and watched the gathering; past day 1000 the endgame can genuinely arrive — and either side can end the campaign: he kills everyone, or you kill him and win.

---

## Phase 12 — Eight questlines (Requirement 21)

**Goal:** every faction's answer to the Long Night, played to its bitter end.

**Reuse:** `GreatAwakening/` (donation-accumulation quest with NPC contribution, altar, resolution, opposition — the explicit baseline for D and H), `NorthmenStones/` (gather-resources-in-one-town, can be stolen, invasion + ending — the explicit baseline for G), `DragonQuestSystem`/`BurningLabQuestSystem` (multi-stage questline patterns), the journal/notification plumbing those systems already use.

**Trigger:** from **~day 50**, the player starts receiving notifications and journal entries to **speak with each faction leader**; the conversation unlocks that faction's quest.

Implement all eight (new folder `src/FactionQuests/`, one behavior family per quest, one commit each):

- **A. Wolf Brothers** — *you design it.* Something worthy of cannibal survivalists (suggestion: "The Great Hunt" — prove the pack's law by hunting the largest living things left: a chain of named demon beasts, ending in a choice between feeding the pack a demon's flesh — transformation — or burning it). Keep your design grim, simple, and implementable.
- **B. Tower** — they believe demons can be **banished**. Gather magical items, demon blood, and holy sigils for the great rite. When performed, **it goes wrong**: instead it summons a large demon host that rampages and attacks cities. Design the details (quantities, staging, aftermath) yourself.
- **C. The Hive** — spreading the fungus **to the demons**. It ends with the faction dying or being possessed — design the middle (delivery vector, infected demon parties, the network screaming) and pick the ending.
- **D. [Khuzait/Bloodbound]** — enough demon blood will let them **surpass human limits**. Gather an enormous amount of Demon Blood and donate it to one of their cities (**use the Great Awakening donation machinery**). Then the choice: **participate in the ritual — you die**; or **run — you abandon the faction**. Either way every clan of the faction drinks and **dies gruesomely**, leaving only small children; demons attack their cities shortly after.
- **E. Temple** — find **5 holy artifacts** that spawn randomly in ruins across the map and bring them together. Once gathered, **all Temple lords and armies bind into one army that can never disband** and marches the land killing demons; the objective becomes **"Kill 50,000 demons"** (pick a huge but technically countable number). There is no salvation — they fight until all are dead; if the number is somehow reached, hope dwindles and the faction disbands.
- **F. The Empire** — **conquer ⅔ of all cities** and crown the faction leader Emperor; then the Empire declares war on the demons and must kill a very large number of them.
- **G. Legion** — the leader believes an **ark** can carry them beyond the sea to peace. Gather a very significant stock of resources in **Ortysia** (**repurpose the Northmen Standing Stones machinery**, including theft when Ortysia falls). Ending: the player **sails beyond the sea (player ending)** or **stays as the new leader of the Legion** while a few random clans leave.
- **H. [Widows]** — enough sacrificed men will stop the demons (**repurpose Great Awakening**). The twist: the Widows *get* their peace — by becoming **mindless and permanently joining the demon faction** (the player is cast out, or chooses to stay and the game ends). Afterwards, contacting any Widow gets only the demons' answer: **"…"**.

**Balance pass (explicit requirement):** after all eight exist, review them together — none may be trivially fast or cheap; quantities must scale against the barter economy (an "enormous amount of demon blood" must actually take many hunts); endings must fire reliably; two quests must not deadlock each other (e.g. Temple artifacts and ruin loot RNG — guarantee artifact placement). Write the balance numbers into the pure math files and adjust with tests.

**Acceptance:** all eight questlines can be started after day ~50, progressed under the scarcity economy, and reach their endings, including the game-ending ones.

---

## Phase 13 — Who you were before the Night (Requirement 23)

**Goal:** character creation tells the new world's story.

**Reuse:** `src/AI/CreationBackstoryRework.cs` — it already rewrites creation stages; extend it.

Apply exactly:

- **Step 1 — Background:** replace all culture/faction options with **one** option built on the Empire background: **"I am a survivor"** — one of the few who actually lived through the Long Night. **Remove all faction bonuses** from standard options.
- **Step 2 — Family:** use the Empire template; keep as is.
- **Step 3 — Early Childhood:** keep as is.
- **Step 4 — Adolescence:** keep as is.
- **Step 5 — Youth:** re-theme from "drafted into an army" to "finding your way in a world overrun by demons, making yourself useful in the city":
  - *Rode with scouts* → **Scavenged for food**.
  - *Trained with the infantry* → **Trained to fight**.
  - *Joined the skirmishers* → **invent something fitting** (e.g. *Ran the night errands* — you carried messages between wards after dark).
  - *Stood guard with a garrison* → keep as is.
  - The envoy-type option → **Served as a messenger**.
  - The lordling/page-type option → **invent something fitting** the new world (e.g. *Tended the ward-fires of a lord's hall*).
- **Step 6 — Young Adulthood:**
  - *You defeated an enemy in battle* → **You held your ground against demons**.
  - *You led a successful manhunt* → **something similar but fitting** (e.g. *You tracked a demon pack to its daylight lair*).
  - *You invested some money in land* → **You studied the arcane arts**: remove the skill/focus bonuses; instead grant **Magic (spellbook unlocked) + 2 random spells with formulas up to 7 characters** at game start.
  - *You hunted a dangerous animal* / *famous escapade in town* / *treated people well* → keep as is.
  - *You saved your village/city quarter from flood/fire* → **You saved your kin from the demon**.
  - *You invested money in a workshop* → **You studied the arcane arts** (same grant as above).
  - *You survived a siege* → **You survived an invasion**.

Commit. **Acceptance:** a new sandbox character walks the six steps in the new fiction, and the arcane options genuinely start you with the spellbook and two short spells.

---

## Phase 14 — Words of the new world (Requirements 8, 27-check)

**Goal:** the game *talks* like the Long Night happened.

**Reuse:** the codex (`MagicLearning`), `AmbientRemarks`, `TavernCampaignBehavior.Rumors`, all the `*Dialogue.cs` systems, `NarrativeStageTextFixer` (text-override pattern).

1. **Requirement 8:** update the **codex/lexicon** with the new lore (the Long Night, the demons, the eight factions, the ruins, spellburn, relics). Update standard **dialogues** to reflect the changed world — lords, notables, tavern keepers should not speak like it is peacetime Calradia. Sweep the faction dialogues from Phase 7 for consistency (titles: Kinsman, Warlock, Integrated, Brother Templar, Comrade…).
2. Re-verify Phase 0's intro text now that all systems exist; make the intro reference real mechanics (night tide, barter, the spellbook).
3. Commit.

**Acceptance:** a player who reads the codex and talks to ten NPCs understands the setting without reading this prompt.

---

## Phase 15 — Retention check, cleanup, and the final polish (Requirements 22, 25, 26-check)

**Goal:** ship-shape.

1. **Requirement 22 — retention:** verify by actually exercising the code paths that these still work untouched: **dice games, sea travel, drinking in the inn, drinking with friends** (`src/Tavern/`, `src/Sea/`). Fix anything the conversion broke.
2. **Requirement 25 — clean code sweep (deliberately last):**
   - Rename classes/namespaces/files whose names no longer match reality (`AshAndEmber` → `TheDarkestNight` namespace; `Ashen*` types that now serve demons/ruins/city-states get honest names; `MiracleInputHandler`-derived spellbook code named for what it is). Use compiler-verified renames; keep save-compatibility keys/strings **unchanged** where they are persisted (the `SEA_*`/`ELEM_*`-style keys and `SaveDefiner` types must not change shape — rename the code, not the save format).
   - **Delete dead code:** player paths you disabled in Phase 4 that nothing references anymore, retired systems, unused assets. Check twice before deleting anything referenced by NPC paths or save data.
   - Re-read `CLAUDE.md` conventions and make the new folders conform (partials by concern, pure math + tests everywhere).
3. **Requirement 26 — verify** Ctrl+Shift+F12 debug mode end-to-end.
4. Update `CLAUDE.md` and `README.md` to describe The Darkest Night as it now is (systems map, new folders, new keybinds), and rewrite `CHANGELOG.md` with a fresh `v0.1.0` entry. Bump the version in all four places listed in `behaviour.md`.
5. Full build + full test run + final commit.

**Acceptance:** a stranger reading the repo sees The Darkest Night, not Ash and Ember with a hat on; the suite is green; the debug key works.

---

## Requirement traceability (verify every box before you call it done)

| # | Requirement (short) | Phase |
|---|---|---|
| 1 | Demon armies spawn at night, vanish by day, aggressive, numerous, never flee | 1 |
| 2 | Demon assets built from existing human/horse meshes (no custom textures exist), grey/red/black, claws + magic, environment variants | 1 |
| 3 | Scarcity: poor traders, almost no city food, crude rare weapons, no horses, village food | 2 |
| 4 | Tier 4/5 promotion costs horse + armour + good weapon | 3 |
| 5 | Gold removed (barter only) or 10× scarcer fallback; no coin upkeep/recruit/build/loot; ransom nerfed | 2 |
| 6 | NPC AI: no big kingdoms, no night travel, fights for food, same rules as player | 10 |
| 7 | Demon AI: never retreat/escape/strategize; night replenish; rare city attacks; no diplomacy; executes prisoners (escape roll) | 1 |
| 8 | Codex/lexicon + dialogues updated to the new world | 14 |
| 9 | NPC rewards are food/items; gold trivial if unavoidable | 2 |
| 10 | Eight reworked factions A–H with unique join mechanics + city options; lords use them too | 7 |
| 11 | All other cities become permanent city-states named for their clan | 8 |
| 12 | ~80% castles → ownerless Ruins; Scouting-timed chambers; nightfall demons; weapons/armor/goods/relics/formulas loot | 9 |
| 13 | Magic via Miracles-style input; spellbook unlock costs a focus point; learn via ruins/Tower/start | 4 |
| 14 | ~7% of lords/companions cast in battle; no map spells | 5 |
| 15 | Rare full spellcaster troop tree, 2–3 battle spells each | 5 |
| 16 | Formulas 5–20 chars; correct tap = learned; also found in ruins | 4 |
| 17 | Existing spellbook keybind; organized formula list; 30–50 spells incl. Fireball/Firewall/all elementals/Summon/Banish/Light; sparse combo space | 4 |
| 18 | Fizzle → spellburn, base 60% scaled down by Intellect; 5 listed + 3 invented burns; hands must be free | 4 |
| 19 | Rare relics from demons/ruins; Crystal + Dark Gift effects weakened; generated names | 6 |
| 20 | Demons vulnerable to spells and magical weapons | 6 |
| 21 | Eight faction questlines from ~day 50, using Great Awakening + Northmen Stones as baselines; balance-reviewed | 12 |
| 22 | Dice games, sea travel, inn drinking, drinking with friends retained | 15 |
| 23 | Character creation: single survivor background; Steps 5–6 rewrites; arcane options grant Magic + 2 short spells | 13 |
| 24 | Neutral/free cities get Looter/Bandit culture and trash recruits | 8 |
| 25 | Clean code: rename to match reality, delete unused code | 15 |
| 26 | Debug mode Ctrl+Shift+F12: all spells + relics | 4, 15 |
| 27 | New intro screen + new-game intro text | 0, 14 |
| 28 | New Campaign disabled — Sandbox only | 0 |
| 29 | Lords' equipment weary, post-apocalyptic | 3 |
| 30 | Tier 3–4 gear shabbier; tier 5 good but costs items to recruit | 3 |
| 31 | Garrisons/food ~half of normal or less | 2 |
| 32 | Night of the Hunt every 20–82 days | 11 |
| 33 | Day ~300 rumours, ~600 gathering, 1000+ Demon Lord endgame; killing him (very hard) wins the campaign | 11 |

---

*Now begin with Phase 0. Read `CLAUDE.md`, `behaviour.md`, and `README.md` first. Keep the fires lit.*
