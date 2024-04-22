using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Menus;
using static BirbCore.Attributes.SMod;

namespace SpookySkill
{
    internal class Utilities
    {
        public static bool IsBetween(int x, int low, int high)
        {
            return low <= x && x <= high;
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            var farmer = Game1.getFarmer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, "moonslime.Spooky", amount);
            MasteryEXPCheck(farmer, amount);
        }

        public static int GetLevel(StardewValley.Farmer who)
        {
            var player = Game1.getFarmer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Spooky");
        }

        public static void MasteryEXPCheck(Farmer who, int howMuch)
        {
            if (who.Level >= 25)
            {
                int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
                Game1.stats.Increment("MasteryExp", howMuch);
                if (MasteryTrackerMenu.getCurrentMasteryLevel() > currentMasteryLevel)
                {
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:Mastery_newlevel"));
                    Game1.playSound("newArtifact");
                }
            }
            else
            {
                Game1.stats.Set("MasteryExp", 0);
            }
        }
    }
}
