using MoonShared.Attributes;
using StardewModdingAPI;

namespace MultipleConstructionOrders.Core
{

    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        internal ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);
        }
    }
}
