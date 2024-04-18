using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley;

namespace LuckSkill.Core
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
    class HarmonyPatches
    {
        [HarmonyLib.HarmonyPostfix]
        public static void LuckEXP(Farmer __instance, ref int which, ref int howMuch)
        {
            if (__instance.IsLocalPlayer && which == 5)
            {
                Utilities.AddEXP(Game1.getFarmer(__instance.UniqueMultiplayerID), howMuch);
            }
        }
    }
}
