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
| `docs/conventions.md` | Code conventions (naming, folder layout, pure `*Math.cs`, null-guards, `ModLog` catch rule, pure tests) | LIVING |
| `docs/gotchas.md` | Frozen-names table + corrections/gotchas — read before any rename or "cleanup" | LIVING |

`dist/TheDarkestNight/` is a generated release snapshot produced by
tools/pack.ps1. It lags src/ and ModuleData/ by design. Never read it as
source of truth and never hand-edit it.

### Frozen names — read before any rename

The namespace, assembly, DLL, module folder, and test project are all `TheDarkestNight`. A handful of `AshAndEmber` / `aae_` / `Ashen*` strings remain **on purpose** (persisted item ids, save-computed quest types, upstream-attribution comments, dead-but-kept helpers). **A blanket find-and-replace of `AshAndEmber` across this repo will corrupt saves and break the test build.** The full table of what stays and why is in **[`docs/gotchas.md`](docs/gotchas.md)**; the rename plan is `REFACTOR_NAMING.md`.

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

## Conventions & gotchas

- **Code conventions** (naming, one-system-per-folder, pure `*Math.cs`, null-guards, the never-swallow-silently `catch` + `ModLog` rules, pure tests) → **[`docs/conventions.md`](docs/conventions.md)**.
- **Corrections and gotchas** (agent scaling, Camp-gated Expeditions, the Awakened/Kindled split, Economy Path B, Soldier-Service encounter rule, unserialized voyages, `TalentId` retired members, the "Legacy two-phase" caveat, and more) → **[`docs/gotchas.md`](docs/gotchas.md)**.

## Working behaviour

Process and working-style guidance (how to build, verify the TaleWorlds API, bump
the version, and avoid mod-conflict crashes) lives in a separate file:

@behaviour.md
