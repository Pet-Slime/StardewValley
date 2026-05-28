using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Attributes;
using MoonShared.APIs;
using StardewModdingAPI;

namespace ThievingSkill.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;

            MoonShared.Attributes.Parser.InitEvents(helper);
            Parser.ParseAll(this);
        }
    }
}
