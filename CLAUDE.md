# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Darkest Night** (mod id `TheDarkestNight`, presented in-launcher under that name; see `SubModule.xml`) is a total-conversion of Mount & Blade II: Bannerlord (~65K+ lines, ~250 C# files under `src/`) built on top of **Ash and Ember**, a magic-overhaul mod whose codebase is this project's baseline and "parts bin." The internal DLL name, root namespace (`AshAndEmber`), and several legacy type names (`Ashen*`, `MiracleInputHandler`, etc.) still read as Ash and Ember — that is a **deliberate, documented gap**, not an oversight; see "On the `AshAndEmber` namespace" below. Target framework: .NET Framework 4.7.2. Sandbox only — "New Campaign" (StoryMode) is intercepted and blocked (`Startup/SandboxOnlyGate`).

**The pitch:** the world shattered overnight. Demons crawl out from the underworld every dusk and hunt the living; humanity survives behind walls, wards, and eight desperate factions born from the old kingdoms. Gold has stopped mattering — barter and scarcity define the economy. Magic is cast by tapping directional formulas (the Spellbook), not born of noble blood. Somewhere past day 1000, a named Demon Lord may rise to end the world, or be killed to end the Night.

The player-facing casting model is the **Spellbook** (`src/Spellbook/`): with free hands, hold a modifier and tap a 5–20-character U/D/L/R sequence; a correct sequence looses the bound spell (fire bolt, wall, gust, nova, summon/banish demon, and more) and is learned on first successful cast even if never studied. A wrong sequence fizzles and rolls a chance of **spellburn** (self-harm, immobilization, a rogue demon, and more). The Spellbook supersedes the older player-facing casting inputs (the unified element hold-and-charge system, the Miracles/Grace gesture, the Nature discipline, and campaign-map spells) — those input paths are now gated off for the player (`PlayerCastingEnabled = false` on each handler) but their **effect code and NPC casting paths are still very much alive**: NPC lords (`ColourLordAI`), the rare spellcaster troop tree (`src/Spellbook/SpellcasterTroops.cs`), and the Kindled/demons all still cast through the underlying `ElementSpellEffects`/`NatureEffects` machinery. Player-facing details live in `README.md`; the release history is in `CHANGELOG.md`.

### On the `AshAndEmber` namespace

Phase 15 of the build (see `PROMPT_THE_DARKEST_NIGHT.md`) explicitly named a full `AshAndEmber` → `TheDarkestNight` namespace/class/file rename as the last, highest-risk item in the whole conversion, and gave clear permission to skip it if it could not be done with high confidence. It was scoped **out** of this pass: a mechanical `namespace`/`using` rename across ~250 files is possible, but several persisted savedata identifiers, `SaveDefiner` type names, and item/troop XML ids are namespace-shaped strings that a text-level rename could silently corrupt for anyone continuing a save from an earlier build. Until a future pass does this deliberately (namespace rename only, verified by a green build, with class/file renames like `AshenCitySystem` or `MiracleInputHandler` left for a later pass still), the internal namespace staying `AshAndEmber` is a **cosmetic-only** gap — `SubModule.xml`'s player-visible `Name`/`Id` already read "The Darkest Night" / `TheDarkestNight`, and nothing about gameplay, save format, or player experience depends on the C# namespace text.

## Commands

**Build** (requires `BannerlordPath` env var or Steam default):
```bash
dotnet build src/TheWitheringArt.csproj
```
Post-build automatically copies the DLL to `<BannerlordPath>/Modules/AshAndEmber/bin/<BannerlordBin>/`.

**Run all tests:**
```bash
dotnet test tests/AshAndEmber.Tests.csproj
```

**Run a single test:**
```bash
dotnet test tests/AshAndEmber.Tests.csproj --filter "PureLogicTests.<TestMethodName>"
```

**Install pre-built release:**
```powershell
.\install.ps1                         # auto-detect Bannerlord path
.\install.ps1 -BuildFirst             # build then install
.\install.ps1 -BannerlordPath "D:\..." # explicit path
```

## Architecture

### Entry Point and Wiring

`SubModule.xml` registers `AshAndEmber.MainSubModule` as the mod entry point. `MagicSystem.cs` contains `MainSubModule`, which on `OnGameStart()`:
- Resets **all** in-mission static state (a long block of `SpellEffects.Clear*`, `Element*.ClearBattleState`, `Nature*`, `Miracle*`, `ColourLordAI`, `Demon*`, etc.) so a save-load in the same process cannot carry stale state.
- Registers `AshenDiplomacyModel` (permanent-war override, also the template the demon faction's own diplomacy model follows).
- Registers ~40 `CampaignBehaviorBase` subclasses (each in its own try/catch): the original Ash and Ember systems (`MagicCampaignBehavior`, `SchemeCampaignBehavior`, `SanctuaryCampaignBehavior`, `AshenAltarsCampaignBehavior`, `SeaCampaignBehavior`, `CrystallinesCampaignBehavior`, `ExchangeCampaignBehavior`, `TavernCampaignBehavior`, `AshenRuinCampaignBehavior`, `MiracleCampaignBehavior`, `NatureCampaignBehavior`, `ClanOrdersCampaignBehavior`, `SoldierServiceCampaignBehavior`, `ElementalWildsBehavior`, `TribalKingdomBehavior`, `CreationBackstoryRework`) plus every Darkest Night system: `DemonSpawnCampaignBehavior`, `MarketScarcityCampaignBehavior`, `PromotionCampaignBehavior`, `SacredSitesCampaignBehavior`, `AshenRecruitCampaignBehavior`, `GreatAwakeningCampaignBehavior`, `NorthmenStonesCampaignBehavior`, `SpellbookCampaignBehavior`, `SpellcasterTroopBehavior`, one campaign behavior per faction (`WolfBrothersCampaignBehavior`, `TowerCampaignBehavior`, `ForestWidowsCampaignBehavior`, `BloodboundCampaignBehavior`, `TempleCampaignBehavior`, `EmpireCampaignBehavior`, `LegionCampaignBehavior`, `ChosenCampaignBehavior`), `WandsCampaignBehavior`, `TalismansCampaignBehavior`, `CityStateCampaignBehavior`, `RuinsCampaignBehavior`, `MortalLawCampaignBehavior`, `ApocalypseCampaignBehavior`, `FactionQuestTriggerCampaignBehavior`, and one questline behavior per faction (`WolfHuntQuestCampaignBehavior`, `TowerRiteQuestCampaignBehavior`, `ChosenQuestCampaignBehavior`, `ForestWidowsQuestCampaignBehavior`, `BloodboundQuestCampaignBehavior`, `TempleQuestCampaignBehavior`, `EmpireQuestCampaignBehavior`, `LegionQuestCampaignBehavior`).
- Registers the dialogue systems (`AshenDialogue`, `ArenicosDialogue`, `TempleDialogue`, `TribesDialogue`, `NorthmenDialogue`, `DunebornDialogue`, plus each faction's own `*Dialogue.cs`) and calls per-system reset/init (`SchemeSystem.Initialize`, `ExchangeCampaignBehavior.ResetState`, `SeaCampaignBehavior.ResetForNewGame`, `ClanOrdersCampaignBehavior.ResetForNewGame`).

`OnGameInitializationFinished` re-applies the culture-text overrides after the engine reloads its XML texts — both the legacy Ash and Ember overrides (Vlandia→Temple, Khuzait→Bloodbound, Sturgia→Wolf Brothers, Aserai→Tower, Battania→Forest Widows) and the Darkest Night faction renames for the three Empire cultures (Northern→The Empire, Western→Legion, Southern→The Chosen), plus lord equipment de-blinging (Requirement 29) and city-state naming (Requirement 11). `OnApplicationTick` skips intro videos, drives the splash/loading screens, polls the map-magic input handlers, and handles map hotkeys:
- **Alt+L** — Spellbook (once unlocked) or the legacy element Codex
- **Shift+L** — Grace litany (non-mages only)
- **Ctrl+Shift+F10 / F11 / F12** — scheme debug toggle, spawn debug combat, grant-all (unlocks the Spellbook with every formula, all Dark Gifts, max Grace, all Nature talents, 100 focus points, one of every crystal, and one relic/wand/talisman — `MainSubModule.DebugGrantAll`)
- **Alt/Controller-RLeft + WASD/left stick** — Spellbook formula input (see `src/Spellbook/SpellbookInputHandler.cs`); **Alt+X** opens the Spellbook mid-formula
- **Left Control** — Bloodbound blood-attunement gesture (`src/Factions/Bloodbound/BloodAttunementInputHandler.cs`) — deliberately reuses the key freed up by the retired Element/Miracle/Nature player inputs, confirmed free by grepping every `InputKey.LeftControl` use at the time it was added

`OnMissionBehaviorInitialize` injects `MagicMissionBehavior` per battle. Its `OnMissionTick` fans out to every combat subsystem (element input/effects/ultimates, crystals, miracles, nature, dark gifts, the Spellbook input/effects, blood attunement, demon aura ticks, relic/wand/talisman effects, spellcaster lords/troops, and the legacy `SpellEffects.Tick*` family); `OnAgentHit`/`OnAgentRemoved`/`OnAgentBuild` route reflect/sunder/dark-gift/crystal/nature-resist/demon-execution hooks.

Each registration is wrapped in its own try/catch for mod-conflict safety.

### Major Systems (folder map under `src/`)

**Darkest Night systems (this conversion):**

- `Demons/` — the night tide: `DemonFactory` (builds a demon from re-dressed/re-tinted human or horse meshes and gear — no custom assets exist), `DemonCatalog` (tier/environment-variant data), `DemonSpawnCampaignBehavior` (hourly dusk-spawn/dawn-despawn, replenishment, rare city assaults), `DemonBattleBehavior` (never-retreat/never-strategize charge AI, prisoner execution with an escape roll), `DemonVisuals` (bone-bound particles/lights/contour, the same one-time-bind pattern `ElementalVisuals` established), pure `DemonMath` + tests.
- `Spellbook/` — the player's only casting path: `SpellbookCatalog` (30–50 spells, formula 5–20 chars, sparse combo space), `SpellbookInputHandler` (hold-and-tap formula input, hands-free gate), `SpellbookCampaignBehavior` (unlock cost, learn-on-cast, debug grant), `SpellbookEffects`/`SpellburnEffects` (fizzle → spellburn table), and the rare-caster layer `SpellcasterLords`/`SpellcasterLordMath` (≈7% of named lords/companions cast in battle) and `SpellcasterTroops`/`SpellcasterTroopCatalog`/`SpellcasterTroopMath` (a full, rare recruit→tier-5 caster tree). Pure math in `SpellbookMath`, `SpellcasterLordMath`, `SpellcasterTroopMath`.
- `Relics/`, `Wands/`, `Talismans/` — magical items looted from demons and ruins: each a `*Catalog` (pure data, generated/curated names), `*Effects` (mission-tick battle hooks, weakened Crystal/Dark Gift-style effects), and a pure `*Math` (Relics also add `RelicNaming`, a name-combinator). Demon-bane bonus damage (Requirement 20) is folded into the same `CastAttack`/relic-hit choke points the element system already used.
- `Ruins/` — ~80% of castles become ownerless, explorable Ruins (Requirement 12): `RuinsCastleSystem` (session-start conversion, stable per save), `RuinsCatalog` (the new "ruined places of the old civilization" chamber list, same def-catalog shape as the legacy `AshenRuinDefs`), `RuinsExplorationSystem` (+`.WaitMenu` partial — Scouting-scaled wait-per-chamber, nightfall risk wired to `DemonSpawnCampaignBehavior`), `RuinsMenus`, pure `RuinsMath`.
- `CityStates/` — every town not claimed by one of the eight factions becomes a one-city, clan-named city-state that never joins a kingdom and recruits Looter/Bandit-culture rabble (Requirements 11, 24): `CityStateSystem`, `CityStateCampaignBehavior`, pure `CityStateMath`.
- `Apocalypse/` — the campaign clock (Requirements 32, 33): `ApocalypseCampaignBehavior` (+ `.Gathering.cs`/`.Resolution.cs` partials) schedules the Night of the Hunt (every 20–82 days), the day-300 rumours, the day-600 gathering band, and the day-1000+ Demon Lord endgame; `DemonLordSystem` gives him a real campaign presence (party/army/sieges, boss-tier stats, victory/defeat resolution). Pure `ApocalypseMath`.
- `MortalLaw/` — NPC lords live under the same rules as the player (Requirement 6): `MortalLawCampaignBehavior` (+ `.Hunger.cs` raiding-for-food, `.NightFear.cs` night-travel avoidance, `.Rosters.cs` scarcity-shaped rosters), pure `MortalLawMath`.
- `Economy/` — the barter/scarcity economy (Requirements 3, 5, 9, 31): `ScarcityModels.cs` (GameModel overrides — wages, upgrade cost, building cost, battle/plunder gold, ransom, garrison/militia growth, auto-recruitment), `MarketScarcityCampaignBehavior` (daily town food/horse/weapon supply pruning), pure `EconomyMath`. **Decision recorded in `EconomyMath.cs`'s header: Path B (gold ~10× scarcer everywhere), not Path A (gold removed)** — the town trade screen and party item-exchange popup hard-code gold in TaleWorlds' own view-models with no model seam to remove it cleanly.
- `Factions/` — one subfolder per reworked kingdom (Requirement 10), each with a `*Culture.cs` (rename + vassal-title dialogue sweep), `*CampaignBehavior.cs` (+ `.Menus.cs` and other concern partials for the faction's unique mechanic), `*Dialogue.cs`, `*Settlements.cs` (starting-town list), and a pure `*Math.cs`: `WolfBrothers` (Sturgia, cannibalism), `Tower` (Aserai, spell teaching), `ForestWidows` (Battania — collective "we" dialogue, the Integrated network), `Bloodbound` (Khuzait — Demon Blood economy, blood attunement), `Temple` (Vlandia — Holy Sigils, prayer), `Empire` (Northern Empire — free grain, Schemes access), `Legion` (Western Empire — aggression, training fields), `Chosen` (Southern Empire — matriarchal succession, sacrifice).
- `FactionQuests/` — one questline per faction, unlocked from day ~50 via `FactionQuestTriggerCampaignBehavior`/`FactionQuestTrigger`, each following the `GreatAwakening`/`NorthmenStones` donation- or gather-then-resolve pattern from the legacy systems below. Pure `FactionQuestMath` holds shared balance constants.

**Legacy Ash and Ember systems (baseline; still live — NPC casting, underlying effects, and several still-retained player features run through these):**

- `Magic/` — the unified element system: `ElementMagicInput` (battle input — player path retired, `PlayerCastingEnabled = false`; NPC lords and the Kindled still cast through `CastAttack`), `ElementSpellEffects`, `ElementWallWards`, `ElementUltimates`, `ElementMapSpells` (disabled entirely — no campaign-map spells exist, Requirement 14), `MagicLearning` (the Codex), `MageElementKnowledge`, teacher dialogue, and pure `*Math.cs`.
- `Spells/` — the **legacy** two-phase Inner Fire (`SpellEffects.*` partials, `BlastSpells`, `SelfSpells`, `CreateSpells`, enchantments). Still drives NPC mage casts and the shared battle-effect ticks.
- `Nature/` — the Living Ember: charges, living-energy economy, seers, backlash (player input retired the same way as `Magic/`; the nature-discipline effects are still cast by NPC seers and folded into the Wind/Earth/Water Spellbook spells). Pure math in `LivingEnergyMath`/`NatureCharge`.
- `Miracles/` — Grace: prayers, grace economy, priest troops, battle AI, talents (player input retired; NPC priests/lords and `PriestTroops` still cast). Pure math in `MiracleMath`. **Lore:** Grace is not bestowed by a deity — it is the same Fire the rest of the mod draws on, called through the caster's own emotional and intellectual alignment (expressed as a personality trait) rather than a drawn cone. Flavour/effect text must never write "the light" as a watching, judging, or granting party; the caster (or their own conviction) is always the one deciding. The Temple's priests may *describe* it as divine favor as their institutional gloss, but player-facing miracle text should stay in the caster's own voice.
- `DarkGifts/` — the Dark Gift path (battle effects + `DarkGiftSystem`); its per-hero effects are the weaker template Relics/Talismans re-balance to per-item.
- `Crystals/` — consumable crystal items (`CrystalCatalog`, `CrystalEffects`, `CrystalBattleAI`, `CrystalMath`); the template Relics re-balance to permanent-but-weaker.
- `Elementals/` — **The Kindled**: elemental beings (fire/water/stone/ice/sand/storm) that roam the wilds, are summoned by mages, or wake mid-battle. `ElementalFactory` builds one; `ElementalBeings` is the mission registry that drives their look + the element/physical weakness; `ElementalVisuals` owns that look — continuous element particle systems bound **once** to each being's skeleton bones (pelvis/chest/head/hands, so the fire/mist/dust rides every limb for free), one follower light per body (created once, only repositioned each tick), and a persistent coloured contour — replacing the old per-tick particle re-stamping that churned GameEntities every frame; `ElementalWildsBehavior` breeds roaming bands (persisted under `ELEM_*` keys); pure `ElementalMath` holds the weakness wheel. The Spirit Unbinding's champion (`ElementUltimates`) is unified onto this core. The `ElementalKind` enum lives in `ElementUltimateMath`. **`DemonVisuals`/`DemonFactory` are this system's direct descendants** — same bind-once bone-particle/light/contour pattern, re-tuned smoke-black/ember-red instead of elemental colours.
- `Schemes/` — covert operations (`SchemeSystem.*`, `SchemeCampaignBehavior.*`, minigame). **Modified for the Empire faction:** access is gated to `EmpireCulture.IsPlayerEmpireKingdom` (player menu only — NPC scheme AI is untouched) and every scheme is now paid in influence alone (`GoldCost` is 0 everywhere; the old gold price was folded into `InfluenceCost` to preserve the cost hierarchy — see the comment block in `SchemeSystem.cs`).
- `Soldier/` — **Take the Lord's Coin**: hire your party out to a warring lord as a common soldier from clan level 0. `SoldierServiceCampaignBehavior` (state/tick/army + weekly pay) and its `.Dialogue` partial; pure `SoldierServiceMath` (pay, desertion penalties). Attaches as a mercenary of the lord's realm (`ChangeKingdomAction.ApplyByJoinFactionAsMercenary`) **and folds the player's party into the commander's host as a true army member** (`Army.AddPartyToMergedParties`; if the commander leads no army, one is raised for him via `new Army(...)` and dissolved on release; if that ever throws, `_armyCreateFailed` trips a `SetMoveEscortParty` fallback) — so the player marches with the company and auto-joins his battles on his side, earning renown. **The deal is sealed from inside the map meeting with the lord, so the conversation consequence changes NO faction/army state** (doing so mid-encounter corrupts it into a hostile Attack/Surrender resolution and crashes) — it only records the terms and sets `PlayerEncounter.LeaveEncounter = true`; the first clean map tick (`!IsEncounterLive()`) runs `FinalizeJoin` (mercenary contract + `ReassertArmy`). Leaving the host (vanilla "Abandon Army", caught via `OnPartyLeftArmyEvent`) before the agreed term is desertion; clean release with a bonus after. Membership is idempotently self-healed each tick (`ReassertArmy`) and the raised host's cohesion topped up (`SustainArmy`). A host **we** raised for a lone commander is only held for `SoldierServiceMath.HostHoldDays`, then `SustainArmy` dissolves it and opens a `HostBreatherDays` window (player rides escort) so the campaign AI gets a clean chance to draft him into — or let him raise — a real war host; a party already in an army can neither create one nor be summoned to one (vanilla `CanLordCreateArmy`/`CheckPartyEligibility` both gate on `MobileParty.Army`), and the self-heal then folds the player into whatever real host he joins. A host the commander gathered himself is never touched.
- `Sea/` — harbors, voyages, trade ventures, NPC sea lanes; pure `SeaMath`. **Retained untouched** through the whole conversion (Requirement 22) — fares are computed independently of the Economy scarcity models.
- `Markets/` — the Exchange / commodity speculation (`ExchangeCampaignBehavior.*`, pure `SpeculationMath`).
- `QuestSystems/` — Dragon main quest, Burning Lab questline, settlement encounters, world events (`CampaignMapEvents.*`), battlefield events. The donation-accumulation pattern (`GreatAwakening/`) and the gather-in-one-town pattern (`NorthmenStones/`) are the explicit reuse templates for several `FactionQuests/` questlines.
- `AI/`, `Tribes/`, `ClanOrders/`, `Conclave/`, `AshenRuins/`, `Apprentice/`, `Tavern/`, `Campaign/`, `Visual/`, `Startup/` — supporting culture, faction, atmosphere, and UI-flow systems. `Tavern/` (dice games, drinking) is **retained untouched** (Requirement 22). `AshenRuins/` is the direct structural template `Ruins/` was built from. `AI/AshenCitySystem.*` is the direct template `CityStates/` was built from, and its retired baseline rename helpers (`RenameNorthmenKingdom`, `RenameDunebornKingdom`, `RenameForestClansKingdom`, etc.) are kept **unreferenced but present** for save compatibility — do not delete them.

### Spell Cast Pipeline

**Current (player): the Spellbook.** `SpellbookInputHandler.Tick` reads the formula gesture (hold Alt/Controller-RLeft, tap the U/D/L/R sequence with free hands) and matches it against `SpellbookCatalog`; a correct sequence dispatches to `SpellbookEffects`, which for the elemental spells simply calls the same `ElementSpellEffects`/`ElementWallWards`/`NatureEffects` entry points described below (Fireball/Firewall/gust/torrent/entangle/nova and their walls), plus Spellbook-only spells (Summon Demon via `DemonFactory`, Banish Demons, Light, wards/curses/veils). An incomplete or wrong sequence fizzles and rolls `SpellburnEffects` (base 60%, reduced by Intellect). This is the **only** player casting path — see "On the `AshAndEmber` namespace" above and the retired-input note in the legacy systems list for what it superseded.

**Underlying element pipeline (still the effect layer under the Spellbook, and still the player's path before Phase 4 retired it for NPC-cast parity discussion below):** `ElementMagicInput.Tick` reads Focus + direction + a stand-still charge, then `ElementSpellEffects` / `ElementWallWards` / `ElementUltimates` resolve the attack, wall, or ultimate. Life-cost is **flat** (the charge buys power, not a cheaper cast); the Nature discipline lowers it, and the Ashen pay in criminal standing.

**Attack forms (per element — each has its own silhouette so they read apart):**

| Element | Attack shape | Implemented in |
|---|---|---|
| Fire | **Flying bolt that explodes on impact** (bursts on first foe reached or at range's end) | `ElementSpellEffects.FireMissile` + `TickBolts`/`ExplodeBolt` (the `_bolts` list, ticked from `Tick`) |
| Wind | **Forward gust/stream** (broad wedge, knockback drives foes ahead) | `NatureEffects.BattleGale` (shared source) |
| Earth | **Short, almost-melee cone of erupting rock** (close fan, heavy damage + root — reach traded for force) | `NatureEffects.BattleEntangle` (shared source) |
| Water | **Forward slowing wave** (cone) | `NatureEffects.BattleTorrent` (shared source) |
| Spirit | **Nova** (radial panic + random enemy order) | `ElementSpellEffects.SpiritPanic` |

`CastAttack(el, caster, power)` is the single dispatch choke point — the player (`ElementMagicInput`), NPC lords (`ColourLordAI`), and the Kindled (`ElementalBeings`) all cast through it, so changing an attack shape there is automatically NPC-parity-correct. It also folds in the **mastery scale** (`ElementMagicMath.MasteryScale(hero.Level)`, +1%/level capped at +30%) by multiplying `power` for hero casters — because `ChargeFraction` clamps at 1, a `power > 1` lifts only the direct damage, never the tuned cone reach / wall depth / ignite (the same path the overchannel already rides). Non-hero casters (troops, the Kindled) map to no hero and keep ×1. Crystals scale the same way through `CrystalEffects.Potency` (player Medicine → `CrystalMath.MasteryScale`); miracle **damage** through `MiracleEffects.Conviction` (caster's summed aligned virtue → `MiracleMath.ConvictionScale`). **Wind/Earth/Water still delegate to the shared `NatureEffects` (Gale/Entangle/Torrent), which the Living Ember nature discipline also casts** (`NatureSeerAI`, the nature input handler — both still live in `MagicSystem`), so reshaping them there deliberately reshapes the nature-discipline versions too (consistent with the "one magic" unification). The fire bolt is a self-contained projectile (no legacy `SpellCast`/`Agent.Main` dependency) so it works for any caster; it trails fire each tick and is cleared with the rest of battle state via `ElementSpellEffects.ClearBattleState`.

**Legacy (NPC and underlying effects):**
```
MagicInputHandler (Alt+Direction buffers)
  → SpellBuilder.Parse(formBuffer, effectBuffer) → SpellCast
  → AgingSystem.ComputeBattleAgingCost(totalInputs) → days cost
  → SpellEffects.Execute*() → dispatches by spell type
```
`SpellEffects.cs` is the core partial class; `BlastSpells.cs`, `SelfSpells.cs`, `CreateSpells.cs`, and `AffectSpells.cs` extend it by spell form. NPC mage lords still cast through this path.

### State: Static vs. Serialized

- **In-mission state** lives in static fields on `SpellEffects`, `ActiveEffects`, the `Element*`/`Nature*`/`Miracle*`/`Crystal*` classes, `ColourLordAI`, etc. `MainSubModule.OnGameStart()` and `MagicMissionBehavior.OnEndMission()` both clear all of it to avoid save-reload / mission carry-over.
- **Persistent hero state** is stored in `MageKnowledgeData` (serialized into the campaign save via TaleWorlds' `CampaignObject` extension API). This holds talent purchases, aging ledger, grimoire unlocks, whisper tiers, Rival Shadow counter, and pending event flags.
- **Other persistent state** is saved per-behavior, mostly as parallel lists keyed by prefixed strings (`SEA_*`, scheme, exchange, clan-order keys) via each behavior's `SyncData`, plus custom savedata types registered in `SaveDefiner.cs`. Nature reserves, Grace, and Dark Gifts persist through their own knowledge/inventory objects. Voyage-in-progress state is intentionally **not** serialized (a mid-crossing reload refunds the fare).

### Campaign Tick Architecture

`MagicCampaignBehavior` hooks three tick rates:
- **Daily:** aging decay, Whisper tier decay, Ashen resurgence logic
- **Weekly (14+ day slots):** independent general-event and war-event queues in `CampaignMapEvents`
- **On settlement enter/leave:** `SettlementEncounters`, gated by cooldown + renown + mage status

The other behaviors register their own daily/weekly/enter-leave hooks (e.g. `NatureCampaignBehavior`, `MiracleCampaignBehavior`, `SchemeCampaignBehavior`, `SeaCampaignBehavior`, `ExchangeCampaignBehavior`, `ClanOrdersCampaignBehavior`). Keep new tick logic in the behavior that owns the concern rather than piling it onto `MagicCampaignBehavior`.

### NPC Mage AI

`ColourLordAI.TryCast()` runs on cooldowns that vary by personality:
- Ashen lords: 6 s, no aging cost, cast proactively
- Calculating: 24 s; Impulsive: 10 s; default: 16 s (stretched up to ×2.5 near burnout by temperament — see `NpcCastPlanner.CooldownMult`)

AI priority: defensive burst (<40% HP) → heal burst (<30% HP) → attack (school-specific). `BanditMageAI` adds burnout risk scaled to bandit tier (35% → 15%).

### Ritual Systems (Sanctuary / Ashen Altars)

Both `SanctuaryCampaignBehavior` and `AshenAltarsCampaignBehavior` share the same hidden-accumulation pattern:
- Player sacrifices a resource per round (HP or prisoner)
- A hidden target is rolled; player decides to continue or stop
- Alignment multiplier scales yield (flipped sign between the two systems)
- NPC lords simulate 3–4 rounds automatically

### Sea Systems (Harbors / Voyages / Ventures)

`SeaCampaignBehavior` (in `src/Sea/`) adds harbor menus to 16 coastal towns, matched **by town name** at session launch (a failed match silently drops the port). Voyages run inside a wait game menu (`sea_voyage`): hazards (one storm roll, one corsair roll) are scheduled at voyage start and fire mid-crossing as inquiries; arrival teleports the party to the destination gate. Trade ventures persist in the save (`SEA_*` keys, parallel lists) and resolve on daily tick. NPC lords and caravans also use the sea lanes: on `OnSettlementLeftEvent` from a port they may be teleported to another port (lords only toward their existing AI target; caravans opportunistically), after an off-screen corsair resolution against their roster. All formulas — fares, travel hours, hazard odds, abstract boarding-battle resolution, venture margins, NPC sail gates — live in `SeaMath.cs`, which is pure (no TaleWorlds types) and covered by `PureLogicTests`. Voyage state is intentionally not serialized: a reload mid-crossing refunds the escrowed fare.

### Talent and Focus Point Costs

Elements and disciplines (Steel, Blood, Nature) are learned in the **Codex** (`MagicLearning`) with focus points. Cost escalates by how many powers you already hold: `TalentCostCurve.Cost(LearnedCount)` — 1 fp for the first power, 2 for the second, and so on (Fire is free from day one). Learning from a **teacher** costs one point less (min 1). `TalentId` still carries retired class/path enum values (Reaper, Pyrelord, the discipline classes, the Nature rites) kept **for save compatibility** — do not assume an enum member is still a live, purchasable talent; check `TalentSystem`'s definition table.

Campaign-map (non-battle) spells cost 1 aging day for the first cast per calendar day, then escalate. Battle casts pay the flat life-cost described in the pipeline section above.

### Key Numerical Constants

Numeric tuning lives in the pure `*Math.cs` files (each system has its own); those files are the source of truth. A few stable, cross-cutting values:

| Thing | Value |
|---|---|
| Mage lord fraction | ~20% of lords |
| Ashen lord fraction | ~10% of lords |
| Settlement encounter cooldown | ~6–7 days |
| World event slot interval | 14+ days |
| Bandit-unit mage fraction | ~4% of eligible units |

**Legacy two-phase values (NPC casts / underlying effects only — verify against code before relying on them):** max 5 form + 5 effect inputs per cast; aging cost `round(1.5^(n−1))` capped at 84 days; Sear/Force/Shred base ~22–35 HP per input; Restore ~15 HP per input; blast/burst radius 2.5 m per input; missile range 3 m per input. The current player casting model is flat-cost, charge-scaled (see the pipeline section), not per-input.

## Conventions

- **Naming:** PascalCase for public members and classes; `_camelCase` for private fields; enum values are PascalCase (e.g., `TalentId.Gift`, `ColorSchool.Red`).
- **One system per folder** under `src/`; large behaviors/classes are split into **partial classes by concern** across several files (e.g. `SchemeSystem.Execution.cs`, `ExchangeCampaignBehavior.Rounds.cs`, `SpellEffects.Battlefield.cs`). Never add new spell-form logic directly to `SpellEffects.cs`; add it to the appropriate `*Spells.cs` partial or a new one. Follow the existing split when a file grows.
- **Numeric logic goes in a pure `*Math.cs` file** (no TaleWorlds types) so it can be unit-tested. If a "pure" method needs a game value, pass it in as a parameter rather than reading `Hero.MainHero` inside — see `behaviour.md` for why (JIT type resolution defeats a `try/catch`).
- **Static utility classes** (`AgingSystem`, `SchoolData`, `SpellDatabase`, the `*Math` classes) have no instance state — keep them that way.
- **Null-guard pattern:** always check `Campaign.Current == null` / `Mission.Current == null` before accessing singletons in behavior methods, and wrap TaleWorlds singleton access in try/catch (mod-conflict safety).
- **Never swallow silently:** a mod-conflict-safety `catch` must record the failure, not drop it. Use `catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }` (see `src/ModLog.cs`). `ModLog` is crash-proof, references no TaleWorlds types (safe from pure `*Math.cs`), de-duplicates per failure site so a per-tick throw is logged once, and writes to `Documents\Mount and Blade II Bannerlord\AshAndEmber\errors.log`. The log self-limits: entries older than 30 days are pruned once at session start, and the file is archived to `.old` if it passes 5 MB. Do not reintroduce bare `catch { }`.
- **Tests live in `tests/PureLogicTests.cs`** and cover only pure (no-TaleWorlds-runtime) logic. Keep new tests pure — do not reference game engine types.

## Working behaviour

Process and working-style guidance (how to build, verify the TaleWorlds API, bump
the version, and avoid mod-conflict crashes) lives in a separate file:

@behaviour.md
