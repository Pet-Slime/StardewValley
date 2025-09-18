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
using StardewValley.Locations;
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

            for (int i = 1; i < codeInstructions.Count; i++)
            {
                
                if (i == 23 || i == 93 || i == 153)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);

                    switch (i)
                    {
                        case 23:
                            yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_1));
                            break;
                        case 93:
                            yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_2));
                            break;
                        case 153:
                            yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_3));
                            break;
                    }
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

    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.checkForBuriedItem))]
    class CheckForBuriedItem_Mineshaft_patch
    {
        [HarmonyLib.HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_MineShaft_checkForBuriedItems(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
            yield return codeInstructions[0];

            for (int i = 1; i < codeInstructions.Count; i++)
            {
                if (i == 200)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Mineshaft_patch), nameof(ArchaeologySkillCheck_4));
                    yield return new CodeInstruction(OpCodes.Ldstr, "");
                }
                yield return codeInstructions[i];
            }
        }


        private static void ArchaeologySkillCheck_4(int xLocation, int yLocation, Farmer farmer, string item)
        {
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
            return;
        }
    }
}
