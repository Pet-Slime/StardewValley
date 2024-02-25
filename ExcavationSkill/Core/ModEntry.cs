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
using ExcavationSkill.Patches;
using StardewValley.Tools;
using ExcavationSkill.Objects;
using SpaceShared.APIs;
using HarmonyLib;

namespace ExcavationSkill
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool JALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool PFWLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Digus.ProducerFrameworkMod");
        internal static bool DGALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");
        internal static bool MargoLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Overhaul");
        internal static bool XPDisplayLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Shockah.XPDisplay");
        internal static bool ItemSpawnerLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("CJBok.ItemSpawner");

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        private readonly List<Func<Item, (int? SkillIndex, string? SpaceCoreSkillName)?>> ToolSkillMatchers = new()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            o => o is Hoe ? (null, "moonslime.Excavation") : null,
            o => o is Pan ? (null, "moonslime.Excavation") : null
        };

        internal static IJsonAssetsApi JsonAssets;
        internal static IProducerFrameworkAPI ProducerFramework;
        internal static IDynamicGameAssetsApi DGAAPI;
        internal static IMargo MargoAPI;
        internal static IXPDisplayApi XpAPI;


        internal ITranslationHelper I18n => this.Helper.Translation;
        internal static Dictionary<string, List<string>> ItemDefinitions;
        internal static IEnumerable<KeyValuePair<string, List<string>>> ExcavationSkillLevelUpTable;
        public static readonly IList<int> BonusLootTable = new List<int>();
        public static readonly IList<int> ArtifactLootTable = new List<int>();
        public static readonly IList<int> ShifterLootTable = new List<int>();

        
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
            sc.RegisterSerializerType(typeof(ShifterObject));
            sc.RegisterSerializerType(typeof(PathsObject));
            sc.RegisterSerializerType(typeof(PathsTerrain));


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

                    // Register excavation skill after checking if MARGO is loaded.
                    Log.Trace("Testing to see if I see this");
                    SpaceCore.Skills.RegisterSkill(new Excavation_Skill());

                    // Load the loot table with Item IDs
                    // Read starting recipes from general data file
                    foreach (string entry in ModEntry.ItemDefinitions["extra_loot_table"])
                    {
                        BonusLootTable.Add(int.Parse(entry));
                    }
                    foreach (string entry in ModEntry.ItemDefinitions["artifact_loot_table"])
                    {
                        ArtifactLootTable.Add(int.Parse(entry));
                    }
                    foreach (string entry in ModEntry.ItemDefinitions["shifter_loot_table"])
                    {
                        ShifterLootTable.Add(int.Parse(entry));
                    }


                    if (JALoaded)
                    {
                        JsonAssets = this.Helper.ModRegistry
                            .GetApi<IJsonAssetsApi>
                            ("spacechase0.JsonAssets");
                        if (JsonAssets is null)
                        {
                            Log.Error("Can't access the Json Assets API. Is the mod installed correctly?");
                        }
                        JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.ObjectsPackPath), I18n);
                        JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.FencesPackPath), I18n);
                        JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.BigCraftablesPackPath), I18n);
                    }

                    if (DGALoaded)
                   {
                        DGAAPI = this.Helper.ModRegistry
                           .GetApi<IDynamicGameAssetsApi>
                           ("spacechase0.DynamicGameAssets");
                        try
                        {
                            ///DGAAPI.AddEmbeddedPack(this.ModManifest, Path.Combine(Helper.DirectoryPath, Assets.DGAPackPath));
                        }
                        catch {

                            Log.Error("Can't access the Dynamic Game Assets API. Is the mod installed correctly?");
                        }

                   }

                    if (MargoLoaded)
                    {
                        MargoAPI = this.Helper.ModRegistry.GetApi<IMargo>("DaLion.Overhaul");
                        if (MargoAPI is null)
                        {
                            Log.Error("Can't access the MARGO API. Is the mod installed correctly?");
                        }
                    }
                    if (XPDisplayLoaded)
                    {
                        XpAPI = this.Helper.ModRegistry.GetApi<IXPDisplayApi>("Shockah.XPDisplay");
                        if (MargoAPI is null)
                        {
                            Log.Error("Can't access the MARGO API. Is the mod installed correctly?");
                        }
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
                    new VolcanoDungeonLevel_patch(),
                    new WaterStrainerInception_patch());


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
            this.Helper.Events.Display.MenuChanged += this.Display_MenuChanged;
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (MargoAPI is not null && ModEntry.Config.EnablePrestige)
            {
                //if MargoAPI is detcted, Register our skill for Prestige
                string id = SpaceCore.Skills.GetSkill("moonslime.Excavation").Id;
                MargoAPI.RegisterCustomSkillForPrestige(id);
            }
            if (XpAPI is not null)
            {
                //mark the hoe as a tool for archaeology
                XpAPI.RegisterToolSkillMatcher(this.ToolSkillMatchers[0]);
                XpAPI.RegisterToolSkillMatcher(this.ToolSkillMatchers[1]);
            }
            if (PFWLoaded)
            {
                ProducerFramework = this.Helper.ModRegistry
                    .GetApi<IProducerFrameworkAPI>
                    ("Digus.ProducerFrameworkMod");
                if (JsonAssets is null)
                {
                    Log.Error("Can't access the Producer Framework API. Is the mod installed correctly?");
                }
                ProducerFramework.AddContentPack(directory: Path.Combine(Helper.DirectoryPath, Assets.PFWPackPath));
            }


        }



        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // On day load, check just to make sure if people are missing recipes or not.
            // Mainly a safeguard with people installing the mod mid-save.
            Utilities.PopulateMissingRecipes();

            if (Game1.player.HasCustomProfession(Excavation_Skill.Excavation5a))
            {
                if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(Excavation_Skill.Excavation5a))
                {
                    Log.Trace("Excavation skill: Pioneer Priestige extra artifact spots fired");
                    SpawnDiggingSpots(4);
                }
                else
                {
                    Log.Trace("Excavation skill: Pioneer extra artifact spots fired");
                    SpawnDiggingSpots(2);
                }
            }
        }

        [EventPriority(EventPriority.Low)]
        private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
        {


            
            // Add new recipes on level-up for Excevation skill
            if (e.NewMenu is SpaceCore.Interface.SkillLevelUpMenu levelUpMenu1)
            {
                //Get the current skill being shown
                 string skill = ModEntry.Instance.Helper.Reflection
                    .GetField<string>(levelUpMenu1, "currentSkill")
                    .GetValue();
                //Get the level of the current skill being shown
                int level = ModEntry.Instance.Helper.Reflection
                    .GetField<int>(levelUpMenu1, "currentLevel")
                    .GetValue();
                //only edit the screen if the current skill is the excavation skill
                // AND
                // if the levels are not 5 and 10
                if (skill == "moonslime.Excavation" &&
                    (level != 5 || level != 10)) {
                    Utilities.AddAndDisplayNewRecipesOnLevelUp(levelUpMenu1, level);
                }
                return;
            }
        }

        public static void ReloadAssets()
        {
            // Reload our own assets
            ModEntry.ItemDefinitions = Assets.ItemDefinitions;
            ModEntry.ExcavationSkillLevelUpTable = Assets.ExcavationSkillLevelUpRecipes;
        }

        private void SpawnDiggingSpots(int spawn)
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

                for (int i = 0; i < spawn; i++)
                {

                    int randomWidth = Game1.random.Next(loc.Map.DisplayWidth / Game1.tileSize);
                    int randomHeight = Game1.random.Next(loc.Map.DisplayHeight / Game1.tileSize);
                    Vector2 newLoc = new Vector2(randomWidth, randomHeight);
                    if (!loc.isTileLocationTotallyClearAndPlaceable(newLoc) ||
                        loc.getTileIndexAt(randomWidth, randomHeight, "AlwaysFront") != -1 ||
                        (loc.getTileIndexAt(randomWidth, randomHeight, "Front") != -1 ||
                         loc.isBehindBush(newLoc)) ||
                        (loc.doesTileHaveProperty(randomWidth, randomHeight, "Diggable", "Back") == null &&
                         (!Game1.currentSeason.Equals("winter") ||
                          loc.doesTileHaveProperty(randomWidth, randomHeight, "Type", "Back") == null ||
                          !loc.doesTileHaveProperty(randomWidth, randomHeight, "Type", "Back").Equals("Grass"))) ||
                        (loc.Name.Equals("Forest") && randomWidth >= 93 && randomHeight <= 22)) continue;
                    loc.objects.Add(newLoc, new StardewValley.Object(newLoc, 590, 1));
                    locations.Add(new Tuple<string, Vector2>(loc.Name, newLoc));
                    Log.Trace($"Location Name: {loc.Name}, IsFarm: {loc.IsFarm}, IsOutDoors: {loc.IsOutdoors}, X: {newLoc.X}, Y: {newLoc.Y}");
                    maxspawn++;
                }
            }
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(who, "moonslime.Excavation", amount);
        }

        public void RegisterNewPaths()
        {

        }
    }
}
