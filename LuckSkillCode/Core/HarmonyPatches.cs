using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using System.Diagnostics;

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
                if (Game1.currentLocation is MineShaft ms)
                {
                    bool foundGeode = false;
                    var st = new StackTrace();
                    foreach (var frame in st.GetFrames())
                    {
                        if (frame.GetMethod().Name.Contains(nameof(MineShaft.checkStoneForItems)))
                        {
                            foundGeode = true;
                            break;
                        }
                    }

                    if (foundGeode)
                    {
                        int msa = ms.getMineArea();
                        if (msa != 0)
                        {
                            howMuch /= msa;
                        }
                    }
                }
                Utilities.AddEXP(Game1.GetPlayer(__instance.UniqueMultiplayerID), howMuch);
            }
        }
    }
}
