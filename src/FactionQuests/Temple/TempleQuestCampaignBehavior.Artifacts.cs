// =============================================================================
// THE DARKEST NIGHT — FactionQuests/Temple/TempleQuestCampaignBehavior.Artifacts.cs
//
// Guaranteed artifact placement (the balance-pass requirement — see
// TempleQuestMath.cs's header) and the delivery menu at a Temple town.
//
// ── How the guarantee works ─────────────────────────────────────────────────
// AssignArtifactRuins runs once, the moment the quest is accepted:
// TempleQuestMath.SelectArtifactRuins deterministically picks five (or fewer,
// see below) of the campaign's own ruin settlements (RuinsCastleSystem.
// AllRuinIds) and the chosen ids are PERSISTED — never re-rolled — so a
// player who has already found artifact #2 can never have it moved out from
// under them by some later session's ruin-set drift.
//
// OnRuinFullyCleared (subscribed to RuinsExplorationSystem.RuinFullyCleared)
// fires every time ANY ruin anywhere is fully cleared. If the cleared
// settlement happens to be one of our five assigned ruins AND that artifact
// has not already been found, the artifact item is granted directly to the
// player's roster — completely bypassing RelicMath.RollRuinLoot / RuinsMath.
// RollChamberLoot. This is a bonus grant layered on top of whatever the
// normal chamber-by-chamber loot rolls already turned up during the crawl,
// not a replacement for them.
//
// ── The "fewer than 5 ruins" fallback ───────────────────────────────────────
// RuinsMath.ConversionPercentOfCastles is 80%, so a campaign with fewer than
// five ruin castles total is extremely unlikely (it would need a very small
// custom map with fewer than ~6 castles overall). If it happens anyway,
// AssignArtifactRuins simply assigns however many ruins exist — the quest's
// own "AllArtifactsFound" check is against the ACTUAL assigned count, not a
// hardcoded 5, so the quest can still complete with fewer Vigils rather than
// deadlocking on relics that no ruin exists to hold. This is documented, not
// silently dropped: the Grand-Master's own dialogue is written to work either
// way, and TempleQuestLog logs exactly how many were ultimately required.
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    public sealed partial class TempleQuestCampaignBehavior
    {
        // ── Assignment ───────────────────────────────────────────────────────
        private static void AssignArtifactRuins()
        {
            try
            {
                var allRuins = RuinsCastleSystem.AllRuinIds();
                _artifactRuinIds = TempleQuestMath.SelectArtifactRuins(allRuins, TempleQuestMath.ArtifactCount);
            }
            catch (System.Exception logEx)
            {
                AshAndEmber.ModLog.Error(logEx);
                _artifactRuinIds = new List<string>();
            }
        }

        internal static int RequiredArtifactCount() => _artifactRuinIds?.Count ?? 0;
        internal static int FoundArtifactCount() => _foundArtifactIndices?.Count ?? 0;

        internal static bool AllArtifactsFound()
            => RequiredArtifactCount() > 0 && FoundArtifactCount() >= RequiredArtifactCount();

        // ── The guaranteed grant ─────────────────────────────────────────────
        private static void OnRuinFullyCleared(string settlementStringId)
        {
            try
            {
                if (_phase != PhaseSeeking) return;
                if (string.IsNullOrEmpty(settlementStringId) || _artifactRuinIds == null) return;

                int idx = _artifactRuinIds.IndexOf(settlementStringId);
                if (idx < 0) return; // not one of the five assigned ruins
                if (_foundArtifactIndices.Contains(idx)) return; // already granted

                if (!TempleQuestArtifacts.TryGet(idx, out var def)) return;

                var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                var roster = MobileParty.MainParty?.ItemRoster;
                if (item == null || roster == null) return;
                roster.AddToCounts(item, 1);

                _foundArtifactIndices.Add(idx);

                InformationManager.ShowInquiry(new InquiryData(
                    def.Name,
                    $"Beneath the last dust of the Throne, something the ordinary loot of this place was never " +
                    $"going to explain: {def.Name}.\n\n{def.Lore}\n\n" +
                    $"Vigils found: {FoundArtifactCount()} / {RequiredArtifactCount()}.",
                    true, false, "So the trail was true.", "", null, null), true);

                try { TempleQuestLog.Current?.LogArtifactFound(FoundArtifactCount(), RequiredArtifactCount()); }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                if (AllArtifactsFound())
                {
                    _phase = PhaseDelivery;
                    try { TempleQuestLog.Current?.LogAllFound(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // ── Delivery menu — mirrors TowerRiteQuestCampaignBehavior.Menus.cs's
        //    gather/deliver shape, gated to a Temple town instead of Iyakis ────
        private static void RegisterDeliveryMenu(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("town", "tplq_deliver_enter", "{TPLQ_DELIVER_TEXT}",
                    args =>
                    {
                        try
                        {
                            if (_phase != PhaseDelivery) return false;
                            if (!TempleSettlements.IsTempleSettlement(Settlement.CurrentSettlement)) return false;
                            MBTextManager.SetTextVariable("TPLQ_DELIVER_TEXT", "Lay the Five Vigils before the Grand-Master");
                            try { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                            args.IsEnabled = true;
                            return true;
                        }
                        catch { return false; }
                    },
                    args => { try { ShowDeliveryMenu(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    false, -1, false);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void ShowDeliveryMenu()
        {
            bool ready = HeldVigilCount() >= RequiredArtifactCount();
            string body =
                "The Grand-Master's own hands are steady, but everyone else in the hall is watching yours.\n\n" +
                $"Vigils carried: {HeldVigilCount()} / {RequiredArtifactCount()}\n\n" +
                (ready
                    ? "Every Vigil the trail promised is here. Lay them before the Grand-Master, and the Order will bind itself for good."
                    : "You do not yet carry every Vigil the trail promised.");

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Rite of Union",
                    body,
                    ready, true,
                    "Lay them before the Grand-Master.",
                    "Leave",
                    ready ? (System.Action)PerformDelivery : null,
                    () => { try { GameMenu.SwitchToMenu("town"); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } }
                ), true, true);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static int HeldVigilCount()
        {
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster == null) return 0;
                int total = 0;
                foreach (var def in TempleQuestArtifacts.All)
                {
                    var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                    if (item == null) continue;
                    if (roster.GetItemNumber(item) > 0) total++;
                }
                return total;
            }
            catch { return 0; }
        }

        private static void PerformDelivery()
        {
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster != null)
                {
                    foreach (var def in TempleQuestArtifacts.All)
                    {
                        var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                        if (item == null) continue;
                        int have = roster.GetItemNumber(item);
                        if (have > 0) roster.AddToCounts(item, -have);
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try { TempleQuestLog.Current?.LogDelivered(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Rite of Union",
                    "Five relics that never once shared a table now share an altar. The Grand-Master lays a hand " +
                    "on each in turn, and speaks a Vow the Order has kept sworn, unspoken, for longer than any of " +
                    "them can say.\n\n" +
                    "\"Every sword. Every banner. One host, from this hour, and no other calling until the dark is " +
                    "gone or we are.\"\n\n" +
                    "There is no cheer in the hall. Only the sound of a great many people quietly agreeing to " +
                    "something they cannot take back.",
                    true, false, "So it is bound.", "",
                    () => { try { BindPermanentArmy(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); } },
                    null
                ), true, true);
            }
            catch (System.Exception logEx)
            {
                AshAndEmber.ModLog.Error(logEx);
                BindPermanentArmy();
            }
        }
    }
}
