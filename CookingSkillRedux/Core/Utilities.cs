using System;
using System.Collections.Generic;
using MoonShared;
using StardewValley;
using System.Linq;

namespace CookingSkill
{
    internal class Utilities
    {

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(Game1.getFarmer(who.UniqueMultiplayerID), "moonslime.Cooking", amount);
        }

        public static int GetLevel(StardewValley.Farmer who)
        {
            var player = Game1.getFarmer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Cooking") + SpaceCore.Skills.GetSkillBuffLevel(player, "moonslime.Cooking");
        }
    }
}
