using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using StardewModdingAPI;

namespace AthleticSkill
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal ITranslationHelper I18N => this.Helper.Translation;

        internal static bool IsWoLLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Professions");


        internal static bool UseAltProfession;

        public override void Entry(IModHelper helper)
        {
            UseAltProfession = false;
            Instance = this;
            Parser.ParseAll(this);
        }
    }
}
