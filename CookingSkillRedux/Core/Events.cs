using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BirbCore.Attributes;
using Force.DeepCloner;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using SpaceCore.Interface;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.Objects;
using static BirbCore.Attributes.SEvent;
using static BirbCore.Attributes.SMod;
using static SpaceCore.Skills;

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


            SpaceEvents.OnItemEaten += OnItemEat;
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
            StardewValley.Farmer who = sender as StardewValley.Farmer;
            if (who == null) return;

            var player = Game1.getFarmer(who.UniqueMultiplayerID);
            if (player == null || !player.HasCustomProfession(Cooking_Skill.Cooking5b)) return;

            StardewValley.Object food = player.itemToEat as StardewValley.Object;
            if (food == null) return;

            if (!Game1.objectData.TryGetValue(food.ItemId, out ObjectData data) || data == null) return;

            foreach (var buffData in data.Buffs)
            {
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

                // Create and apply the buff
                Buff buff = new(
                    id: "Cooking:profession:random_buff",
                    displayName: ModEntry.Instance.I18n.Get("moonslime.Cooking.Profession10b2.buff"),
                    description: null,
                    iconTexture: ModEntry.Assets.Random_Buff,
                    iconSheetIndex: 0,
                    duration: BuffDurationMultiplier * Utilities.GetLevel(player),
                    effects: randomEffect
                );
                player.buffs.Remove(buff.id);
                player.applyBuff(buff);
            }

        }

        private static void ApplyAttributeBuff(BuffEffects effects, float value)
        {
            var effectArrays = new[] { new[] { effects.FarmingLevel, effects.FishingLevel, effects.MiningLevel, effects.LuckLevel, effects.ForagingLevel, effects.Speed, effects.Defense, effects.Attack }, new[] { effects.MaxStamina, effects.MagneticRadius } };

            foreach (var effectArray in effectArrays)
            {
                foreach (var netFloat in effectArray)
                {
                    if (netFloat.Value != 0f)
                    {
                        netFloat.Value += value * (effectArray == effectArrays[1] ? 1.2f : 1f);
                    }
                }
            }
        }


        public static Item PreCook(CraftingRecipe recipe, Item item)
        {
            if (recipe.isCookingRecipe && item is StardewValley.Object obj)
            {

                obj.Edibility = (int)(obj.Edibility * Utilities.GetLevelValue(Game1.player));

                if (Game1.player.HasCustomProfession(Cooking_Skill.Cooking10a2))
                    obj.Price = obj.Price * 2;

                if (Game1.player.HasCustomProfession(Cooking_Skill.Cooking5a))
                {
                    obj.Quality += 1;

                    if (obj.Quality == 3)
                        obj.Quality += 1;
                }
                return item;
            }
            return item;
        }

        public static Item PostCook(CraftingRecipe recipe, Item heldItem, Farmer who)
        {
            if (recipe.isCookingRecipe && heldItem is StardewValley.Object obj)
            {

                StardewValley.Object itemObj = heldItem as StardewValley.Object;
                float exp = ModEntry.Config.ExperienceFromCooking + (itemObj.Edibility * ModEntry.Config.ExperienceFromEdibility);
                Utilities.AddEXP(who, (int)(Math.Floor(exp)));
                if (who.HasCustomProfession(Cooking_Skill.Cooking10a1) && who.couldInventoryAcceptThisItem(heldItem))
                {
                    heldItem.Stack += recipe.numberProducedPerCraft;
                    return heldItem;
                }
            }

            return heldItem;
        }
    }
}
