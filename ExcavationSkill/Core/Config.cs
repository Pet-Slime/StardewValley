using BirbCore.Attributes;
using MoonShared.Config;

namespace ArchaeologySkill
{
    [SConfig]
    public class Config
    {

        [SConfig.Option(1, 2, 1)]
        public int AlternativeSkillPageIcon { get; set; } = 1;


        [SConfig.Option(0f, 5f, 0.1f)]
        public float DisplaySellPrice { get; set; } = 1f;



        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromArtifactSpots { get; set; } = 20;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromPanSpots { get; set; } = 30;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromMinesDigging { get; set; } = 10;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromWaterShifter { get; set; } = 5;



        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromResearchTable { get; set; } = 10;

        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromAncientBattery { get; set; } = 10;


        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromPreservationChamber { get; set; } = 10;


        [SConfig.Option(0, 100, 1)]
        public int ExperienceFromHPreservationChamber { get; set; } = 20;
    }
}
