using MoonShared;
using MoonShared.Config;
using MoonShared.Command;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using MoonShared.Asset;
using MoonShared.APIs;
using HarmonyLib;
using System.IO;

namespace ScytheToolUpgrades
{
    internal class ModEntry : Mod
    {
        public static ModEntry Instance;
        public static Config Config;
        public static Assets Assets;

        internal static bool RadiationTier => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.MoonMisadventures");


        internal static int MythicitePlaceholder = 852;

        public static IJsonAssetsApi JsonAssets;
        public static ISpaceCore SpaceCore;

        internal ITranslationHelper I18n => this.Helper.Translation;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Log.Init(this.Monitor);

            Config = helper.ReadConfig<Config>();

            Assets = new Assets();
            new AssetClassParser(this, Assets).ParseAssets();

            this.Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            SpaceCore = this.Helper.ModRegistry
                .GetApi<ISpaceCore>
                ("spacechase0.SpaceCore");
            if (SpaceCore is null)
            {
                Log.Error("Can't access the SpaceCore API. Is the mod installed correctly?");
                return;
            }



            new ConfigClassParser(this, Config).ParseConfigs();
            new Harmony(this.ModManifest.UniqueID).PatchAll();
            new CommandClassParser(this.Helper.ConsoleCommands, new Command()).ParseCommands();

            SpaceCore.RegisterSerializerType(typeof(UpgradeableScythe));

        }

        public static int PriceForToolUpgradeLevel(int level)
        {
            return level switch
            {
                1 => 3125,
                2 => 6250,
                3 => 12500,
                4 => 25000,
                5 => 50000,
                _ => 2000,
            };
        }

        public static int IndexOfExtraMaterialForToolUpgrade(int level)
        {
            return level switch
            {
                1 => 334,
                2 => 335,
                3 => 336,
                4 => 337,
                5 => 910,
                _ => 334,
            };
        }
    }
}
