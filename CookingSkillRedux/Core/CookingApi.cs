using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Objects;
using StardewValley;
using StardewModdingAPI;

namespace CookingSkill.Core
{
    public interface ICookingApi
    {
        /// <summary>
        /// Modify a cooked item based on the player's cooking skill.
        /// Always returns true.
        /// </summary>
        /// <param name="recipe">The crafting recipe.</param>
        /// <param name="item">The crafted item from the recipe. Nothing is changed if the recipe isn't cooking.</param>
        /// <returns>Returns the held item.</returns>
        Item PreCook(CraftingRecipe recipe, Item item);

        /// <summary>
        /// Grants the player EXP and increases the held Item by the value of the crafting recipe.
        /// </summary>
        /// <param name="recipe">The crafting recipe.</param>
        /// <param name="heldItem">The held item, to increase the stack size of and to get the edibility of.</param>
        /// <param name="who"> the player who did the cooking, to grant exp to.</param>
        /// <returns>Returns the held item.</returns>
        Item PostCook(CraftingRecipe recipe, Item heldItem, Farmer who);
    }

    public class CookingAPI : ICookingApi
    {
        public Item PreCook(CraftingRecipe recipe, Item item)
        {
            return CookingSkill.Core.Events.PreCook(recipe, item);
        }

        public Item PostCook(CraftingRecipe recipe, Item heldItem, Farmer who)
        {
            return CookingSkill.Core.Events.PostCook(recipe, heldItem, who);
        }
    }
}
