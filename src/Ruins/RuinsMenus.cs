// =============================================================================
// THE DARKEST NIGHT — Ruins/RuinsMenus.cs
//
// Session launch: menu registration for ruin castles. Every other town/
// castle-scoped feature in this codebase (Crystals, AshenAltars, Sanctuary,
// Sea harbors, the Ashen recruiter, the Great Awakening altar) registers its
// option onto the vanilla "town" menu and gates on Settlement.CurrentSettlement
// — there is no separate "castle" menu id anywhere in this codebase's ~200
// files, confirming castles share the "town" menu id in this game version.
// Ruins follow the identical pattern, gated additionally on
// RuinsCastleSystem.IsRuin.
//
// NOTE this is ADDITIVE, not a replacement: it adds "Explore the ruin"
// alongside whatever vanilla castle options the engine already shows (manage
// garrison, etc.) — it does NOT hide them, and does not touch the siege flow.
// A ruin staying worthless to actually hold is instead enforced continuously
// by RuinsCastleSystem.ReapplyRuinNamesIfNeeded (every daily tick, not just
// once) — see that file's ownership note.
// =============================================================================

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TheDarkestNight
{
    public static class RuinsMenus
    {
        public static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try { RuinsExplorationSystem.RegisterWaitMenu(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
            try { RegisterEntryOption(starter); } catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
        }

        private static void RegisterEntryOption(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "ruins_enter", "{RUINS_ENTER_LABEL}",
                args =>
                {
                    try
                    {
                        var s = Settlement.CurrentSettlement;
                        if (s == null || !s.IsCastle || !RuinsCastleSystem.IsRuin(s)) return false;

                        bool onCd    = RuinsCastleSystem.IsOnCooldown(s.StringId);
                        bool cleared = RuinsCastleSystem.IsCleared(s.StringId);

                        string note = "";
                        if (onCd && cleared) note = " [picked clean]";
                        else if (onCd)       note = $" [unsettled — {RuinsCastleSystem.CooldownDaysLeft(s.StringId)} day(s)]";

                        MBTextManager.SetTextVariable("RUINS_ENTER_LABEL", $"Explore the ruin{note}");
                        args.IsEnabled = !onCd;
                        return true;
                    }
                    catch { return false; }
                },
                args =>
                {
                    try
                    {
                        var s = Settlement.CurrentSettlement;
                        if (s == null || !RuinsCastleSystem.IsRuin(s)) return;
                        RuinsExplorationSystem.BeginExploration(s);
                    }
                    catch (System.Exception logEx) { TheDarkestNight.ModLog.Error(logEx); }
                },
                false, -1, false);
        }
    }
}
