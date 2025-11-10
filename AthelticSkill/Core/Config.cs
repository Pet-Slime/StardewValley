using MoonShared.Attributes;
using StardewModdingAPI.Utilities;

namespace AthleticSkill.Core
{
    [SConfig]
    public class Config
    {

        [SConfig.Option()]
        public bool AlternativeStrongmanProfession { get; set; } = false;

        [SConfig.Option()]
        public bool AlternativeSkillPageIcon { get; set; } = false;

        [SConfig.Option()]
        public KeybindList Key_Cast { get; set; } = KeybindList.Parse("LeftControl");

        [SConfig.Option()]
        public bool ToggleSprint { get; set; } = false;

        [SConfig.Option(6, 100, 1)]
        public int MinimumEnergyToSprint { get; set; } = 20;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromSprinting { get; set; } = 1;


        [SConfig.Option(0, 100, 1)]
        public int ExpChanceFromTools { get; set; } = 25;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromLightToolUse { get; set; } = 2;

        [SConfig.Option(1, 100, 1)]
        public int ExpFromHeavyToolUse { get; set; } = 3;


        [SConfig.Option(1, 8, 1)]
        public int SprintSpeed { get; set; } = 1;


        [SConfig.Option(1, 20, 1)]
        public int SprintingExpEvent { get; set; } = 8;


        [SConfig.Option()]
        public bool BuffIcon { get; set; } = false;
    }
}
