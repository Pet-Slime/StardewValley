using MoonShared.Attributes;

namespace CookingSkillRedux
{
    [SConfig]
    public class Config
    {

        [SConfig.Option(0, 2, 1)]
        public int AlternativeSkillPageIcon { get; set; } = 0;


        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromCooking { get; set; } = 2;


        [SConfig.Option(0.0f, 1.0f, 0.1f)]
        public float ExperienceFromEdibility { get; set; } = 0.50f;


        [SConfig.Option(1, 4000, 1)]
        public int BonusExpLimit { get; set; } = 11;
    }
}
