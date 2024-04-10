using System;
using System.Collections.Generic;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using BirbCore.Attributes;
using HarmonyLib;
using MoonShared.Patching;

namespace ArchaeologySkill
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        internal static Config Config;
        internal static Assets Assets;
        internal static bool XPDisplayLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");

        internal readonly List<Func<Item, (int? SkillIndex, string? SpaceCoreSkillName)?>> ToolSkillMatchers =
        [
            o => o is Hoe ? (null, "moonslime.Archaeology") : null,
            o => o is Pan ? (null, "moonslime.Archaeology") : null
        ];

        internal static IXPDisplayApi XpAPI;
        public static Dictionary<string, List<string>> ItemDefinitions;
        public static readonly IList<string> BonusLootTable = [];
        public static readonly IList<string> ArtifactLootTable = [];

        public ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;


            Parser.ParseAll(this);
        }


    }
}
