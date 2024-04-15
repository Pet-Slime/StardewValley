using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace CookingSkill.Core
{

    [HarmonyPatch(typeof(CraftingPage), "clickCraftingRecipe")]
    class HarmonyPatches
    {
        [HarmonyLib.HarmonyPrefix]
        public static bool ClickCraftingRecipe(CraftingPage __instance, ClickableTextureComponent c, bool playSound, ref int ___currentCraftingPage, ref Item ___heldItem, ref bool ___cooking)
        {
            CraftingRecipe craftingRecipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][c];
            Item item = craftingRecipe.createItem();
            List<KeyValuePair<string, int>> list = null;
            if (___cooking && item.Quality == 0)
            {
                list = new List<KeyValuePair<string, int>>();
                list.Add(new KeyValuePair<string, int>("917", 1));
                if (CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, GetContainerContents(__instance._materialContainers)))
                {
                    item.Quality = 2;
                    
                }
                else
                {
                    list = null;
                }


                ///Custom Code to increase the quality of the item
                if (Game1.player.HasCustomProfession(Cooking_Skill.Cooking5a))
                {
                    item.Quality += 1;
                    if (item.Quality == 3)
                        item.Quality += 1;
                }
                ///
            }

            if (___heldItem == null)
            {
                craftingRecipe.consumeIngredients(__instance._materialContainers);
                ___heldItem = item;
                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }
            else
            {
                if (___cooking)
                {
                    /// Make sure the item of food has the same type of edibility value. if they do not, prevent stacking.
                    /// This is to make sure if the player levels up mid cooking, all the items they are crafting doesn't suddenly stack and go up in edibility
                    ///
                    StardewValley.Object stackCheck = (Object)craftingRecipe.createItem();
                    if (item is Object @object && @object.Edibility == stackCheck.Edibility)
                    {
                        stackCheck.Edibility = (int)(stackCheck.Edibility * Utilities.GetLevelValue(Game1.player));
                    }
                    if (!(___heldItem.Name == item.Name) || !___heldItem.getOne().canStackWith(stackCheck) || ___heldItem.Stack + craftingRecipe.numberProducedPerCraft - 1 >= ___heldItem.maximumStackSize())
                    {
                        return false;
                    }
                    ///
                } else
                {
                    if (!(___heldItem.Name == item.Name) || !___heldItem.getOne().canStackWith(item.getOne()) || ___heldItem.Stack + craftingRecipe.numberProducedPerCraft - 1 >= ___heldItem.maximumStackSize())
                    {
                        return false;
                    }

                }



                

                ___heldItem.Stack += craftingRecipe.numberProducedPerCraft;
                craftingRecipe.consumeIngredients(__instance._materialContainers);
                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }

            if (list != null)
            {
                if (playSound)
                {
                    Game1.playSound("breathin");
                }

                CraftingRecipe.ConsumeAdditionalIngredients(list, __instance._materialContainers);
                if (!CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(list, GetContainerContents(__instance._materialContainers)))
                {
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Seasoning_UsedLast"));
                }
            }

            var player = Game1.getFarmer(Game1.player.UniqueMultiplayerID);

            player.checkForQuestComplete(null, -1, -1, item, null, 2);
            if (!___cooking && player.craftingRecipes.ContainsKey(craftingRecipe.name))
            {
                player.craftingRecipes[craftingRecipe.name] += craftingRecipe.numberProducedPerCraft;
            }

            if (___cooking)
            {
                player.cookedRecipe(___heldItem.ItemId);
                Game1.stats.checkForCookingAchievements();

                /// /////////////////////////////
                /// Custom Code
                /// for Cooking
                ___heldItem.modDataForSerialization.TryAdd("moonslime.cooking.Cooked", "1");
                StardewValley.Object itemObj = ___heldItem as StardewValley.Object;
                if (item is Object @object && @object.Edibility == itemObj.Edibility)
                {
                    itemObj.Edibility = (int)(itemObj.Edibility * Utilities.GetLevelValue(Game1.player));
                }
                float exp = ModEntry.Config.ExperienceFromCooking + (itemObj.Edibility * 0.1f);
                Utilities.AddEXP(player, (int)(Math.Floor(exp)));
                if (player.HasCustomProfession(Cooking_Skill.Cooking10a1) && player.couldInventoryAcceptThisItem(___heldItem))
                {
                    ___heldItem.Stack += craftingRecipe.numberProducedPerCraft;
                }

                ////////////////////////////////////

            }
            else
            {
                Game1.stats.checkForCraftingAchievements();
            }

            if (Game1.options.gamepadControls && ___heldItem != null && player.couldInventoryAcceptThisItem(___heldItem))
            {
                player.addItemToInventoryBool(___heldItem);
                ___heldItem = null;
            }
            return false;
        }

        public static IList<Item> GetContainerContents(List<IInventory> _materialContainers)
        {
            if (_materialContainers == null)
            {
                return null;
            }

            List<Item> list = new List<Item>();
            foreach (IInventory materialContainer in _materialContainers)
            {
                list.AddRange(materialContainer);
            }

            return list;
        }
    }


    [HarmonyPatch(typeof(StardewValley.Object), "getPriceAfterMultipliers")]
    class GetPriceAfterMultipliers_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix(
        StardewValley.Object __instance, ref float __result, float startPrice, long specificPlayerID)
        {
            float saleMultiplier = 1f;
            try
            {
                foreach (var farmer in Game1.getAllFarmers())
                {
                    if (Game1.player.useSeparateWallets)
                    {
                        if (specificPlayerID == -1)
                        {
                            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !farmer.isActive())
                            {
                                continue;
                            }
                        }
                        else if (farmer.UniqueMultiplayerID != specificPlayerID)
                        {
                            continue;
                        }
                    }
                    else if (!farmer.isActive())
                    {
                        continue;
                    }
                    if (__instance.modDataForSerialization.ContainsKey("moonslime.cooking.Cooked"))
                    {
                        if (farmer.HasCustomProfession(Cooking_Skill.Cooking10a2))
                        {
                            saleMultiplier += 1f;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BirbCore.Attributes.Log.Error($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            }
            __result *= saleMultiplier;
        }
    }

    [HarmonyPatch(typeof(StardewValley.Item), nameof(Item.canStackWith))]
    class CanStackWith_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(
        StardewValley.Item __instance, ref bool __result, ref ISalable other)
        {
            //Prevent items with different edibility values from stacking. 
            if (__instance is Object @object && other is Object object2 && object2.Edibility != @object.Edibility)
            {
                __result = false;
                return;
            }
        }
    }
}
