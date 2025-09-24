using System;
using System.Collections.Generic;
using System.Linq;
using BibliocraftSkill.Objects.Book_Restoration_Table;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace BibliocraftSkill.Objects.Book_Restoration_Table
{
    internal static class Patches
    {
        internal static IMonitor IMonitor => ModEntry.Instance.Monitor;

        internal static readonly BookRestorationTable _instance = new();

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
                if (__instance.ItemId == "moonslime.BibliocraftSkill.Book_Restoration")
                {
                    Point tile = new((int)Math.Floor(x / 64f), (int)Math.Floor(y / 64f));
                    __result = new BookRestorationTable(new(tile.X, tile.Y)).placementAction(location, x, y, who);
                    if (__result && __instance.Stack <= 0)
                        Game1.player.removeItemFromInventory(__instance);
                    return false;
                }
                return true;
            }
            catch (Exception ex) { return HandleError($"Object.{nameof(Object.placementAction)}", ex, __instance?.ItemId != "moonslime.BibliocraftSkill.Book_Restoration"); }
        }





        private static bool HandleError(string source, Exception ex, bool result)
        {
            IMonitor.Log($"Faild patching {source}", LogLevel.Error);
            IMonitor.Log($"{ex.Message}\n{ex.StackTrace}");
            return result;
        }
    }
}
