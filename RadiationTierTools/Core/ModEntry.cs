using MoonShared;
using MoonShared.Config;
using MoonShared.Command;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using MoonShared.Asset;
using MoonShared.APIs;
using HarmonyLib;
using MoonShared.Patching;
using RadiationTierTools.Patches;
using StardewValley.Menus;
using StardewValley.Tools;
using StardewValley;

namespace RadiationTierTools
{
    internal class ModEntry : Mod
    {
        public static ModEntry Instance;
        public static Config Config;
        public static Assets Assets;
        internal static bool RanchToolUpgrades => ModEntry.Instance.Helper.ModRegistry.IsLoaded("drbirbdev.RanchingToolUpgrades");

        public static ISpaceCore SpaceCore;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Log.Init(this.Monitor);
            I18n.Init(helper.Translation);

            Config = helper.ReadConfig<Config>();

            Assets = new Assets();
            new AssetClassParser(this, Assets).ParseAssets();

            this.Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
 ///           this.Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            new ConfigClassParser(this, Config).ParseConfigs();
            HarmonyPatcher.Apply(this,
                new UpgradePrices_Patcher(),
                new UpgradeItems_Patcher(),
                new Blacksmith_Patcher());
            new Harmony(this.ModManifest.UniqueID).PatchAll();
            new CommandClassParser(this.Helper.ConsoleCommands, new Command()).ParseCommands();

            SpaceCore = this.Helper.ModRegistry
                .GetApi<ISpaceCore>
                ("spacechase0.SpaceCore");
            if (SpaceCore is null)
            {
                Log.Error("Can't access the SpaceCore API. Is the mod installed correctly?");
                return;
            }
        }
    }
}
