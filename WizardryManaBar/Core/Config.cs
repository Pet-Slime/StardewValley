using MoonShared.Attributes;

namespace WizardryManaBar.Core
{
    internal static class ManaBarModes
    {
        public const string Compact = "Compact";
        public const string Custom = "Custom";
    }

    [SConfig]
    public class Config
    {
        [SConfig.Option()]
        public bool RenderManaBar { get; set; } = true;


        // Compact uses the vanilla bar frame + ManaBarIcon; Custom uses manabg.png as the full frame.
        [SConfig.Option(new string[] { ManaBarModes.Compact, ManaBarModes.Custom })]
        public string ManaBarMode { get; set; } = ManaBarModes.Compact;


        [SConfig.Option(-500, 500, 1)]
        public int XManaBarOffset { get; set; } = 0;


        [SConfig.Option(0, 1000, 1)]
        public int YManaBarOffset { get; set; } = 0;


        [SConfig.Option(-1, 10, 1)]
        public int ManaBarExtraSnaps { get; set; } = 0;

        [SConfig.Option(100, 1000, 10)]
        public int ManaBarGrowthLimit { get; set; } = 500;

    }
}
