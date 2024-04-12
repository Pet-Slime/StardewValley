using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArchaeologySkill.Objects.Water_Shifter;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.APIs;
using Newtonsoft.Json;
using SpaceCore;
using SpaceCore.Interface;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using static BirbCore.Attributes.SMod;
using static SpaceCore.Skills;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ArchaeologySkill.Core
{
    [SEvent]
    internal class Events
    {

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {

            ModEntry.ItemDefinitions = ModEntry.Assets.ItemDefinitions;
            Game1.objectData.TryGetValue("moonslime.Archaeology.water_shifter", out ObjectData value);
            ModEntry.ObjectInfo.Object = value;
            ModEntry.ObjectInfo.Id = "moonslime.Archaeology.water_shifter";

            ArchaeologySkill.Objects.Water_Shifter.Patches.Patch(ModEntry.Instance.Helper);

            var sc = ModEntry.Instance.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");

            Log.Trace("Archaeology: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Archaeology_Skill());

            foreach (string entry in ModEntry.ItemDefinitions["extra_loot_table"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the bonus loot table");
                ModEntry.BonusLootTable.Add(entry);
            }
            foreach (string entry in ModEntry.ItemDefinitions["artifact_loot_table"])
            {
                Log.Trace("Archaeology: Adding " + entry + " to the artifact loot table");
                ModEntry.ArtifactLootTable.Add(entry);
            }


            Log.Trace("Archaeology: Do I see XP display?");
            if (ModEntry.XPDisplayLoaded)
            {
                Log.Trace("Archaeology: I do see XP display, Registering API.");
                ModEntry.XpAPI = ModEntry.Instance.Helper.ModRegistry.GetApi<IXPDisplayApi>("Shockah.XPDisplay");
            }
        }

        [SEvent.DayStarted]
        private void DayStarted(object sender, DayStartedEventArgs e)
        {

            Log.Trace("Archaeology: Does player have Pioneer Profession?");
            if (Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology5a))
            {
                Log.Trace("Archaeology: They do have Pioneer profession, spawn extra artifact spots.");
                SpawnDiggingSpots(2);

            }

            if (ModEntry.Instance.ValidateInventory) //Check if the item's id has changed, so the player doesn't end up with bugged objects
            {
                var items = new List<Item>(Game1.player.Items.Where(x => x is not null));
                foreach (var item in items)
                    if (item.ItemId == "moonslime.Archaeology.water_shifter")
                        Game1.player.Items[Game1.player.Items.IndexOf(item)] = new Object(ModEntry.ObjectInfo.Id, item.Stack, quality: item.Quality);
                ModEntry.Instance.ValidateInventory = false;
            }

            if (!Context.IsMainPlayer)
                return;

            foreach (var l in Game1.locations)
            {
                if (!l.modData.ContainsKey(ModEntry.ModDataKey))
                    continue;

                string json = l.modData[ModEntry.ModDataKey];
                var deserialized = JsonConvert.DeserializeObject<List<WaterShifterSerializable>>(json);

                foreach (var f in deserialized)
                {
                    var waterShifter = new WaterShifter(f.Tile);
                    waterShifter.Owner.Value = f.Owner;
                    if (!string.IsNullOrWhiteSpace(f.Bait))
                        waterShifter.ShifterBait.Value = (Object?)ItemRegistry.Create(f.Bait, 1, f.BaitQuality, true);//new(f.Bait, 1) { Quality = f.BaitQuality };
                    waterShifter.heldObject.Value = Utilities.GetObjectFromSerializable(f);
                    if (!l.Objects.ContainsKey(f.Tile))
                        l.Objects.Add(f.Tile, waterShifter);

                    waterShifter.DayUpdate();

                }

                l.modData.Remove(ModEntry.ModDataKey);
            }
        }


        [SEvent.DayEnding]
        private void DayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            // Water Shifters are (still) removed from every location once the day ends and added to ModData
            // to avoid Crashes / Broken objects
            foreach (var l in Game1.locations)
            {
                if (l.Objects is null || l.Objects.Count() <= 0)
                {
                    if (l.modData.ContainsKey(ModEntry.ModDataKey))
                        l.modData.Remove(ModEntry.ModDataKey);
                    continue;
                }

                var waterShifter = l.Objects.Values.Where(x => x is WaterShifter).Cast<WaterShifter>();
                var serializable = new List<WaterShifterSerializable>();
                foreach (var f in waterShifter)
                {
                    f.DayUpdate();
                    serializable.Add(new(f));
                }

                if (serializable.Count > 0)
                {
                    string json = JsonConvert.SerializeObject(serializable);
                    l.modData[ModEntry.ModDataKey] = json;
                }
                else
                    l.modData.Remove(ModEntry.ModDataKey);

                foreach (var f in waterShifter)
                    l.Objects.Remove(f.TileLocation);
            }
        }

        [SEvent.AssetRequested]
        private void AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ModEntry.ObjectInfo.Object.DisplayName = ModEntry.Instance.I18N.Get("moonslime.Archaeology.water_shifter.name");
                    ModEntry.ObjectInfo.Object.Description = ModEntry.Instance.I18N.Get("moonslime.Archaeology.water_shifter.description");
                    data[ModEntry.ObjectInfo.Id] = ModEntry.ObjectInfo.Object;
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data["moonslime.Archaeology.water_shifter"] = string.Format(ModEntry.ObjectInfo.Recipe, ModEntry.ObjectInfo.Id, ModEntry.Instance.I18N.Get("moonslime.Archaeology.water_shifter.name"));
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("moonslime.Archaeology.water_shifter/waterShifter"))
            {
                e.LoadFromModFile<Texture2D>("assets/water_shifter.png", AssetLoadPriority.Exclusive);
            }
        }

        [SEvent.MenuChanged]
        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is not SkillLevelUpMenu levelUpMenu)
            {
                return;
            }
            


            string skill = ModEntry.Instance.Helper.Reflection.GetField<string>(levelUpMenu, "currentSkill").GetValue();
            if (skill != "moonslime.Archaeology")
            {
                return;
            }

            int level = ModEntry.Instance.Helper.Reflection.GetField<int>(levelUpMenu, "currentLevel").GetValue();

            List<CraftingRecipe> newRecipes = [];

            int menuHeight = 0;
            foreach (KeyValuePair<string, string> recipePair in CraftingRecipe.craftingRecipes)
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(skill) || !conditions.Contains(level.ToString()))
                {
                    continue;
                }

                CraftingRecipe recipe = new(recipePair.Key, isCookingRecipe: false);
                newRecipes.Add(recipe);
                Game1.player.craftingRecipes.TryAdd(recipePair.Key, 0);
                menuHeight += recipe.bigCraftable ? 128 : 64;
            }

            foreach (KeyValuePair<string, string> recipePair in CraftingRecipe.cookingRecipes)
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(skill) || !conditions.Contains(level.ToString()))
                {
                    continue;
                }

                CraftingRecipe recipe = new(recipePair.Key, isCookingRecipe: true);
                newRecipes.Add(recipe);
                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }

                menuHeight += recipe.bigCraftable ? 128 : 64;
            }

            ModEntry.Instance.Helper.Reflection.GetField<List<CraftingRecipe>>(levelUpMenu, "newCraftingRecipes")
                .SetValue(newRecipes);

            levelUpMenu.height = menuHeight + 256 + (levelUpMenu.getExtraInfoForLevel(skill, level).Count * 64 * 3 / 4);
        }



        [SEvent.SaveLoaded]
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (ModEntry.XpAPI is not null)
            {
                Log.Trace("Archaeology: XP display found, Marking Hoe and Pan as Skill tools");
                ModEntry.XpAPI.RegisterToolSkillMatcher(ModEntry.Instance.ToolSkillMatchers[0]);
                ModEntry.XpAPI.RegisterToolSkillMatcher(ModEntry.Instance.ToolSkillMatchers[1]);
            }

            string Id = "moonslime.Archaeology";
            int skillLevel = Game1.player.GetCustomSkillLevel(Id);
            if (skillLevel == 0)
            {
                return;
            }

            if (skillLevel >= 5 && !(Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology5a) ||
                                     Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology5b)))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(Id, 5));
            }

            if (skillLevel >= 10 && !(Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10a1) ||
                                      Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10a2) ||
                                      Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10b1) ||
                                      Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10b2)))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(Id, 10));
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                Game1.player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CookingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }
            }
        
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
    }
}
