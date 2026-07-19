Detail for the spell cast pipeline. Indexed from CLAUDE.md.

### Spell Cast Pipeline

**Current (player): the Spellbook.** `SpellbookInputHandler.Tick` reads the formula gesture (hold Alt/Controller-RLeft, tap the U/D/L/R sequence with free hands) and matches it against `SpellbookCatalog`; a correct sequence dispatches to `SpellbookEffects`, which for the elemental spells simply calls the same `ElementSpellEffects`/`ElementWallWards`/`NatureEffects` entry points described below (Fireball/Firewall/gust/torrent/entangle/nova and their walls), plus Spellbook-only spells (Summon Demon via `DemonFactory`, Banish Demons, Light, wards/curses/veils). An incomplete or wrong sequence fizzles and rolls `SpellburnEffects` (base 60%, reduced by Intellect). This is the **only** player casting path — see `LEGACY.md` and the retired-input note in the legacy systems list for what it superseded.

**Underlying element pipeline (still the effect layer under the Spellbook, and still the player's path before Phase 4 retired it for NPC-cast parity discussion below):** `ElementMagicInput.Tick` reads Focus + direction + a stand-still charge, then `ElementSpellEffects` / `ElementWallWards` / `ElementUltimates` resolve the attack, wall, or ultimate. Life-cost is **flat** (the charge buys power, not a cheaper cast); the Nature discipline lowers it, and the Ashen pay in criminal standing.

**Attack forms (per element — each has its own silhouette so they read apart):**

| Element | Attack shape | Implemented in |
|---|---|---|
| Fire | **Flying bolt that explodes on impact** (bursts on first foe reached or at range's end) | `ElementSpellEffects.FireMissile` + `TickBolts`/`ExplodeBolt` (the `_bolts` list, ticked from `Tick`) |
| Wind | **Forward gust/stream** (broad wedge, knockback drives foes ahead) | `NatureEffects.BattleGale` (shared source) |
| Earth | **Short, almost-melee cone of erupting rock** (close fan, heavy damage + root — reach traded for force) | `NatureEffects.BattleEntangle` (shared source) |
| Water | **Forward slowing wave** (cone) | `NatureEffects.BattleTorrent` (shared source) |
| Spirit | **Nova** (radial panic + random enemy order) | `ElementSpellEffects.SpiritPanic` |

`CastAttack(el, caster, power)` is the single dispatch choke point — the player (`ElementMagicInput`), NPC lords (`ElementLordAI`), and the Awakened (`ElementalBeings`) all cast through it, so changing an attack shape there is automatically NPC-parity-correct. It also folds in the **mastery scale** (`ElementMagicMath.MasteryScale(hero.Level)`, +1%/level capped at +30%) by multiplying `power` for hero casters — because `ChargeFraction` clamps at 1, a `power > 1` lifts only the direct damage, never the tuned cone reach / wall depth / ignite (the same path the overchannel already rides). Non-hero casters (troops, the Awakened) map to no hero and keep ×1. Crystals scale the same way through `CrystalEffects.Potency` (player Medicine → `CrystalMath.MasteryScale`); miracle **damage** through `MiracleEffects.Conviction` (caster's summed aligned virtue → `MiracleMath.ConvictionScale`). **Wind/Earth/Water still delegate to the shared `NatureEffects` (Gale/Entangle/Torrent), which the Living Ember nature discipline also casts** (`NatureSeerAI`, the nature input handler — both still live in `MagicSystem`), so reshaping them there deliberately reshapes the nature-discipline versions too (consistent with the "one magic" unification). The fire bolt is a self-contained projectile (no legacy `SpellCast`/`Agent.Main` dependency) so it works for any caster; it trails fire each tick and is cleared with the rest of battle state via `ElementSpellEffects.ClearBattleState`.

**Legacy (NPC and underlying effects):**
```
MagicInputHandler (Alt+Direction buffers)
  → SpellBuilder.Parse(formBuffer, effectBuffer) → SpellCast
  → AgingSystem.ComputeBattleAgingCost(totalInputs) → days cost
  → SpellEffects.Execute*() → dispatches by spell type
```
`SpellEffects.cs` is the core partial class; `BlastSpells.cs`, `SelfSpells.cs`, `CreateSpells.cs`, and `AffectSpells.cs` extend it by spell form. NPC mage lords still cast through this path.

## Player casting model

**The pitch:** the world shattered overnight. Demons crawl out from the underworld every dusk and hunt the living; humanity survives behind walls, wards, and eight desperate factions born from the old kingdoms. Gold has stopped mattering — barter and scarcity define the economy. Magic is cast by tapping directional formulas (the Spellbook), not born of noble blood. Somewhere past day 1000, a named Demon Lord may rise to end the world, or be killed to end the Night.

The player-facing casting model is the **Spellbook — the Scrived Word** (`src/Spellbook/`, rune system as of v0.10.0, `RUNE_MAGIC_PLAN.md`): with free hands, hold a modifier and draw **runes** — each exactly three marks of U/D/L/R — then release the binding. `RuneSequenceMath` (pure) resolves the drawn runes as a sentence (repetition amplifies; two elements fuse, three form a Triad, four the Unbound Weave; one Form reshapes the working; Manners stack) and `RuneEffects` dispatches the resolved working into the same `ElementSpellEffects`/`ElementalFactory`/`DemonFactory`/`SpellEffects` primitives the mod already uses. A real rune drawn is learned on the spot; a malformed/over-long binding fizzles and rolls **spellburn** (self-harm, immobilization, a rogue demon, and more), and even a valid long binding carries **strain**. The legacy `SpellbookCatalog`/`SpellId`/`SpellbookEffects` path is retained for **wands, the Chosen's Rod, and NPC caster lords/troops** (their bound workings) plus save migration (`RuneCatalog.RunesForLegacySpell`). The Spellbook supersedes the older player-facing casting inputs (the unified element hold-and-charge system, the Miracles/Grace gesture, the Nature discipline, and campaign-map spells) — those input paths are now gated off for the player (`PlayerCastingEnabled = false` on each handler) but their **effect code and NPC casting paths are still very much alive**: NPC lords (`ElementLordAI`), the rare spellcaster troop tree (`src/Spellbook/SpellcasterTroops.cs`), and the Awakened/demons all still cast through the underlying `ElementSpellEffects`/`NatureEffects` machinery.
