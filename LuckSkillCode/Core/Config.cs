using MoonShared.Attributes;

namespace LuckSkill.Core
{
    [SConfig]
    public class Config
    {
        [SConfig.Option(0, 4000, 1)]
        public int DailyLuckExpBonus { get; set; } = 750;

        [SConfig.Option(1, 100, 1)]
        public int QuestChecks { get; set; } = 3;
    }
}
