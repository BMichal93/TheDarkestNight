// =============================================================================
// THE DARKEST NIGHT — AI/CreationBackstoryRework.Keepsakes.cs
// "The Keepsake" — replaces the vanilla Young Adulthood stage
// ("narrative_adulthood_menu", vanilla title "Young Adulthood", vanilla
// framing "Before you set out for a life of adventure, your biggest
// achievement was...") outright. Verified against the shipped
// TaleWorlds.CampaignSystem.dll (ildasm) — the menu is built by
// CharacterCreationCampaignBehavior.AddAdulthoodMenu/AddAdulthoodMenuOptions
// and carries twelve vanilla options, each gated by its own occupation/
// culture/register OnCondition delegate:
//   adulthood_defeated_enemy_option    (always true)
//   adulthood_manhunt_option           (non-urban occupation)
//   adulthood_caravan_leader_option    (urban occupation, several cultures)
//   adulthood_saved_village_option     (non-urban, Sturgia/Nord only)
//   adulthood_saved_city_option        (urban, Sturgia/Nord only)
//   adulthood_workshop_option          (urban occupation, register B)
//   adulthood_investor_option          (non-urban occupation, register B)
//   adulthood_hunter_option
//   adulthood_siege_survivor_option
//   adulthood_escapade_high_register_option
//   adulthood_escapade_low_register_option
//   adulthood_nice_person_option
// Requirement 23 Step 1 already pins every campaign to the Empire
// background, so several of these vanilla conditions (Sturgia/Nord-gated,
// occupation-gated) would leave the reachable set unpredictable and fewer
// than six. Rather than depend on that, we take the first six options in
// the engine's own add-order, rewrite them into the six keepsakes below,
// force their OnCondition to AlwaysVisible (unconditional, all cultures —
// Step 2 of the task explicitly forbids culture-gating this stage), and
// remove the remaining six from the menu's option list outright (the
// backing field is a mutable TaleWorlds.Library.MBList<NarrativeMenuOption>,
// confirmed via reflection — not a read-only collection) so no stale
// vanilla option can ever surface on this stage.
//
// Panel-expressible effects (skills/focus/attribute/one trait) are set via
// each keepsake's args getter, mirroring SquireArgs/ApostleArgs. Everything
// else — items, spells, renown — is a pending boon: OnCharacterCreationFinalize
// (in CreationBackstoryRework.cs) records the pick as `_pendingKeepsake`;
// ApplyKeepsakeBoon below is called from ApplyPendingBoons, AFTER the
// new-game reset, exactly like the Apostle/Squire boons.
//
// ── Substituted trade-good ids ───────────────────────────────────────────
// The task brief's suggested ids "meat" and "grain" do not exist in this
// Bannerlord build — already independently verified and documented in
// BeastsOfTheNorth/BeastsOfTheNorthMath.cs (full <!-- #region Trade -->
// sweep of SandBoxCore/ModuleData/items/horses_and_others.xml, the only
// file that defines trade-good Items here). "fish" and "mule" ARE real.
// Keepsake 4 ("Pack of goods") substitutes cheese + butter for meat + grain
// (real IsFood="true" trade goods, plausible travel provisions); Keepsake 3
// ("Trusted mount") substitutes fish for the promised "grain" for the same
// reason. Both substitutions are reflected in the option's own description
// text, so the promise always matches the grant.
//
// ── Item selection ────────────────────────────────────────────────────────
// The sword/horse pool is pulled from the live item registry at grant time
// (MBObjectManager.Instance.GetObjectTypeList<ItemObject>(), the same
// technique RuinsExplorationSystem.GrantRandomItem already uses) rather than
// a hardcoded StringId. Grants land in MobileParty.MainParty.ItemRoster, not
// equipped battle-gear slots: ApplyPendingBoons runs during OnNewGameCreated,
// before the player has ever seen the map, and NarrativeMenuOptionArgs offers
// no equipment-slot API — reflecting into Hero.MainHero's BattleEquipment at
// that exact moment (mid new-game bring-up, several other systems still
// wiring themselves in the same tick) is the fragile path the task spec
// explicitly permits falling back from. Roster is simple, safe, and the
// item is in the player's hands within the first town visit either way.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TheDarkestNight
{
    internal sealed partial class CreationBackstoryRework
    {
        private enum KeepsakeId { None, Blade, Book, Mount, Goods, Heirloom, Luck }

        private static KeepsakeId _pendingKeepsake = KeepsakeId.None;

        private const string KeepsakeMenuId = "narrative_adulthood_menu";

        // The six vanilla option ids we keep and rewrite (first six in the
        // engine's own AddAdulthoodMenuOptions add-order), mapped to the
        // keepsake each becomes. The remaining six vanilla ids are removed
        // from the menu outright in RewriteKeepsakeMenu.
        private static readonly Dictionary<string, KeepsakeId> KeepsakeOptionIds = new Dictionary<string, KeepsakeId>
        {
            { "adulthood_defeated_enemy_option", KeepsakeId.Blade    },
            { "adulthood_manhunt_option",        KeepsakeId.Book     },
            { "adulthood_caravan_leader_option", KeepsakeId.Mount    },
            { "adulthood_saved_village_option",  KeepsakeId.Goods    },
            { "adulthood_saved_city_option",     KeepsakeId.Heirloom },
            { "adulthood_workshop_option",       KeepsakeId.Luck     },
        };

        private static readonly string[] KeepsakeOptionsToRemove =
        {
            "adulthood_investor_option",
            "adulthood_hunter_option",
            "adulthood_siege_survivor_option",
            "adulthood_escapade_high_register_option",
            "adulthood_escapade_low_register_option",
            "adulthood_nice_person_option",
        };

        // NarrativeMenu.Title / .Description are public but readonly fields
        // (same shape as NarrativeMenuOption.Text/DescriptionText above).
        private static readonly System.Reflection.FieldInfo MenuTitleField =
            typeof(NarrativeMenu).GetField("Title", FPub);
        private static readonly System.Reflection.FieldInfo MenuDescriptionField =
            typeof(NarrativeMenu).GetField("Description", FPub);

        // Backing store for NarrativeMenu.CharacterCreationMenuOptions (the public
        // property is an MBReadOnlyList view); the field itself is a mutable MBList,
        // confirmed via reflection against the shipped TaleWorlds.CampaignSystem.dll.
        private static readonly System.Reflection.FieldInfo MenuOptionsField =
            typeof(NarrativeMenu).GetField("_characterCreationMenuOptions", FPriv);

        // ── Requirement 23, Step 6 (superseded) — "The Keepsake" ─────────────

        private static void RewriteKeepsakeMenu(CharacterCreationManager m)
        {
            var menu = m.GetNarrativeMenuWithId(KeepsakeMenuId);
            if (menu == null) return;

            try
            {
                MenuTitleField?.SetValue(menu, new TextObject("The Keepsake"));
                MenuDescriptionField?.SetValue(menu, new TextObject(
                    "Young adulthood ended the day you left home — not with a ceremony, but with a door closing "
                    + "behind you, and the night already taking its measure of you. You did not carry much. What "
                    + "you carried out that door was..."));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            Edit(m, KeepsakeMenuId, "adulthood_defeated_enemy_option",
                "your blade.",
                "It was already old when it came to you — notched, oiled, kept close by whoever carried it "
                + "before. You never asked how they died. It never told you.\n\n"
                + "(You will begin with one good one-handed sword.)",
                new GetNarrativeMenuOptionArgsDelegate(BladeArgs));

            Edit(m, KeepsakeMenuId, "adulthood_manhunt_option",
                "a stranger's book.",
                "You traded for it in a doorway, from someone who wanted it gone more than they wanted the "
                + "coin — five marks in an old, patient hand, and directions for saying them with your body "
                + "instead of your voice. You learned two of them badly, by the second week. They have not "
                + "badly failed you since.\n\n"
                + "(You will begin with the Spellbook unlocked and two spoken formulas already known.)",
                new GetNarrativeMenuOptionArgsDelegate(BookArgs));

            Edit(m, KeepsakeMenuId, "adulthood_caravan_leader_option",
                "the one horse worth trusting.",
                "Everyone else's mount bolted at the first scent of what walks after dark. Yours didn't — not "
                + "because it was braver, but because the two of you had already agreed, long before, on who "
                + "was in charge when the world went wrong.\n\n"
                + "(You will begin with one good, war-capable horse and 3 salted fish packed in its stores.)",
                new GetNarrativeMenuOptionArgsDelegate(MountArgs));

            Edit(m, KeepsakeMenuId, "adulthood_saved_village_option",
                "whatever you could pack onto a mule before dawn.",
                "Not much, but enough to trade your way to the next wall if this one falls: cheese wax-sealed "
                + "against the damp, butter in a stoppered jar, a little dried fish, and the mule patient "
                + "enough to carry it all without complaint.\n\n"
                + "(You will begin with 3 cheese, 3 butter, 3 fish, and a mule.)",
                new GetNarrativeMenuOptionArgsDelegate(GoodsArgs));

            Edit(m, KeepsakeMenuId, "adulthood_saved_city_option",
                "a family heirloom nobody would explain.",
                "Nobody in your family would say where it came from — only that it was to be kept, not sold, "
                + "not even in the worst winters. You kept it. You still don't fully understand what it is, "
                + "only that it hums, faintly, on nights when the dead are close.\n\n"
                + "(You will begin with +20 clan renown and a wand carried down through your family, its "
                + "working unknown to you.)",
                new GetNarrativeMenuOptionArgsDelegate(HeirloomArgs));

            Edit(m, KeepsakeMenuId, "adulthood_workshop_option",
                "nothing. Just luck, and it hasn't run out yet.",
                "No sword worth mentioning, no coin, no useful trade. Whatever got you this far was closer to "
                + "chance than to plan, and you stopped questioning it a while ago — mostly because questioning "
                + "it feels like tempting it to stop.",
                new GetNarrativeMenuOptionArgsDelegate(LuckArgs));

            // Requirement (this task) Step 2 — global, no culture gating: every
            // kept option must always show, regardless of the occupation/culture
            // picked earlier in Family (Requirement 23 already pins culture to
            // Empire, but occupation is still a live vanilla choice).
            foreach (string id in KeepsakeOptionIds.Keys)
                ForceAlwaysVisible(m, KeepsakeMenuId, id);

            // Remove the surplus six vanilla options outright so none can ever
            // surface with stale vanilla text on this stage.
            foreach (string id in KeepsakeOptionsToRemove)
                RemoveOption(m, KeepsakeMenuId, id);
        }

        private static void RemoveOption(CharacterCreationManager m, string menuId, string optionId)
        {
            var menu = m.GetNarrativeMenuWithId(menuId);
            var o = Find(m, menuId, optionId);
            if (menu == null || o == null) return;
            try
            {
                var list = MenuOptionsField?.GetValue(menu) as MBList<NarrativeMenuOption>;
                list?.Remove(o);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Args getters (panel-expressible effects) ─────────────────────────
        // DefaultSkills has no "Smithing" in this build (renamed Crafting) —
        // verified via reflection against TaleWorlds.Core.dll's DefaultSkills.

        private static void BladeArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.OneHanded, DefaultSkills.Tactics });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attr);
            args.SetAffectedTraits(new TraitObject[] { DefaultTraits.Valor });
            args.SetLevelToTraits(1);
        }

        private static void BookArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Medicine, DefaultSkills.Roguery });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attr);
            args.SetAffectedTraits(new TraitObject[] { DefaultTraits.Calculating });
            args.SetLevelToTraits(1);
        }

        private static void MountArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Scouting });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attr);
            args.SetAffectedTraits(_noTraits);   // no trait promised for this keepsake
        }

        private static void GoodsArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attr);
            args.SetAffectedTraits(new TraitObject[] { DefaultTraits.Generosity });
            args.SetLevelToTraits(1);
        }

        private static void HeirloomArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Steward, DefaultSkills.Crafting });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attr);
            args.SetAffectedTraits(_noTraits);   // no trait promised for this keepsake
        }

        private static void LuckArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[]
                { DefaultSkills.Athletics, DefaultSkills.Charm, DefaultSkills.Leadership });
            args.SetFocusToSkills(_focus);
            args.SetLevelToSkills(_skill);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attr);
            args.SetAffectedTraits(new TraitObject[] { DefaultTraits.Valor });
            args.SetLevelToTraits(1);
        }

        // ── Boon grants (post new-game-reset) ─────────────────────────────────

        private static void ApplyKeepsakeBoon(KeepsakeId keepsake)
        {
            var rng = new Random();
            switch (keepsake)
            {
                case KeepsakeId.Blade:
                    try { GrantRandomItemToRoster(FindOneHandedSwordPool(), rng); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case KeepsakeId.Book:
                    try
                    {
                        var pool = SpellbookCatalog.QualifyingForArcaneStart.ToList();
                        var picks = SpellbookMath.PickDistinctIndices(pool.Count, 2, rng);
                        foreach (int i in picks)
                            SpellbookCampaignBehavior.GrantStartingSpell(pool[i].Id);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case KeepsakeId.Mount:
                    try { GrantRandomItemToRoster(FindWarHorsePool(), rng); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { GrantTradeGood("fish", 3); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case KeepsakeId.Goods:
                    try { GrantTradeGood("cheese", 3); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { GrantTradeGood("butter", 3); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { GrantTradeGood("fish", 3); }   catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { GrantTradeGood("mule", 1); }   catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case KeepsakeId.Heirloom:
                    try { ClanRenown.Gain(Hero.MainHero?.Clan, 20f); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    try { GrantRandomWand(rng); }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    break;

                case KeepsakeId.Luck:
                    // No boon — the driest of the six by design.
                    break;
            }
        }

        // ── Item registry helpers ────────────────────────────────────────────

        private static List<ItemObject> FindOneHandedSwordPool()
        {
            var items = MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
            if (items == null) return new List<ItemObject>();

            var swords = items.Where(it => it != null
                && it.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon
                && it.PrimaryWeapon != null && it.PrimaryWeapon.WeaponClass == WeaponClass.OneHandedSword
                && !it.IsCraftedByPlayer && !it.IsCraftedWeapon
                && it.Tier >= ItemObject.ItemTiers.Tier3 && it.Tier <= ItemObject.ItemTiers.Tier5)
                .ToList();

            return PreferHeroCulture(swords);
        }

        private static List<ItemObject> FindWarHorsePool()
        {
            var items = MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
            if (items == null) return new List<ItemObject>();

            var horses = items.Where(it => it != null && it.HasHorseComponent
                && it.HorseComponent.IsRideable && !it.HorseComponent.IsPackAnimal && !it.HorseComponent.IsLiveStock
                && it.Tier >= ItemObject.ItemTiers.Tier3 && it.Tier <= ItemObject.ItemTiers.Tier5)
                .ToList();

            return PreferHeroCulture(horses);
        }

        // Prefers items matching the player's own culture where that narrows the
        // pool without emptying it; falls back to the full pool otherwise.
        private static List<ItemObject> PreferHeroCulture(List<ItemObject> pool)
        {
            string cultureId = null;
            try { cultureId = Hero.MainHero?.Culture?.StringId; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (cultureId == null) return pool;

            var preferred = pool.Where(it => it.Culture == null || it.Culture.StringId == cultureId).ToList();
            return preferred.Count > 0 ? preferred : pool;
        }

        private static void GrantRandomItemToRoster(List<ItemObject> pool, Random rng)
        {
            if (pool == null || pool.Count == 0) return;
            var picks = SpellbookMath.PickDistinctIndices(pool.Count, 1, rng);
            if (picks.Count == 0) return;
            var roster = MobileParty.MainParty?.ItemRoster;
            if (roster == null) return;
            roster.AddToCounts(pool[picks[0]], 1);
        }

        private static void GrantRandomWand(Random rng)
        {
            var pool = WandsCatalog.All.Where(w => w.Tier == WandTier.Standard).ToList();
            if (pool.Count == 0) return;
            var picks = SpellbookMath.PickDistinctIndices(pool.Count, 1, rng);
            if (picks.Count == 0) return;
            var item = MBObjectManager.Instance?.GetObject<ItemObject>(pool[picks[0]].ItemId);
            var roster = MobileParty.MainParty?.ItemRoster;
            if (item == null || roster == null) return;
            roster.AddToCounts(item, 1);
        }

        private static void GrantTradeGood(string itemId, int count)
        {
            var item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            var roster = MobileParty.MainParty?.ItemRoster;
            if (item == null || roster == null) return;
            roster.AddToCounts(item, count);
        }
    }
}
