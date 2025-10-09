using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using MagicSkillCode.API;
using MagicSkillCode.Framework;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewValley;

namespace MagicSkillCode.Core
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;
        internal static MapEditor Editor;
        internal static LegacyDataMigrator LegacyDataMigrator;
        internal  long NewID;

        public Api Api;

        public static bool HasStardewValleyExpanded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
        internal ITranslationHelper I18N => this.Helper.Translation;
        public static IManaBarApi Mana;


        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            Instance = this;
            LegacyDataMigrator = new(this.Monitor);

            GameLocation.RegisterTileAction("MagicAltar", MagicSkillCode.Framework.Magic.HandleMagicAltar);
            GameLocation.RegisterTileAction("MagicRadio", MagicSkillCode.Framework.Magic.HandleMagicRadio);
            Parser.ParseAll(this);
        }

        /// <summary>Get an API that other mods can access. This is always called after <see cref="M:StardewModdingAPI.Mod.Entry(StardewModdingAPI.IModHelper)" />.</summary>
        public override object GetApi()
        {
            try
            {
                return this.Api ??= new Api();
            } catch
            {
                return null;
            }
        }
    }
}
