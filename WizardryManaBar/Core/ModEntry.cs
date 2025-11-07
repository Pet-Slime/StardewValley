using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;

namespace WizardryManaBar.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool ArsVeneficiLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("HeyImAmethyst.ArsVenefici");
        internal static bool MagicStardewLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C");

        public ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Assembly assembly = this.GetType().Assembly;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);
            ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += Events.GameLaunched;
        }

        public override object GetApi()
        {
            try
            {
                return new WizardryManaBar.Core.Api();
            }
            catch
            {
                return null;
            }

        }
    }
}
