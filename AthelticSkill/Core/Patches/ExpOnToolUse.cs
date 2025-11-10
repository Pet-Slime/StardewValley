using System;
using System.Collections.Generic;
using MoonShared.Attributes;
using HarmonyLib;
using SpaceCore;
using StardewValley;
using StardewValley.Tools;

namespace AthleticSkill.Core.Patches
{
    /// <summary>
    /// Adds custom context tags to tools for Athletics skill categorization.
    /// </summary>
    [HarmonyPatch(typeof(StardewValley.Item), "_PopulateContextTags")]
    class PopulateContextTags_patch
    {
        [HarmonyPostfix]
        public static void Postfix(StardewValley.Item __instance, ref HashSet<string> tags)
        {
            // Categorize tools based on their "weight" or effort required
            switch (__instance)
            {
                // Heavy tools: Pickaxe and Axe
                case Pickaxe:
                case Axe:
                    tags.Add("moonslime.Athletics.heavy_tool");
                    break;

                // Light tools: FishingRod, Hoe, MilkPail, Shears, WateringCan
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

    /// <summary>
    /// Awards Athletics skill experience when using tools.
    /// </summary>
    [HarmonyPatch(typeof(Tool), nameof(Tool.DoFunction))]
    public static class ToolExpPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tool __instance, Farmer who)
        {
            // --- Validate inputs ---
            if (who == null || __instance == null)
                return;

            bool isLightTool = __instance.HasContextTag("moonslime.Athletics.light_tool");
            bool isHeavyTool = __instance.HasContextTag("moonslime.Athletics.heavy_tool");

            // Only continue if the tool has either tag
            if (!isLightTool && !isHeavyTool)
                return;

            // --- Debug logging (optional) ---
            Log.Trace($"-------------------------------");
            Log.Trace($"Athletic's tool use exp tracker");

            // Random roll for chance-based EXP gain
            double checkValue = Game1.random.NextDouble();
            Log.Trace($"The check value is: {checkValue}");

            // Base chance to gain EXP from any tool use
            int baseExpChance = ModEntry.Config.ExpChanceFromTools;
            Log.Trace($"The base chance to get exp from using an atheltic's tool is: {baseExpChance}");

            // Determine difference between tool upgrade level and player's athletic level
            int difference = (__instance.UpgradeLevel * 2) - who.GetCustomSkillLevel("moonslime.Athletic");
            Log.Trace($"The player's tool level is: {__instance.UpgradeLevel}");
            Log.Trace($"The player's athletic level is: {who.GetCustomSkillLevel("moonslime.Athletic")}");
            Log.Trace($"The difference between player's tools and level is {difference}");

            // Final chance = base chance + 10% per level difference, then normalized to 0-1
            double finalChance = (baseExpChance + (difference * 10)) / 100.0;
            Log.Trace($"Final chance is: {finalChance}");

            // --- Check if player fails the roll ---
            Log.Trace($"Does the check fail? {checkValue >= finalChance}");
            if (checkValue >= finalChance)
                return; // Exit if EXP roll fails

            // --- EXP roll passed ---
            Log.Trace($"Check passed, EXP is gained!");
            int expToAdd = 0;

            // Determine EXP based on tool type
            if (isLightTool)
                expToAdd = ModEntry.Config.ExpFromLightToolUse;
            else if (isHeavyTool)
                expToAdd = ModEntry.Config.ExpFromHeavyToolUse;

            Log.Trace($"Total exp gained is: {expToAdd}");
            Log.Trace($"-------------------------------");

            // Apply EXP to player if any
            if (expToAdd > 0)
                Utilities.AddEXP(who, expToAdd);
        }
    }
}
