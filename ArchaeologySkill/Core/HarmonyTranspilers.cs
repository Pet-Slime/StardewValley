using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using MoonShared.Attributes;
using HarmonyLib;
using SpaceShared;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Menus;

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
            Log.Alert("Archaeology Insertion 1 successful");
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation);
            return;
        }

        private static void ArchaeologySkillCheck_2(int xLocation, int yLocation, Farmer farmer)
        {
            Log.Alert("Archaeology Insertion 2 successful");
            Random r = Utility.CreateDaySaveRandom((double)(xLocation * 2000), (double)(yLocation * 77), Game1.stats.DirtHoed);
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: r.Choose("(O)412", "(O)416"));
            return;
        }
        private static void ArchaeologySkillCheck_3(int xLocation, int yLocation, Farmer farmer)
        {
            Log.Alert("Archaeology Insertion 3 successful");
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: "(O)330");
            return;
        }
    }


    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.checkForBuriedItem))]
    class CheckForBuriedItem_Mineshaft_patch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_MineShaft_checkForBuriedItems(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var targetMethod = typeof(Game1).GetMethod(nameof(Game1.createObjectDebris),
                new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(GameLocation) });

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                // Inject after the call to Game1.createObjectDebris
                if (codes[i].opcode == OpCodes.Call && codes[i].Calls(targetMethod))
                {
                    // Load arguments for our method: xLocation, yLocation, who, id
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // xLocation
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // yLocation
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5); // Farmer who
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // string id (assuming id is stored in loc_0)
                    yield return CodeInstruction.Call(typeof(CheckForBuriedItem_Mineshaft_patch), nameof(ArchaeologySkillCheck_4));
                }
            }
        }

        private static void ArchaeologySkillCheck_4(int xLocation, int yLocation, Farmer who, string item)
        {
            Log.Alert("Archaeology Insertion 4 successful");
            Utilities.ApplyArchaeologySkill(who, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
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

                    new CodeInstruction(OpCodes.Ldloc_S, code.First(ci => ci.opcode == OpCodes.Stloc_S && ((LocalBuilder)ci.operand).LocalIndex >= 0).operand),


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
        private static void ArchaeologySkillEXP(Item item)
        {
            if (item == null)
                return;

            string key = $"moonslime.Archaeology.donatedArtifacts.{item.QualifiedItemId}";
            var farm = Game1.getFarm();

            if (!farm.modData.TryGetValue(key, out _))
            {
                int amount = ModEntry.Config.ExperienceFromDonationRewards;

                foreach (var farmer in Game1.getOnlineFarmers())
                    Utilities.AddEXP(farmer, amount);

                farm.modData[key] = "1";
            }
        }
    }
}


