// =============================================================================
// THE DARKEST NIGHT — Spellbook/SpellcasterTroopBehavior.cs
//
// Requirement 15 — makes the Hollow Choir (ModuleData/troops.xml:
// hollow_apprentice) genuinely rare to encounter or recruit. The troop is
// deliberately is_basic_troop="false" in its XML definition, so vanilla
// recruiting never generates one on its own; the ONLY way one enters play is
// this weekly, low-probability seed into a single notable's volunteer slot
// in a town — mirroring how PriestTroops.WeeklySeed tops up garrisons, but
// gated by a real roll (SpellcasterTroopMath.RollSeeds) instead of running
// unconditionally every week.
//
// Not persisted: `_seededSettlementIds` is a plain in-memory set. A save
// reload simply re-rolls the (tiny) chance for settlements it hasn't already
// seeded this session — an acceptable trade for a flavour rarity system,
// the same choice ElementLordRegistry/SpellcasterLords already make for their
// own seeded-not-serialized populations.
// =============================================================================

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public class SpellcasterTroopBehavior : CampaignBehaviorBase
    {
        private const string ApprenticeId = "hollow_apprentice";
        private static readonly Random _rng = new Random();
        private static readonly System.Collections.Generic.HashSet<string> _seededSettlementIds
            = new System.Collections.Generic.HashSet<string>();

        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore store) { }

        public static void ResetForNewGame() => _seededSettlementIds.Clear();

        private static void OnWeeklyTick()
        {
            try
            {
                if (Campaign.Current == null) return;
                CharacterObject apprentice = MBObjectManager.Instance?.GetObject<CharacterObject>(ApprenticeId);
                if (apprentice == null) return;

                foreach (var settlement in Settlement.All)
                {
                    try
                    {
                        if (settlement == null || !settlement.IsTown) continue;
                        if (_seededSettlementIds.Contains(settlement.StringId)) continue;
                        if (!SpellcasterTroopMath.RollSeeds(_rng.NextDouble())) continue;

                        var notable = settlement.Notables?
                            .Where(h => h != null && h.IsAlive)
                            .OrderBy(_ => _rng.Next())
                            .FirstOrDefault();
                        if (notable == null) continue;

                        var slots = notable.VolunteerTypes;
                        if (slots == null || slots.Length == 0) continue;
                        int slot = slots.Length - 1; // overwrite the last recruit slot only
                        slots[slot] = apprentice;

                        _seededSettlementIds.Add(settlement.StringId);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
