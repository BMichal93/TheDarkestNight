# RUNE_MAGIC_PLAN.md — The Scrived Word (rune-drawn casting)

> Status: **IMPLEMENTED in v0.10.0** (Phases 1–5). This document is the design
> of record for the rune system that replaced the Spellbook's fixed 5–20-mark
> spoken formulas. Deliberate deviations from the original plan, made for safety
> in a build that cannot be play-tested from the toolchain:
> - **Effects (Phase 2):** `RuneEffects.cs` is self-contained and calls only the
>   PUBLIC effect API (plus its own target-finders) rather than lifting a shared
>   `SpellbookEffectPrimitives` out of `SpellbookEffects` — this leaves the
>   load-bearing `SpellbookEffects` (wands, the Rod, NPC workings) 100% untouched.
>   The exotic Manners (Mirror counter, Still dispel, Vigil persistence, Gift
>   redirection) resolve in the grammar but degrade to inert no-ops in the effect
>   layer; the composed working still fires. Snare/Brand/Husk forms are
>   approximated with existing primitives, not bespoke mission-tick token lists.
> - **NPC casters (Phase 4 / §6):** caster lords/troops keep their proven
>   `SpellId` bound-workings path (which §8 already preserves) rather than being
>   re-routed through `RuneSequenceMath`. `SpellbookMath.NpcSpellburnChance`
>   exists for a future wiring pass.
> These are the only material departures; the pure core (Phase 1) matches the
> plan exactly and is fully unit-tested.

---

## 1. Context and intent

Today the player casts by matching one exact 5–20-character U/D/L/R string
against `SpellbookCatalog` (`src/Spellbook/`). One wrong mark anywhere in the
string is a total miss. The system works, but it is a lookup, not a language.

The replacement: mages no longer *recite formulas* — they **draw runes in the
air and bind them into sequences**. Lore-wise, runes are the oldest craft of
the shattered world: the same rune-wards scribed into city walls are the
reason the night tide breaks against settlements instead of flooding them
(see §9 for the flavour touchpoints). A mage's casting is the same act at
speed — scriving in empty air.

Mechanically:

- A **rune is exactly 3 marks** of U/D/L/R (W/A/S/D → U/L/D/R, same mapping
  as today). 64 possible triplets; **36 are real runes** — the space stays
  sparse enough that discovery is a hunt, and large enough that the hunt
  lasts (re-asserted by tests).
- Each rune **means something** (Fire, Water, Reach, Wall, Summon…) and
  **does something alone** (release after one rune = its base working).
- **Repetition amplifies**: `DDD DDD` is twice-written Fire — longer burn,
  more damage.
- **Different runes interact**: `UUU DDD` (Reach + Fire) is no longer a weak
  bolt or a close burst — it is a **fireball** that flies and explodes on
  impact. Fire + Water is **Fog** (unique, non-damaging, already implemented).
- A malformed rune or a senseless binding **fizzles and rolls spellburn**.
- A *valid* rune the player never knew is **discovered** and written into the
  spellbook the moment it is first drawn.
- After every successful release, the player sees the **composed spell name**
  ("Fire Blast", "Fog Wall", "Twice-Written Cinder").
- NPC caster lords/companions carry **pre-bound sequences** and roll their own
  spellburn chance from personality + Intellect.
- Demons and the Bloodbound's demon-blood attunement are **unchanged** — they
  release raw element cones through `ElementSpellEffects` and never scrive
  (Requirement: that system is Ash and Ember's element magic, kept as-is).

### What this is NOT

- **Not** a rewrite of the effect layer. Every resolved rune working
  dispatches into the primitives that already exist and are battle-proven:
  `ElementSpellEffects.CastAttack/CastWall` (including all six fusions),
  `ElementalFactory.SpawnElemental`, `DemonFactory.SpawnDemon`,
  `SpellEffects.*` (heal/ward/light/fog/root), `SpellburnEffects.Trigger`.
- **Not** a removal of `SpellId`/`SpellbookCatalog`. That catalog is
  load-bearing for **wands** (`WandsCatalog` binds one `SpellId` per item —
  Requirement: wands keep their assigned spells), the **Chosen's Rod**
  (`ChosenRodEffects`), **NPC caster lords/troops** (`SpellcasterLords`,
  `SpellcasterTroops`), and **Tower teaching**. It becomes the catalog of
  *bound workings* — pre-scrived sequences frozen into items and NPC
  repertoires — while the player's free-form input path is runes.
- **Not** a save-breaker. Old saves keep `SPELLBOOK_Unlocked`; their
  `SPELLBOOK_KnownIds` are migrated by granting the runes that compose each
  known working (§7).

---

## 2. The rune catalog (36 runes)

New file: `src/Spellbook/RuneCatalog.cs` — pure data, no TaleWorlds types,
mirroring `SpellbookCatalog`'s shape. Each rune: id, name, triplet, meaning,
description, **grammatical role** (§3), and its **solo working**.

Triplets are chosen so the five elements read as "pure strokes", modifiers as
"turned strokes". (W=U, A=L, S=D, D=R.)

| # | Rune | Triplet | Keys | Meaning | Drawn alone |
|---|------|---------|------|---------|-------------|
| 1 | **Cinder** | `DDD` | SSS | Fire | Close burst of flame ahead — moderate damage + ignite (`CastAttack(Fire, power 0.7)`) |
| 2 | **Tide** | `LLL` | AAA | Water | Slowing wave cone (`CastAttack(Water, 0.7)`) |
| 3 | **Stone** | `RRR` | DDD | Earth | Short fan of erupting rock, root (`CastAttack(Earth, 0.7)`) |
| 4 | **Gale** | `URU` | WDW | Wind | Forward gust, knockback (`CastAttack(Wind, 0.7)`) |
| 5 | **Wyrd** | `UDU` | WSW | Spirit | Heartens the **closest friendly unit** — morale surge on the nearest ally (new small effect; `agent.SetMorale`) |
| 6 | **The Long Mark** | `UUU` | WWW | Reach | Arrow-like force bolt from the fingertips — small damage, long range (new: reuse the fire-bolt projectile machinery `ElementSpellEffects.FireMissile`/`_bolts` with a colourless, no-ignite variant) |
| 7 | **The Bar** | `LRL` | ADA | Wall | A low warding line — brief neutral slow-zone ahead (weak `SpawnFogPatch`-style slow strip) |
| 8 | **The Echo** | `RLR` | DAD | Multiply | Alone: nothing to double — fizzle **without** burn ("the echo finds no voice") |
| 9 | **The Calling** | `DUD` | SWS | Summon | A weak elemental of a random kind fights beside you (`ElementalFactory.SpawnElemental`, weakest tier) |
| 10 | **The Circle** | `ULU` | WAW | Ward | Small self-ward (`SpellEffects.ExecuteWardFromAgent`) |
| 11 | **The Fetter** | `DLD` | SAS | Bind | Roots the nearest foe a short while (reuse `CastRoot` logic, to be lifted from `SpellbookEffects` into the shared effect layer) |
| 12 | **The Shroud** | `LDL` | ASA | Veil | Thin ash-veil around the caster (`SpawnTempSmokeParticle` + ward, as Veil of Ash today) |
| 13 | **The Sundering** | `DUU` | SWW | Banish | A pale flash that stings every demon close by (weakened `CastBanishDemons`) |
| 14 | **The Lamp** | `UUD` | WWS | Light | A lingering lamp demons cannot bear (as `CastLight` today) |
| 15 | **The Mending** | `UDD` | WSS | Mend | Modest self-heal (`SpellEffects.HealAgent`) |
| 16 | **The Grave Mark** | `DDU` | SSW | Fear | Nearby foes falter — small radial fear (as `CastFear` today) |
| 17 | **The Hush** | `LLD` | AAS | Silence | A short hush — nearby enemy morale dips (as The Long Silence, weakened) |
| 18 | **The Stride** | `RUR` | DWD | Step | A short wraithstep forward (`CastWraithstep`) |
| 19 | **The Rot** | `DRD` | SDS | Curse | Gnawing curse on the nearest foe (as Bonewind Curse, weakened) |
| 20 | **The Beacon** | `RRU` | DDW | Rally | A hearth-glow that lifts the courage of every soul near it (as Hearthlight's morale half) |
| 21 | **The Maw** | `DDL` | SSA | Hunger | Dark draw: small damage to nearest foe, half returned as healing (new small effect from `DamageAgent`+`HealAgent`) |
| 22 | **The Vigil** | `ULD` | WAS | Linger | Alone: harmless fizzle, no burn ("the vigil keeps watch over nothing"). Bound: the working **persists** — bursts become lingering ground-patches, walls stand longer, summons stay, snares wait |
| 23 | **The Brand** | `URD` | WDS | Imbue | Alone: a faint gleam along the blade, nothing more. Bound: element + Brand = the caster's **weapon carries the element** for a time (reuse the legacy enchantment machinery in `Spells/`) |
| 24 | **The Husk** | `RDR` | DSD | Mantle | Alone: a brittle plain mantle (small ward). Bound: element + Husk = an **elemental mantle** — Cinder-husk burns attackers, Stone-husk soaks blows, Tide-husk shrugs off slow/burn, Gale-husk quickens the step |
| 25 | **The Night Mark** | `DLR` | SAD | Dark | Alone: a breath of false night — foes falter, but nearby **demons quicken** (double-edged, reuses the FalseNight primitives). Bound: + Calling = **summon a demon** (rogue chance — it is the Night's, not yours); + element = darker working that also gnaws morale; **+ the Lamp = contradiction → spellburn** |
| 26 | **The Price** | `DUL` | SWA | Blood | Alone: you bleed for nothing (self-damage, no burn roll — a lesson). Bound: costs a cut of the caster's health, multiplies the working's power beyond what repetition reaches |
| 27 | **The Snare** | `LDR` | ASD | Trap | Alone: a bare snare — trips the first foe who crosses (tiny root). Bound: element + Snare = the working is **buried ahead** and detonates when a foe steps in |
| 28 | **The Chain** | `RLD` | DAS | Arc | Alone: a static snap at the nearest foe, trivial damage. Bound: the working **leaps** foe-to-nearest-foe, up to 3 leaps at ~65% decaying power (vs the Echo's full-power duplicate at a *random* target) |
| 29 | **The Ring** | `RLL` | DAA | Nova | Form. Alone: a bare shockwave — a small stagger around the caster. Bound: the working breaks **radially outward** from the caster — Cinder+Ring = a fire nova; Wyrd+Ring = courage breaking over every ally nearby |
| 30 | **The Rain** | `DUR` | SWD | Fall | Form. Alone: a brief, plain drizzle over the ground ahead (cosmetic, douses nothing much). Bound: the working **falls from above** across an area — Tide+Rain = a slowing rain (the Weeping Sky's pattern), Cinder+Rain = ember-fall |
| 31 | **The Mirror** | `LUL` | AWA | Turn | Manner. Palindrome key — it reads the same both ways. Alone: a glint, nothing more. Bound: the working also arms a brief **counter** — the next hostile working or missile volley against the caster is turned back, weakened |
| 32 | **The Still** | `LLU` | AAW | Quench | Manner. Alone: a held breath — a heartbeat of silence (no effect). Bound: the working also **dispels** — burning, slow, and roots stripped from allies it touches; enemy wards it crosses gutter out |
| 33 | **The Sentry** | `RLU` | DAW | Dawn | Coda. Drawn `D-A-W` — the answer to the Night Mark's `S-A-D`. A standing watch-light that sears demons within its ring slowly for as long as it burns (the Lamp repels; the Sentry hurts) |
| 34 | **The Hollow** | `DRR` | SDD | Decoy | Coda. A phantom of the caster steps out and draws nearby foes to it for a few seconds before folding into ash |
| 35 | **The Anchor** | `LRR` | ADD | Hold fast | Coda. The caster stands rooted by choice: immune to knockback, pull, and stagger while it holds — the rune you write before the wave hits the wall |
| 36 | **The Gift** | `ULR` | WAD | Bestow | Manner. Alone: an open, empty hand (no effect). Bound: every **self-working** in the binding (Mending, Circle, Husk…) lands on the **nearest ally** instead — the healer's rune |

Tests assert: exactly-3-mark triplets, all-distinct, only U/D/L/R, count ≥ 30,
and space sparseness (36/64 ≤ 60% — and this is the ceiling: at least 40% of
the space stays permanently empty, or misdrawn bindings stop being dangerous).

> The Night Mark is drawn `S-A-D`, the Vigil `W-A-S`, the Sentry `D-A-W` —
> deliberate; the keys spell the mood.

> Naming note: rune names must stay climatic and mysterious per the project's
> style — they are *marks*, not spells, so they read as nouns of the old
> craft ("the Fetter", "the Long Mark"), never as game verbs ("Root", "Buff").

---

## 3. The binding grammar (how runes interact)

New file: `src/Spellbook/RuneSequenceMath.cs` — **pure** resolver, no
TaleWorlds types, fully covered by `PureLogicTests`. Input: the ordered list
of rune ids drawn this focus. Output: a `ResolvedWorking` struct
(`Kind`, `Element`, `Power`, `Form`, `TargetCount`, `Strain`, `Name`) or
`Malformed` (→ fizzle + spellburn roll).

### The binding is read as a sentence

Every rune has one **grammatical role**, stored on its `RuneCatalog` entry:

- **Matter** (5): Cinder, Tide, Stone, Gale, Wyrd — what the working is made
  of. Deliberately never grows: a sixth element would square the fusion
  table; new expressiveness comes from Forms and Manners instead.
- **Form** (8, mutually exclusive — at most ONE per binding): the Bar (wall),
  the Calling (summon), the Long Mark (bolt), the Snare (trap), the Brand
  (imbue), the Husk (mantle — matter poured onto the caster's own skin), the
  Ring (radial nova), the Rain (falls from above over an area). What the
  matter is poured into.
- **Manner** (8, freely stackable): the Echo, the Chain, the Vigil, the
  Price, the Night Mark, the Mirror (arms a counter), the Still (adds
  dispel), the Gift (self-workings land on the nearest ally instead). How
  the working behaves.
- **Coda** (the rest): Circle, Shroud, Mending, Lamp, Fetter, Hush, Stride,
  Rot, Beacon, Maw, Grave Mark, Sundering, Sentry, Hollow, Anchor —
  self-contained workings that simply stack their solo effect onto the
  binding.

Resolution rules, applied in order (order of runes within the sequence does
NOT matter — the resolver reads a multiset, which keeps it pure, testable,
and forgiving):

1. **Chunking** (input layer, but validated here): marks are consumed 3 at a
   time. A trailing 1–2 marks on release = malformed. A triplet that is not a
   rune = malformed.
2. **Repetition = amplification** (Req 6). For each rune, count `n`:
   `AmplifyScale(n) = 1, 1.75, 2.4, 3.0` (capped at 4 repetitions;
   `DDDDDD` → Cinder ×2 → longer burn + more damage, exactly the
   twice-fire example). Duration-type workings scale duration, damage-type
   scale damage — the resolver only outputs the scalar; the effect layer
   decides which axis it multiplies.
3. **Matter fuses** (Req 7, 18) — by count of distinct element runes:
   - **1 element** → that element is the working's element.
   - **2 elements** → `ElementComboMath.TryFuse(a, b)` — the six fusions
     already implemented and tuned: Fire+Wind→**Lightning**,
     Fire+Water→**Fog** (unique non-damaging area denial, own pale-grey
     visual — Requirement 18 satisfied by existing code),
     Fire+Earth→**Magma**, Wind+Water→**Ice**, Wind+Earth→**Sandstorm**,
     Earth+Water→**Mire**. Element+Wyrd → the four battle **commands**
     (Onslaught/Quicken/Steadfast/Hold the Line), also already implemented.
   - **3 elements, no Wyrd** → a named **Triad** — greater than any fusion,
     and inherently unstable (see Strain): **The Tempest** (Fire+Wind+Water —
     lightning strikes under a slowing rain), **The Eruption**
     (Fire+Wind+Earth — a burning, blinding wave), **The Seething**
     (Fire+Water+Earth — a boiling mire that burns and bogs), **The
     Avalanche** (Wind+Water+Earth — a knockdown wave that roots). Each
     Triad's effect composes two existing fusion/element casts — no new
     engine surface.
   - **All 4 elements** → **The Unbound Weave** — the mightiest working in
     the catalog, a field-wide composite, and it *always* bites its caster
     back (a guaranteed backlash component — self-damage — on top of Strain).
   - **Wyrd + 2 or more other elements** → malformed — will is not matter;
     the mind cannot share a weave with more than one other ("the weave
     tears").
4. **One Form re-shapes the whole working:**
   - **The Long Mark (Reach)** → projectile: the working flies as a bolt and
     delivers its effect on impact. Reach+Fire = **Fireball** (the existing
     `FireMissile` bolt); Reach+fusion = that fusion thrown (Fog thrown far =
     the existing `FogThrowDistance`, extended).
   - **The Bar (Wall)** → `CastWall(element)` — Fire Wall, Mistwall, etc.
     Fusion walls use `ElementComboMath.WallFallback` (already written).
     "Fog Wall" = Fog + Bar → the Mistwall path.
   - **The Calling (Summon)** → `ElementalFactory.SpawnElemental` of the
     matter's kind (fire/water/stone/storm… via the existing `ElementalKind`
     mapping; a Triad calls its dominant kind — the Tempest a storm
     elemental); amplified = higher tier; + Night Mark = a **demon** instead
     (`DemonFactory.SpawnDemon`, with a rogue chance).
   - **The Snare (Trap)** → the working is planted ahead and detonates on
     the first foe to cross it; the Vigil extends how long it waits.
   - **The Brand (Imbue)** → the working is bound into the caster's wielded
     weapon for a time.
   - **The Husk (Mantle)** → the working is worn: Cinder-husk burns those
     who strike the caster, Stone-husk soaks blows, Tide-husk shrugs off
     slow/burn, Gale-husk quickens the step (fusion mantles from the same
     mapping, tuned later).
   - **The Ring (Nova)** → the working breaks radially outward from the
     caster — Cinder+Ring a fire nova, Wyrd+Ring courage over every nearby
     ally.
   - **The Rain (Fall)** → the working falls from above across an area ahead
     — Tide+Rain the Weeping Sky's slowing rain, Cinder+Rain ember-fall
     (reuses the ultimates' area patterns).
   - **Two or more Forms in one binding** → **malformed**.
5. **Manner stacks:** the Echo adds `+1` random eligible target per echo
   (Req 10's multiply); the Chain makes the working leap foe-to-nearest-foe
   (≤3 leaps, ~65% decay per leap); the Vigil multiplies duration/persistence;
   the Price costs a cut of the caster's health for a large power multiplier;
   the Night Mark darkens the working (adds morale damage) and is the demon
   key for the Calling. Night Mark + the Lamp in one binding = contradiction
   → **malformed** (a special fizzle line — "the lamp gutters"). The Mirror
   arms a short counter (next hostile working/volley turned back weakened);
   the Still adds dispel (allies cleansed, enemy wards guttered); the Gift
   redirects every self-working in the binding to the nearest ally.
   Contradictory manners are malformed the same way the Lamp/Night Mark pair
   is: **the Still + the Vigil** (a working cannot both quench and linger)
   and **the Gift + the Husk** resolve normally (the mantle is simply worn
   by the ally — the healer's craft), but **the Mirror + the Gift** is
   malformed (a counter cannot be given away).
6. **Codas compose additively**: Circle/Shroud/Mending/Lamp etc. stack their
   solo effect onto the working (e.g. `Cinder + Circle` = flame burst +
   self-ward). A sequence of only codas just performs each.
7. **Strain — the price of ambition.** Every rune past the third adds
   inherent spellburn risk *even to a perfectly valid binding*:
   `StrainChance = (runeCount − 3) × 0.04`, reduced by the same Intellect
   scaling as the fizzle-burn curve, floored at 0. Short bindings are safe
   craft; a six-rune Triad is a gamble a master takes when the line breaks.
   Strain is rolled after a successful cast — the working still happens; the
   burn arrives on top of it.
8. **Anything unresolvable** → malformed → fizzle + spellburn roll (Req 8).

### The completeness rule — no undefined cells, and forms stay outnumbered

Two standing constraints, both enforced by tests, so the grammar cannot rot
as content grows:

**a) Effects must outnumber forms — permanently.** Matter is not 5 effects
but **20 distinct matter-states** (5 elements + 6 fusions + 4 Triads + the
Unbound Weave + 4 commands), against 8 Forms. **Forms are capped at 8
forever**: every future rune must be effect-side (a Manner or a Coda), never
a new Form. A test asserts the role census (`Forms == 8`,
`matter-states + codas > 2 × forms`).

**b) Every cell of the matter-state × Form matrix is defined.** The resolver
must return a real working or an *explicitly declared* contradiction for
every pair — no accidental holes. The awkward columns, decided now:

- **Wyrd × forms** (will, shaped): + Long Mark = a bolt of dread (morale
  damage on the struck foe); + Bar = the spirit wall (the existing
  `CastWall(Spirit)` — Ward of Whispers' path); + Calling = a lesser Bent
  Knee — the nearest foe's will bends and fights for you briefly (the
  existing Thrall machinery in `ElementUltimates`, weakened); + Snare = a
  panic-trap; + Brand = a weapon that strikes fear (morale damage on hit);
  + Husk = a presence-mantle (morale drains from foes who close in);
  + Ring = courage over every nearby ally; + Rain = dread falling across an
  area.
- **Commands × forms**: a command (Wyrd+element) is already a complete
  working — a Form in the same binding = **malformed** ("a command cannot be
  poured into a vessel").
- **The Unbound Weave × forms**: **malformed** — what is unbound cannot be
  shaped. (Triads DO take forms — Tempest+Calling is the storm elemental.)
- **Fusions/Triads × forms**: defined by the same per-element parameter
  tables the base elements use (Fog wall = Mistwall path, Ice brand = a
  freezing edge, Magma snare = a buried burst that burns and bogs…) — the
  effect layer keys on `MagicElement`, so every fusion inherits every form
  for free; only the Triads' four rows are new tuning.
- **Manners** apply where they can; a manner that cannot touch the working
  (the Echo on a pure self-ward, the Gift with no self-working present) is
  **inert wasted ink** — the cast still resolves, but the wasted rune still
  counts toward Strain (rule 7), so sloppy writing is taxed, not detonated.
  The ONLY manner-level malformed cases are the declared contradictions
  (Lamp + Night Mark, Mirror + Gift, Still + Vigil).

A pure test enumerates all 20 matter-states × 9 form-states (8 forms +
formless) and asserts each resolves to a working or a declared contradiction
— never a silent hole; a second test walks every manner against every
form-state the same way.

### Worked example

`Gale + Cinder + Tide + Calling + Echo` (15 marks):
matter Gale+Cinder+Tide → **the Tempest**; form the Calling → a **storm
elemental**; manner the Echo → the summon doubled; 5 runes → 8% strain
before Intellect. Result: *"The Twin Callings of the Tempest"* — two storm
elementals torn into being, with a real chance the weave burns its caster.
No table anywhere lists this spell; the grammar produced it.

### Composed names (Req 13)

`RuneSequenceMath.ComposeName(resolved)` — pure string composition:
matter name (element / fusion via `ElementComboMath.ElementName` / Triad /
"the Unbound Weave") + form word + manner qualifiers + amplification prefix.
Examples: "Fire Blast", "Fireball", "Fog Wall", "Twice-Written Cinder",
"The Twin Callings of the Tempest". Shown on release via
`InformationManager.DisplayMessage` in the Spellbook's purple, exactly where
"`{def.Name}` answers." prints today (`SpellbookInputHandler.TryResolve`).

---

## 4. Input layer changes

`src/Spellbook/SpellbookInputHandler.cs` — same skeleton (hold Alt /
Controller-RLeft, tap, release to cast; hands-free gate; unlock gate;
Alt+X opens the book), with:

- Buffer chunked per-rune for display: `[ DDD · LR_ ]` — completed runes shown
  by **name** once known ("Cinder · The Bar · …"), by triplet when unknown.
- Max sequence length: **7 runes** (21 marks) — a new
  `RuneCatalog.MaxSequenceRunes` constant; tests cover it.
- On release: resolve via `RuneSequenceMath`; dispatch via new
  `RuneEffects.Cast(resolved, caster)`; on malformed → existing fizzle +
  `SpellbookMath.SpellburnChance(intellect)` roll → `SpellburnEffects.Trigger`
  (the whole spellburn table is reused untouched, Talisman of the Unburnt
  Tongue reduction included).
- **Discovery** (Req 9): each *valid* triplet drawn that is not yet known →
  `SpellbookCampaignBehavior.LearnRune(id)` + a discovery line
  ("A new mark answers your hand: **the Fetter**."). Discovery happens even
  inside a sequence that later resolves malformed — the rune was real even if
  the binding was not.

---

## 5. Effect layer

New file: `src/Spellbook/RuneEffects.cs` — the dispatch from
`ResolvedWorking` to game effects, structurally a sibling of
`SpellbookEffects` and reusing its private helpers. Refactor step: lift
`CastRoot`, `CastFear`, `CastWraithstep`, `CastLight`, `NearestEnemy`,
`FlashSelf` (and add `NearestAlly` for Wyrd) from `SpellbookEffects` into a
shared internal static class (`SpellbookEffectPrimitives`) so both dispatchers
use one copy — **no behaviour change to `SpellbookEffects`**, which must keep
working verbatim for wands, the Rod, and NPC bound workings.

New small effects needed (all from proven primitives):
- **Force bolt** (Long Mark solo): parameterise the existing `_bolts`
  projectile (colour, ignite on/off, damage) instead of duplicating it.
- **Wyrd solo**: morale surge on nearest ally.
- **The Maw**: `DamageAgent` + `HealAgent(half)`.
- **Echo targeting**: re-run the single-target effect on extra
  random eligible targets.
- **Chain leaps**: nearest-to-nearest re-application with decay — a loop over
  `NearestEnemy` excluding already-struck agents.
- **Mantles** (the Husk): a mission-scoped token list (same shape as
  `NatureEffects`' speed tokens / `SpellburnEffects._wildDemons`) — per-element
  on-hit/passive behaviour, ticked from `MagicMissionBehavior`, cleared with
  battle state.
- **Snares**: a planted-position list checked against enemy proximity each
  tick — the same pattern `ElementWallWards` uses for its standing lines.
- **Brand**: reuse the legacy weapon-enchantment machinery in `Spells/`
  (verify its current entry point against the DLL before wiring — never guess
  the signature; see behaviour.md).
- **Triads / the Unbound Weave**: each composes two (or more) existing
  element/fusion casts fired together + the backlash self-damage — no new
  engine surface.
- **Ring/Rain forms**: radial = the existing nova/`SpiritPanic` area pattern
  re-parameterised; falling = the Weeping Sky ultimate's area pattern.
- **Mirror counters / Hollow decoys / Anchor stance**: small mission-scoped
  token lists (the same shape as the mantle tokens above), ticked and
  cleared with battle state.
- **The Still's dispel**: walks the existing token lists (speed tokens,
  burn/root state, wall wards) and removes matching entries — no new state,
  only removal of existing kinds.

Everything else is a call into existing `ElementSpellEffects` /
`ElementUltimates` / `ElementalFactory` / `SpellEffects` code.

---

## 6. NPC casters (Req 14)

`SpellcasterLords` / `SpellcasterTroops` keep their seeding, cooldowns, and
mission ticks. Changes:

- A lord's repertoire becomes 1–3 entries from a curated list of **bound
  sequences** (`RuneCatalog.NpcBoundSequences` — ~15 sensible bindings:
  Fireball, Fog Wall, Stone Fetter, Storm Echo…), each stored as rune ids so
  the cast goes through the *same* `RuneSequenceMath.Resolve` +
  `RuneEffects.Cast` path as the player — one pipeline, NPC-parity-correct
  by construction.
- **NPC spellburn** (new pure math in `SpellbookMath`):
  `NpcSpellburnChance(intellect, isCalculating, isImpulsive)` — base 12%,
  −1% per Intellect point, ×0.5 Calculating, ×1.5 Impulsive, floored at 2%.
  On a failed roll the lord's cast **misfires**: fizzle line + 
  `SpellburnEffects.Trigger(agent)` — an enemy mage's demon turning on him is
  exactly the battlefield drama this buys.
- `SpellcasterTroops` (the tier-1→5 caster tree) same mechanism, chance by
  tier instead of Intellect (`SpellcasterTroopMath`).
- `GrantToHero` (Tower's joining-lord grant) switches to granting bound
  sequences.

**Demons/Bloodbound untouched** (Req 15): `DemonBattleBehavior`,
`BloodAttunement*` already cast raw element cones through
`ElementSpellEffects.CastAttack` — no scriving, no spellburn. No code change;
one comment line each noting the distinction is deliberate.

---

## 7. Persistence, migration, learning (Req 9, 16)

`SpellbookCampaignBehavior`:

- New save key `SPELLBOOK_KnownRuneIds` (parallel int list, same pattern as
  `SPELLBOOK_KnownIds`). `SPELLBOOK_Unlocked` reused as-is — the one-time
  focus-point unlock and its menu stay.
- **Migration**: on load, if `SPELLBOOK_KnownIds` is non-empty and
  `SPELLBOOK_KnownRuneIds` is absent → grant the runes composing each known
  working via a pure map (`RuneCatalog.RunesForLegacySpell(SpellId)`), post
  one summary line ("Your formulas resolve into marks: N runes carried
  over."). Legacy ids are **kept in the save** afterwards (harmless, and
  wands/NPC systems still read the enum) — never deleted.
- **Learning sources** re-pointed at runes: Tower menu teaches unknown
  *runes* (price scaling in `TowerMath` by rune tier — elements cheap,
  Calling/Sundering dear); ruins finds and the arcane backstory grant runes
  (`LearnRuneFromRuin`, `GrantStartingRune` — same stub-hook pattern as
  today); Ctrl+Shift+F12 debug grants all runes.
- **"A stranger's book" keepsake** (`CreationBackstoryRework.Keepsakes.cs`,
  `KeepsakeId.Book` — today: unlock + two random short formulas via
  `QualifyingForArcaneStart`): now unlocks the Spellbook and grants **two
  runes with a guaranteed shape** — the first is always one of the five
  element runes; the second is either *another* element rune or one from a
  curated starter pool (`RuneCatalog.StarterEligible`: the Long Mark, the
  Bar, the Circle, the Mending, the Lamp — simple, safe first lessons; the
  dark and blood runes — Night Mark, Price, Maw — are never in a stranger's
  opening pages). Pure pick logic `RuneCatalog.PickStarterPair(Random)`,
  tested: every possible pair is castable on day one (element alone works;
  element+element fuses; element+form shapes). Keepsake menu/confirmation
  text updated to say *runes*, not formulas ("two runes already written in a
  stranger's hand").
- **Spellbook UI** (`ShowSpellbook`): two sections — *The Marks* (known
  runes: name, triplet, meaning, solo working) and *The Craft* (a short
  static primer: repetition amplifies, elements fuse, the Long Mark throws,
  the Bar raises walls…). The primer teaches the grammar without spoiling
  undiscovered runes.
- Heir succession inquiry ("The Book Passes") counts runes instead of
  formulas; `ForgetAll` wipes both lists.

---

## 8. What explicitly does NOT change

| System | Why untouched |
|---|---|
| `SpellbookCatalog`/`SpellId`/`SpellbookEffects.Cast` | Bound workings for wands (`WandsCatalog`, Req 17), Chosen's Rod, legacy NPC grants. |
| `ElementSpellEffects`, fusions, `ElementComboMath` | The effect layer runes dispatch into. |
| Demon casting, Bloodbound attunement | Req 15 — raw element cones, by design. |
| `SpellburnEffects` table | Reused verbatim for player and now NPCs. |
| `ElementLordAI`, Awakened, Miracles/Nature NPC paths | Separate populations, unchanged. |

---

## 9. Lore touchpoints (Req 2)

Flavour-only, no mechanics:
- Spellbook unlock text and README casting section rewritten: scriving, not
  reciting ("hold the focus, draw the marks, release the binding").
- One line in the city-assault messaging in `DemonSpawnCampaignBehavior`'s
  rare assault path: the wards held / the wards failed.
- `LEGACY.md` gains a "Spoken formulas (v0.4–v0.8)" section describing the
  retired input model, exactly like earlier retired paths.

---

## 10. Phases and file map

Each phase ends with a green `dotnet build` + full `dotnet test` run.

**Phase 1 — pure core.** `RuneCatalog.cs`, `RuneSequenceMath.cs` (resolver,
amplification, naming, NPC bound-sequence list, legacy-spell→runes map),
`SpellbookMath.NpcSpellburnChance`. Tests: triplet validity/uniqueness/
sparseness, role assignment completeness and the role census (Forms == 8,
effect-side > 2× forms), resolver table-driven cases (each grammar rule,
each fusion, all four Triads, the Unbound Weave, Wyrd-overload and two-Form
and contradiction malformed cases), **the completeness matrix** (all 20
matter-states × 9 form-states resolve or declare a contradiction; every
manner × form-state likewise — no silent holes), strain curve, inert-manner
strain taxing, name composition, amplification curve, NPC burn curve,
migration map completeness (every `SpellId` maps to only-known runes).

**Phase 2 — effects.** `SpellbookEffectPrimitives` lift-out (no behaviour
change), `RuneEffects.cs`, force-bolt parameterisation in
`ElementSpellEffects`.

**Phase 3 — input + persistence.** `SpellbookInputHandler` rework,
`SpellbookCampaignBehavior` (known runes, migration, UI, succession),
discovery flow.

**Phase 4 — NPCs + learning sources.** `SpellcasterLords`, `SpellcasterTroops`
(+ their `*Math`), `TowerCampaignBehavior.Menus`/`TowerMath`, ruins/backstory
grant hooks, the "stranger's book" keepsake pair-grant
(`RuneCatalog.PickStarterPair` + `Keepsakes.cs` text), debug grant-all.

**Phase 5 — docs + version.** v0.9.0 bump in all **five** places
(`src/TheDarkestNight.csproj`, `SubModule.xml`, `dist/TheDarkestNight/SubModule.xml`,
`CHANGELOG.md`, `README.md`), `LEGACY.md` section, `CLAUDE.md` Spellbook
paragraph update, lore flavour strings.

---

## 11. Verification

- `dotnet build src/TheDarkestNight.csproj` and
  `dotnet test tests/TheDarkestNight.Tests.csproj` after every phase — the
  test project failing to compile silently disables the suite, so both.
- In-game checklist (manual):
  1. Unlock book, draw `DDD` → close flame burst, "Fire Blast" line, Cinder
     discovered if unknown.
  2. `WWW SSS` (UUU DDD) → fireball bolt exploding on impact, "Fireball".
  3. `SSS AAA` (DDD LLL) → fog bank, unique visual, "Fog".
  4. `SSS SSS` → amplified burn duration.
  5. `SSS ADA` → "Fire Wall". `DDD LLL ADA` → "Fog Wall".
  6. Draw junk (`WSD` ×2) → fizzle, spellburn sometimes.
  7. `WDW SSS AAA SWS DAD` (Gale+Cinder+Tide+Calling+Echo) → two storm
     elementals, "The Twin Callings of the Tempest", occasional strain burn.
  8. `SAD SWS` (Night Mark + Calling) → a demon answers; sometimes it turns.
  9. Old v0.8 save load → migration line, runes granted, wands still fire.
  10. Watch an NPC caster lord battle → bound sequences cast, occasional
      misfire.
  11. New character with "a stranger's book" → Spellbook unlocked, exactly
      two runes known, first always an element, and the pair casts something
      real on day one.
