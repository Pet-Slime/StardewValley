using BirbCore.Attributes;
using MoonShared.Config;

namespace ArchaeologySkill
{
    [SConfig]
    public class Config
    {

        [SConfig.Option(1, 2, 1)]
        public int AlternativeSkillPageIcon { get; set; } = 1;



        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromArtifactSpots { get; set; } = 10;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromPanSpots { get; set; } = 20;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromMinesDigging { get; set; } = 5;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromWaterShifter { get; set; } = 2;
    }
}
