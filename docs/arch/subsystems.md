Detail for NPC mage AI, ritual systems, sea systems, and talent/focus costs. Indexed from CLAUDE.md.

### NPC Mage AI

`ElementLordAI.TryCast()` runs on cooldowns that vary by personality:
- Ashen lords: 6 s, no aging cost, cast proactively
- Calculating: 24 s; Impulsive: 10 s; default: 16 s (stretched up to ×2.5 near burnout by temperament — see `NpcCastPlanner.CooldownMult`)

AI priority: defensive burst (<40% HP) → heal burst (<30% HP) → attack (school-specific). `BanditMageAI` adds burnout risk scaled to bandit tier (35% → 15%).

### Ritual Systems (Sanctuary / Ashen Altars)

Both `SanctuaryCampaignBehavior` and `AshenAltarsCampaignBehavior` share the same hidden-accumulation pattern:
- Player sacrifices a resource per round (HP or prisoner)
- A hidden target is rolled; player decides to continue or stop
- Alignment multiplier scales yield (flipped sign between the two systems)
- NPC lords simulate 3–4 rounds automatically

### Sea Systems (Harbors / Voyages / Ventures)

`SeaCampaignBehavior` (in `src/Sea/`) adds harbor menus to 16 coastal towns, matched **by town name** at session launch (a failed match silently drops the port). Voyages run inside a wait game menu (`sea_voyage`): hazards (one storm roll, one corsair roll) are scheduled at voyage start and fire mid-crossing as inquiries; arrival teleports the party to the destination gate. Trade ventures persist in the save (`SEA_*` keys, parallel lists) and resolve on daily tick. NPC lords and caravans also use the sea lanes: on `OnSettlementLeftEvent` from a port they may be teleported to another port (lords only toward their existing AI target; caravans opportunistically), after an off-screen corsair resolution against their roster. All formulas — fares, travel hours, hazard odds, abstract boarding-battle resolution, venture margins, NPC sail gates — live in `SeaMath.cs`, which is pure (no TaleWorlds types) and covered by `PureLogicTests`. Voyage state is intentionally not serialized: a reload mid-crossing refunds the escrowed fare.

### Talent and Focus Point Costs

Elements and disciplines (Steel, Blood, Nature) are learned in the **Codex** (`MagicLearning`) with focus points. Cost escalates by how many powers you already hold: `TalentCostCurve.Cost(LearnedCount)` — 1 fp for the first power, 2 for the second, and so on (Fire is free from day one). Learning from a **teacher** costs one point less (min 1). `TalentId` still carries retired class/path enum values (Reaper, Pyrelord, the discipline classes, the Nature rites) kept **for save compatibility** — do not assume an enum member is still a live, purchasable talent; check `TalentSystem`'s definition table.

Campaign-map (non-battle) spells cost 1 aging day for the first cast per calendar day, then escalate. Battle casts pay the flat life-cost described in the pipeline section above.
