using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Patching;
using HarmonyLib;
using StardewModdingAPI;

namespace RadiationTierTools.Patches
{
    internal class UpgradeItems_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<StardewValley.Utility>("indexOfExtraMaterialForToolUpgrade"),
                prefix: this.GetHarmonyMethod(nameof(Before_RadiationTier))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Post Fix to make it so the player gets more money with the Antiquary profession

        [HarmonyLib.HarmonyPrefix]
        private static bool Before_RadiationTier(
        StardewValley.Utility __instance, ref int __result, int level)
        {
            int ResultToReplace;
            switch (level)
            {
                case 1:
                    ResultToReplace = 2500;
                    break;
                case 2:
                    ResultToReplace = 5000;
                    break;
                case 3:
                    ResultToReplace = 10000;
                    break;
                case 4:
                    ResultToReplace = 20000;
                    break;
                case 5:
                    ResultToReplace = 40000;
                    break;
                case 6:
                    ResultToReplace = 80000;
                    break;
                default:
                    ResultToReplace = 2500;
                    break;
            }
            __result = ResultToReplace;
            return false;
        }
    }
}
