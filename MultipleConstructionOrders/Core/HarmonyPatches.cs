using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace MultipleConstructionOrders.Core
{
    /// <summary>
    /// Lets Robin accept multiple construction orders only on days
    /// where no construction was active at the start of the day.
    /// </summary>
    [HarmonyPatch(typeof(Game1), nameof(Game1.IsThereABuildingUnderConstruction))]
    public static class Game1_IsThereABuildingUnderConstruction_Patch
    {
        public static void Postfix(string builder, ref bool __result)
        {
            if (builder != Game1.builder_robin && builder != "Robin")
                return;

            if (!__result)
                return;

            if (Events.RobinCanAcceptConstructionOrdersToday)
                __result = false;
        }
    }
}
