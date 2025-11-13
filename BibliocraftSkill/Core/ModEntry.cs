using System.Collections.Generic;
using StardewModdingAPI;

namespace BibliocraftSkill.Core
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal ITranslationHelper I18N => this.Helper.Translation;

        public const string SkillID = "moonslime.Bibliocraft";


        public static readonly Dictionary<string, string> MailingList = [];

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);
        }
    }
}
