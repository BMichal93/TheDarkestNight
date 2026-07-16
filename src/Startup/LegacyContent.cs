// =============================================================================
// THE DARKEST NIGHT — Startup/LegacyContent.cs
//
// v0.8.0 (playtest issue 7): a single, grep-able authority switch for
// "does new-game world setup still stand up the Ashen?" The Ashen (the old
// Ash and Ember "ancient fire-lords" kingdom) belonged to the previous
// fiction — a mage-blooded world where the cold claimed the ancient and the
// captive. In The Darkest Night's fiction, demons already own the night;
// a second, separate immortal-villain kingdom competing for the same
// narrative space reads as clutter, not depth.
//
// AshenEnabled = false gates only the CREATION/ESTABLISHMENT paths for NEW
// games (CampaignBehavior.Events.cs' FinishNewGameWorldSetup no longer calls
// AshenCitySystem.Initialize()/DailyTick(), ReassignImperialSettlements no
// longer seeds an Ashen leader or declares the founding war). It does NOT
// remove any code, save key, or type — an existing v0.7.x save that already
// has an Ashen kingdom keeps it fully alive: AshenCitySystem's daily tick,
// renames, and dialogue all still run for as long as that kingdom exists,
// because every presentation/tick path in that system is now gated on
// "is there a living Ashen kingdom OR is legacy content explicitly enabled"
// rather than on this flag alone. See AshenCitySystem.HasLivingAshenKingdom.
// =============================================================================

namespace TheDarkestNight
{
    public static class LegacyContent
    {
        // Flip to true only for debugging/reverting — no shipped build should
        // ever need to.
        public const bool AshenEnabled = false;

        // v0.8.0 (issue 17) — NPC magic is the Spellbook now. The legacy unified
        // element system's NPC lord casters (ElementLordAI, gated on
        // ElementLordRegistry.IsElementLord) are seeded once per campaign by
        // ElementLordRegistry.SeedInitialLords() — never calling that on a NEW
        // game means _mageIds stays empty and ElementLordAI/BanditMageAI's lord
        // path never fires, with zero risk to old saves: SeedInitialLords()
        // itself no-ops once LDM_MageSeeded is restored true from an existing
        // save, so this flag only ever changes behaviour on a fresh campaign.
        // The Spellbook's own rare casters (SpellcasterLords/SpellcasterTroops)
        // and the Nature seers (still shared with the Wind/Earth/Water Spellbook
        // spells — see CLAUDE.md's Nature/ note) are UNAFFECTED by this flag.
        public const bool LegacyNpcCastersEnabled = false;
    }
}
