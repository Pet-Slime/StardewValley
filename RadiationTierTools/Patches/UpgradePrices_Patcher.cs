using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Patching;
using MoonShared;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace RadiationTierTools.Patches
{
    internal class UpgradePrices_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<StardewValley.Utility>("priceForToolUpgradeLevel"),
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
                    ResultToReplace = 334;
                    break;
                case 2:
                    ResultToReplace = 335;
                    break;
                case 3:
                    ResultToReplace = 336;
                    break;
                case 4:
                    ResultToReplace = 337;
                    break;
                case 5:
                    ResultToReplace = 910;
                    break;
                case 6:
                    ResultToReplace = 852;
                    break;
                default:
                    ResultToReplace = 334;
                    break;
            }
            __result = ResultToReplace;
            return false;
        }
    }
}

