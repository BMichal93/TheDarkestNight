# LEGACY.md — the retired Ash and Ember caster paths

**None of this is how you cast in The Darkest Night.** The player's only casting path is **the Spellbook** — hold the focus key with free hands and tap a directional formula. See [`README.md`](README.md#the-spellbook).

This file documents the older Ash and Ember player-facing casting paths, which The Darkest Night **retired for the player** (`PlayerCastingEnabled = false` on each input handler). They are kept for two reasons, and both matter:

1. **The code is still alive and still runs.** Retiring these paths gated off the *player's input*, not the effects underneath. NPC lords (`ElementLordAI`), nature seers, Temple priests, the rare spellcaster troop tree, the Awakened, and demons all still cast through this same `ElementSpellEffects` / `NatureEffects` machinery. If you are changing how a spell *behaves*, this is the shape you are changing — for everyone on the field.
2. **Saves from earlier builds still carry this state.** Talents, aging ledgers, and path choices persist.

Read this as internals and NPC behaviour, not as instructions. Where it says "you," read "an NPC caster" — the second person is a fossil of when the player walked these paths.

### Still live for the player, despite living next to this material

Two things commonly mistaken for retired, because they are entangled with the paths below:

- **The Gift prompt still fires** at new-game start (*"Do you feel it still?"*) and still sets `MageKnowledge.IsMage`. It no longer opens the three-branch path menu described below; it keeps the Codex/NPC-parity plumbing consistent, and Alt+L still falls back to the legacy Codex for a non-Spellbook mage.
- **Aging is still a real cost** — just no longer a *casting* cost. The Spellbook never charges aging, but `AgingSystem` still ages and rejuvenates the player through the Ashen Ruins menus and the Reaping. Its documentation stayed in [`README.md`](README.md#aging-cost) for exactly that reason.

---

## Getting the Gift *(legacy — describes the underlying Ash and Ember caster paths and mostly no longer chosen at creation)*

**What actually still happens:** every new campaign still opens with a short yes/no "The Gift" prompt (`CampaignBehavior.Events.ShowGiftPrompt`) — *"Do you feel it still?"* — that sets `MageKnowledge.IsMage`. Answering yes no longer opens the old three-branch menu below or lets you cast through the retired element input; it exists to keep the underlying Codex/NPC-parity plumbing (and the Alt+L key's fallback to the legacy Codex for a non-Spellbook mage) consistent. **Actual player casting is exclusively the Spellbook** ([`README.md`](README.md#the-spellbook)). The three-path description below is kept for reference on what the *paths themselves* still are and drive for NPC lords/seers/priests.

Two paths open at campaign start. Each is permanent — you walk one or the other.

**The Inner Fire** — The fire must be *found*, not chosen at a menu.
- A prompt appears asking if the fire has always been there. Accepting grants the Gift.
- The Gift can also arrive through certain in-game events (aging, bloodline, encounters, companions).
- Once you carry the Gift, the grimoire is available at any time (Left Alt + X).

**The Living Ember** — For those who hear the land instead of carrying a fire within.
- At the same gift prompt, choose *"The world beneath me has always been louder than the fire."*
- This opens the Living Ember path — terrain-drawing, elemental powers, and hermit teachers.

**The Dark Gift** — For the cruel. If your hero is **Dishonourable**, the gift prompt also offers *"I bargained with the dark, and it marked me."* — choosing it starts you bearing **one random Dark Gift** (see *The Dark Altars and the Dark Gifts*). Visit a Dark Altar to buy more or renounce them.

All these paths are **mutually exclusive** — Inner Fire, Grace, Nature, and the Dark Gifts cannot be mixed. **The three-branch menu text above is not shown to the player** in The Darkest Night — only the simplified yes/no Gift prompt described above fires at new-game start. The paths, their talents, and their effects remain fully live for NPC lords, priest troops, and seer troops.

---

## Controls *(legacy — the underlying element input; retired for the player, still drives NPC lords/the Awakened/demons)*

*(As of v0.35.0, fire and nature magic are one unified art. The in-game journal entry **"Notes for the Adventurer"** always holds the authoritative, build-current controls; this is a summary. In The Darkest Night, the hold-and-charge gesture below is no longer bound to the player — see [**The Spellbook**](README.md#the-spellbook) for what Left Alt / focus now actually does for you in battle.)*

### Keyboard

| Action | Input |
|--------|-------|
| Focus | Hold **Left Alt** |
| Load a learned element | **W** Wind · **S** Earth · **A** Water · **D** Spirit (Fire is default) |
| Draw the charge | Stand still, hand free, armour light — hold to build power |
| Attack (element cone) | **Left Mouse** while focused |
| Wall (element barrier) | **Right Mouse** while focused |
| Open grimoire | **Alt + X** |
| Codex of the Inner Fire (learn) | **Alt + L** (campaign map) |
| Litany of Devotions (Grace talents) | **Shift + L** (campaign map) |

### Gamepad

| Action | Input |
|--------|-------|
| Focus | Hold **X** — bumpers are taken (LB = order radial, RB = miracles); X keeps both triggers free |
| Load a learned element | Flick the **left stick** (↑ Wind · ↓ Earth · ← Water · → Spirit); **click L3** for Fire |
| Attack / Wall | **Right Trigger** / **Left Trigger** |
| Open grimoire (map) | **LB + Right Bumper (RB)** |

### How casting works

1. **Hold the focus key.** Fire is loaded by default; tap a direction to load a learned element.
2. **Stand still and draw.** The longer you hold (up to ~5 s), the **stronger** the working — power peaks at five seconds.
   - **Overchannel:** keep pouring past **~10 s** and the working **overchannels** — it strikes **twice as hard**. You then have until ~15 s to loose it before the charge **disperses**. NPC mage-lords overchannel too (recklessly-tempered and desperate ones most).
   - There is no minimum, so an instant release is allowed but weak.
3. **Attack** looses the element's cone; **Block** raises its wall.
4. **The charge lingers.** Let go of Focus with a charge drawn and it **stays in your hand for ~4 seconds** — loose it with a lone Attack/Block, or re-take Focus to keep drawing. You no longer have to release the instant you stop drawing.

The life-cost of a cast is **flat** — the draw buys power, never a cheaper cast. The **Nature** discipline lowers that flat cost; the Ashen pay in criminal standing instead of years.

Magic is an **inborn gift**, so it deepens as you do: a spell's damage scales slightly with your **character Level** (+1% per level, up to +30%), so it never falls behind late in a long campaign. It lifts damage only — a cone's reach, a wall's depth and its ignite stay as tuned. (Enemy mage lords grow with their level the same way.) **Miracles** deepen with your **Conviction** — the summed strength of your aligned virtues — and **crystals** with your **Medicine**, on the same gentle, capped curve.

**Free hand and light armour required** — unless you know **Steel**, which lets you cast with a weapon drawn and bear twice the weight.

### The five elements

| Element | Attack | Wall |
|--------|--------|------|
| **Fire** | cone of fire | wall of fire |
| **Wind** | hurling, slowing blast | wall that turns arrows and bogs down |
| **Earth** | close, almost-melee crush of stone (short reach, heavy damage + root) | stone wall |
| **Water** | slowing wave | mist barrier |
| **Spirit** | fear + a stray order into enemy ranks | wall that heartens and mends your own |

Elements and disciplines (Steel, Blood, Nature) are learned in the **Codex** with focus points, or from a **teacher** for one point less. Each element also grants a **campaign-map working** cast through the grimoire's *Cast* menu.

---

## The Living Ember (legacy path)

> **Note:** As of v0.35.0 the living-world elements are folded into the unified magic above (learned as Wind / Earth / Water / Spirit), and the seers attuned to the land are now **teachers**. The separate Living-Ember attunement below remains for backward compatibility with existing saves.

A discipline for those who hear the living land — root, river, stone, and sky. They **choose** an element, draw it from the world around them, and release it as a natural force. Drawing is never free: every working spends the **living energy** of the place it is fought over, and a land stripped bare turns on those who force it.

### Choosing this path

At the gift prompt at campaign start, select **"The world beneath me has always been louder than the fire."** This grants attunement to the Living Ember instead of the Inner Fire. The two paths are mutually exclusive.

### Controls

You **choose** an element by tracing a direction while focused, gather a charge of it by **standing still**, then spend it with your **Attack** or **Block**. The focus key is shared with miracles (Grace, Cold, Nature and the Dark Gifts are all mutually exclusive).

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Choose element | Hold **Left Ctrl**, trace **W**/**S**/**A**/**D** | Hold **R3**, flick left stick |
| Gather the charge | …then **stand still** | …then stand still |
| Cast attack | Hold **Left Ctrl** + **Attack** (left mouse) | Hold **R3** + **Right Trigger** |
| Cast support | Hold **Left Ctrl** + **Block** (right mouse) | Hold **R3** + **Left Trigger** |
| Campaign map | Choose element in the litany (**Shift+X** / R3+L3); stand still ~4 hours to gather; cast via the litany | — |

A bar appears while you channel, coloured by your chosen element, filling over ~6 seconds; the charge then lasts ~30 seconds. **Requirements (battle):** both hands empty (no weapon or shield) and armour weight ≤ 25.

### Choosing the element

You pick which element to draw — the land no longer decides. Trace a direction:

| Direction | Element |
|-----------|---------|
| **W** (Up) | **Wind** |
| **S** (Down) | **Earth** |
| **A** (Left) | **Water** |
| **D** (Right) | **Storm** |

### Living energy — terrain and cost

Terrain no longer dictates which element answers; it dictates how dearly the draw **costs the land**. Every battlefield and stretch of country holds a hidden reserve of living energy, sized by how much grows there. Each terrain *favours* certain elements — drawing a favoured element spends little of the reserve; drawing against the land spends far more.

| Terrain | Favours (cheap to draw) |
|---------|-------------------------|
| Mountain, Hills, Steppe | **Wind** |
| Forest | **Earth** |
| River, Lake, Shore, Snow, Wetland | **Water** |
| Desert, Plains, Meadow | **Storm** |
| (other / mixed) | none in particular (neutral cost) |

**The reserve.** You are never shown the number, but the land warns you as it thins — at the **half**, the **quarter**, and when it runs **dry**. Both nature draws *and* Inner Fire casts spend it, for the player **and every NPC mage** alike. Forest brims (~120); desert holds almost nothing (~15). Left in peace, a place mends ~6% of its capacity per day. The reserves are saved with your campaign and persist across battles fought in the same region.

**Drawn past empty, the land bites back — but only at nature casters.** A nature draw on exhausted ground **bleeds the hearth of the nearest village** and has a ~35% chance to **sour**. The souring takes many forms — in battle: a raw recoil, dead briars that root you, a hollowing that saps your speed, a gout of grey ash, or a slow wither; on the march: a blood-tithe, blighted (spoiled) food, a contagious despair (morale loss), or a creeping fever that wounds the weakest. Player and NPC nature casters draw from the same palette.

**Inner Fire is immune to this** — fire does not commune with the land, it only burns it. A fire mage is never bitten back, but every fire cast still strips the local reserve, leaving the ground dangerous for any nature caster who draws there. In practice this makes the Living Ember **harder and riskier to use on a battlefield crowded with fire-mages** — they scorch the reserve dry, and the land takes its anger out on you, not them.

**The Old Green.** Any tavern offers a land-attuned hero a pouch of rare weeds (150 denars). Smoking it costs **−10% of your health** and a few drowsy hours, but for **24 hours** each nature draw has a **30% chance to cost the land nothing at all** — you are, briefly, part of it. A way to draw hard without killing the ground beneath you.

### Powers

Each element has one attack (Attack key) and one support (Block key).

| Element | Attack | Support |
|---------|--------|---------|
| **Wind** | **Gale** — 360° gust, ~22 damage + knockback + slow, 10 m | **Tailwind** — +35% speed for you and nearby allies, 15 s |
| **Earth** | **Entangle** — a close, almost-melee crush of rock (~5 m): heavy damage (~85) and foes held fast ~4 s | **Bulwark** — −40% damage taken for you and allies, 12 s |
| **Water** | **Torrent** — forward cone, ~30 damage + knockback that breaks formations | **Renewal** — heal yourself and nearby allies + morale |
| **Storm** | **Thunderclap** — ~65 damage bolt that chains to 2 more foes | **Stormstep** — an instant dash forward |

A held charge lasts ~30 seconds in battle. Only one charge at a time unless the **Living Root** talent is active. On the campaign map the support powers help your column (lighter march, mended wounded, a quickening).

### Hermit teachers

Three hermits scatter across the old lands. Each teaches one rite and is a one-time encounter, appearing when you enter a qualifying town with clan renown ≥ 100 (25% chance per visit, 3-day cooldown per settlement).

| Hermit | Region | Teaches |
|--------|--------|---------|
| **Gwydion the Root-Listener** | Battanian towns | Living Root |
| **Birna of the Still Water** | Sturgian towns | Still Draw |
| **Bekh the Open Hand** | Khuzait towns | Open Grip |
| **Tiryn of the High Root** | Marunath (village menu, always) | Deep Earth |
| **Faruk the Patient** | Aserai villages | Dawn Call |

Hermits do not appear for an Inner Fire mage.

### Talents

| Talent | Effect |
|--------|--------|
| **Living Root** | Charge capacity ×2 — hold two charges (two elements) at once. |
| **Still Draw** | The channel bar fills twice as fast. |
| **Deep Earth** | You draw gently — each charge spends only **half** the land's living energy. |
| **Open Grip** | Held charges no longer fade. |
| **Dawn Call** | On the campaign map the land fills your chosen charge an hour sooner. |
| **Wildsworn** | Class talent — bundles Living Root, Still Draw and Open Grip for 2 focus points. |

### Nature seers (NPC)

Some lords and companions carry attunement to the living world. In battle they draw and release nature charges independently — and their draws spend the battlefield's living energy exactly as yours do, so a place can be exhausted by either side. A seer who draws from drained ground risks the same souring recoil. Seeded at campaign start by culture: Battanian lords (~20%), Sturgian lords (~15%), Khuzait lords (~10%), others (~3%).

Two unit types appear rarely in warbands:
- **Battanian Forest Listener** — melee nature seer in Battanian parties
- **Sturgian Storm-Reader** — ranged nature seer in Sturgian parties

---

## Spell Forms (before Break) — *pre-v0.35 reference*

> **Superseded in v0.35.0.** The two-phase form/effect/Break system below describes the *old* Inner Fire. The player now casts with the unified element system (see **Controls → How casting works**). These sections are retained for players on older versions and because NPC mages still resolve their casts through the underlying blast/burst effects.

| Key | Arrow | Form | What it does |
|-----|-------|------|--------------|
| W | ↑ | **Blast** | Forward cone. Range = max(4, formCount × 2.5) m. Cone visuals scale to match. |
| A | ← | **Missile** | Fast projectile that travels forward then explodes. Range = max(8, missileCount × 3) m. Explosion radius = 1 + missileCount m. |
| D | → | **Barrier** | Wall of stationary fire nodes perpendicular to facing. One node per press, 1.5 m apart. Cast again to release. |
| S | ↓ | **Burst** | Circle centred on caster. Radius = max(2, formCount × 2.5) m. |

### Multi-form example

`WW SS X UUU` — Blast (5 m) + Burst (5 m) simultaneously, 75 fire damage to all units in range including allies. 7 inputs = 8 days cost.

---

## Effects (after Break)

Every damage key deals 25 fire damage per press (friendly fire included) — but each carries its own **nature**, with a weak innate effect that the matching enchantment talent amplifies:

| Key | Arrow | Nature | Per count | Innate effect (no talent) | Amplified by |
|-----|-------|--------|-----------|---------------------------|--------------|
| W | ↑ | **Sear** | 25 fire damage | +5 searing burn | **Immolate** |
| A | ← | **Force** | 25 fire damage | 1.5 m concussive push | **Scatter** |
| D | → | **Shred** | 25 fire damage | +4% damage taken for 4 s (max 12%) | **Sunder** |
| S | ↓ | **Restore** | 15 healing | +4 morale lift | **Hearthlight** |

Owning a key's talent replaces its weak innate effect with the full version — no double-dipping. **Smoulder** triggers on any damage nature. Natures mix freely in one cast: `WWA` after Break = 75 damage carrying sear ×2 + force ×1.

---
