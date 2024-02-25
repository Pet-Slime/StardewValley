using MoonShared.Config;

namespace ShovelToolUpgrades
{
    [ConfigClass]
    internal class Config
    {

        [ConfigOption(Min = 0, Max = 500, Interval = 1)]
        public int ShovelMaxEnergyUsage { get; set; } = 20;

        [ConfigOption(Min = 0, Max = 400, Interval = 1)]
        public int ShovelEnergyDecreasePerLevel { get; set; } = 3;


        [ConfigOption]
        public bool MargoCompact { get; set; } = false;
    }
}
