// =============================================================================
// THE DARKEST NIGHT — Demons/DemonSpawnCampaignBehavior.cs
//
// THE NIGHT TIDE. At dusk the ground gives up demon warbands — near
// settlements, along the roads, out in the wilderness — aggressive and
// numerous. At dawn every one of them sinks back below; nothing that rose in
// the night is still standing when the sun is up.
//
// ── Design decision: no bespoke Kingdom ─────────────────────────────────────
// Requirement 3 asks for "a demon campaign faction permanently at war with
// everyone, no diplomacy possible... demons hold no settlements," modeled on
// AshenDiplomacyModel. AshenCitySystem's Kingdom, though, is built around
// owning a home settlement (Kingdom.InitializeKingdom takes a mandatory
// initialHomeSettlement) and a ruling Clan/Hero — machinery demons have no
// use for and that risks real engine trouble if forced onto a settlement-less,
// lord-less faction (untested null-home-settlement path, Kingdom-leader UI
// assumptions, etc.) — exactly the "clever fragile" trap behaviour.md warns
// against. Every existing summon in this codebase that needs a faction to
// belong to (the wild elemental bands in ElementalWildsBehavior, the Great
// Awakening's Great Other in GreatOtherParty, the Ashen ambush spawns in
// CampaignMapEvents) instead hosts its party under an existing
// Clan.BanditFactions clan — which is ALREADY permanently at war with every
// real kingdom and cannot be offered or receive peace in vanilla Bannerlord,
// satisfying requirement 7f for free. Demon parties follow that same proven
// pattern. "Which MobileParty is actually a demon party" is our own concern,
// tracked here (IsDemonParty) exactly like ElementalWildsBehavior tracks
// _bandKind — never by faction identity. A later phase's Demon Lord (Phase 11)
// is the point where the horde is meant to bind into a single, real named
// faction; Phase 1 deliberately stays a leaderless, faction-less tide.
//
// ── Persistence ──────────────────────────────────────────────────────────
// Only what must survive a save/load is persisted, under DEMON_* keys
// (mirrors ElementalWildsBehavior's ELEM_* keys): which parties are ours, the
// body count each was born with (for nightfall replenishment), and the
// environment variant each was born under (for the OnAgentBuild health
// scaling — see DemonBattleBehavior.PendingVariant). The night/day cadence
// itself is NOT persisted as an edge-triggered flag — only "last day we
// processed nightfall/dawn," which is naturally reload-safe (see OnHourlyTick).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public class DemonSpawnCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        // Party StringId → the body count it was spawned/last replenished to,
        // and the environment variant it rose under.
        private static readonly Dictionary<string, int> _partyOriginalSize = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _partyVariant      = new Dictionary<string, int>();

        private static int _lastNightfallDay = -1;
        private static int _lastDawnDay      = -1;

        public static void ResetForNewGame()
        {
            _partyOriginalSize.Clear();
            _partyVariant.Clear();
            _lastNightfallDay = -1;
            _lastDawnDay      = -1;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            var ids      = _partyOriginalSize.Keys.ToList();
            var sizes    = _partyOriginalSize.Values.ToList();
            var variants = ids.Select(id => _partyVariant.TryGetValue(id, out int v) ? v : 0).ToList();
            dataStore.SyncData("DEMON_PartyIds",      ref ids);
            dataStore.SyncData("DEMON_PartySizes",    ref sizes);
            dataStore.SyncData("DEMON_PartyVariants", ref variants);
            dataStore.SyncData("DEMON_LastNightfallDay", ref _lastNightfallDay);
            dataStore.SyncData("DEMON_LastDawnDay",      ref _lastDawnDay);

            if (dataStore.IsLoading)
            {
                _partyOriginalSize.Clear();
                _partyVariant.Clear();
                if (ids != null && sizes != null)
                    for (int i = 0; i < ids.Count && i < sizes.Count; i++)
                    {
                        _partyOriginalSize[ids[i]] = sizes[i];
                        _partyVariant[ids[i]]      = (variants != null && i < variants.Count) ? variants[i] : 0;
                    }
            }
        }

        // ── Query ────────────────────────────────────────────────────────────────
        public static bool IsDemonParty(MobileParty party)
            => party != null && _partyOriginalSize.ContainsKey(party.StringId);

        // ── Hourly cadence ───────────────────────────────────────────────────────
        private void OnHourlyTick()
        {
            try
            {
                if (Campaign.Current == null) return;
                CrashDiagnostics.MarkHourly("Demons.PruneDead enter");
                PruneDead();

                float hour = CurrentHourOfDay();
                int   day  = CurrentDay();
                bool  night = DemonMath.IsNightHour(hour);

                if (night && _lastNightfallDay != day)
                {
                    _lastNightfallDay = day;
                    // Party creation on the campaign map is the likeliest native-AV
                    // site in the first-hours window, so nightfall is breadcrumbed
                    // step by step — see CrashDiagnostics.
                    CrashDiagnostics.MarkTick("Demons.ReplenishSurvivors enter");
                    try { ReplenishSurvivors(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    CrashDiagnostics.MarkTick("Demons.SpawnNightTide enter");
                    try { SpawnNightTide();     } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    CrashDiagnostics.MarkTick("Demons.RollSettlementAssaults enter");
                    try { RollSettlementAssaults(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    CrashDiagnostics.MarkTick("Demons.nightfall exit");
                }
                else if (!night && _lastDawnDay != day)
                {
                    _lastDawnDay = day;
                    try { DespawnNightTide(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                if (night)
                {
                    CrashDiagnostics.MarkHourly("Demons.DirectDemonParties enter");
                    try { DirectDemonParties(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }

                // Requirement 7f/7g: no negotiation, no release, no survivors of
                // captivity — checked every hour so it is genuinely "on the spot."
                try { ResolveCaptives(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static float CurrentHourOfDay()
        {
            try { return (float)CampaignTime.Now.CurrentHourInDay; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 12f; }
        }

        private static int CurrentDay()
        {
            try { return (int)CampaignTime.Now.ToDays; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return 0; }
        }

        // ── Nightfall: the tide rises ────────────────────────────────────────────
        private void SpawnNightTide()
        {
            int count = DemonMath.NightSpawnPartyCount(_rng, _partyOriginalSize.Count);
            // The Veil's turn swells or thins the tide: more demons cross while the
            // Veil is thin (the Thinning), far fewer while it holds thick (the
            // Warding). See VeilMath. Re-clamped to the living-party budget so a
            // 1.2× surge can never blow past MaxLivingDemonParties.
            VeilPhase phase = VeilCampaignBehavior.CurrentPhase();
            count = VeilMath.ApplySpawnMultiplier(count, phase);
            int room = Math.Max(0, DemonMath.MaxLivingDemonParties - _partyOriginalSize.Count);
            if (count > room) count = room;
            for (int i = 0; i < count; i++)
                SpawnParty(DemonMath.RollSpawnLocation(_rng));
        }

        private MobileParty SpawnParty(DemonMath.SpawnLocationKind locationKind)
        {
            try
            {
                Vec2 anchor;
                string biomeHint;
                if (!TryPickAnchor(locationKind, out anchor, out biomeHint)) return null;

                Clan banditClan = Clan.BanditFactions.FirstOrDefault(c => c != null && !c.IsEliminated);
                if (banditClan == null) return null;
                var pt = banditClan.DefaultPartyTemplate;
                if (pt == null) return null;

                Hideout hideout = FindHideoutFallback(banditClan, anchor);
                if (hideout == null) return null;

                const float scatter = 2.5f;
                Vec2 spawnPos = anchor + new Vec2(
                    (float)(_rng.NextDouble() - 0.5) * scatter * 2f,
                    (float)(_rng.NextDouble() - 0.5) * scatter * 2f);
                var cvec = new CampaignVec2(spawnPos, true);

                string partyId = "demon_tide_" + _rng.Next(999999).ToString("D6");
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, banditClan, hideout, false, pt, cvec);
                if (party == null) return null;

                try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                int bodies = DemonMath.PartyBodyCount(_rng);
                AddDemonBodies(party, bodies);

                DemonMath.EnvironmentVariant variant = DemonMath.VariantForCulture(biomeHint);
                try { party.Party.SetCustomName(new TextObject(NightTideName())); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                _partyOriginalSize[party.StringId] = bodies;
                _partyVariant[party.StringId]      = (int)variant;

                return party;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // Adds `count` bodies to `party`'s roster, each an independently-rolled
        // demon tier (DemonMath.RollTier), falling back through the same troop
        // ids DemonFactory does if a particular tier failed to load.
        private static void AddDemonBodies(MobileParty party, int count)
        {
            for (int i = 0; i < count; i++)
            {
                DemonMath.DemonTier tier = DemonMath.RollTier(_rng);
                CharacterObject troop =
                    MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.TroopIdFor(tier))
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>(DemonCatalog.FiendTroopId)
                 ?? MBObjectManager.Instance.GetObject<CharacterObject>("mountain_bandit");
                if (troop == null) continue;
                try { party.MemberRoster.AddToCounts(troop, 1); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        private bool TryPickAnchor(DemonMath.SpawnLocationKind kind, out Vec2 anchor, out string biomeHint)
        {
            anchor = default; biomeHint = "";
            try
            {
                switch (kind)
                {
                    case DemonMath.SpawnLocationKind.NearSettlement:
                    case DemonMath.SpawnLocationKind.Road:
                    {
                        var towns = Settlement.All.Where(s => s != null && (s.IsTown || s.IsCastle)).ToList();
                        if (towns.Count == 0) return false;
                        Settlement s = towns[_rng.Next(towns.Count)];
                        anchor = s.GetPosition2D;
                        biomeHint = CultureHint(s);
                        // A "road" spawn stands off further from the walls than a
                        // "near settlement" one, but both key off the same town.
                        float offset = kind == DemonMath.SpawnLocationKind.Road ? 6f : 1.5f;
                        anchor += new Vec2((float)(_rng.NextDouble() - 0.5) * offset,
                                           (float)(_rng.NextDouble() - 0.5) * offset);
                        return true;
                    }
                    default: // Wilderness — breed at a hideout's remote land, same as the Kindled.
                    {
                        var hideouts = Settlement.All.Where(s => s?.Hideout != null).ToList();
                        if (hideouts.Count == 0) return false;
                        Settlement s = hideouts[_rng.Next(hideouts.Count)];
                        anchor = s.GetPosition2D;
                        biomeHint = CultureHint(s);
                        return true;
                    }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return false; }
        }

        // The same 3-level fallback CampaignMapEvents.Helpers.SpawnAshenSpawnParty
        // uses (a mechanic already proven working in this codebase): the bandit
        // clan's own hideout, then the nearest hideout to the anchor, then ANY
        // hideout in the world. A null hideout crashes the post-battle loot
        // screen, so CreateBanditParty must never receive one — but bailing out
        // after only the first two levels (the demon spawn's old behaviour) is
        // one silent-failure step short of that proven path.
        private static Hideout FindHideoutFallback(Clan banditClan, Vec2 anchor)
        {
            try
            {
                Settlement hs = banditClan.Settlements.FirstOrDefault(s => s?.Hideout != null);
                if (hs == null)
                    hs = Settlement.All.Where(s => s?.Hideout != null)
                        .OrderBy(s => (s.GetPosition2D - anchor).LengthSquared).FirstOrDefault();
                if (hs == null)
                    hs = Settlement.All.FirstOrDefault(s => s?.Hideout != null);
                return hs?.Hideout;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        private static string CultureHint(Settlement s)
        {
            try { return s?.Culture?.StringId ?? ""; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return ""; }
        }

        private static string NightTideName()
        {
            string[] names =
            {
                "The Night Tide", "A Wound in the Dark", "The Hungering Host",
                "What Crawled Up", "The Grey Marching", "A Rent in the World",
            };
            return names[_rng.Next(names.Length)];
        }

        // ── Public spawn-near-position hook (Phase 9: Ruins nightfall risk) ──────
        // Every other spawn path in this file picks its own anchor
        // (TryPickAnchor / SpawnNightTide); this one exists for callers that
        // already know exactly where they want a demon party to appear right
        // now — specifically RuinsExplorationSystem.WaitMenu, when a chamber
        // wait crosses into nightfall while the player is standing inside a
        // ruin. Always exactly one party, no location-kind roll, and
        // deliberately NOT gated on MaxLivingDemonParties — a single extra
        // party never meaningfully risks the campaign-wide budget the way an
        // unbounded spawn loop would.
        public static MobileParty SpawnAmbushNear(Vec2 anchor, string biomeHint = "")
        {
            try
            {
                Clan banditClan = Clan.BanditFactions.FirstOrDefault(c => c != null && !c.IsEliminated);
                if (banditClan == null) return null;
                var pt = banditClan.DefaultPartyTemplate;
                if (pt == null) return null;

                Hideout hideout = FindHideoutFallback(banditClan, anchor);
                if (hideout == null) return null;

                const float scatter = 1.0f;
                Vec2 spawnPos = anchor + new Vec2(
                    (float)(_rng.NextDouble() - 0.5) * scatter * 2f,
                    (float)(_rng.NextDouble() - 0.5) * scatter * 2f);
                var cvec = new CampaignVec2(spawnPos, true);

                string partyId = "demon_ambush_" + _rng.Next(999999).ToString("D6");
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, banditClan, hideout, false, pt, cvec);
                if (party == null) return null;

                try { party.MemberRoster.Clear(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                int bodies = DemonMath.PartyBodyCount(_rng);
                AddDemonBodies(party, bodies);

                DemonMath.EnvironmentVariant variant = DemonMath.VariantForCulture(biomeHint);
                try { party.Party.SetCustomName(new TextObject(NightTideName())); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                _partyOriginalSize[party.StringId] = bodies;
                _partyVariant[party.StringId]      = (int)variant;

                return party;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // ── Nightfall: surviving parties regain some of their numbers ───────────
        private void ReplenishSurvivors()
        {
            foreach (var id in _partyOriginalSize.Keys.ToList())
            {
                try
                {
                    MobileParty party = MobileParty.All.FirstOrDefault(p => p != null && p.StringId == id);
                    if (party == null || !party.IsActive) continue;
                    int original = _partyOriginalSize[id];
                    int current  = party.MemberRoster?.TotalManCount ?? 0;
                    int amount   = DemonMath.ReplenishAmount(current, original);
                    if (amount > 0) AddDemonBodies(party, amount);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Nightfall: a rare few turn to besiege a town or castle ──────────────
        private void RollSettlementAssaults()
        {
            foreach (var id in _partyOriginalSize.Keys.ToList())
            {
                try
                {
                    if (!DemonMath.RollSettlementAssault(_rng.NextDouble())) continue;
                    MobileParty party = MobileParty.All.FirstOrDefault(p => p != null && p.StringId == id);
                    if (party == null || !party.IsActive) continue;

                    Settlement target = Settlement.All
                        .Where(s => s != null && (s.IsTown || s.IsCastle))
                        .OrderBy(s => (s.GetPosition2D - party.GetPosition2D).LengthSquared)
                        .FirstOrDefault();
                    if (target == null) continue;

                    try { party.SetMoveRaidSettlement(target, MobileParty.NavigationType.Default, false); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                    Announce($"The dark surges against {target.Name} tonight.", new Color(0.65f, 0.15f, 0.12f));
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Overnight: demons hunt — never manoeuvre, never withdraw ────────────
        // Requirement 7a/7b/7c on the map side: we never issue a retreat/flee
        // order ourselves, and every hour we re-point any party not already
        // mid-assault or mid-battle at the nearest living target. A party the
        // vanilla AI may have nudged toward caution gets overwritten again
        // within the hour, so nothing "escapes" toward safety for long.
        private const float EngageSearchRadius = 40f;

        // Packs within this radius of each other band together — the smaller
        // escorts the larger rather than each hunting alone (issue 16).
        private const float BandRadius = 8f;

        private void DirectDemonParties()
        {
            var living = _partyOriginalSize.Keys
                .Select(id => MobileParty.All.FirstOrDefault(p => p != null && p.StringId == id))
                .Where(p => p != null && p.IsActive)
                .ToList();

            foreach (MobileParty party in living)
            {
                try
                {
                    if (party.MapEvent != null) continue;           // already fighting
                    if (party.BesiegedSettlement != null) continue; // already assaulting

                    // Pin the party to pure aggression on the campaign map every
                    // hour: max attack initiative, zero avoid initiative, and never
                    // shy from the main party — a vanilla AI nudge toward caution
                    // gets overwritten again within the hour (requirement 7a/7b/7c).
                    try { party.Ai.SetInitiative(1f, 0f, 6f); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { party.Ai.SetDoNotAttackMainParty(0); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                    // Faction D (the Bloodbound) and Faction C (the Forest
                    // Widows) can each buy a few days of being left alone — a
                    // party currently under any bought-off window is simply
                    // never picked as prey (see BloodboundCampaignBehavior.
                    // IsPartyIgnored / GrantIgnore and ForestWidowsCampaignBehavior.
                    // IsPartyIgnored / GrantIgnoreDays — two parallel,
                    // faction-owned trackers consulted at the same choke
                    // point). Faction H (the Chosen) has no such tracker — the
                    // Chosen fend for themselves against the Night Tide, same
                    // as everyone else. Once the Forest Widows' "Final Peace"
                    // dark pact is sealed (FactionQuests/ForestWidows/
                    // ForestWidowsQuestCampaignBehavior.Resolution.cs), their
                    // parties are excluded permanently, not just for a bought
                    // window — see IsDarkPactParty.
                    MobileParty prey = MobileParty.All
                        .Where(p => p != null && p.IsActive && p != party
                                 && !IsDemonParty(p) && p.MapEvent == null
                                 && (p.MemberRoster?.TotalManCount ?? 0) > 0
                                 && !BloodboundCampaignBehavior.IsPartyIgnored(p)
                                 && !ForestWidowsCampaignBehavior.IsPartyIgnored(p)
                                 && !ForestWidowsQuestCampaignBehavior.IsDarkPactParty(p))
                        .OrderBy(p => (p.GetPosition2D - party.GetPosition2D).LengthSquared)
                        .FirstOrDefault(p => (p.GetPosition2D - party.GetPosition2D).Length <= EngageSearchRadius);

                    if (prey != null)
                    {
                        try { party.SetMoveEngageParty(prey, MobileParty.NavigationType.Default); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        continue;
                    }

                    // No prey nearby — band together instead of hunting alone:
                    // the smaller of two nearby packs escorts the larger one.
                    MobileParty packLeader = living
                        .Where(p => p != party
                                 && (p.MemberRoster?.TotalManCount ?? 0) >= (party.MemberRoster?.TotalManCount ?? 0)
                                 && (p.GetPosition2D - party.GetPosition2D).Length <= BandRadius)
                        .OrderByDescending(p => p.MemberRoster?.TotalManCount ?? 0)
                        .FirstOrDefault();

                    if (packLeader != null)
                        try { party.SetMoveEscortParty(packLeader, MobileParty.NavigationType.Default, false); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Dawn: the tide sinks back below ──────────────────────────────────────
        private void DespawnNightTide()
        {
            foreach (var id in _partyOriginalSize.Keys.ToList())
            {
                try
                {
                    MobileParty party = MobileParty.All.FirstOrDefault(p => p != null && p.StringId == id);
                    if (party != null && party.IsActive)
                        try { DestroyPartyAction.Apply(party.Party, null); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                _partyOriginalSize.Remove(id);
                _partyVariant.Remove(id);
            }
        }

        // ── Requirement 7f/7g: prisoners never survive demon captivity ─────────
        // Every hour: any living hero currently a prisoner of a tracked demon
        // party rolls its escape chance immediately (an "on-the-spot" roll, not
        // a lingering captivity); troops in a demon party's prisoner roster are
        // resolved the same way, in aggregate. At most a few heavy actions per
        // tick to avoid cascading KillCharacterAction calls in one frame.
        private const int MaxExecutionsPerTick = 3;

        private void ResolveCaptives()
        {
            int actions = 0;

            foreach (Hero hero in Hero.AllAliveHeroes.ToList())
            {
                if (actions >= MaxExecutionsPerTick) return;
                try
                {
                    if (!hero.IsPrisoner) continue;
                    MobileParty captor = hero.PartyBelongedToAsPrisoner?.MobileParty;
                    if (captor == null || !IsDemonParty(captor)) continue;

                    bool escapes = DemonMath.RollCaptiveEscapes(_rng.NextDouble(), true);
                    if (escapes)
                    {
                        try { EndCaptivityAction.ApplyByReleasedAfterBattle(hero); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        Announce($"{hero.Name} tore free of the dark before it could finish what it started.", new Color(0.55f, 0.55f, 0.55f));
                    }
                    else
                    {
                        // No demon hero exists to name as the executor — the same
                        // null-executor form AshenCitySystem's own capture prompt
                        // uses for an unnamed death (ApplyByMurder(hero, null, true)).
                        try { KillCharacterAction.ApplyByMurder(hero, null, true); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        Announce($"{hero.Name} did not come back from the dark.", new Color(0.55f, 0.20f, 0.18f));
                    }
                    actions++;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }

            foreach (var id in _partyOriginalSize.Keys.ToList())
            {
                if (actions >= MaxExecutionsPerTick) return;
                try
                {
                    MobileParty party = MobileParty.All.FirstOrDefault(p => p != null && p.StringId == id);
                    var roster = party?.PrisonRoster?.GetTroopRoster()?.ToList();
                    if (roster == null || roster.Count == 0) continue;

                    foreach (var entry in roster)
                    {
                        if (entry.Character == null || entry.Character.IsHero || entry.Number <= 0) continue;
                        // Every troop in the stack rolls at once — those who fail are
                        // executed, those who escape simply vanish from the demons'
                        // count (freed; nothing further tracks them in Phase 1).
                        try { party.PrisonRoster.AddToCounts(entry.Character, -entry.Number); }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                    actions++;
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Battle hookup: mark the coming fight so OnAgentBuild can scale HP
        //    for the involved demon party's environment variant ──────────────
        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (mapEvent == null) return;
                DemonMath.EnvironmentVariant? variant = null;
                foreach (var side in new[] { mapEvent.AttackerSide, mapEvent.DefenderSide })
                {
                    if (side == null) continue;
                    foreach (var p in side.Parties)
                    {
                        MobileParty mp = p?.Party?.MobileParty;
                        if (mp != null && _partyVariant.TryGetValue(mp.StringId, out int v))
                        {
                            variant = (DemonMath.EnvironmentVariant)v;
                            break;
                        }
                    }
                    if (variant != null) break;
                }
                DemonBattleBehavior.PendingVariant = variant;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            try { DemonBattleBehavior.PendingVariant = null; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { RollRelicDrop(mapEvent); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { RollDemonBloodDrop(mapEvent); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Faction D (the Bloodbound) — demon blood from a hunt ────────────────
        // Unlike RollRelicDrop (player-only), this checks EVERY winning party on
        // the winning side — a Bloodbound vassal's own lords earn Demon Blood
        // from their own kills too, not just the player (see BloodboundCulture.
        // IsBloodboundParty / BloodboundCampaignBehavior.GrantDemonBlood). No
        // roll gate on "did we fight demons at all" beyond the same enemySide
        // check RollRelicDrop already does; the yield itself is always 1-3 (never
        // zero) per Requirement D, so there is no drop-chance to roll here.
        private static void RollDemonBloodDrop(MapEvent mapEvent)
        {
            if (mapEvent == null) return;

            foreach (var winningSide in new[] { mapEvent.AttackerSide, mapEvent.DefenderSide })
            {
                try
                {
                    if (winningSide == null) continue;
                    bool isWinningSide = (winningSide == mapEvent.AttackerSide && mapEvent.WinningSide == BattleSideEnum.Attacker)
                                      || (winningSide == mapEvent.DefenderSide && mapEvent.WinningSide == BattleSideEnum.Defender);
                    if (!isWinningSide) continue;

                    var losingSide = winningSide == mapEvent.AttackerSide ? mapEvent.DefenderSide : mapEvent.AttackerSide;
                    bool foughtDemons = losingSide != null && losingSide.Parties.Any(p => IsDemonParty(p?.Party?.MobileParty));
                    if (!foughtDemons) continue;

                    foreach (var p in winningSide.Parties)
                    {
                        try
                        {
                            if (p?.Party == null) continue;
                            BloodboundCampaignBehavior.GrantDemonBlood(p.Party);
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    }
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
        }

        // ── Requirement 19 — relics from demon battles ────────────────────────
        // Small chance, when the PLAYER personally defeats a demon party, that
        // one relic falls out of the wreckage. Chance/index rolls live in
        // RelicMath so they stay pure and tested; this method only supplies
        // the campaign-side "did the player just win a fight against demons"
        // check, mirrored on DragonQuestSystem.OnMapEventEnded's playerWon test.
        private void RollRelicDrop(MapEvent mapEvent)
        {
            if (mapEvent == null) return;

            bool playerAttacker = mapEvent.AttackerSide?.Parties.Any(p => p.Party == PartyBase.MainParty) == true;
            bool playerDefender = mapEvent.DefenderSide?.Parties.Any(p => p.Party == PartyBase.MainParty) == true;
            if (!playerAttacker && !playerDefender) return;

            bool playerWon = (playerAttacker && mapEvent.WinningSide == BattleSideEnum.Attacker)
                          || (playerDefender && mapEvent.WinningSide == BattleSideEnum.Defender);
            if (!playerWon) return;

            var enemySide = playerAttacker ? mapEvent.DefenderSide : mapEvent.AttackerSide;
            if (enemySide == null) return;

            bool foughtDemons = enemySide.Parties.Any(p => IsDemonParty(p?.Party?.MobileParty));
            if (!foughtDemons) return;

            if (!RelicMath.RollRelicDrop(_rng.NextDouble())) return;

            var relics = RelicCatalog.All;
            int idx = RelicMath.PickRelicIndex(_rng.NextDouble(), relics.Count);
            if (idx < 0) return;
            var def = relics[idx];

            try
            {
                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item != null && roster != null)
                {
                    roster.AddToCounts(item, 1);
                    Announce($"Among the wreckage of the Night's dead, something answers a different light — {def.Name}.",
                        new Color(0.75f, 0.65f, 0.35f));
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void Announce(string text, Color color)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(text, color)); }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void PruneDead()
        {
            try
            {
                var alive = new HashSet<string>(MobileParty.All.Select(p => p.StringId));
                var dead = _partyOriginalSize.Keys.Where(id => !alive.Contains(id)).ToList();
                foreach (var id in dead) { _partyOriginalSize.Remove(id); _partyVariant.Remove(id); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
