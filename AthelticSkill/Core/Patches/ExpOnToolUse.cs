using System.Collections.Generic;
using BirbCore.Attributes;
using HarmonyLib;
using SpaceCore;
using StardewValley;
using StardewValley.Tools;

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
            //Make sure the player isnt null and the tool isnt null
            if (who == null || __instance == null)
                return;

            Log.Trace($"-------------------------------");
            Log.Trace($"Athletic's tool use exp tracker");
            double checkValue = Game1.random.NextDouble();
            Log.Trace($"The check value is: {checkValue}");
            int baseExpChance = ModEntry.Config.ExpChanceFromTools;
            Log.Trace($"The base chance to get exp from using an atheltic's tool is: {baseExpChance}");
            int difference = (__instance.UpgradeLevel * 2) - who.GetCustomSkillLevel("moonslime.Athletic");
            Log.Trace($"The player's tool level is: {__instance.UpgradeLevel}");
            Log.Trace($"The player's athletic level is: {who.GetCustomSkillLevel("moonslime.Athletic")}");
            Log.Trace($"The difference between player's tools and level is {difference}");
            double finalChance = (baseExpChance + (difference*10)) / 100;
            Log.Trace($"Final chance is: {finalChance}");


            Log.Trace($"Does the check fail? {checkValue >= finalChance}");
            if (checkValue >= finalChance)
                return;

            Log.Trace($"Check passed, EXP is gained!");
            int expToAdd = 0;

            if (__instance.HasContextTag("moonslime.Athletics.light_tool"))
                expToAdd = ModEntry.Config.ExpFromLightToolUse;
            else if (__instance.HasContextTag("moonslime.Athletics.heavy_tool"))
                expToAdd = ModEntry.Config.ExpFromHeavyToolUse;

            Log.Trace($"Total exp gained is: {expToAdd}");
            Log.Trace($"-------------------------------");
            if (expToAdd > 0)
                Utilities.AddEXP(who, expToAdd);
        }
    }
}
