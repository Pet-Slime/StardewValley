using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.APIs;
using MoonShared.Asset;
using MoonShared.Command;
using MoonShared.Config;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using IJsonAssetsApi = MoonShared.APIs.IJsonAssetsApi;
using MoonShared.Patching;
using ArchaeologySkill.Patches;
using StardewValley.Tools;
using SpaceShared.APIs;
using HarmonyLib;
using System.Numerics;
using System.Xml.Linq;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ArchaeologySkill
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

/// <summary>
///
///        Mod no longer uses Json Assets or Profession Framework
/// 
///       internal static bool JALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
///       internal static bool PFWLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Digus.ProducerFrameworkMod");
/// </summary>
/// 
        internal static bool XPDisplayLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");

        private readonly List<Func<Item, (int? SkillIndex, string? SpaceCoreSkillName)?>> ToolSkillMatchers =
        [
            o => o is Hoe ? (null, "moonslime.Archaeology") : null,
            o => o is Pan ? (null, "moonslime.Archaeology") : null
        ];

        internal static IJsonAssetsApi JsonAssets;
        internal static IProducerFrameworkAPI ProducerFramework;
        internal static IXPDisplayApi XpAPI;


        internal ITranslationHelper I18n => this.Helper.Translation;
        internal static Dictionary<string, List<string>> ItemDefinitions;
        internal static IEnumerable<KeyValuePair<string, List<string>>> ArchaeologySkillLevelUpTable;
        public static readonly IList<string> BonusLootTable = new List<string>();
        public static readonly IList<string> ArtifactLootTable = new List<string>();

        
        public override void Entry(IModHelper helper)
        {
            Instance = this;

            //Set up a logger so one can use debug messages
            Log.Init(this.Monitor);

            //Set up the Config Files (there are no configs yet)
            Config = helper.ReadConfig<Config>();

            //Call the Assets File and read them
            Assets = new Assets();
            new AssetClassParser(this, Assets).ParseAssets();

            //Call the game Launch Event
            this.Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;

        }


        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // delay loading our stuff by just a tiny bit, to make sure other mods load first 
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.Event_LoadLate;

            var sc = this.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");


        }

        private void Event_LoadLate(object sender, OneSecondUpdateTickedEventArgs e)
        {
            // Make sure to remove this event to not have it run twice after running once
            this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.Event_LoadLate;
            bool isLoaded = false;
            try
            {
                // If Init() is false, Run Init()
                if (!this.Init())
                {
                    Log.Error($"{this.ModManifest.Name} couldn't be initialised.");
                }
                else
                {
                    // Assets and definitions
                    ReloadAssets();


                    new ConfigClassParser(this, Config).ParseConfigs();
                    new CommandClassParser(this.Helper.ConsoleCommands, new Command()).ParseCommands();

                    Log.Trace("Testing to see if I see this");
                    SpaceCore.Skills.RegisterSkill(new Archaeology_Skill());

                    // Load the loot table with Item IDs
                    // Read starting recipes from general data file
                    foreach (string entry in ModEntry.ItemDefinitions["extra_loot_table"])
                    {
                        BonusLootTable.Add(entry);
                    }
                    foreach (string entry in ModEntry.ItemDefinitions["artifact_loot_table"])
                    {
                        ArtifactLootTable.Add(entry);
                    }

/// Mod no longer uses Json Assets and has moved to content Patcher
 ///                   if (JALoaded)
 ///                   {
 ///                       JsonAssets = this.Helper.ModRegistry
 ///                           .GetApi<IJsonAssetsApi>
 ///                           ("spacechase0.JsonAssets");
 ///                       if (JsonAssets is null)
 ///                       {
 ///                           Log.Error("Can't access the Json Assets API. Is the mod installed correctly?");
 ///                       }
 ///                       JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.ObjectsPackPath), I18n);
 ///                       JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.FencesPackPath), I18n);
 ///                       JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.BigCraftablesPackPath), I18n);
 ///                   }


                    if (XPDisplayLoaded)
                    {
                        XpAPI = this.Helper.ModRegistry.GetApi<IXPDisplayApi>("Shockah.XPDisplay");
                    }

                    isLoaded = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            if (!isLoaded)
            {
                Log.Error($"{this.ModManifest.Name} failed to load completely. Mod may not be usable.");
            }


        }

        private bool Init()
        {

            // Harmony patches
            try
            {
                HarmonyPatcher.Apply(this,
                    new CheckForBuriedItem_Base_patch(),
                    new CheckForBuriedItem_IslandLocation_patch(),
                    new CheckForBuriedItem_Mineshaft_patch(),
                    new DigUpArtifactSpot_Patcher(),
                    new GetPanItems_Patcher(),
                    new GetPriceAfterMultipliers_patcher(),
                    new VolcanoWarpTotem_patch(),
                    new VolcanoDungeonLevel_patch());


                new Harmony(this.ModManifest.UniqueID).PatchAll();

            }
            catch (Exception e)
            {
                Log.Error($"Error in applying Harmony patches:{Environment.NewLine}{e}");
                return false;
            }

            // Game events
            this.RegisterEvents();

            return true;
        }

        private void RegisterEvents()
        {
            this.Helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            this.Helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
///            this.Helper.Events.Display.MenuChanged += this.Display_MenuChanged;
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (XpAPI is not null)
            {
                //mark the hoe as a tool for archaeology
                XpAPI.RegisterToolSkillMatcher(this.ToolSkillMatchers[0]);
                XpAPI.RegisterToolSkillMatcher(this.ToolSkillMatchers[1]);
            }

/// Mod no Longer uses Production Framework and instead uses content Patcher
///           if (PFWLoaded)
///           {
///               ProducerFramework = this.Helper.ModRegistry
///                   .GetApi<IProducerFrameworkAPI>
///                   ("Digus.ProducerFrameworkMod");
///               if (JsonAssets is null)
///               {
///                   Log.Error("Can't access the Producer Framework API. Is the mod installed correctly?");
///               }
///               ProducerFramework.AddContentPack(directory: Path.Combine(Helper.DirectoryPath, Assets.PFWPackPath));
///           }
        }



        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // On day load, check just to make sure if people are missing recipes or not.
            // Mainly a safeguard with people installing the mod mid-save.
            Utilities.PopulateMissingRecipes();

            if (Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology5a))
            {
                SpawnDiggingSpots(2);
            }
        }

        [EventPriority(EventPriority.Low)]
///       private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
///       {
///
///
///           
///           // Add new recipes on level-up for Excevation skill
///           if (e.NewMenu is SpaceCore.Interface.SkillLevelUpMenu levelUpMenu1)
///           {
///               //Get the current skill being shown
///                string skill = ModEntry.Instance.Helper.Reflection
///                   .GetField<string>(levelUpMenu1, "currentSkill")
///                   .GetValue();
///               //Get the level of the current skill being shown
///               int level = ModEntry.Instance.Helper.Reflection
///                   .GetField<int>(levelUpMenu1, "currentLevel")
///                   .GetValue();
///               //only edit the screen if the current skill is the Archaeology skill
///               // AND
///               // if the levels are not 5 and 10
///               if (skill == "moonslime.Archaeology" &&
///                   (level != 5 || level != 10)) {
///                   Utilities.AddAndDisplayNewRecipesOnLevelUp(levelUpMenu1, level);
///               }
///               return;
///           }
///       }

        public static void ReloadAssets()
        {
            // Reload our own assets
            ModEntry.ItemDefinitions = Assets.ItemDefinitions;
            ModEntry.ArchaeologySkillLevelUpTable = Assets.ArchaeologySkillLevelUpRecipes;
        }

        private static void SpawnDiggingSpots(int spawn)
        {
            List<Tuple<string, Vector2>> locations;
            locations = new List<Tuple<string, Vector2>>();

            int maxspawn = 0;

            foreach (GameLocation loc in Game1.locations)
            {

                if (loc.IsFarm || !loc.IsOutdoors)
                    continue;

                if (maxspawn >= spawn)
                    continue;

                for (int z = 0; z < spawn; z++)
                {

                    int i = Game1.random.Next(loc.Map.DisplayWidth / Game1.tileSize);
                    int j = Game1.random.Next(loc.Map.DisplayHeight / Game1.tileSize);
                    GameLocation gameLocation = loc;
                    Vector2 vector = new Vector2(i, j);
                    if (gameLocation.CanItemBePlacedHere(vector) && !gameLocation.IsTileOccupiedBy(vector) && gameLocation.getTileIndexAt(i, j, "AlwaysFront") == -1 && gameLocation.getTileIndexAt(i, j, "Front") == -1 && !gameLocation.isBehindBush(vector) && (gameLocation.doesTileHaveProperty(i, j, "Diggable", "Back") != null || (gameLocation.GetSeason() == Season.Winter && gameLocation.doesTileHaveProperty(i, j, "Type", "Back") != null && gameLocation.doesTileHaveProperty(i, j, "Type", "Back").Equals("Grass"))))
                    {
                        if (loc.Name.Equals("Forest") && i >= 93 && j <= 22)
                        {
                            continue;
                        }

                        gameLocation.objects.Add(vector, ItemRegistry.Create<Object>("(O)590"));
                    }
                    locations.Add(new Tuple<string, Vector2>(loc.Name, vector));
                    Log.Trace($"Location Name: {loc.Name}, IsFarm: {loc.IsFarm}, IsOutDoors: {loc.IsOutdoors}, X: {vector.X}, Y: {vector.Y}");
                    maxspawn++;
                }
            }
        }

        public void RegisterNewPaths()
        {

        }
    }
}
