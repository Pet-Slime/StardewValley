using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Menus;
using static BirbCore.Attributes.SMod;

namespace AthleticSkill
{
    internal class Utilities
    {
        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, "moonslime.Athletic", amount);
        }

        public static int GetLevel(StardewValley.Farmer who)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Athletic") + SpaceCore.Skills.GetSkillBuffLevel(player, "moonslime.Athletic");
        }
    }
}
