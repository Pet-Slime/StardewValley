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
            yield return codeInstructions[0];
            for (int i = 1; i < codeInstructions.Count; i++)
            {

                if (i == 23 || i == 74 || i == 137)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);

                    switch (i)
                    {
                        case 23:
                            yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_1));
                            break;
                        case 74:
                            yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Base_patch), nameof(ArchaeologySkillCheck_2));
                            break;
                        case 137:
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
                if (i == 171)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Mineshaft_patch), nameof(ArchaeologySkillCheck_4));
                }
                yield return codeInstructions[i];
            }
        }


        private static void ArchaeologySkillCheck_4(int xLocation, int yLocation, Farmer farmer, string item)
        {
            Log.Alert("Archaeology testing 4");
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
            return;
        }
    }

    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.receiveLeftClick))]
    class MuseumMenu_patch
    {
        [HarmonyLib.HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MuseumMenu_receiveLeftClick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {

            var code = instructions.ToList();
            try
            {
                var matcher = new CodeMatcher(code, il);

                matcher.MatchStartForward(
                    new CodeMatch(op => op.IsLdloc()),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ldstr, "stoneStep")
                );
                matcher.MatchEndForward(
                    new CodeMatch(OpCodes.Ldstr, "stoneStep")
                ).Advance(1);

                matcher.Insert(
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MuseumMenu_patch), nameof(ArchaeologySkillEXP)))
                );

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Log.Error("Error in moonslime.Archaeology.LibraryMuseumPatches_MuseumMenu_receiveLeftClick_Transpiler: \n" + ex);
                return code;
            }
        }
        private static void ArchaeologySkillEXP()
        {
            foreach (var farmer in Game1.getOnlineFarmers()) {
                Utilities.AddEXP(farmer, ModEntry.Config.ExperienceFromDonationRewards);
            }

        }
    }
}


