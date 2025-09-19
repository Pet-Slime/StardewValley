using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Netcode;
using SpaceCore;
using SpaceShared.APIs;
using StardewValley;
using StardewValley.Locations;

namespace SpaceCoreLevels.Core
{
    [HarmonyPatch(typeof(Farmer), "get_Level")]
    class AdjustTotalLevel_Patch
    {
        private const int VanillaSkillCount = 6;
        private const int MaxSkillLevel = 10;
        private const int NormalizedTarget = 30;

        [HarmonyPostfix]
        public static void Postfix(Farmer __instance, ref int __result)
        {
            // Get custom skills (or empty array if none installed)
            // Only count skills that are visible on the skill page!
            string[] customSkills = Skills.GetSkillList().Where(s => Skills.GetSkill(s).ShouldShowOnSkillsPage).ToArray() ?? Array.Empty<string>();
            int customSkillCount = customSkills.Length;

            // Compute divisor (normalize total skill levels to a level 30 scale)
            int divisor = (VanillaSkillCount + customSkillCount) * MaxSkillLevel / NormalizedTarget;
            if (divisor <= 0) divisor = 1; // safeguard

            // Sum vanilla skills
            int vanillaTotal =
                __instance.FarmingLevel +
                __instance.FishingLevel +
                __instance.ForagingLevel +
                __instance.CombatLevel +
                __instance.MiningLevel +
                __instance.LuckLevel;

            // Sum custom skills
            int customTotal = 0;
            foreach (string skill in customSkills)
                customTotal += __instance.GetCustomSkillLevel(skill);

            // Final adjusted result
            __result = (vanillaTotal + customTotal) / divisor;
        }
    }
}
