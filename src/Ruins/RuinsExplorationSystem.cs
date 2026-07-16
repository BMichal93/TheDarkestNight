// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsExplorationSystem.cs
//
// Phase 9 (Requirement 12) — chamber-by-chamber crawl through a converted
// ruin castle. Mirrors AshenRuinSystem's "arrive → resolve → advance or
// retreat" shape, but every chamber transition now costs a genuine,
// Scouting-scaled in-game wait (RuinsExplorationSystem.WaitMenu.cs) rather
// than resolving instantly, and that wait is where the nightfall risk lives.
//
// Loot wiring (Requirement 12):
//   • Weapons / armor / trade goods — a random real ItemObject of the
//     matching ItemTypeEnum, the same "pull one from the live catalogue"
//     technique GearWeathering.BuildCheapestCatalogue already uses.
//   • Relics — RelicMath.RollRuinLoot (the exact hook Phase 6 left for this:
//     "Phase 9's chamber-clear loot roll can call this directly... without
//     RelicMath needing to know anything about chambers, Scouting, or
//     wait-menus" — RelicMath.cs, Ruin loot hook comment) gates a
//     RelicCatalog pick, granted the same way RollRelicDrop already does.
//   • Spell formulas — SpellbookCampaignBehavior.LearnSpellFromRuin(id), the
//     exact stub Phase 4 left ("Ruins — stub only; ruin exploration is
//     Phase 9" — SpellbookCampaignBehavior.cs header), now wired for real.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    public static partial class RuinsExplorationSystem
    {
        private static readonly Random _rng = new Random();

        // The ruin currently being crawled (there is only ever one active
        // crawl — the player can only be in one settlement at a time). Not
        // persisted, exactly like AshenRuinSystem's inquiry chain and
        // SettlementEncounters'/Sea's wait state: a reload mid-crawl simply
        // drops back to the town/castle menu having kept whatever was already
        // looted, which is the same "no resume" convention used everywhere
        // else a wait-menu is involved in this codebase.
        private static Settlement _activeRuin;
        private static int[]      _sequence;
        private static int        _chamberIdx;

        public static bool HasActiveCrawl => _activeRuin != null;

        // Extensibility hook (mod-author-directed addition, Phase 12) — fired
        // once, with the settlement's StringId, the moment a ruin is FULLY
        // cleared (FinishCrawl(fullyCleared: true)). Lets an outside system
        // (the Temple's "The Unbroken Vow" — FactionQuests/Temple/) hang a
        // deterministic, RNG-free grant off a specific ruin's full clear
        // WITHOUT this file needing to know anything about artifacts,
        // factions, or quest state — see TempleQuestMath.cs's header for why
        // the normal chamber loot roll (RelicMath.RollRuinLoot) is never used
        // for a quest-critical, must-exist item. Subscribers must guard their
        // own logic; a throwing subscriber is caught here so a broken hook
        // can never break ordinary ruin exploration.
        public static event Action<string> RuinFullyCleared;

        // ── Entry point (called from RuinsMenus) ────────────────────────────
        public static void BeginExploration(Settlement ruin)
        {
            if (ruin == null) return;
            if (RuinsCastleSystem.IsOnCooldown(ruin.StringId))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "The ruin is quiet for now — there is nothing left to find here yet.",
                    Dim));
                return;
            }

            var sequence = RuinsCastleSystem.ChamberSequenceFor(ruin);
            if (sequence == null || sequence.Length == 0) return;

            string ruinName = ruin.Name?.ToString() ?? "the ruin";
            InformationManager.ShowInquiry(new InquiryData(
                ruinName,
                "The gate hangs open. Whatever kept this place has been gone a long time — or is still down there, waiting. "
              + $"{sequence.Length} chamber(s) lie ahead of you.",
                true, true, "Enter", "Walk away",
                () =>
                {
                    _activeRuin  = ruin;
                    _sequence    = sequence;
                    _chamberIdx  = 0;
                    ArriveAtChamber();
                },
                () => { }), true);
        }

        // ── Arrival at the current chamber ──────────────────────────────────
        private static void ArriveAtChamber()
        {
            if (_activeRuin == null || _sequence == null || _chamberIdx >= _sequence.Length)
            {
                FinishCrawl(fullyCleared: true);
                return;
            }

            var def = RuinsCatalog.GetByIndex(_sequence[_chamberIdx]);
            InformationManager.ShowInquiry(new InquiryData(
                def.Name,
                def.EntryLore,
                true, true, "Search the chamber", "Retreat",
                () => SearchChamber(def),
                () => FinishCrawl(fullyCleared: false)), true);
        }

        // ── Searching: hazard roll, loot roll, then the wait to the next room ─
        private static void SearchChamber(ChamberDef def)
        {
            var body = new System.Text.StringBuilder();
            body.AppendLine(def.SearchLine);

            if (RuinsMath.RollHazard(_rng.NextDouble()))
                body.AppendLine().AppendLine(ApplyHazard());

            bool isFinal = _chamberIdx == _sequence.Length - 1;
            RuinsMath.LootKind loot = isFinal
                ? RuinsMath.FinalChamberLootRoll(_rng.NextDouble())
                : RuinsMath.BiasedLootRoll(_rng.NextDouble(), _rng.NextDouble(), def.LootBias);

            string lootLine = GrantLoot(loot);
            if (!string.IsNullOrEmpty(lootLine))
                body.AppendLine().AppendLine(lootLine);

            int scouting = 0;
            try { scouting = Hero.MainHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0; }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            float hours = RuinsMath.HoursForChamber(scouting);

            InformationManager.ShowInquiry(new InquiryData(
                def.Name, body.ToString(), true, false, "Press on", "",
                () => StartChamberWait(hours), null), true);
        }

        private static string ApplyHazard()
        {
            try
            {
                var roster = MobileParty.MainParty?.MemberRoster;
                int healthy = roster?.TotalHealthyCount ?? 0;
                var nonHeroTroop = healthy > 1
                    ? roster.GetTroopRoster().FirstOrDefault(t => t.Character != null && !t.Character.IsHero && t.Number > 0).Character
                    : null;
                if (nonHeroTroop != null)
                {
                    int lost = RuinsMath.HazardTroopLoss(_rng);
                    // Wound rather than kill outright — a close call, not a massacre.
                    try { roster.AddToCounts(nonHeroTroop, -Math.Min(lost, healthy - 1)); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    return "Something in the dark finds a few of your people before you find it. You lose a handful of soldiers to it.";
                }
                else
                {
                    int hp = RuinsMath.HazardHpLoss(_rng);
                    try { Hero.MainHero.HitPoints = Math.Max(1, Hero.MainHero.HitPoints - hp); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    return "Alone, the close call falls on you. You carry the wound out with you.";
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return "Something in the dark almost finds you."; }
        }

        // ── Loot granting ────────────────────────────────────────────────────
        private static string GrantLoot(RuinsMath.LootKind kind)
        {
            switch (kind)
            {
                case RuinsMath.LootKind.Weapon:
                    return GrantRandomItem(new[]
                    {
                        ItemObject.ItemTypeEnum.OneHandedWeapon, ItemObject.ItemTypeEnum.TwoHandedWeapon,
                        ItemObject.ItemTypeEnum.Polearm, ItemObject.ItemTypeEnum.Bow,
                        ItemObject.ItemTypeEnum.Crossbow, ItemObject.ItemTypeEnum.Thrown,
                    }, "You come away with a weapon still worth carrying: {0}.");

                case RuinsMath.LootKind.Armor:
                    return GrantRandomItem(new[]
                    {
                        ItemObject.ItemTypeEnum.HeadArmor, ItemObject.ItemTypeEnum.BodyArmor,
                        ItemObject.ItemTypeEnum.ChestArmor, ItemObject.ItemTypeEnum.LegArmor,
                        ItemObject.ItemTypeEnum.HandArmor, ItemObject.ItemTypeEnum.Cape,
                    }, "A piece of old armour, still sound: {0}.");

                case RuinsMath.LootKind.TradeGoods:
                    return GrantRandomItem(new[] { ItemObject.ItemTypeEnum.Goods },
                        "Goods worth carrying out, if nothing else: {0}.");

                case RuinsMath.LootKind.Relic:
                    return GrantRelic();

                case RuinsMath.LootKind.SpellFormula:
                    return GrantSpellFormula();

                case RuinsMath.LootKind.Wand:
                    return GrantWand();

                case RuinsMath.LootKind.Talisman:
                    return GrantTalisman();

                default:
                    return null;
            }
        }

        private static string GrantRandomItem(ItemObject.ItemTypeEnum[] types, string lineFormat)
        {
            try
            {
                var mgr = MBObjectManager.Instance;
                var items = mgr?.GetObjectTypeList<ItemObject>();
                if (items == null) return null;

                var candidates = items.Where(it => it != null && types.Contains(it.ItemType) && it.Value > 0).ToList();
                if (candidates.Count == 0) return null;

                var item = candidates[_rng.Next(candidates.Count)];
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster == null) return null;
                roster.AddToCounts(item, 1);
                return string.Format(lineFormat, item.Name?.ToString() ?? item.StringId);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        private static string GrantRelic()
        {
            try
            {
                if (!RelicMath.RollRuinLoot(_rng.NextDouble())) return null;

                var relics = RelicCatalog.All;
                int idx = RelicMath.PickRelicIndex(_rng.NextDouble(), relics.Count);
                if (idx < 0) return null;
                var def = relics[idx];

                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return null;
                roster.AddToCounts(item, 1);
                return $"Something answers a different light beneath the dust — {def.Name}.";
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // Wand loot (mod-author-directed addition) — the same "pull a random
        // catalog item and grant it" shape GrantRelic already uses, but the
        // chamber-loot roll already gates the rarity (RuinsMath.RollChamberLoot),
        // so there is no separate WandsMath.RollRuinWandLoot re-roll here —
        // reaching this method at all already means "the roll landed on Wand."
        private static string GrantWand()
        {
            try
            {
                var wands = WandsCatalog.All;
                if (wands.Count == 0) return null;
                int idx = WandsMath.PickWandIndex(_rng.NextDouble(), wands.Count);
                if (idx < 0) return null;
                var def = wands[idx];

                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return null;
                roster.AddToCounts(item, 1);
                WandEffects.RefillPlayerCharges(def.ItemId);
                return $"Wrapped in oiled cloth, untouched by the dust — {def.Name}.";
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // Talisman loot (mod-author-directed addition) — the same "pull a
        // random catalog item and grant it" shape GrantWand already uses; the
        // chamber-loot roll already gates the rarity (RuinsMath.RollChamberLoot),
        // so there is no separate TalismansMath.RollRuinTalismanLoot re-roll
        // here — reaching this method at all already means "the roll landed
        // on Talisman."
        private static string GrantTalisman()
        {
            try
            {
                var talismans = TalismansCatalog.All;
                if (talismans.Count == 0) return null;
                int idx = TalismansMath.PickTalismanIndex(_rng.NextDouble(), talismans.Count);
                if (idx < 0) return null;
                var def = talismans[idx];

                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return null;
                roster.AddToCounts(item, 1);
                return $"A small stone charm, still warm to the touch — {def.Name}.";
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        private static string GrantSpellFormula()
        {
            try
            {
                var unknown = SpellbookCatalog.All
                    .Where(d => !SpellbookCampaignBehavior.KnowsSpell(d.Id))
                    .ToList();
                if (unknown.Count == 0) return null;

                var def = unknown[_rng.Next(unknown.Count)];
                SpellbookCampaignBehavior.LearnSpellFromRuin(def.Id);
                return $"A formula, half-legible, still readable: {def.Name} [{def.Formula}]. You will not forget it now.";
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); return null; }
        }

        // ── Advance / finish ─────────────────────────────────────────────────
        private static void AdvanceAfterWait()
        {
            _chamberIdx++;
            ArriveAtChamber();
        }

        private static void FinishCrawl(bool fullyCleared)
        {
            string ruinName = _activeRuin?.Name?.ToString() ?? "the ruin";
            string stringId = _activeRuin?.StringId;

            if (fullyCleared && stringId != null)
            {
                RuinsCastleSystem.MarkCleared(stringId);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You reach the Throne of Dust and find nothing left sitting on it. {ruinName} has given up what it had.",
                    new Color(0.75f, 0.65f, 0.35f)));
                try { RuinFullyCleared?.Invoke(stringId); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            }
            else if (stringId != null)
            {
                RuinsCastleSystem.SetRetreatCooldown(stringId);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You leave {ruinName} with what you could carry. The rest of it can wait.",
                    new Color(0.6f, 0.55f, 0.5f)));
            }

            _activeRuin = null;
            _sequence   = null;
            _chamberIdx = 0;

            try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static readonly Color Dim = new Color(0.7f, 0.65f, 0.55f);
    }
}
