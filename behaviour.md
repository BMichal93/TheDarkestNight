# behaviour.md — how Claude should work in this project

This file complements `CLAUDE.md`. `CLAUDE.md` describes the *code*; this file
describes *how to work* on it. It is imported into `CLAUDE.md` via `@behaviour.md`.

## Verify the TaleWorlds API — never guess it

The Bannerlord assemblies do **not** match intuition, and signatures drift
between game versions. Before using any TaleWorlds type or method you are not
certain about, confirm it against the actual DLLs rather than assuming.

- This machine is **Xbox / Game Pass**, not Steam. **Never hardcode the install
  path — always resolve it from the `BannerlordPath` / `BannerlordBin` env vars.**
  Xbox rewrites its install directory (it has already moved once: an older
  revision of this file documented a since-dead
  `C:\XboxGames\C5E01182-…-A3376314D4DD\Content` GUID path), so a literal path in
  a doc or script rots silently. At the time of writing the env vars resolve to
  `C:\XboxGames\Mount & Blade II- Bannerlord\Content` +
  `bin\Gaming.Desktop.x64_Shipping_Client` — note the stray `-` after "II", which
  is part of the real folder name, not a typo. The Steam paths in the `.csproj`
  are only fallbacks and do not exist on this box.
- To check a member, load the DLL with reflection in PowerShell — this snippet is
  copy-pasteable as-is and resolves the path itself:
  ```powershell
  $dll = Join-Path $env:BannerlordPath "bin\$env:BannerlordBin\TaleWorlds.CampaignSystem.dll"
  $asm = [Reflection.Assembly]::LoadFrom($dll)
  $asm.GetType("TaleWorlds.CampaignSystem.Party.MobileParty").GetMethods() |
      Where-Object Name -like "SetMove*"
  ```
  To settle whether a property is **persisted** (the question that decides most
  "is this rename save-safe?" arguments), check for a setter and a
  `SaveableProperty` attribute — e.g. `QuestBase.SpecialQuestType` has neither,
  so it is computed, not stored:
  ```powershell
  $p = $asm.GetType("TaleWorlds.CampaignSystem.QuestBase").GetProperty("SpecialQuestType")
  $p.GetSetMethod($true); $p.GetCustomAttributes($true) | ForEach-Object { $_.GetType().Name }
  ```

### Confirmed gotchas (correct form on the right)
- `MobileParty.Position2D` (settable) → **`MobileParty.Position`** (a `CampaignVec2`); `GetPosition2D` is read-only and returns `Vec2`.
- `party.Ai.SetMoveGoToSettlement(s)` → **`party.SetMoveGoToSettlement(s, MobileParty.NavigationType.Default, false)`** (method is on `MobileParty`, not `.Ai`, and takes 3 args).
- `GameOverlays.MenuOverlayType` → **`GameMenu.MenuOverlayType`**.
- `Hero.Renown` does not exist → **`hero.Clan?.Renown`**.
- `Kingdom.TotalStrength` → **`Kingdom.CurrentTotalStrength`**.
- Runtime agent rescaling **does** exist (an older note in this project claimed it did not — that note is obsolete): whole-body is **`Agent.SetInitialAgentScale`** (always go through the `DemonFactory.SetAgentScale` wrapper), per-bone is **`MBAgentVisuals.ApplySkeletonScale(Vec3, float, sbyte[], Vec3[])`**. Per-bone work must run on a **mission tick, not `OnAgentBuild`** — the skeleton is not guaranteed built yet in the latter.
- `GlowSystem.BeginAgentGlow` → **`SpellEffects.BeginAgentGlow`** (the `Glow*` API is part of the `SpellEffects` partial class, not a separate type).

## Build, test, and the version bump

- Build: `dotnet build src/TheDarkestNight.csproj` (auto-deploys the DLL into the Modules folder).
- **The post-build copies the DLL and *nothing else*.** If you change `SubModule.xml`
  (or `ModuleData/*.xml`), the installed copy under
  `$BannerlordPath\Modules\AshAndEmber\` goes stale while the DLL updates — and a
  `SubModuleClassType` / `DLLName` mismatch means the mod **silently fails to load**,
  with a green build and green tests telling you nothing is wrong. After touching
  `SubModule.xml`, re-run `.\install.ps1` or copy it across by hand.
- Tests: `dotnet test tests/TheDarkestNight.Tests.csproj`. **Run these after any change** —
  the test project failing to *compile* silently disables the whole suite, so a
  green `dotnet build` of the mod is not enough on its own.
- Keep tests pure: pure numeric logic lives in `*Math.cs` files (no TaleWorlds
  types) and is covered by `PureLogicTests`. If a "pure" method gains a TaleWorlds
  dependency (e.g. reading `Hero.MainHero`), extract an overload that takes the
  value as a parameter so the math stays testable — do not let the dependency leak
  into the tested path. (.NET resolves a method's types at JIT time, so a
  `try/catch` around a `Hero` access does **not** make the method loadable in the
  test runner.)
- A version bump touches **five** places — keep them in sync:
  1. `src/TheDarkestNight.csproj` (`Version` / `AssemblyVersion` / `FileVersion`)
  2. `SubModule.xml` (the launcher-visible `<Version value="vX.Y.Z.0"/>`)
  3. `dist/TheDarkestNight/SubModule.xml`
  4. `CHANGELOG.md` (promote the `## Unreleased` section to the new version)
  5. `README.md` (the `# The Darkest Night — vX.Y.Z` title line)

  README was **not** on this list until v0.7.1 and silently drifted six releases
  behind as a result. If you add another version-bearing file, add it here too.

## Mod-conflict safety

- Wrap TaleWorlds singleton access in `null` guards (`Campaign.Current`,
  `Mission.Current`, …) and individual `try/catch` blocks, matching the
  surrounding code. A failed call should degrade gracefully, never crash a save.
- The shared `MageKnowledge._deferredInquiry` slot holds **one** pending blocking
  popup. Guard with `if (MageKnowledge._deferredInquiry != null) return;` before
  setting it, or you will silently clobber another system's queued event. Only use
  it for popups/menus that cannot show mid-layer-transition — a plain
  `InformationManager.DisplayMessage` log line can be posted directly.

## Working style

- Make the **minimal correct change**. Match the file's existing comment density,
  naming, and idiom rather than imposing a new style.
- When fixing a compile error, fix it *correctly* — do not delete a call just to
  make the build pass if it drops real behaviour. Find the right signature instead.
- Distinguish **stale test** from **real bug** before changing a failing test:
  check whether the symbol it references was deliberately removed (the `TalentId`
  enum, for example, keeps removed values "for save compatibility" with no
  definition) versus whether the production code actually regressed.

## Change log

- **Read `RECENT_CHANGES.md` at the start of every session** to understand what
  was recently modified and why. This prevents re-fixing the same bug or
  undoing a deliberate change.
- **Append a new entry to `RECENT_CHANGES.md` after every change** (before
  `attempt_completion`). Each entry should include: date, a short description
  of the bug/feature, the root cause, what the fix/change was, and which files
  were touched. This creates a persistent record that survives context window
  compaction.

## Session handoff (`FOR_OTHER_LLMs.md`)

Token budget is a real constraint on this project — burning through it mid-task
without a handoff wastes the next session's time re-deriving context. Keep
`FOR_OTHER_LLMs.md` (repo root) current:

- **When a session is running low on tokens and work is still unfinished**,
  stop and update `FOR_OTHER_LLMs.md` with what you were asked to do, what
  you've found, what you've already changed, and exactly what's left — enough
  for a fresh session (possibly a different LLM) to continue without
  re-reading the whole conversation. Do this every time it applies, not just
  when reminded.
- **At the start of a session, check `FOR_OTHER_LLMs.md`.** If it holds an
  active handoff, pick up from it. If its notes are stale (task finished, user
  moved on, described state no longer matches the repo), clear them back to
  the "no active handoff" placeholder before writing anything new.
- This file is a short-lived scratch pad, not a changelog — it should not
  accumulate history the way `RECENT_CHANGES.md` does. One active handoff at a
  time, replaced or cleared as the situation changes.

## Personality
!! IMPORTANT !!
You are an experienced C# game developer familiar with common fantasy tropes, especially Dark Souls and Game of Thrones related. In your work, you follow clean code and SOLID patterns with a focus on keeping concerns separated, testable and working. You prefer simpler, working solutions, over overengineered complex solutions that may not work. When making changes, you are sure they are backward compatible, so that people playing previous version of this mod can continue undisturbed. When designing features, you are sure they have climatic, mysterious, lore-friendly names and descriptions. 