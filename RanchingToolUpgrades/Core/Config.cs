using MoonShared.Config;

namespace RanchingToolUpgrades
{
    [ConfigClass]
    internal class Config
    {

        [ConfigOption]
        public bool MargoCompact { get; set; } = false;


        [ConfigOption(Min = 0, Max = 100000, Interval = 500)]
        public int PailBuyCost { get; set; } = 1000;

        [ConfigOption(Min = 0, Max = 3, Interval = 0.1f)]
        public float PailUpgradeCostMultiplier { get; set; } = 1.0f;

        [ConfigOption(Min = 1, Max = 20, Interval = 1)]
        public int PailUpgradeCostBars { get; set; } = 5;

        [ConfigOption(Min = 1, Max = 5, Interval = 1)]
        public int PailUpgradeDays { get; set; } = 2;

        [ConfigOption(Min = 0, Max = 100000, Interval = 500)]
        public int ShearsBuyCost { get; set; } = 1000;

        [ConfigOption(Min = 0, Max = 3, Interval = 0.1f)]
        public float ShearsUpgradeCostMultiplier { get; set; } = 1.0f;

        [ConfigOption(Min = 1, Max = 20, Interval = 1)]
        public int ShearsUpgradeCostBars { get; set; } = 5;

        [ConfigOption(Min = 1, Max = 5, Interval = 1)]
        public int ShearsUpgradeDays { get; set; } = 2;

        /*  
        public bool BuyableAutograbber { get; set; } = true;

        public int AutograbberBuyCost { get; set; } = 25000;

        public float AutograbberUpgradeCostMultiplier { get; set; } = 5.0f;

        public int AutograbberUpgradeCostBars { get; set; } = 10;

        public int AutograbberUpgradeDays { get; set; } = 2;*/

        // N extra friendship per upgrade level.
        [ConfigOption(Min = 0, Max = 10, Interval = 1)]
        public int ExtraFriendshipBase { get; set; } = 2;

        // N% chance of higher quality goods.
        [ConfigOption(Min = 0, Max = 1, Interval = 0.01f)]
        public float QualityBumpChanceBase { get; set; } = 0.05f;

        // N% chance of double produce.
        [ConfigOption(Min = 0, Max = 1, Interval = 0.01f)]
        public float ExtraProduceChance { get; set; } = 0.1f;

        [ConfigOption]
        public bool BuyablePan { get; set; } = false;

        [ConfigOption(Min = 0, Max = 100000, Interval = 500)]
        public int BuyCost { get; set; } = 1000;

        [ConfigOption(Min = 0, Max = 3, Interval = 0.1f)]
        public float UpgradeCostMultiplier { get; set; } = 1.0f;

        [ConfigOption(Min = 1, Max = 20, Interval = 1)]
        public int UpgradeCostBars { get; set; } = 5;

        [ConfigOption(Min = 1, Max = 5, Interval = 1)]
        public int UpgradeDays { get; set; } = 2;

        [ConfigOption(Min = 0, Max = 1, Interval = 0.01f)]
        public float ExtraDrawBaseChance { get; set; } = 0.7f;

        [ConfigOption(Min = 0, Max = 1, Interval = 0.01f)]
        public float DailyLuckMultiplier { get; set; } = 1.0f;

        [ConfigOption(Min = 0, Max = 1, Interval = 0.01f)]
        public float LuckLevelMultiplier { get; set; } = 0.1f;

        [ConfigSectionTitle("AnimationSection")]

        [ConfigParagraph("AnimationSectionText")]

        [ConfigOption]
        public int AnimationFrameDuration { get; set; } = 140;

        [ConfigOption]
        public int AnimationYOffset { get; set; } = -8;
    }
}
