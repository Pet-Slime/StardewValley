using System.Collections.Generic;
using MoonShared.APIs;
using StardewModdingAPI;
using IJsonAssetsApi = MoonShared.APIs.IJsonAssetsApi;
using BirbCore.Attributes;
using SpaceCore.Events;
using CookingSkill.Core;

namespace CookingSkill
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool JALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool MargoLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Overhaul");

        internal static IJsonAssetsApi JsonAssets;

        internal ITranslationHelper I18n => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Parser.ParseAll(this);
        }



    }
}
