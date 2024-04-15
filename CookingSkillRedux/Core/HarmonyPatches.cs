using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
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
        [HarmonyLib.HarmonyPostfix]
        public static void ClickCraftingRecipe(CraftingPage __instance, ClickableTextureComponent c, bool playSound, ref int ___currentCraftingPage, ref Item ___heldItem, ref bool ___cooking)
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
                if (!(___heldItem.Name == item.Name) || !___heldItem.getOne().canStackWith(item.getOne()) || ___heldItem.Stack + craftingRecipe.numberProducedPerCraft - 1 >= ___heldItem.maximumStackSize())
                {
                    return;
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
                /// Custom Code
                Utilities.AddEXP(player, 2);
                ///
                Game1.stats.checkForCookingAchievements();
            }
            else
            {
                Game1.stats.checkForCraftingAchievements();
            }

            if (Game1.options.gamepadControls && ___heldItem != null && player.couldInventoryAcceptThisItem(___heldItem))
            {
                if (___cooking && player.HasCustomProfession(Cooking_Skill.Cooking5a))
                {
                    if (___heldItem.Quality != 4)
                    {
                        ___heldItem.Quality += 1;
                    }
                    player.addItemToInventoryBool(___heldItem);

                    if ( player.HasCustomProfession(Cooking_Skill.Cooking10a1) && player.couldInventoryAcceptThisItem(___heldItem))
                    {
                        player.addItemToInventoryBool(___heldItem);
                    }
                } else
                {
                    //Give the player the item
                    player.addItemToInventoryBool(___heldItem);
                }
                ___heldItem = null;
            }
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
}
