# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Darkest Night** (mod id `TheDarkestNight`, presented in-launcher under that name; see `SubModule.xml`) is a total-conversion of Mount & Blade II: Bannerlord (~106K lines, ~430 C# files under `src/`) built on top of **Ash and Ember**, a magic-overhaul mod whose codebase is this project's baseline and "parts bin" — **reuse only; this is a new thing, not a continuation.** The root namespace is `TheDarkestNight`, the assembly is `TheDarkestNight.dll`, and it deploys to `Modules/TheDarkestNight/`. The remaining Ash and Ember names are legacy **type** names (`Ashen*`, `Miracle*`, `EmberConclave*`) — cosmetic leftovers pending `REFACTOR_NAMING.md` Phase 4 — plus the deliberately-frozen strings in the table below. Target framework: .NET Framework 4.7.2. Sandbox only — "New Campaign" (StoryMode) is intercepted and blocked (`Startup/SandboxOnlyGate`).

**The pitch:** the world shattered overnight. Demons crawl out from the underworld every dusk and hunt the living; humanity survives behind walls, wards, and eight desperate factions born from the old kingdoms. Gold has stopped mattering — barter and scarcity define the economy. Magic is cast by tapping directional formulas (the Spellbook), not born of noble blood. Somewhere past day 1000, a named Demon Lord may rise to end the world, or be killed to end the Night.

The player-facing casting model is the **Spellbook — the Scrived Word** (`src/Spellbook/`, rune system as of v0.10.0, `RUNE_MAGIC_PLAN.md`): with free hands, hold a modifier and draw **runes** — each exactly three marks of U/D/L/R — then release the binding. `RuneSequenceMath` (pure) resolves the drawn runes as a sentence (repetition amplifies; two elements fuse, three form a Triad, four the Unbound Weave; one Form reshapes the working; Manners stack) and `RuneEffects` dispatches the resolved working into the same `ElementSpellEffects`/`ElementalFactory`/`DemonFactory`/`SpellEffects` primitives the mod already uses. A real rune drawn is learned on the spot; a malformed/over-long binding fizzles and rolls **spellburn** (self-harm, immobilization, a rogue demon, and more), and even a valid long binding carries **strain**. The legacy `SpellbookCatalog`/`SpellId`/`SpellbookEffects` path is retained for **wands, the Chosen's Rod, and NPC caster lords/troops** (their bound workings) plus save migration (`RuneCatalog.RunesForLegacySpell`). The Spellbook supersedes the older player-facing casting inputs (the unified element hold-and-charge system, the Miracles/Grace gesture, the Nature discipline, and campaign-map spells) — those input paths are now gated off for the player (`PlayerCastingEnabled = false` on each handler) but their **effect code and NPC casting paths are still very much alive**: NPC lords (`ElementLordAI`), the rare spellcaster troop tree (`src/Spellbook/SpellcasterTroops.cs`), and the Awakened/demons all still cast through the underlying `ElementSpellEffects`/`NatureEffects` machinery. Player-facing details live in `README.md`; the retired player-facing caster paths (still the authoritative description of how spells *behave* for NPC casters) are in `LEGACY.md`; the release history is in `CHANGELOG.md`.

### Names that must NOT be "tidied up"

The namespace, assembly, DLL, module folder, and test project are all `TheDarkestNight` now (Phases 2–3, each verified by a green build plus a full test run). The few `AshAndEmber` / `aae_` strings that remain are **not** leftovers to sweep; each is load-bearing for a specific reason:

| String | Why it stays |
|---|---|
| `aae_*` item ids (53, in `ModuleData/items.xml` + C#) | **Persisted in player inventories.** Renaming orphans every existing save's items. Never touch. |
| `"AshAndEmberQuest"` (`SpecialQuestType`, 15 files) | Verified by reflection: getter-only, no `SaveableProperty` — computed, never persisted. Harmless either way, and out of scope. |
| `"the baseline AshAndEmber mod"` in comments (22) | Correct historical references to the **upstream project**, not to our namespace. |
| `Ashen*` / `Miracle*` / `EmberConclave*` type names | Cosmetic; `REFACTOR_NAMING.md` **Phase 4**. Note `AI/AshenCitySystem`'s retired rename helpers are kept unreferenced-but-present for save compatibility — do not delete them. |
| `God-King` / "Tribes of the East" in `AI/AshenCitySystem.Renaming.cs` (`RenameTribesKingdom`, `ApplyTribalCultureTexts`) and all of `AI/TribesDialogue.cs` | **Dead — verified uncalled**, and kept per the row above. Khuzait is the Bloodbound (ruler: **Huntmaster**), and every *live* surface now says so. Before "fixing" a God-King string, check whether its function has a call site: these have none, and `BloodboundCulture`/`BloodboundDialogue` own the live path. |

**A blanket find-and-replace of `AshAndEmber` across this repo will corrupt saves and break the test build.** Work from `REFACTOR_NAMING.md`.

## Commands

**Build** (requires `BannerlordPath` env var or Steam default):
```bash
dotnet build src/TheDarkestNight.csproj
```
Post-build automatically copies the DLL to `<BannerlordPath>/Modules/TheDarkestNight/bin/<BannerlordBin>/`.

**Run all tests:**
```bash
dotnet test tests/TheDarkestNight.Tests.csproj
```

**Run a single test:**
```bash
dotnet test tests/TheDarkestNight.Tests.csproj --filter "PureLogicTests.<TestMethodName>"
```

**Install pre-built release:**
```powershell
.\install.ps1                         # auto-detect Bannerlord path
.\install.ps1 -BuildFirst             # build then install
.\install.ps1 -BannerlordPath "D:\..." # explicit path
```

## Architecture

### Entry Point and Wiring

`SubModule.xml` registers `TheDarkestNight.MainSubModule` as the mod entry point. `MagicSystem.cs` contains `MainSubModule`, which on `OnGameStart()`:
- Resets **all** in-mission static state (a long block of `SpellEffects.Clear*`, `Element*.ClearBattleState`, `Nature*`, `Miracle*`, `ElementLordAI`, `Demon*`, etc.) so a save-load in the same process cannot carry stale state.
- Registers `AshenDiplomacyModel` (permanent-war override, also the template the demon faction's own diplomacy model follows).
- Registers ~40 `CampaignBehaviorBase` subclasses (each in its own try/catch): the original Ash and Ember systems (`MagicCampaignBehavior`, `SchemeCampaignBehavior`, `SanctuaryCampaignBehavior`, `AshenAltarsCampaignBehavior`, `SeaCampaignBehavior`, `CrystallinesCampaignBehavior`, `ExchangeCampaignBehavior`, `TavernCampaignBehavior`, `AshenRuinCampaignBehavior`, `MiracleCampaignBehavior`, `NatureCampaignBehavior`, `ClanOrdersCampaignBehavior`, `SoldierServiceCampaignBehavior`, `ElementalWildsBehavior`, `TribalKingdomBehavior`, `CreationBackstoryRework`) plus every Darkest Night system: `DemonSpawnCampaignBehavior`, `VeilCampaignBehavior`, `MarketScarcityCampaignBehavior`, `PromotionCampaignBehavior`, `SacredSitesCampaignBehavior`, `AshenRecruitCampaignBehavior`, `GreatAwakeningCampaignBehavior`, `NorthmenStonesCampaignBehavior`, `SpellbookCampaignBehavior`, `SpellcasterTroopBehavior`, one campaign behavior per faction (`WolfBrothersCampaignBehavior`, `TowerCampaignBehavior`, `ForestWidowsCampaignBehavior`, `BloodboundCampaignBehavior`, `TempleCampaignBehavior`, `EmpireCampaignBehavior`, `LegionCampaignBehavior`, `ChosenCampaignBehavior`), `WandsCampaignBehavior`, `TalismansCampaignBehavior`, `CityStateCampaignBehavior`, `RuinsCampaignBehavior`, `MortalLawCampaignBehavior`, `ApocalypseCampaignBehavior`, `ExpeditionCampaignBehavior`, `ForeignMusterCampaignBehavior`, `BeastsOfTheNorthCampaignBehavior`, `FactionQuestTriggerCampaignBehavior`, and one questline behavior per faction (`WolfHuntQuestCampaignBehavior`, `TowerRiteQuestCampaignBehavior`, `ChosenQuestCampaignBehavior`, `ForestWidowsQuestCampaignBehavior`, `BloodboundQuestCampaignBehavior`, `TempleQuestCampaignBehavior`, `EmpireQuestCampaignBehavior`, `LegionQuestCampaignBehavior`).
- Registers the dialogue systems (`AshenDialogue`, `ArenicosDialogue`, `TempleDialogue`, `TribesDialogue`, `NorthmenDialogue`, `DunebornDialogue`, plus each faction's own `*Dialogue.cs`) and calls per-system reset/init (`SchemeSystem.Initialize`, `ExchangeCampaignBehavior.ResetState`, `SeaCampaignBehavior.ResetForNewGame`, `ClanOrdersCampaignBehavior.ResetForNewGame`).

`OnGameInitializationFinished` re-applies the culture-text overrides after the engine reloads its XML texts — both the legacy Ash and Ember overrides (Vlandia→Temple, Khuzait→Bloodbound, Sturgia→Wolf Brothers, Aserai→Tower, Battania→Forest Widows) and the Darkest Night faction renames for the three Empire cultures (Northern→The Empire, Western→Legion, Southern→The Chosen), plus lord equipment de-blinging (Requirement 29) and city-state naming (Requirement 11). `OnApplicationTick` skips intro videos, drives the splash/loading screens, polls the map-magic input handlers, and handles map hotkeys:
- **Alt+X** — Spellbook (once unlocked) or the legacy element Codex (matches the in-battle open key)
- **Shift+L** — Grace litany (non-mages only)
- **Ctrl+Shift+F10 / F11 / F12** — scheme debug toggle, spawn debug combat, grant-all (unlocks the Spellbook with every formula, all Dark Gifts, max Grace, all Nature talents, 100 focus points, one of every crystal, and one relic/wand/talisman — `MainSubModule.DebugGrantAll`)
- **Alt/Controller-RLeft + WASD/left stick** — Spellbook formula input (see `src/Spellbook/SpellbookInputHandler.cs`); **Alt+X** opens the Spellbook mid-formula
- **Left Control** — Bloodbound blood-attunement gesture (`src/Factions/Bloodbound/BloodAttunementInputHandler.cs`) — deliberately reuses the key freed up by the retired Element/Miracle/Nature player inputs, confirmed free by grepping every `InputKey.LeftControl` use at the time it was added

`OnMissionBehaviorInitialize` injects `MagicMissionBehavior` per battle. Its `OnMissionTick` fans out to every combat subsystem (element input/effects/ultimates, crystals, miracles, nature, dark gifts, the Spellbook input/effects, blood attunement, demon aura ticks, relic/wand/talisman effects, spellcaster lords/troops, and the legacy `SpellEffects.Tick*` family); `OnAgentHit`/`OnAgentRemoved`/`OnAgentBuild` route reflect/sunder/dark-gift/crystal/nature-resist/demon-execution hooks.

Each registration is wrapped in its own try/catch for mod-conflict safety.

### Major Systems (folder map under `src/`)

**Darkest Night systems (this conversion):**

- `Veil/` — the seasonal magic/demon tide: a predictable 21-day cycle (one Bannerlord season, keyed off `day % 21` so it aligns with the season boundaries) split into three seven-day turns — **the Warding** (veil thick: magic ×0.7, demon spawns ×0.6), **the Steady** (×1.0/×1.0), and **the Thinning** (veil thin: magic ×1.5, demon spawns ×1.2). Pure `VeilMath` holds the rhythm and every multiplier; `VeilCampaignBehavior` announces each turn and exposes the shared `CurrentPhase()` read that four choke points consume: the magic multiplier folds into `ElementSpellEffects.CastAttack`/`CastWall` (reaches player Spellbook, NPC lords, the Awakened, and demon casters alike), the spawn multiplier scales `DemonSpawnCampaignBehavior.SpawnNightTide`, mage-lord cadence rides `ElementLordAI.SetCooldown` (mages cast more in the Thinning), and campaigning-lord caution rides the Veil-aware `MortalLawMath.IsSafeFromNightFear` overload (armies march freely in the Warding, shelter in the Thinning).
- `Demons/` — the night tide: `DemonFactory` (builds a demon from re-dressed/re-tinted human or horse meshes and gear — no custom assets exist), `DemonCatalog` (tier/environment-variant data), `DemonSpawnCampaignBehavior` (hourly dusk-spawn/dawn-despawn, replenishment, rare city assaults), `DemonBattleBehavior` (never-retreat/never-strategize charge AI, prisoner execution with an escape roll), `DemonVisuals` (bone-bound particles/lights/contour, the same one-time-bind pattern `ElementalVisuals` established), pure `DemonMath` + tests.

  **Agent scaling (v0.7.0) — verified, and it supersedes an earlier "impossible" note.** Two distinct engine hooks make demons read as beasts, both confirmed against the shipped DLLs:
  - **Whole-body:** `Agent.SetInitialAgentScale`, wrapped as `DemonFactory.SetAgentScale`. This is the single entry point for *every* rescaled agent in the mod — demons, the Hellsteed's mount, and the Wolf Brothers' Jotunn-Blooded (`BeastsOfTheNorth/`) all call it. v0.2.0 documented that no safe runtime agent-scale surface was known and built the giant from maxed body proportions instead; **that note is obsolete** — do not repeat it, and prefer this hook.
  - **Per-bone:** `MBAgentVisuals.ApplySkeletonScale(Vec3, float, sbyte[], Vec3[])` — the same channel Native's own `skeleton_scales.xml` horse entries ride. Applied via `DemonFactory.ApplyBeastWarp` from `DemonBattleBehavior`'s **first tick**, not `OnAgentBuild`: the skeleton is only guaranteed to exist by then (the same lazy timing the `DemonVisuals` shroud uses). Keep every value inside the vanilla-proven 0.8–2.1 envelope, and note `UseScaledWeapons(false)` is what stops a scaled hand bone from ballooning the wielded weapon.

  `ModuleData/monsters.xml` holds the additive `demon_hulking` Monster entry (`base_monster="human"`, so combat/animation stay compatible) giving Ravagers a genuinely larger capsule. It is purely additive — the shared vanilla `human` Monster is untouched, and must stay that way.
- `Spellbook/` — the player's only casting path (rune system, v0.10.0): `RuneCatalog` (36 runes: role/triplet/meaning/solo-working + starter-pair + legacy→rune migration map), `RuneSequenceMath` (pure binding-grammar resolver → `ResolvedWorking`), `RuneEffects` (dispatch to the effect layer), `SpellbookInputHandler` (hold-and-tap → chunk into runes → resolve → discovery + composed name, hands-free gate), `SpellbookCampaignBehavior` (unlock cost, known-runes `SPELLBOOK_KnownRuneIds`, v0.9-save migration, learn-on-draw, debug grant, book UI), `SpellburnEffects` (fizzle/strain → spellburn table). The retained legacy `SpellbookCatalog` (30–50 bound workings) / `SpellbookEffects` serve wands, the Rod, and NPC repertoires. Then the rare-caster layer `SpellcasterLords`/`SpellcasterLordMath` (≈15% of named lords/companions cast in battle — raised from 7% in v0.8.0 once the legacy NPC lord casters were retired for new games, see `LegacyContent.LegacyNpcCastersEnabled`) and `SpellcasterTroops`/`SpellcasterTroopCatalog`/`SpellcasterTroopMath` (a full, rare recruit→tier-5 caster tree). Pure math in `SpellbookMath`, `SpellcasterLordMath`, `SpellcasterTroopMath`.
- `Relics/`, `Wands/`, `Talismans/` — magical items looted from demons and ruins: each a `*Catalog` (pure data, generated/curated names), `*Effects` (mission-tick battle hooks, weakened Crystal/Dark Gift-style effects), and a pure `*Math` (Relics also add `RelicNaming`, a name-combinator). Demon-bane bonus damage (Requirement 20) is folded into the same `CastAttack`/relic-hit choke points the element system already used. `Wands/` additionally runs a wandwright shop (the one currently-held Tower town, the one Chosen town, and — permanently — the Children of the Forest's Pen Cannoc) stocking a small rotating case (`WandsMath.ShopStockSize`/`ForestShopStockSize`, re-rolled every `ShopRestockDays`, one purchase per slot) rather than the whole catalog, and grants — and, since v0.5.0, actually equips into `BattleEquipment`, self-healing weekly — a wand to a rolled-once subset of Tower, Chosen, and Children of the Forest lords.
- `Ruins/` — ~80% of castles become ownerless, explorable Ruins (Requirement 12): `RuinsCastleSystem` (session-start conversion, stable per save), `RuinsCatalog` (the new "ruined places of the old civilization" chamber list, same def-catalog shape as the legacy `AshenRuinDefs`), `RuinsExplorationSystem` (+`.WaitMenu` partial — Scouting-scaled wait-per-chamber, nightfall risk wired to `DemonSpawnCampaignBehavior`), `RuinsMenus`, pure `RuinsMath`.
- `CityStates/` — every town not claimed by one of the eight factions becomes a one-city, clan-named city-state that never joins a kingdom and recruits Looter/Bandit-culture rabble (Requirements 11, 24): `CityStateSystem`, `CityStateCampaignBehavior`, pure `CityStateMath`. Two settlements are special-cased into permanent, named "sanctuary kingdoms" instead of the generic path (`CityStateSystem.IsSanctuaryKingdom`, kept out of every war via a shared `AshenDiplomacyModel` predicate + daily force-peace backstop): **The Camp** (Revyl — a banner-less mercenary free-camp, original Sturgia troop tree kept) and **the Children of the Forest** (Pen Cannoc — Battania's culture/troop tree kept, no army of their own, their lords held permanently in a young-adult age window via a weekly `SetBirthDay` reanchor; see `Wands/` above for their wand economy and their weapon-free market).
- `Apocalypse/` — the campaign clock (Requirements 32, 33): `ApocalypseCampaignBehavior` (+ `.Gathering.cs`/`.Resolution.cs` partials) schedules the Night of the Hunt (every 20–82 days), the day-300 rumours, the day-600 gathering band, and the day-1000+ Demon Lord endgame; `DemonLordSystem` gives him a real campaign presence (party/army/sieges, boss-tier stats, victory/defeat resolution). Pure `ApocalypseMath`.
- `MortalLaw/` — NPC lords live under the same rules as the player (Requirement 6): `MortalLawCampaignBehavior` (+ `.Hunger.cs` raiding-for-food, `.NightFear.cs` night-travel avoidance, `.Rosters.cs` scarcity-shaped rosters), pure `MortalLawMath`.
- `Economy/` — the barter/scarcity economy (Requirements 3, 5, 9, 31): `ScarcityModels.cs` (GameModel overrides — wages, upgrade cost, building cost, battle/plunder gold, ransom, garrison/militia growth, auto-recruitment), `MarketScarcityCampaignBehavior` (daily town food/horse/weapon/armour supply pruning — horses are gated behind a deterministic per-town-per-day roll, `EconomyMath.TownSellsHorsesToday`, so most towns show none at all; the Children of the Forest's own market strips weapons entirely, `ForestWeaponSaleQuantity`), pure `EconomyMath`. **Decision recorded in `EconomyMath.cs`'s header: Path B (gold ~10× scarcer everywhere), not Path A (gold removed)** — the town trade screen and party item-exchange popup hard-code gold in TaleWorlds' own view-models with no model seam to remove it cleanly.
- `Factions/` — one subfolder per reworked kingdom (Requirement 10), each with a `*Culture.cs` (rename + vassal-title dialogue sweep), `*CampaignBehavior.cs` (+ `.Menus.cs` and other concern partials for the faction's unique mechanic), `*Dialogue.cs`, `*Settlements.cs` (starting-town list), and a pure `*Math.cs`: `WolfBrothers` (Sturgia, cannibalism), `Tower` (Aserai, spell teaching), `ForestWidows` (Battania — collective "we" dialogue, the Integrated network), `Bloodbound` (Khuzait — Demon Blood economy, blood attunement), `Temple` (Vlandia — Holy Sigils, prayer), `Empire` (Northern Empire — free grain, Schemes access), `Legion` (Western Empire — aggression, training fields), `Chosen` (Southern Empire — matriarchal succession, sacrifice).
- `FactionQuests/` — one questline per faction, unlocked from day ~50 via `FactionQuestTriggerCampaignBehavior`/`FactionQuestTrigger`, each following the `GreatAwakening`/`NorthmenStones` donation- or gather-then-resolve pattern from the legacy systems below. Pure `FactionQuestMath` holds shared balance constants.
- `Expeditions/` — the Antiquarian Charter: pick a procedurally generated leader and core team, pay gold, and send them into a ruin for several days; success yields gold/gear/crystals/renown, failure can cost the leader. One at a time. **Gated on The Camp** (`CityStateSystem.IsCampSettlement`) — it shipped Legion-gated in v0.2.0 and moved to The Camp in v0.4.0, so treat any "Legion Expeditions" wording in comments or docs as stale. Pure `ExpeditionMath`.
- `ForeignMuster/` — Legion (`empire_w`) towns offer one other main culture's tier-1 recruit, rotating weekly (deterministic per town-and-week, so **no save state**), priced as the town's own recruit and capped per town per week. Pure `ForeignMusterMath`.
- `BeastsOfTheNorth/` — Wolf Brothers (Sturgia) towns recruit two costly troops paid in fish and gold: the **Jotunn-Blooded** (a true giant — see the skeleton-scale note under `Demons/`) and the **Ulfhednar** (wolf-rider on a re-tinted vanilla horse; no invented wolf mesh exists). Capped per town per month. Pure `BeastsOfTheNorthMath`.
- `Units/` — cross-cutting troop/gear rules: `PromotionCampaignBehavior` + `PromotionToll`/`RecruitToll` (scarcity-priced promotion and recruitment), and `GearWeathering`/`LordGearWeathering` (Requirement 29's lord-equipment de-blinging — note it explicitly **exempts wand items** so it cannot race the `Wands/` weekly equip pass). Pure `UnitsMath`.

**Shared core (not a system — the plumbing everything else reads):**

- `Mage/` — `MageKnowledge` (+ `.Events`/`.UI` partials): the central persistent hero-state object described under "State" below, plus the shared `_deferredInquiry` popup slot (see `behaviour.md` before using it).
- `Talents/` — `TalentSystem` (+ `.Player`/`.NpcSpells`/`.MapSpells` partials) and `TalentCostCurve`, the focus-point economy described under "Talent and Focus Point Costs" below.

**Legacy Ash and Ember systems (baseline; still live — NPC casting, underlying effects, and several still-retained player features run through these):**

- `Magic/` — the unified element system: `ElementMagicInput` (battle input — player path retired, `PlayerCastingEnabled = false`; NPC lords and the Awakened still cast through `CastAttack`), `ElementSpellEffects`, `ElementWallWards`, `ElementUltimates`, `ElementMapSpells` (disabled entirely — no campaign-map spells exist, Requirement 14), `MagicLearning` (the Codex), `MageElementKnowledge`, teacher dialogue, and pure `*Math.cs`.
- `Spells/` — the **legacy** two-phase Inner Fire (`SpellEffects.*` partials, `BlastSpells`, `SelfSpells`, `CreateSpells`, enchantments). Still drives NPC mage casts and the shared battle-effect ticks.
- `Nature/` — the Living Ember: charges, living-energy economy, seers, backlash (player input retired the same way as `Magic/`; the nature-discipline effects are still cast by NPC seers and folded into the Wind/Earth/Water Spellbook spells). Pure math in `LivingEnergyMath`/`NatureCharge`.
- `Miracles/` — Grace: prayers, grace economy, priest troops, battle AI, talents (player input retired; NPC priests/lords and `PriestTroops` still cast). Pure math in `MiracleMath`. **Lore:** Grace is not bestowed by a deity — it is the same Fire the rest of the mod draws on, called through the caster's own emotional and intellectual alignment (expressed as a personality trait) rather than a drawn cone. Flavour/effect text must never write "the light" as a watching, judging, or granting party; the caster (or their own conviction) is always the one deciding. The Temple's priests may *describe* it as divine favor as their institutional gloss, but player-facing miracle text should stay in the caster's own voice.
- `DarkGifts/` — the Dark Gift path (battle effects + `DarkGiftSystem`); its per-hero effects are the weaker template Relics/Talismans re-balance to per-item.
- `Crystals/` — consumable crystal items (`CrystalCatalog`, `CrystalEffects`, `CrystalBattleAI`, `CrystalMath`); the template Relics re-balance to permanent-but-weaker.
- `Elementals/` — **The Awakened** (named *the Kindled* until v0.7.0 — the rename was player-facing strings **only**: every code identifier, troop id (`sacred_kindled_*`), and save key deliberately still reads `Kindled`/`Elemental`, and must stay that way for save compatibility): elemental beings (fire/water/stone/ice/sand/storm) that roam the wilds, are summoned by mages, or wake mid-battle. `ElementalFactory` builds one; `ElementalBeings` is the mission registry that drives their look + the element/physical weakness; `ElementalVisuals` owns that look — continuous element particle systems bound **once** to each being's skeleton bones (pelvis/chest/head/hands, so the fire/mist/dust rides every limb for free), one follower light per body (created once, only repositioned each tick), and a persistent coloured contour — replacing the old per-tick particle re-stamping that churned GameEntities every frame; `ElementalWildsBehavior` breeds roaming bands (persisted under `ELEM_*` keys); pure `ElementalMath` holds the weakness wheel. The Spirit Unbinding's champion (`ElementUltimates`) is unified onto this core. The `ElementalKind` enum lives in `ElementUltimateMath`. **`DemonVisuals`/`DemonFactory` are this system's direct descendants** — same bind-once bone-particle/light/contour pattern, re-tuned smoke-black/ember-red instead of elemental colours.
- `Schemes/` — covert operations (`SchemeSystem.*`, `SchemeCampaignBehavior.*`, minigame). **Modified for the Empire faction:** access is gated to `EmpireCulture.IsPlayerEmpireKingdom` (player menu only — NPC scheme AI is untouched) and every scheme is now paid in influence alone (`GoldCost` is 0 everywhere; the old gold price was folded into `InfluenceCost` to preserve the cost hierarchy — see the comment block in `SchemeSystem.cs`).
- `Soldier/` — **Take the Lord's Coin**: hire your party out to a warring lord as a common soldier from clan level 0. `SoldierServiceCampaignBehavior` (state/tick/army + weekly pay) and its `.Dialogue` partial; pure `SoldierServiceMath` (pay, desertion penalties). Attaches as a mercenary of the lord's realm (`ChangeKingdomAction.ApplyByJoinFactionAsMercenary`) **and folds the player's party into the commander's host as a true army member** (`Army.AddPartyToMergedParties`; if the commander leads no army, one is raised for him via `new Army(...)` and dissolved on release; if that ever throws, `_armyCreateFailed` trips a `SetMoveEscortParty` fallback) — so the player marches with the company and auto-joins his battles on his side, earning renown. **The deal is sealed from inside the map meeting with the lord, so the conversation consequence changes NO faction/army state** (doing so mid-encounter corrupts it into a hostile Attack/Surrender resolution and crashes) — it only records the terms and sets `PlayerEncounter.LeaveEncounter = true`; the first clean map tick (`!IsEncounterLive()`) runs `FinalizeJoin` (mercenary contract + `ReassertArmy`). Leaving the host (vanilla "Abandon Army", caught via `OnPartyLeftArmyEvent`) before the agreed term is desertion; clean release with a bonus after. Membership is idempotently self-healed each tick (`ReassertArmy`) and the raised host's cohesion topped up (`SustainArmy`). A host **we** raised for a lone commander is only held for `SoldierServiceMath.HostHoldDays`, then `SustainArmy` dissolves it and opens a `HostBreatherDays` window (player rides escort) so the campaign AI gets a clean chance to draft him into — or let him raise — a real war host; a party already in an army can neither create one nor be summoned to one (vanilla `CanLordCreateArmy`/`CheckPartyEligibility` both gate on `MobileParty.Army`), and the self-heal then folds the player into whatever real host he joins. A host the commander gathered himself is never touched.
- `Sea/` — harbors, voyages, trade ventures, NPC sea lanes; pure `SeaMath`. **Retained untouched** through the whole conversion (Requirement 22) — fares are computed independently of the Economy scarcity models.
- `Markets/` — the Exchange / commodity speculation (`ExchangeCampaignBehavior.*`, pure `SpeculationMath`).
- `QuestSystems/` — Dragon main quest, Burning Lab questline, settlement encounters, world events (`CampaignMapEvents.*`), battlefield events. The donation-accumulation pattern (`GreatAwakening/`) and the gather-in-one-town pattern (`NorthmenStones/`) are the explicit reuse templates for several `FactionQuests/` questlines.
- `AI/`, `Tribes/`, `ClanOrders/`, `Conclave/`, `AshenRuins/`, `Apprentice/`, `Tavern/`, `Campaign/`, `Visual/`, `Startup/` — supporting culture, faction, atmosphere, and UI-flow systems. `Tavern/` (dice games, drinking) is **retained untouched** (Requirement 22). `AshenRuins/` is the direct structural template `Ruins/` was built from. `AI/AshenCitySystem.*` is the direct template `CityStates/` was built from, and its retired baseline rename helpers (`RenameNorthmenKingdom`, `RenameDunebornKingdom`, `RenameForestClansKingdom`, etc.) are kept **unreferenced but present** for save compatibility — do not delete them.

### Spell Cast Pipeline

**Current (player): the Spellbook.** `SpellbookInputHandler.Tick` reads the formula gesture (hold Alt/Controller-RLeft, tap the U/D/L/R sequence with free hands) and matches it against `SpellbookCatalog`; a correct sequence dispatches to `SpellbookEffects`, which for the elemental spells simply calls the same `ElementSpellEffects`/`ElementWallWards`/`NatureEffects` entry points described below (Fireball/Firewall/gust/torrent/entangle/nova and their walls), plus Spellbook-only spells (Summon Demon via `DemonFactory`, Banish Demons, Light, wards/curses/veils). An incomplete or wrong sequence fizzles and rolls `SpellburnEffects` (base 60%, reduced by Intellect). This is the **only** player casting path — see `LEGACY.md` and the retired-input note in the legacy systems list for what it superseded.

**Underlying element pipeline (still the effect layer under the Spellbook, and still the player's path before Phase 4 retired it for NPC-cast parity discussion below):** `ElementMagicInput.Tick` reads Focus + direction + a stand-still charge, then `ElementSpellEffects` / `ElementWallWards` / `ElementUltimates` resolve the attack, wall, or ultimate. Life-cost is **flat** (the charge buys power, not a cheaper cast); the Nature discipline lowers it, and the Ashen pay in criminal standing.

**Attack forms (per element — each has its own silhouette so they read apart):**

| Element | Attack shape | Implemented in |
|---|---|---|
| Fire | **Flying bolt that explodes on impact** (bursts on first foe reached or at range's end) | `ElementSpellEffects.FireMissile` + `TickBolts`/`ExplodeBolt` (the `_bolts` list, ticked from `Tick`) |
| Wind | **Forward gust/stream** (broad wedge, knockback drives foes ahead) | `NatureEffects.BattleGale` (shared source) |
| Earth | **Short, almost-melee cone of erupting rock** (close fan, heavy damage + root — reach traded for force) | `NatureEffects.BattleEntangle` (shared source) |
| Water | **Forward slowing wave** (cone) | `NatureEffects.BattleTorrent` (shared source) |
| Spirit | **Nova** (radial panic + random enemy order) | `ElementSpellEffects.SpiritPanic` |

`CastAttack(el, caster, power)` is the single dispatch choke point — the player (`ElementMagicInput`), NPC lords (`ElementLordAI`), and the Awakened (`ElementalBeings`) all cast through it, so changing an attack shape there is automatically NPC-parity-correct. It also folds in the **mastery scale** (`ElementMagicMath.MasteryScale(hero.Level)`, +1%/level capped at +30%) by multiplying `power` for hero casters — because `ChargeFraction` clamps at 1, a `power > 1` lifts only the direct damage, never the tuned cone reach / wall depth / ignite (the same path the overchannel already rides). Non-hero casters (troops, the Awakened) map to no hero and keep ×1. Crystals scale the same way through `CrystalEffects.Potency` (player Medicine → `CrystalMath.MasteryScale`); miracle **damage** through `MiracleEffects.Conviction` (caster's summed aligned virtue → `MiracleMath.ConvictionScale`). **Wind/Earth/Water still delegate to the shared `NatureEffects` (Gale/Entangle/Torrent), which the Living Ember nature discipline also casts** (`NatureSeerAI`, the nature input handler — both still live in `MagicSystem`), so reshaping them there deliberately reshapes the nature-discipline versions too (consistent with the "one magic" unification). The fire bolt is a self-contained projectile (no legacy `SpellCast`/`Agent.Main` dependency) so it works for any caster; it trails fire each tick and is cleared with the rest of battle state via `ElementSpellEffects.ClearBattleState`.

**Legacy (NPC and underlying effects):**
```
MagicInputHandler (Alt+Direction buffers)
  → SpellBuilder.Parse(formBuffer, effectBuffer) → SpellCast
  → AgingSystem.ComputeBattleAgingCost(totalInputs) → days cost
  → SpellEffects.Execute*() → dispatches by spell type
```
`SpellEffects.cs` is the core partial class; `BlastSpells.cs`, `SelfSpells.cs`, `CreateSpells.cs`, and `AffectSpells.cs` extend it by spell form. NPC mage lords still cast through this path.

### State: Static vs. Serialized

- **In-mission state** lives in static fields on `SpellEffects`, `ActiveEffects`, the `Element*`/`Nature*`/`Miracle*`/`Crystal*` classes, `ElementLordAI`, etc. `MainSubModule.OnGameStart()` and `MagicMissionBehavior.OnEndMission()` both clear all of it to avoid save-reload / mission carry-over.
- **Persistent hero state** is stored in `MageKnowledgeData` (serialized into the campaign save via TaleWorlds' `CampaignObject` extension API). This holds talent purchases, aging ledger, grimoire unlocks, whisper tiers, Rival Shadow counter, and pending event flags.
- **Other persistent state** is saved per-behavior, mostly as parallel lists keyed by prefixed strings (`SEA_*`, scheme, exchange, clan-order keys) via each behavior's `SyncData`, plus custom savedata types registered in `SaveDefiner.cs`. Nature reserves, Grace, and Dark Gifts persist through their own knowledge/inventory objects. Voyage-in-progress state is intentionally **not** serialized (a mid-crossing reload refunds the fare).

### Campaign Tick Architecture

`MagicCampaignBehavior` hooks three tick rates:
- **Daily:** aging decay, Whisper tier decay, Ashen resurgence logic
- **Weekly (14+ day slots):** independent general-event and war-event queues in `CampaignMapEvents`
- **On settlement enter/leave:** `SettlementEncounters`, gated by cooldown + renown + mage status

The other behaviors register their own daily/weekly/enter-leave hooks (e.g. `NatureCampaignBehavior`, `MiracleCampaignBehavior`, `SchemeCampaignBehavior`, `SeaCampaignBehavior`, `ExchangeCampaignBehavior`, `ClanOrdersCampaignBehavior`). Keep new tick logic in the behavior that owns the concern rather than piling it onto `MagicCampaignBehavior`.

### NPC Mage AI

`ElementLordAI.TryCast()` runs on cooldowns that vary by personality:
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
