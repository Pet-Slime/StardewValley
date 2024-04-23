using StardewModdingAPI;
using BirbCore.Attributes;

namespace ExpControl.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        internal static Config Config;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Parser.ParseAll(this);
        }
    }
}
