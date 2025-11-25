using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using Log = MoonShared.Attributes.Log;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace BibliocraftSkill.Core
{

    // The goal of this patch is to add in a second check for the monster drop book. 
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    public static class MonsterDrop_SecondCheck_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Log.Trace("[SecondLootChance] Starting transpiler");

            var codes = instructions.ToList();
            int insertIndex = -1;

            // Find the injection point via 'Book_Void' string
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string s && s == "Book_Void")
                {
                    bool patternMatch = false;

                    // Check one instruction back
                    if (i > 0 && codes[i - 1].opcode == OpCodes.Ldfld) // stats field
                    {
                        // Check two instructions back
                        if (i > 1 && codes[i - 2].opcode == OpCodes.Ldarg_S &&
                            (codes[i - 2].operand is byte b && b == 4 ||
                             codes[i - 2].operand is ParameterInfo pi && pi.Name == "who"))
                        {
                            patternMatch = true;
                        }
                    }

                    if (patternMatch)
                    {
                        insertIndex = i;
                        Log.Trace($"[SecondLootChance] Found injection point at IL index {i}");
                        break;
                    }
                }
            }

            if (insertIndex == -1)
            {
                Log.Warn("[SecondLootChance] Could not find injection point, patch failed");
                return codes;
            }

            Log.Trace($"[SecondLootChance] Found injection point at IL index {insertIndex}");

            // Insert call to our helper method before 'Book_Void' check
            int debrisLocal = 3;      // debrisToAdd list
            int playerPosLocal = 1;   // playerPosition vector

            var injected = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_S, 4),       // Farmer who
            new CodeInstruction(OpCodes.Ldarg_0),         // this GameLocation
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GameLocation), "debris")), // debris
            new CodeInstruction(OpCodes.Ldloc_S, debrisLocal),  // debrisToAdd
            new CodeInstruction(OpCodes.Ldarg_1),         // Monster monster
            new CodeInstruction(OpCodes.Ldarg_2),         // x
            new CodeInstruction(OpCodes.Ldarg_3),         // y
            new CodeInstruction(OpCodes.Ldloc_S, playerPosLocal), // playerPosition
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MonsterDrop_SecondCheck_Patch), nameof(TripleLoot)))
        };

            codes.InsertRange(insertIndex, injected);

            Log.Trace("[SecondLootChance] Injection complete");

            return codes;
        }

        public static void TripleLoot(Farmer who, NetCollection<Debris> debris, List<Debris> debrisToAdd, Monster monster, int x, int y, Vector2 playerPosition)
        {
            Log.Trace("[SecondLootChance] Running helper method");

            if (who.stats.Get("Book_Void") > 0 && Game1.random.NextDouble() < 0.03 && debrisToAdd != null && monster != null && Utilities.GetLevel(who, true) >= 7)
            {
                Log.Trace($"[SecondLootChance] Farmer {who.Name} qualifies for extra loot!");
                foreach (Debris d2 in debrisToAdd)
                {
                    if (d2.item != null)
                    {
                        Item tmp2 = d2.item.getOne();
                        if (tmp2 != null)
                        {
                            tmp2.Stack = d2.item.Stack;
                            tmp2.HasBeenInInventory = false;
                            debris.Add(monster.ModifyMonsterLoot(new Debris(tmp2, new Vector2(x, y), playerPosition)));
                        }
                    }
                    else if (d2.itemId.Value != null && d2.itemId.Value.Length > 0)
                    {
                        Item tmp3 = ItemRegistry.Create(d2.itemId.Value, 1, 0, false);
                        tmp3.HasBeenInInventory = false;
                        debris.Add(monster.ModifyMonsterLoot(new Debris(tmp3, new Vector2(x, y), playerPosition)));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Farmer), "takeDamage")]
    class Defense_patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            int insertIndex = -1;

            // find where defense is set (stloc.s 4)
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_S && codes[i].operand is LocalBuilder lb && lb.LocalIndex == 4)
                {
                    insertIndex = i + 1; // insert *after* defense is set
                    break;
                }
            }

            if (insertIndex == -1)
            {
                Log.Warn("[Defense_patch] Could not find defense local; patch failed.");
                return codes;
            }

            var injected = new List<CodeInstruction>
                {
                    // Load 'this' onto stack
                    new CodeInstruction(OpCodes.Ldarg_0),
                    // Load local 'defense' by reference
                    new CodeInstruction(OpCodes.Ldloc_S, 4),
                    // Call helper
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Defense_patch), nameof(Better_Book_Defense))),
                    // Store result back into 'defense'
                    new CodeInstruction(OpCodes.Stloc_S, 4)
                };

            codes.InsertRange(insertIndex, injected);

            Log.Trace("[Defense_patch] Injection complete.");
            return codes;
        }

        public static int Better_Book_Defense(Farmer player, int amount)
        {
            if (player.stats.Get("Book_Defense") != 0 && Utilities.GetLevel(player, true) >= 4)
            {
                return amount + 1;
            }
            return amount;
        }
    }

    //Goal of the patch is to increase wood dropping at a low chance if you have woody's secret
    [HarmonyPatch(typeof(Tree), "tickUpdate")]
    class ExtraWood_patch
    {

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // method reference
            MethodInfo better = AccessTools.Method(typeof(ExtraWood_patch),
                nameof(ExtraWood_patch.Better_Book_Woodcutting));

            // locals in this method
            // local 9  = lastHitBy (Farmer)
            // local 14 = numToDrop (int)
            const int lastHitByIndex = 9;
            const int numToDropIndex = 14;

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                // look for: stloc.s 14   (this is numToDrop after the *=2)
                if (codes[i].opcode == OpCodes.Stloc_S &&
                    codes[i].operand is LocalBuilder lb &&
                    lb.LocalIndex == numToDropIndex)
                {
                    // Inject:
                    // ldloc.s 9         (lastHitBy)
                    // ldloc.s 14        (numToDrop)
                    // call Better_Book_Woodcutting
                    // stloc.s 14        (overwrite numToDrop with new value)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, lastHitByIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, numToDropIndex);
                    yield return new CodeInstruction(OpCodes.Call, better);
                    yield return new CodeInstruction(OpCodes.Stloc_S, numToDropIndex);
                }
            }
        }



        public static int Better_Book_Woodcutting(Farmer player, int amount)
        {
            Log.Trace("[ExtraWoood] Running helper method");
            if (Utilities.GetLevel(player, true) >= 2) {

                Log.Trace("[ExtraWoood] Granting Extra Wood");
                return amount += 5;
            }
            Log.Trace("[ExtraWoood] Not level 4 or greater, returning default amount");
            return amount;
        }
    }

    //Goal of this patch is to increase the crabpot bonus rate when harvesting by hand
    [HarmonyPatch(typeof(StardewValley.Objects.CrabPot), "checkForAction")]
    class Crabpot_Bonus_patch
    {
        [HarmonyTranspiler]
        public static void Postfix()
        {
            static IEnumerable<CodeInstruction> CrabPot_checkForAction_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                CodeMatcher matcher = new(instructions);
                // Old: NextDouble() < 0.25
                // New: NextDouble() < 0.25 + Patcher.GetExtraCrabPotDoublePercentage(who)
                // up to you to implement Patcher.GetExtraCrabPotDoublePercentage(who) returns a float
                //
                matcher
                  .MatchEndForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Random), nameof(Random.NextDouble))),
                    new CodeMatch(OpCodes.Ldc_R8, 0.25)
                    )
                  .ThrowIfNotMatch($"Could not find entry point for {nameof(CrabPot_checkForAction_Transpiler)}");
                matcher
                  .Advance(1)
                  .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Crabpot_Bonus_patch), nameof(GetExtraCrabPotDoublePercentage))),
                new CodeInstruction(OpCodes.Add)
                      );
                return matcher.InstructionEnumeration();
            }
        }

        private static object GetExtraCrabPotDoublePercentage(Farmer who)
        {
            if (Utilities.GetLevel(who, true) >=8)
            {
                return 0.25;
            } else
            {
                return 0;
            }
        }
    }

}
