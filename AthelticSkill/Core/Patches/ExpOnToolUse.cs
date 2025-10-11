using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley.Tools;
using StardewValley;
using BirbCore.Attributes;

namespace AthleticSkill.Core.Patches
{

    [HarmonyPatch(typeof(StardewValley.Item), "_PopulateContextTags")]
    class PopulateContextTags_patch
    {
        [HarmonyPostfix]
        public static void Postfix(StardewValley.Item __instance, ref HashSet<string> tags)
        {
            switch (__instance)
            {
                case Pickaxe:
                case Axe:
                    tags.Add("moonslime.Athletics.heavy_tool");
                    break;

                case FishingRod:
                case Hoe:
                case MilkPail:
                case Shears:
                case WateringCan:
                    tags.Add("moonslime.Athletics.light_tool");
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(Tool), nameof(Tool.DoFunction))]
    public static class ToolExpPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tool __instance, Farmer who)
        {
            if (who == null || __instance == null)
                return;

            if (Game1.random.NextDouble() >= ModEntry.Config.ExpChanceFromTools / 100.0)
                return;

            int expToAdd = 0;

            if (__instance.HasContextTag("moonslime.Athletics.light_tool"))
                expToAdd = ModEntry.Config.ExpFromLightToolUse;
            else if (__instance.HasContextTag("moonslime.Athletics.heavy_tool"))
                expToAdd = ModEntry.Config.ExpFromHeavyToolUse;

            if (expToAdd > 0)
                Utilities.AddEXP(who, expToAdd);
        }
    }
}
