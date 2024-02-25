using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Patching;
using CookingSkill.Core;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using StardewValley;
using MoonShared;

namespace CookingSkill.Patches
{
    internal class CraftingRecipePatcher : BasePatcher
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to actually consume items for the current recipe.</summary>
        public static bool ShouldConsumeItems { get; set; } = true;

        /// <summary>The items consumed by the last recipe, if any.</summary>
        public static IList<ConsumedItem> LastUsedItems { get; } = new List<ConsumedItem>();


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<CraftingRecipe>(nameof(CraftingRecipe.consumeIngredients)),
                prefix: this.GetHarmonyMethod(nameof(Before_ConsumeIngredients))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call before <see cref="CraftingRecipe.consumeIngredients"/>.</summary>
        /// <returns>Returns whether to run the original method.</returns>
        /// <remarks>This is copied verbatim from the original method with some changes (marked with comments).</remarks>
        public static bool Before_ConsumeIngredients(ref CraftingRecipe __instance, List<Chest> additional_materials)
        {
            Log.Debug("Cooking 2 patch fired 1");
            CraftingRecipePatcher.LastUsedItems.Clear();
            var recipe = __instance;
            if (!recipe.isCookingRecipe)
                return true;

            Log.Debug("Cooking 2 patch fired 2");
            for (int recipeIndex = recipe.recipeList.Count - 1; recipeIndex >= 0; --recipeIndex)
            {
                int requiredCount = recipe.recipeList[recipe.recipeList.Keys.ElementAt(recipeIndex)];
                bool foundInBackpack = false;

                Log.Debug("Cooking 3 patch fired 1");
                for (int itemIndex = Game1.player.Items.Count - 1; itemIndex >= 0; --itemIndex)
                {
                    if (Game1.player.Items[itemIndex] is StardewValley.Object obj && !obj.bigCraftable.Value && (obj.ParentSheetIndex == recipe.recipeList.Keys.ElementAt(recipeIndex) || obj.Category == recipe.recipeList.Keys.ElementAt(recipeIndex) || CraftingRecipe.isThereSpecialIngredientRule(obj, recipe.recipeList.Keys.ElementAt(recipeIndex))))
                    {
                        int toRemove = recipe.recipeList[recipe.recipeList.Keys.ElementAt(recipeIndex)];
                        requiredCount -= obj.Stack;

                        // custom code begins
                        CraftingRecipePatcher.LastUsedItems.Add(new ConsumedItem(obj));
                        if (CraftingRecipePatcher.ShouldConsumeItems)
                        {
                            // custom code ends
                            obj.Stack -= toRemove;
                            if (obj.Stack <= 0)
                                Game1.player.Items[itemIndex] = null;
                        }
                        if (requiredCount <= 0)
                        {
                            foundInBackpack = true;
                            break;
                        }
                    }
                }

                Log.Debug("Cooking 4 patch fired 1");
                if (additional_materials != null && !foundInBackpack)
                {

                    Log.Debug("Cooking 5 patch fired 1");
                    foreach (Chest chest in additional_materials)
                    {
                        if (chest == null)
                            continue;

                        bool removedItem = false;

                        Log.Debug("Cooking 6 patch fired 1");
                        for (int materialIndex = chest.items.Count - 1; materialIndex >= 0; --materialIndex)
                        {
                            if (chest.items[materialIndex] != null && chest.items[materialIndex] is StardewValley.Object && (chest.items[materialIndex].ParentSheetIndex == recipe.recipeList.Keys.ElementAt(recipeIndex) || chest.items[materialIndex].Category == recipe.recipeList.Keys.ElementAt(recipeIndex) || CraftingRecipe.isThereSpecialIngredientRule((StardewValley.Object)chest.items[materialIndex], recipe.recipeList.Keys.ElementAt(recipeIndex))))
                            {
                                int removedCount = Math.Min(requiredCount, chest.items[materialIndex].Stack);
                                requiredCount -= removedCount;
                                // custom code begins
                                CraftingRecipePatcher.LastUsedItems.Add(new ConsumedItem(chest.items[materialIndex] as StardewValley.Object));
                                if (CraftingRecipePatcher.ShouldConsumeItems)
                                {
                                    // custom code ends
                                    chest.items[materialIndex].Stack -= removedCount;
                                    if (chest.items[materialIndex].Stack <= 0)
                                    {
                                        chest.items[materialIndex] = null;
                                        removedItem = true;
                                    }
                                }
                                if (requiredCount <= 0)
                                    break;
                            }
                        }
                        if (removedItem)
                            chest.clearNulls();
                        if (requiredCount <= 0)
                            break;
                    }
                }
            }

            return false;
        }
    }
}
