// =============================================================================
// THE DARKEST NIGHT — AshenRuins/AshenRuinSystem.Rewards.cs
// Reward dispatch, grant helpers, cooldown tracking, and misc helpers.
// Partial of AshenRuinSystem (shared state lives in AshenRuinSystem.cs).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public static partial class AshenRuinSystem
    {
        // ── Reward dispatch ───────────────────────────────────────────────────
        private static void GrantReward(RuinReward reward, bool sharedReward, string ruinName, bool full, bool repeatSubstituted = false)
        {
            if (reward == null) return;
            float split = sharedReward ? 0.5f : 1f;

            string header = repeatSubstituted
                ? $"{ruinName} has been picked over before, yet the dark refills it. You come away with less than the first time, but not with nothing."
                : full
                    ? $"You emerge from {ruinName} carrying something the darkness did not want you to have."
                    : $"You retreat from {ruinName} with what you could carry.";

            switch (reward.Type)
            {
                case RewardType.GrimoireFragment:
                    GrantGrimoireFragment(header); break;

                case RewardType.AgingReclaim:
                    int days = Math.Max(1, (int)(reward.Points * split));
                    AgingSystem.RejuvenateHero(Hero.MainHero, days);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} The fire gives back {days} day{(days!=1?"s":"")}.",
                        new Color(0.9f, 0.6f, 0.3f)));
                    break;

                case RewardType.WhisperPurge:
                    int purge = Math.Max(1, (int)(reward.Points * split));
                    MageKnowledge.RemoveWhispers(purge);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} The cold recedes — {purge} whisper{(purge!=1?"s":"")} quieted.",
                        new Color(0.7f, 0.7f, 0.9f)));
                    break;

                case RewardType.WhisperBrand:
                    int brand = (int)(reward.Points * split);
                    MageKnowledge.AddWhispers(brand);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} The cold follows you out — {brand} whisper{(brand!=1?"s":"")} deeper.",
                        new Color(0.45f, 0.35f, 0.65f)));
                    break;

                case RewardType.FocusPoints:
                    int fp = Math.Max(1, (int)(reward.Points * split));
                    try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints += fp; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} The knowledge crystallises into {fp} focus point{(fp!=1?"s":"")}.",
                        new Color(0.7f, 0.9f, 0.7f)));
                    break;

                case RewardType.RenownBurst:
                    float renown = reward.Points * split;
                    try { ClanRenown.Gain(Hero.MainHero.Clan, renown); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} Word of this spreads. (+{(int)renown} renown)",
                        new Color(0.9f, 0.78f, 0.25f)));
                    break;

                case RewardType.LoreVision:
                    InformationManager.ShowInquiry(new InquiryData(
                        ruinName, reward.LoreText ?? header,
                        true, false, "Leave", "", () => { }, null), true);
                    break;

                case RewardType.DragonArtifact:
                    _eyeFound = true;
                    InformationManager.ShowInquiry(new InquiryData(
                        "The Eye of Aenos",
                        "A sphere of obsidian that never cools. The last thing the First Drake saw before the old order sealed it. You hold it and every rune you know pulls toward it like a compass finding north.\n\nSomething about the old mage's request makes a different kind of sense now.",
                        true, false, "Take it", "",
                        () => { }, null), true);
                    break;

                case RewardType.AshenCrownFragment:
                    _crownFragments++;
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} A piece of something older than any crown. Fragment {_crownFragments}/3.",
                        new Color(0.6f, 0.45f, 0.8f)));
                    if (_crownFragments >= 3)
                    {
                        try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints += AshenCrownFpBonus; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                        InformationManager.DisplayMessage(new InformationMessage(
                            "The three fragments align. Something clicks into place in the work. +3 focus points.",
                            new Color(0.9f, 0.6f, 0.9f)));
                    }
                    break;

                case RewardType.VoidCrystal:
                    InformationManager.ShowInquiry(new InquiryData(
                        "Void Crystal",
                        "A dark glass shard, cold even when held. It hums at the same pitch as a rune half-drawn. You could sell it — 5000 denars, to the right buyer — or let it dissolve into you, which costs you nothing and gives back 20 days.",
                        true, true, "Dissolve it (20 days reclaimed)", "Sell it (5000 denars)",
                        () => AgingSystem.RejuvenateHero(Hero.MainHero, 20),
                        () => { try { Hero.MainHero.ChangeHeroGold(5000); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); } }), true);
                    break;

                case RewardType.AncientGrimoire:
                    GrantAllGrimoireFragments(header); break;

                case RewardType.MagicCrystal:
                    GrantMagicCrystal(header, Math.Max(1, (int)(reward.Points * split))); break;

                case RewardType.GoldCache:
                    int gold = Math.Max(50, (int)(reward.Points * split));
                    try { Hero.MainHero.ChangeHeroGold(gold); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{header} A cache of coin, still good — {gold} denars.",
                        new Color(0.9f, 0.78f, 0.25f)));
                    break;

                case RewardType.SkillTome:
                    GrantSkillTome(header, Math.Max(10f, reward.Points * split)); break;

                case RewardType.EmberBoon:
                    GrantEmberBoon(header, Math.Max(1, (int)(reward.Points * split))); break;
            }
        }

        private static readonly SkillObject[] _skillTomePool =
        {
            DefaultSkills.Roguery, DefaultSkills.Charm, DefaultSkills.Leadership,
            DefaultSkills.Medicine, DefaultSkills.Steward,
        };

        private static void GrantSkillTome(string header, float xp)
        {
            var skill = _skillTomePool[_rng.Next(_skillTomePool.Length)];
            try { Hero.MainHero?.HeroDeveloper?.AddSkillXp(skill, xp); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            InformationManager.DisplayMessage(new InformationMessage(
                $"{header} Old knowledge settles into a skill you already had — {(int)xp} {skill.Name} experience.",
                new Color(0.7f, 0.9f, 0.7f)));
        }

        // Reacts to the caster's own path — the ruins reward conviction differently
        // depending on what kind of fire the player actually carries.
        private static void GrantEmberBoon(string header, int points)
        {
            if (MageKnowledge.IsAshen)
            {
                MageKnowledge.RemoveWhispers(points);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{header} The cold recedes a little further than it should — {points} whisper{(points!=1?"s":"")} quieted.",
                    new Color(0.7f, 0.7f, 0.9f)));
                return;
            }
            bool hasGrace = false;
            try { hasGrace = MiracleInventory.HasGrace; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (hasGrace)
            {
                int added = MiracleInventory.AddGrace(points);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{header} Conviction answers conviction — {added} Grace.",
                    new Color(0.95f, 0.85f, 0.6f)));
                return;
            }
            int fp = Math.Max(1, points / 2);
            try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints += fp; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            InformationManager.DisplayMessage(new InformationMessage(
                $"{header} Nothing claims the offering, so the hand simply steadies. +{fp} focus point{(fp!=1?"s":"")}.",
                new Color(0.7f, 0.9f, 0.7f)));
        }

        private static void GrantMagicCrystal(string header, int count)
        {
            var defs = CrystalCatalog.All;
            string lastName = null;
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                for (int i = 0; i < count; i++)
                {
                    var def  = defs[_rng.Next(defs.Count)];
                    var item = TaleWorlds.ObjectSystem.MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                    if (item != null && roster != null) roster.AddToCounts(item, 1);
                    lastName = def.Name;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }

            string body = count > 1
                ? $"{count} crystals rest among the ash, their lattices somehow unbroken."
                : $"A {lastName} rests among the ash, its lattice somehow unbroken.";
            InformationManager.DisplayMessage(new InformationMessage($"{header} {body}", new Color(0.75f, 0.55f, 0.85f)));
        }

        private static void GrantGrimoireFragment(string header)
        {
            var lostForms = new[] { TalentId.LostBlast, TalentId.LostMissile, TalentId.LostBarrier, TalentId.LostBurst };
            var available = lostForms.Where(id => !TalentSystem.Has(id)).ToArray();
            if (available.Length > 0)
            {
                TalentSystem.GrantFree(available[_rng.Next(available.Length)], Hero.MainHero);
                InformationManager.DisplayMessage(new InformationMessage(header, new Color(0.9f, 0.7f, 0.3f)));
            }
            else
            {
                try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints += 2; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{header} The knowledge was already yours — but the hand steadies anyway. +2 focus points.",
                    new Color(0.7f, 0.9f, 0.7f)));
            }
        }

        private static void GrantAllGrimoireFragments(string header)
        {
            var lostForms = new[] { TalentId.LostBlast, TalentId.LostMissile, TalentId.LostBarrier, TalentId.LostBurst };
            int granted = 0;
            foreach (var id in lostForms)
                if (TalentSystem.GrantFree(id, Hero.MainHero)) granted++;
            if (granted > 0)
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{header} {granted} lost form{(granted!=1?"s":"")} recovered from the dark.",
                    new Color(0.9f, 0.7f, 0.3f)));
            else
            {
                try { Hero.MainHero.HeroDeveloper.UnspentFocusPoints += 4; } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{header} Every lost form was already yours. The grimoire gives back 4 focus points instead.",
                    new Color(0.7f, 0.9f, 0.7f)));
            }
        }

        // ── Cooldown tracking ──────────────────────────────────────────────────
        private static void MarkCleared(string vname)
        {
            _cleared.Add(vname);
            int days = AshenRuinMath.RecoveryCooldownDays(_rng.Next(91)); // 30-120 inclusive
            SetCooldown(vname, days);
        }

        private static void SetCooldown(string vname, int days)
        {
            int idx = _cdKeys.IndexOf(vname);
            if (idx >= 0) { _cdDays[idx] = days; return; }
            _cdKeys.Add(vname); _cdDays.Add(days);
        }

        private static void SetGuardCooldown(string vname, int days)
        {
            int idx = _guardCdKeys.IndexOf(vname);
            if (idx >= 0) { _guardCdDays[idx] = days; return; }
            _guardCdKeys.Add(vname); _guardCdDays.Add(days);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static void AgePlayer(int days)
        {
            try { AgingSystem.AgeHero(Hero.MainHero, days); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void LoseTroops(int count)
        {
            try
            {
                var roster = MobileParty.MainParty?.MemberRoster;
                if (roster == null) return;
                int remaining = count;
                foreach (var entry in roster.GetTroopRoster().ToList())
                {
                    if (remaining <= 0) break;
                    if (entry.Character == null || entry.Character.IsHero) continue;
                    int healthy = entry.Number - entry.WoundedNumber;
                    if (healthy <= 0) continue;
                    int kill = Math.Min(remaining, healthy);
                    roster.AddToCounts(entry.Character, -kill);
                    remaining -= kill;
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void ApplyPartyHealthPenalty(float fraction)
        {
            try
            {
                var roster = MobileParty.MainParty?.MemberRoster;
                if (roster == null) return;
                foreach (var entry in roster.GetTroopRoster().ToList())
                {
                    if (entry.Character == null || entry.Character.IsHero) continue;
                    int healthy = entry.Number - entry.WoundedNumber;
                    int wound = Math.Max(1, (int)(healthy * fraction));
                    if (wound > 0) try { roster.AddToCounts(entry.Character, 0, false, wound); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

    }
}
