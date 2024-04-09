using System;
using System.Collections.Generic;
using MoonShared;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Buffs;

namespace ArchaeologySkill
{
    internal class Utilities
    {
        internal static void PopulateMissingRecipes()
        {

            // Add any missing starting recipes
            foreach (string recipe in Archaeology_Skill.StartingRecipes)
            {
                if (!Game1.player.craftingRecipes.ContainsKey(recipe))
                {
                    Log.Trace($"Added missing starting recipe {recipe}");
                    Game1.player.craftingRecipes.Add(recipe, 0);
                }
            }
        
            // Add any missing recipes from the level-up recipe table
            int level = GetLevel();
            IReadOnlyDictionary<int, IList<string>> recipes = (IReadOnlyDictionary<int, IList<string>>)Archaeology_Skill.ArchaeologySkillLevelUpRecipes;
            IEnumerable<string> missingRecipes = recipes
                // Take all recipe lists up to the current level
                .TakeWhile(pair => pair.Key < level)
                .SelectMany(pair => pair.Value) // Flatten recipe lists into their recipes
                .Select(r => r) // Add item prefixes
                .Where(r => !Game1.player.craftingRecipes.ContainsKey(r)); // Take recipes not known by the player
            foreach (string recipe in missingRecipes)
            {
                Log.Trace($"Added missing recipe {recipe}");
                Game1.player.craftingRecipes.Add(recipe, 0);
            }
        
        }

///        public static void AddAndDisplayNewRecipesOnLevelUp(SpaceCore.Interface.SkillLevelUpMenu menu, int level)
///        {
///            List<CraftingRecipe> excevationRecipes =
///                GetArchaeologyRecipesForLevel(level)
///                .ToList()
///                .ConvertAll(name => new CraftingRecipe(name))
///                .Where(recipe => !Game1.player.knowsRecipe(recipe.name))
///                .ToList();
///            if (excevationRecipes is not null && excevationRecipes.Count > 0)
///            {
///                foreach (CraftingRecipe recipe in excevationRecipes.Where(r => !Game1.player.craftingRecipes.ContainsKey(r.name)))
///                {
///                    Game1.player.craftingRecipes[recipe.name] = 0;
///                }
///            }
///
///            // Add crafting recipes
///            var craftingRecipes = new List<CraftingRecipe>();
///            // No new crafting recipes currently.
///
///            // Apply new recipes
///            List<CraftingRecipe> combinedRecipes = craftingRecipes
///                .Concat(excevationRecipes)
///                .ToList();
///            ModEntry.Instance.Helper.Reflection
///                .GetField<List<CraftingRecipe>>(menu, "newCraftingRecipes")
///                .SetValue(combinedRecipes);
///            Log.Trace(combinedRecipes.Aggregate($"New recipes for level {level}:", (total, cur) => $"{total}{Environment.NewLine}{cur.name} ({cur.createItem().ParentSheetIndex})"));
///
///            // Adjust menu to fit if necessary
///            const int defaultMenuHeightInRecipes = 4;
///            int menuHeightInRecipes = combinedRecipes.Count + combinedRecipes.Count(recipe => recipe.bigCraftable);
///            if (menuHeightInRecipes >= defaultMenuHeightInRecipes)
///            {
///                menu.height += (menuHeightInRecipes - defaultMenuHeightInRecipes) * StardewValley.Object.spriteSheetTileSize * Game1.pixelZoom;
///            }
///        }

        //For the goldrush profession
        public static bool ApplySpeedBoost(Farmer who)
        {
            //Get the player
            var player = Game1.player;
            //check to see the player who is doing the request is the same one as this player. 
            if (who != player)
                return false;

            //Check to see if the player has the Archaeology skill. If not, return false.
            if (!player.HasCustomProfession(Archaeology_Skill.Archaeology10b2))
                return false;

            Buff buff = new Buff(
                id: "Archaeology:profession:haste",
                displayName: "Gold Rush", // can optionally specify description text too
                iconTexture: ModEntry.Assets.Gold_Rush_Buff,
                iconSheetIndex: 0,
                duration: 30_000, // 30 seconds
                effects: new BuffEffects()
                {
                    Speed = { 10 } // shortcut for buff.Speed.Value = 10
                }
            );
            //Check to see if the player already has the haste buff. if so, don't refresh it and return false.
            if (Game1.player.hasBuff(buff.id))
                return false;


            //Apply the buff make sure we have it have a custon name.
            Game1.player.applyBuff(buff);

            //get the player's tile positon as a vector2
            Vector2 tile = new(
                x: (int)(player.position.X / Game1.tileSize),
                y: (int)(player.position.Y / Game1.tileSize)
            );

            //Play a sound to give feedback that this profession is working
            player.currentLocation.localSound("debuffHit", tile);
            return true;
        }

        /// <summary>
        /// Apply the basic Archaeology skill items. Give who the exp, check to see if they have the gold rush profession, and spawn bonus loot
        /// </summary>
        /// <param name="who"> The player</param>
        /// <param name="bonusLoot"> Do they get bonus loot or not</param>
        /// <param name="Object"> the int that the bonus loot should be</param>
        /// <param name="xLocation">the player's x location</param>
        /// <param name="yLocation">the player's y location</param>
        public static void ApplyArchaeologySkill(Farmer who, bool bonusLoot = false, string Object = "(O)874", int xLocation = 0, int yLocation = 0)
        {
            AddEXP(who, ModEntry.Config.ExperienceFromBuriedAndPannedItem);
            Utilities.ApplySpeedBoost(who);
            if (bonusLoot)
            {
                Game1.createObjectDebris(Object, xLocation, yLocation);
            }
        }

        public static void AddEXP(StardewValley.Farmer who, int amount)
        {
            SpaceCore.Skills.AddExperience(who, "moonslime.Archaeology", amount);
        }

        public static int GetLevel()
        {
            return SpaceCore.Skills.GetSkillLevel(Game1.player, "moonslime.Archaeology");
        }

        /// <returns>New recipes learned when reaching this level.</returns>
        public static IReadOnlyList<string> GetArchaeologyRecipesForLevel(int level)
        {
            // Level undefined
            if (!Archaeology_Skill.ArchaeologySkillLevelUpRecipes.ContainsKey(level))
            {
                return new List<string>();
            }
            // Level used for professions, no new recipes added
            if (level % 5 == 0)
            {
                return new List<string>();
            }
            return (IReadOnlyList<string>)Archaeology_Skill.ArchaeologySkillLevelUpRecipes[level];
        }
    }
}
