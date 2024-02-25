using System;
using System.Collections.Generic;
using MoonShared;
using StardewValley;
using System.Linq;

namespace CookingSkill
{
    internal class Utilities
    {



        internal static void PopulateMissingRecipes()
        {

            // Add any missing starting recipes
            foreach (string recipe in CookingSkill.StartingRecipes)
            {
                if (!Game1.player.cookingRecipes.ContainsKey(recipe))
                {
                    Log.Trace($"Added missing starting recipe {recipe}");
                    Game1.player.cookingRecipes.Add(recipe, 0);
                }
            }
        
            // Add any missing recipes from the level-up recipe table
            int level = GetLevel();
            IReadOnlyDictionary<int, IList<string>> recipes = (IReadOnlyDictionary<int, IList<string>>)CookingSkill.CookingSkillLevelUpRecipes;
            IEnumerable<string> missingRecipes = recipes
                // Take all recipe lists up to the current level
                .TakeWhile(pair => pair.Key < level)
                .SelectMany(pair => pair.Value) // Flatten recipe lists into their recipes
                .Select(r => ModEntry.ObjectPrefix + r) // Add item prefixes
                .Where(r => !Game1.player.cookingRecipes.ContainsKey(r)); // Take recipes not known by the player
            foreach (string recipe in missingRecipes)
            {
                Log.Trace($"Added missing recipe {recipe}");
                Game1.player.cookingRecipes.Add(recipe, 0);
            }
        
        }

        public static void AddAndDisplayNewRecipesOnLevelUp(SpaceCore.Interface.SkillLevelUpMenu menu, int level)
        {
            List<CraftingRecipe> cookingRecipes = 
                GetCookingRecipesForLevel(level)
                .ToList()
                .ConvertAll(name => new CraftingRecipe(ModEntry.ObjectPrefix + name, true))
                .Where(recipe => !Game1.player.knowsRecipe(recipe.name))
                .ToList();


            if (cookingRecipes is not null && cookingRecipes.Count > 0)
            {
                foreach (CraftingRecipe recipe in cookingRecipes.Where(r => !Game1.player.cookingRecipes.ContainsKey(r.name)))
                {
                    Game1.player.cookingRecipes[recipe.name] = 0;
                }
            }

            // Add crafting recipes
            var craftingRecipes = new List<CraftingRecipe>();
            // No new crafting recipes currently.

            // Apply new recipes
            List<CraftingRecipe> combinedRecipes = craftingRecipes
                    .Concat(cookingRecipes)
                    .ToList();
            ModEntry.Instance.Helper.Reflection
                .GetField<List<CraftingRecipe>>(menu, "newCraftingRecipes")
                .SetValue(combinedRecipes);
            Log.Debug(combinedRecipes.Aggregate($"New recipes for level {level}:", (total, cur) => $"{total}{Environment.NewLine}{cur.name} ({cur.createItem().ParentSheetIndex})"));

            // Adjust menu to fit if necessary
            const int defaultMenuHeightInRecipes = 4;
            int menuHeightInRecipes = combinedRecipes.Count + combinedRecipes.Count(recipe => recipe.bigCraftable);
            if (menuHeightInRecipes >= defaultMenuHeightInRecipes)
            {
                menu.height += (menuHeightInRecipes - defaultMenuHeightInRecipes) * StardewValley.Object.spriteSheetTileSize * Game1.pixelZoom;
            }
        }

        public static int GetLevel()
        {
            return SpaceCore.Skills.GetSkillLevel(Game1.player, "spacechase0.Cooking");
        }

        /// <returns>New recipes learned when reaching this level.</returns>
        public static IReadOnlyList<string> GetCookingRecipesForLevel(int level)
        {
            // Level undefined
            if (!CookingSkill.CookingSkillLevelUpRecipes.ContainsKey(level))
            {
                return new List<string>();
            }
            // Level used for professions, no new recipes added
            if (level % 5 == 0)
            {
                return new List<string>();
            }
            return (IReadOnlyList<string>)CookingSkill.CookingSkillLevelUpRecipes[level];
        }
    }
}
