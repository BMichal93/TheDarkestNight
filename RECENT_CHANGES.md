# Recent Changes

This file logs changes made by Claude Code sessions. Each entry describes the
change, why it was made, and which files were touched. Future Claude Code
sessions should read this file first to understand what was recently modified,
and should append new entries at the top after making changes.

---

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