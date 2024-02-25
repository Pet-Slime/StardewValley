using SpaceCore;
using StardewValley;

namespace MoonShared
{
    static class FarmerExtensions
    {
        public static bool HasCustomPrestigeProfession(this Farmer player, Skills.Skill.Profession profession)
        {
            return player.professions.Contains(profession.GetVanillaId() + 100);
        }
    }
}
