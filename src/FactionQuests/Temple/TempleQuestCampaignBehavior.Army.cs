// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestCampaignBehavior.Army.cs
//
// The Vow itself — every Temple lord's party folded into ONE permanent army,
// and the disbandment ending once the kill target is somehow reached.
//
// ── The permanent merge ─────────────────────────────────────────────────────
// Adapted from Soldier/SoldierServiceCampaignBehavior.cs's proven ReassertArmy/
// SustainArmy shape (raise an Army for a lone commander via `new Army(kingdom,
// leaderParty, ArmyTypes.Patrolling)`, fold a party in via
// `party.Army = army; army.AddPartyToMergedParties(party);`, top cohesion up
// so it never bleeds out) — but generalised from "one lord + the player" to
// "every Temple lord with a party," and made PERMANENT rather than held for a
// fixed window:
//   • SoldierServiceCampaignBehavior.SustainArmy dissolves its own raised host
//     after SoldierServiceMath.HostHoldDays so a lone commander isn't locked
//     out of the realm's real armies forever. The Vow has no such release —
//     ReassertPermanentArmyHourly runs every hour, forever, for as long as
//     PhaseBound holds, re-forming the army the instant it's ever gone and
//     re-folding any Temple lord who has drifted out of it (a lord whose own
//     party died and respawned, one recruited fresh into the Order, one the
//     vanilla AI tried to peel into a different army — all self-heal on the
//     very next hourly tick).
//   • Cohesion is kept pinned at TempleQuestMath.ArmyCohesionTopUp whenever it
//     drifts below TempleQuestMath.ArmyCohesionFloor, exactly like SustainArmy
//     — this is what keeps the engine's own low-cohesion auto-dissolve from
//     ever actually firing.
//   • The leader seat is self-healing too: if the current leader dies, is
//     captured, or leaves the Order, PickNewLeader promotes another Temple
//     lord with a party the very next tick, and the reassert loop simply
//     re-forms around them.
// The player's own party is deliberately NEVER forced into this army — per
// this codebase's own precedent (BloodboundQuestCampaignBehavior's Ending B,
// ChosenQuestCampaignBehavior.Split.cs's header note) never drag the player
// into a fate they did not personally choose. A player who wants to fight
// alongside the Vow can still ride escort or answer a muster on their own.
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AshAndEmber
{
    public sealed partial class TempleQuestCampaignBehavior
    {
        // Persisted: which hero currently anchors the bound army (the army
        // itself is a live engine object, not something we can hand a stable
        // id of our own — we always re-resolve it through this hero's party).
        private static string _boundLeaderHeroId = "";

        private static void SyncArmyData(IDataStore store)
        {
            try { store.SyncData("TPLQ_LeaderHeroId", ref _boundLeaderHeroId); }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ResetArmyState()
        {
            _boundLeaderHeroId = "";
        }

        // ── Sealing the Vow — called once, right after delivery ─────────────
        internal static void BindPermanentArmy()
        {
            try
            {
                _phase = PhaseBound;
                var kingdom = GetTempleKingdom();
                var leader = TempleLeader() ?? PickNewLeader(kingdom);
                _boundLeaderHeroId = leader?.StringId ?? "";

                try { TempleQuestLog.Current?.LogBound(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                InformationManager.DisplayMessage(new InformationMessage(
                    "The Unbroken Vow is sealed. Every Templar sword marches as one host now, and will not stop " +
                    "marching until the dark is gone, or they are.", new Color(0.90f, 0.82f, 0.42f)));

                // Fold everyone in right away rather than waiting for the next
                // hourly tick, so the binding reads as immediate.
                ReassertPermanentArmy(kingdom);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── The self-healing reassert loop ──────────────────────────────────
        private void ReassertPermanentArmyHourly()
        {
            if (_phase != PhaseBound) return;
            ReassertPermanentArmy(GetTempleKingdom());
        }

        private static void ReassertPermanentArmy(Kingdom kingdom)
        {
            try
            {
                if (kingdom == null) return; // the kingdom itself is gone — CheckDisbandDaily's own guard handles that

                Hero leader = ResolveLeaderHero();
                if (leader == null || !leader.IsAlive || leader.IsPrisoner
                    || leader.PartyBelongedTo == null || leader.Clan?.Kingdom != kingdom)
                {
                    leader = PickNewLeader(kingdom);
                    _boundLeaderHeroId = leader?.StringId ?? "";
                }
                if (leader == null) return; // no Temple lord left with a party to anchor the host — nothing to reassert this tick

                MobileParty leaderParty = leader.PartyBelongedTo;
                Army army = leaderParty.Army;

                if (army == null)
                {
                    var objective = PickArmyObjective(kingdom, leaderParty);
                    if (objective != null)
                    {
                        try { army = new Army(kingdom, leaderParty, Army.ArmyTypes.Patrolling); }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); army = null; }
                        if (army != null)
                            try { army.AiBehaviorObject = objective; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }
                if (army == null) return; // nothing to fold the rest of the Order into this tick — try again next hour

                if (army.Cohesion < TempleQuestMath.ArmyCohesionFloor)
                    army.Cohesion = TempleQuestMath.ArmyCohesionTopUp;

                foreach (Hero hero in TempleLordsWithParties(kingdom))
                {
                    try
                    {
                        if (hero == leader) continue;
                        MobileParty party = hero.PartyBelongedTo;
                        if (party == null || party.Army == army) continue;

                        party.Army = army;
                        army.AddPartyToMergedParties(party);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static Hero ResolveLeaderHero()
        {
            if (string.IsNullOrEmpty(_boundLeaderHeroId)) return null;
            try { return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _boundLeaderHeroId); }
            catch { return null; }
        }

        // Every adult, alive Temple lord (excluding the player, whose party is
        // never forced into the Vow) who currently commands their own party.
        private static System.Collections.Generic.IEnumerable<Hero> TempleLordsWithParties(Kingdom kingdom)
        {
            if (kingdom == null) yield break;
            System.Collections.Generic.List<Clan> clans;
            try { clans = kingdom.Clans.ToList(); } catch { yield break; }

            foreach (Clan clan in clans)
            {
                if (clan == null || clan.IsEliminated || clan == Clan.PlayerClan) continue;
                System.Collections.Generic.List<Hero> heroes;
                try { heroes = clan.Heroes?.ToList(); } catch { continue; }
                if (heroes == null) continue;

                foreach (Hero hero in heroes)
                {
                    bool ok = false;
                    try { ok = hero != null && hero.IsAlive && !hero.IsChild && !hero.IsPrisoner && hero.PartyBelongedTo != null; }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    if (ok) yield return hero;
                }
            }
        }

        private static Hero PickNewLeader(Kingdom kingdom)
        {
            try
            {
                return TempleLordsWithParties(kingdom)
                    .OrderByDescending(h => h.Clan?.Tier ?? 0)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        // Mirrors SoldierServiceCampaignBehavior.PickArmyObjective's fallback
        // chain, simplified: a hand-built Army must be given a valid
        // AiBehaviorObject before anything reads it, or the army overlay UI
        // NREs describing it.
        private static Settlement PickArmyObjective(Kingdom kingdom, MobileParty leaderParty)
        {
            try
            {
                var s = leaderParty?.TargetSettlement;
                if (s != null) return s;

                s = leaderParty?.CurrentSettlement;
                if (s != null) return s;

                s = kingdom?.FactionMidSettlement;
                if (s != null) return s;

                if (kingdom?.Fiefs != null)
                    foreach (var town in kingdom.Fiefs)
                    {
                        var ts = town?.Settlement;
                        if (ts != null) return ts;
                    }

                if (kingdom?.Settlements != null)
                    foreach (var set in kingdom.Settlements)
                        if (set != null) return set;
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            return null;
        }

        // ── Ending A — the kill target is somehow reached ───────────────────
        // "Hope dwindles and the faction disbands": the merged army dissolves,
        // and every Temple clan (the player's own included — there is no war
        // being declared here, unlike ChosenQuestCampaignBehavior.Split.cs, so
        // nothing is gained by singling the player out) scatters out of the
        // kingdom. TaleWorlds marks a kingdom eliminated automatically once
        // its last clan departs — the same mechanic ChosenQuestCampaignBehavior.
        // Split.cs's own header note documents.
        internal static void CheckDisbandDaily()
        {
            if (_phase != PhaseBound) return;
            if (!TempleQuestMath.HasReachedKillTarget(_demonsKilled)) return;
            DisbandOrder();
        }

        private static void DisbandOrder()
        {
            _phase = PhaseEndedDisbanded;

            try
            {
                Hero leader = ResolveLeaderHero();
                Army army = leader?.PartyBelongedTo?.Army;
                if (army != null)
                    try { DisbandArmyAction.ApplyByObjectiveFinished(army); }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                var kingdom = GetTempleKingdom();
                if (kingdom != null)
                {
                    foreach (Clan clan in kingdom.Clans.ToList())
                    {
                        if (clan == null || clan.IsEliminated) continue;
                        bool isPlayerClan = clan == Clan.PlayerClan;
                        try { ChangeKingdomAction.ApplyByLeaveKingdom(clan, isPlayerClan); }
                        catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try { TempleQuestLog.Current?.LogDisbanded(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Unbroken Vow",
                    $"Fifty thousand. Someone, somewhere in the host, was still counting, and word of the tally " +
                    "spreads faster than any muster order ever did.\n\n" +
                    "It should feel like a victory. It does not. There is no fewer dark left tonight than there " +
                    "was the night the Vow was sworn — only fewer Templars left to swear it again. The Grand-" +
                    "Master, or whoever now wears that title, does not order the host to stand down so much as " +
                    "watch it simply stop marching together, hall by hall, sword by sword, until there is no " +
                    "'Order' left to speak the word for.\n\n" +
                    "The Temple does not fall to the dark. It just runs out of people willing to keep counting.",
                    true, false, "So it ends.", "", null, null), true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
