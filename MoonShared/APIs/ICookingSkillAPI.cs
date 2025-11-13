using System;
using System.Collections.Generic;
using StardewValley;

namespace MoonShared.APIs
{
    public interface ICookingSkillAPI
    {

        event Action<IPostCookEvent> PostCook;
    }

    public interface IPostCookEvent
    {
        CraftingRecipe Recipe { get; }
        Farmer Player { get; }
        IList<StardewValley.Object> CookedItems { get; set; }
        IList<IList<Item>> ConsumedItems { get; }
    }
}
