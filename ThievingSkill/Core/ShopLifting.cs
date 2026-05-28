using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoonShared.Attributes;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using StardewValley.Menus;

namespace ThievingSkill.Core
{
    internal static class ShopliftingManager
    {
        // How much internal item value each Thieving level safely covers before adding extra caught chance
        public static int ShopliftGoldPerLevel => ModEntry.Config.ShopliftGoldPerLevel;

        // Base caught chance before skill level, daily attempts, and item value are applied
        public static double BaseCatchChance => ModEntry.Config.ShopliftingBaseCatchChance;

        // How much caught chance is reduced per Thieving level
        public static double CatchChanceReductionPerLevel => ModEntry.Config.ShopliftingCatchChanceReductionPerLevel;

        // How much caught chance is added for each shoplifting attempt already made today
        public static double CatchChanceIncreasePerAttemptToday => ModEntry.Config.ShopliftingCatchChanceIncreasePerAttemptToday;

        // How much caught chance is added for each value step above the player's safe value limit
        public static double CatchChanceIncreasePerValueStepOverLimit => ModEntry.Config.ShopliftingCatchChanceIncreasePerValueStepOverLimit;

        // How many days the player is banned from a shop after being caught
        public static int ShopBanDays => ModEntry.Config.ShopliftingBanDays;

        // How much EXP the player gets when they successfully shoplift
        public static int ShopliftingSuccessExp => ModEntry.Config.ShopliftingSuccessExp;

        // How much EXP the player gets when they are caught
        public static int ShopliftingCaughtExp => ModEntry.Config.ShopliftingCaughtExp;

        // How far away NPCs can witness the player getting caught
        public static float WitnessRadius => ModEntry.Config.ShopliftingWitnessRadius;

        // How much friendship nearby NPCs lose when the player is caught
        public static int WitnessFriendshipLoss => ModEntry.Config.ShopliftingWitnessFriendshipLoss;

        // How much friendship the shop owner loses when the player is caught
        public static int ShopOwnerFriendshipLoss => ModEntry.Config.ShopliftingShopOwnerFriendshipLoss;

        private const string TodayCountKey = "moonslime.Thieving.Shoplifting.TodayCount";
        private const string ShopBanPrefix = "moonslime.Thieving.Shoplifting.Ban/";

        private static readonly FieldInfo IsStorageShopField = AccessTools.Field(typeof(ShopMenu), "_isStorageShop");

        public static bool IsStealButtonHeld()
        {
            // Events.cs keeps track of whether the configured steal button is currently held
            bool held = Events.IsStealButtonHeld;
            return held;
        }

        public static bool TryShopliftClickedItem(ShopMenu shopMenu, int x, int y)
        {
            // Make sure the shop menu exists
            if (shopMenu == null)
                return false;

            // Make sure there is a player to give the item to / punish
            if (Game1.player == null)
                return false;

            // Only the local player should handle their own shoplifting attempt
            if (!Game1.player.IsLocalPlayer)
                return false;

            // Figure out which shop item row was clicked
            if (!TryGetClickedShopItem(shopMenu, x, y, out int shopIndex, out ISalable salable, out ItemStockInformation stockInfo))
                return false;

            // If we found a valid clicked item, try to shoplift it
            return TryShoplift(shopMenu, shopIndex, salable, stockInfo);
        }

        private static bool TryShoplift(ShopMenu shopMenu, int shopIndex, ISalable salable, ItemStockInformation stockInfo)
        {
            Farmer farmer = Game1.player;

            // Get a stable key for this shop so bans can be tracked per shop
            string shopKey = GetShopKey(shopMenu);

            // If the player is banned from this shop, close the menu and stop the attempt
            if (IsBannedFromShop(farmer, shopKey, out int daysRemaining))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.banned").Replace("{daysRemaining}", daysRemaining.ToString()), 3));
                shopMenu.exitThisMenu();
                return true;
            }

            // Check all general rules that might prevent shoplifting this item
            if (!CanAttemptShoplift(shopMenu, shopIndex, salable, stockInfo, out string deniedMessage))
            {
                Game1.playSound("cancel");

                if (!string.IsNullOrWhiteSpace(deniedMessage))
                    Game1.addHUDMessage(new HUDMessage(deniedMessage, 3));

                return true;
            }

            // Create the item the player would receive if the theft succeeds
            if (!TryCreateStolenItem(salable, out Item stolenItem))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_that"), 3));
                return true;
            }

            // Get the item's internal gold value for the fine and caught chance math
            int itemValue = GetShopliftValue(stolenItem);

            // Don't let the player steal items with no usable value
            if (itemValue <= 0)
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.no_value"), 3));
                return true;
            }

            // The player needs enough gold to pay the fine if they get caught
            if (farmer.Money < itemValue)
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.not_enough_gold"), 3));
                return true;
            }

            // Make sure the item can fit in the player's inventory before rolling the theft attempt
            if (!farmer.couldInventoryAcceptThisItem(stolenItem))
            {
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.inventory_full"), 3));
                return true;
            }

            // Get today's previous shoplifting attempts before this attempt is counted
            int attemptsToday = GetAttemptsToday(farmer);

            // Calculate the chance to get caught
            double catchChance = GetCatchChance(farmer, itemValue, attemptsToday);

            // Roll to see if the player gets caught
            double roll = Game1.random.NextDouble();

            // Count this shoplifting attempt for future attempts today
            AddAttemptToday(farmer);

            // If the roll is below the caught chance, punish the player and close the shop
            if (roll < catchChance)
            {
                GetCaught(shopMenu, farmer, shopKey, itemValue);
                return true;
            }

            // If the player was not caught, give them the item and update the shop stock
            StealItem(shopMenu, salable, stockInfo, stolenItem);

            // Give EXP for a successful shoplift
            if (ShopliftingSuccessExp > 0)
                Utilities.AddEXP(farmer, ShopliftingSuccessExp);

            return true;
        }

        private static bool CanAttemptShoplift(ShopMenu shopMenu, int shopIndex, ISalable salable, ItemStockInformation stockInfo, out string deniedMessage)
        {
            deniedMessage = "";

            // Make sure the shop exists
            if (shopMenu == null)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_from_shop");
                return false;
            }

            // Do not allow stealing from read-only shops
            if (shopMenu.readOnly)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_from_shop");
                return false;
            }

            // Do not allow stealing from storage shops
            bool isStorageShop = IsStorageShop(shopMenu);
            if (isStorageShop)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_from_shop");
                return false;
            }

            // Do not allow stealing from catalogues
            bool isCatalogueShop = IsCatalogueShop(shopMenu);
            if (isCatalogueShop)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_catalogue");
                return false;
            }

            // Do not allow stealing while the player is already holding an item on the cursor
            if (shopMenu.heldItem != null)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.put_down_item");
                return false;
            }

            // Make sure the clicked shop entry exists
            if (salable == null)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_that");
                return false;
            }

            // Recipes are learned instead of added as normal inventory items, so don't support them here
            if (salable.IsRecipe)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_recipes");
                return false;
            }

            // Only actual items can be stolen
            if (salable is not Item)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_that");
                return false;
            }

            // Make sure the shop has stock data for this item
            if (stockInfo == null)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_that");
                return false;
            }

            // Do not allow stealing if the item is out of stock
            if (stockInfo.Stock == 0)
            {
                deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.out_of_stock");
                return false;
            }

            // Respect vanilla's per-item purchase check if the shop has one
            if (shopMenu.canPurchaseCheck != null)
            {
                bool canPurchase = shopMenu.canPurchaseCheck(shopIndex);

                if (!canPurchase)
                {
                    deniedMessage = Utilities.Text("moonslime.Thieving.shoplifting.cant_steal_now");
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetClickedShopItem(ShopMenu shopMenu, int x, int y, out int shopIndex, out ISalable salable, out ItemStockInformation stockInfo)
        {
            shopIndex = -1;
            salable = null;
            stockInfo = null;

            // Make sure the shop menu exists
            if (shopMenu == null)
                return false;

            // Make sure the shop has clickable item rows
            if (shopMenu.forSaleButtons == null)
                return false;

            // Make sure the shop has items for sale
            if (shopMenu.forSale == null)
                return false;

            // Make sure the shop has price and stock data
            if (shopMenu.itemPriceAndStock == null)
                return false;

            // Go through each visible shop button and check if the click landed on it
            for (int i = 0; i < shopMenu.forSaleButtons.Count; i++)
            {
                int candidateIndex = shopMenu.currentItemIndex + i;

                // If this button points beyond the shop list, skip it
                if (candidateIndex >= shopMenu.forSale.Count)
                    continue;

                bool containsPoint = shopMenu.forSaleButtons[i].containsPoint(x, y);

                // If the click was not on this row, keep checking the next row
                if (!containsPoint)
                    continue;

                // Store the clicked shop index and item
                shopIndex = candidateIndex;
                salable = shopMenu.forSale[shopIndex];

                // If the clicked row somehow has no item, stop
                if (salable == null)
                    return false;

                // Get the item's shop stock data
                if (!shopMenu.itemPriceAndStock.TryGetValue(salable, out stockInfo))
                    return false;

                return true;
            }

            return false;
        }

        private static bool TryCreateStolenItem(ISalable salable, out Item stolenItem)
        {
            stolenItem = null;

            // Ask the shop entry for the item instance it would normally give the player
            if (salable?.GetSalableInstance() is not Item item)
                return false;

            // Shoplifting only steals one item at a time
            stolenItem = item;
            stolenItem.Stack = 1;

            return true;
        }

        private static int GetShopliftValue(Item item)
        {
            // If there is no item, it has no value
            if (item == null)
                return 0;

            // Try to get the internal object data for the item
            var itemData = ItemRegistry.GetData(item.QualifiedItemId);

            // If this is a normal object, use the internal Data/Objects price
            if (itemData != null && Game1.objectData.TryGetValue(itemData.ItemId, out var objectData))
            {
                int price = Math.Max(0, objectData.Price);
                return price;
            }

            // If it is not a normal object, fall back to the item's sale price
            int fallbackPrice = Math.Max(0, item.salePrice());
            return fallbackPrice;
        }

        private static double GetCatchChance(Farmer farmer, int itemValue, int attemptsToday)
        {
            // Get the player's current Thieving level including buffs
            int thievingLevel = Utilities.GetLevel(farmer);

            // Figure out how many value steps the player can safely steal at their current level
            int safeValueSteps = thievingLevel + 1;

            // If the player has the Phantom profession, increase their safe shoplifting value by one step
            if (farmer.HasCustomProfession(Events.Proffession10b1))
                safeValueSteps++;

            // Figure out how much item value the player can safely steal before getting extra caught chance
            int safeValueLimit = safeValueSteps * ShopliftGoldPerLevel;

            // Figure out how far above the safe value limit this item is
            int overValue = Math.Max(0, itemValue - safeValueLimit);

            // Convert the extra value into penalty steps
            int overValueSteps = overValue <= 0 ? 0 : (int)Math.Ceiling(overValue / (double)ShopliftGoldPerLevel);

            // Start with the base caught chance
            double chance = BaseCatchChance;

            // Reduce caught chance by Thieving level
            chance -= thievingLevel * CatchChanceReductionPerLevel;

            // Increase caught chance for each attempt already made today
            chance += attemptsToday * CatchChanceIncreasePerAttemptToday;

            // Increase caught chance if the item is above the player's safe value limit
            chance += overValueSteps * CatchChanceIncreasePerValueStepOverLimit;

            // If the player has the Rogue profession, reduce caught chance by 10%.
            bool rogueBonus = farmer.HasCustomProfession(Events.Proffession10b2);

            if (rogueBonus)
            {
                chance -= 0.1;
            }

            // Clamp the final chance between 0% and 100%
            double clampedChance = Math.Clamp(chance, 0.0, 1.0);

            return clampedChance;
        }

        private static void StealItem(ShopMenu shopMenu, ISalable salable, ItemStockInformation stockInfo, Item stolenItem)
        {
            Farmer farmer = Game1.player;

            // Try to add the stolen item to the player's inventory
            if (farmer.addItemToInventoryBool(stolenItem))
            {
                // Give feedback that the theft worked
                Game1.playSound("coin");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.stole_item").Replace("{itemName}", stolenItem.DisplayName), 2));

                // Reduce the shop stock if this item has limited stock
                ReduceShopStock(shopMenu, salable, stockInfo);
            }
            else
            {
                // This should rarely happen because we already checked inventory space earlier
                Game1.playSound("cancel");
                Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.inventory_full"), 3));
            }
        }

        private static void GetCaught(ShopMenu shopMenu, Farmer farmer, string shopKey, int itemValue)
        {
            // Remove gold equal to the stolen item's internal value
            farmer.Money = Math.Max(0, farmer.Money - itemValue);

            // Ban the player from this shop for a few days
            SetShopBan(farmer, shopKey, ShopBanDays);

            // Apply friendship penalties to witnesses and the shop owner
            ApplyCaughtFriendshipLoss(shopMenu, farmer);

            // Give EXP for being caught if that is configured
            if (ShopliftingCaughtExp > 0)
                Utilities.AddEXP(farmer, ShopliftingCaughtExp);

            // Give the player feedback
            Game1.playSound("cancel");
            Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.caught").Replace("{itemValue}", itemValue.ToString()), 3));

            // Close the shop menu after getting caught
            shopMenu.exitThisMenu();
        }

        private static void ReduceShopStock(ShopMenu shopMenu, ISalable salable, ItemStockInformation stockInfo)
        {
            // Make sure the needed shop data exists
            if (shopMenu == null || salable == null || stockInfo == null)
                return;

            // Infinite-stock items do not need to be reduced
            if (stockInfo.Stock == int.MaxValue || salable.IsInfiniteStock())
                return;

            // Save the old stock so we can tell if vanilla stock syncing changed it
            int oldStock = stockInfo.Stock;

            // Let the shop run its synced stock purchase handling
            shopMenu.HandleSynchedItemPurchase(salable, Game1.player, 1);

            // Get the updated stock data after the synced purchase handling
            if (!shopMenu.itemPriceAndStock.TryGetValue(salable, out ItemStockInformation updatedStockInfo))
                return;

            // If synced purchase handling did not reduce the stock, reduce it manually
            if (updatedStockInfo.Stock == oldStock)
            {
                updatedStockInfo.Stock = Math.Max(0, oldStock - 1);
                shopMenu.itemPriceAndStock[salable] = updatedStockInfo;
            }

            // Keep the synced stack item matching the new stock value
            if (updatedStockInfo.ItemToSyncStack != null)
                updatedStockInfo.ItemToSyncStack.Stack = updatedStockInfo.Stock;

            // If there is no stock left, remove the item from the shop menu
            if (updatedStockInfo.Stock <= 0)
            {
                shopMenu.itemPriceAndStock.Remove(salable);
                shopMenu.forSale.Remove(salable);
                shopMenu.hoveredItem = null;
            }

            // Make sure the shop list scroll position stays in a valid range
            shopMenu.currentItemIndex = Math.Max(0, Math.Min(shopMenu.currentItemIndex, Math.Max(0, shopMenu.forSale.Count - 4)));

            // Update controller/keyboard neighbor links for the remaining shop buttons
            shopMenu.updateSaleButtonNeighbors();
        }

        private static void ApplyCaughtFriendshipLoss(ShopMenu shopMenu, Farmer farmer)
        {
            // Track NPCs who already lost friendship as witnesses so the owner does not get double punished
            HashSet<string> affectedNpcNames = new HashSet<string>();

            // Phantom Reduces witness Radius
            int newWitnessRaidus = (int)WitnessRadius;
            if (farmer.HasCustomProfession(Events.Proffession10b1))
                newWitnessRaidus = newWitnessRaidus >> 1;

            // Apply the witness friendship penalty to nearby NPCs with friendship data
            if (newWitnessRaidus > 0 && WitnessFriendshipLoss > 0)
                Utilities.LoseFriendshipWithNpcsInRange(farmer, newWitnessRaidus, WitnessFriendshipLoss);

            // Apply the shop owner friendship penalty if the owner has friendship data
            foreach (string ownerName in GetShopOwnerNames(shopMenu))
            {
                if (!farmer.friendshipData.ContainsKey(ownerName))
                    continue;

                int ownerLoss = Math.Abs(ShopOwnerFriendshipLoss);


                farmer.friendshipData[ownerName].Points -= ownerLoss;
            }
        }

        private static IEnumerable<string> GetShopOwnerNames(ShopMenu shopMenu)
        {
            // If this shop has no shop data or no owner data, there are no owners to penalize
            if (shopMenu?.ShopData?.Owners == null)
                yield break;

            // Go through each possible shop owner entry
            foreach (ShopOwnerData ownerData in shopMenu.ShopData.Owners)
            {
                // Skip bad owner data
                if (ownerData == null)
                    continue;

                // Only named NPC owners can have friendship penalties
                if (ownerData.Type != ShopOwnerType.NamedNpc)
                    continue;

                // Skip empty names
                if (string.IsNullOrWhiteSpace(ownerData.Name))
                    continue;

                // Respect the shop owner's conditions
                if (!ownerData.IsValid(null))
                    continue;

                yield return ownerData.Name;
            }
        }

        private static bool IsStorageShop(ShopMenu shopMenu)
        {
            // If there is no shop menu, assume it is not a storage shop
            if (shopMenu == null)
                return false;

            // If reflection failed to find the field, assume it is not a storage shop
            if (IsStorageShopField == null)
                return false;

            // Read the private _isStorageShop field
            bool isStorageShop = IsStorageShopField.GetValue(shopMenu) is true;
            return isStorageShop;
        }

        private static bool IsCatalogueShop(ShopMenu shopMenu)
        {
            // If the shop has no ID, do not treat it as a catalogue
            if (shopMenu == null || string.IsNullOrWhiteSpace(shopMenu.ShopId))
                return false;

            // Block catalogue-style shops so the player cannot steal from infinite catalogues
            bool isCatalogue = shopMenu.ShopId.Contains("Catalogue", StringComparison.OrdinalIgnoreCase) ||
                               shopMenu.ShopId.Contains("Catalog", StringComparison.OrdinalIgnoreCase);

            return isCatalogue;
        }

        public static string GetShopKey(ShopMenu shopMenu)
        {
            // Prefer the shop's real ShopId when one exists
            if (!string.IsNullOrWhiteSpace(shopMenu?.ShopId))
                return shopMenu.ShopId;

            // Fallback for odd shops without a ShopId
            return "UnknownShop";
        }

        public static int GetAttemptsToday(Farmer farmer)
        {
            // If there is no farmer, they have no attempts
            if (farmer == null)
                return 0;

            // If there is no stored attempt count, they have no attempts
            if (!farmer.modDataForSerialization.TryGetValue(TodayCountKey, out string value))
                return 0;

            // Parse the stored attempt count safely
            int count = int.TryParse(value, out int parsedCount) ? parsedCount : 0;
            return count;
        }

        private static void AddAttemptToday(Farmer farmer)
        {
            // If there is no farmer, there is no attempt to count
            if (farmer == null)
                return;

            // Increase the number of shoplifting attempts made today
            int count = GetAttemptsToday(farmer);
            farmer.modDataForSerialization[TodayCountKey] = (count + 1).ToString();
        }

        public static void ClearDailyAttemptCount(Farmer farmer)
        {
            // If there is no farmer, there is nothing to clear
            if (farmer == null)
                return;

            // Remove the daily attempt counter at the start of a new day
            farmer.modDataForSerialization.Remove(TodayCountKey);
        }

        public static bool IsBannedFromShop(Farmer farmer, string shopKey, out int daysRemaining)
        {
            daysRemaining = 0;

            // If there is no farmer, they are not banned
            if (farmer == null)
                return false;

            // If there is no shop key, we cannot check a ban
            if (string.IsNullOrWhiteSpace(shopKey))
                return false;

            // Build the modData key for this shop's ban
            string key = ShopBanPrefix + shopKey;

            // If there is no ban key, the player is not banned
            if (!farmer.modDataForSerialization.TryGetValue(key, out string value))
                return false;

            // If the stored value is invalid or expired, remove it
            if (!int.TryParse(value, out daysRemaining) || daysRemaining <= 0)
            {
                farmer.modDataForSerialization.Remove(key);
                daysRemaining = 0;
                return false;
            }

            return true;
        }

        private static void SetShopBan(Farmer farmer, string shopKey, int days)
        {
            // If there is no farmer, there is nobody to ban
            if (farmer == null)
                return;

            // If there is no shop key, there is no shop to ban them from
            if (string.IsNullOrWhiteSpace(shopKey))
                return;

            // Store the ban duration on the player
            int safeDays = Math.Max(0, days);
            farmer.modDataForSerialization[ShopBanPrefix + shopKey] = safeDays.ToString();
        }

        public static void UpdateShopBansForNewDay(Farmer farmer)
        {
            // If there is no farmer, there are no bans to update
            if (farmer == null)
                return;

            // Get every shop ban key from the player's modData
            List<string> banKeys = farmer.modDataForSerialization.Keys.Where(key => key.StartsWith(ShopBanPrefix)).ToList();

            // Tick each ban down by one day
            foreach (string key in banKeys)
            {
                // If the value is broken, remove the key
                if (!int.TryParse(farmer.modDataForSerialization[key], out int daysRemaining))
                {
                    farmer.modDataForSerialization.Remove(key);
                    continue;
                }

                int oldDays = daysRemaining;
                daysRemaining--;

                // Remove expired bans
                if (daysRemaining <= 0)
                {
                    farmer.modDataForSerialization.Remove(key);
                }
                else
                {
                    // Otherwise store the updated remaining days
                    farmer.modDataForSerialization[key] = daysRemaining.ToString();
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShopMenu), "Initialize")]
    class ShopMenu_Initialize_ShopliftingBan_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(
        ShopMenu __instance)
        {
            try
            {
                // Make sure the shop menu exists
                if (__instance == null)
                    return;

                // Make sure there is a player
                if (Game1.player == null)
                    return;

                // Only the local player should be blocked from their own shop menu
                if (!Game1.player.IsLocalPlayer)
                    return;

                // Get the key used to track bans for this shop
                string shopKey = ShopliftingManager.GetShopKey(__instance);

                // If the player is not banned from this shop, let the menu stay open
                if (!ShopliftingManager.IsBannedFromShop(Game1.player, shopKey, out int daysRemaining))
                    return;

                // Close the menu shortly after it opens so the shop can finish initializing first
                DelayedAction.functionAfterDelay(delegate
                {
                    // Only close the menu if this is still the active menu
                    if (Game1.activeClickableMenu == __instance)
                    {
                        Game1.playSound("cancel");
                        Game1.addHUDMessage(new HUDMessage(Utilities.Text("moonslime.Thieving.shoplifting.banned").Replace("{daysRemaining}", daysRemaining.ToString()), 3));
                        __instance.exitThisMenu();
                    }
                }, 10);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {nameof(ShopMenu_Initialize_ShopliftingBan_Patch)}:\n{ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.receiveLeftClick))]
    class ShopMenu_receiveLeftClick_Shoplifting_Patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        ShopMenu __instance, int x, int y, bool playSound)
        {
            try
            {
                // Check whether the player is holding the configured steal button
                bool stealButtonHeld = ShopliftingManager.IsStealButtonHeld();

                // If they are not holding the steal button, let vanilla handle the click normally
                if (!stealButtonHeld)
                    return true;

                // If there is no shop menu, let vanilla handle it
                if (__instance == null)
                    return true;

                // If the shop just opened, let vanilla ignore/process the click normally
                if (__instance.safetyTimer > 0)
                    return true;

                // Try to handle the click as a shoplifting attempt
                bool handled = ShopliftingManager.TryShopliftClickedItem(__instance, x, y);

                // If shoplifting handled it, skip the vanilla purchase
                if (handled)
                    return false;

                // If shoplifting did not handle it, let vanilla process the click
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {nameof(ShopMenu_receiveLeftClick_Shoplifting_Patch)}:\n{ex}");
                return true;
            }
        }
    }
}
