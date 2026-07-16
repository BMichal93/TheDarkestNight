# REFACTOR_NAMING.md — standardizing `AshAndEmber` → `TheDarkestNight`

Working plan for retiring the last Ash and Ember naming. Phase 1 (docs) is **done**;
Phases 0 and 2–4 are queued. Delete this file once Phase 4 lands.

Every claim below was verified against the code or the shipped DLLs, not assumed.

---

## The headline: the documented blocker was not real

`CLAUDE.md` justified deferring this rename by claiming that "several persisted
savedata identifiers, `SaveDefiner` type names, and item/troop XML ids are
namespace-shaped strings that a text-level rename could silently corrupt." All
three claims were checked, and **none of them hold**:

| Claim | Reality |
|---|---|
| `SaveDefiner` type **names** persist | `AddClassDefinition(typeof(X), <int>)` persists the **numeric id**. The type name is never written. |
| Persisted savedata identifiers are namespace-shaped | All **544** `SyncData` call sites across 93 files use hand-written prefixes (`SEA_*`, `APOC_*`, `AG_*`). **Zero** contain `AshAndEmber`. |
| Item/troop XML ids are namespace-shaped | They are **`aae_*`** (`aae_embershard`, `aae_relic_cinderfang`). A rename of the `AshAndEmber` token never matches them. |

A C# namespace rename is **save-safe**. What actually persists — and must never
change — is the `aae_*` item ids (they sit in player inventories) and the module
`Id`, which already reads `TheDarkestNight`.

The real hazard lives elsewhere: the **assembly name and module folder**, which
break *installs*, not saves. That is Phase 3, and it ships alone.

---

## Phase 0 — hygiene ✅ done

47 test build artifacts untracked (`tests/bin/`, `tests/obj/`), and both added to
`.gitignore` (which already covered `src/bin`/`src/obj` — `tests/` was the gap).
Files stay on disk; only the index changed. `TheWitheringArt` / `ColoursOfCalradia`
now survive **only** in `CHANGELOG.md`, as history — leave them there.

> ⚠️ **`dist/AshAndEmber/bin/**/*.dll` is deliberately still tracked.** Those are the
> shipped pre-built DLLs `install.ps1` installs for players who don't build from
> source (it probes `$ModRoot\bin\$detectedBin\$ModName.dll` first). Untracking them
> breaks the release package. A blanket "ignore all bin/" would be a real bug.

## Phase 1 — docs truth pass ✅ done

`CLAUDE.md`, `README.md`, `behaviour.md` corrected; `LEGACY.md` split out. Size claim
was a 63% undercount (~65K/250 files → ~106K/430). Six systems were entirely
unmapped (`Expeditions/`, `ForeignMuster/`, `BeastsOfTheNorth/`, `Units/`, `Mage/`,
`Talents/`). README was frozen at v0.1.0 and is now a version sync point.

## Phase 2 — namespace only ✅ done

Landed as one commit: 432 namespace declarations, 4,453 `ModLog` qualifiers,
`TheDarkestNightSaveDefiner`, the tests' namespace + `using`, both
`SubModuleClassType` strings, and `<RootNamespace>`. **Verified: green build, 627/627
tests** (identical to the pre-sweep baseline).

> ⚠️ **`SubModuleClassType` is why this could never land partially.** It is the string
> Bannerlord reflects on to find the entry point. Rename the namespace in the `.cs`
> files but leave that string, and **the mod silently fails to load** — a total break
> with no compile error to warn you.

**Preconditions (all verified clean):**
- `ModLog` is declared in `namespace AshAndEmber` (`src/ModLog.cs`).
- Only two namespace declarations exist: `AshAndEmber` (432 files) and
  `AshAndEmber.Tests` (1). **No nested namespaces.**
- **Zero** files reference `AshAndEmber.ModLog` from outside `namespace AshAndEmber`.

**Do:**
1. `namespace AshAndEmber` → `namespace TheDarkestNight` (432 files).
2. `AshAndEmber.ModLog` → `TheDarkestNight.ModLog` (4,453 occurrences — 90% of all
   `AshAndEmber` tokens are this one redundant qualifier). Keep it a **pure token
   swap**; semantics are then provably identical. Dropping the now-redundant
   qualifier down to bare `ModLog` is a *separate*, later cleanup — do not ride it
   on the rename.
3. `AshAndEmberSaveDefiner` → `TheDarkestNightSaveDefiner`, plus its own error text
   and the assertion message in `PureLogicTests.cs` (~line 2640).
4. `namespace AshAndEmber.Tests` → `TheDarkestNight.Tests`.
5. `SubModuleClassType` → `TheDarkestNight.MainSubModule` (**both** SubModule.xml files).
6. `<RootNamespace>` in the csproj (cosmetic — affects new-file templates only).
7. Delete `CLAUDE.md`'s "On the `AshAndEmber` namespace" section and the
   `CHANGELOG.md` line that defers this.

**Do NOT touch:**
- **`"AshAndEmberQuest"`** (15 files, `SpecialQuestType`). Verified by reflection:
  getter-only, **no `SaveableProperty` attribute** — computed, never persisted. So
  it is namespace-*shaped* but carries no save risk either way; it is simply out of
  scope for a namespace rename.
- **`InternalsVisibleTo("AshAndEmber.Tests")`** — this must match the test
  **assembly name**, which is independent of the namespace. `tests/AshAndEmber.Tests.csproj`
  sets no `AssemblyName`, so the assembly is still named `AshAndEmber.Tests` after
  renaming the namespace. **Leave it alone**, or the internals sweep breaks.
  It changes only if the test *project file* is renamed.
- **`ModLog`'s log directory** (`Documents\...\AshAndEmber\errors.log`) — renaming
  orphans players' existing logs. Belongs with Phase 3's folder rename, not here.
- **`aae_*` item ids** — persisted in player inventories. Never.

**Verify:** green `dotnet build` **and** `dotnet test` (627 tests — the test project
failing to *compile* silently disables the whole suite, so a green mod build alone
proves nothing), then load a pre-existing save.

## Phase 3 — assembly + folder (breaking; ships alone)

`AshAndEmber.dll` → `TheDarkestNight.dll`; `Modules/AshAndEmber` →
`Modules/TheDarkestNight`; plus `install.ps1`, the csproj post-build path, `dist/`,
and `ModLog`'s log directory.

**This is a breaking install change.** An upgrading player ends up with two folders
both declaring `Id=TheDarkestNight`, which the launcher treats as a conflict. Needs
a release note telling players to delete the old folder. Do **not** bundle with Phase 2.

## Phase 4 — legacy type names (cosmetic; optional; per-cluster)

`Ashen*` (18 types), `Miracle*` (13), `EmberConclave*` (7), `AshEmber*` (GUI).

Two snags:
- The GUI classes (`AshEmberSplash`, `AshEmberLore`, `AshEmberNatureBar`) are coupled
  **by string** to prefab/brush filenames via `LoadMovie("AshEmberSplash")` and
  `new GauntletLayer("AshEmberNatureBar", …)` — class, file, and XML must move together.
- `AshenQuestLog` / `EmberConclave*Log` are `SaveDefiner` rows. Renaming the C# type
  is safe (the **id** persists), but the ids must keep their numbers.
- `AI/AshenCitySystem`'s retired rename helpers are kept **unreferenced but present**
  for save compatibility — do not delete them.

---

## Also outstanding (not naming, found en route)

- ✅ **Five orphaned changelog entries recovered.** `CHANGELOG.md` was missing
  **v0.15.0, v0.12.2, v0.12.1, v0.11.2, v0.11.0** — README's archive was their only
  record, and CHANGELOG's `## v0.11.x` stub actually read *"no changelog recorded"*
  while README held the records. All five are merged back in descending order, the
  stub is now `## v0.10.x and earlier`, and README points to `CHANGELOG.md`.
