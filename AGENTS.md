# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

**The Darkest Night** (mod id `TheDarkestNight`, presented in-launcher under that name; see `SubModule.xml`) is a total-conversion of Mount & Blade II: Bannerlord (~106K lines, ~430 C# files under `src/`) built on top of **Ash and Ember**, a magic-overhaul mod whose codebase is this project's baseline and "parts bin" — **reuse only; this is a new thing, not a continuation.** The root namespace is `TheDarkestNight`, the assembly is `TheDarkestNight.dll`, and it deploys to `Modules/TheDarkestNight/`. The remaining Ash and Ember names are legacy **type** names (`Ashen*`, `Miracle*`, `EmberConclave*`) — cosmetic leftovers pending `REFACTOR_NAMING.md` Phase 4 — plus the deliberately-frozen strings in the table below. Target framework: .NET Framework 4.7.2. Sandbox only — "New Campaign" (StoryMode) is intercepted and blocked (`Startup/SandboxOnlyGate`).

Player-facing details live in `README.md`; the retired player-facing caster paths (still the authoritative description of how spells *behave* for NPC casters) are in `LEGACY.md`; the release history is in `CHANGELOG.md`.

## Doc map

| File | Purpose | Status |
|---|---|---|
| `README.md` | Player-facing description of current mechanics | LIVING |
| `LEGACY.md` | Retired player-facing caster paths; still the authoritative description of NPC caster behaviour | LIVING |
| `CHANGELOG.md` | Release history (recent versions; older entries archived) | LIVING |
| `RECENT_CHANGES.md` | Session-by-session change log for Codex sessions | LIVING |
| `FOR_OTHER_LLMs.md` | Scratch handoff pad for in-progress work between sessions | LIVING |
| `REFACTOR_NAMING.md` | Working plan for the `AshAndEmber` → `TheDarkestNight` rename (Phase 1 done, Phases 0/2–4 queued; delete once Phase 4 lands). Phase 4 still outstanding - the frozen-names table depends on this. | LIVING |
| `LORE_REVIEW.md` | Record of player-facing prose already written/re-themed, grouped by system | HISTORICAL |
| `RUNE_MAGIC_PLAN.md` | Design of record for the rune casting system; marked "IMPLEMENTED in v0.10.0" | HISTORICAL |
| `PROMPT_THE_DARKEST_NIGHT.md` | Original build prompt used to bootstrap the conversion from Ash and Ember | HISTORICAL |
| `SCHEME_MINIGAME_GUIDE.md` | Implementation guide for the scheme minigame | UNCLEAR - verify |
| `behaviour.md` | How-to-work guidance (build/test, TaleWorlds API verification, mod-conflict safety); imported into this file via `@behaviour.md` | LIVING |

`dist/TheDarkestNight/` is a generated release snapshot produced by
tools/pack.ps1. It lags src/ and ModuleData/ by design. Never read it as
source of truth and never hand-edit it.

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

## Architecture index

| File | What's in it |
|---|---|
| `docs/arch/wiring.md` | `SubModule.xml`/`MainSubModule` entry point: game-start resets, behavior registration, culture-text/hotkey wiring, mission-behavior injection |
| `docs/arch/systems-current.md` | Folder map of the Darkest Night systems (Veil, Demons, Spellbook, Relics/Wands/Talismans, Ruins, CityStates, Apocalypse, MortalLaw, Economy, Factions, FactionQuests, etc.) plus the shared core (Mage, Talents) |
| `docs/arch/systems-legacy.md` | Folder map of the retained legacy Ash and Ember systems (Magic, Spells, Nature, Miracles, DarkGifts, Crystals, Elementals, Schemes, Soldier, Sea, Markets, QuestSystems, and supporting folders) |
| `docs/arch/casting.md` | The spell cast pipeline (current Spellbook + legacy two-phase), the attack-forms table, and the player casting model (world pitch + Spellbook mechanics) |
| `docs/arch/state-and-ticks.md` | Static vs. serialized state, and the campaign tick architecture |
| `docs/arch/subsystems.md` | NPC mage AI, ritual systems (Sanctuary/Ashen Altars), sea systems, talent/focus point costs |
| `docs/arch/constants.md` | Key numerical constants (cross-cutting and legacy two-phase values) |

## Corrections and gotchas

- **Agent scaling IS possible.** `Agent.SetInitialAgentScale` (whole-body) and `MBAgentVisuals.ApplySkeletonScale` (per-bone) both work; the v0.2.0 "no safe runtime agent-scale surface was known" note is obsolete — do not repeat it. See `docs/arch/systems-current.md` (Demons).
- **Expeditions are Camp-gated, not Legion-gated.** It shipped Legion-gated in v0.2.0 and moved to The Camp in v0.4.0 — treat any "Legion Expeditions" wording anywhere as stale. See `docs/arch/systems-current.md` (Expeditions).
- **The Awakened were renamed from "the Kindled" in player-facing strings ONLY.** Every code identifier, troop id (`sacred_kindled_*`), and save key deliberately still reads `Kindled`/`Elemental`, and must stay that way for save compatibility. See `docs/arch/systems-legacy.md` (Elementals).
- **Economy uses Path B (gold ~10× scarcer everywhere), not Path A (gold removed).** The town trade screen and party item-exchange popup hard-code gold in TaleWorlds' own view-models with no model seam to remove it cleanly. See `docs/arch/systems-current.md` (Economy).
- **Retired rename helpers in `AI/AshenCitySystem` are deliberately unreferenced-but-present** (`RenameNorthmenKingdom`, `RenameDunebornKingdom`, `RenameForestClansKingdom`, etc.) for save compatibility — do not delete them. See `docs/arch/systems-legacy.md`.
- **God-King / Tribes strings are dead code kept on purpose.** Before "fixing" a God-King string, check whether its function has a call site — these have none, and `BloodboundCulture`/`BloodboundDialogue` own the live path. See "Names that must NOT be tidied up" below.
- **`TalentId` carries retired class/path enum values for save compatibility.** An enum member is not necessarily a live, purchasable talent — check `TalentSystem`'s definition table. See `docs/arch/subsystems.md` (Talent and Focus Point Costs).
- **The Soldier Service map-meeting conversation must change NO faction/army state.** Doing so mid-encounter corrupts it into a hostile Attack/Surrender resolution and crashes — it only records terms and sets `PlayerEncounter.LeaveEncounter = true`; the real join happens on the next clean map tick. See `docs/arch/systems-legacy.md` (Soldier).
- **Voyage-in-progress state is intentionally not serialized.** A mid-crossing reload refunds the fare rather than resuming the voyage — don't assume it survives a reload. See `docs/arch/state-and-ticks.md`.
- **The "Legacy two-phase" numeric values are for NPC casts / underlying effects only — verify against code before relying on them.** The current player casting model is flat-cost, charge-scaled, not per-input. See `docs/arch/constants.md`.

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
