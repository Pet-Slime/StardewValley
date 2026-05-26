using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;
using System.Linq;
using StardewValley.GameData.Machines;
using StardewValley.Quests;

namespace CookingSkillRedux.Core
{

    //Left over patch, am not deleting just in case the transpiler breaks
    class ClickCraftingRecipe_patch
    {
        public static bool ClickCraftingRecipe(CraftingPage __instance, ClickableTextureComponent c, bool playSound, ref int ___currentCraftingPage, ref Item ___heldItem, ref bool ___cooking)
        {
            //do not change anything not cooking related - pointlessly dangerous
            if(!___cooking)
            {
                return true;
            }
            ModEntry.Instance.Monitor.Log("YACS Starting click crafting recipe prefix - should not happen if bettercrafting is instaleld", LogLevel.Trace);
            CraftingRecipe craftingRecipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][c];
            Item item = craftingRecipe.createItem();
            var player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            List<KeyValuePair<string, int>> list = null;
            if (___cooking && item.Quality == 0)
            {
                //don't allow the player to force a certain quality dish -
                //if inventory is full do not allow the craft - display full inventory toaster
                if (player.isInventoryFull() && ___heldItem != null)
                {
                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
                    return false;
                }
                list = new List<KeyValuePair<string, int>>();
                list.Add(new KeyValuePair<string, int>("917", 1));
                if (CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, GetContainerContents(__instance._materialContainers)))
                {
                    item.Quality = 2;

                }
                else
                {
                    list = null;
                }

                /// /////////////////////////////
                /// Custom Code
                /// for Cooking
                if (craftingRecipe is not null && craftingRecipe.isCookingRecipe)
                {
                    var consumed_items = FigureOutItems(craftingRecipe, __instance._materialContainers);
                    Events.PreCook(craftingRecipe, item);
                    Events.PostCook(craftingRecipe, item, consumed_items, player);
                }
                
                ////////////////////////////////////
            }

            if (___heldItem == null)
            {
                craftingRecipe.consumeIngredients(__instance._materialContainers);
                ___heldItem = item;
                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }
            else
            {
                if (!(___heldItem.Name == item.Name) || !___heldItem.getOne().canStackWith(item.getOne()) || ___heldItem.Stack + craftingRecipe.numberProducedPerCraft - 1 >= ___heldItem.maximumStackSize())
                {
                    item.Stack = craftingRecipe.numberProducedPerCraft;
                    if (player.couldInventoryAcceptThisItem(item))
                    {
                        player.addItemToInventoryBool(item);
                    }
                    else { return false; }
                }
                else
                {
                    ___heldItem.Stack += craftingRecipe.numberProducedPerCraft;
                }
                craftingRecipe.consumeIngredients(__instance._materialContainers);
                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }

            if (list != null)
            {
                if (playSound)
                {
                    Game1.playSound("breathin");
                }

                CraftingRecipe.ConsumeAdditionalIngredients(list, __instance._materialContainers);
                if (!CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, GetContainerContents(__instance._materialContainers)))
                {
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Seasoning_UsedLast"));
                }
            }

            Game1.player.NotifyQuests((Quest quest) => quest.OnRecipeCrafted(craftingRecipe, item));
            if (!___cooking && player.craftingRecipes.ContainsKey(craftingRecipe.name))
            {
                player.craftingRecipes[craftingRecipe.name] += craftingRecipe.numberProducedPerCraft;
            }

            if (___cooking)
            {
                player.cookedRecipe(item.ItemId);
                Game1.stats.checkForCookingAchievements();

            }
            else
            {
                Game1.stats.checkForCraftingAchievements();
            }

            if (Game1.options.gamepadControls && ___heldItem != null && player.couldInventoryAcceptThisItem(___heldItem))
            {
                player.addItemToInventoryBool(___heldItem);
                ___heldItem = null;
            }
            return false;
        }

        public static IList<Item> GetContainerContents(List<IInventory> _materialContainers)
        {
            if (_materialContainers == null)
            {
                return null;
            }

            List<Item> list = new List<Item>();
            foreach (IInventory materialContainer in _materialContainers)
            {
                list.AddRange(materialContainer);
            }

            return list;
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


    [HarmonyPatch(typeof(StardewValley.Item), nameof(Item.canStackWith))]
    class CanStackWith_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(
        StardewValley.Item __instance, ref bool __result, ref ISalable other)
        {
            //Prevent items with different edibility values from stacking. 
            if (__instance is Object @object && other is Object object2 && object2.Edibility != @object.Edibility)
            {
                __result = false;
                return;
            }
        }
    }

    // This Harmony patch hooks into StardewValley.Object.OutputMachine().
    // It runs AFTER the original method (Postfix) to modify or replace
    // the output when the custom "soda machine" produces an item.

    [HarmonyPatch(typeof(StardewValley.Object), nameof(Object.OutputMachine))]
    class CreateFlavoredSoda_patch
    {
        [HarmonyPostfix]
        private static void Postfix(
            StardewValley.Object __instance,       // The machine object running OutputMachine()
            MachineData machine,                   // Machine data definition (from Data/Machines)
            MachineOutputRule outputRule,           // Rule used to determine output
            Item inputItem,                        // The item placed into the machine
            Farmer who,                            // The farmer using the machine
            GameLocation location,                 // The location the machine is in
            bool probe                             // True if this is a dry-run (e.g. checking output only)
        )
        {
            // Check if this is our custom soda machine and a valid fruit input
            if (__instance.QualifiedItemId.Equals("(BC)moonslime.Cooking.soda_machine") && // Only apply to our mod’s soda machine
                __instance.heldObject.Value != null &&                                    // Must have a valid output object
                inputItem is Object item2 && item2 is not null &&                         // Input must be an Object
                item2.HasContextTag("category_fruits"))                                   // Input must be a fruit
            {
                // Prepare variables and determine color
                Object output = __instance.heldObject.Value;                              // The machine's current output
                Color color = ItemContextTagManager.GetColorFromTags(item2) ?? Color.Brown; // Try to extract a color from the fruit, fallback to brown

                // Create a new colored soda item 
                ColoredObject soda = new ColoredObject("(O)moonslime.Cooking.soda", output.Stack, color)
                {
                    Quality = inputItem.Quality                                            // Inherit input item quality
                };

                // Apply flavor-specific data if input has an ItemId
                if (item2?.ItemId is not null)
                {
                    // "%PRESERVED_DISPLAY_NAME" makes the game format the item as “<Fruit> Soda”
                    soda.displayNameFormat = "%PRESERVED_DISPLAY_NAME %DISPLAY_NAME";

                    // Store the parent fruit's ID for tooltip flavor text and color logic
                    soda.preservedParentSheetIndex.Value = item2.ItemId;

                    // Base price for soda; this will be used to calculate stack count
                    soda.Price = 25;

                    // Determine output stack size and edibility based on fruit value
                    // Calculate relative yield: higher value fruit = more sodas
                    double value = (item2.Price + 200.0) / soda.Price;
                    soda.Stack = (int)Math.Ceiling(value);

                    // Scale edibility per soda to prevent infinite energy farming
                    soda.Edibility = item2.Edibility / soda.Stack;

                    // Profession bonus: Cooking Level 10 “a1” doubles yield sometimes
                    if (who.HasCustomProfession(Cooking_Skill.Cooking10a1))
                    {
                        // Get profession-based chance modifier (likely scales with cooking level)
                        float doubleLevelChance = Utilities.GetLevelValue(who, true) + Utilities.GetLevelValue(who, true);

                        // Roll random chance to double the output stack
                        if (Game1.random.NextDouble() < doubleLevelChance)
                        {
                            soda.Stack += soda.Stack;
                        }
                    }

                    // Name the soda uniquely using the input fruit’s name
                    // e.g., “moonslime.Cooking.soda_Apple” or similar
                    if (item2?.Name is not null)
                    {
                        soda.Name = $"{soda.QualifiedItemId}_{item2.Name}";
                    }
                }

                // Replace the machine's held output with our flavored soda
                __instance.heldObject.Value = soda;
            }
        }
    }



    [HarmonyPatch(typeof(StardewValley.Buildings.Building), "CheckItemConversionRule")]
    class MillItemConversion_patch
    {
        [HarmonyLib.HarmonyPrefix]
        public static bool Prefix(StardewValley.Buildings.Building __instance, BuildingItemConversion conversion, ItemQueryContext itemQueryContext)
        {
            // Only replace vanilla conversion logic for Mills.
            // Returning true lets the original Stardew method run for every other building.
            if (__instance.buildingType.Value != "Mill")
                return true;

            ModEntry.Instance.Monitor.Log("Starting to run the logic for Mills", LogLevel.Trace);

            // Track how many valid input items exist at each Stardew quality value.
            // Stardew normally uses:
            // 0 = normal, 1 = silver, 2 = gold, 4 = iridium.
            // Index 3 is included only so item.Quality can be used directly as an array index.
            int[] currentCount = { 0, 0, 0, 0, 0 };

            // Get the Mill's source/input chest and destination/output chest from the building conversion data.
            Chest sourceChest = __instance.GetBuildingChest(conversion.SourceChest);
            Chest destinationChest = __instance.GetBuildingChest(conversion.DestinationChest);

            // If either chest is missing, skip custom Mill logic.
            // This prevents errors when trying to read from or add items to a null chest.
            if (sourceChest == null || destinationChest == null)
                return false;

            // Count all valid source items by quality.
            // This does not consume anything yet; it only builds a summary of available ingredients.
            foreach (Item item in sourceChest.Items)
            {
                if (!HasRequiredTags(item, conversion))
                    continue;

                currentCount[item.Quality] += item.Stack;
            }

            // Build a list of planned conversions.
            // Each int[] in consumeCounts represents the exact qualities consumed by one conversion.
            // Example: [0, 0, 1, 0, 2] means this one conversion uses 1 gold and 2 iridium items.
            List<int[]> consumeCounts = BuildConversionRecipes(currentCount, conversion.RequiredCount, conversion.MaxDailyConversions);
            if (consumeCounts.Count == 0)
                return false;

            // Log how many conversions are planned, grouped by the lowest quality used in each conversion.
            // This is only for debugging and does not affect gameplay.
            ModEntry.Instance.Monitor.Log($"Will try to produce [{string.Join(", ", CountConversionsByLowestQuality(consumeCounts))}]", LogLevel.Trace);

            // totalConversions tracks how many conversions successfully produced output.
            // requiredAmount tracks how many input items of each quality should be deleted afterward.
            int totalConversions = 0;
            int[] requiredAmount = { 0, 0, 0, 0, 0 };

            // Try to create output items for each planned conversion.
            for (int j = 0; j < consumeCounts.Count; j++)
            {
                bool conversionCreatedItem = false;

                // A conversion can define one or more possible produced items.
                // This mirrors Stardew's data-driven item conversion behavior.
                for (int i = 0; i < conversion.ProducedItems.Count; i++)
                {
                    GenericSpawnItemDataWithCondition producedItem = conversion.ProducedItems[i];

                    // Only produce this item if its game state query condition passes.
                    if (!GameStateQuery.CheckConditions(producedItem.Condition, __instance.GetParentLocation()))
                        continue;

                    // Resolve the output item from the building conversion data.
                    Item item = ItemQueryResolver.TryResolveRandomItem(producedItem, itemQueryContext);

                    // Calculate output quality from the consumed ingredients for this specific conversion.
                    // This treats qualities as ranks:
                    // normal = 0, silver = 1, gold = 2, iridium = 3.
                    // Stardew stores iridium as quality 4, so consumeCounts[j][4] is weighted as rank 3.
                    double average_quality = (double)(consumeCounts[j][1] + consumeCounts[j][2] * 2 + consumeCounts[j][4] * 3) / consumeCounts[j].Sum();

                    // Probabilistically round fractional quality.
                    // Example: average 1.75 becomes quality 1 with a 75% chance to round up to 2.
                    double chance = average_quality - (int)average_quality;
                    int quality = (int)average_quality;
                    if (Game1.random.NextDouble() < chance)
                    {
                        quality++;
                    }

                    // Add a small random quality swing after the average-quality roll.
                    // r == 0 gives +1 quality. r == 1, 2, 3, or 4 gives -1 quality.
                    // Otherwise, quality stays as rolled above.
                    int r = Game1.random.Next(15);
                    if (r == 0)
                    {
                        quality++;
                    }
                    else if (r < 5)
                    {
                        quality--;
                    }

                    // Convert internal quality rank 3+ back into Stardew's iridium quality value.
                    if (quality >= 3)
                    {
                        quality = 4;
                    }

                    // Prevent negative quality after the random -1 roll.
                    if (quality < 0)
                    {
                        quality = 0;
                    }

                    // Apply the calculated quality to the produced Mill output.
                    item.Quality = quality;

                    // Try to add the produced item to the destination chest.
                    // If the chest accepts all or part of the item, the conversion counts as successful.
                    int producedCount = item.Stack;
                    Item item2 = destinationChest.addItem(item);
                    if (item2 == null || item2.Stack != producedCount)
                    {
                        conversionCreatedItem = true;
                    }
                }

                // Only mark ingredients for deletion if this conversion actually created output.
                // This prevents the Mill from eating ingredients when the destination chest is full.
                if (conversionCreatedItem)
                {
                    totalConversions++;

                    // Add this conversion's consumed quality counts to the final deletion list.
                    for (int k = 0; k < 5; k++)
                    {
                        requiredAmount[k] += consumeCounts[j][k];
                    }
                }
            }

            // If no output could be created, do not consume any source ingredients.
            if (totalConversions <= 0)
                return false;

            ModEntry.Instance.Monitor.Log($"Need to delete [{string.Join(", ", requiredAmount)}]", LogLevel.Trace);

            // Delete the exact quality amounts that were used by successful conversions.
            // This second pass removes items from the actual source chest after output was confirmed.
            for (int i = 0; i < sourceChest.Items.Count; i++)
            {
                Item item = sourceChest.Items[i];
                if (!HasRequiredTags(item, conversion))
                    continue;

                // Consume only as many items of this quality as are still required.
                int consumedAmount = Math.Min(requiredAmount[item.Quality], item.Stack);
                sourceChest.Items[i] = item.ConsumeStack(consumedAmount);
                requiredAmount[item.Quality] -= consumedAmount;

                // Stop once all required ingredient amounts have been consumed.
                if (requiredAmount.Sum() <= 0)
                    break;
            }

            // Returning false skips vanilla CheckItemConversionRule for Mills,
            // since this prefix has already handled the conversion.
            return false;
        }

        private static bool HasRequiredTags(Item item, BuildingItemConversion conversion)
        {
            // Null slots are not valid ingredients.
            if (item == null)
                return false;

            // The item must have every required tag from the conversion data.
            // This is how the Mill knows whether an item is valid for the current recipe.
            foreach (string requiredTag in conversion.RequiredTags)
            {
                if (!item.HasContextTag(requiredTag))
                    return false;
            }

            return true;
        }

        private static List<int[]> BuildConversionRecipes(int[] currentCount, int requiredCount, int maxConversions)
        {
            // Each entry in this list represents one planned conversion.
            // Each int[] stores how many items of each quality that conversion will consume.
            List<int[]> consumeCounts = new();

            // Bad conversion data should not create an infinite loop or free output.
            if (requiredCount <= 0)
                return consumeCounts;


            int conversionsLeft = maxConversions < 0 ? int.MaxValue : maxConversions;

            while (conversionsLeft > 0)
            {
                int needed = requiredCount;
                int[] consumeCount = { 0, 0, 0, 0, 0 };

                // Build one conversion by taking from highest quality to lowest quality.
                // This makes higher-quality ingredients influence output quality first.
                for (int quality = currentCount.Length - 1; quality >= 0 && needed > 0; quality--)
                {
                    int consumed = Math.Min(currentCount[quality], needed);
                    if (consumed <= 0)
                        continue;

                    currentCount[quality] -= consumed;
                    consumeCount[quality] = consumed;
                    needed -= consumed;
                }

                // If we couldn't gather enough ingredients for this conversion, stop.
                if (needed > 0)
                    break;

                // Store the completed conversion recipe.
                consumeCounts.Add(consumeCount);
                conversionsLeft--;
            }

            return consumeCounts;
        }

        private static int[] CountConversionsByLowestQuality(List<int[]> consumeCounts)
        {
            // Used for logging only.
            // Counts how many planned conversions include each quality as their lowest consumed quality.
            int[] conversions = { 0, 0, 0, 0, 0 };

            foreach (int[] consumeCount in consumeCounts)
            {
                // Scan from low quality to high quality.
                // The first quality with a consumed item is this conversion's lowest ingredient quality.
                for (int quality = 0; quality < consumeCount.Length; quality++)
                {
                    if (consumeCount[quality] <= 0)
                        continue;

                    conversions[quality]++;
                    break;
                }
            }

            return conversions;
        }
    }
}
