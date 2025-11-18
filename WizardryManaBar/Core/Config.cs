using MoonShared.Attributes;

namespace WizardryManaBar.Core
{
    [SConfig]
    public class Config
    {
        [SConfig.Option()]
        public bool RenderManaBar { get; set; } = true;


        [SConfig.Option(-500, 500, 1)]
        public int XManaBarOffset { get; set; } = 0;


        [SConfig.Option(0, 1000, 1)]
        public int YManaBarOffset { get; set; } = 0;


        [SConfig.Option(-1, 10, 1)]
        public int ManaBarExtraSnaps { get; set; } = 0;

    }
}
