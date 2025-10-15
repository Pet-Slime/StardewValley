using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewValley;

namespace WizardryManaBar.Core
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool ArsVeneficiLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("HeyImAmethyst.ArsVenefici");
        internal static bool MagicStardewLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C");

        internal ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Parser.ParseAll(this);
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
