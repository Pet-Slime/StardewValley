using System.Reflection;
using MoonShared.APIs;
using BirbCore.Attributes;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using WizardrySkill.API;
using WizardrySkill.Core.Framework;

namespace WizardrySkill.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;
        internal static MapEditor Editor;
        internal static LegacyDataMigrator LegacyDataMigrator;
        internal long NewID;

        public Api Api;

        public static bool HasStardewValleyExpanded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
        internal ITranslationHelper I18N => this.Helper.Translation;
        public static IManaBarApi Mana;


        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            Instance = this;
            Assembly assembly = this.GetType().Assembly;
            LegacyDataMigrator = new(this.Monitor);

            GameLocation.RegisterTileAction("MagicAltar", Events.HandleMagicAltar);
            GameLocation.RegisterTileAction("MagicRadio", Events.HandleMagicRadio);
            ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += Events.GameLaunched;

            BirbCore.Attributes.Parser.ParseAll(this);
            TriggerActionManager.RegisterAction(
            $"moonslime.WizardrySkill.learnedmagic",
            LearnedMagic);

            TriggerActionManager.RegisterAction(
            $"moonslime.WizardrySkill.learnedspell",
            LearnedSPell);
        }

        /// <summary>Get an API that other mods can access. This is always called after <see cref="M:StardewModdingAPI.Mod.Entry(StardewModdingAPI.IModHelper)" />.</summary>
        public override object GetApi()
        {
            try
            {
                return this.Api ??= new Api();
            }
            catch
            {
                return null;
            }
        }

        static bool LearnedMagic(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
            {
                return false;
            }
            Utilities.LearnedMagic(points);
            return true;
        }

        static bool LearnedSPell(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGet(args, 1, out string points, out error))
            {
                return false;
            }
            Utilities.LearnedSpell(points);
            return true;
        }
    }
}
