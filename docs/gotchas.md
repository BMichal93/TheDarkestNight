# Gotchas & frozen names

Reference detail extracted from `CLAUDE.md` so that file stays a roadmap. Two
things live here: strings that look like leftovers but must **not** be renamed,
and hard-won corrections that keep re-surfacing.

## Names that must NOT be "tidied up"

The namespace, assembly, DLL, module folder, and test project are all `TheDarkestNight` now (Phases 2–3, each verified by a green build plus a full test run). The few `AshAndEmber` / `aae_` strings that remain are **not** leftovers to sweep; each is load-bearing for a specific reason:

| String | Why it stays |
|---|---|
| `aae_*` item ids (53, in `ModuleData/items.xml` + C#) | **Persisted in player inventories.** Renaming orphans every existing save's items. Never touch. |
| `"AshAndEmberQuest"` (`SpecialQuestType`, 15 files) | Verified by reflection: getter-only, no `SaveableProperty` — computed, never persisted. Harmless either way, and out of scope. |
| `"the baseline AshAndEmber mod"` in comments (22) | Correct historical references to the **upstream project**, not to our namespace. |
| `Ashen*` / `Miracle*` / `EmberConclave*` type names | Cosmetic; `REFACTOR_NAMING.md` **Phase 4**. Note `AI/AshenCitySystem`'s retired rename helpers are kept unreferenced-but-present for save compatibility — do not delete them. |
| `God-King` / "Tribes of the East" in `AI/AshenCitySystem.Renaming.cs` (`RenameTribesKingdom`, `ApplyTribalCultureTexts`) and all of `AI/TribesDialogue.cs` | **Dead — verified uncalled**, and kept per the row above. Khuzait is the Bloodbound (ruler: **Huntmaster**), and every *live* surface now says so. Before "fixing" a God-King string, check whether its function has a call site: these have none, and `BloodboundCulture`/`BloodboundDialogue` own the live path. |

**A blanket find-and-replace of `AshAndEmber` across this repo will corrupt saves and break the test build.** Work from `REFACTOR_NAMING.md`.

## Corrections and gotchas

- **Agent scaling IS possible.** `Agent.SetInitialAgentScale` (whole-body) and `MBAgentVisuals.ApplySkeletonScale` (per-bone) both work; the v0.2.0 "no safe runtime agent-scale surface was known" note is obsolete — do not repeat it. See `docs/arch/systems-current.md` (Demons).
- **Expeditions are Camp-gated, not Legion-gated.** It shipped Legion-gated in v0.2.0 and moved to The Camp in v0.4.0 — treat any "Legion Expeditions" wording anywhere as stale. See `docs/arch/systems-current.md` (Expeditions).
- **The Awakened were renamed from "the Kindled" in player-facing strings ONLY.** Every code identifier, troop id (`sacred_kindled_*`), and save key deliberately still reads `Kindled`/`Elemental`, and must stay that way for save compatibility. See `docs/arch/systems-legacy.md` (Elementals).
- **Economy uses Path B (gold ~10× scarcer everywhere), not Path A (gold removed).** The town trade screen and party item-exchange popup hard-code gold in TaleWorlds' own view-models with no model seam to remove it cleanly. See `docs/arch/systems-current.md` (Economy).
- **Retired rename helpers in `AI/AshenCitySystem` are deliberately unreferenced-but-present** (`RenameNorthmenKingdom`, `RenameDunebornKingdom`, `RenameForestClansKingdom`, etc.) for save compatibility — do not delete them. See `docs/arch/systems-legacy.md`.
- **God-King / Tribes strings are dead code kept on purpose.** Before "fixing" a God-King string, check whether its function has a call site — these have none, and `BloodboundCulture`/`BloodboundDialogue` own the live path. See "Names that must NOT be tidied up" above.
- **`TalentId` carries retired class/path enum values for save compatibility.** An enum member is not necessarily a live, purchasable talent — check `TalentSystem`'s definition table. See `docs/arch/subsystems.md` (Talent and Focus Point Costs).
- **The Soldier Service map-meeting conversation must change NO faction/army state.** Doing so mid-encounter corrupts it into a hostile Attack/Surrender resolution and crashes — it only records terms and sets `PlayerEncounter.LeaveEncounter = true`; the real join happens on the next clean map tick. See `docs/arch/systems-legacy.md` (Soldier).
- **Voyage-in-progress state is intentionally not serialized.** A mid-crossing reload refunds the fare rather than resuming the voyage — don't assume it survives a reload. See `docs/arch/state-and-ticks.md`.
- **The "Legacy two-phase" numeric values are for NPC casts / underlying effects only — verify against code before relying on them.** The current player casting model is flat-cost, charge-scaled, not per-input. See `docs/arch/constants.md`.
