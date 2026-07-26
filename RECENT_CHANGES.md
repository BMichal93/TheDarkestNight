# Recent Changes

This file logs changes made by Claude Code sessions. Each entry describes the
change, why it was made, and which files were touched. Future Claude Code
sessions should read this file first to understand what was recently modified,
and should append new entries at the top after making changes.

---

## 2026-07-26 — Pre-release sweep: compile/crash safety, old-title cleanup, CLAUDE.md slimmed to a roadmap

A three-part safety-and-consistency pass over the current working tree, via `/translate-ai`.

**A — Compile + early-tick crash safety.** `dotnet build` clean (0 errors), all
665 tests green. Reviewed the two new untracked files — `src/CrashDiagnostics.cs`
(a breadcrumb/first-chance-exception net for the recurring first-tick *native*
AV that managed `catch`es can't trap) and `src/CityStates/SettlementCultureNormalizer.cs`
(deterministic new-game culture pass) — plus the modified first-tick pipeline
(`CampaignBehavior.Events.cs`/`Ticks.cs`, `CityStateCampaignBehavior.cs`,
`DemonSpawnCampaignBehavior.cs`, etc.). All paths are null-guarded, try/caught
with `ModLog.Error`, and breadcrumbed; no bare catches. The 8 build warnings are
pre-existing CS0162 (deliberate `const false` feature gates, e.g.
`LegacyContent.LegacyNpcCastersEnabled`) / CS9191 — not regressions. Note: a
native AV cannot be ruled out statically, which is precisely why CrashDiagnostics
exists; managed safety is verified, live 5-tick behaviour is not (can't launch
the game from here).

**B — Dead old-title self-references.** Root cause: the log folder was already
moved to `Documents\...\TheDarkestNight\`, but several surfaces still named the
mod "Ash and Ember" / pointed at the old `AshAndEmber` folder. Fixed only genuine
*self*-references (not the correct upstream-attribution mentions, and not any
frozen `aae_`/`Ashen*`/save-key identifier): `ModLog.cs` session banner + folder
doc-comment → "The Darkest Night" / `TheDarkestNight\errors.log`;
`behaviour.md` install-folder path → `Modules\TheDarkestNight\`; the stale
`AshAndEmber.ModLog.Error` example (won't compile — `ModLog` is in namespace
`TheDarkestNight`) corrected in the moved conventions doc. "Withering"/"Colour"
hits were left alone: they are live in-world content (Great Withering event, the
`ColourKnowledge` back-compat alias), not old titles.

**C — CLAUDE.md as a roadmap, not a monolith.** Moved the two heavy reference
blocks out to new `docs/conventions.md` (code conventions) and `docs/gotchas.md`
(frozen-names table + corrections/gotchas), leaving pointers + the one
load-bearing "blanket AshAndEmber replace corrupts saves" safety line. CLAUDE.md
went 111→85 lines. Mirrored the identical slimming into `AGENTS.md` (the Codex
parallel copy) to prevent drift.

**Files:** `src/ModLog.cs`, `behaviour.md`, `CLAUDE.md`, `AGENTS.md`,
`docs/conventions.md` (new), `docs/gotchas.md` (new), `RECENT_CHANGES.md`.

---

## 2026-07-20 (latest+1) — Crystals/Relics become real crafted swords; deferred-popup flush hardened

Two user-reported issues plus a requested tick-code audit, via `/translate-ai`.

**#1 "The sword is a giant boulder."** The boulder was a CRYSTAL, not a relic —
last session's `horse_whip` swap covered relics only, on purpose. Investigating
the "use the game's own sword code" instruction turned up the load-bearing fact
this file should have recorded long ago: **Bannerlord ships no sword mesh usable
from a `mesh=""` attribute.** Every real sword in
`SandBoxCore/ModuleData/items/weapons.xml` is a `<CraftedItem>` assembled from
crafting pieces; `horse_whip` is the ONLY stock non-crafted item of
weapon_class `OneHandedSword`, which is why the original author cloned it. So
`throwing_stone` renders a boulder in the hand, `horse_whip` a whip, and no
`<Item>` clone can ever look like a blade.

Fix: the 11 Crystals and 10 Relics are now `<CraftedItem
crafting_template="OneHandedSword">` clones of the vanilla reward sword
`winds_fury_sword_t3` (khuzait blade_8/guard_6/grip_6/pommel_5, all four piece
ids verified present in `Native/ModuleData/crafting_pieces.xml`). **Item ids and
names are unchanged → save-safe.** Wands stay `horse_whip` (a whip already reads
as a rod). Accepted consequences, documented in the file header: value/weight are
now derived from the pieces so the hand-set prices (crystals 1100-2000, relics
3000) are gone — no C# reads either by id, so this is a shop-price change only;
`is_merchandise` is omitted (vanilla default true) so the Crystalline Chamber
restock still works; `has_modifier="false"` keeps vanilla from producing a
"Rugged Sunstone" and keeps `LordGearWeathering`'s ItemModifier check clean.

**#2 First-tick crash — narrowed hard, and one real defect fixed.** The 20:53
log ran the FULL breadcrumb trail (`Init.*` + all six `Setup.*`, ending
`Setup.FinishNewGameWorldSetup exit`) and then stopped with **no `tick:` line at
all** — so the process dies after world setup and before any daily handler.
The audit found the likely reason, and it is ours:

`MageKnowledge.FlushDeferredInquiry` is called from `MagicInputHandler.Tick`,
i.e. **every frame** — the hottest path in the mod — and had no try/catch (against
this project's own never-swallow-but-never-crash rule) and no check that a map
screen existed. On a new game the first queued popup is
`FinishNewGameWorldSetup`'s controls pointer, so the first frame after character
creation opened a blocking `InquiryData` while the map's Gauntlet layer was still
being built — the exact mid-layer-transition case `behaviour.md` warns about, and
an AV in engine UI code never reaches a managed catch, which explains the clean
errors.log. Now gated on `MapState.IsReady` (the engine's own "map is up and
idle" flag, verified by reflection against `TaleWorlds.CampaignSystem.dll`) and
wrapped in try/catch. Popups are queued, never dropped — an early call returns
and the next frame retries.

Also added `CrashDiagnostics.MarkHourly` (self-limiting at 120 marks so hourly
handlers cannot burn ModLog's 600-breadcrumb budget) and wired it into the
hourly path — MortalLaw, Temple, TempleQuest, Bloodbound, Demons
(`PruneDead`/`DirectDemonParties`). Nature and Soldier hourly handlers were left
alone: both early-return when not attuned / not in service, so they are no-ops
on day one.

**Tick audit (requested).** Swept all 62 tick registrations. Every mutation of
game state inside a `foreach` runs over a materialized `ToList()`/local batch —
no live-collection mutation found (checked `DestroyPartyAction`,
`ChangeOwnerOfSettlementAction`, `ChangeKingdomAction`, `KillCharacterAction`
call sites, plus every direct `foreach` over `Settlement.All` / `MobileParty.All`
/ `Clan.All` / `Kingdom.All` / `Hero.AllAliveHeroes`). `RuinsCastleSystem`'s
garrison destroys are already batched at 3/day and, per the breadcrumbs, are not
even reached before the crash. The unguarded per-frame flush above was the one
genuine defect the sweep turned up.

Files: `ModuleData/items.xml`, `src/Mage/MageKnowledge.cs`,
`src/CrashDiagnostics.cs`, `src/MortalLaw/MortalLawCampaignBehavior.cs`,
`src/Factions/Temple/TempleCampaignBehavior.cs`,
`src/Factions/Bloodbound/BloodboundCampaignBehavior.cs`,
`src/FactionQuests/Temple/TempleQuestCampaignBehavior.cs`,
`src/Demons/DemonSpawnCampaignBehavior.cs`. Build green, 665 tests green.
items.xml hand-copied to the module folder again (install.ps1 still broken —
see the entry below).

## 2026-07-20 (latest) — Relic mesh, stacked ruin-name prefixes, pre-tick breadcrumbs

Three user-reported issues, worked via `/translate-ai`.

**#1 "Sword with stone texture".** The ten `aae_relic_*` items in
`ModuleData/items.xml` were byte-for-byte clones of the Crystal template,
including `mesh="throwing_stone"` — but unlike a Crystal (which IS a held
mineral) a relic is a named blade/charm with `weapon_class="OneHandedSword"`
and `item_holsters="sword_left_hip"`, so "Cinderfang"/"Gravebite Brand"
rendered as a rock in the sword slot. Fix: `mesh="horse_whip"` on the ten
relics only — the same verified-safe vanilla mesh the wands took last session.
The eleven Crystals keep `throwing_stone` on purpose. **Item ids untouched, so
save-safe.** Note `install.ps1` is currently unrunnable on this box (it uses
PowerShell 7 `?.` syntax; the shell here is Windows PowerShell 5.1 — separate,
untouched issue), so items.xml was copied into
`$BannerlordPath\Modules\TheDarkestNight\ModuleData\` by hand. Any future
ModuleData change needs the same manual copy until that script is fixed.

**#2 "Ruined City of Ruined City of Odrysa Castle".** Non-idempotent rename.
`RuinsCastleSystem.ApplyRuinAppearance` derives the "original" name from the
settlement's CURRENT name, and it now runs several times per session (session
launch, end of `FinishNewGameWorldSetup`, `OnGameInitializationFinished`, and
every daily tick) — each pass stacked another prefix. Fix: new pure
`RuinsMath.StripRuinPrefix` (loops, so saves already carrying two or more
prefixes heal on the next pass) called from `RuinNameFor`. Names are re-derived
and never persisted, so no save migration is needed. +2 tests (665 green).

**#3 First-tick crash — diagnostics widened, still not fixable blind.** The
20:21 errors.log showed `[trace] CrashDiagnostics installed` and then **no
`tick:` breadcrumb at all**, i.e. the process dies BEFORE the first daily tick,
in a window last session's instrumentation did not cover. Added
`CrashDiagnostics.MarkPhase` (writes `phase: …`) and breadcrumbed
`OnGameInitializationFinished` (troop-tree/lord gear weathering, ruin renames)
and `FinishNewGameWorldSetup` (imperial reassignment, faction scoping, town
conversion, culture normalization), plus per-step `MarkTick`s inside
`DemonSpawnCampaignBehavior`'s nightfall block — map party creation being the
likeliest native-AV site in the first hours. Still pure instrumentation, no
game state changed. Next crash should name the phase.

Files: `ModuleData/items.xml`, `src/Ruins/RuinsMath.cs`,
`src/CrashDiagnostics.cs`, `src/MagicSystem.cs`,
`src/Campaign/CampaignBehavior.Events.cs`,
`src/Demons/DemonSpawnCampaignBehavior.cs`, `tests/PureLogicTests.Ruins.cs`.
Build green, 665 tests green.

## 2026-07-20 (later) — Wand-in-sword-pool fix + first-tick native-crash diagnostics

Two user-reported issues, worked via `/translate-ai`.

**#1 Blade keepsake handed a WAND instead of a sword.** Confirmed root cause:
`aae_wand_*` items are defined in `ModuleData/items.xml` with
`weapon_class="OneHandedSword"` and Tier3-5 value (12000), so they matched
`FindOneHandedSwordPool`'s `WeaponClass.OneHandedSword` + tier filter and could
be rolled as the "good one-handed sword". `LordGearWeathering` already dodges
this via `WandsCatalog.IsWandItemId`; the keepsake pool did not. Fix: folded a
`!WandsCatalog.IsWandItemId(it.StringId)` guard into the pool's `NotCrafted`
predicate so it applies at ALL three fallback tiers. `src/AI/CreationBackstoryRework.Keepsakes.cs`.

**#2 First-tick native crash — diagnostic instrument added (NOT a blind fix).**
Pulled the real crash record this session (the log lives at
`OneDrive\Dokumenty\Mount and Blade II Bannerlord\TheDarkestNight\errors.log`;
Windows Event Log `Application`): WER `Launcher.Native.exe`, exception
**`0xc0000005` access violation, `StackHash_f7a4`, faulting module "unknown"**
(= JIT'd managed code, no symbols). This is a **pre-existing** signature (repo
history logs `StackHash_f7a4` first-tick crashes on 2026-07-19, before ANY of
this week's changes) and an access violation is a corrupted-state exception that
.NET Framework does NOT deliver to ordinary `try/catch`, so the fully-wrapped
tick pipeline steps right past it and ModLog never sees it. The managed log again
showed only CAUGHT errors (LordGearWeathering NRE, RelabelCulturalFeats ambiguous
reflection match, AshenRuins ResolveVillages) — none fatal. A 16.5 MB minidump
exists (`…\CrashDumps\Launcher.Native.exe.26004.dmp`) but no cdb/WinDbg is
installed to symbolise it. Screenshot showed the crash coinciding with a vanilla
kingdom fief-grant decision ("Monchug of the Bloodbound takes Kaysar Castle")
on the first tick — i.e. engine code tripping on some settlement/kingdom state.

Since a native AV with no stack cannot be responsibly fixed blind, added
`src/CrashDiagnostics.cs` (+ `ModLog.Breadcrumb`, a bounded/non-dedup/flush-each
trace writer capped at 600 lines): installed from `MainSubModule.OnGameStart`,
it (a) drops breadcrumbs through the first-tick settlement/kingdom pipeline
(`Ruins.ReapplyRuinNames`/`Ruins.DailyTick` garrison-destroys,
`CityState.OnDailyTick`, `Magic.ReassignImperialSettlements`) so the LAST
breadcrumb in errors.log names the phase that ran immediately before the
process dies, and (b) hooks `FirstChanceException` (filtered to exceptions
passing through our namespace, plus severe types) and `UnhandledException` to
capture any managed stack that precedes the AV. Pure instrumentation — no game
state changed. User to reproduce; the next crash will localise the faulting
phase. Files: `src/CrashDiagnostics.cs` (new), `src/ModLog.cs`,
`src/MagicSystem.cs`, `src/TheDarkestNight.csproj`,
`src/Ruins/RuinsCampaignBehavior.cs`, `src/CityStates/CityStateCampaignBehavior.cs`,
`src/Campaign/CampaignBehavior.Ticks.cs`. Build green, 663 tests green.

## 2026-07-20 — Ruins-neutral ordering, culture normalization, keepsake grant hardening, session-launch NRE cascade

Four user-reported issues, worked via `/translate-ai`. The crash log this time
survived: `Documents` is redirected to OneDrive, so the real error log is
`C:\Users\mbudz\OneDrive\Dokumenty\Mount and Blade II Bannerlord\TheDarkestNight\errors.log`
(not the non-existent `~\Documents\...` path). Its 18:41 session showed a
cascade of **caught** NREs at launch; the fatal crash itself was native/unlogged.

**#1 "Ruins of…" castles still faction-owned at day 1 (e.g. Chosen).** Pure
ordering bug. `RuinsCastleSystem.OnSessionLaunched` strips ruin ownership at
session launch, but `ReassignImperialSettlements` runs LATER (on
`OnCharacterCreationIsOver`, inside `FinishNewGameWorldSetup`) and re-hands some
of those same castle ids (castle_B5/B2/V2/V7 + Razih/Qasira matches) to the
Empire trio. The existing self-heal only re-ran on the next daily tick, so a
fresh save showed ruins owned by the Chosen. Fix: call the idempotent
`RuinsCastleSystem.ReapplyRuinNamesIfNeeded()` once more at the END of
`FinishNewGameWorldSetup`, after all reassignment/conversion passes — strips
re-owned ruins before the player ever sees the map. No new logic; reuses the
file's own strip path.

**#2 Culture spread wrong (Sea-Riders monopoly; "Templar" on non-Temple cities).**
Root cause: `ReassignImperialSettlements` folds NON-Empire-culture border towns
(e.g. Rovalt/Ocs Hall = Vlandia) into the Empire trio's seat lists; because they
then belong to a living kingdom, `ConvertOwnerlessTowns` skips them
(`clan.Kingdom != null`) and their culture is never touched — so an Empire city
kept reading as "Templar" (Vlandia). Converted towns, meanwhile, map their
bandit culture off their original culture, and central Calradia is mostly
Empire-culture → most became sea_raiders. New authoritative pass
`SettlementCultureNormalizer` (new file `src/CityStates/SettlementCultureNormalizer.cs`):
snapshot every town's ORIGINAL culture before conversions rewrite it, then at the
end of setup pin every faction SEAT to its own faction culture (Empire trio →
empire, fixing the "Templar" leak; Temple → vlandia/"Templar", which it legit is;
Wolf Brothers → sturgia; etc.) and give every other free town a geography-split
bandit culture; two deterministically-chosen neutral towns instead wear a full
base culture for variety. Sanctuaries (Camp/Children of the Forest), Ashen, and
player holdings are skipped. Only rewrites the runtime `Settlement.Culture` field
at new-game setup — no persisted id renamed, fully save-compatible. Pure helpers
`StableHash`/`OtherCultureIdFor`/`BaseCultureIds`/`OtherCultureTownCount` added to
`CityStateMath` (+ 3 new PureLogicTests).

**#3 "Trusted blade" starting gift never received.** No item literally named
"Trusted blade" exists — user confirmed this is the character-creation **Blade
keepsake** ("your blade" → one good one-handed sword). It failed SILENTLY:
`GrantRandomItemToRoster` returned on an empty pool with no log, while the
keepsake confirmation message still promised a sword. Fix: `FindOneHandedSwordPool`
now widens progressively (Tier3-5 swords → any sword → any one-hander) so it
never returns empty on item-registry drift; `FindWarHorsePool` gets the same
tier-drop fallback; and every silent early-return in `GrantRandomItemToRoster` /
`GrantRandomWand` / `GrantTradeGood` / both pool finders now logs via `ModLog`.

**#4 Session-launch NRE cascade (the managed lead near the native crash).**
`LordGearWeathering.ApplyToAllLords` read `Hero.MainHero` inside a LINQ predicate
before the main hero existed on a new game — `Hero.MainHero` THROWS (not returns
null), NRE'ing per hero. Resolved once up front (null = exclude nobody).
`SetCultureVariation` across all six `*Culture.cs` + `AshenCitySystem.Renaming.cs`
passed `null` as `GameText.SetVariationWithId`'s `choiceTags` arg, which the
engine dereferences internally → the NRE seen for every culture in the log.
Fixed to pass an empty `List<GameTextManager.ChoiceTag>` (verified only one
overload exists, taking that list). `RelabelCulturalFeats`' caught reflection
error left as-is (already degrades gracefully; cosmetic; sensitive AshenCitySystem
territory). The fatal crash was native/unlogged — user to retest with these
guards in place.

Files: `src/Campaign/CampaignBehavior.Events.cs`,
`src/CityStates/SettlementCultureNormalizer.cs` (new),
`src/CityStates/CityStateMath.cs`, `src/TheDarkestNight.csproj`,
`src/AI/CreationBackstoryRework.Keepsakes.cs`, `src/Units/LordGearWeathering.cs`,
`src/Factions/{WolfBrothers,Tower,ForestWidows,Bloodbound,Empire}/*Culture.cs`,
`src/AI/AshenCitySystem.Renaming.cs`, `tests/PureLogicTests.CityStates.cs`.
Build green, 663 tests green.

## 2026-07-20 — Wand mesh fix, ruins-ownership self-heal, wands never break, new crash lead (StackHash_a395, unresolved)

Four user-reported issues, worked via `/translate-ai`.

**#1 Wand/Sigil textures.** All 16 `aae_wand_*` items (and nothing else) in
`ModuleData/items.xml` had `mesh="throwing_stone"` despite header comments
claiming a "horse_whip" template — verified against the real vanilla item
(`SandBoxCore/ModuleData/items/weapons.xml`, id `horse_whip`) that every other
field (body_name, weapon_class, physics_material, item_usage) already matched
exactly; only `mesh` was wrong. Swapped all 16 to `mesh="horse_whip"`. Holy
Sigil already used `throwing_stone` — left as-is (that was the correct ask).

**#2 "Ruins of..." castles starting as faction territory (esp. Empire trio).**
`RuinsCastleSystem`'s own header comment documents a deliberate decision to
never set `Settlement.OwnerClan = null` (untested engine territory, cited
past crash risk) — so ownership was left alone when a castle converts to a
cosmetic ruin, which let `ReassignImperialSettlements` (or vanilla map gen)
leave a "ruin" still painted as real kingdom territory. Fix extends the
file's existing self-healing daily-reapply pattern (already used for
garrison/prosperity) to ownership: `ApplyRuinAppearance` now detects
kingdom-owned ruins (`IsFactionOwned`, mirroring `IsExempt`'s own city-state
carve-out) and reassigns them to a cached kingdomless clan's hero
(`GetCustodianHero`) via the same proven `ChangeOwnerOfSettlementAction.
ApplyByDefault` call `FactionScoping.cs` already uses elsewhere — never
touches `OwnerClan = null`, never touches a player-held ruin, runs every
daily tick so it self-corrects regardless of what granted ownership or
whether a rival lord recaptures one.

**#3 First-tick crash, ~2026-07-20 17:08 CEST (CRITICAL — NOT FIXED).**
`errors.log` shows the session launching at 17:07:59, the usual harmless
`AshenRuinMenus.ResolveVillages` warnings through 17:08:08, then nothing —
the crash was never caught by any try/catch. The real event, found in the
Windows Application/WER event log: `Launcher.Native.exe` died at 17:19:06 to
`0xc0000005` (access violation), `StackHash_a395`, in an unsymbolized native
module — a different signature than the 2026-07-19 `StackHash_f7a4` new-game
crashes (already fixed), so a related-area regression, not a repeat. No
managed stack trace exists to act on. Flagged to the user for a repro/
debugger session; noted that the #2 fix above runs in the same session-launch
cluster (`RuinsCastleSystem`) as a "watch this on next playtest" risk, though
660/660 tests and a clean build don't show anything obviously wrong.

**#4 Wands should use charges, never break.** `WandEffects.cs` already had a
correct player-side charge pool (`TryConsumePlayerCharge`), but NPC wielders
used a separate `NpcWandBreaks` roll that made the wand inert for the mission
AND struck one copy from the wielder's roster — real item destruction,
contrary to the ask. Removed the NPC break mechanic entirely (also deleted
`WandsMath.NpcBreakChancePerUse`/`NpcWandBreaks` and their two now-obsolete
tests in `PureLogicTests.Wands.cs`) — NPCs now just cast on cooldown like a
Rod/Sigil, no charge tracking, no breaking. Added `WandEffects.
RefillAllPlayerCharges()`, called from `CampaignBehavior.Ticks.cs`'s existing
`OnMissionEnded`, so the player's wand charges top back up after every battle
(previously only refilled when a wand was first granted) — mirrors vanilla
arrows.

Files: `ModuleData/items.xml`, `src/Ruins/RuinsCastleSystem.cs`,
`src/Wands/WandEffects.cs`, `src/Wands/WandsMath.cs`,
`src/Campaign/CampaignBehavior.Ticks.cs`, `tests/PureLogicTests.Wands.cs`.
Build green; 660 tests pass.

---

## 2026-07-19 22:51 — Fourth new-game map crash (RuinsCastleSystem garrison-destroy burst) + ruins ownership hardening

Another first-tick crash report (~22:51 CEST, same WER symptom: `Launcher.
Native.exe`, `0xc0000005`, `StackHash_f7a4` / `PCH_6D_FROM_KERNELBASE+0xC1ADA`
— IDENTICAL StackHash to the 22:34:50 crash, meaning the 22:30 session's
Temple/Bloodbound re-entrancy fix (deployed 22:49:04, before this 22:52:53
crash) did NOT change the signature at all — a different, still-unfixed bug).

**Root cause, found by re-examining my OWN previous fix.** Earlier this
session I "fixed" `RuinsCastleSystem.ApplyRuinAppearance`'s backwards
`DestroyPartyAction.Apply(garrison.Party, null)` call (reasoned as clearly
wrong from the method signature). That reasoning was correct — **verified
this time by decompiling `DestroyPartyAction.ApplyInternal` directly with
`ilspycmd`** (`destroyedParty.RemoveParty()` is called on the second
parameter; the first is only passed through to `OnMobilePartyDestroyed` as
optional attribution, and can be `null` — `ApplyForDisbanding` proves this by
calling `ApplyInternal(null, disbandedParty)`). But fixing it turned a call
that had ALWAYS silently NRE'd (a no-op, for as long as this codebase has
existed) into one that actually executes `RemoveParty()` — and
`RuinsCastleSystem.ConvertCastles()` calls it for every one of the ~30-40
castles converted to a Ruin, synchronously, in one loop, at session launch.
That volume of real `DestroyPartyAction.Apply` calls (each broadcasting
`OnMobilePartyDestroyed`/`OnMapInteractableDestroyed`) in a single burst,
landing in the same fragile new-game-setup window this whole investigation
has been chasing bugs in, is the new prime suspect — and exactly matches when
the crash signature changed from `StackHash_a395` (the original
`MortalLawCampaignBehavior` re-entrancy) to `StackHash_f7a4` (first seen at
22:34:50, the very next crash after this fix's 22:25:08 deploy).

**Fix — batch instead of burst (`src/Ruins/RuinsCastleSystem.cs`):**
`ApplyRuinAppearance` no longer calls `DestroyPartyAction.Apply` itself; it
queues the settlement id into `_pendingGarrisonDestroys`. A new
`ProcessPendingGarrisonDestroys` (called from the existing `DailyTick`) drains
that queue at 3 settlements per day — the same net effect (every ruin
eventually loses its garrison) without ever asking the engine to tear down
more than a handful of parties in one tick.

**Found six more call sites with the exact same backwards `DestroyPartyAction.
Apply(party.Party, null)` bug** (`DemonSpawnCampaignBehavior.cs`,
`ApocalypseCampaignBehavior.Gathering.cs`, `ApocalypseCampaignBehavior.
Resolution.cs`, `TowerRiteHostParty.cs` ×2, `CampaignMapEvents.Events01_09.cs`)
— every one of them has therefore ALSO always silently no-op'd (demon dawn-
despawn, the apocalypse gathering-band cleanup, the Tower rite host party,
and caravan destruction have never actually removed their target party).
**Deliberately NOT fixed this session** — each needs its own review for
whether its call site is a tight per-tick loop (repeating today's mistake) or
a naturally-throttled one-at-a-time tick, and this session already has three
rounds of "fixed one bug, crash signature changed, found another" behind it.
Flagging for a dedicated follow-up pass rather than risking a fifth crash
class by fixing all six blind.

**Ruins ownership hardening (user request: "ruins should not be treated as
normal castles you can own").** Investigated what's actually enforced today:
`RuinsMenus` only ADDS an "Explore the ruin" option to the vanilla town/castle
menu — it does not hide the normal garrison/management options, and the siege
flow was never touched (the file's own header already documented this as a
deliberate, `behaviour.md`-rule-3-driven simplification: no proven way in
this codebase to safely set `OwnerClan` to null or intercept a siege).
Hardened the existing mitigation instead of attempting a new, unverified
siege-block: `ReapplyRuinNamesIfNeeded` (previously a ONE-SHOT backstop for
the engine's XML-text-reload reverting the ruin's name) now runs
unconditionally on **every** daily tick. If a rival lord's AI ever does
capture and garrison a ruin through the untouched vanilla siege flow, the
very next day queues that new garrison for removal and re-zeroes
prosperity/security — so holding one stays permanently worthless rather than
becoming a real castle, even though the siege itself is still technically
possible. Updated the stale/misleading comments in both `RuinsCastleSystem.cs`
and `RuinsMenus.cs` that claimed the menu "replaces" normal interaction (it
doesn't — verified by reading `RuinsMenus.RegisterEntryOption`, which is
purely additive).

Build green, all 662 tests pass, DLL redeployed. **This is the fourth
consecutive crash report on the same new-game flow** — each fix so far has
demonstrably changed the WER StackHash (proving each was a real, distinct
bug), but a clean new-game start still needs to be re-verified before trusting
this one either.

## 2026-07-19 22:30 — Third new-game map crash (Temple/Bloodbound re-entrancy) + Empire trio over-sized territory

Two reports after the 22:08 fix: (1) another first-tick crash at ~22:34
(same `PCH_6D_FROM_KERNELBASE` WER signature as the 22:08 one — confirmed the
deployed DLL, timestamped 22:25, already had that fix, so this was a second,
distinct instance of the same bug class); (2) the Northern and Southern Empire
holding far more towns/castles than intended (screenshot evidence).

**Crash — root cause.** Grepped every `ChangeKingdomAction.Apply*` call site
for the same "called synchronously from inside `OnClanChangedKingdomEvent`'s
own dispatch" re-entrancy hazard the 22:08 fix identified in
`MortalLawCampaignBehavior`. Two more offenders: `BloodboundCampaignBehavior.
OnClanChangedKingdom` and `TempleCampaignBehavior.OnClanChangedKingdom` both
call `ChangeKingdomAction.ApplyByLeaveKingdom` synchronously when a joining
clan's leader fails their faction's "join gate" (Qualifies check) — the exact
same pattern, just gated on leader personality instead of fief count.
**Fix:** both now queue the clan (`_pendingUnworthyEjections`) and process the
actual `ApplyByLeaveKingdom` on the next `OnHourlyTick`, mirroring
`MortalLawCampaignBehavior`'s fix. Files: `src/Factions/Bloodbound/
BloodboundCampaignBehavior.cs`, `src/Factions/Temple/TempleCampaignBehavior.cs`.

**Territory — root cause.** Investigating the screenshot (via `settlements.xml`)
showed several "over-sized Empire" towns (e.g. Myzea/`town_EN5`) are just
*native vanilla* Northern Empire towns — `EmpireMath.StartingTownIds` (3 towns)
was only ever a "protected seat" list, never an actual cap; `FactionScoping`
explicitly EXCLUDED the three Empire-culture kingdoms from `StripExtraFactionTowns`
("their extras are by design"). So Empire/Legion/Chosen kept their full vanilla
territory *plus* `ReassignImperialSettlements`' deliberate border grab
(9 named towns) *plus* an unbounded "grab every castle within 40 map-units of
each named anchor" radius sweep — while the other 5 factions were scoped down
to 2 seats each. Asked the user: shrink all three Empire kingdoms to a curated
seat list like the other 5 (chosen) vs. leave native Empire size and just trim
the border grab.

**Fix:**
- Removed the radius-based "nearby castle" sweep from `AssignSettlementAndNearby`
  (`src/Campaign/CampaignBehavior.Events.cs`) — it transfers only the named
  anchor now. A radius sweep can't be reconciled with a fixed seat list.
- Folded `ReassignImperialSettlements`' border grab into a fixed, named seat
  list per Empire kingdom: `EmpireMath.StartingTownIds` (3→7: +castle_B5,
  castle_B2, Seonon/town_B4, Rovalt/town_V9), `LegionMath.StartingTownIds`
  (2→9: +town_V6, castle_V2, castle_V7, Galend/town_V5, Charas/town_V7,
  Quyaz/town_A1, Sanala/town_A6), `ChosenMath.StartingTownIds` (2→4:
  +Razih/town_A4, Qasira/town_A8).
- **Found and fixed a self-defeating bug in my own first pass:** once those
  border towns joined `EmpireMath`/etc.'s `StartingTownIds`, the existing
  `CityStateSystem.IsCoreFactionTown` guard (checks all 8 factions' seats)
  would refuse to ever GRANT them — a town is "core," so `ReassignImperialSettlements`
  would skip transferring its own newly-declared seats to itself. Added a
  narrower `CampaignBehavior.Events.cs`-local `IsRemnantFactionSeat` (the five
  non-Empire factions' seats ONLY) for the three guard call sites that decide
  whether to grant a border town — `IsCoreFactionTown` (all eight) is
  unchanged everywhere else (Ruins exemption, city-state conversion guard).
- **Found a genuine pre-existing content conflict while cross-checking:**
  Akkalat (`town_K2`) is simultaneously `ReassignImperialSettlements`' comment-
  block "Southern Empire border grab" AND one of `BloodboundMath.
  StartingTownIds`' own two protected seats ("Akkalat, Chaikand"). The seat
  guard has therefore ALWAYS silently no-op'd that particular grab (Akkalat
  was already in `_coreFactionTownIds` before this session). Left the
  no-op in place (didn't add `town_K2` to `ChosenMath`), documented the
  conflict in `ChosenMath.cs` for the mod author to resolve — same class as
  the pre-existing Temple/Ocs-Hall-Pravend conflict already noted in
  `TempleMath.cs`.
- Extended `FactionScoping.ShortListFactions()` (the `StripExtraFactionTowns`
  pass) to cover all eight kingdoms instead of five, so a clan that holds one
  of the Empire's new seats AND an extra native town (the Myzea case) has the
  extra redistributed into a city-state exactly like the other five factions.
- Updated `EmpireQuestMath.cs`'s header + a `PureLogicTests` assertion that
  read `EmpireMath.StartingTownIds.Length` as a towns-only count (it's now
  towns+castles mixed); fixed 4 stale test assertions for the new seat counts
  (`ChosenMath`: 2→4, `EmpireMath`: 3→7, `LegionMath`: 2→9, plus the
  `EmpireQuestMath` conquest-threshold sanity check).

Build green, all 662 tests pass, DLL redeployed. **Needs one more in-game
new-game test** to confirm both the crash and the territory size are actually
fixed — this class of bug has now taken three rounds to fully surface.

## 2026-07-19 — Fixed mislabeled toast for found-only Lost Forms (Twin Bolts / Lingering Ward / Asymmetric Burst)

Audited `TalentSystem.cs` for dead "kept for save compatibility" entries per user
request. The numeric-value "REMOVED" `TalentId` members (Rejuvenate, PlantGrowth,
DevourLife, Bewilder, Waver, Rouse, Consume, Char, Overflow, Renewal, VeteranAsh,
Ashfall) are genuinely dead everywhere *except* the enum declaration — confirmed
by grep (`TalentId.<name>` has zero other hits) — but they legitimately serve a
purpose and were left alone: `TalentSystem.NpcSpells.cs` persists `_purchased` as
a raw `List<int>` and restores it via `(TalentId)v`, so these reserved numeric
slots exist purely to stop a future talent from being assigned the same int and
silently inheriting old players' saved ownership. This matches the project's
existing "frozen names" convention (`REFACTOR_NAMING.md`) and needed no change.

**Real bug found while doing that audit:** `TalentId.LostMissile` / `LostBarrier`
/ `LostBurst` are NOT dead despite the `ClassMembers` comment implying they were
"consolidated-out forms" — they are live mechanics (`SpellBuilder.cs`,
`Spells/SelfSpells.cs`, `Spells/CreateSpells.cs` all branch on
`TalentSystem.Has(...)` for them) and are actively granted as random rewards by
`AshenRuinSystem.Rewards.GrantGrimoireFragment` / `GrantAllGrimoireFragments`.
But they had no `TalentDef` entry in `TalentSystem.All`, so `TalentSystem.GetDef`
(`All.FirstOrDefault(...) ?? All[0]`) silently fell back to `All[0]` (the
`DarkMage`/"Reaper" class def) — meaning finding one of these Lost Forms in the
Ashen Ruins displayed "Talent learned: Reaper." instead of its real name.

**Fix:** added proper `TalentDef` entries (`Category.LostForm`, `IsConsumable =
true` so they read as found-only like `ToxicFog`, not purchasable with focus
points) for `LostMissile` ("Twin Bolts"), `LostBarrier` ("Lingering Ward"), and
`LostBurst` ("Asymmetric Burst"), with lore/mechanic text matching their existing
enum comments. Corrected the stale `ClassMembers` comment that mischaracterized
them as consolidated-out. No `TalentId` values changed, no `ClassMembers`
membership changed — save-compatible.

Files touched: `src/Talents/TalentSystem.cs`.

---

## 2026-07-19 22:08 — Second new-game map crash (re-entrant kingdom-change event)

Same symptom class as the 17:28 crash below (native hard crash, no managed
stack, ~2 min after starting a new game — WER: `Launcher.Native.exe`, code
`0xc0000005`, faulting module resolved to `StackHash_a395` /
`PCH_6D_FROM_KERNELBASE`, i.e. no attributable native frame). That 17:28 fix
(bandit-clan-becomes-kingdom) was already deployed (DLL redeployed 22:06) and
did not prevent this one — different root cause, found by pulling the actual
WER `Report.wer` from `C:\ProgramData\Microsoft\Windows\WER\ReportArchive`
(the mod's own `errors.log` only logs *caught* exceptions and had nothing new;
the crash itself can never be caught).

**Root cause:** `MortalLawCampaignBehavior.OnClanChangedKingdom` called
`ChangeKingdomAction.ApplyByLeaveKingdom` **synchronously from inside its own
`OnClanChangedKingdomEvent` handler** whenever a clan joined an over-cap
kingdom (`MortalLawMath.KingdomFiefCap = 6`). This is exactly the re-entrancy
hazard `CityStateSystem.OnClanChangedKingdom` and `AshenCitySystem`'s handler
are deliberately left **empty** to avoid (see their own comments) — firing a
kingdom-membership action while still inside the event dispatch for a
DIFFERENT kingdom-membership action re-enters the native
campaign/diplomacy machinery mid-call.

New-game setup made this reachable for the first time: `CampaignBehavior.
Events.cs`'s `ReassignImperialSettlements` hands an Empire kingdom several
towns/castles (pushing its fief count past 6), then `MigrateBorderLords` — in
the very same synchronous pass — calls `ChangeKingdomAction.
ApplyByJoinToKingdom` to move a border clan into that same now-over-cap
kingdom. The join fired `OnClanChangedKingdomEvent`; MortalLaw's old handler
saw the cap was already exceeded and immediately called
`ApplyByLeaveKingdom` for the same clan **before the join had returned** —
re-entering kingdom-membership mutation and corrupting native state (matches
the StackHash/KERNELBASE signature far better than a clean managed
exception would).

**Fix (`src/MortalLaw/MortalLawCampaignBehavior.cs`):** the event handler now
only *queues* the clan (`_pendingDefectorEjections`, not persisted); the
actual `ApplyByLeaveKingdom` call moved to `ProcessPendingDefectorEjections`,
run from the existing `OnHourlyTick` — a safe top-level context, never inside
another action's event dispatch. Mirrors the pattern
`CityStateSystem.ReassertCityStateMembership` already uses safely on its own
daily tick. `ResetForNewGame` clears the queue (static-leak hygiene).

**Also fixed while investigating (both real, both previously masked by their
own `catch`):**
- `RuinsCastleSystem.ApplyRuinAppearance` called
  `DestroyPartyAction.Apply(garrison.Party, null)` — backwards. Signature is
  `Apply(destroyerParty, destroyedParty)`; the call passed the garrison as the
  *destroyer* and `null` as the *destroyed* party, so it NRE'd on `null` every
  single session (visible in `errors.log` for months) and never actually
  removed a ruin's garrison. Fixed to `Apply(null, garrison)`.
- `FactionScoping.IsUsableRecipient`'s catch-all returned `true` (usable) on
  any exception — fail-open, the wrong direction for a guard whose entire job
  is keeping bandit/minor/outlaw clans from becoming city-state kingdoms (the
  17:28 crash's own cause). Changed to fail-closed (`return false`).

Build green, all 662 tests pass, DLL redeployed.

## 2026-07-18 — Bug-fix pass (crash on leaving town, backgrounds, visuals, map remnants)

Session addressing a reported list of issues. Reviewed the prior Deepseek entry
(below) — its Empire-feats fix is sound; see the review note at the end.

**#8 — Crash on leaving town (the big one).** The 07-18 13:0x crash left a 38 MB
`rgl_log_errors` flooded with `SCRIPT ERROR: IMono_MBTeam::is_enemy:
other_team_index_invalid!` (millions of lines in seconds → watchdog dump). Root
cause: `Team.IsEnemyOf` drops into native code that PRINTS that error and returns
false whenever either team's index is invalid — a native assertion a C# try/catch
CANNOT suppress. In non-battle town/tavern walk-around scenes, wandering agents
carry non-null-but-invalid teams, so our per-tick combat loops that call
`IsEnemyOf` on every agent spammed the log until the frame stalled.
- New `src/TeamSafety.cs` — `Team.IsEnemyOfSafe(other)` extension: false unless
  both teams exist AND `IsValid`. Registered in the csproj (explicit compile items).
- Routed every per-tick agent-team `IsEnemyOf` call through it: DemonBattleBehavior,
  ElementalBeings, RelicEffects, ElementWallWards, ElementUltimates,
  DarkGiftBattleEffects (×2), NatureEffects (×2), SpellEffects(.Combat),
  BattleEvents.Events (×2). Null-guards preserved verbatim.

**#2 — Backgrounds now grant normal bonuses again.** `CreationBackstoryRework`
was intentionally zeroing every Family/Adolescence/Youth option's
skill/focus/attribute grant (`NeutralizeMenu`/`NeutralArgs`, per old Req 23),
leaving the Keepsake as the only bonus source. Per mod-author directive,
removed the three `NeutralizeMenu` calls so each stage keeps its vanilla args
(bonuses) while still using our flavour rewrites. `NeutralArgs`/`NeutralizeMenu`
kept (unused) for reversibility.

**#3 — Spellbook open key.** Already `Alt+X` in source (battle) / `Alt+L` (map);
there is no `Alt+R` anywhere in the tree. No code change; rebuild syncs the DLL.

**#6 — Sanctuaries & Dark Altars removed from the map.** Unregistered
`SanctuaryCampaignBehavior` and `AshenAltarsCampaignBehavior` (MagicSystem) and
skipped their `EstablishForNewCampaign` calls (CampaignBehavior.Events) — no town
menus, no map announcements. Kept the `ResetForNewGame` calls (static-leak
hygiene) and every underlying class (NPC Grace/Miracle + Dark-Gift effect code,
PriestTroops gating, GreatAwakening altar-city pick all still reference them).

**#7 — Spellcasting hold animation + pale visuals.** The Spellbook input handler
only triggered the focus AURA, not the animation. Added
`SpellEffects.BeginCastLoop`/`EndCastLoop` on focus press/release (the exact
pair the retired `ElementMagicInput` player path used) — the caster now holds the
cast stance while focus is down and drops it the instant it's released. Also made
the player's held-focus pulse pass `spawnFireParticles: false` so it shows only
the soft light/glow, no fire dancing round the caster (NPC wind-up flashes keep
their particle).

Build green, all 637 tests pass, DLL auto-deployed.

**#5 — Factions holding extra cities (conservative fix).** Root cause:
`ScopeToStartingTowns` only EJECTS a clan that holds none of its faction's seats;
a clan holding a seat AND extra towns keeps all of them. New
`FactionScoping.StripExtraFactionTowns()` (run between `ScopeAllFactionsNow` and
`ConvertOwnerlessTownsNow`) hands every town a SHORT-LIST faction holds beyond its
`StartingTownIds` to a landless free clan, so the existing city-state conversion
turns each into its own free town. Only the five non-Empire factions
(sturgia/aserai/battania/khuzait/vlandia) are stripped — the three Empire kingdoms
are deliberately expanded by the legacy `ReassignImperialSettlements` and have no
single clean id list to check against. Fully guarded; NEEDS IN-GAME RE-TEST.
> Note found while tracing this: `TempleMath.StartingTownIds` (town_V2/town_V3 =
> Ocs Hall/Pravend) contradicts `ReassignImperialSettlements`, which hands those
> two cities to the Empire — the seat lists and the reassignment layer disagree.
> That's a pre-existing data-design inconsistency needing in-game verification;
> left untouched here.

**#4 — Rune magic, Phase 1 (pure core) implemented.** The rune system was only
`RUNE_MAGIC_PLAN.md` (never built). Implemented Phase 1 per the plan:
- `src/Spellbook/RuneCatalog.cs` — the 36 runes (Matter/Form/Manner/Coda roles,
  triplets, meanings, solo workings), the starter-pair picker, and the legacy
  `SpellId → runes` migration map. Pure data.
- `src/Spellbook/RuneSequenceMath.cs` — the pure binding-grammar resolver
  (chunking, repetition amplification, matter fusion via the existing
  `ElementComboMath.TryFuse`, triads, the Unbound Weave, Wyrd-overload, single-Form
  legality, manner stacking with the three declared contradictions, strain curve,
  composed names). Returns a `ResolvedWorking` descriptor.
- 20 new `PureLogicTests` cover triplet validity/uniqueness/sparseness, the role
  census (Forms == 8, effects > 2× forms), the amplify/strain curves, chunking,
  and every resolver branch + migration-map completeness. **All 657 tests pass.**

**#4 UPDATE — Rune magic Phases 2–5 now implemented (v0.10.0).**
- **Phase 2 — `RuneEffects.cs`:** dispatches a `ResolvedWorking` into the proven
  public effect API (ElementSpellEffects.CastAttack/CastWall — which already know
  every element/fusion/command — plus ElementalFactory/DemonFactory/SpellEffects).
  Self-contained (its own NearestEnemy/NearestAlly) so `SpellbookEffects` is left
  100% untouched. Exotic Manners (Mirror/Still/Vigil/Gift) resolve in the grammar
  but are inert no-ops in effects; Snare/Brand/Husk approximated with primitives.
- **Phase 3 — input + persistence:** `SpellbookInputHandler` now chunks marks into
  runes → discovery (learn-on-draw) → `RuneSequenceMath.Resolve` → `RuneEffects.Cast`
  + composed-name line; malformed → fizzle (+ burn), harmless inert bindings fizzle
  WITHOUT burn, valid bindings roll STRAIN. `SpellbookCampaignBehavior` gains
  known-runes state, the `SPELLBOOK_KnownRuneIds` save key, a load-safe v0.9→rune
  migration (guarded by `dataStore.IsLoading` against static-leak), and a rune book
  UI (The Marks + The Craft primer).
- **Phase 4 — learning sources:** the "a stranger's book" keepsake now grants two
  RUNES (the real fix for "I got two spells not runes"); the Tower teaches runes
  for influence (`TowerMath.RuneInfluenceCost`); ruins yield runes; debug grant-all
  learns every rune. NPC caster lords/troops keep their proven `SpellId` bound
  workings (plan §8), so nothing NPC-side changed.
- **Phase 5 — docs + version:** bumped to **v0.10.0** in all five places, CHANGELOG
  + README + CLAUDE.md rewritten for runes, RUNE_MAGIC_PLAN.md marked IMPLEMENTED
  with the deliberate deviations recorded.

Review pass — 3 uncertainties investigated and fixed: (1) migration static-leak
across same-session loads (now starts from fresh empty lists on `IsLoading`);
(2) Wyrd/command Calling summoned the near-unkillable Void "Great Other" (now a
safe Stone-born); (3) lone inert runes over-punished with spellburn (now a
harmless fizzle, genuine misbindings still burn). Build green; **660 tests pass.**

### Review of the Deepseek entry below (#1)
Sound and working: the empty-`Feats[]` early-return did leave the vanilla Empire
feats cached in the card VM, and clearing each feat Description fixes it. One
latent note (not a bug in the current flow): the Sturgia→Northmen and Empire
cards both use `Feats = new string[0]`, which the code comments treat as
"no-op / leave vanilla feats", but the new code now CLEARS them. Harmless today
because survivor-only mode removes every non-Empire card before it's shown and
the walk stops once the Empire card is corrected — so only the Empire card (which
WANTS clearing) is ever reached. No change needed.

## 2026-07-19 — Campaign-map crash fix, Alt+X map key, ward wording, gap-closing pass

**CRASH (new-game campaign map, ~17:28) — fixed.** A native hard crash (no
managed stack) right after a NEW game loaded the map. Root cause: v0.10.0's
`FactionScoping.StripExtraFactionTowns` (#5) drew recipient clans from ALL
kingdomless clans — including **bandit / minor / outlaw factions** — and
`CityStateSystem.ConvertOwnerlessTowns` then minted a KINGDOM ruled by a bandit
clan, a corrupt map state the native campaign code crashes on.
- `FactionScoping.IsUsableRecipient` now requires a proper NOBLE clan
  (`!IsNoble || IsBanditFaction || IsMinorFaction || IsOutlaw || IsNomad ||
  IsClanTypeMercenary` → rejected).
- `CityStateSystem.ConvertOwnerlessTowns` defensively refuses to ever convert a
  bandit/minor/outlaw clan into a city-state (belt-and-suspenders).

**Map Spellbook key: Alt+L → Alt+X** (matches the in-battle open key). Updated
`MagicSystem` map hotkey, the in-game controls manual (`KeybindReferenceLog`, also
rune-ified — it still described the retired formula system), and `CLAUDE.md`.

**Ward wording (player-facing): protective FIRE → protective RUNES.** "Ember Ward"
("a private warmth wrapped around you") → "Warding Rune" ("a protective rune
scrived close about you"); the Circle rune reads "a protective rune scrived about
the caster". (The Grace/Temple warmth-warding is a separate, intentional faith
lore and was left as-is.)

**Gap fixes (from the self-review list):**
- **#4b MarketScarcity roster underflow** (the recurring caught `MBUnderFlowException`):
  `AdjustRoster` read a stack's amount via `GetItemAtIndex` but wrote via
  `AddToCounts(ItemObject, delta)`, which targets the UNMODIFIED element — a
  different (or empty) slot for modifier-bearing stacks → underflow. Now snapshots
  and writes the exact `EquipmentElement`, with a clamp that can never remove more
  than was counted.
- **#5 rune grammar completeness matrix:** added the plan's exhaustive tests —
  all 20 matter-states × 9 form-states resolve to a working or a DECLARED
  contradiction (no silent holes), and every manner × form-state on a valid
  binding resolves. 662 tests pass.
- **#6 Sanctuary/Altar loose threads:** retired the `EC_LocalPriest` encounter
  (it took up to 10,000 denars to "build a sanctuary" that now has no menu), and
  re-gated Flame Priest garrison troops from the removed Sanctuaries onto the
  surviving Temple faction's towns (`TempleSettlements.IsTempleSettlement`) so the
  priest mechanic stays alive.
- **#1 faction seat/reassignment contradiction — FACTIONS WERE BEING ELIMINATED.**
  The legacy `ReassignImperialSettlements` gave the Empire other factions' declared
  capitals — the Temple's Ocs Hall (town_V2) + Pravend (town_V3) and the Forest
  Widows' Marunath (town_B1) + Car Banseth (town_B3) — so those factions lost ALL
  their seats and were eliminated at new-game (Bloodbound lost Akkalat but kept
  Chaikand, so it survived). Added a seat-protection guard: the Empire land-grab
  now skips any settlement that is another faction's declared seat
  (`CityStateSystem.IsCoreFactionTown`), so every faction keeps its capital and the
  Empire keeps only genuinely-unclaimed border cities. (Verified town ids/names
  against the shipped settlements.xml.)
- **#3 save-migration semantics — verified, no change.** Confirmed (by the
  codebase's own precedent: Demon/Veil/Wands add save keys every version and old
  saves load) that `SyncData` tolerates a missing key. The v0.9→rune migration
  starts from a fresh empty list on load, so an absent `SPELLBOOK_KnownRuneIds`
  correctly triggers migration.

Build green; 662 tests pass.

---

## 2026-07-19 — Rune effect visuals (evocative, effect-matched)

**Gap:** the element runes/fusions/walls looked good (they route through
`ElementSpellEffects.CastAttack`/`CastWall`, which carry fire bolts, gale/torrent
cones, ignite, element flashes), but the rune-ONLY effects in `RuneEffects.cs`
applied their mechanical effect with little or no distinctive visual — the Long
Mark bolt, Fear/Grave Mark, the Rot curse, the Maw, the Beacon rally, Wyrd's
heartening, the Night Mark, roots, wraithstep, and the Mirror manner all fired
"blind."

**Fix:** wired each into the existing visual toolkit (`SpawnExplosionEffect`,
`SpawnBurstExplosion`, `SpawnTrailParticle`, `BeginAgentGlow`, `SpawnNpcMoraleAura`,
`SpawnTempSmoke/SnowParticle`, `SpawnTempLightRgb`):
- **Long Mark bolt / the Maw** — a light-trail streak between caster and foe plus
  an impact burst; the Maw draws a dark burst off the foe and a red life-glow back
  into the caster.
- **The Rot** — a green nature burst and a lingering green glow on the cursed foe.
- **Fear / Grave Mark / Hush** — a dark purple dread-wave breaks outward, foes
  briefly limned purple; the **Night Mark** adds its own smoke-black false-night
  pulse first.
- **Wyrd hearten / Beacon rally** — a warm hearth-light and morale aura, allies lit
  gold.
- **Roots / Fetter / Snare** — cold snow + a blue burst locking the foe's feet.
- **Wraithstep** — smoke where the caster was and reappears.
- **The Mirror** — a silver, glass-still shimmer stands up around the caster (the
  "mirror-like entity" cue), even though the counter mechanic itself is a later
  refinement. **The Price** briefly lights the caster red as blood is paid.
- **Summons / Banish / Light / Husk** — a colour-matched burst at the summon point
  (ember-red + smoke for a demon), a white sear-burst on banished demons, a golden
  flare for the Lamp, and a matter-coloured mantle glow for the Husk.

New local helpers in `RuneEffects`: `BeginGlow`, `MirrorShimmer`, `StreakBetween`,
`ElementSchool`. Build green; 660 tests pass. Files: `src/Spellbook/RuneEffects.cs`.

---

## 2026-07-18 — Fixed Empire feats leaking on character-creation card

**Bug:** On the character-creation culture-selection screen, the "I am a survivor"
card (Empire background) displayed stale vanilla Empire cultural feats/bonuses
(e.g. cheaper caravans) in the dedicated feats panel, even though the card should
show no feats at all — "I am a survivor" is a neutral background with no faction
bonuses.

**Root Cause:** `TempleCultureCardFixer.RewriteFeats()` had an early return
`if (feats == null || feats.Length == 0) return;` — the Empire card defines
`Feats = new string[0]` (empty array, meaning no feats), but the method returned
before ever clearing the cached vanilla feat descriptions in the view-model. The
stale descriptions remained visible.

**Fix:** Modified `RewriteFeats` so it only returns early for `null`, not empty
arrays. When `feats.Length == 0`, it now iterates every existing feat VM and
sets its `Description` to an empty string, erasing the stale vanilla bonuses.

**Files touched:**
- `src/AI/TempleCultureCardFixer.cs` — `RewriteFeats()` method logic