using MoonShared.Attributes;

namespace LuckSkill.Core
{
    [SConfig]
    public class Config
    {
        [SConfig.Option(0, 4000, 1)]
        public int DailyLuckExpBonus { get; set; } = 750;
        public int QuestChecks { get; set; } = 3;
    }
}
