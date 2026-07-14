// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Bloodbound/BloodboundQuestCampaignBehavior.Altar.cs
//
// The Surpassing Rite's own shrine, mirroring GreatAwakeningCampaignBehavior.
// Altar.cs's shape almost exactly: one fixed settlement (chosen once from
// BloodboundMath.StartingTownIds, preferring Akkalat), a "town" menu entry
// that only appears there, and a "give everything you're holding" contribute
// action — the player empties their current Demon Blood stock in one action
// rather than a slow one-at-a-time menu click, since DonationTarget=750 one
// vial at a time (like the day-to-day spending menu in Factions/Bloodbound/
// BloodboundCampaignBehavior.Menus.cs) would take an unreasonable number of
// menu visits. NPC contribution (Bloodbound lords occasionally feeding the
// shrine from their own hunted stock) mirrors GreatAwakeningCampaignBehavior.
// NpcContribution.cs's weekly-roll pattern.
// =============================================================================

using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public sealed partial class BloodboundQuestCampaignBehavior
    {
        // Southernmost-style fixed pick isn't needed here — the Bloodbound only
        // ever hold two seats (BloodboundMath.StartingTownIds), so this simply
        // prefers Akkalat (town_K2, matching the Huntmaster's own reveal line)
        // and falls back to Chaikand if Akkalat isn't currently Bloodbound-held.
        private static void EnsureShrineChosen()
        {
            if (!string.IsNullOrEmpty(_shrineSettlementId)) return;
            try
            {
                Kingdom bloodbound = GetBloodboundKingdom();
                if (bloodbound == null) return;

                foreach (string townId in BloodboundMath.StartingTownIds)
                {
                    Settlement s = bloodbound.Settlements.FirstOrDefault(x => x != null && x.StringId == townId);
                    if (s != null) { _shrineSettlementId = s.StringId; return; }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        internal static Settlement ShrineSettlement()
        {
            if (string.IsNullOrEmpty(_shrineSettlementId)) return null;
            try { return Settlement.All.FirstOrDefault(s => s.StringId == _shrineSettlementId); }
            catch { return null; }
        }

        internal static bool ShrineIsBloodboundOwned()
        {
            var s = ShrineSettlement();
            try { return s != null && s.MapFaction?.StringId == BloodboundCulture.CultureId; }
            catch { return false; }
        }

        internal static void RegisterQuestAltarMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "bldq_shrine_enter", "{BLDQ_SHRINE_ENTER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseAccumulating) return false;
                            if (Settlement.CurrentSettlement == null
                                || Settlement.CurrentSettlement.StringId != _shrineSettlementId) return false;
                            MBTextManager.SetTextVariable("BLDQ_SHRINE_ENTER_TEXT",
                                $"Pour your Demon Blood into the Surpassing Rite  [{_bloodDonated:N0} / {BloodboundQuestMath.DonationTarget:N0}]");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { GameMenu.SwitchToMenu("bldq_shrine_main"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenu("bldq_shrine_main", "{BLDQ_SHRINE_HEADER}", args =>
                {
                    try
                    {
                        bool ownedByBloodbound = ShrineIsBloodboundOwned();
                        string status = ownedByBloodbound
                            ? "The shrine still answers to the Bloodbound."
                            : "The shrine has fallen out of Bloodbound hands. Nothing offered here reaches the draught now.";
                        MBTextManager.SetTextVariable("BLDQ_SHRINE_HEADER",
                            "Vials line the shrine's stone shelves, dark and unhurried, waiting for the count to " +
                            "reach what the Huntmaster asked for.\n\n" +
                            $"Poured toward the Surpassing Rite: {_bloodDonated:N0} / {BloodboundQuestMath.DonationTarget:N0}\n\n" +
                            status);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                });
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenuOption("bldq_shrine_main", "bldq_shrine_contribute", "{BLDQ_SHRINE_CONTRIBUTE_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseAccumulating) return false;
                            int held = PlayerBloodCount();
                            bool canGive = ShrineIsBloodboundOwned() && held > 0;
                            string note = !ShrineIsBloodboundOwned() ? "  [the shrine is not the Bloodbound's to use]"
                                        : held <= 0 ? "  [you carry no Demon Blood]"
                                        : "";
                            MBTextManager.SetTextVariable("BLDQ_SHRINE_CONTRIBUTE_TEXT",
                                $"Give every vial you carry to the draught ({held}){note}");
                            args.IsEnabled = canGive;
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Continue; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            return true;
                        }
                        catch { return false; }
                    },
                    args => DoContribute());
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                starter.AddGameMenuOption("bldq_shrine_main", "bldq_shrine_leave", "Step away",
                    args => { try { args.optionLeaveType = GameMenuOption.LeaveType.Leave; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } return true; },
                    args => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    true, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static ItemObject DemonBloodItem()
        {
            try { return MBObjectManager.Instance?.GetObject<ItemObject>(BloodboundCatalog.DemonBloodItemId); }
            catch { return null; }
        }

        private static int PlayerBloodCount()
        {
            try
            {
                var item = DemonBloodItem();
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return 0;
                return roster.GetItemNumber(item);
            }
            catch { return 0; }
        }

        private static void DoContribute()
        {
            try
            {
                var item = DemonBloodItem();
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return;

                int given = roster.GetItemNumber(item);
                if (given <= 0) return;
                roster.AddToCounts(item, -given);

                AddDonation(given);

                MBInformationManager.AddQuickInformation(new TextObject(
                    $"{given} vial(s) go into the shrine's basin and do not come back out. The Surpassing Rite " +
                    $"stands at {_bloodDonated:N0} / {BloodboundQuestMath.DonationTarget:N0}."));
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── NPC contribution — background trickle ────────────────────────────────
        private void NpcContributionWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;

            Kingdom bloodbound = GetBloodboundKingdom();
            if (bloodbound == null) return;

            var item = DemonBloodItem();
            if (item == null) return;

            int total = 0;
            try
            {
                foreach (Hero lord in bloodbound.Heroes.Where(h =>
                             h != null && h.IsLord && h.IsAlive && !h.IsChild && h != Hero.MainHero).ToList())
                {
                    var party = lord.PartyBelongedTo;
                    var roster = party?.ItemRoster;
                    if (roster == null) continue;
                    int held = roster.GetItemNumber(item);
                    if (held <= 0) continue;
                    if (_rng.NextDouble() >= BloodboundQuestMath.NpcWeeklyContributionChance) continue;

                    int give = BloodboundQuestMath.NpcContributionAmount(_rng, held);
                    if (give <= 0) continue;

                    roster.AddToCounts(item, -give);
                    total += give;
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (total > 0) AddDonation(total);
        }

        // ── Threshold check — hands off to .Resolution.cs the moment it's met ───
        private void CheckThresholdWeeklyTick()
        {
            if (_phase != PhaseAccumulating) return;
            if (!BloodboundQuestMath.HasReachedThreshold(_bloodDonated)) return;

            _phase = PhaseAwaitingChoice;
            try { ShowResolutionChoice(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
