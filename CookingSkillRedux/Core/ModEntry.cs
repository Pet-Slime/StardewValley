using StardewModdingAPI;
using CookingSkillRedux.Core;
using MoonShared.APIs;
using MoonShared.Attributes;

namespace CookingSkillRedux
{
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool BCLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("leclair.bettercrafting");
        internal static bool LoveOfCookingLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("blueberry.LoveOfCooking");

        internal static IBetterCrafting BetterCrafting;
        internal static ICookingSkillAPI LoveOfCooking;
        public const string SkillID = "moonslime.Cooking";

        internal ITranslationHelper I18n => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);
        }

        public override object GetApi()
        {
            try
            {
                return new CookingAPI();
            }
            catch
            {
                return null;
            }

        }
    }
}
