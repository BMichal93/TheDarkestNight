// =============================================================================
// THE DARKEST NIGHT — CityStates/FactionScoping.cs
//
// Phase D fix (v0.8.0 playtest issue 4): a new campaign showed no free
// city-states because CityStateSystem.OnDailyTick waited
// CityStateMath.SettleDelayDays (3 days) before its first conversion pass,
// and each faction's own ScopeToStartingTowns ejection (which is what
// orphans a town in the first place) only runs from that same daily tick.
// A player who saved and reported the bug inside day 1-3 would see every
// non-core town still sitting with its renamed core faction.
//
// This helper makes the whole faction-scoping + city-state-conversion pass
// run once, eagerly, at new-game setup (see CampaignBehavior.Events.cs'
// FinishNewGameWorldSetup), in explicit order: scope every faction down to
// its starting towns FIRST (so their orphaned clans exist), then convert
// the newly-ownerless towns into city-states. The daily tick keeps running
// afterward as the ongoing repair pass — this is only the first sweep.
// =============================================================================

namespace TheDarkestNight
{
    internal static class FactionScoping
    {
        public static void ScopeAllFactionsNow()
        {
            try { WolfBrothersSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TowerSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ForestWidowsSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { BloodboundSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TempleSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { EmpireSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { LegionSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { ChosenSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
