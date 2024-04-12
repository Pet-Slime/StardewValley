using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace ArchaeologySkill.Objects.Water_Shifter
{
    internal static class Patches
    {
        internal static IMonitor IMonitor => ModEntry.Instance.Monitor;

        internal static readonly WaterShifter _instance = new();

        internal static void Patch(IModHelper helper)
        {
            Harmony harmony = new(helper.ModRegistry.ModID);

            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.placementAction)),
                prefix: new(typeof(Patches), nameof(placementActionPrefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.canBePlacedHere)),
                prefix: new(typeof(Patches), nameof(canBePlacedHerePrefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.isPlaceable)),
                prefix: new(typeof(Patches), nameof(isPlaceablePrefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.drawPlacementBounds)),
                postfix: new(typeof(Patches), nameof(drawPlacementBoundsPostfix))
            );
        }

        internal static bool placementActionPrefix(Object __instance, ref bool __result, GameLocation location, int x, int y, Farmer who = null)
        {
            try
            {
                if (__instance.ItemId == ModEntry.ObjectInfo.Id)
                {
                    Point tile = new((int)Math.Floor(x / 64f), (int)Math.Floor(y / 64f));
                    if (!WaterShifter.IsValidPlacementLocation(location, tile.X, tile.Y))
                        return false;
                    __result = new WaterShifter(new(tile.X, tile.Y)).placementAction(location, x, y, who);
                    if (__result && __instance.Stack <= 0)
                        Game1.player.removeItemFromInventory(__instance);
                    return false;
                }
                return true;
            }
            catch (Exception ex) { return handleError($"Object.{nameof(Object.placementAction)}", ex, __instance?.ItemId != ModEntry.ObjectInfo.Id); }
        }

        internal static bool canBePlacedHerePrefix(Object __instance, ref bool __result, GameLocation l, Vector2 tile)
        {
            try
            {
                if (__instance.ItemId == ModEntry.ObjectInfo.Id)
                {
                    __result = WaterShifter.IsValidPlacementLocation(l, (int)tile.X, (int)tile.Y);
                    return false;
                }
                return true;
            }
            catch (Exception ex) { return handleError($"Object.{nameof(Object.canBePlacedHere)}", ex, true); }
        }

        internal static bool isPlaceablePrefix(Object __instance, ref bool __result)
        {
            try
            {
                if (__instance.ItemId == ModEntry.ObjectInfo.Id)
                {
                    __result = _instance.isPlaceable();
                    return false;
                }
                return true;
            }
            catch (Exception ex) { return handleError($"Object.{nameof(Object.isPlaceable)}", ex, true); }
        }

        internal static void drawPlacementBoundsPostfix(Object __instance, SpriteBatch spriteBatch, GameLocation location)
        {
            try
            {
                if (__instance.ItemId != ModEntry.ObjectInfo.Id)
                    return;
                int X = (int)Game1.GetPlacementGrabTile().X * 64;
                int Y = (int)Game1.GetPlacementGrabTile().Y * 64;
                Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
                if (Game1.isCheckingNonMousePlacement)
                {
                    Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(Game1.player, location, _instance, X, Y);
                    X = (int)nearbyValidPlacementPosition.X;
                    Y = (int)nearbyValidPlacementPosition.Y;
                }
                _instance.draw(spriteBatch, X / 64, Y / 64, 0.5f);
            }
            catch (Exception ex) { handleError($"Object.{nameof(Object.drawWhenHeld)}", ex, false); }
        }



        private static bool handleError(string source, Exception ex, bool result)
        {
            IMonitor.Log($"Faild patching {source}", LogLevel.Error);
            IMonitor.Log($"{ex.Message}\n{ex.StackTrace}");
            return result;
        }
    }
}
