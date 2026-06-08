using HarmonyLib;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace MultipleConstructionOrders.Core
{
    /// <summary>
    /// Lets Robin accept multiple construction orders only on valid ordering days,
    /// while still making Robin count as busy once construction is active.
    /// </summary>
    [HarmonyPatch(typeof(Game1), nameof(Game1.IsThereABuildingUnderConstruction))]
    public static class Game1_IsThereABuildingUnderConstruction_Patch
    {
        public static void Postfix(string builder, ref bool __result)
        {
            if (builder != Game1.builder_robin && builder != "Robin")
                return;

            // On a valid ordering day, lie to the carpenter menu and say Robin isn't busy.
            if (Events.RobinCanAcceptConstructionOrdersToday)
            {
                __result = false;
                return;
            }

            // If vanilla lost its builder state but construction still exists,
            // force Robin to count as busy so she keeps working until all construction is done.
            if (ConstructionOrderManager.TryGetRobinConstructionBuilding(out _))
                __result = true;
        }
    }

    /// <summary>
    /// Makes vanilla Robin's construction animation target this mod's assigned Robin building.
    /// This prevents Robin from going home while tagged construction is still active.
    /// </summary>
    [HarmonyPatch(typeof(Game1), nameof(Game1.GetBuildingUnderConstruction))]
    public static class Game1_GetBuildingUnderConstruction_Patch
    {
        public static bool Prefix(string builder, ref Building __result)
        {
            if (builder != Game1.builder_robin && builder != "Robin")
                return true;

            if (ConstructionOrderManager.TryGetRobinConstructionBuilding(out Building building))
            {
                __result = building;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Marks the current day as a valid multi-order day after Robin successfully accepts construction.
    /// Also immediately assigns worker tags so reloading the save on the same day keeps the order state.
    /// </summary>
    [HarmonyPatch(typeof(CarpenterMenu), nameof(CarpenterMenu.tryToBuild))]
    public static class CarpenterMenu_tryToBuild_Patch
    {
        public static void Postfix(CarpenterMenu __instance, bool __result)
        {
            if (!__result)
                return;

            if (__instance.Builder != Game1.builder_robin && __instance.Builder != "Robin")
                return;

            ConstructionOrderManager.MarkRobinOrderPlacedToday();
            Events.RefreshConstructionState();
        }
    }
}
