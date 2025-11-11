using System;
using System.Collections.Generic;
using System.Text;
using SpaceCore;
using StardewValley;

namespace MoonSharedSpaceCore
{
    public class SpaceUtilities
    {
        public static void LearnRecipesOnLoad(Farmer player, string Id)
        {
            int skillLevel = player.GetCustomSkillLevel(Id);
            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(Id))
                    continue;
                if (conditions.Split(" ").Length < 2)
                    continue;

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                    continue;

                player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CookingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }
            }
        }
    }
}
