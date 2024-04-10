using System;
using System.Collections.Generic;
using BirbCore.Attributes;
using MoonShared.APIs;
using SpaceCore;
using SpaceCore.Interface;
using SpaceShared.APIs;
using StardewModdingAPI.Events;
using StardewValley;
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
