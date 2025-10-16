using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using Object = StardewValley.Object;

namespace ArchaeologySkill.Objects.Restoration_Table
{
    internal static class Patches
    {
        internal static IMonitor IMonitor => ModEntry.Instance.Monitor;

        internal static readonly RestorationTable _instance = new();

        internal static void Patch(IModHelper helper)
        {
            Harmony harmony = new(helper.ModRegistry.ModID);

            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.placementAction)),
                prefix: new(typeof(Patches), nameof(PlacementActionPrefix))
            );
        }

        internal static bool PlacementActionPrefix(Object __instance, ref bool __result, GameLocation location, int x, int y, Farmer who = null)
        {
            try
            {
                if (__instance.ItemId == "moonslime.Archaeology.restoration_table")
                {
                    Point tile = new((int)Math.Floor(x / 64f), (int)Math.Floor(y / 64f));
                    __result = new RestorationTable(new(tile.X, tile.Y)).placementAction(location, x, y, who);
                    if (__result && __instance.Stack <= 0)
                        Game1.player.removeItemFromInventory(__instance);
                    return false;
                }
                return true;
            }
            catch (Exception ex) { return HandleError($"Object.{nameof(Object.placementAction)}", ex, __instance?.ItemId != "moonslime.Archaeology.restoration_table"); }
        }





        private static bool HandleError(string source, Exception ex, bool result)
        {
            IMonitor.Log($"Faild patching {source}", LogLevel.Error);
            IMonitor.Log($"{ex.Message}\n{ex.StackTrace}");
            return result;
        }
    }
}
