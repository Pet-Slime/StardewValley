using System.Reflection;
using AthleticSkill.Core.Patches;
using HarmonyLib;
using MoonShared.Attributes;
using StardewModdingAPI;

namespace AthleticSkill.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal ITranslationHelper I18N => this.Helper.Translation;

        internal static bool IsWoLLoaded => Instance.Helper.ModRegistry.IsLoaded("DaLion.Professions");
        internal static bool IsMOLoaded => Instance.Helper.ModRegistry.IsLoaded("Rafseazz.MovementOverhaul");



        internal static bool UseAltProfession;

        public const string SkillID = "moonslime.Athletic";

        public override void Entry(IModHelper helper)
        {
            UseAltProfession = false;
            Instance = this;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);

        }
    }
}
