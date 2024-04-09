using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using MoonShared;
using StardewModdingAPI;
using MoonShared.Patching;

namespace ArchaeologySkill
{
    internal class CheckForBuriedItem_IslandLocation_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<IslandLocation>("checkForBuriedItem"),
                prefix: this.GetHarmonyMethod(nameof(After_Buried_Nut_EXP))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Post Fix to make it so the player gets more money with the Antiquary profession

        [HarmonyLib.HarmonyPrefix]
        private static void After_Buried_Nut_EXP(
        IslandLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            Log.Trace("Archaeology skill check for buried treasure: Island");
            Log.Trace(__instance.IsBuriedNutLocation(new Point(xLocation, yLocation)).ToString());
            if (__instance.IsBuriedNutLocation(new Point(xLocation, yLocation)))
            {
                Log.Trace("Has the team collected said nut?");
                Log.Trace(Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation).ToString());
                if (Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation) == false)
                {
                    Log.Trace("The Team has not collected said not, award the player bonus exp!");
                    Utilities.ApplyArchaeologySkill(who);
                }
            }
        }
    }
}
