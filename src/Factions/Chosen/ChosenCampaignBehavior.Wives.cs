// =============================================================================
// THE DARKEST NIGHT — Factions/Chosen/ChosenCampaignBehavior.Wives.cs
//
// Player wife-taking for a male Chosen player — two UX paths, per the mod
// author's brief (both implemented, since neither is hard given the
// God-King precedent and each suits a different playstyle):
//
//   1. On the player CAPTURING a settlement (OnSettlementOwnerChangedEvent,
//      gated to BySiege so a diplomatic gift/rebellion never fires this) —
//      an InformationManager.ShowInquiry prompt offers a woman of that
//      settlement as a new wife. Template selection mirrors
//      TribalKingdomBehavior.AcquireConsort exactly: prefer a female
//      template from the settlement's own culture.
//   2. A Chosen-town city menu option ("take a prisoner as a wife" — see
//      ChosenCampaignBehavior.Menus.cs) that consumes a qualifying female
//      prisoner from the player's PrisonRoster, mirroring
//      AshenRecruitCampaignBehavior's "spend a qualifying prisoner"
//      consumption pattern, and uses the SPENT PRISONER'S OWN CharacterObject
//      as the template instead of the settlement's culture.
//
// Both paths create the new wife hero via HeroCreator.CreateChild(template,
// birthPlace, Clan.PlayerClan, age) — the SAME call TribalKingdomBehavior's
// God-King consorts already use. Despite the "child" name, this creates a
// freshly-instantiated adult Hero at whatever age is passed (18-26 here, the
// same span already proven safe by the God-King precedent; the age is a
// parameter, not a hardcoded infant) — verified by reading
// SettlementEncounters.Events1.cs (age 18, an adult companion) vs
// SettlementEncounters.Events3.cs (age 1, an actual newborn) both calling
// the exact same method: "child" means "hero created fresh from a template",
// not "infant."
//
// Cap: ChosenMath.PlayerWifeMax (8), mirroring the God-King's TribalWifeMax
// precedent. Persistence: CHO_PLAYER_WIFE_IDS, the same "flat StringId list"
// shape _consortIds already uses.
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
    public partial class ChosenCampaignBehavior
    {
        private static readonly Random _wifeRng = new Random();

        private static List<string> _playerWifeIds = new List<string>();

        private static void SyncWifeData(IDataStore store)
        {
            try { store.SyncData("CHO_PLAYER_WIFE_IDS", ref _playerWifeIds); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            if (_playerWifeIds == null) _playerWifeIds = new List<string>();
        }

        private static void ResetWifeStateForNewGame()
        {
            _playerWifeIds = new List<string>();
        }

        internal static bool IsMalePlayerEligibleForMoreWives()
        {
            try
            {
                Hero player = Hero.MainHero;
                if (player == null || player.IsFemale) return false;
                if (!ChosenCulture.IsPlayerChosen) return false;
                return _playerWifeIds.Count < ChosenMath.PlayerWifeMax;
            }
            catch { return false; }
        }

        private static void AddNewWife(Hero wife)
        {
            if (wife == null) return;
            try
            {
                _playerWifeIds.Add(wife.StringId);
                Hero.MainHero.Spouse = wife;
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        // ── Path 1: settlement-capture prompt ───────────────────────────────────
        private void OnSettlementOwnerChanged(Settlement settlement, bool ownerFactionChanged,
            Hero newOwner, Hero oldOwner, Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            try
            {
                if (settlement == null) return;
                if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege) return; // only a real conquest
                if (capturerHero != Hero.MainHero && newOwner != Hero.MainHero) return; // the player did the capturing

                if (!IsMalePlayerEligibleForMoreWives()) return;

                CharacterObject template = FindFemaleTemplateForSettlement(settlement);
                if (template == null) return;

                InformationManager.ShowInquiry(new InquiryData(
                    "A Woman of the Conquered",
                    $"{settlement.Name} is yours now, and with it, its people. Will you take a woman of this place as a wife, "
                  + "as the PriestKing's own line has always done?",
                    true, true, "Take her", "Leave her be",
                    () =>
                    {
                        try
                        {
                            var wife = HeroCreator.CreateChild(template, settlement, Clan.PlayerClan,
                                ChosenMath.RollWifeAge(_wifeRng.NextDouble()));
                            if (wife == null) return;
                            AddNewWife(wife);
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{wife.Name} joins your household — one more of {settlement.Name}'s own now bound to your line.",
                                new Color(0.85f, 0.72f, 0.25f)));
                        }
                        catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                    },
                    null));
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static CharacterObject FindFemaleTemplateForSettlement(Settlement settlement)
        {
            try
            {
                string cultureId = settlement?.Culture?.StringId ?? "";
                CharacterObject template = CharacterObject.All.FirstOrDefault(c =>
                    c != null && !c.IsHero && c.IsFemale && c.Culture?.StringId == cultureId);
                return template ?? CharacterObject.All.FirstOrDefault(c => c != null && !c.IsHero && c.IsFemale);
            }
            catch { return null; }
        }

        // ── Path 2: convert a qualifying female prisoner ────────────────────────
        // Mirrors AshenRecruitCampaignBehavior.HasQualifyingPrisoners/
        // ConsumeQualifyingPrisoners exactly, but for any non-hero female
        // prisoner troop rather than a tiered lord/notable — the PrisonRoster
        // only ever holds common-troop CharacterObjects (hero prisoners are
        // tracked separately via Hero.IsPrisoner), so the spent prisoner's own
        // CharacterObject doubles as the wife's template.
        internal static bool TryGetQualifyingFemalePrisoner(out CharacterObject template)
        {
            template = null;
            try
            {
                var party = MobileParty.MainParty;
                if (party?.PrisonRoster == null) return false;
                var entry = party.PrisonRoster.GetTroopRoster()
                    .FirstOrDefault(e => e.Character != null && !e.Character.IsHero
                                       && e.Character.IsFemale && e.Number > 0);
                if (entry.Character == null) return false;
                template = entry.Character;
                return true;
            }
            catch { return false; }
        }

        internal static void ConsumePrisonerAndWed(CharacterObject template)
        {
            if (template == null) return;
            try
            {
                var party = MobileParty.MainParty;
                if (party?.PrisonRoster == null) return;
                party.PrisonRoster.AddToCounts(template, -1);

                var wife = HeroCreator.CreateChild(template, Settlement.CurrentSettlement ?? party.CurrentSettlement,
                    Clan.PlayerClan, ChosenMath.RollWifeAge(_wifeRng.NextDouble()));
                if (wife == null) return;
                AddNewWife(wife);
            }
            catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }
    }
}
