// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenCampaignBehavior.cs
//
// Faction H — the Chosen (Southern Empire). Owns everything Phase 7 adds for
// this faction: town-scoping to Phycaon/Lycaron (via ChosenSettlements), and
// the PriestKing mechanics, all adapted directly from the God-King mechanic
// (Tribes/TribalKingdomBehavior.cs, which the mod author asked to reuse as
// the template) and retargeted from Khuzait to empire_s:
//
//   PriestKing Dominance — the ruling clan's influence pinned high, every
//     other Apostle capped low (mirrors MaintainGodKingInfluence/
//     CapLordInfluence) — the mechanical expression of "no vote regarding
//     distribution of castles, or policies."
//   Wives of Conquest    — every settlement the Chosen capture adds a woman
//     of that place to the PriestKing's household, actually wed via
//     Hero.Spouse = (mirrors AcquireConsort/CheckConquestWives), capped at
//     ChosenMath.PriestKingWifeMax.
//   Blood Succession     — on the PriestKing's death, the oldest living son
//     inherits (mirrors EnforceGodKingSuccession).
//   Self-Immolation      — a captured PriestKing burns rather than kneel to
//     a captor (mirrors CheckGodKingCapture).
//   Never allies with the Temple, at war whenever possible — UNLIKE
//     TribalKingdomBehavior's "No Quarter" (which re-declares war on
//     WHOEVER the Tribes just made peace with), this scopes the reversal to
//     the Temple (vlandia) specifically: peace/alliance with anyone else is
//     left alone, but the Chosen and the Temple are never allowed to stay at
//     peace. A daily safety-net check also declares war on the Temple if the
//     two are ever found at peace through some path that didn't fire
//     OnMakePeace (diplomacy grants, faction merges, etc).
//   Very expansive        — adapted from Legion's raid-nudge + forced-war-
//     after-too-long-at-peace technique (Factions/Legion/LegionCampaignBehavior.cs),
//     but markedly more aggressive: a higher raid-nudge chance, half the
//     peace tolerance, AND a chance to nudge an idle Apostle lord into
//     besieging an already-hostile neighbouring settlement instead of merely
//     raiding it — actual territorial conquest, not just raiding.
//
// The Rod of the Apostle (item + battle effect + distribution + buy menu)
// and player wife-taking (settlement-capture prompt + prisoner-conversion
// menu) live in their own files — see ChosenRodEffects.cs,
// ChosenCampaignBehavior.Menus.cs and ChosenCampaignBehavior.Wives.cs.
//
// Persistence: consort StringIds (CHO_CONSORT_IDS), processed-settlement ids
// for the wives-of-conquest sweep (CHO_PROCESSED_SETTLEMENTS), the initial-
// snapshot flag, and the peace-day streak — the same shapes
// TribalKingdomBehavior / LegionCampaignBehavior already persist.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class ChosenCampaignBehavior : CampaignBehaviorBase
    {
        internal const string TempleKingdomId = "vlandia";

        private static readonly Random _rng = new Random();

        // ── Persistent state ────────────────────────────────────────────────────
        private static List<string> _consortIds = new List<string>();
        private static HashSet<string> _processedSettlements = new HashSet<string>();
        private static bool _initialSettlementsRecorded = false;
        private static int _peaceDayStreak = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore store)
        {
            try { store.SyncData("CHO_CONSORT_IDS", ref _consortIds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try
            {
                var settled = _processedSettlements.ToList();
                store.SyncData("CHO_PROCESSED_SETTLEMENTS", ref settled);
                if (store.IsLoading)
                {
                    _processedSettlements = new HashSet<string>(settled ?? new List<string>());
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("CHO_INITIAL_RECORDED", ref _initialSettlementsRecorded); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("CHO_PEACE_STREAK", ref _peaceDayStreak); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            SyncWifeData(store);

            if (_consortIds == null) _consortIds = new List<string>();
            if (_processedSettlements == null) _processedSettlements = new HashSet<string>();
        }

        public static void ResetForNewGame()
        {
            _consortIds = new List<string>();
            _processedSettlements = new HashSet<string>();
            _initialSettlementsRecorded = false;
            _peaceDayStreak = 0;
            ResetWifeStateForNewGame();
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterChosenMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SweepGrantRodsToChosenLords(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { ChosenSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { CheckPriestKingCapture(); }                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { EnsureTempleWar(); }                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickRaidAndSiegeNudges(); }                 catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { TickWarEagerness(); }                       catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void OnWeeklyTick()
        {
            try { MaintainPriestKingInfluence(); }     catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { CapApostleInfluence(); }              catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { CheckConquestWives(); }               catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { SweepGrantRodsToChosenLords(); }       catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static Kingdom GetChosenKingdom() => ChosenCulture.GetChosenKingdom();

        // ── PriestKing dominance ─────────────────────────────────────────────────
        private static void MaintainPriestKingInfluence()
        {
            try
            {
                var chosen = GetChosenKingdom();
                var ruling = chosen?.RulingClan ?? chosen?.Leader?.Clan;
                if (ruling == null) return;
                if (ruling.Influence < ChosenMath.PriestKingInfluenceMin)
                    ruling.Influence = ChosenMath.PriestKingInfluenceMin;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void CapApostleInfluence()
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;

                var rulingClan = chosen.RulingClan;
                foreach (var clan in chosen.Clans.ToList())
                {
                    if (clan == null || clan.IsEliminated || clan == rulingClan) continue;
                    if (clan == Clan.PlayerClan) continue;
                    if (clan.Influence > ChosenMath.ApostleInfluenceCap)
                        clan.Influence = ChosenMath.ApostleInfluenceCap;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Wives of Conquest ────────────────────────────────────────────────────
        private static void CheckConquestWives()
        {
            try
            {
                var chosenTowns = Settlement.All
                    .Where(s => s.IsTown && s.OwnerClan?.Kingdom?.StringId == ChosenCulture.KingdomId)
                    .ToList();

                if (!_initialSettlementsRecorded)
                {
                    foreach (var t in chosenTowns) _processedSettlements.Add(t.StringId);
                    _initialSettlementsRecorded = true;
                    return;
                }

                foreach (var town in chosenTowns)
                {
                    if (_processedSettlements.Contains(town.StringId)) continue;
                    _processedSettlements.Add(town.StringId);
                    if (_consortIds.Count >= ChosenMath.PriestKingWifeMax) continue;
                    AcquireConsort(town);
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void AcquireConsort(Settlement capturedTown)
        {
            try
            {
                var chosen = GetChosenKingdom();
                var priestKingClan = chosen?.RulingClan ?? chosen?.Leader?.Clan;
                if (priestKingClan == null) return;

                string cultureId = capturedTown.Culture?.StringId ?? "";

                CharacterObject template = CharacterObject.All.FirstOrDefault(c =>
                    c != null && !c.IsHero && c.IsFemale && c.Culture?.StringId == cultureId);
                if (template == null)
                    template = CharacterObject.All.FirstOrDefault(c => c != null && !c.IsHero && c.IsFemale);
                if (template == null) return;

                int age = ChosenMath.RollWifeAge(_rng.NextDouble());
                Hero consort = HeroCreator.CreateChild(template, capturedTown, priestKingClan, age);
                if (consort == null) return;

                _consortIds.Add(consort.StringId);

                try
                {
                    var priestKing = chosen?.Leader ?? priestKingClan.Leader;
                    if (priestKing != null && priestKing.IsAlive && !priestKing.IsFemale && consort.IsFemale)
                        priestKing.Spouse = consort;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                InformationManager.DisplayMessage(new InformationMessage(
                    $"A woman of {capturedTown.Name} is brought into the PriestKing's household — one more oath sealed against the dark.",
                    new Color(0.85f, 0.72f, 0.25f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Blood Succession ─────────────────────────────────────────────────────
        private static void OnHeroKilled(Hero victim, Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            try { EnforcePriestKingSuccession(victim); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void EnforcePriestKingSuccession(Hero deadHero)
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;

                var rulingClan = chosen.RulingClan;
                if (rulingClan == null || rulingClan.Leader != deadHero) return;

                Hero heir = Hero.AllAliveHeroes
                    .Where(h => h.IsAlive && !h.IsChild && !h.IsFemale
                             && h.Clan == rulingClan && h != deadHero
                             && h.Father?.StringId == deadHero.StringId)
                    .OrderByDescending(h => h.Age)
                    .FirstOrDefault();

                if (heir == null)
                    heir = Hero.AllAliveHeroes
                        .Where(h => h.IsAlive && !h.IsChild && !h.IsFemale
                                 && h.Clan == rulingClan && h != deadHero)
                        .OrderByDescending(h => h.Age)
                        .FirstOrDefault();

                if (heir == null) return;

                try { ChangeClanLeaderAction.ApplyWithSelectedNewLeader(rulingClan, heir); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                InformationManager.DisplayMessage(new InformationMessage(
                    $"The PriestKing is dead. His heir {heir.Name} rises — the Chosen believe the vision passes with the blood.",
                    new Color(0.85f, 0.72f, 0.25f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Self-Immolation ──────────────────────────────────────────────────────
        private static void CheckPriestKingCapture()
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;

                var priestKing = chosen.Leader;
                if (priestKing == null || !priestKing.IsAlive || !priestKing.IsPrisoner) return;

                try { KillCharacterAction.ApplyByMurder(priestKing, null, false); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                InformationManager.DisplayMessage(new InformationMessage(
                    "The PriestKing would not be Heaven's prisoner and a captor's both. He is ash before his captors can savour the victory.",
                    new Color(0.85f, 0.35f, 0.15f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Never allies with the Temple, at war whenever possible ──────────────
        private static void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;
                if (faction1 != chosen && faction2 != chosen) return;

                var other = faction1 == chosen ? faction2 : faction1;
                if (!(other is Kingdom otherKingdom) || otherKingdom.StringId != TempleKingdomId) return;
                if (otherKingdom.IsEliminated) return;

                try { DeclareWarAction.ApplyByDefault(chosen, otherKingdom); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                if (ChosenCulture.IsPlayerChosen)
                    InformationManager.DisplayMessage(new InformationMessage(
                        "No peace with the false altar — the PriestKing's vision names the Temple an enemy of Heaven, and the war resumes.",
                        new Color(0.85f, 0.35f, 0.15f)));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // Daily safety net: if the Chosen and the Temple are ever found at peace
        // through a path that did not fire MakePeace (diplomacy grants, faction
        // merges, mod-conflict edge cases), close the gap directly.
        private static void EnsureTempleWar()
        {
            try
            {
                var chosen = GetChosenKingdom();
                if (chosen == null) return;
                var temple = Kingdom.All.FirstOrDefault(k => k.StringId == TempleKingdomId && !k.IsEliminated);
                if (temple == null) return;
                if (chosen.IsAtWarWith(temple)) return;

                try { DeclareWarAction.ApplyByDefault(chosen, temple); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Very expansive ────────────────────────────────────────────────────
        private static void TickRaidAndSiegeNudges()
        {
            var chosen = GetChosenKingdom();
            if (chosen == null) return;
            if (!IsAtWarWithAnyone(chosen)) return;

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null || !party.IsActive || !party.IsLordParty) continue;
                    if (party == MobileParty.MainParty) continue; // never override the player's own orders
                    var leaderClan = party.LeaderHero?.Clan;
                    if (leaderClan == null || leaderClan == Clan.PlayerClan) continue;
                    if (leaderClan.Kingdom != chosen) continue;
                    if (party.Army != null) continue;
                    if (party.MapEvent != null) continue;
                    if (party.BesiegedSettlement != null) continue;
                    if (party.ShortTermBehavior == AiBehavior.RaidSettlement
                     || party.ShortTermBehavior == AiBehavior.BesiegeSettlement
                     || party.ShortTermBehavior == AiBehavior.AssaultSettlement) continue;

                    // Territorial conquest first: a real siege of a weakly-held
                    // hostile settlement reads as "very expansive" far more than
                    // raiding alone. Rolled first (a separate, smaller chance) so
                    // it doesn't simply become a strict superset of the raid roll.
                    if (ChosenMath.ShouldNudgeToSiege(_rng.NextDouble()))
                    {
                        Settlement siegeTarget = FindWeaklyHeldHostileSettlement(party);
                        if (siegeTarget != null)
                        {
                            try { party.SetMoveBesiegeSettlement(siegeTarget, MobileParty.NavigationType.Default); }
                            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                            continue;
                        }
                    }

                    if (!ChosenMath.ShouldNudgeToRaid(_rng.NextDouble())) continue;

                    Settlement raidTarget = FindRaidTarget(party);
                    if (raidTarget == null) continue;

                    try { party.SetMoveRaidSettlement(raidTarget, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private static Settlement FindRaidTarget(MobileParty party)
        {
            try
            {
                Settlement village = Settlement.All
                    .Where(s => s != null && s.IsVillage && s.Village != null
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
                if (village != null) return village;

                return Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle)
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction))
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        // A hostile town/castle whose garrison + holding party strength is
        // weak relative to the raiding party — "weakly held" so the AI does
        // not throw a single lord party at a fortress it cannot hope to take.
        private static Settlement FindWeaklyHeldHostileSettlement(MobileParty party)
        {
            try
            {
                int partyStrength = party.MemberRoster?.TotalManCount ?? 0;
                if (partyStrength <= 0) return null;

                return Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle)
                             && s.MapFaction != null
                             && FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction)
                             && s.SiegeEvent == null
                             && (s.Town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0) < partyStrength)
                    .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        private static bool IsAtWarWithAnyone(Kingdom chosen)
        {
            try { return Kingdom.All.Any(k => k != null && !k.IsEliminated && k != chosen && chosen.IsAtWarWith(k)); }
            catch { return false; }
        }

        private static void TickWarEagerness()
        {
            var chosen = GetChosenKingdom();
            if (chosen == null) { _peaceDayStreak = 0; return; }
            if (IsAtWarWithAnyone(chosen)) { _peaceDayStreak = 0; return; }

            _peaceDayStreak++;
            if (!ChosenMath.ShouldForceWarDeclaration(_peaceDayStreak)) return;

            var target = FindNearestKingdom(chosen);
            if (target == null) return;

            try { DeclareWarAction.ApplyByDefault(chosen, target); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            _peaceDayStreak = 0;

            if (ChosenCulture.IsPlayerChosen)
                InformationManager.DisplayMessage(new InformationMessage(
                    $"The PriestKing has sat idle too long. The Chosen march on {target.Name} — Heaven's vision does not rest.",
                    new Color(0.85f, 0.35f, 0.15f)));
        }

        private static Kingdom FindNearestKingdom(Kingdom chosen)
        {
            try
            {
                var chosenTowns = Settlement.All.Where(s => s != null && s.IsTown && s.MapFaction == chosen).ToList();
                if (chosenTowns.Count == 0) return null;

                Kingdom best = null;
                float bestDistSq = float.MaxValue;
                foreach (var k in Kingdom.All)
                {
                    if (k == null || k.IsEliminated || k == chosen) continue;
                    var towns = Settlement.All.Where(s => s != null && s.IsTown && s.MapFaction == k).ToList();
                    if (towns.Count == 0) continue;

                    float dist = chosenTowns.Min(ct => towns.Min(t => (ct.GetPosition2D - t.GetPosition2D).LengthSquared));
                    if (dist < bestDistSq) { bestDistSq = dist; best = k; }
                }
                return best;
            }
            catch { return null; }
        }
    }
}
