// =============================================================================
// LIFE & DEATH MAGIC — MagicSystem.cs
// Module entry point. Wires up the campaign behaviour and mission behaviour.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AshAndEmber
{
    // =========================================================================
    // MODULE ENTRY POINT
    // =========================================================================
    public class MainSubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            // Reset all in-mission static state that may be stale from a previous
            // game session running in the same process (save load without restart).
            // ClearAreaEffects / ClearSelfEffects are internally try/catch'd.
            try { SpellEffects.ClearAreaEffects();   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearSelfEffects();   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearGlows();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearMoves();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearPendingDeaths(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAnimTimers();    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearCastLoops();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearWard();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearStoneskin();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearSunder();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearChar();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearReflect();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAttackWeaken();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearScorch();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAshmark();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearInnerFireHeat(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearMagicMemory();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearDarkGiftsBattleState();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MagicInputHandler.ResetInputState();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementWallWards.Clear();              } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementSpellEffects.ClearBattleState();} catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementUltimates.ClearBattleState();   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementalBeings.ClearBattleState();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { DemonBattleBehavior.ClearBattleState(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleEffects.ClearBattleState();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleBattleAI.Reset();               } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleInputHandler.ResetInputState(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CrystalEffects.ClearBattleState();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CrystalBattleAI.Reset();               } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureEffects.ClearBattleState();      } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellbookInputHandler.ResetInputState(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellburnEffects.ClearBattleState();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { RelicEffects.ClearBattleState();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Wire the Grace bank to the live Abundant Grace devotion (the bank itself
            // is kept TaleWorlds-free so it stays unit-testable — see behaviour.md).
            MiracleInventory.TalentCapBonusProvider = () =>
            { try { return MiracleTalents.GraceCapBonus; } catch { return 0; } };
            try { NatureCharge.ClearForMission();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureChargeBar.Reset();               } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureSeerAI.ClearCooldowns();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureInputHandler.ResetInputState();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ColourLordAI.ClearCooldowns();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ColourLordAI.FlushBattleCasts();      } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellcasterLords.ClearCooldowns();    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellcasterTroops.ClearBattleState();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { BanditMageAI.OnMissionEnd();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshenSceneTone.Reset();                } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { BattleWhispers.Reset();                } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshenVisuals.Reset();                  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { GreatOtherParty.ClearMissionLatch();    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { HiveBattleEffects.Reset();              } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            if (game.GameType is Campaign &&
                gameStarterObject is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddModel(new AshenDiplomacyModel());
                campaignStarter.AddModel(new ForestClansSpeedModel());
                campaignStarter.AddModel(new ForestClansWageModel());
                campaignStarter.AddModel(new ForestClansPartySizeModel());
                // Phase 2 — the barter economy (Path B: gold ~10x scarcer
                // everywhere; see EconomyMath.cs header for the full feasibility
                // write-up on why Path A / full gold removal was rejected).
                campaignStarter.AddModel(new EconomyWageModel());
                campaignStarter.AddModel(new EconomyTroopUpgradeModel());
                campaignStarter.AddModel(new EconomyBuildingModel());
                campaignStarter.AddModel(new EconomyBattleRewardModel());
                campaignStarter.AddModel(new EconomyRansomModel());
                campaignStarter.AddModel(new EconomyGarrisonModel());
                campaignStarter.AddModel(new EconomyMilitiaModel());
                // Phase 3 — units reflect scarcity (Requirements 4, 30b).
                campaignStarter.AddModel(new UnitsRecruitModel());
                campaignStarter.AddBehavior(new MagicCampaignBehavior());
                campaignStarter.AddBehavior(new SchemeCampaignBehavior());
                campaignStarter.AddBehavior(new SanctuaryCampaignBehavior());
                campaignStarter.AddBehavior(new AshenAltarsCampaignBehavior());
                campaignStarter.AddBehavior(new SeaCampaignBehavior());
                campaignStarter.AddBehavior(new CrystallinesCampaignBehavior());
                campaignStarter.AddBehavior(new ExchangeCampaignBehavior());
                campaignStarter.AddBehavior(new TavernCampaignBehavior());
                campaignStarter.AddBehavior(new AshenRuinCampaignBehavior());
                campaignStarter.AddBehavior(new MiracleCampaignBehavior());
                campaignStarter.AddBehavior(new NatureCampaignBehavior());
                campaignStarter.AddBehavior(new ClanOrdersCampaignBehavior());
                campaignStarter.AddBehavior(new SoldierServiceCampaignBehavior());
                campaignStarter.AddBehavior(new ElementalWildsBehavior());
                campaignStarter.AddBehavior(new DemonSpawnCampaignBehavior());
                campaignStarter.AddBehavior(new MarketScarcityCampaignBehavior());
                campaignStarter.AddBehavior(new PromotionCampaignBehavior());
                campaignStarter.AddBehavior(new SacredSitesCampaignBehavior());
                campaignStarter.AddBehavior(new AshenRecruitCampaignBehavior());
                campaignStarter.AddBehavior(new TribalKingdomBehavior());
                campaignStarter.AddBehavior(new CreationBackstoryRework());
                campaignStarter.AddBehavior(new GreatAwakeningCampaignBehavior());
                campaignStarter.AddBehavior(new NorthmenStonesCampaignBehavior());
                campaignStarter.AddBehavior(new SpellbookCampaignBehavior());
                campaignStarter.AddBehavior(new SpellcasterTroopBehavior());
                // Phase 7, Faction A — the Wolf Brothers (Sturgia).
                campaignStarter.AddBehavior(new WolfBrothersCampaignBehavior());
                // Phase 7, Faction B — the Tower (Aserai).
                campaignStarter.AddBehavior(new TowerCampaignBehavior());
                // Phase 7, Faction C — the Hive (Battania).
                campaignStarter.AddBehavior(new HiveCampaignBehavior());
                // Phase 7, Faction D — the Bloodbound (Khuzait).
                campaignStarter.AddBehavior(new BloodboundCampaignBehavior());
                // Phase 7, Faction E — the Temple (Vlandia). The Vlandia -> Temple
                // rename/dialogue already exist in the baseline (TempleCulture.cs,
                // TempleDialogue.cs); this behavior only owns what Phase 7 adds:
                // town-scoping, the join gate, the Holy Sigil and its town menus.
                campaignStarter.AddBehavior(new TempleCampaignBehavior());
                // Phase 7, Faction F — the Empire (Northern Empire). A fresh
                // identity (the baseline mod never renamed any Empire kingdom).
                campaignStarter.AddBehavior(new EmpireCampaignBehavior());
                // Phase 7, Faction G — Legion (Western Empire). Another fresh
                // identity sharing the Empire's CultureObject but not its
                // Kingdom — see Factions/Legion/LegionCulture.cs header.
                campaignStarter.AddBehavior(new LegionCampaignBehavior());
                // Phase 7, Faction H — the Pale Widows (Southern Empire). The
                // last of the three Empire successors; a fresh identity sharing
                // the Empire's CultureObject but not its Kingdom — see
                // Factions/PaleWidows/PaleWidowsCulture.cs.
                campaignStarter.AddBehavior(new PaleWidowsCampaignBehavior());
                // Phase 8 — the wretched free towns. Turns every town the eight
                // Phase 7 factions ejected into its own permanent one-city
                // kingdom (modelled on AshenCitySystem's mechanics).
                campaignStarter.AddBehavior(new CityStateCampaignBehavior());
                // Phase 9 — the ruins. ~80% of castles (deterministically, by
                // settlement id, exempting Phase 7 starting towns, Phase 8
                // city-states, the Ashen realm, and the player's own holdings)
                // become ownerless-in-all-but-name legacy dungeons. See
                // Ruins/RuinsCastleSystem.cs for the full design note.
                campaignStarter.AddBehavior(new RuinsCampaignBehavior());
                // Phase 10 — Mortal AI under the same law. Campaign-AI shaping
                // across all eight Phase 7 kingdoms: no great kingdoms, fear
                // the night, fight for food, and NPC rosters trimmed toward
                // the same scarcity silhouette the player already lives under.
                // See MortalLaw/MortalLawCampaignBehavior.cs for the full design note.
                try { campaignStarter.AddBehavior(new MortalLawCampaignBehavior()); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Phase 11 — the clock of the apocalypse. The Night of the Hunt
                // (requirement 32), the day-300/600/1000 escalation stages, and
                // the Demon Lord's rise/victory/defeat (requirement 33). See
                // Apocalypse/ApocalypseCampaignBehavior.cs.
                try { campaignStarter.AddBehavior(new ApocalypseCampaignBehavior()); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { AshenDialogue.Register(campaignStarter);    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ElementalDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ArenicosDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { TempleDialogue.Register(campaignStarter);   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Sturgia is now the Wolf Brothers, not the (retired) Northmen —
                // see Factions/WolfBrothers/WolfBrothersDialogue.cs. NorthmenDialogue.cs
                // is left in place, unreferenced, for save compatibility.
                try { WolfBrothersDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Aserai is now the Tower, not the (retired) Duneborn — see
                // Factions/Tower/TowerDialogue.cs. DunebornDialogue.cs is left in
                // place, unreferenced, for save compatibility.
                try { TowerDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Battania is now the Hive, not the (retired) Forest Clans — see
                // Factions/Hive/HiveDialogue.cs. ForestClansCulture-adjacent
                // registrations are left in place, unreferenced, for save
                // compatibility.
                try { HiveDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Khuzait is now the Bloodbound, not the (retired) Tribes of the East —
                // see Factions/Bloodbound/BloodboundDialogue.cs. TribesDialogue.cs is
                // left in place, unreferenced, for save compatibility.
                try { BloodboundDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The Northern Empire is now simply "The Empire" — see
                // Factions/Empire/EmpireDialogue.cs.
                try { EmpireDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The Western Empire is now Legion — see Factions/Legion/LegionDialogue.cs.
                try { LegionDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The Southern Empire is now the Pale Widows — see Factions/PaleWidows/PaleWidowsDialogue.cs.
                try { PaleWidowsDialogue.Register(campaignStarter); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SchemeSystem.Initialize();              } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Drop the previous campaign's Ashen rolls before this one's data loads.
                // A save reload repopulates them in SyncData, which runs before
                // OnSessionLaunched; a NEW game leaves them empty until Initialize claims
                // the realm. Without this, a new game started without restarting the game
                // inherits the old campaign's clan list and renames the fresh world's
                // settlements at session launch, out of any clan's hands.
                try { AshenCitySystem.ResetForNewGame();      } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ExchangeCampaignBehavior.ResetState();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SeaCampaignBehavior.ResetForNewGame();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ClanOrdersCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SoldierServiceCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ElementalWildsBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { DemonSpawnCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { BattleEvents.ResetForNewGame();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { GreatAwakeningCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { NorthmenStonesCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SandboxOnlyGate.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SpellcasterLords.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SpellcasterTroopBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { SpellbookCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { HiveCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { BloodboundCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { TempleCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { EmpireCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { LegionCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { PaleWidowsCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { CityStateCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { RuinsCastleSystem.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { MortalLawCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ApocalypseCampaignBehavior.ResetForNewGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // A quest whose type is missing from the save definer only fails when the
                // player hits Save — long after the quest triggered. Audit at boot instead.
                try { AshAndEmberSaveDefiner.SelfCheck(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        // Game.Initialize() reloads all game texts from XML, which runs AFTER
        // OnGameStart and would wipe an earlier override. This hook fires after that
        // reload and before the character-creation screen is built, so it is the
        // correct point to rewrite the culture-card text (Vlandia IS The Holy Temple).
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            try { AshenCitySystem.ApplyTempleCultureTexts();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Sturgia is now the Wolf Brothers, not the (retired) Northmen.
            try { WolfBrothersCulture.ApplyWolfBrothersCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Aserai is now the Tower, not the (retired) Duneborn.
            try { TowerCulture.ApplyTowerCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Battania is now the Hive, not the (retired) Forest Clans — see
            // Factions/Hive/HiveCulture.cs. AshenCitySystem.ApplyForestClansCultureTexts
            // (and the Forest Clans rename helpers behind it) are left in place,
            // unreferenced, for save compatibility — only the call site moves.
            try { HiveCulture.ApplyHiveCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Khuzait is now the Bloodbound, not the (retired) Tribes of the East —
            // see Factions/Bloodbound/BloodboundCulture.cs. AshenCitySystem.ApplyTribalCultureTexts
            // (and the Tribal rename helpers behind it) are left in place,
            // unreferenced, for save compatibility — only the call site moves.
            try { BloodboundCulture.ApplyBloodboundCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Northern Empire is now simply "The Empire" — a fresh
            // identity, see Factions/Empire/EmpireCulture.cs.
            try { EmpireCulture.ApplyEmpireCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Phase 3 — Requirement 30a: tier 3-4 troop trees (every culture) are
            // re-equipped with the cheapest real armour of the same slot type;
            // Requirement 29: lords are stripped of gold/ornate/rich gear.
            try { GearWeathering.ApplyShabbyGearToTroopTrees(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { LordGearWeathering.ApplyToAllLords();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Re-applies the Templar culture text while still in the menu / intro-video /
        // character-creation flow (before the campaign map exists). Stops once the
        // campaign is actually running so it costs nothing during play.
        private static void EnsureTempleCultureTextPreGame()
        {
            object st = null;
            try { st = GameStateManager.Current?.ActiveState; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            bool preGame = Campaign.Current == null
                || st is TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreationState
                || st is VideoPlaybackState;
            if (preGame)
            {
                try { AshenCitySystem.ApplyTempleCultureTexts();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Sturgia is now the Wolf Brothers, not the (retired) Northmen.
            try { WolfBrothersCulture.ApplyWolfBrothersCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Aserai is now the Tower, not the (retired) Duneborn.
                try { TowerCulture.ApplyTowerCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Battania is now the Hive, not the (retired) Forest Clans.
                try { HiveCulture.ApplyHiveCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Khuzait is now the Bloodbound, not the (retired) Tribes of the East.
                try { BloodboundCulture.ApplyBloodboundCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The Northern Empire is now simply "The Empire".
                try { EmpireCulture.ApplyEmpireCultureTexts(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The character-creation culture cards cache their name when built, so
                // the text override above never reaches them — rename the card directly.
                try { TempleCultureCardFixer.TickTryFix(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // The backstory option VMs cache their labels the same way (they only
                // re-read on hover) — keep them synced with the narrative rewrites.
                try { NarrativeStageTextFixer.TickTryFix(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new MagicMissionBehavior());
        }

        protected override void OnApplicationTick(float dt)
        {
            // Vlandia IS The Holy Temple. Game.Initialize() reloads every game text
            // (restoring "Vlandian") AFTER our game-start hooks, and the culture card
            // is built a few frames later — after the intro-video state — by
            // LaunchSandboxCharacterCreation. The text override (a first-match replace)
            // is cheap and idempotent, so re-apply it every frame through the
            // pre-campaign flow, and do it BEFORE SkipIntroVideos hands off to the
            // character-creation screen, so the card is guaranteed to read "Templars".
            try { EnsureTempleCultureTextPreGame(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // Runs before the campaign gate below so it also fires at the main menu and
            // during the new-game flow, where Campaign.Current is still null.
            try { SkipIntroVideos(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshEmberSplash.Tick(dt); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshEmberLoreIntro.Tick(dt); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshEmberLoadingScreen.Tick(dt); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            try
            {
                if (Campaign.Current == null || Mission.Current != null) return;
                try { SandboxOnlyGate.Tick(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { MagicInputHandler.Tick(inMission: false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { MiracleInputHandler.Tick(inMission: false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { NatureInputHandler.Tick(inMission: false);  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                try { ActiveEffectManager.MapTick(dt); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // The spell-list keybind — Left Alt + L. Requirement 17: this now
                // opens the new Spellbook (a read-only list of every formula known)
                // once the player has unlocked it; the old element-learning Codex
                // still answers here too, alongside it, for a mage who has not yet
                // spent the focus point to open the book.
                try
                {
                    if (TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftAlt)
                        && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.L)
                        && MageKnowledge._deferredInquiry == null)
                    {
                        if (SpellbookCampaignBehavior.IsUnlocked)
                            MageKnowledge._deferredInquiry = SpellbookCampaignBehavior.ShowSpellbook;
                        else if (MageKnowledge.IsMage)
                            MageKnowledge._deferredInquiry = MagicLearning.ShowCodex;
                        else
                            MageKnowledge._deferredInquiry = SpellbookCampaignBehavior.ShowSpellbook;
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // Litany of Devotions — Left Shift + L opens the Grace talent list on the
                // map, for the faithful (not mages, not the land-attuned, not the dark).
                try
                {
                    if (!MageKnowledge.IsMage && !NatureKnowledge.IsAttuned && !DarkGiftSystem.HasAnyGift
                        && TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftShift)
                        && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.L)
                        && MageKnowledge._deferredInquiry == null)
                    {
                        MageKnowledge._deferredInquiry = MiracleTalents.ShowCodex;
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // Ctrl+Shift+F10 — toggle scheme debug mode
                try
                {
                    if (TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftControl)
                     && TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftShift)
                     && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F10))
                    {
                        SchemeSystem.DebugFree = !SchemeSystem.DebugFree;
                        MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject(
                            SchemeSystem.DebugFree
                                ? "[DEBUG] Schemes: costs disabled, success forced."
                                : "[DEBUG] Schemes: normal mode restored."));
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // Ctrl+Shift+F11 — debug combat trigger: warp nearest hostile to player (or spawn one)
                try
                {
                    if (TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftControl)
                     && TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftShift)
                     && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F11))
                    {
                        DebugTriggerCombat();
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // Ctrl+Shift+F9 — debug: spawn a Kindled (elemental) band beside the player
                try
                {
                    if (TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftControl)
                     && TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftShift)
                     && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F9))
                    {
                        ElementalWildsBehavior.DebugSpawnNearPlayer();
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                // Ctrl+Shift+F12 — debug grant: 100 fp, all dark gifts, max grace,
                // all nature talents, and one of each crystal. Nature must be granted
                // before dark gifts because GrantGift clears grace as a side effect.
                try
                {
                    if (TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftControl)
                     && TaleWorlds.InputSystem.Input.IsKeyDown(TaleWorlds.InputSystem.InputKey.LeftShift)
                     && TaleWorlds.InputSystem.Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F12))
                    {
                        DebugGrantAll();
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        // Skips TaleWorlds' intro cinematics — the logo reels at launch and, more
        // importantly, the campaign intro video that plays before character creation.
        // We simply finish any active VideoPlaybackState the moment it appears, which
        // is exactly what the engine's own "skip" button does, so the flow advances
        // cleanly to the next state. Guarded so a future engine change degrades to a
        // no-op rather than a crash.
        private static void SkipIntroVideos()
        {
            try
            {
                var active = GameStateManager.Current?.ActiveState;
                if (active is VideoPlaybackState video)
                    video.OnVideoFinished();
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }


        private static void DebugTriggerCombat()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main == null) return;

                // Look for the nearest party we're at war with
                MobileParty enemy = null;
                try
                {
                    var mainFaction = main.MapFaction;
                    if (mainFaction != null)
                    {
                        enemy = MobileParty.All
                            .Where(p => p != main && p.IsActive && !p.IsGarrison
                                && p.MapFaction != null
                                && FactionManager.IsAtWarAgainstFaction(p.MapFaction, mainFaction))
                            .OrderBy(p => (p.GetPosition2D - main.GetPosition2D).LengthSquared)
                            .FirstOrDefault();
                    }
                }
                catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

                if (enemy != null)
                {
                    MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject(
                        "[DEBUG] Enemy found nearby — engage to enter battle."));
                }
                else
                {
                    // No existing hostile — spawn a fresh Ashen ambush party
                    try
                    {
                        CampaignMapEvents.SpawnAshenAmbushNear(
                            main.GetPosition2D + new Vec2(0.1f, 0f), 30, 0f);
                    }
                    catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                    MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject(
                        "[DEBUG] No hostile found — Ashen ambush spawned nearby."));
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        private static void DebugGrantAll()
        {
            var hero = Hero.MainHero;
            if (hero == null) return;

            // 1. Nature magic — must come before dark gifts because GrantGift clears grace.
            try
            {
                TalentSystem.GrantFree(TalentId.NatureLivingRoot, hero);
                TalentSystem.GrantFree(TalentId.NatureStillDraw,  hero);
                TalentSystem.GrantFree(TalentId.NatureOpenGrip,   hero);
                TalentSystem.GrantFree(TalentId.Wildsworn,        hero);
                TalentSystem.GrantFree(TalentId.NatureDeepEarth,  hero);
                TalentSystem.GrantFree(TalentId.NatureDawnCall,   hero);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // 2. All Dark Gifts (DarkSpirit stacks to 3, grant it three times).
            try
            {
                foreach (DarkGiftId gift in Enum.GetValues(typeof(DarkGiftId)))
                {
                    if (gift == DarkGiftId.DarkSpirit)
                    {
                        DarkGiftSystem.GrantGift(DarkGiftId.DarkSpirit);
                        DarkGiftSystem.GrantGift(DarkGiftId.DarkSpirit);
                        DarkGiftSystem.GrantGift(DarkGiftId.DarkSpirit);
                    }
                    else
                    {
                        DarkGiftSystem.GrantGift(gift);
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // 3. Max grace — set directly because dark gifts would otherwise block AddGrace.
            try { MiracleInventory._grace = MiracleMath.GraceCap(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // 4. 100 focus points.
            try { hero.HeroDeveloper.UnspentFocusPoints += 100; } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // 5. Spellbook (Requirement 26): unlock it and learn every formula.
            //    No magical items exist yet (relics are Phase 6) — the crystal
            //    grant below already stands in for "magical items" per the
            //    prompt's own suggested fallback.
            try { SpellbookCampaignBehavior.DebugUnlockAll(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            // 6. One of every crystal into the player party's inventory.
            try
            {
                var roster = MobileParty.MainParty?.ItemRoster;
                if (roster != null)
                {
                    foreach (var def in CrystalCatalog.All)
                    {
                        var item = MBObjectManager.Instance?.GetObject<ItemObject>(def.ItemId);
                        if (item != null) roster.AddToCounts(item, 1);
                    }
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }

            MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject(
                "[DEBUG] Granted: 100 focus points, all Dark Gifts, max Grace, all Nature talents, all crystals, spellbook unlocked with every formula known."));
        }
    }

    // =========================================================================
    // MISSION BEHAVIOR
    // =========================================================================
    public class MagicMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private static readonly Random _rng = new Random();

        public override void OnMissionTick(float dt)
        {
            var mission = Mission.Current;
            if (mission == null || mission.CurrentState != Mission.State.Continuing) return;

            // Unified elemental magic (merges the old fire + nature battle input).
            ElementMagicInput.Tick(inMission: true, dt);
            ElementWallWards.Tick(dt);
            ElementSpellEffects.Tick(dt);
            ElementUltimates.Tick(dt);
            ElementalBeings.TickAuras(dt);
            DemonBattleBehavior.TickAuras(dt);
            CrystalEffects.MissionTick(dt);
            CrystalBattleAI.MissionTick(dt);
            MiracleInputHandler.Tick(inMission: true);
            NatureChargeBar.Tick(dt);
            MiracleEffects.MissionTick(dt);
            MiracleBattleAI.MissionTick(dt);
            NatureEffects.MissionTick(dt);
            NatureSeerAI.MissionTick(dt);
            NatureCharge.MissionTick(dt);
            SpellbookInputHandler.Tick(inMission: true);
            SpellburnEffects.Tick(dt);
            ActiveEffectManager.MissionTick(dt);
            ColourLordAI.MissionTick(dt);
            SpellcasterLords.MissionTick(dt);
            SpellcasterTroops.MissionTick(dt);
            SpellEffects.TickGlows(dt);
            SpellEffects.TickFocusVisuals(dt);
            SpellEffects.TickColourCooldown(dt);
            SpellEffects.TickAnimClears(dt);
            SpellEffects.TickCastLoops(dt);
            SpellEffects.TickPendingNpcCasts(dt);
            SpellEffects.TickMoves(dt);
            SpellEffects.TickAreaEffects(dt);
            SpellEffects.TickMissile(dt);
            SpellEffects.TickWard(dt);
            SpellEffects.TickStoneskin(dt);
            SpellEffects.TickSunder(dt);
            SpellEffects.TickChar(dt);
            SpellEffects.TickReflect(dt);
            SpellEffects.TickAttackWeaken(dt);
            SpellEffects.TickScorch(dt);
            SpellEffects.TickAshmark(dt);
            SpellEffects.TickInnerFireHeat(dt);
            SpellEffects.TickMagicMemory(dt);
            SpellEffects.TickHaltedAgents(dt);
            SpellEffects.TickDarkGifts(dt);
            RelicEffects.MissionTick(dt);
            SpellEffects.FlushPendingDeaths();
            BanditMageAI.MissionTick(dt);
            BattleEvents.MissionTick(dt);
            AshenSceneTone.MissionTick(dt);
            BattleWhispers.MissionTick(dt);
            HiveBattleEffects.MissionTick(dt);
        }

        protected override void OnEndMission()
        {
            try { SpellEffects.ClearAnimTimers();    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearCastLoops();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearPendingDeaths(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAreaEffects();   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearMissile();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearWard();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearStoneskin();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearSunder();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearChar();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearReflect();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAttackWeaken();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearScorch();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearAshmark();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearInnerFireHeat(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearMagicMemory();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearDarkGiftsBattleState(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearFocusVisuals();  } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearGlows();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearColourCooldown();} catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ClearMoves();         } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ColourLordAI.ClearCooldowns();            } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { BanditMageAI.OnMissionEnd();             } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AgingSystem.ClearKnockdowns();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ActiveEffectManager.ClearMissionEffects(); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementMagicInput.ResetInputState();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MagicInputHandler.ResetInputState();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CrystalEffects.ClearBattleState();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CrystalBattleAI.Reset();                    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleEffects.ClearBattleState();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleBattleAI.Reset();                    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleInputHandler.ResetInputState();      } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementWallWards.Clear();                   } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementSpellEffects.ClearBattleState();     } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementUltimates.ClearBattleState();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { ElementalBeings.ClearBattleState();          } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { DemonBattleBehavior.ClearBattleState();      } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureEffects.ClearBattleState();           } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureCharge.ClearForMission();             } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureChargeBar.Reset();                    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureSeerAI.ClearCooldowns();              } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { NatureInputHandler.ResetInputState();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellbookInputHandler.ResetInputState();    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellburnEffects.ClearBattleState();        } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { RelicEffects.ClearBattleState();            } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { BattleEvents.OnMissionEnd();               } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { AshenSceneTone.Reset();                    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { BattleWhispers.Reset();                    } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { GreatOtherParty.ClearMissionLatch();       } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { HiveBattleEffects.Reset();                 } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            // Witchy-ashen look (grey skin, cold-blue eyes, ragged armour
            // elements) for Ashen Spawn units, Ashen kingdom soldiers and
            // Ashen heroes.
            try { AshenVisuals.TryApply(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // All non-hero soldiers fighting for the Ashen (kingdom or Ashen player)
            // are called "Ashen Warrior" in battle — they have abandoned their old names.
            try { AshenVisuals.TryRenameToAshenWarrior(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Dark Gifts: apply persistent contour to player if gifts are active.
            try { SpellEffects.ApplyDarkGiftAgentBuild(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Kindled: if the player marched on a wild elemental band, remake
            // the enemy bodies into that kind (aura + weakness). No-op otherwise.
            try { ElementalBeings.ConvertBattleAgent(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Sacred-site-crafted Kindled fielded as real army troops: register
            // the same aura/weakness/self-cast behaviour by troop id. No-op for
            // every other troop.
            try { ElementalBeings.RegisterSacredKindled(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Night Tide: identifies a troops.xml demon id (Fiend/Stalker/
            // Ravager/Hellsteed) the moment its Agent is built, wreathes it in
            // DemonVisuals, scales its health for its region, and forces it to
            // charge. No-op for every other troop.
            try { DemonBattleBehavior.OnAgentBuild(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Great Awakening: the one Great Other champion, the moment its
            // own party's mission builds it. No-op unless this mission actually
            // involves that party (GreatOtherParty.OnMapEventStarted).
            try { GreatOtherParty.TryRegisterChampion(agent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent,
            in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            // Reflect enchantment: melee hits only — ranged weapons are excluded.
            bool isMeleeHit = true;
            try { isMeleeHit = affectorWeapon.IsEmpty || !(affectorWeapon.CurrentUsageItem?.IsRangedWeapon ?? false); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            if (isMeleeHit)
                try { SpellEffects.TryApplyReflect(affectedAgent, affectorAgent, blow.InflictedDamage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Unbinding (element ultimates): a struck flyer falls, a struck
            // channelling lord loses the working, the stone mantle drinks most of
            // the blow, and rain-soaked bowstrings cost a ranged hit its bite.
            try { ElementUltimates.OnAgentHit(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Fog: a ranged hit loosed from inside a standing fog bank loses half
            // its bite — the shooter cannot find the mark true through their own cloud.
            try { ElementSpellEffects.OnRangedHitThroughFog(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // The Kindled: stone and ice shatter under blunt force; blades pass
            // half-harmless through flame, tide and storm. Corrected after the blow.
            try { ElementalBeings.OnWeaponHit(affectedAgent, affectorAgent, blow.DamageType, blow.InflictedDamage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Sunder enchantment: applies to all hits (attacker is globally weakened).
            try { SpellEffects.TryApplyAttackWeakening(affectedAgent, affectorAgent, blow.InflictedDamage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Dark Gifts: passive on-hit effects for attacker and defender.
            try { SpellEffects.ApplyDarkGiftAttackEffects(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { SpellEffects.ApplyDarkGiftDefenseEffects(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { MiracleEffects.OnAgentHit(affectedAgent, affectorAgent, blow.InflictedDamage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { CrystalEffects.OnCrystalHit(affectedAgent, affectorAgent, affectorWeapon, blow.InflictedDamage); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Relics: Dark-Gift-sourced relic on-hit procs (weaker, per-item).
            try { RelicEffects.OnAgentHitAttack(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            try { RelicEffects.OnAgentHitDefense(affectedAgent, affectorAgent, blow.InflictedDamage, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Phase 7, Faction E — the Holy Sigil: small damage to nearby demons
            // on a landed blow, slight morale restore on a block/parry.
            try { TempleSigilEffects.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData, isMeleeHit); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
            // Nature resistance (reserved for future barrier talents): OnAgentHit fires after
            // damage is applied; heal back the mitigated portion against real weapon hits.
            try
            {
                if (affectedAgent != null && affectedAgent.IsActive()
                    && NatureEffects.HasResist(affectedAgent) && blow.InflictedDamage > 0)
                {
                    float kept      = NatureEffects.ApplyResistance(affectedAgent, blow.InflictedDamage);
                    float mitigated = blow.InflictedDamage - kept;
                    if (mitigated > 0f)
                        affectedAgent.Health = Math.Min(affectedAgent.HealthLimit, affectedAgent.Health + mitigated);
                }
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent,
            AgentState agentState, KillingBlow blow)
        {
            try
            {
                if (affectedAgent == null || affectedAgent.IsMount) return;
                if (agentState != AgentState.Killed) return;
                // Blood Pact gift: heal killer on kill (player and gifted NPC lords)
                try { SpellEffects.ApplyDarkGiftKillEffects(affectedAgent, affectorAgent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // MartyrsRefusal relic: weaker per-item BloodPact.
                try { RelicEffects.OnAgentKill(affectedAgent, affectorAgent); } catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
                // Ember passive (legacy): a kill sometimes repays the fire's debt
                if (affectorAgent == Agent.Main && MageKnowledge.IsMage && TalentSystem.Has(TalentId.Ember))
                    if (_rng.NextDouble() < 0.10)
                        AgingSystem.RestoreLifeExpectancy(Hero.MainHero, 1);
            }
            catch (System.Exception logEx) { AshAndEmber.ModLog.Error(logEx); }
        }
    }
}
