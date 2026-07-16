// =============================================================================
// THE DARKEST NIGHT — Apocalypse/DemonLordSystem.cs
//
// Requirement 33, stage 3 — the Demon Lord. A real campaign presence: an
// actual Hero, ruling an actual one-clan Kingdom, leading an actual growing
// MobileParty that besieges and takes settlements through vanilla siege AI.
// Not a text event.
//
// ── Design decision: how he is founded ──────────────────────────────────────
// Every other summon in this codebase that needs a faction avoids building a
// Kingdom from scratch (see DemonSpawnCampaignBehavior's header note) — but
// Phase 11 explicitly requires him to conquer and HOLD settlements, which
// vanilla only lets a Kingdom-affiliated clan do. So, once, for him alone,
// this file follows the exact proven sequence CityStateSystem.CreateCityState
// already uses successfully elsewhere in this mod (Kingdom.CreateKingdom +
// InitializeKingdom + ChangeKingdomAction.ApplyByCreateKingdom), but for the
// CLAN itself uses Clan.CreateSettlementRebelClan — the same public, vanilla,
// battle-tested API the game itself uses when a settlement's notable rebels
// and seizes a fief — because that is exactly our scenario: the Demon Lord
// violently takes one settlement and becomes its lord. Nothing here invents
// raw MBObjectManager plumbing; every step is a public API already proven
// elsewhere in this codebase or in vanilla itself.
//
// ── Identifying him in a mission ─────────────────────────────────────────────
// HeroCreator.CreateSpecialHero wraps the "demon_lord" troops.xml template in
// a freshly cloned CharacterObject with its own generated StringId, so
// DemonCatalog's plain id lookup will never find him once he's a Hero.
// DemonBattleBehavior.OnAgentBuild instead asks IsTrackedHero(hero) — the same
// Hero-identity pattern SpellcasterLords.MissionTick already uses.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public static class DemonLordSystem
    {
        public const string KingdomId = "demon_lord_kingdom";
        private const string LordTemplateId = "demon_lord";

        // A handful of grim, unique names — one is picked once per campaign so
        // no two playthroughs' endgame boss reads identically.
        private static readonly string[] Names =
        {
            "Vhorrath, the Demon Lord",
            "Ka'ruzek, the Devourer of Dusk",
            "Mor'gathis, the Unmaking",
            "Skaraneth, Who Wears the Night",
            "Ulgrimoth, the Last Hunger",
        };

        private static readonly Random _rng = new Random();

        private static string _heroId    = null;
        private static string _partyId   = null;
        private static int    _appearedDay = -1;
        private static bool   _victoryResolved = false;
        private static bool   _defeatResolved  = false;

        public static void ResetForNewGame()
        {
            _heroId = null; _partyId = null; _appearedDay = -1;
            _victoryResolved = false; _defeatResolved = false;
        }

        public static void SyncData(IDataStore store)
        {
            try { store.SyncData("APOC_LordHeroId",    ref _heroId); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("APOC_LordPartyId",   ref _partyId); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("APOC_LordAppearedDay", ref _appearedDay); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("APOC_LordVictory",   ref _victoryResolved); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { store.SyncData("APOC_LordDefeat",    ref _defeatResolved); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Query ────────────────────────────────────────────────────────────────
        public static bool HasAppeared => !string.IsNullOrEmpty(_heroId);
        public static bool VictoryResolved => _victoryResolved;
        public static bool DefeatResolved  => _defeatResolved;
        public static int  AppearedDay     => _appearedDay;

        public static Hero CurrentHero()
        {
            if (string.IsNullOrEmpty(_heroId)) return null;
            try { return Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _heroId)
                       ?? Hero.DeadOrDisabledHeroes.FirstOrDefault(h => h.StringId == _heroId); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        public static bool IsTrackedHero(Hero hero) => hero != null && hero.StringId == _heroId;

        public static MobileParty CurrentParty()
        {
            if (string.IsNullOrEmpty(_partyId)) return null;
            try { return MobileParty.All.FirstOrDefault(p => p != null && p.StringId == _partyId); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        public static Kingdom CurrentKingdom()
        {
            try { return Kingdom.All.FirstOrDefault(k => k != null && k.StringId == KingdomId && !k.IsEliminated); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        public static bool IsAliveAndUnresolved()
        {
            if (!HasAppeared || _victoryResolved || _defeatResolved) return false;
            Hero h = CurrentHero();
            return h != null && h.IsAlive;
        }

        // ── Appearance ───────────────────────────────────────────────────────────
        // Called once by ApocalypseCampaignBehavior when the day-1000+ weekly
        // roll succeeds. Founds his kingdom, seizes one settlement outright
        // (mirrors CampaignMapEvents' "Ashen Tide" — a random castle falls
        // instantly), and raises his own host around him.
        public static bool TryAppear()
        {
            if (HasAppeared) return false;
            try
            {
                Settlement target = Settlement.All
                    .Where(s => s != null && s.IsCastle && s.Town != null && !s.IsUnderSiege
                             && s.OwnerClan != Clan.PlayerClan)
                    .OrderBy(_ => _rng.Next())
                    .FirstOrDefault();
                if (target == null) return false;

                CharacterObject template = MBObjectManager.Instance.GetObject<CharacterObject>(LordTemplateId);
                if (template == null) return false;

                Hero lord = HeroCreator.CreateSpecialHero(template, target, null, null, 45);
                if (lord == null) return false;

                string name = Names[_rng.Next(Names.Length)];
                try { lord.SetName(new TextObject(name), new TextObject(name)); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                // CreateSettlementRebelClan is the same vanilla path a settlement
                // rebellion uses — it mints the clan AROUND the hero taking the
                // settlement and (per vanilla rebellion behaviour) transfers
                // ownership as part of that, so this must run BEFORE any
                // ChangeOwnerOfSettlementAction call (which otherwise has no
                // clan yet to assign the settlement to).
                Clan clan;
                try { clan = Clan.CreateSettlementRebelClan(target, lord, 0); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
                if (clan == null) return false;
                try { clan.SetLeader(lord); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                // Defensive re-assert now that the hero has a clan — a no-op if
                // CreateSettlementRebelClan already transferred ownership.
                try { ChangeOwnerOfSettlementAction.ApplyByDefault(lord, target); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { if (target.Town != null) { target.Town.Loyalty = 100f; target.Town.Security = 100f; } }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                Kingdom kingdom = Kingdom.CreateKingdom(KingdomId);
                if (kingdom == null) return false;
                try
                {
                    kingdom.InitializeKingdom(
                        new TextObject("The Devouring Host"),
                        new TextObject("The Devouring Host"),
                        clan.Culture ?? target.Culture,
                        Banner.CreateRandomBanner(),
                        0xFF120608,   // near-black, blood undertone
                        0xFF4A0B0B,   // deep ember red
                        target,
                        new TextObject("What the Long Night was always building toward, given a name and a will."),
                        new TextObject("The Devouring Host"),
                        new TextObject("The Demon Lord"));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }

                try { ChangeKingdomAction.ApplyByCreateKingdom(clan, kingdom, false); }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                MobileParty party = null;
                try
                {
                    party = LordPartyComponent.CreateLordParty(
                        "demon_lord_host_" + _rng.Next(999999).ToString("D6"),
                        lord, new CampaignVec2(target.GetPosition2D, true), 1f, target, lord);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (party != null)
                {
                    try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    AddHostBodies(party, ApocalypseMath.DemonLordHostInitialSize);
                    try { party.Party.SetCustomName(new TextObject($"{name}'s Host")); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                _heroId = lord.StringId;
                _partyId = party?.StringId;
                _appearedDay = (int)CampaignTime.Now.ToDays;

                Announce(
                    $"{name} has come. The demons of Calradia bend to a single, waking will — and {target.Name} " +
                    "is already lost.",
                    new Color(0.55f, 0.05f, 0.05f));

                return true;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
        }

        // Bulk-adds `count` demon bodies to the host's roster, weighted toward
        // the same tier mix as an ordinary night-tide party (DemonMath.RollTier)
        // so his host reads as an army of demons, not a single unit.
        private static void AddHostBodies(MobileParty party, int count)
        {
            for (int i = 0; i < count; i++)
            {
                DemonMath.DemonTier tier = DemonMath.RollTier(_rng);
                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId);
                if (troop == null) continue;
                try { party.MemberRoster.AddToCounts(troop, 1); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Weekly upkeep — his growing host, and his conquest ───────────────────
        public static void WeeklyTick()
        {
            if (!IsAliveAndUnresolved()) return;
            try
            {
                MobileParty party = CurrentParty();
                if (party == null || !party.IsActive) return;

                int weeks = Math.Max(0, ((int)CampaignTime.Now.ToDays - _appearedDay) / 7);
                int wanted = ApocalypseMath.DemonLordHostSizeAfterWeeks(weeks);
                int have = party.MemberRoster?.TotalManCount ?? 0;
                if (have < wanted) AddHostBodies(party, wanted - have);

                try { ReassertPermanentWar(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                if (party.MapEvent == null && party.BesiegedSettlement == null)
                {
                    Settlement objective = Settlement.All
                        .Where(s => s != null && (s.IsTown || s.IsCastle) && !s.IsUnderSiege
                                 && s.MapFaction?.StringId != KingdomId)
                        .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                        .FirstOrDefault();
                    if (objective != null)
                    {
                        try { party.SetMoveBesiegeSettlement(objective, MobileParty.NavigationType.Default); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ReassertPermanentWar()
        {
            Kingdom kingdom = CurrentKingdom();
            if (kingdom == null) return;
            foreach (Kingdom other in Kingdom.All.ToList())
            {
                if (other == null || other == kingdom || other.IsEliminated) continue;
                if (kingdom.IsAtWarWith(other)) continue;
                try { DeclareWarAction.ApplyByDefault(kingdom, other); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Resolution flags (fired from ApocalypseCampaignBehavior.Resolution.cs) ─
        public static void MarkVictoryResolved() { _victoryResolved = true; }
        public static void MarkDefeatResolved()  { _defeatResolved  = true; }

        public static int SettlementsHeldByLord()
        {
            try { return Settlement.All.Count(s => s != null && s.MapFaction?.StringId == KingdomId); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }

        private static void Announce(string text, Color color)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text, color)); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
