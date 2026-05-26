using StardewValley;

namespace CookingSkillRedux
{
    internal static class Utilities
    {
        public static Item BetterCraftingTempItem;

        private const string SkillBookItemId = "moonslime.Cooking.skill_book";
        private const float LevelValuePerLevel = 0.03f;

        private const double SkillBookChanceGate = 0.02;
        private const double SkillBookChanceAfterGate = 0.02;

        public static void AddEXP(Farmer who, int amount)
        {
            if (amount <= 0)
                return;

            Farmer player = Game1.GetPlayer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(player, ModEntry.SkillID, amount);

            if (!RollSkillBookDrop())
                return;

            Game1.createMultipleObjectDebris(SkillBookItemId, who.TilePoint.X, who.TilePoint.Y, 1, who.UniqueMultiplayerID, who.currentLocation);
        }

        private static bool RollSkillBookDrop()
        {
            if (Game1.random.NextDouble() >= SkillBookChanceGate)
                return false;

            if (Game1.random.NextDouble() >= SkillBookChanceAfterGate)
                return false;

            return true;
        }

        public static float GetLevelValue(Farmer who, bool additive = false)
        {
            float levelValue = GetLevel(who) * LevelValuePerLevel;
            return additive ? levelValue : levelValue + 1f;
        }

        public static int GetLevel(Farmer who)
        {
            Farmer player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, ModEntry.SkillID) + SpaceCore.Skills.GetSkillBuffLevel(player, ModEntry.SkillID);
        }
    }
}
