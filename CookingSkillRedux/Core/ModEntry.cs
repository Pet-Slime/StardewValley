using StardewModdingAPI;
using IJsonAssetsApi = MoonShared.APIs.IJsonAssetsApi;
using IBetterCraftingApi = MoonShared.APIs.IBetterCrafting;
using BirbCore.Attributes;
using CookingSkill.Core;
using MoonShared.APIs;

namespace CookingSkill
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool JALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool BCLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("leclair.bettercrafting");

        internal static IJsonAssetsApi JsonAssets;
        internal static IBetterCraftingApi BetterCrafting;
        internal static IPostCraftEvent PostCraftEvent;
        internal static IGlobalPerformCraftEvent GlobalPerformCraftingEvent;

        internal ITranslationHelper I18n => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Parser.ParseAll(this);
        }

        public override object GetApi()
        {
            return new CookingAPI();
        }



    }
}
