Detail for key numerical constants. Indexed from CLAUDE.md.

### Key Numerical Constants

Numeric tuning lives in the pure `*Math.cs` files (each system has its own); those files are the source of truth. A few stable, cross-cutting values:

| Thing | Value |
|---|---|
| Mage lord fraction | ~20% of lords |
| Ashen lord fraction | ~10% of lords |
| Settlement encounter cooldown | ~6–7 days |
| World event slot interval | 14+ days |
| Bandit-unit mage fraction | ~4% of eligible units |

**Legacy two-phase values (NPC casts / underlying effects only — verify against code before relying on them):** max 5 form + 5 effect inputs per cast; aging cost `round(1.5^(n−1))` capped at 84 days; Sear/Force/Shred base ~22–35 HP per input; Restore ~15 HP per input; blast/burst radius 2.5 m per input; missile range 3 m per input. The current player casting model is flat-cost, charge-scaled (see the pipeline section), not per-input.
