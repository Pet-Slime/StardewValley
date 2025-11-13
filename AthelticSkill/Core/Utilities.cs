using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;

namespace AthleticSkill.Core
{
    internal class Utilities
    {

        public static void AddEXP(Farmer who, int amount)
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, ModEntry.SkillID, amount);
        }

        public static int GetLevel(Farmer who, bool original = false, bool buff = false)
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            int baseLevel = SpaceCore.Skills.GetSkillLevel(farmer, ModEntry.SkillID);

            if (original) return baseLevel;

            int buffLevel = SpaceCore.Skills.GetSkillBuffLevel(farmer, ModEntry.SkillID);
            if (buff) return buffLevel;

            return baseLevel + buffLevel;
        }

        public static List<Vector2> TilesAffected(Vector2 origin, int power, Farmer who)
        {
            List<Vector2> tiles = new() { origin };

            // Direction vectors: 0=Up, 1=Right, 2=Down, 3=Left
            Vector2[] dir = { new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0) };
            Vector2 forward = dir[who.FacingDirection];
            Vector2 side = new Vector2(-forward.Y, forward.X);

            int length = 3 + ((power % 2 == 0) ? (power / 2 + 1) : 0);

            int width = 1 + ((power - 1) / 2) * 2;
            int halfWidth = width / 2;

            for (int l = 0; l < length; l++)
            {
                for (int w = -halfWidth; w <= halfWidth; w++)
                {
                    tiles.Add(origin + forward * l + side * w);
                }
            }

            return tiles.Distinct().ToList(); // remove duplicates
        }


    }
}
