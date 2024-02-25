using MoonShared;
using MoonShared.Config;
using MoonShared.Command;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using MoonShared.Asset;
using MoonShared.APIs;
using HarmonyLib;
using System.IO;

namespace RanchingToolUpgrades
{
    internal class ModEntry : Mod
    {
        public static ModEntry Instance;
        public static Config Config;
        public static Assets Assets;

        internal static bool RadiationTier => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.MoonMisadventures");
        internal static bool MoonLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.MoonMisadventures");
        internal static bool MargoLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Overhaul");

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

            JsonAssets = this.Helper.ModRegistry
                .GetApi<IJsonAssetsApi>
                ("spacechase0.JsonAssets");
            if (JsonAssets is null)
            {
                Log.Error("Can't access the Json Assets API. Is the mod installed correctly?");
                return;
            }
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

            SpaceCore.RegisterSerializerType(typeof(UpgradeablePail));
            SpaceCore.RegisterSerializerType(typeof(UpgradeableShears));
            SpaceCore.RegisterSerializerType(typeof(UpgradeablePan));

            JsonAssets.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "PanHats"));
        }

        public static int PriceForToolUpgradeLevelClint(int level)
        {
            //If Moon is loaded, Make the price level to 6
            if (MoonLoaded)
            {
                //If Moon and Margo is loaded, Revert prices from Clint to 4, as Radiation and Mythite is made at the forge
                if (MargoLoaded)
                {
                    return level switch
                    {
                        1 => 2000,
                        2 => 5000,
                        3 => 10000,
                        4 => 25000,
                        _ => 2000,
                    };
                }
                //No Margo is loaded, upgrade tool to 6 at clint
                return level switch
                {
                    1 => 2000,
                    2 => 5000,
                    3 => 10000,
                    4 => 25000,
                    5 => 40000,
                    6 => 80000,
                    _ => 2000,
                };
            }
            //No Moon or Margo Loaded, Upgrade tools to Iridium
            return level switch
            {
                1 => 2000,
                2 => 5000,
                3 => 10000,
                4 => 25000,
                5 => 40000,
                _ => 2000,
            };
        }

        public static int IndexOfExtraMaterialForToolUpgradeClint(int level)
        {
            //If Moon is loaded, Make the price level to 6
            if (MoonLoaded)
            {
                //If Moon and Margo is loaded, Revert prices from Clint to 4, as Radiation and Mythite is made at the forge
                if (MargoLoaded)
                {
                    return level switch
                    {
                        1 => ObjectIds.CopperBar,
                        2 => ObjectIds.IronBar,
                        3 => ObjectIds.GoldBar,
                        4 => ObjectIds.IridiumBar,
                        _ => ObjectIds.PrismaticShard,
                    };
                }
                //No Margo is loaded, upgrade tool to 6 at clint
                return level switch
                {
                    1 => ObjectIds.CopperBar,
                    2 => ObjectIds.IronBar,
                    3 => ObjectIds.GoldBar,
                    4 => ObjectIds.IridiumBar,
                    5 => ObjectIds.RadioactiveBar,
                    6 => "spacechase0.MoonMisadventures/Mythicite Bar".GetDeterministicHashCode(),
                    _ => ObjectIds.PrismaticShard,
                };

            }
            //No Moon Loaded, Upgrade tools to Iridium
            return level switch
            {
                1 => ObjectIds.CopperBar,
                2 => ObjectIds.IronBar,
                3 => ObjectIds.GoldBar,
                4 => ObjectIds.IridiumBar,
                5 => ObjectIds.RadioactiveBar,
                _ => ObjectIds.PrismaticShard,
            };
        }
    }
}
