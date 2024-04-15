using BirbCore.Attributes;
using MoonShared.Config;

namespace CookingSkill
{
    [SConfig]
    public class Config
    {

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromCooking { get; set; } = 2;
    }
}
