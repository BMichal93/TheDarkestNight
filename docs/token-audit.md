# Token Audit

## 1. Repo snapshot

- Tracked files: **482**
- Extension breakdown: `.cs` 441, `.xml` 20, `.md` 13, `.ps1` 3, `.dll` 2, `.csproj` 2, `.gitignore` 1
- Top-line-count files (raw `wc -l`, note the two `.dll` entries are binary noise, not real lines):
  1. `dist/TheDarkestNight/bin/Win64_Shipping_Client/AshAndEmber.dll` — 47,876 (binary, 2.9M)
  2. `dist/TheDarkestNight/bin/Gaming.Desktop.x64_Shipping_Client/AshAndEmber.dll` — 47,876 (binary, 2.9M, identical blob hash to #1)
  3. `tests/PureLogicTests.cs` — 6,626 (662 test methods, one file)
  4. `CHANGELOG.md` — 1,697
  5. `ModuleData/items.xml` — 1,637
  6. `src/AshenRuins/AshenRuinSystem.cs` — 1,488
  7. `src/QuestSystems/SettlementEncounters.Events1.cs` — 1,423
  8. `ModuleData/troops.xml` — 1,308
  9. `dist/TheDarkestNight/README.md` — 1,278 (stale fork of root `README.md`, 683 lines diverged)
  10. `src/QuestSystems/SettlementEncounters.Events6.cs` — 1,265
- 44 `.cs` files exceed 500 lines; `src/QuestSystems/` alone is 19,607 lines across 45 files (already split into partials by concern, which is the correct pattern per `CLAUDE.md`).
- Context files present: `CLAUDE.md` (217 lines), `behaviour.md` (131 lines, imported via `@behaviour.md`), `.gitignore` (26 lines). No `.claudeignore`, no `.cursorrules`, no `AGENTS.md`.
- `CLAUDE.md` is dense but **not stale or duplicated** — it cross-references `behaviour.md` rather than repeating it, and its "Names that must NOT be tidied up" table exists specifically to prevent expensive rediscovery/mistakes. Total onboarding context (`CLAUDE.md` + `behaviour.md`) is 348 lines, which is reasonable for a 441-file C# codebase — the risk here is confirmed-drift docs (`dist/`), not the root context file.
- `dist/TheDarkestNight/` (14 tracked files: 2 DLLs, `README.md`, `SubModule.xml`, `install.ps1`, `GUI/*`, `ModuleData/*`) is a **packaged, checked-in build artifact directory** that has drifted significantly from source: `ModuleData/items.xml` (1,637 lines source) vs. `dist/.../items.xml` (358 lines) differ by 1,280 lines; `troops.xml` (1,308 vs 477) is similarly stale; `README.md` differs by 683 lines.

## 2. Top 10 token sinks

| Rank | Path | Category | Size | Impact | Fix |
|---|---|---|---|---|---|
| 1 | `dist/TheDarkestNight/bin/**/AshAndEmber.dll` (×2) | A | 2.9M each, identical blob | Binary; if ever globbed/read/hashed by a broad search it wastes a full tool call and returns garbage tokens; also doubles repo weight for no reason (same blob twice) | `.claudeignore` the `dist/` tree; keep in git via LFS or a release artifact instead of duplicating the identical file under two folder names |
| 2 | `dist/TheDarkestNight/ModuleData/*.xml`, `dist/TheDarkestNight/README.md`, `dist/TheDarkestNight/GUI/*` | A/D | ~2,000 lines, stale by 600–1,300 lines vs. source | A search that isn't scoped to `src`/`ModuleData` can surface the **wrong, outdated** copy and silently misinform an edit or answer | `.claudeignore` `dist/**` except maybe `install.ps1`; regenerate `dist/` only at release time via `tools/pack.ps1`, never hand-edit or let Claude read it as source of truth |
| 3 | `tests/PureLogicTests.cs` | B | 6,626 lines / 662 test methods, single file | Any task touching tests (add/find/fix one test) risks a full-file read or a large grep context; also the single biggest non-binary file in the repo | Split by system to mirror `src/` folders, e.g. `tests/Veil.Tests.cs`, `tests/Spellbook.Tests.cs`, `tests/Economy.Tests.cs` (same partial-class-by-concern convention already used in `src/`) |
| 4 | `CHANGELOG.md` | B | 1,697 lines | Rarely needed in full; a "what changed recently" question only needs the top few versions, but a naive read pulls the whole history | Keep only the last ~5 versions in `CHANGELOG.md`; move older entries to `CHANGELOG_ARCHIVE.md` (link it from the top) |
| 5 | `src/QuestSystems/SettlementEncounters.Events1-7.cs` | B | 1,423 / 1,265 / 1,129 / 1,026 / 874 / 515 / 187 (≈6,700 total) | Six same-named-pattern files; a question like "find the encounter for X" often means grepping all seven before narrowing down | Not urgent (already partial-split correctly) — but consider a one-line index comment or a `SettlementEncounters.Index.md`-style header per file listing the encounter IDs it contains, so `Grep` for an ID resolves in one pass instead of reading each Events file |
| 6 | `ModuleData/items.xml` / `troops.xml` vs `dist/` duplicates | A/C | 1,637+1,308 (source) vs 358+477 (stale dist copies) | Two representations of the same domain data force a diff-check every time someone asks "is this item/troop defined?" to be sure which copy is authoritative | Document in `CLAUDE.md` (or delete) that `ModuleData/` at repo root is the only source of truth, `dist/` is generated-and-frozen at ship time |
| 7 | `src/AshenRuins/AshenRuinSystem.cs` | B | 1,488 lines, single file | Not yet split into partials despite being one of the largest files in `src/`, unlike `SettlementEncounters`/`CampaignMapEvents` which already use the partial pattern | Split by concern (e.g. `.Chambers.cs`, `.Rewards.cs`, `.Menus.cs`) following the project's own stated convention |
| 8 | `src/Campaign/AmbientRemarks.cs` | B | 1,146 lines, likely mostly string-literal data | Large flavour-text tables read in full even when only one line is relevant | If it's mostly string tables (not logic), consider moving the literal text to a data file (`ModuleData/*.xml` or a resx) so code search doesn't have to scan prose to find logic |
| 9 | `src/Visual/AreaEffects.Particles.cs` | B | 1,124 lines | Same pattern — likely dense particle-config boilerplate | Lower priority; only worth splitting if frequently touched |
| 10 | `SCHEME_MINIGAME_GUIDE.md`, `RUNE_MAGIC_PLAN.md`, `LORE_REVIEW.md`, `PROMPT_THE_DARKEST_NIGHT.md` | D | 905 + 550 + 298 + 438 = 2,191 lines | These read like point-in-time design/planning docs, not living references; unclear if still authoritative vs. superseded by shipped code (e.g. `RUNE_MAGIC_PLAN.md` for a system marked "v0.10.0" and already implemented) | Audit each for "is this plan now fully implemented?" — if so, fold any surviving facts into `CLAUDE.md`'s relevant section and delete/archive the planning doc so it stops being a candidate for accidental reads |

## 3. Quick wins (do first, low risk)

1. **Add a `.claudeignore` excluding `dist/`, binaries, and build output.** Estimated reduction: **high** (removes ~6M of binary + ~2,000 lines of stale duplicate XML/MD from any broad search). Risk: **low** — `dist/` is a packaging output, not source; nothing in the working code depends on Claude reading it.
2. **Point-fix: never let a search span both `ModuleData/` and `dist/TheDarkestNight/ModuleData/`.** Estimated reduction: medium (avoids reading the wrong, stale copy and re-deriving which one is correct). Risk: low — purely additive guidance, no file changes beyond the ignore file below.
3. **Trim `CHANGELOG.md` to recent versions + an archive file.** Estimated reduction: medium (1,697 → maybe 300 lines for the live file). Risk: low, but must preserve full history in the archive file (don't delete data, just relocate).
4. **Confirm whether `RUNE_MAGIC_PLAN.md`, `SCHEME_MINIGAME_GUIDE.md`, `LORE_REVIEW.md`, `PROMPT_THE_DARKEST_NIGHT.md` are still live references or completed-plan artifacts; archive/delete the completed ones.** Estimated reduction: medium (up to ~2,200 lines removed from the "docs that might get read" set). Risk: low if verified against `CHANGELOG.md`/git history first (a plan doc for a shipped v0.10.0 feature is very likely done).
5. **Add a one-line note in `CLAUDE.md`'s "Names that must NOT be tidied up" style table (or a new short section) stating `dist/` is a generated release snapshot, not a place to read or edit code from.** Estimated reduction: low-medium (prevents future sessions from treating dist/ as source). Risk: low.

Proposed `.claudeignore`:

```
# Packaged/shipped build output — stale duplicate of src/ and ModuleData/, never a source of truth
dist/

# Build artifacts (already gitignored, but belt-and-suspenders for any local untracked copies)
src/bin/
src/obj/
tests/bin/
tests/obj/
**/bin/
**/obj/

# Binaries — never useful as text context
*.dll
*.exe
*.pdb

# VCS / editor internals
.git/
.vs/
.idea/
```

## 4. Structural refactorings (do later, higher effort)

1. **Split `tests/PureLogicTests.cs` (6,626 lines) into per-system test files mirroring `src/` folder names** (e.g. `VeilMathTests.cs`, `EconomyMathTests.cs`, `SpellbookMathTests.cs`), matching the "one system per folder" / partial-class-by-concern convention `CLAUDE.md` already mandates for production code. Estimated reduction: high for any single-system test task (read one ~300–600 line file instead of the whole 6,626-line file). Risk: **medium** — must preserve all 662 test methods and keep `dotnet test` green; mechanical but large diff. Effort: **M**.
2. **Split `src/AshenRuins/AshenRuinSystem.cs` (1,488 lines) into concern partials**, following the pattern already used for `SettlementEncounters.*.cs` / `CampaignMapEvents.*.cs`. Estimated reduction: medium. Risk: low (same file, same class, just partitioned). Effort: **S**.
3. **Stop tracking `dist/TheDarkestNight/bin/**/AshAndEmber.dll` in git, or move to Git LFS / GitHub Releases**, and regenerate the rest of `dist/` from `tools/pack.ps1` at release time only (never hand-maintained in parallel with `src/`/`ModuleData/`). Estimated reduction: high (repo weight and duplicate-blob confusion). Risk: **medium** — `.gitignore` currently has an explicit comment saying these DLLs are "deliberately NOT ignored" for players who install without building; changing this requires confirming `install.ps1`'s distribution story still works (e.g. via a GitHub Release asset instead of a repo-tracked binary). Effort: **M** (needs a user decision, not just cleanup).
4. **Reconcile or delete the stale `dist/TheDarkestNight/ModuleData/*.xml` and `dist/TheDarkestNight/README.md` copies** — either wire `tools/pack.ps1` to regenerate them on every release (so they're never manually stale) or drop them from git entirely and build `dist/` on demand. Estimated reduction: medium. Risk: low-medium (need to confirm nothing manually edits the dist copies today — the 683/1,280-line drift suggests they already aren't kept in sync). Effort: **S–M**.
5. **Add a short `docs/` index or table-of-contents section in `CLAUDE.md` listing each root `.md` file's purpose and whether it's a "living reference" or "historical/planning" doc** (`LEGACY.md`, `REFACTOR_NAMING.md`, `LORE_REVIEW.md`, `RUNE_MAGIC_PLAN.md`, `PROMPT_THE_DARKEST_NIGHT.md`, `SCHEME_MINIGAME_GUIDE.md`). Estimated reduction: medium (prevents opening a stale plan doc trying to answer a "how does X work now" question). Risk: low. Effort: **S**.

## 5. Proposed CLAUDE.md

> Note: the repo's actual `CLAUDE.md` already contains substantially more (and more precise) architectural detail than a 120-line file can hold — that detail is genuinely load-bearing (e.g. the frozen-strings table, the tick architecture, the numeric-constants table) and should **not** be deleted. The version below is what a from-scratch 120-line file would look like if forced to that budget; treat it as a "if we ever had to compress" reference, not a recommendation to actually replace the current, richer file.

```markdown
# CLAUDE.md

## Project
The Darkest Night (mod id `TheDarkestNight`) is a total-conversion Bannerlord mod
(~441 C# files, .NET Framework 4.7.2) built on the Ash and Ember magic-overhaul
codebase — reuse only, not a continuation. Root namespace `TheDarkestNight`,
assembly `TheDarkestNight.dll`, deploys to `Modules/TheDarkestNight/`. Sandbox-only;
StoryMode is blocked. Player casting is the rune-based Spellbook (`src/Spellbook/`);
legacy element/miracle/nature input paths are retired for players but still drive
NPC casters. See `README.md` (player-facing), `LEGACY.md` (retired systems,
still-authoritative for NPC behavior), `CHANGELOG.md` (release history).

## Directory map
- `src/<System>/` — one folder per system (Veil, Demons, Spellbook, Economy,
  Factions/<Faction>, etc.); large classes split into `Thing.Concern.cs` partials.
- `src/Magic/`, `src/Spells/`, `src/Nature/`, `src/Miracles/` — legacy Ash and Ember
  effect layers; still power NPC casting, do not delete.
- `*/*Math.cs` — pure numeric logic, no TaleWorlds types, unit-tested.
- `tests/PureLogicTests.cs` — all pure-logic tests (grep by system name).
- `ModuleData/*.xml` — item/troop/monster data (source of truth).
- `dist/TheDarkestNight/` — **generated release snapshot only**. Never read this as
  source; it lags `src/`/`ModuleData/` by design until the next `tools/pack.ps1` run.
- `GUI/` — UI prefab/brush XML.
- `tools/pack.ps1`, `install.ps1` — packaging/install scripts.

## Commands
- Build: `dotnet build src/TheDarkestNight.csproj` (auto-copies DLL to
  `<BannerlordPath>/Modules/TheDarkestNight/bin/<BannerlordBin>/`).
- Test all: `dotnet test tests/TheDarkestNight.Tests.csproj`
- Test one: `dotnet test tests/TheDarkestNight.Tests.csproj --filter "PureLogicTests.<Name>"`
- Install release build: `.\install.ps1` (`-BuildFirst`, `-BannerlordPath` optional)

## Conventions
- PascalCase public members/classes; `_camelCase` private fields; PascalCase enum values.
- One system per folder under `src/`; split large files into partials by concern
  (`Thing.Concern.cs`), matching the existing `SettlementEncounters.*`/`CampaignMapEvents.*` pattern.
- Pure numeric/decision logic goes in a `*Math.cs` file with no TaleWorlds types —
  needed for `PureLogicTests` to compile and run.
- Always null-guard `Campaign.Current`/`Mission.Current`; wrap TaleWorlds singleton
  access in `try/catch`, log via `AshAndEmber.ModLog.Error(ex)` — never swallow silently.
- Never hardcode the Bannerlord install path — resolve via `BannerlordPath`/`BannerlordBin` env vars.

## Do not read
- `dist/**` (generated, frequently stale vs. source — confirmed drift as of this audit)
- `**/bin/**`, `**/obj/**`
- `*.dll`, `*.pdb`

## Task recipes
- **Add a new spell/rune effect:** edit `RuneCatalog` (definition) →
  `RuneSequenceMath` (grammar, if new combination logic) → `RuneEffects` (dispatch)
  → add a `PureLogicTests` case for the math.
- **Add a new faction mechanic:** create/edit `src/Factions/<Faction>/<Faction>CampaignBehavior.cs`
  (+ concern partials), pure `<Faction>Math.cs`, register the behavior in `MagicSystem.cs`'s
  `OnGameStart`.
- **Change an attack shape (fire/wind/earth/water/spirit):** edit the single dispatch
  point in `ElementSpellEffects.CastAttack` (or the shared `NatureEffects` for
  wind/earth/water) — this is automatically NPC-parity-correct.
- **Bump the version:** update all 5 places — `src/TheDarkestNight.csproj`,
  `SubModule.xml`, `dist/TheDarkestNight/SubModule.xml`, `CHANGELOG.md`, `README.md` title.
- **Before using an unfamiliar TaleWorlds API:** reflect against the real DLL first
  (see `behaviour.md`) — signatures drift and intuition is wrong more often than not.
```

## 6. Session hygiene rules

- **Never read `dist/**` as source.** It is a generated snapshot that has already drifted 600–1,300 lines from `ModuleData/`/`README.md`; treat any question about "what does the game currently do" as answered only by `src/` and root `ModuleData/`.
- **Prefer `Grep`/`Glob` over opening large files whole**, especially `tests/PureLogicTests.cs` (6,626 lines), `CHANGELOG.md` (1,697 lines), and any `src/QuestSystems/*.cs` file — grep for the symbol/test name first, then read only the surrounding lines.
- **Scope test searches to a filter string** (`--filter "PureLogicTests.<Name>"`) rather than running/reading the whole suite when only one system changed.
- **For "what changed recently" questions, read `RECENT_CHANGES.md` (242 lines) first**, not `CHANGELOG.md` (1,697 lines) — the former is the session-handoff log `behaviour.md` mandates keeping current.
- **Check `FOR_OTHER_LLMs.md` at session start** before re-deriving context that a prior session already discovered — this is explicit project policy in `behaviour.md`.
- **Use the Explore subagent for any "where is X implemented" question spanning more than ~3 files** rather than manually grepping across `src/`'s 60+ folders in the main context.
- **Before trusting an `.md` planning doc** (`RUNE_MAGIC_PLAN.md`, `SCHEME_MINIGAME_GUIDE.md`, `LORE_REVIEW.md`), cross-check against `CHANGELOG.md`/git log for whether the plan already shipped — planning docs for completed features are a stale-context trap.
- **Never guess a TaleWorlds API signature** — reflect against the real DLL (`behaviour.md`'s PowerShell snippet) rather than reading through unrelated source files hoping to find a matching call pattern; this is both more accurate and cheaper than searching.
