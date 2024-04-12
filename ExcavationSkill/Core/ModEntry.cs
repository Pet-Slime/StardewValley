using System;
using System.Collections.Generic;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using BirbCore.Attributes;
using HarmonyLib;
using MoonShared.Patching;
using ArchaeologySkill.Objects.Water_Shifter;
using StardewModdingAPI.Events;
using SpaceShared.APIs;

namespace ArchaeologySkill
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;
        public static string ModDataKey;

        internal static Config Config;
        internal static Assets Assets;
        internal static bool XPDisplayLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");
        internal static bool JsonAssetsLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool DynamicGameAssetsLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");

        internal readonly List<Func<Item, (int? SkillIndex, string? SpaceCoreSkillName)?>> ToolSkillMatchers =
        [
            o => o is Hoe ? (null, "moonslime.Archaeology") : null,
            o => o is Pan ? (null, "moonslime.Archaeology") : null
        ];

        public ITranslationHelper I18N => this.Helper.Translation;
        internal static MoonShared.APIs.IJsonAssetsApi JAAPI;
        internal static IDynamicGameAssetsApi DGAAPI;
        internal static IXPDisplayApi XpAPI;

        public static Dictionary<string, List<string>> ItemDefinitions;
        public static readonly IList<string> BonusLootTable = [];
        public static readonly IList<string> ArtifactLootTable = [];
        public static readonly IList<string> WaterSifterLootTable = [];


        internal static ObjectInformation ObjectInfo;
        internal bool ValidateInventory = true;



        public override void Entry(IModHelper helper)
        {
            Instance = this;
            ModDataKey = $"{helper.ModRegistry.ModID}.water_shifter";
            ObjectInfo = Helper.Data.ReadJsonFile<ObjectInformation>($"assets/data.json");
            Instance.Helper.Events.GameLoop.GameLaunched += Instance.OnGameLaunched;
            Instance.Helper.Events.GameLoop.ReturnedToTitle += (_, _) => Instance.ValidateInventory = true;

            Parser.ParseAll(this);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (JsonAssetsLoaded)
                JAAPI = ModEntry.Instance.Helper.ModRegistry.GetApi<MoonShared.APIs.IJsonAssetsApi>("spacechase0.JsonAssets");
            if (DynamicGameAssetsLoaded)
                DGAAPI = Instance.Helper.ModRegistry.GetApi<IDynamicGameAssetsApi>("spacechase0.DynamicGameAssets");


            var sc = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(WaterShifter));

        }
    }
}
