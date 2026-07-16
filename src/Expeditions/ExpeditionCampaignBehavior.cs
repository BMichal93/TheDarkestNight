// =============================================================================
// THE DARKEST NIGHT — Expeditions/ExpeditionCampaignBehavior.cs
//
// "Charter an Expedition" (the Antiquarian Charter) — a background,
// caravan-like ruin-expedition system available from the town menu in Revyl,
// the mercenary free-camp (CityStateSystem.IsCampSettlement — see
// CityStates/CityStateSystem.cs's Revyl special-case, "The Camp"). Originally
// a Legion ("empire_w") offer; moved wholesale to The Camp along with a
// reflavour and an influence-to-gold cost change (ExpeditionMath.GoldCost) —
// see ExpeditionCampaignBehavior.Menus.cs for the gating and menu flow. Not
// gated on MageKnowledge.IsMage — expeditions are mundane business, open to
// any player character.
//
// State/tick machinery lives here; the town-menu selection flow (leader →
// team → destination → confirm) lives in the .Menus.cs partial. Numeric
// tuning is pure in ExpeditionMath.cs.
//
// Only ONE expedition may be active at a time (mirrors the Schemes system's
// single-slot-per-player pattern). The 10-leader pool persists across the
// whole campaign under "EXP_" keys; an old save with none of these keys
// present simply gets a freshly generated pool on next load (additive-only —
// no new MANDATORY key).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class ExpeditionCampaignBehavior : CampaignBehaviorBase
    {
        // ── Leader pool (data-only records — never real Hero objects) ──────────
        private class LeaderRecord
        {
            public string Id;
            public string Name;
            public ExpeditionLeaderSpecialty Specialty;
            public bool Proven;
        }

        private const int LeaderPoolSize = 10;

        private static readonly List<LeaderRecord> _leaderPool = new List<LeaderRecord>();
        private static readonly Random _rng = new Random();

        // ── Active expedition (single slot) ─────────────────────────────────────
        private static bool   _active;
        private static string _activeLeaderId;
        private static string _activeVillage;
        private static int    _activeTeam;     // (int)ExpeditionTeamType
        private static int    _activeDaysLeft;

        internal static bool IsActive => _active;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterExpeditionMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { TickExpedition(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        public override void SyncData(IDataStore store)
        {
            try
            {
                var ids   = _leaderPool.Select(l => l.Id).ToList();
                var names = _leaderPool.Select(l => l.Name).ToList();
                var specs = _leaderPool.Select(l => (int)l.Specialty).ToList();
                var proven = _leaderPool.Select(l => l.Proven).ToList();

                store.SyncData("EXP_LeaderIds",    ref ids);
                store.SyncData("EXP_LeaderNames",  ref names);
                store.SyncData("EXP_LeaderSpecs",  ref specs);
                store.SyncData("EXP_LeaderProven", ref proven);

                store.SyncData("EXP_Active",     ref _active);
                store.SyncData("EXP_LeaderId",   ref _activeLeaderId);
                store.SyncData("EXP_Village",    ref _activeVillage);
                store.SyncData("EXP_Team",       ref _activeTeam);
                store.SyncData("EXP_DaysLeft",   ref _activeDaysLeft);

                if (ids != null && names != null && specs != null && proven != null
                    && ids.Count > 0
                    && ids.Count == names.Count && ids.Count == specs.Count && ids.Count == proven.Count)
                {
                    _leaderPool.Clear();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        _leaderPool.Add(new LeaderRecord
                        {
                            Id        = ids[i],
                            Name      = names[i],
                            Specialty = (ExpeditionLeaderSpecialty)specs[i],
                            Proven    = proven[i],
                        });
                    }
                }
                else if (_leaderPool.Count == 0)
                {
                    GenerateFreshPool();
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        public static void ResetForNewGame()
        {
            _active = false;
            _activeLeaderId = null;
            _activeVillage  = null;
            _activeTeam     = 0;
            _activeDaysLeft = 0;
            GenerateFreshPool();
        }

        // ── Leader pool generation ───────────────────────────────────────────────
        private static readonly string[] _leaderFirstNames =
        {
            "Ilya", "Bron", "Sera", "Petra", "Ardan", "Mira", "Doran", "Yeva",
            "Casimir", "Rurik", "Anzhela", "Torvald", "Ysolde", "Marek", "Odalys", "Guntram",
        };

        private static readonly string[] _leaderSurnames =
        {
            "Vantor", "Duskholm", "Ashcroft", "Greywick", "Blackmire", "Northgale",
            "Cindermoor", "Hollowbrook", "Wraithstone", "Emberfall", "Draeburn", "Fellhallow",
        };

        private static string GenerateLeaderName()
        {
            string first = _leaderFirstNames[_rng.Next(_leaderFirstNames.Length)];
            string last  = _leaderSurnames[_rng.Next(_leaderSurnames.Length)];
            return $"{first} {last}";
        }

        private static LeaderRecord GenerateLeader(string id)
        {
            var specialties = (ExpeditionLeaderSpecialty[])Enum.GetValues(typeof(ExpeditionLeaderSpecialty));
            return new LeaderRecord
            {
                Id        = id,
                Name      = GenerateLeaderName(),
                Specialty = specialties[_rng.Next(specialties.Length)],
                Proven    = false,
            };
        }

        private static void GenerateFreshPool()
        {
            _leaderPool.Clear();
            for (int i = 0; i < LeaderPoolSize; i++)
                _leaderPool.Add(GenerateLeader($"EXP_L{i}"));
        }

        private static LeaderRecord FindLeader(string id)
            => string.IsNullOrEmpty(id) ? null : _leaderPool.FirstOrDefault(l => l.Id == id);

        private static void MarkLeaderProven(string id)
        {
            var leader = FindLeader(id);
            if (leader != null) leader.Proven = true;
        }

        // A leader lost to a failed expedition keeps their slot Id but is replaced
        // wholesale — a fresh recruit steps into the ledger.
        private static void ReplaceLeader(string id)
        {
            int idx = _leaderPool.FindIndex(l => l.Id == id);
            if (idx < 0) return;
            _leaderPool[idx] = GenerateLeader(id);
        }

        // ── Daily resolution ──────────────────────────────────────────────────
        private static void TickExpedition()
        {
            if (!_active) return;
            if (Campaign.Current == null || Hero.MainHero == null) return;

            _activeDaysLeft--;
            if (_activeDaysLeft > 0) return;

            ResolveExpedition();
        }

        private static void ResolveExpedition()
        {
            var def     = AshenRuinDefs.All.FirstOrDefault(r => r.VillageName == _activeVillage);
            var leader  = FindLeader(_activeLeaderId);
            var team    = (ExpeditionTeamType)_activeTeam;
            RuinTier tier = def?.Tier ?? RuinTier.Standard;
            var specialty = leader?.Specialty ?? ExpeditionLeaderSpecialty.ZealousAntiquarian;
            bool proven   = leader?.Proven ?? false;
            string ruinName = def?.RuinName ?? "the old ruin";
            string leaderName = leader?.Name ?? "the charter's leader";

            int chance = ExpeditionMath.SuccessChance(tier, specialty, team, proven);
            int roll   = _rng.Next(100);
            bool success = roll < chance;

            string title, body;
            Color color;

            if (success)
            {
                int gold = ExpeditionMath.SuccessGold(tier, specialty, _rng.Next(100));
                float abscond = specialty == ExpeditionLeaderSpecialty.TombRobber
                    ? ExpeditionMath.TombRobberAbscondFraction(_rng.Next(100)) : 0f;
                int absconded = (int)(gold * abscond);
                int keptGold  = gold - absconded;
                float renown  = ExpeditionMath.SuccessRenown(tier);

                bool grantsCrystal = ExpeditionMath.RollGrantsCrystal(_rng.Next(100), tier);
                bool grantsRelic   = !grantsCrystal && ExpeditionMath.RollGrantsRelic(_rng.Next(100), tier);

                try { Hero.MainHero.ChangeHeroGold(keptGold); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                try { ClanRenown.Gain(Hero.MainHero.Clan, renown); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                if (grantsCrystal) GrantRandomCrystal();
                else if (grantsRelic) GrantRandomRelic();

                MarkLeaderProven(_activeLeaderId);

                // Put the ruin on cooldown exactly as if an NPC lord had cleared it —
                // via the public hook, never touching AshenRuinSystem's private state.
                try { AshenRuinSystem.MarkClearedByExpedition(_activeVillage); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

                string absconLine = absconded > 0
                    ? $" {leaderName} quietly pockets {absconded} denars before the tally is even written down."
                    : "";
                string itemLine = grantsCrystal ? " A crystal, still humming, comes back with the ledgers."
                                 : grantsRelic  ? " Something older than a crystal comes back wrapped in cloth, unlabeled."
                                 : "";

                title = "The Charter Returns";
                body  = $"{leaderName} brings the charter back from {ruinName}, {keptGold} denars richer and the Camp's "
                       + $"ledger a little heavier. (+{(int)renown} renown){itemLine}{absconLine}";
                color = new Color(0.75f, 0.65f, 0.35f);

                if (specialty == ExpeditionLeaderSpecialty.ZealousAntiquarian
                    && ExpeditionMath.RollAshenTakesNote(_rng.Next(100)))
                {
                    body += " Something cold and distant takes a passing interest in the name on the charter.";
                }
            }
            else
            {
                bool leaderLost = ExpeditionMath.LeaderLostOnFailure(roll, tier, team);
                if (leaderLost)
                {
                    ReplaceLeader(_activeLeaderId);
                    title = "The Charter Is Lost";
                    body  = $"No word returns from {ruinName} but the charter itself, torn and stained. "
                          + $"{leaderName} does not come back. The influence spent is gone with them.";
                }
                else
                {
                    title = "The Charter Fails";
                    body  = $"{leaderName} returns from {ruinName} empty-handed and shaken. The dark held onto what it had. "
                          + "The influence spent buys nothing this time.";
                }
                color = new Color(0.55f, 0.2f, 0.2f);
            }

            _active = false;
            _activeLeaderId = null;
            _activeVillage  = null;
            _activeTeam     = 0;
            _activeDaysLeft = 0;

            InformationManager.DisplayMessage(new InformationMessage(body, color));

            // Never clobber another system's queued blocking popup (behaviour.md's
            // single-slot rule) — the log line above already reached the player
            // either way, so skipping the inquiry here loses nothing critical.
            if (MageKnowledge._deferredInquiry != null) return;
            string capturedTitle = title, capturedBody = body;
            MageKnowledge._deferredInquiry = () =>
            {
                try
                {
                    InformationManager.ShowInquiry(new InquiryData(
                        capturedTitle, capturedBody, true, false, "Acknowledge", "",
                        () => { }, null), true);
                }
                catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            };
        }

        private static void GrantRandomCrystal()
        {
            try
            {
                var defs = CrystalCatalog.All;
                var def  = defs[_rng.Next(defs.Count)];
                var item = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.ItemRoster;
                if (item != null && roster != null) roster.AddToCounts(item, 1);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void GrantRandomRelic()
        {
            try
            {
                var defs = RelicCatalog.All;
                if (defs == null || defs.Count == 0) { GrantRandomCrystal(); return; }
                var def  = defs[_rng.Next(defs.Count)];
                var item = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = TaleWorlds.CampaignSystem.Party.MobileParty.MainParty?.ItemRoster;
                if (item != null && roster != null) roster.AddToCounts(item, 1);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
