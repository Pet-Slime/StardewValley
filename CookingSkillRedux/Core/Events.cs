using System;
using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using MoonShared.APIs;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using SpaceCore.Interface;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Objects;

namespace CookingSkill.Core
{
    [SEvent]
    public class Events
    {
        public static Skills.SkillBuff Test { get; private set; }

        [SEvent.GameLaunchedLate]
        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Cooking: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Cooking_Skill());

            try
            {
                Log.Trace("Cooking: Do I see better crafting?");
                if (ModEntry.BCLoaded)
                {
                    Log.Trace("Cooking: I do see better crafting, Registering API.");
                    ModEntry.BetterCrafting = ModEntry.Instance.Helper.ModRegistry.GetApi<IBetterCrafting>("leclair.bettercrafting");

                    ModEntry.BetterCrafting.PerformCraft += BetterCraftingPerformCraftEvent;
                    ModEntry.BetterCrafting.PostCraft += BetterCraftingPostCraftEvent;
                }
            }
            catch
            {
                Log.Trace("Cooking: Error with trying to load better crafting API");
            }
            SpaceEvents.OnItemEaten += OnItemEat;
            SpaceEvents.AfterGiftGiven += AfterGiftGiven;
        }

        private static void AfterGiftGiven(object sender, EventArgsGiftGiven e)
        {
            if (e.Gift.modDataForSerialization.ContainsKey("moonslime.Cooking.homemade") && sender is StardewValley.Farmer farmer)
            {
                int bonusFriendship = ((int)Math.Ceiling(e.Gift.Edibility * 0.10));
                farmer.changeFriendship(bonusFriendship, e.Npc);
            }
        }

        private static void BetterCraftingPerformCraftEvent(IGlobalPerformCraftEvent @event)
        {
            @event.Item = PreCook(@event.Recipe.CraftingRecipe, @event.Item);
            @event.Complete();
        }

        private static void BetterCraftingPostCraftEvent(IPostCraftEvent @event)
        {
            @event.Item = PostCook(@event.Recipe.CraftingRecipe, @event.Item, @event.Player);
        }

        [SEvent.MenuChanged]
        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is not SkillLevelUpMenu levelUpMenu)
            {
                return;
            }



            string skill = ModEntry.Instance.Helper.Reflection.GetField<string>(levelUpMenu, "currentSkill").GetValue();
            if (skill != "moonslime.Cooking")
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
            string Id = "moonslime.Cooking";
            int skillLevel = Game1.player.GetCustomSkillLevel(Id);
            if (skillLevel == 0)
            {
                return;
            }

            if (skillLevel >= 5 && !(Game1.player.HasCustomProfession(Cooking_Skill.Cooking5a) ||
                                     Game1.player.HasCustomProfession(Cooking_Skill.Cooking5b)))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(Id, 5));
            }

            if (skillLevel >= 10 && !(Game1.player.HasCustomProfession(Cooking_Skill.Cooking10a1) ||
                                      Game1.player.HasCustomProfession(Cooking_Skill.Cooking10a2) ||
                                      Game1.player.HasCustomProfession(Cooking_Skill.Cooking10b1) ||
                                      Game1.player.HasCustomProfession(Cooking_Skill.Cooking10b2)))
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

        public static void OnItemEat(object sender, EventArgs e)
        {
            // get the farmer. If there is no farmer (like maybe they disconnected, make sure it isnt null)
            StardewValley.Farmer who = sender as StardewValley.Farmer;
            if (who == null) return;

            //Get the farmer's unique ID, and again check for an ull player (cause I am parinoid)
            // If they don't have any professions related to food buffs, return so the rest of the code does not get ran.
            var player = Game1.getFarmer(who.UniqueMultiplayerID);
            if (player == null || !player.HasCustomProfession(Cooking_Skill.Cooking5b)) return;

            //Get the food item the player is going to eat. Make sure it doesnt return as a null item.
            StardewValley.Object food = player.itemToEat as StardewValley.Object;
            if (food == null) return;

            //Get the food's ObjectData and make sure it isn't null
            if (!Game1.objectData.TryGetValue(food.ItemId, out ObjectData data) || data == null) return;

            //For each buff in the object data, let's go through it.
            foreach (var buffData in data.Buffs)
            {
                //If there are any spaceCore buffs on the food, run this code to increase those buffs. If there are not, run the other code.
                if (buffData.CustomFields != null && buffData.CustomFields.Any(b => b.Key.StartsWith("spacechase.SpaceCore.SkillBuff.")))
                {
                    Buff matchingBuff = null;
                    string id = string.IsNullOrWhiteSpace(buffData.BuffId) ? (data.IsDrink ? "drink" : "food") : buffData.BuffId;
                    foreach (Buff buff in food.GetFoodOrDrinkBuffs())
                    {
                        matchingBuff = buff;
                    }
                    if (matchingBuff != null)
                    {
                        var newSkillBuff = new Skills.SkillBuff(matchingBuff, id, buffData.CustomFields);
                        if (player.hasBuff(newSkillBuff.id))
                        {
                            player.buffs.Remove(newSkillBuff.id);
                            newSkillBuff.millisecondsDuration = (int)(Utilities.GetLevelValue(player) * newSkillBuff.millisecondsDuration);
                            if (player.HasCustomProfession(Cooking_Skill.Cooking10b1))
                            {
                                ApplyAttributeBuff(newSkillBuff.effects, 1f);
                                newSkillBuff.SkillLevelIncreases = newSkillBuff.SkillLevelIncreases.ToDictionary(kv => kv.Key, kv => kv.Value + 1);
                            }
                            player.buffs.Apply(newSkillBuff);
                        }
                    }
                }
                else
                {
                    foreach (Buff buff in food.GetFoodOrDrinkBuffs())
                    {
                        if (player.hasBuff(buff.id))
                        {
                            player.buffs.Remove(buff.id);
                            buff.millisecondsDuration = (int)(Utilities.GetLevelValue(player) * buff.millisecondsDuration);
                            if (player.HasCustomProfession(Cooking_Skill.Cooking10b1))
                            {
                                ApplyAttributeBuff(buff.effects, 1f);
                            }
                            player.buffs.Apply(buff);
                        }
                    }
                }
            }

            // If the player has the right profession, give them an extra buff
            if (player.HasCustomProfession(Cooking_Skill.Cooking10b2))
            {
                // Define constants for attribute types and max values
                const int NumAttributes = 10;
                const int MaxAttributeValue = 5;
                const int MaxStaminaMultiplier = 16;
                const int BuffDurationMultiplier = (6000 * 10);

                // Generate random attribute and level
                int attributeBuff = Game1.random.Next(1, NumAttributes + 1);
                Log.Trace("Cooking: random attibute is: " + attributeBuff.ToString());
                int attributeLevel = Game1.random.Next(1, MaxAttributeValue + 1);
                Log.Trace("Cooking: random level is: " + attributeLevel.ToString());

                // Create a BuffEffects instance
                BuffEffects randomEffect = new()
                {
                    FarmingLevel = { 0 },
                    FishingLevel = { 0 },
                    MiningLevel = { 0 },
                    LuckLevel = { 0 },
                    ForagingLevel = { 0 },
                    MaxStamina = { 0 },
                    MagneticRadius = { 0 },
                    Defense = { 0 },
                    Attack = { 0 },
                    Speed = { 0 }
                };


                // Apply the random effect based on the randomly generated attribute
                switch (attributeBuff)
                {
                    case 1: randomEffect.FarmingLevel.Value = attributeLevel; break;
                    case 2: randomEffect.FishingLevel.Value = attributeLevel; break;
                    case 3: randomEffect.MiningLevel.Value = attributeLevel; break;
                    case 4: randomEffect.LuckLevel.Value = attributeLevel; break;
                    case 5: randomEffect.ForagingLevel.Value = attributeLevel; break;
                    case 6: randomEffect.MaxStamina.Value = attributeLevel * MaxStaminaMultiplier; break;
                    case 7: randomEffect.MagneticRadius.Value = attributeLevel * MaxStaminaMultiplier; break;
                    case 8: randomEffect.Defense.Value = attributeLevel; break;
                    case 9: randomEffect.Attack.Value = attributeLevel; break;
                    case 10: randomEffect.Speed.Value = attributeLevel; break;
                }

                // Create the buff
                Buff buff = new(
                    id: "Cooking:profession:random_buff",
                    displayName: ModEntry.Instance.I18n.Get("moonslime.Cooking.Profession10b2.buff"),
                    description: null,
                    iconTexture: ModEntry.Assets.Random_Buff,
                    iconSheetIndex: 0,
                    duration: BuffDurationMultiplier * Utilities.GetLevel(player), //Buff duration based on player Cooking level, to reward them for eating cooking foods
                    effects: randomEffect
                );
                //Remove the old buff
                player.buffs.Remove(buff.id);
                //Apply the new buff
                player.applyBuff(buff);
            }

        }

        private static void ApplyAttributeBuff(BuffEffects effects, float value)
        {
            // Define an array of all attributes with their base modifier and a multiplier
            var attributes = new (NetFloat attribute, float multiplier)[]
            {
                (effects.FarmingLevel, 1f),
                (effects.FishingLevel, 1f),
                (effects.MiningLevel, 1f),
                (effects.LuckLevel, 1f),
                (effects.ForagingLevel, 1f),
                (effects.Speed, 1f),
                (effects.Defense, 1f),
                (effects.Attack, 1f),
                (effects.CombatLevel, 1f),
                (effects.Immunity, 1f),
                (effects.MaxStamina, 16f), // Special multiplier for MaxStamina
                (effects.MagneticRadius, 16f) // Special multiplier for MagneticRadius
            };

            // Apply the value modification to each attribute
            foreach (var (attribute, multiplier) in attributes)
            {
                if (attribute.Value != 0f)
                {
                    attribute.Value += value * multiplier;
                }
            }
        }

        public static Item PreCook(CraftingRecipe recipe, Item item)
        {
            //Make sure the recipe is not null
            //Check to see if the recipe is a cooking recipe
            //Make sure the item coming out of the cooking recipe is an object
            if (recipe is not null && recipe.isCookingRecipe && item is StardewValley.Object obj)
            {

                float levelValue = Utilities.GetLevelValue(Game1.player);

                //increase the edibility of the object based on the cooking level of the player
                obj.Edibility = (int)(obj.Edibility * levelValue);

                //If the player has the right profession, increase the selling price
                if (Game1.player.HasCustomProfession(Cooking_Skill.Cooking10a2))
                {
                    obj.Price *= ((int)(2 * levelValue));
                }

                //If the player has right profession, increase item quality
                if (Game1.player.HasCustomProfession(Cooking_Skill.Cooking5a))
                {
                    obj.Quality += 1;
                    // make sure quality is equal to 4 and not 3 if the player has Qi Seasoning
                    if (obj.Quality == 3)
                        obj.Quality += 1;
                }
                //Return the object
                return item;
            }
            //Return the object
            return item;
        }

        public static Item PostCook(CraftingRecipe recipe, Item heldItem, Farmer who)
        {
            //Make sure the recipe is not null
            //Check to see if the recipe is a cooking recipe
            //Make sure the item coming out of the cooking recipe is an object
            if (recipe is not null && recipe.isCookingRecipe && heldItem is StardewValley.Object obj)
            {
                //Get the exp value, based off the general exp you get from cooking (Default:2)
                float exp = ModEntry.Config.ExperienceFromCooking;
                //Get the bonus exp value based off the object's edbility. (default:50% of the object's edbility)
                float bonusExp = (obj.Edibility * ModEntry.Config.ExperienceFromEdibility);

                //Find out how many times they have cooked said recipe
                who.recipesCooked.TryGetValue(heldItem.ItemId, out int value);
                if (value <= ModEntry.Config.BonusExpLimit)
                {
                    //Then add it to the bonus value gained from the objects edibility (Default: 10% of the items edibility given as bonus exp)
                    exp += bonusExp;
                } else
                {
                    //Else, give a diminishing return on the bonus exp
                    float min = Math.Max(1, value - ModEntry.Config.BonusExpLimit);
                    exp += (bonusExp / min);
                }

                //Send a message to the player at the limit for the bonus exp
                if (value == ModEntry.Config.BonusExpLimit - 1)
                {
                    Game1.showGlobalMessage(ModEntry.Instance.I18n.Get("moonslime.Cooking.no_more_bonus_exp"));
                }

                //Give the player exp. Make sure to floor the value. Don't want no decimels.
                Utilities.AddEXP(who, (int)(Math.Floor(exp)));

                //Add the homecooked value to the modData for the item. So we can check for it later
                obj.modDataForSerialization.TryAdd("moonslime.Cooking.homemade", "yes");


                //If the player has the right profession, they get an extra number of crafts from crafting the item.
                if (who.HasCustomProfession(Cooking_Skill.Cooking10a1) && who.couldInventoryAcceptThisItem(heldItem))
                {
                    if (Game1.random.NextDouble() < (Utilities.GetLevelValue(who) + Utilities.GetLevelValue(who)))
                    {
                        heldItem.Stack += recipe.numberProducedPerCraft;
                    }
                    //Return the object
                    return heldItem;
                }
            }
            //Return the object
            return heldItem;
        }
    }
}
