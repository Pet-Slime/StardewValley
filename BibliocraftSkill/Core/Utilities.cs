using StardewValley;

namespace BibliocraftSkill.Core
{
    public class Utilities
    {

        public static void AddEXP(Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(Game1.GetPlayer(who.UniqueMultiplayerID), ModEntry.SkillID, amount);
        }

        public static int GetLevel(Farmer who, bool baseLevelOnly = false)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            if (baseLevelOnly)
            {
                return SpaceCore.Skills.GetSkillLevel(player, ModEntry.SkillID);
            } else
            {
                return SpaceCore.Skills.GetSkillLevel(player, ModEntry.SkillID) + SpaceCore.Skills.GetSkillBuffLevel(player, ModEntry.SkillID);
            }

        }


    }
}
