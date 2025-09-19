using BirbCore.Attributes;
using MoonShared.APIs;
using StardewModdingAPI;

namespace SpaceCoreLevels.Core
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;

        internal ITranslationHelper I18N => this.Helper.Translation;
        public static bool SpaceCoreLoaded => Instance.Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore");

        /// <summary>The interfact in which we register SpaceCore's API to.</summary>
        internal static ISpaceCore ISpaceCore;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Parser.ParseAll(this);
        }
    }
}
