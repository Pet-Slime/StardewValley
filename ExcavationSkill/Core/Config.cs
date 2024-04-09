using MoonShared.Config;

namespace ArchaeologySkill
{
    [ConfigClass(I18NNameSuffix = "")]
    public class Config
    {
        [ConfigOption]
        public bool AlternativeSkillPageIcon { get; set; } = false;


        [ConfigOption]
        public bool EnablePrestige{ get; set; } = false;

        [ConfigOption(Min = 0, Max = 100, Interval = 1)]
        public int ExperienceFromArtifactSpots { get; set; } = 10;

        [ConfigOption(Min = 0, Max = 100, Interval = 1)]
        public int ExperienceFromMinesDigging { get; set; } = 5;


        [ConfigOption(Min = 0, Max = 100, Interval = 1)]
        public int ExperienceFromBuriedAndPannedItem { get; set; } = 5;


        [ConfigOption(Min = 0, Max = 100, Interval = 1)]
        public int ExperienceFromWaterShifter { get; set; } = 2;
    }
}
