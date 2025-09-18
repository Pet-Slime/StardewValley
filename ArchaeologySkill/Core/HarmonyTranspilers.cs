using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spacechase.Shared.Patching;
using SpaceCore;
using SpaceShared;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.Menus;
using static System.Net.Mime.MediaTypeNames;
using static SpaceCore.Skills;

namespace ArchaeologySkill.Core
{

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkForBuriedItem))]
    class CheckForBuriedItem_Base_patch
    {
        [HarmonyLib.HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_GameLocation_checkForBuriedItems(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
            int step = 0;
            yield return codeInstructions[0];

            Log.Alert(codeInstructions.Count.ToString());
            for (int i = 1; i < codeInstructions.Count; i++)
            {
                
                if (i == 23)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_1));
                }

                if (i == 93)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_2));
                }

                if (i == 153)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_3));
                }

                if (codeInstructions[i].opcode == OpCodes.Ret)
                {
                    Log.Alert(i.ToString());
                    Log.Alert(codeInstructions[i].opcode.ToString());
                }
                yield return codeInstructions[i];
            }
        }


        private static void ArchaeologySkillCheck_1(int xLocation, int yLocation, Farmer farmer)
        {
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation);
            return;
        }

        private static void ArchaeologySkillCheck_2(int xLocation, int yLocation, Farmer farmer)
        {
            Random r = Utility.CreateDaySaveRandom((double)(xLocation * 2000), (double)(yLocation * 77), Game1.stats.DirtHoed);
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: r.Choose("(O)412", "(O)416"));
            return;
        }
        private static void ArchaeologySkillCheck_3(int xLocation, int yLocation, Farmer farmer)
        {
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: "(O)330");
            return;
        }
    }
}
