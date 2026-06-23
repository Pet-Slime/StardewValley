using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace FruitTreeBugFix.Core
{
    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.placementAction))]
    class FruitTreePlacementAction_Patch
    {
        //Copied vanilla placement logic for fruit tree saplings, but uses CanPlantTreesHere so actual placement matches the preview better.
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(StardewValley.Object __instance, ref bool __result, GameLocation location, int x, int y, Farmer? who = null)
        {
            if (!__instance.IsFruitTreeSapling())
            {
                return true;
            }

            Vector2 vector = new Vector2(x / 64, y / 64);

            __instance.Location = location;
            __instance.TileLocation = vector;
            __instance.owner.Value = who?.UniqueMultiplayerID ?? Game1.player.UniqueMultiplayerID;

            if (location.getObjectAtTile((int)vector.X, (int)vector.Y) != null)
            {
                __result = false;
                return false;
            }

            bool removeHoeDirt = false;
            if (location.terrainFeatures.TryGetValue(vector, out TerrainFeature terrainFeature))
            {
                if (terrainFeature is not HoeDirt { crop: null })
                {
                    __result = false;
                    return false;
                }

                removeHoeDirt = true;
            }

            if (FruitTree.IsTooCloseToAnotherTree(vector, location))
            {
                Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13060"));
                __result = false;
                return false;
            }

            if (FruitTree.IsGrowthBlocked(vector, location))
            {
                Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:FruitTree_PlacementWarning", __instance.DisplayName));
                __result = false;
                return false;
            }

            bool flag2 = location.doesEitherTileOrTileIndexPropertyEqual((int)vector.X, (int)vector.Y, "CanPlantTrees", "Back", "T");
            if (location.IsNoSpawnTile(vector, "Tree") && !flag2)
            {
                __result = false;
                return false;
            }

            if (!removeHoeDirt && !location.CanItemBePlacedHere(vector, itemIsPassable: true))
            {
                __result = false;
                return false;
            }

            if (!location.CanPlantTreesHere(__instance.ItemId, (int)vector.X, (int)vector.Y, out string deniedMessage))
            {
                if (deniedMessage != null)
                {
                    Game1.showRedMessage(deniedMessage);
                }

                __result = false;
                return false;
            }

            string text3 = location.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back");

            if (removeHoeDirt)
            {
                location.terrainFeatures.Remove(vector);
            }

            location.playSound("dirtyHit");
            DelayedAction.playSoundAfterDelay("coin", 100);

            FruitTree fruitTree = new FruitTree(__instance.ItemId)
            {
                GreenHouseTileTree = (location.IsGreenhouse && text3 == "Stone")
            };
            fruitTree.growthRate.Value = Math.Max(1, __instance.Quality + 1);
            location.terrainFeatures.Add(vector, fruitTree);

            __result = true;
            return false;
        }
    }
}
