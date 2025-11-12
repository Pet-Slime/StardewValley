using System;
using System.Collections.Generic;
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

    //patch isnt loaded. cant get this to work 100% how I want it to. makes me sad.
    public static class ClickCraftingRecipe_Transpiler
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);

            // References to fields in the display class
            var displayClassType = typeof(CraftingPage).GetNestedType("<>c__DisplayClass42_0",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var craftedField = AccessTools.Field(displayClassType, "crafted");
            var heldItemField = AccessTools.Field(displayClassType, "heldItem");
            var recipeField = AccessTools.Field(displayClassType, "recipe");

            // Reference to helper methods
            MethodInfo injectMethod = AccessTools.Method(typeof(ClickCraftingRecipe_Transpiler), nameof(InjectCookingHooksAfterSeasoning));
            MethodInfo handleStackOverflow = AccessTools.Method(typeof(ClickCraftingRecipe_Transpiler), nameof(HandleStackOverflow));
            MethodInfo debug = AccessTools.Method(typeof(ClickCraftingRecipe_Transpiler), nameof(Debugger));

            // Game1.showRedMessage method
            MethodInfo showRedMessage = AccessTools.Method(typeof(Game1), nameof(Game1.showRedMessage), new[] { typeof(string) });


            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                // Look for the point right before the seasoning list is created
                if (code.opcode == OpCodes.Newobj && code.operand is ConstructorInfo ctor &&
                    ctor.DeclaringType == typeof(List<KeyValuePair<string, int>>))
                {

                    yield return new CodeInstruction(OpCodes.Call, debug);
                }

                yield return code;


                // --- Post-seasoning hooks ---
                if (code.opcode == OpCodes.Call &&
                    code.operand is MethodInfo method &&
                    method.Name == nameof(CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, craftedField);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, recipeField);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, injectMethod);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Stfld, craftedField);
                }

                // --- Inject stack overflow / add to inventory logic ---
                // Look for any Stfld assignment to heldItem and inject helper immediately after
                if (code.opcode == OpCodes.Stfld && code.operand is FieldInfo f && f.Name == "heldItem")
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);              // display class
                    yield return new CodeInstruction(OpCodes.Ldfld, craftedField);  // crafted
                    yield return new CodeInstruction(OpCodes.Ldloc_0);              // display class
                    yield return new CodeInstruction(OpCodes.Ldfld, recipeField);   // recipe
                    yield return new CodeInstruction(OpCodes.Ldarg_0);              // CraftingPage 'this'
                    yield return new CodeInstruction(OpCodes.Call, handleStackOverflow);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);              // store result back
                    yield return new CodeInstruction(OpCodes.Stfld, craftedField);
                }
            }
        }

        public static void Debugger()
        {
            Log.Alert("DEBUGGER LOADED WHOOO");
        }

        // Post-seasoning hook
        public static Item InjectCookingHooksAfterSeasoning(Item crafted, CraftingRecipe recipe, CraftingPage page)
        {
            if (recipe == null || crafted == null || !recipe.isCookingRecipe)
                return crafted;

            var player = Game1.player;

            var consumedItems = FigureOutItems(recipe, page._materialContainers);

            Events.PreCook(recipe, crafted);
            Events.PostCook(recipe, crafted, consumedItems, player);

            return crafted;
        }

        public static Dictionary<Item, int> FigureOutItems(CraftingRecipe recipe, List<IInventory> additionalInventories)
        {
            Dictionary<Item, int> items = new Dictionary<Item, int>();
            foreach (var ingredient in recipe.recipeList)
            {
                string key = ingredient.Key;
                int num = ingredient.Value;
                bool done = false;

                // player inventory
                for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
                {
                    if (CraftingRecipe.ItemMatchesForCrafting(Game1.player.Items[i], key))
                    {
                        int amount = num;
                        num -= Game1.player.Items[i].Stack;
                        items.Add(Game1.player.Items[i], System.Math.Min(Game1.player.Items[i].Stack, amount));
                        if (num <= 0) { done = true; break; }
                    }
                }

                if (additionalInventories == null || done) continue;

                // additional inventories
                foreach (var inventory in additionalInventories)
                {
                    if (inventory == null) continue;
                    for (int j = inventory.Count - 1; j >= 0; j--)
                    {
                        if (CraftingRecipe.ItemMatchesForCrafting(inventory[j], key))
                        {
                            int n = System.Math.Min(num, inventory[j].Stack);
                            num -= n;
                            items.Add(inventory[j], n);
                            if (num <= 0) break;
                        }
                    }
                    if (num <= 0) break;
                }
            }
            return items;
        }

        public static Item HandleStackOverflow(Item crafted, CraftingRecipe recipe, CraftingPage page)
        {
            var player = Game1.player;

            if (page.heldItem == null)
            {
                recipe.consumeIngredients(page._materialContainers);
                page.heldItem = crafted;
            }
            else
            {
                if (!(page.heldItem.Name == crafted.Name) || !page.heldItem.getOne().canStackWith(crafted.getOne()) || page.heldItem.Stack + recipe.numberProducedPerCraft - 1 >= page.heldItem.maximumStackSize())
                {
                    crafted.Stack = recipe.numberProducedPerCraft;
                    if (player.couldInventoryAcceptThisItem(crafted))
                    {
                        player.addItemToInventoryBool(crafted);
                    }
                    else
                    {
                        // Inventory full; early return to skip crafting
                        return crafted; // transpiler will respect this as stored crafted item
                    }
                }
                else
                {
                    page.heldItem.Stack += recipe.numberProducedPerCraft;
                }
                recipe.consumeIngredients(page._materialContainers);

            }

            return crafted;
        }
    }
}
