using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Netcode;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using static BirbCore.Attributes.SMod;

namespace SpaceCoreLevels.Core
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), [typeof(string[]), typeof(Farmer), typeof(xTile.Dimensions.Location)])]
    class GameLocation_Patch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_MineShaft_checkForBuriedItems(IEnumerable<CodeInstruction> instructions)
        {

            MethodInfo masteryConfigInfo = AccessTools.Method(typeof(GameLocation_Patch), nameof(MasteryRequired));
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
            yield return codeInstructions[0];
            for (int i = 1; i < codeInstructions.Count; i++)
            {
                if (i == 1244)
                {
                    yield return new CodeInstruction(OpCodes.Call, masteryConfigInfo);
                    yield return new CodeInstruction(OpCodes.Stloc_S, 5);
                }
                yield return codeInstructions[i];
            }

        }

        internal static int MasteryRequired()
        {

            var Farmer = Game1.player;

            
            return Farmer.Level / 5;
        }
    }
}
