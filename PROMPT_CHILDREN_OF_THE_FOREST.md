# THE DARKEST NIGHT — Wand Scarcity & The Children of the Forest — Build Prompt for Claude Code

> Paste this whole file as the task, or tell Claude Code: *"Read PROMPT_CHILDREN_OF_THE_FOREST.md and execute it phase by phase, starting at Phase 0."*

---

## Mission

Four features, built on the existing wand system (which is **already done** — do not rebuild it):

1. **Wands become scarce in shops** — the wandwright no longer stocks the whole catalog.
2. **Weapons, horses and armour become scarcer at town traders** (armour is currently not thinned at all).
3. **A new special city-state: the Children of the Forest**, seated at **Pen Cannoc** (the Battanian town — the brief spells it "Per Cannoc"; the real settlement name is *Pen Cannoc*, match tolerantly by name). They live beside strange woods and cut wands from its branches. They are **peaceful**. Their lore: they do not raise armies — they **charm their enemies into serving as their soldiers**, guardians of the forest.
4. **Their market sells almost no weapons — but it sells wands**, and **their lords wield wands in battle instead of normal weapons (mostly)**, and those lords are **young adults** (not children, not middle-aged).

`CLAUDE.md` describes the code; `behaviour.md` describes how to work on it (API verification, build/test, version bump). **Read both before writing any code.** The "Non-negotiable working rules" of `PROMPT_THE_DARKEST_NIGHT.md` all still apply: phase-by-phase commits, explore before editing, never guess a TaleWorlds API (grep for an existing usage and copy its exact form), pure `*Math.cs` tuning with tests in `tests/PureLogicTests.cs`, `catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }` everywhere, climatic Dark-Souls/Gothic tone in every player-facing string.

---

## What already exists (read all of it in Phase 0 — none of it is to be rebuilt)

| Piece | Where | What it does today |
|---|---|---|
| Wand items | `ModuleData/items.xml` (16 `aae_wand_*` items) | Horse-whip-template melee items; safe, verified fields |
| Wand catalog | `src/Wands/WandsCatalog.cs` | 16 wands: 10 Standard (element attack/wall pairs), 6 Dramatic |
| Wand battle effect | `src/Wands/WandEffects.cs` | On a landed melee hit **with the wand as the wielded weapon**, casts the bound spell via `SpellbookEffects.Cast`; player = 8 charges per item id (persisted), NPC = 12% break per use |
| Wand tuning | `src/Wands/WandsMath.cs` | Prices (12000/20000), charges, cooldown, break chance, ruin-loot chance (0.05), Tower/Chosen lord grant chance (0.15, rolled once per hero ever) |
| Wand shop | `src/Wands/WandsCampaignBehavior.cs` + `.Menus.cs` | "Seek the wandwright" town-menu shop in ONE random currently-held Tower town and ONE Chosen town (re-picked only if lost); **stocks all 16 wands, always, unlimited** |
| Ruin loot | `src/Ruins/` + `WandsMath.RollRuinWandLoot` | Rare wand drops while exploring Ruins — leave untouched |
| City-states | `src/CityStates/CityStateSystem.cs`, `CityStateMath.cs`, `CityStateCampaignBehavior.cs` | Orphaned towns become one-city "Clan <X>" kingdoms with bandit culture; **The Camp (Revyl)** is the existing special case: name-matched, fixed identity/banner/colours, keeps its real culture, kept out of every war (`ReassertCampPeace` + `AshenDiplomacyModel` discouragement), rebranded in place on older saves (`RebrandCampIfPresent`) |
| Market scarcity | `src/Economy/MarketScarcityCampaignBehavior.cs` + `EconomyMath.cs` | Daily prune of town markets: food ×0.05, horses capped at 1, weapons above tier 3 removed and the rest ×0.40. **Armour is untouched today** |

**A latent gap you must respect (and exploit):** `WandsCampaignBehavior.GrantWandToHero` and the Chosen Rod's `GrantRodOfApostle` only add the item to the hero's **party item roster**. Agents spawn into battle from `hero.BattleEquipment` — a roster item is never wielded, so `WandEffects.OnAgentHit` (which reads `affectorWeapon.Item.StringId`) can never fire for those lords unless something *equips* the wand. Verify this reading yourself, then: for the Children of the Forest, the equip step is **mandatory** (Phase 4). Whether to also equip Tower/Chosen lords' wands is your call — a small, safe follow-up is welcome (it makes their existing 15% grants real); if you skip it, record the decision in the commit message. The proven write-seam for hero equipment in this codebase is `src/Crystals/CrystallinesCampaignBehavior.cs:148-150` — `hero.BattleEquipment[(EquipmentIndex)i] = new EquipmentElement(item)`.

---

## Phase 0 — Recon (no code)

Read end-to-end: the seven `src/Wands/*` and `src/CityStates/*` files, `src/Economy/MarketScarcityCampaignBehavior.cs`, `src/Economy/EconomyMath.cs`, `src/Factions/Chosen/ChosenCampaignBehavior.Rod.cs`, `src/Crystals/CrystallinesCampaignBehavior.cs` (the `BattleEquipment` write), `src/Units/LordGearWeathering.cs` (it rewrites lord equipment weekly — you must not fight it, and it must not strip your wand), and the `AshenDiplomacyModel` Camp-peace seam. Confirm in `src/Factions/ForestWidows/ForestWidowsMath.cs` that Pen Cannoc is **not** a Forest Widows starting town (`town_B1`/`town_B3` are Marunath and Car Banseth), so its clan is ejected and Pen Cannoc already falls into an ordinary "Clan <X>" city-state — the Children are a **re-identification** of that existing conversion path, exactly like The Camp was for Revyl.

---

## Phase 1 — Wands become scarce in shops

The wandwright currently lists all 16 wands, forever. Replace that with a small rotating stock:

- Each wandwright shop town independently stocks **`WandsMath.ShopStockSize = 3`** wands, re-rolled every **`WandsMath.ShopRestockDays = 14`** days (ride the existing weekly tick plus a persisted last-restock day; don't add a new tick if the existing cadence can carry it).
- Each stocked wand can be bought **once**; a bought slot stays empty until the next restock ("the case holds three rods today, and one hollow where a fourth once lay").
- Persist per-town stock and restock timestamps with the same parallel-list `WND_*` `SyncData` pattern `WandsCampaignBehavior.SyncData` already uses. Menu options for out-of-stock/empty slots hide, not disable.
- Stock selection is a pure, deterministic function in `WandsMath` (inputs: rolls/seed, catalog count, stock size; no TaleWorlds types) with `PureLogicTests` coverage — mirror `PickWandIndex`'s shape. No duplicate wands within one restock.
- Update the wandwright's shop-description text to sell the scarcity ("each answers only to the one thing it was made to say" stays; the case is now nearly empty).

---

## Phase 2 — Weapons, horses and armour scarcer at traders

All in `EconomyMath` constants + pure helpers (tests updated), applied by the existing `MarketScarcityCampaignBehavior.AdjustRoster` — no new behavior class:

- **Weapons:** tighten `CrudeWeaponTierCap` 3 → **2** and `TownWeaponSaleFactor` 0.40 → **0.15**.
- **Horses:** `TownHorseSaleCap` 1 stays, but most towns should show **none**: add a deterministic per-town-per-day gate (e.g. hash of settlement id + day, pure function `TownSellsHorsesToday(...)` ≈ 1 town in 3) so horses read as a lucky find, not a guarantee.
- **Armour (new):** add an `item.HasArmorComponent` branch — armour above **`ArmorTierCap = 3`** vanishes, the rest thinned by **`TownArmorSaleFactor = 0.30`**. Verify the exact component-check member against an existing usage or the DLLs before trusting `HasArmorComponent`.
- Remember `AdjustRoster` also runs for castles (`Town` backs both) — that is intended; keep it.
- Wand items must never be pruned by the weapon branch (they are weapon-component items); exempt `WandsCatalog.IsWandItemId` explicitly.

---

## Phase 3 — The Children of the Forest (Pen Cannoc)

Model this **exactly** on The Camp special case — same seams, second instance:

- `CityStateMath`: add name-match `IsPenCannocHomeSettlement` ("Pen Cannoc", case-insensitive — the same tolerant name matching `IsRevylHomeSettlement` uses), kingdom name **"Children of the Forest"**, an informal name, ruler title (something like "Warden of the Strange Wood" — your words, in tone), encyclopedia text carrying the lore: *they dwell at the edge of woods that are older and stranger than the Night itself; they cut wands from its branches; they keep no army of their own — those who march against them come home changed, and stay to guard the trees*. Fixed banner colours (deep forest green field, pale device) and a fitting native `banner_icons.xml` icon id (verify the id exists the way `CampBannerIconMeshId = 400` was verified).
- `CityStateSystem.CreateCityState`: branch on Pen Cannoc the same way `isCamp` branches — fixed identity, fixed banner on kingdom **and** ruling clan, **skip `ApplyBanditCulture`** (they keep the Battanian culture and troop tree; the "culture" of the Children is expressed through the kingdom identity, encyclopedia text, and their charmed-soldiers lore — do **not** attempt to mint a new `CultureObject` at runtime unless you first verify a safe, precedented way; there is none in this codebase).
- **Rebrand pass for existing saves**: extend `OnSessionLaunched`'s `RebrandCampIfPresent` pattern to also re-identify an already-converted generic "Clan <X>" city-state whose home settlement is Pen Cannoc — idempotent, no new mandatory save keys.
- **Peaceful:** generalize the Camp's two-layer peace (the `AshenDiplomacyModel` score discouragement + the daily `ReassertCampPeace` force-peace backstop) into a shared "sanctuary kingdoms" check covering both The Camp and the Children of the Forest — one predicate (`IsSanctuaryKingdom` or similar), both call sites, so a third such kingdom later is a one-line addition.
- Refactor shared Camp/Forest identity plumbing rather than copy-pasting it, but keep the diff surgical — `ApplyCampIdentity`'s reflection helpers are already general (`SetKingdomField`/`SetKingdomColorField`); reuse them.

---

## Phase 4 — Wands of the strange wood: market, shop, and wand-wielding lords

**Market (almost no weapons for sale):** in `MarketScarcityCampaignBehavior`, when the settlement belongs to the Children of the Forest (same `MapFaction` membership shape as `CityStateSystem.IsCampSettlement`), the weapon branch strips **all** weapons regardless of tier (constant `ForestWeaponSaleFactor = 0f` or a tier cap of 0 — pure helper + test). Horses/armour/food follow the ordinary town rules.

**Wandwright:** Pen Cannoc becomes the third wandwright town — **permanent**, not randomly re-picked (they make the wands; the Tower and Chosen towns merely import them). Extend `IsWandShopTown` to include the Children's seat. Their case is fuller than the imports': stock size **5** where the others hold 3 (`WandsMath.ForestShopStockSize`), same restock cadence, same one-purchase-per-slot rule from Phase 1. Give the Pen Cannoc shop its own short intro text (the wandwright here is a Child of the Forest; the wood is described, never explained).

**Lords wield wands (mostly):**

- Extend the existing rolled-once-per-hero sweep in `WandsCampaignBehavior.SweepGrantWandsToLords` to lords of the Children of the Forest kingdom with **`WandsMath.ForestLordWandChance = 0.85`** ("mostly").
- For a Forest lord who wins the roll: add the wand to the party roster (as today — the NPC break path `TryRemoveOneFromWielderRoster` strikes a roster copy, so the roster copy must exist) **and equip it**: write the wand into the hero's `BattleEquipment` primary weapon slot (`EquipmentIndex.Weapon0`), leaving at least one other weapon slot untouched as a sidearm ("mostly" wands, not helplessly wand-only when it breaks mid-mission). Use the exact `CrystallinesCampaignBehavior` write pattern; verify slot enum values against an existing usage.
- Make the equip **idempotent and self-healing** on the weekly sweep (another system — `LordGearWeathering` rewrites lord weapon slots — may replace it; re-assert, don't stack). Check `LordGearWeathering`'s slot loop and make sure it either skips wand items or your re-assert runs after it deterministically; state in a comment which one you chose and why.
- Decide (and record) whether Tower/Chosen wand-holding lords get the same equip fix — see the latent-gap note above.

**Young-adult lords who never age:** at conversion/rebrand time, every lord of the Children's ruling clan is aged into the **16–18** window (Bannerlord's young-adult band — this is the intended look, faces just past coming-of-age, not middle-aged and not children; never set an age below the campaign's coming-of-age so party leadership and command keep working — verify `Campaign.Current.Models.AgeModel.HeroComesOfAge` or the equivalent seam). Verify the hero-age API against the DLLs before use (`Hero.SetBirthDay` / `Hero.BirthDay` — `behaviour.md` rules apply; do not guess). Target age per hero is a pure deterministic function of the hero's `StringId` hash in `CityStateMath` (+ test) so a reload never re-rolls ages. Apply once and make it idempotent (a hero already inside the window is left alone).

**And they must not age at all** — the wood keeps them. A one-time set is not enough: campaign time marches on and vanilla aging would carry them out of the 16–18 window within a couple of in-game years. Hold each Forest lord's age fixed for the life of the campaign. Investigate the cheapest reliable seam and pick one, recording the choice and why in a comment + the commit message:
  - **Re-assert (simplest, proven):** on the existing weekly sweep, if a Forest lord has drifted out of the young-adult window, push their `BirthDay` forward so their age lands back in it — a rolling anchor keyed off `CampaignTime.Now` and the deterministic target age, so it is idempotent and reload-safe (no new saved state; age is re-derived from live campaign time each week). This is the low-risk default and needs no unverified API.
  - Only if you find and **verify** a clean per-hero "disable aging" flag in the DLLs (do not assume one exists — Bannerlord has no obvious public one) may you use it instead of the re-assert; otherwise use the re-assert.
  The drift-check threshold and the re-anchor math live as pure functions in `CityStateMath` with `PureLogicTests` coverage (inputs: current campaign day, hero birth day, target age; no TaleWorlds types).

---

## Phase 5 — Ship it

- `dotnet build src/TheDarkestNight.csproj` **and** `dotnet test tests/AshAndEmber.Tests.csproj` green (a compiling build alone is not enough — the test project failing to compile silently disables the suite). If game DLLs are unavailable, run the pure tests and say so in the commit message.
- Version bump in all **four** places (`src/TheDarkestNight.csproj`, `SubModule.xml`, `dist/AshAndEmber/SubModule.xml`, `CHANGELOG.md` — promote Unreleased to **v0.5.0 — The Children of the Forest**) per `behaviour.md`.
- Update `CLAUDE.md`'s system map where behavior changed (wand shop stock, market armour thinning, the second sanctuary kingdom).
- Changelog entries in the same voice as v0.4.0's Camp entry: what changed, which requirement, and any decision taken (Tower/Chosen equip fix yes/no, gear-weathering interaction).

---

## Acceptance checklist

| # | Requirement | Where proven |
|---|---|---|
| 1 | Wandwright stocks a small rotating subset, one purchase per slot, persisted | Phase 1, `WandsMath` tests |
| 2 | Weapons ×0.15 ≤ tier 2; horses a rare find; armour thinned ≤ tier 3 ×0.30 | Phase 2, `EconomyMath` tests |
| 3 | Pen Cannoc becomes "Children of the Forest": fixed identity, banner, encyclopedia charm-lore, Battanian culture kept, old saves rebranded | Phase 3 |
| 4 | Children of the Forest never at war (score discouragement + daily force-peace) | Phase 3, shared sanctuary predicate |
| 5 | Pen Cannoc market: no weapons; permanent wandwright with the fullest case | Phase 4 |
| 6 | ~85% of Forest lords carry **and actually wield** a wand (`BattleEquipment`), sidearm kept, self-healing vs. gear weathering | Phase 4 |
| 7 | Forest lords are young adults (16–18, deterministic, ≥ coming-of-age) **and never age** (held in the window for the whole campaign) | Phase 4, `CityStateMath` tests |
| 8 | Ruin wand loot untouched; player charge economy untouched | no diff in those files |
| 9 | Build + pure tests green; version bumped in 4 places; changelog written | Phase 5 |
