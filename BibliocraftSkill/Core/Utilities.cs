using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace BibliocraftSkill
{
    public class Utilities
    {

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(Game1.GetPlayer(who.UniqueMultiplayerID), "moonslime.Biblocraft", amount);
        }

        public static int GetLevel(StardewValley.Farmer who, bool baseLevelOnly = false)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            if (baseLevelOnly)
            {
                return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Biblocraft");
            } else
            {
                return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Biblocraft") + SpaceCore.Skills.GetSkillBuffLevel(player, "moonslime.Biblocraft");
            }

        }
    }
}
