using System;
using System.Collections.Generic;
using System.Linq;
using ArchaeologySkill.Objects;
using ArchaeologySkill.Objects.Restoration_Table;
using MoonShared.APIs;
using MoonShared.Attributes;
using MoonSharedSpaceCore;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ArchaeologySkill.Core
{
    [SEvent]
    public class Events
    {

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {

            var sc = ModEntry.Instance.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(RestorationTable));
            ArchaeologySkill.Objects.Restoration_Table.Patches.Patch(ModEntry.Instance.Helper);

            ModEntry.ItemDefinitions = ModEntry.Assets.ItemDefinitions;


            Log.Trace("Archaeology: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Archaeology_Skill());

            foreach (string entry in ModEntry.ItemDefinitions["extra_loot_table"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the bonus loot table");
                ModEntry.BonusLootTable.Add(entry);
            }
            foreach (string entry in ModEntry.ItemDefinitions["waterShifter_loot_table"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the water shifter loot table");
                ModEntry.WaterSifterLootTable.Add(entry);
            }
            foreach (string entry in ModEntry.ItemDefinitions["extra_loot_table_GI"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the bonus loot table");
                ModEntry.BonusLootTable_GI.Add(entry);
            }
            foreach (string entry in ModEntry.ItemDefinitions["waterShifter_loot_table_GI"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the water shifter loot table");
                ModEntry.WaterSifterLootTable_GI.Add(entry);
            }
            foreach (var kvp in Game1.objectData)
            {
                if (kvp.Value.Type == "Arch")
                    ModEntry.ArtifactLootTable.Add(kvp.Key);
            }


            Log.Trace("Archaeology: Do I see XP display?");
            if (ModEntry.XPDisplayLoaded)
            {
                Log.Trace("Archaeology: I do see XP display, Registering API.");
                ModEntry.XpAPI = ModEntry.Instance.Helper.ModRegistry.GetApi<IXPDisplayApi>("Shockah.XPDisplay");
            }
        }

        [SEvent.DayStarted]
        private void DayStarted(object sender, DayStartedEventArgs e)
        {

            if (Game1.IsMasterGame)
            {
                int extraArtifactSpot = 0;

                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    Log.Trace("Archaeology: Does a player have Pioneer Profession?");
                    var player = Game1.GetPlayer(farmer.UniqueMultiplayerID);
                    if (player.isActive() && player.HasCustomProfession(Archaeology_Skill.Archaeology5a))
                    {
                        Log.Trace("Archaeology: They do have Pioneer profession, spawn extra artifact spots.");
                        extraArtifactSpot += 2;
                        Log.Trace("Archaeology: extra artifact spot chance increased by: " + extraArtifactSpot.ToString());
                    }
                }

                if (extraArtifactSpot != 0)
                {
                    SpawnDiggingSpots(extraArtifactSpot);
                }
            }
        }

        [SEvent.StatChanged("moonslime.ArchaeologySkill.Restoration_Table")]
        private void StatChanged_Restoration_Table(object sender, SEvent.StatChanged.EventArgs e)
        {
            ///            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromResearchTable);
        }

        [SEvent.StatChanged("moonslime.ArchaeologySkill.Ancient_Battery")]
        private void StatChanged_ancient_battery(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromAncientBattery);
        }

        [SEvent.StatChanged("moonslime.ArchaeologySkill.preservation_chamber")]
        private void StatChanged_preservation_chamber(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromPreservationChamber);
        }

        [SEvent.StatChanged("moonslime.ArchaeologySkill.h_preservation_chamber")]
        private void StatChanged_h_preservation_chamber(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromHPreservationChamber);
        }

        [SEvent.StatChanged("moonslime.ArchaeologySkill.water_shifter")]
        private void StatChanged_water_shifter(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromWaterShifter);
        }

        [SEvent.StatChanged("GeodesCracked")]
        private void StatChanged_geodesCracked(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, 1);
        }


        [SEvent.SaveLoaded]
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {

            if (ModEntry.XpAPI is not null)
            {
                Log.Trace("Archaeology: XP display found, Marking Hoe and Pan as Skill tools");
                ModEntry.XpAPI.RegisterToolSkillMatcher(ModEntry.Instance.ToolSkillMatchers[0]);
                ModEntry.XpAPI.RegisterToolSkillMatcher(ModEntry.Instance.ToolSkillMatchers[1]);
            }

            foreach (Farmer player in Game1.getAllFarmers())
            {
                SpaceUtilities.LearnRecipesOnLoad(Game1.GetPlayer(player.UniqueMultiplayerID), ModEntry.SkillID);
            }
        }

        private static void SpawnDiggingSpots(int extraSpots)
        {
            if (extraSpots <= 0) return;

            var random = Game1.random;
            int spotsPlaced = 0;

            // Get all valid maps: outdoor, non-farm, and shuffle randomly
            List<GameLocation> validLocations = Game1.locations
                .Where(loc => loc.IsOutdoors && !loc.IsFarm)
                .OrderBy(_ => random.Next())
                .ToList();

            Log.Trace($"[Archaeology] Attempting to spawn up to {extraSpots} extra digging spots today.");

            foreach (GameLocation location in validLocations)
            {
                if (spotsPlaced >= extraSpots)
                {
                    Log.Trace("[Archaeology] Reached global limit of extra spots. Stopping.");
                    break;
                }

                // Count existing artifact/seed spots on this map
                int existingSpots = location.objects.Pairs.Count(kvp =>
                    kvp.Value.QualifiedItemId == "(O)590" || kvp.Value.QualifiedItemId == "(O)SeedSpot");

                Log.Trace($"[Archaeology] Checking map '{location.Name}': {existingSpots} existing spots.");

                // Skip maps with 4 or more existing spots
                if (existingSpots >= 4)
                {
                    Log.Trace($"[Archaeology] Skipping map '{location.Name}' because it already has {existingSpots} spots.");
                    continue;
                }

                int attempts = 0;

                // Try up to 2 times per map
                while (attempts < 2 && spotsPlaced < extraSpots)
                {
                    attempts++;
                    int x = random.Next(location.Map.DisplayWidth / Game1.tileSize);
                    int y = random.Next(location.Map.DisplayHeight / Game1.tileSize);
                    Vector2 tile = new Vector2(x, y);

                    // Check vanilla-style tile placement rules
                    bool validTile = location.CanItemBePlacedHere(tile, false, CollisionMask.All, ~CollisionMask.Objects, false, false)
                                     && !location.IsTileOccupiedBy(tile, CollisionMask.All, CollisionMask.None, false)
                                     && !location.hasTileAt(x, y, "AlwaysFront", null)
                                     && !location.hasTileAt(x, y, "Front", null)
                                     && !location.isBehindBush(tile)
                                     && (location.doesTileHaveProperty(x, y, "Diggable", "Back", false) != null
                                         || (location.GetSeason() == Season.Winter
                                             && location.doesTileHaveProperty(x, y, "Type", "Back", false) != null
                                             && location.doesTileHaveProperty(x, y, "Type", "Back", false).Equals("Grass")));

                    // Forest special case
                    if (location.Name.Equals("Forest") && x >= 93 && y <= 22)
                        validTile = false;

                    if (!validTile)
                    {
                        Log.Trace($"[Archaeology] Attempt {attempts} failed on map '{location.Name}' at tile ({x},{y}). Invalid location.");
                        continue;
                    }

                    location.objects.Add(tile, ItemRegistry.Create<Object>("(O)590", 1, 0, false));

                    spotsPlaced++;
                    Log.Trace($"[Archaeology] Spawned 'artifact spot' on map '{location.Name}' at tile ({x},{y}). Total placed: {spotsPlaced}/{extraSpots}.");

                    break; // Stop after successfully placing one spot in this map
                }
            }

            Log.Trace($"[Archaeology] Finished spawning extra digging spots. Total spots placed today: {spotsPlaced}/{extraSpots}.");
        }

    }
}
