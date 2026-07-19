Detail for state persistence and campaign tick architecture. Indexed from CLAUDE.md.

### State: Static vs. Serialized

- **In-mission state** lives in static fields on `SpellEffects`, `ActiveEffects`, the `Element*`/`Nature*`/`Miracle*`/`Crystal*` classes, `ElementLordAI`, etc. `MainSubModule.OnGameStart()` and `MagicMissionBehavior.OnEndMission()` both clear all of it to avoid save-reload / mission carry-over.
- **Persistent hero state** is stored in `MageKnowledgeData` (serialized into the campaign save via TaleWorlds' `CampaignObject` extension API). This holds talent purchases, aging ledger, grimoire unlocks, whisper tiers, Rival Shadow counter, and pending event flags.
- **Other persistent state** is saved per-behavior, mostly as parallel lists keyed by prefixed strings (`SEA_*`, scheme, exchange, clan-order keys) via each behavior's `SyncData`, plus custom savedata types registered in `SaveDefiner.cs`. Nature reserves, Grace, and Dark Gifts persist through their own knowledge/inventory objects. Voyage-in-progress state is intentionally **not** serialized (a mid-crossing reload refunds the fare).

### Campaign Tick Architecture

`MagicCampaignBehavior` hooks three tick rates:
- **Daily:** aging decay, Whisper tier decay, Ashen resurgence logic
- **Weekly (14+ day slots):** independent general-event and war-event queues in `CampaignMapEvents`
- **On settlement enter/leave:** `SettlementEncounters`, gated by cooldown + renown + mage status

The other behaviors register their own daily/weekly/enter-leave hooks (e.g. `NatureCampaignBehavior`, `MiracleCampaignBehavior`, `SchemeCampaignBehavior`, `SeaCampaignBehavior`, `ExchangeCampaignBehavior`, `ClanOrdersCampaignBehavior`). Keep new tick logic in the behavior that owns the concern rather than piling it onto `MagicCampaignBehavior`.
