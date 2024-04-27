using SpaceCore;
using StardewValley;
using StardewValley.Menus;
using MoonShared;
using static BirbCore.Attributes.SMod;

namespace SpookySkill
{
    internal class Utilities
    {

        const string Boo = "moonslime.Spooky";

        public static bool IsBetween(int x, int low, int high)
        {
            return low <= x && x <= high;
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            var farmer = Game1.getFarmer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, Boo, amount);
        }

        public static int GetLevel(StardewValley.Farmer who)
        {
            var player = Game1.getFarmer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, Boo) + SpaceCore.Skills.GetSkillBuffLevel(player, Boo);
        }
    }
}
