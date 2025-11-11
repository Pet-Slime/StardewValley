using System.Collections.Generic;
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

        public static List<Vector2> TilesAffected(Vector2 tileLocation, int power, Farmer who)
        {
            power++;
            var list = new List<Vector2> { tileLocation };

            // Direction vectors for facing: 0=Up, 1=Right, 2=Down, 3=Left
            Vector2[] dir = { new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0) };
            Vector2 facing = dir[who.FacingDirection];
            Vector2 side = new Vector2(-facing.Y, facing.X); // perpendicular (for spreading sideways)

            if (power >= 6)
            {
                // Special AoE centered two tiles ahead
                Vector2 center = tileLocation + facing * 2f;
                list.Clear();
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        list.Add(center + new Vector2(x, y));
                    }
                }
                return list;
            }

            // Power 2: line forward (2 tiles)
            if (power >= 2)
            {
                list.Add(tileLocation + facing * 1f);
                list.Add(tileLocation + facing * 2f);
            }

            // Power 3: extend further (2 more tiles)
            if (power >= 3)
            {
                list.Add(tileLocation + facing * 3f);
                list.Add(tileLocation + facing * 4f);
            }

            // Power 4: replace last two with a 3-wide column around 2 steps forward
            if (power >= 4)
            {
                list.RemoveRange(list.Count - 2, 2);
                for (int offset = -1; offset <= 1; offset++)
                {
                    list.Add(tileLocation + facing * 0f + side * offset);   // at origin
                    list.Add(tileLocation + facing * 1f + side * offset);   // one step forward
                    list.Add(tileLocation + facing * 2f + side * offset);   // two steps forward
                }
            }

            // Power 5: duplicate everything shifted 3 tiles further in facing direction
            if (power >= 5)
            {
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    list.Add(list[i] + facing * 3f);
                }
            }

            return list;
        }


    }
}
