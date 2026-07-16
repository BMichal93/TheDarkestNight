// =============================================================================
// THE DARKEST NIGHT — Factions/Tower/TowerCampaignBehavior.cs
//
// Faction B — the Tower (formerly Aserai/Duneborn). Owns:
//   • session renaming (kingdom/culture text, via TowerCulture)
//   • town-scoping to Iyakis (via TowerSettlements), daily
//   • the join ritual: joining the Tower opens the way to magic —
//       - the PLAYER, if they lack the spellbook, gets it unlocked for free
//         (no focus-point cost) via SpellbookCampaignBehavior.TryUnlock(free: true)
//       - every OTHER hero in a joining clan (the Tower's lords) who is not
//         already a recognised spellcaster (SpellcasterLords.IsEligible) is
//         granted TowerMath.LordGrantedSpellCount random spells — the closest
//         lord-facing equivalent, since the spellbook unlock flag itself is
//         player-only state (see SpellcasterLords.GrantToHero for the note)
// See TowerCampaignBehavior.Menus.cs for the player-facing teaching/
// transmutation city menus.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace TheDarkestNight
{
    public partial class TowerCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Random _rng = new Random();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore store)
        {
            // Nothing persisted — renaming/town-scoping are idempotent, and the
            // magic grants they trigger persist through the systems that already
            // own that state (SpellbookCampaignBehavior's save data, and
            // SpellcasterLords' seeded-per-session population).
        }

        public static void ResetForNewGame()
        {
            // Renaming/town-scoping are idempotent and re-derive themselves from
            // live campaign state; nothing to reset between sessions.
        }

        // The kingdom-name rename itself runs from the SAME hook every other
        // culture identity uses — AshenCitySystem.EnsureKingdomRenames (fired off
        // OnSessionLaunchedEvent), which now calls TowerCulture.RenameTowerKingdom()
        // in place of the retired RenameDunebornKingdom(). This behavior only
        // owns what is unique to the Tower: the town-scoping, the teaching/
        // transmutation menus, and the join ritual.
        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RegisterTowerMenus(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private void OnDailyTick()
        {
            try { TowerSettlements.ScopeToStartingTowns(); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── The join ritual — magic opens to whoever joins ─────────────────────
        // Fires for every hero in a clan that genuinely joins the Tower (was not
        // already Aserai/Tower before). Covers NPC lords exactly as it covers
        // the player.
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            try
            {
                if (clan == null || newKingdom == null) return;
                if (newKingdom.StringId != TowerCulture.CultureId) return;
                if (oldKingdom != null && oldKingdom.StringId == TowerCulture.CultureId) return; // already Tower

                foreach (Hero hero in clan.Heroes.Where(h => h != null && h.IsAlive).ToList())
                    try { ApplyTowerJoinMagic(hero); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        internal static void ApplyTowerJoinMagic(Hero hero)
        {
            if (hero == null) return;
            try
            {
                if (hero == Hero.MainHero)
                {
                    bool wasLocked = !SpellbookCampaignBehavior.IsUnlocked;
                    if (wasLocked && SpellbookCampaignBehavior.TryUnlock(free: true))
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "The Tower opens its shelves to you. The spellbook is yours to read, free of charge.",
                            Gold));
                    }
                    return;
                }

                if (SpellcasterLords.IsEligible(hero)) return; // already a recognised caster

                var allIds = SpellbookCatalog.All.Select(d => d.Id).ToList();
                if (allIds.Count == 0) return;
                var granted = PickRandom(allIds, Math.Min(TowerMath.LordGrantedSpellCount, allIds.Count));
                SpellcasterLords.GrantToHero(hero, granted);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static List<SpellId> PickRandom(List<SpellId> source, int count)
        {
            var pool = new List<SpellId>(source);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }
            return pool.Take(count).ToList();
        }

        private static readonly Color Gold = new Color(0.95f, 0.8f, 0.3f);
    }
}
