# Code conventions

Extracted from `CLAUDE.md` to keep that file a roadmap. These are the house
rules for writing code in this repo.

- **Naming:** PascalCase for public members and classes; `_camelCase` for private fields; enum values are PascalCase (e.g., `TalentId.Gift`, `ColorSchool.Red`).
- **One system per folder** under `src/`; large behaviors/classes are split into **partial classes by concern** across several files (e.g. `SchemeSystem.Execution.cs`, `ExchangeCampaignBehavior.Rounds.cs`, `SpellEffects.Battlefield.cs`). Never add new spell-form logic directly to `SpellEffects.cs`; add it to the appropriate `*Spells.cs` partial or a new one. Follow the existing split when a file grows.
- **Numeric logic goes in a pure `*Math.cs` file** (no TaleWorlds types) so it can be unit-tested. If a "pure" method needs a game value, pass it in as a parameter rather than reading `Hero.MainHero` inside — see `behaviour.md` for why (JIT type resolution defeats a `try/catch`).
- **Static utility classes** (`AgingSystem`, `SchoolData`, `SpellDatabase`, the `*Math` classes) have no instance state — keep them that way.
- **Null-guard pattern:** always check `Campaign.Current == null` / `Mission.Current == null` before accessing singletons in behavior methods, and wrap TaleWorlds singleton access in try/catch (mod-conflict safety).
- **Never swallow silently:** a mod-conflict-safety `catch` must record the failure, not drop it. Use `catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }` (see `src/ModLog.cs`). `ModLog` is crash-proof, references no TaleWorlds types (safe from pure `*Math.cs`), de-duplicates per failure site so a per-tick throw is logged once, and writes to `Documents\Mount and Blade II Bannerlord\TheDarkestNight\errors.log`. The log self-limits: entries older than 30 days are pruned once at session start, and the file is archived to `.old` if it passes 5 MB. Do not reintroduce bare `catch { }`.
- **Tests live in `tests/PureLogicTests.cs`** and cover only pure (no-TaleWorlds-runtime) logic. Keep new tests pure — do not reference game engine types.
