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
using StardewValley.Objects;
using static SpaceCore.Skills;
using CookingSkill.Core;
using CookingSkill.Patches;
using System.Linq;
using SpaceCore.Events;
using SObject = StardewValley.Object;
using Api = CookingSkill.Core.Api;

namespace CookingSkill
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool JALoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets");
        internal static bool MargoLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Overhaul");

        internal static IJsonAssetsApi JsonAssets;
        internal static IMargo MargoAPI;


        internal ITranslationHelper I18n => this.Helper.Translation;

        internal static Dictionary<string, List<string>> ItemDefinitions;
        internal static IEnumerable<KeyValuePair<string, List<string>>> CookingSkillLevelUpTable;
        public static readonly IList<int> BonusLootTable = new List<int>();
        public static readonly IList<int> ArtifactLootTable = new List<int>();
        internal const string ObjectPrefix = "moonslime.cooking."; // DO NOT EDIT

        

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

        public override object GetApi()
        {
            return new Api();
        }


        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // delay loading our stuff by just a tiny bit, to make sure other mods load first 
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.Event_LoadLate;
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

                    // Load Json Objects first so the Item Definitions don't error out
                    if (JALoaded)
                    {
                        JsonAssets = this.Helper.ModRegistry
                            .GetApi<IJsonAssetsApi>
                            ("spacechase0.JsonAssets");
                        if (JsonAssets is null)
                        {
                            Log.Error("Can't access the Json Assets API. Is the mod installed correctly?");
                        }
                        JsonAssets.LoadAssets(path: Path.Combine(Helper.DirectoryPath, Assets.ObjectsPackPath), this.I18n);
                    }


                    // Assets and definitions
                    ModEntry.ItemDefinitions = Assets.ItemDefinitions;
                    ModEntry.CookingSkillLevelUpTable = Assets.CookingSkillLevelUpRecipes;


                    new ConfigClassParser(this, Config).ParseConfigs();
                    new CommandClassParser(this.Helper.ConsoleCommands, new Command()).ParseCommands();

                    // Register Cooking skill after checking if MARGO is loaded.
                    SpaceCore.Skills.RegisterSkill(new CookingSkill());




                    if (MargoLoaded)
                    {
                        MargoAPI = this.Helper.ModRegistry.GetApi<IMargo>("DaLion.Overhaul");
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
            Log.Debug("Cooking Init fired");
            // Harmony patches
            try
            {
                HarmonyPatcher.Apply(this,
                    new CraftingPagePatcher(),
                    new CraftingRecipePatcher(),
                    new ObjectPatcher()
                );

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
            SpaceEvents.OnItemEaten += this.OnItemEaten;
        }


        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (MargoAPI is not null && ModEntry.Config.EnablePrestige)
            {
                //if MargoAPI is detcted, Register our skill for Prestige
                string id = SpaceCore.Skills.GetSkill("spacechase0.Cooking").Id;
                MargoAPI.RegisterCustomSkillForPrestige(id);
            }
        }

        
        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            // On day load, check just to make sure if people are missing recipes or not.
            // Mainly a safeguard with people installing the mod mid-save.
            Utilities.PopulateMissingRecipes();
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
                //only edit the screen if the current skill is the cooking skill
                // AND
                // if the levels are not 5 and 10
                if (skill == "spacechase0.Cooking" &&
                    (level != 5 || level != 10)) {
                    Utilities.AddAndDisplayNewRecipesOnLevelUp(levelUpMenu1, level);
                }
                return;
            }
        }

        public static double GetEdibilityMultiplier()
        {
            return 1 + SpaceCore.Skills.GetSkillLevel(Game1.player, "spacechase0.Cooking") * 0.03;
        }

        public static double GetNoConsumeChance()
        {
            //Professional Chef profession
            if (Game1.player.HasCustomProfession(CookingSkill.Cooking10a1))
            {

                if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking10a1))
                {
                    return 0.25;
                }
                else
                {
                    return 0.15;
                }
            }
            return 0;
        }

        // Modifies the item based on professions and stuff
        // Returns for whether or not we should consume the ingredients
        public static bool OnCook(CraftingRecipe recipe, Item item, List<Chest> additionalItems)
        {
            if (recipe.isCookingRecipe && item is SObject obj)
            {
                if (!Game1.player.recipesCooked.TryGetValue(obj.ParentSheetIndex, out int timesCooked))
                    timesCooked = 0;

                Random rand = new Random((int)(Game1.stats.daysPlayed + Game1.uniqueIDForThisGame + (uint)obj.ParentSheetIndex + (uint)timesCooked));

                obj.Edibility = (int)(obj.Edibility * GetEdibilityMultiplier());

                //Sous Chef Profession
                if (Game1.player.HasCustomProfession(CookingSkill.Cooking5a))
                {
                    if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking5a))
                    {
                        obj.Price = (int)(obj.Price * 1.3);
                    }
                    else
                    {
                        obj.Price = (int)(obj.Price * 1.2);
                    }
                }

                //Five Star Cook Profession
                if (Game1.player.HasCustomProfession(CookingSkill.Cooking10a2))
                {
                    if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking10a2))
                    {
                        obj.Quality = SObject.highQuality;
                    }
                    else
                    {
                        obj.Quality = SObject.medQuality;
                    }
                }

                ConsumedItem[] used;
                try
                {
                    CraftingRecipePatcher.ShouldConsumeItems = false;
                    recipe.consumeIngredients(additionalItems);
                    used = CraftingRecipePatcher.LastUsedItems.ToArray();
                }
                finally
                {
                    CraftingRecipePatcher.ShouldConsumeItems = true;
                }

                int total = 0;
                foreach (ConsumedItem ingredient in used)
                    total += ingredient.Amount;

                for (int quality = 1; quality <= SObject.bestQuality; ++quality)
                {
                    if (quality == 3)
                        continue; // not a real quality

                    double chance = 0;
                    foreach (ConsumedItem ingredient in used)
                    {
                        if (ingredient.Item.Quality >= quality)
                            chance += (1.0 / total) * ingredient.Amount;
                    }

                    if (rand.NextDouble() < chance)
                        obj.Quality = quality;
                }

                return rand.NextDouble() >= GetNoConsumeChance();
            }

            return true;
        }

        private Buff LastDrink;

        private void OnItemEaten(object sender, EventArgs e)
        {
            // get object eaten
            if (Game1.player.itemToEat is not SObject { Category: SObject.CookingCategory } obj || !Game1.objectInformation.TryGetValue(obj.ParentSheetIndex, out string rawObjData))
                return;
            string[] objFields = rawObjData.Split('/');
            bool isDrink = objFields.GetOrDefault(SObject.objectInfoMiscIndex) == "drink";

            // get buff data
            Buff oldBuff = isDrink ? Game1.buffsDisplay.drink : Game1.buffsDisplay.food;
            Buff curBuff = this.CreateBuffFromObject(obj, objFields);
            if (oldBuff != null && curBuff != null && oldBuff.buffAttributes.SequenceEqual(curBuff.buffAttributes) && oldBuff != this.LastDrink)
            {
                // Now that we know that this is the original buff, we can buff the buff.
                Log.Trace("Buffing buff");
                int[] newAttr = (int[])curBuff.buffAttributes.Clone();

                //Glutton Profession
                if (Game1.player.HasCustomProfession(CookingSkill.Cooking10b1))
                {
                    if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking10b1))
                    {
                        for (int id = 0; id < newAttr.Length; ++id)
                        {
                            if (newAttr[id] <= 0)
                                continue;

                            if (id is Buff.maxStamina or Buff.magneticRadius)
                                newAttr[id] = (int)(newAttr[id] * 1.3);
                            else
                                newAttr[id] = newAttr[id]+2;
                        }
                    }
                    else
                    {
                        for (int id = 0; id < newAttr.Length; ++id)
                        {
                            if (newAttr[id] <= 0)
                                continue;

                            if (id is Buff.maxStamina or Buff.magneticRadius)
                                newAttr[id] = (int)(newAttr[id] * 1.2);
                            else
                                newAttr[id]++;
                        }
                    }
                }

                float minutesDuration = curBuff.millisecondsDuration / 7000f * 10f;

                //Gourmet Profession
                if (Game1.player.HasCustomProfession(CookingSkill.Cooking5b))
                {
                    if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking5b))
                    {
                        minutesDuration *= 1.35f;
                    }
                    else
                    {
                        minutesDuration *= 1.25f;
                    }
                }

                Buff newBuff = this.CreateBuff(newAttr, (int)minutesDuration, objFields);
                this.ReplaceBuff(newBuff, isDrink);
            } // Picky Eater profession
            else if (curBuff == null && Game1.player.HasCustomProfession(CookingSkill.Cooking10b2))
            {

                if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(CookingSkill.Cooking5b))
                {
                    Log.Trace("Buffing plain");
                    Random rand = new();
                    int[] newAttr = new int[12];
                    int count = 3 + Math.Min(obj.Edibility / 30, 3);
                    for (int i = 0; i < count; ++i)
                    {
                        int attr = rand.Next(10);
                        if (attr >= 3)
                            ++attr; // 3 unused?
                        if (attr >= Buff.speed)
                            ++attr; // unused?

                        int amt = 1;
                        if (attr is Buff.maxStamina or Buff.magneticRadius)
                            amt = 25 + rand.Next(4) * 5;
                        else
                        {
                            // 36% (assuming I used this probability calculator right) chance for a buff to be level 2
                            // 4% chance for it to be 3
                            if (rand.NextDouble() < 0.2)
                                ++amt;
                            if (rand.NextDouble() < 0.2)
                                ++amt;
                        }
                        newAttr[attr] += amt;
                    }

                    int newTime = 120 + obj.Edibility / 10 * 30;

                    Buff newBuff = this.CreateBuff(newAttr, newTime, objFields);
                    this.ReplaceBuff(newBuff, isDrink);
                }
                else
                {
                    Log.Trace("Buffing plain");
                    Random rand = new();
                    int[] newAttr = new int[12];
                    int count = 1 + Math.Min(obj.Edibility / 30, 3);
                    for (int i = 0; i < count; ++i)
                    {
                        int attr = rand.Next(10);
                        if (attr >= 3)
                            ++attr; // 3 unused?
                        if (attr >= Buff.speed)
                            ++attr; // unused?

                        int amt = 1;
                        if (attr is Buff.maxStamina or Buff.magneticRadius)
                            amt = 25 + rand.Next(4) * 5;
                        else
                        {
                            // 36% (assuming I used this probability calculator right) chance for a buff to be level 2
                            // 4% chance for it to be 3
                            if (rand.NextDouble() < 0.2)
                                ++amt;
                            if (rand.NextDouble() < 0.2)
                                ++amt;
                        }
                        newAttr[attr] += amt;
                    }

                    int newTime = 120 + obj.Edibility / 10 * 30;

                    Buff newBuff = this.CreateBuff(newAttr, newTime, objFields);
                    this.ReplaceBuff(newBuff, isDrink);
                }
            }
        }

        /// <summary>Create a buff instance.</summary>
        /// <param name="attr">The buff attributes.</param>
        /// <param name="minutesDuration">The buff duration in minutes.</param>
        /// <param name="objectFields">The raw object fields from <see cref="Game1.objectInformation"/>.</param>
        private Buff CreateBuff(int[] attr, int minutesDuration, string[] objectFields)
        {
            return new(
                farming: attr.GetOrDefault(Buff.farming),
                fishing: attr.GetOrDefault(Buff.fishing),
                mining: attr.GetOrDefault(Buff.mining),
                digging: attr.GetOrDefault(3),
                luck: attr.GetOrDefault(Buff.luck),
                foraging: attr.GetOrDefault(Buff.foraging),
                crafting: attr.GetOrDefault(Buff.crafting),
                maxStamina: attr.GetOrDefault(Buff.maxStamina),
                magneticRadius: attr.GetOrDefault(Buff.magneticRadius),
                speed: attr.GetOrDefault(Buff.speed),
                defense: attr.GetOrDefault(Buff.defense),
                attack: attr.GetOrDefault(Buff.attack),
                minutesDuration: minutesDuration,
                source: objectFields.GetOrDefault(SObject.objectInfoNameIndex),
                displaySource: objectFields.GetOrDefault(SObject.objectInfoDisplayNameIndex)
            );
        }

        /// <summary>Create a buff instance for an object, if valid.</summary>
        /// <param name="obj">The object instance.</param>
        /// <param name="fields">The raw object fields from <see cref="Game1.objectInformation"/>.</param>
        private Buff CreateBuffFromObject(SObject obj, string[] fields)
        {
            // get object info
            int edibility = Convert.ToInt32(fields.GetOrDefault(SObject.objectInfoEdibilityIndex));
            string name = fields.GetOrDefault(SObject.objectInfoNameIndex);
            string displayName = fields.GetOrDefault(SObject.objectInfoDisplayNameIndex);

            // ignore if item doesn't provide a buff
            if (edibility < 0 || fields.Length <= SObject.objectInfoBuffTypesIndex)
                return null;

            // get buff duration
            if (!fields.TryGetIndex(SObject.objectInfoBuffDurationIndex, out string rawDuration) || !int.TryParse(rawDuration, out int minutesDuration))
                minutesDuration = 0;

            // get buff fields
            string[] attr = fields[SObject.objectInfoBuffTypesIndex].Split(' ');
            obj.ModifyItemBuffs(attr);
            if (attr.All(val => val == "0"))
                return null;

            // parse buff
            int GetAttr(int index) => attr.TryGetIndex(index, out string raw) && int.TryParse(raw, out int value) ? value : 0;
            return new Buff(
                farming: GetAttr(Buff.farming),
                fishing: GetAttr(Buff.fishing),
                mining: GetAttr(Buff.mining),
                digging: GetAttr(3),
                luck: GetAttr(Buff.luck),
                foraging: GetAttr(Buff.foraging),
                crafting: GetAttr(Buff.crafting),
                maxStamina: GetAttr(Buff.maxStamina),
                magneticRadius: GetAttr(Buff.magneticRadius),
                speed: GetAttr(Buff.speed),
                defense: GetAttr(Buff.defense),
                attack: GetAttr(Buff.attack),
                minutesDuration: minutesDuration,
                source: name,
                displaySource: displayName
            );
        }

        /// <summary>Replace the current food or drink buff.</summary>
        /// <param name="newBuff">The new buff to set.</param>
        /// <param name="isDrink">Whether the buff is a drink buff; else it's a food buff.</param>
        private void ReplaceBuff(Buff newBuff, bool isDrink)
        {
            if (isDrink)
            {
                Game1.buffsDisplay.drink?.removeBuff();
                Game1.buffsDisplay.drink = newBuff;
                Game1.buffsDisplay.drink.addBuff();
                this.LastDrink = newBuff;
            }
            else
            {
                Game1.buffsDisplay.food?.removeBuff();
                Game1.buffsDisplay.food = newBuff;
                Game1.buffsDisplay.food.addBuff();
            }

            Game1.buffsDisplay.syncIcons();
        }
    }
}
