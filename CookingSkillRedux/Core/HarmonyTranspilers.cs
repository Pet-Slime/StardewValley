using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CookingSkillRedux.Core;
using HarmonyLib;
using MoonShared.Attributes;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;




namespace CookingSkillRedux.Core
{

    [HarmonyPatch(typeof(CraftingPage), "clickCraftingRecipe")]
    internal static class ClickCraftingRecipe_Transpiler
    {
        private static readonly MethodInfo ApplyVanillaUICookingHooksMethod = AccessTools.Method(typeof(ClickCraftingRecipe_Transpiler), nameof(ApplyVanillaUICookingHooks));
        private static readonly FieldInfo HeldItemField = AccessTools.Field(typeof(CraftingPage), "heldItem");

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Type displayClassType = typeof(CraftingPage).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(type =>
                    AccessTools.Field(type, "recipe")?.FieldType == typeof(CraftingRecipe) &&
                    AccessTools.Field(type, "crafted")?.FieldType == typeof(Item));
            if (displayClassType == null)
                throw new Exception("Could not find CraftingPage clickCraftingRecipe display class.");


            FieldInfo recipeField = AccessTools.Field(displayClassType, "recipe");
            FieldInfo craftedField = AccessTools.Field(displayClassType, "crafted");
            if (recipeField == null || craftedField == null)
                throw new Exception("Could not find recipe/crafted fields on CraftingPage display class.");

            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, HeldItemField)
                );

            if (!matcher.IsValid)
                throw new Exception("Failed to find CraftingPage.heldItem check in clickCraftingRecipe transpiler.");

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, recipeField),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, craftedField),
                new CodeInstruction(OpCodes.Call, ApplyVanillaUICookingHooksMethod),
                new CodeInstruction(OpCodes.Stfld, craftedField)
            );

            return matcher.InstructionEnumeration();
        }

        private static Item ApplyVanillaUICookingHooks(CraftingPage page, CraftingRecipe recipe, Item crafted)
        {
            if (recipe == null || !recipe.isCookingRecipe)
                return crafted;

            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            Dictionary<Item, int> consumed_items = FigureOutItems(recipe, page._materialContainers);
            crafted = Events.PreCook(recipe, crafted);
            crafted = Events.PostCook(recipe, crafted, consumed_items, player);
            return crafted;
        }


        public static Dictionary<Item, int> FigureOutItems(CraftingRecipe recipe, List<IInventory> additionalInventories)
        {
            Dictionary<Item, int> items = new Dictionary<Item, int>();
            foreach (KeyValuePair<string, int> ingredient in recipe.recipeList)
            {
                string key = ingredient.Key;
                int num = ingredient.Value;
                bool flag = false;
                for (int num2 = Game1.player.Items.Count - 1; num2 >= 0; num2--)
                {
                    if (CraftingRecipe.ItemMatchesForCrafting(Game1.player.Items[num2], key))
                    {
                        int amount = num;
                        num -= Game1.player.Items[num2].Stack;
                        items.Add(Game1.player.Items[num2], Math.Min(Game1.player.Items[num2].Stack, amount));
                        if (num <= 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                }

                if (additionalInventories == null || flag)
                {
                    continue;
                }

                for (int i = 0; i < additionalInventories.Count; i++)
                {
                    IInventory inventory = additionalInventories[i];
                    if (inventory == null)
                    {
                        continue;
                    }
                    for (int num3 = inventory.Count - 1; num3 >= 0; num3--)
                    {
                        if (CraftingRecipe.ItemMatchesForCrafting(inventory[num3], key))
                        {
                            int num4 = Math.Min(num, inventory[num3].Stack);
                            num -= num4;
                            items.Add(inventory[num3], num4);

                            if (num <= 0)
                            {
                                break;
                            }
                        }
                    }


                    if (num <= 0)
                    {
                        break;
                    }
                }
            }

            return items;
        }
    }
}
