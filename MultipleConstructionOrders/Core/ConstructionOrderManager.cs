using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

namespace MultipleConstructionOrders.Core
{
    internal static class ConstructionOrderManager
    {
        /*********
        ** Fields
        *********/
        private const string ModDataPrefix = "moonslime.MultipleConstructionOrders";

        private const string RobinOrderDayKey = ModDataPrefix + "/RobinOrderDay";
        private const string WorkerKindKey = ModDataPrefix + "/WorkerKind";
        private const string WorkerAssignedDayKey  = ModDataPrefix + "/ConstructionSeenDay";

        private const string WorkerKindRobin = "Robin";
        private const string WorkerKindConstructionWorker = "ConstructionWorker";

        private const string WorkerNpcNamePrefix = "moonslime_MCO_ConstructionWorker_";

        // This should match your asset path.
        private const string ConstructionWorkerSpriteAsset = "Mods/moonslime.MultipleConstructionOrders/textures";


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether Robin should be allowed to accept construction orders today.</summary>
        public static bool CanRobinAcceptOrdersToday()
        {
            // If there are no active Robin constructions, Robin is free.
            if (!HasActiveRobinConstruction())
                return true;

            // If the player already started ordering today, keep the ordering window open for today.
            return IsRobinOrderDay();
        }

        /// <summary>Mark today as a day where Robin accepted construction orders.</summary>
        public static void MarkRobinOrderPlacedToday()
        {
            Game1.MasterPlayer.modData[RobinOrderDayKey] = Game1.Date.TotalDays.ToString();
        }

        /// <summary>Clear the same-day ordering window.</summary>
        public static void ClearRobinOrderDay()
        {
            Game1.MasterPlayer.modData.Remove(RobinOrderDayKey);
        }

        /// <summary>Assign one active construction job to Robin, and the rest to temporary construction workers.</summary>
        public static void AssignConstructionWorkers()
        {
            if (!ContextSafe())
                return;

            List<Building> activeBuildings = GetActiveRobinConstructionBuildings();

            if (activeBuildings.Count == 0)
                return;

            Building robinTarget = null;

            // Prefer keeping Robin assigned to the same building if that building is still active.
            foreach (Building building in activeBuildings)
            {
                if (building.modData.TryGetValue(WorkerKindKey, out string workerKind) && workerKind == WorkerKindRobin)
                {
                    robinTarget = building;
                    break;
                }
            }

            // If Robin has no active assigned building, assign her to the first active building.
            robinTarget ??= activeBuildings[0];

            int currentDay = Game1.Date.TotalDays;

            foreach (Building building in activeBuildings)
            {
                building.modData[WorkerKindKey] = building == robinTarget
                    ? WorkerKindRobin
                    : WorkerKindConstructionWorker;

                if (!building.modData.ContainsKey(WorkerAssignedDayKey ))
                    building.modData[WorkerAssignedDayKey ] = currentDay.ToString();
            }
        }

        /// <summary>Remove stale construction tags from finished buildings.</summary>
        public static void CleanFinishedConstructionTags()
        {
            foreach (GameLocation location in Game1.locations.ToArray())
            {
                if (location?.buildings == null)
                    continue;

                foreach (Building building in location.buildings.ToArray())
                {
                    if (building == null)
                        continue;

                    if (IsActiveConstruction(building))
                        continue;

                    building.modData.Remove(WorkerKindKey);
                    building.modData.Remove(WorkerAssignedDayKey );
                }
            }
        }

        /// <summary>Spawn temporary construction worker NPCs for active non-Robin construction jobs.</summary>
        public static void SpawnTemporaryWorkers()
        {
            if (!Game1.IsMasterGame)
                return;

            RemoveTemporaryWorkers();

            if (!IsWorkingDay())
                return;

            AssignConstructionWorkers();

            int index = 0;

            foreach (Building building in GetActiveRobinConstructionBuildings())
            {
                if (!building.modData.TryGetValue(WorkerKindKey, out string workerKind))
                    continue;

                // Vanilla Robin should handle the building tagged as Robin.
                if (workerKind == WorkerKindRobin)
                    continue;

                SpawnTemporaryWorkerForBuilding(building, index++);
            }
        }

        /// <summary>Remove this mod's temporary construction worker NPCs from all locations, including building interiors.</summary>
        public static void RemoveTemporaryWorkers()
        {
            foreach (GameLocation location in Game1.locations.ToArray())
            {
                if (location?.characters == null)
                    continue;

                for (int i = location.characters.Count - 1; i >= 0; i--)
                {
                    NPC npc = location.characters[i];

                    if (npc?.Name != null && npc.Name.StartsWith(WorkerNpcNamePrefix, StringComparison.Ordinal))
                        location.characters.RemoveAt(i);
                }
            }
        }

        /// <summary>Try to get the construction building Robin should work on.</summary>
        public static bool TryGetRobinConstructionBuilding(out Building building)
        {
            building = null;

            // First look for the building explicitly tagged for Robin.
            foreach (Building activeBuilding in GetActiveRobinConstructionBuildings())
            {
                if (activeBuilding.modData.TryGetValue(WorkerKindKey, out string workerKind) && workerKind == WorkerKindRobin)
                {
                    building = activeBuilding;
                    return true;
                }
            }

            // Fallback: if tags are missing for any reason, use the first active Robin construction.
            List<Building> activeBuildings = GetActiveRobinConstructionBuildings();

            if (activeBuildings.Count > 0)
            {
                building = activeBuildings[0];
                return true;
            }

            return false;
        }

        /// <summary>Get whether any Robin construction job is active anywhere.</summary>
        public static bool HasActiveRobinConstruction()
        {
            foreach (GameLocation location in Game1.locations.ToArray())
            {
                if (location?.buildings == null)
                    continue;

                foreach (Building building in location.buildings.ToArray())
                {
                    if (IsActiveRobinConstruction(building))
                        return true;
                }
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        private static bool ContextSafe()
        {
            return Game1.hasLoadedGame && Game1.MasterPlayer != null;
        }

        private static bool IsRobinOrderDay()
        {
            return Game1.MasterPlayer.modData.TryGetValue(RobinOrderDayKey, out string rawDay)
                && int.TryParse(rawDay, out int orderDay)
                && orderDay == Game1.Date.TotalDays;
        }

        private static List<Building> GetActiveRobinConstructionBuildings()
        {
            List<Building> buildings = new();

            foreach (GameLocation location in Game1.locations.ToArray())
            {
                if (location?.buildings == null)
                    continue;

                foreach (Building building in location.buildings.ToArray())
                {
                    if (IsActiveRobinConstruction(building))
                        buildings.Add(building);
                }
            }

            return buildings;
        }

        private static bool IsActiveRobinConstruction(Building building)
        {
            if (building == null || !IsActiveConstruction(building))
                return false;

            BuildingData data = building.GetData();

            // Vanilla farm buildings are usually Robin-built. If data is missing, treat it as Robin-built.
            if (data == null)
                return true;

            // If the building data explicitly uses a non-Robin builder, don't touch it.
            if (!string.IsNullOrEmpty(data.Builder) && data.Builder != Game1.builder_robin && data.Builder != "Robin")
                return false;

            return true;
        }

        private static bool IsActiveConstruction(Building building)
        {
            return building != null
                && (building.daysOfConstructionLeft.Value > 0 || building.daysUntilUpgrade.Value > 0);
        }

        private static bool IsWorkingDay()
        {
            if (Utility.isFestivalDay())
                return false;

            if (Game1.isGreenRain && Game1.year == 1)
                return false;

            return true;
        }

        private static void SpawnTemporaryWorkerForBuilding(Building building, int index)
        {
            GameLocation workerLocation = building.GetParentLocation();

            if (workerLocation == null)
                return;

            Vector2 tilePosition;
            Vector2 positionOffset = Vector2.Zero;

            // Upgrades can place the worker inside the building, like vanilla Robin does.
            if (building.daysUntilUpgrade.Value > 0 && building.GetIndoors() != null)
            {
                workerLocation = building.GetIndoors();

                string indoorsName = building.GetIndoorsName();

                if (indoorsName != null && indoorsName.StartsWith("Shed", StringComparison.Ordinal))
                {
                    tilePosition = new Vector2(2f, 2f);
                    positionOffset = new Vector2(-28f, 0f);
                }
                else
                {
                    tilePosition = new Vector2(1f, 5f);
                }
            }
            else
            {
                tilePosition = new Vector2(
                    building.tileX.Value + building.tilesWide.Value / 2,
                    building.tileY.Value + building.tilesHigh.Value / 2
                );

                positionOffset = new Vector2(16f, -32f);
            }

            NPC worker = new(
                new AnimatedSprite(ConstructionWorkerSpriteAsset, 0, 16, 32),
                tilePosition * 64f,
                2,
                WorkerNpcNamePrefix + index,
                null
            );

            worker.Position += positionOffset;
            worker.currentLocation = workerLocation;
            worker.ignoreScheduleToday = true;
            worker.datable.Value = false;

            ApplyBuildingAnimation(worker, workerLocation);

            if (!workerLocation.characters.Contains(worker))
                workerLocation.characters.Add(worker);
        }

        private static void ApplyBuildingAnimation(NPC worker, GameLocation location)
        {
            AnimatedSprite.endOfAnimationBehavior hammerSound = delegate
            {
                if (!Utility.isOnScreen(worker.TilePoint, 3, location))
                    return;

                location.playSound(Game1.random.NextDouble() < 0.1 ? "clank" : "axchop", null, null, SoundContext.Default);
                worker.shakeTimer = 250;
            };

            worker.Sprite.CurrentAnimation = new List<FarmerSprite.AnimationFrame>
                {
                    new FarmerSprite.AnimationFrame(0, 90),
                    new FarmerSprite.AnimationFrame(1, 90),
                    new FarmerSprite.AnimationFrame(2, 260, false, false, hammerSound, false),
                    new FarmerSprite.AnimationFrame(3, Game1.random.Next(700, 1600))
                };

            worker.Sprite.loop = true;
        }



    }
}
