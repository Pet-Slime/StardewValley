using HarmonyLib;
using System.Collections.Generic;

namespace WizardrySkill.Core
{
    [HarmonyPatch(typeof(StardewValley.Object), "_PopulateContextTags")]
    class PopulateContextTags_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(StardewValley.Object __instance, ref HashSet<string> tags)
        {
            // Map of item names to mana restoration values
            if (!ManaFillMap.TryGetValue(__instance.BaseName, out int manaValue))
                return;

            tags.Add($"moonslime.ManaBarApi.ManaFill/{manaValue}");
        }

        private static readonly Dictionary<string, int> ManaFillMap = new()
        {
            { "Common Mushroom", 5 },
            { "Purple Mushroom", 25 },
            { "Fried Mushroom", 10 },
            { "Stir Fry", 20 },
            { "Tom Kha Soup", 15 }
        };
    }
}
