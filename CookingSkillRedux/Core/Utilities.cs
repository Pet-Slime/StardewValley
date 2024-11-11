using System;
using System.Collections.Generic;
using MoonShared;
using StardewValley;
using System.Linq;
using BirbCore.Attributes;
using xTile.Dimensions;
using StardewValley.Locations;

namespace CookingSkill
{
    internal class Utilities
    {
        public static Item BetterCraftingTempItem;

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            if (amount > 0)
            {
                SpaceCore.Skills.AddExperience(Game1.GetPlayer(who.UniqueMultiplayerID), "moonslime.Cooking", amount);
                if (Game1.random.NextDouble() < 0.05)
                {
                    Game1.createMultipleObjectDebris("moonslime.Cooking.skill_book", who.TilePoint.X, who.TilePoint.Y, 1, who.UniqueMultiplayerID);
                }
            }
        }

        public static float GetLevelValue(StardewValley.Farmer who, bool additive = false)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            float level = SpaceCore.Skills.GetSkillLevel(player, "moonslime.Cooking") + SpaceCore.Skills.GetSkillBuffLevel(player, "moonslime.Cooking");
            if (additive)
            {
                float sendback = (level * 0.03f);
                return sendback;
            } else
            {
                float sendback = (level * 0.03f) + 1f;
                return sendback;
            }
        }


        public static int GetLevel(StardewValley.Farmer who)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Cooking") + SpaceCore.Skills.GetSkillBuffLevel(player, "moonslime.Cooking");
        }
    }
}
