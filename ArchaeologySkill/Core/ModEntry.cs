using System;
using System.Collections.Generic;
using MoonShared.Attributes;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace ArchaeologySkill.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        internal static Config Config;
        internal static Assets Assets;
        internal static bool XPDisplayLoaded => Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");
        internal static bool JsonAssetsLoaded => Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool DynamicGameAssetsLoaded => Instance.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");

        public const string SkillID = "moonslime.Archaeology";


        internal readonly List<Func<Item, (int? SkillIndex, string SpaceCoreSkillName)?>> ToolSkillMatchers =
        [
            o => o is Hoe ? (null, SkillID) : null,
            o => o is Pan ? (null, SkillID) : null
        ];

        public ITranslationHelper I18N => this.Helper.Translation;
        internal static IJsonAssetsApi JAAPI;
        internal static IDynamicGameAssetsApi DGAAPI;
        internal static IXPDisplayApi XpAPI;

        public static Dictionary<string, List<string>> ItemDefinitions;
        public static readonly IList<string> BonusLootTable = [];
        public static readonly IList<string> ArtifactLootTable = [];
        public static readonly IList<string> WaterSifterLootTable = [];
        public static readonly IList<string> BonusLootTable_GI = [];
        public static readonly IList<string> WaterSifterLootTable_GI = [];



        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Parser.InitEvents(helper);
            Parser.ParseAll(this);
        }

    }
}
