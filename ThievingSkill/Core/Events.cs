using MoonShared.Attributes;
using Microsoft.Xna.Framework;
using MoonShared;
using SpaceCore;
using SpaceCore.Interface;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using Log = MoonShared.Attributes.Log;

namespace ThievingSkill.Core
{
    [SEvent]
    internal class Events
    {
        public const string Cooldown = "moonslime.Thieving.Cooldown";
        public const string BadLuckProtection = "moonslime.Thieving.BadLuck";

        public static KeyedProfession Proffession5a => Thieving_Skill.Thieving5a;
        public static KeyedProfession Proffession5b => Thieving_Skill.Thieving5b;
        public static KeyedProfession Proffession10a1 => Thieving_Skill.Thieving10a1;
        public static KeyedProfession Proffession10a2 => Thieving_Skill.Thieving10a2;
        public static KeyedProfession Proffession10b1 => Thieving_Skill.Thieving10b1;
        public static KeyedProfession Proffession10b2 => Thieving_Skill.Thieving10b2;

        public static bool IsStealButtonHeld { get; private set; }

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            MoonShared.Attributes.Log.Trace("Thieving: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Thieving_Skill());
        }

        [SEvent.SaveLoaded]
        private static void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            string skillId = "moonslime.Thieving";
            int skillLevel = Game1.player.GetCustomSkillLevel(skillId);
            if (skillLevel == 0)
                return;

            //           CheckSkillLevelUp(skillId, skillLevel, 5, Proffession5a, Proffession5b);
            //           CheckSkillLevelUp(skillId, skillLevel, 10, Proffession10a1, Proffession10a2, Proffession10b1, Proffession10b2);

            AddCraftingRecipes(skillId, skillLevel);
            AddCookingRecipes(skillId, skillLevel);
        }

        private static void CheckSkillLevelUp(string skillId, int skillLevel, int targetLevel, params KeyedProfession[] professions)
        {
            if (skillLevel >= targetLevel && professions.Any(prof => !Game1.player.HasCustomProfession(prof)))
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(skillId, targetLevel));
        }

        private static void AddCraftingRecipes(string skillId, int skillLevel)
        {
            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(skillId) || conditions.Split(" ").Length < 2 || skillLevel < int.Parse(conditions.Split(" ")[1]))
                    continue;

                Game1.player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }
        }

        private static void AddCookingRecipes(string skillId, int skillLevel)
        {
            foreach (KeyValuePair<string, string> recipePair in DataLoader.CookingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(skillId) || conditions.Split(" ").Length < 2 || skillLevel < int.Parse(conditions.Split(" ")[1]))
                    continue;

                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) && !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                    Game1.mailbox.Add("robinKitchenLetter");
            }
        }

        // Set the stealing cooldown to 0 at the end of the day
        [SEvent.DayEnding]
        private static void DayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;

            var farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            LockpickManager.ClearDailyDoorMemory(farmer);

            if (!farmer.modDataForSerialization.TryGetValue(Cooldown, out string value))
                return;

            int storedCooldown = int.Parse(value);
            if (storedCooldown != 0) { SetCooldown(farmer, 0); }
        }

        [SEvent.DayStarted]
        private static void DayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;

            Farmer farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            ShopliftingManager.ClearDailyAttemptCount(farmer);
            ShopliftingManager.UpdateShopBansForNewDay(farmer);

            LockpickManager.ClearDailyDoorMemory(farmer);
        }

        [SEvent.TimeChanged]
        private static void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;

            var farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);

            if (!farmer.modDataForSerialization.TryGetValue(Cooldown, out string value))
                return;

            int storedCooldown = int.Parse(value);
            if (storedCooldown <= 0)
                return;

            SetCooldown(farmer, storedCooldown - 1);

            if (storedCooldown - 1 == 0)
            {
                string line = "moonslime.Thieving.Cooldown.off_cooldown";
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(line)));
            }
        }


        [SEvent.ButtonPressed]
        private static void ButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            if (!Game1.player.IsLocalPlayer)
            {
                return;
            }

            if (e.Button == ModEntry.Config.Key_Cast)
            {
                IsStealButtonHeld = true;
            }
        }

        [SEvent.ButtonReleased]
        private static void ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {

            if (Game1.player.IsLocalPlayer && e.Button == ModEntry.Config.Key_Cast)
                IsStealButtonHeld = false;

            if (e.Button != ModEntry.Config.Key_Cast || !Game1.player.IsLocalPlayer || Game1.eventUp || !Context.CanPlayerMove)
                return;

            Farmer player = Game1.player;
            player.modDataForSerialization.TryGetValue(Cooldown, out string value_1);
            int storedCooldown = 0;
            if (value_1 != null)
            {
                storedCooldown = int.Parse(value_1);
            }

            if (storedCooldown != 0)
            {
                player.performPlayerEmote("sad");
                int timeLeft = storedCooldown * 10;
                string line = $"moonslime.Thieving.Cooldown.on_cooldown.{timeLeft}";
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(line)));
                return;
            }

            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;
            List<NPC> npcsInRange = [];
            List<NPC> monstersInRange = [];

            foreach (var npc in location.characters)
            {
                float distance = Vector2.Distance(npc.Tile, playerTile);
                float profession = (player.HasCustomProfession(Proffession10a1) ? 5 : 1);

                Log.Trace("Thieving: Button pressed, going to go through the list...");
                Log.Trace("NPC name is: " + npc.Name);
                Log.Trace("is NPC villager?: " + npc.IsVillager.ToString());
                Log.Trace("NPC Distance: " + distance.ToString());
                Log.Trace("distance value: " + profession.ToString());
                Log.Trace("distance check: " + (distance > profession).ToString());

                //Check to see if the config is set to only steal from monsters
                if (!ModEntry.Config.MonstersOnly &&
                    //Check to see if they are a villager
                    npc.IsVillager &&
                    //Check to see if they are in range of the player
                    //5 tiles if they have the range profession, 1 if not
                    distance <= profession &&
                    //Check to see if they are giftable
                    npc.CanReceiveGifts() &&
                    //Make sure the player has friendship data with them
                    player.friendshipData.ContainsKey(npc.Name) &&
                    //Check to make sure the player has not given them two gifts this week
                    player.friendshipData[npc.Name].GiftsThisWeek < 2 &&
                    //Check to make sure the player has not given them a gift today
                    player.friendshipData[npc.Name].GiftsToday < 1 &&
                    //And last, I don't want the player stealing from the elderly
                    //Sorry Cross-mod Elderfolk
                    npc.Name != "Evelyn" && npc.Name != "George")
                {
                    npcsInRange.Add(npc);
                    continue;
                }

                // The monster side of things. we don't want to continue if the NPC is not a monster.
                if (npc is not Monster)
                {
                    continue;
                }

                Monster monsterNPC = (Monster)npc;
                profession = (player.HasCustomProfession(Proffession10a1) ? 6 : 2);
                if (monsterNPC.IsMonster &&
                    !monsterNPC.CanSocialize && // Just in case someone got the idea of making a friendly monster a monster class instead of an NPC class
                    !monsterNPC.IsInvisible && // If we can't see the monster, we can't steal from it
                    !monsterNPC.isInvincible() && //If the monster is currently invincible, don't steal from it
                    monsterNPC is not null && //If the NPC is a monster
                    distance <= profession //and if the monster is in range.
                    )
                {
                    monstersInRange.Add(npc);
                }
            }

            // We always steal from villagers first, if there are both villagers and monsters on the map
            if (npcsInRange.Any())
            {
                if (player.HasCustomProfession(Proffession10a1))
                {
                    foreach (var npc in npcsInRange)
                    {
                        // If the player has profession Proffession10a1, then all villagers on the list are affected
                        Villager_STEALING(npc, player);
                    }
                }
                else
                {
                    // Only the first villager on the list is affected
                    Villager_STEALING(npcsInRange[0], player);
                }
                return;
            }

            if (monstersInRange.Any())
            {
                if (player.HasCustomProfession(Proffession10a1))
                {
                    foreach (var monster in monstersInRange)
                    {
                        // If the player has profession Proffession10a1, then all monsters on the list are affected
                        Monster_STEALING(monster, player);
                    }
                }
                else
                {
                    // Only the first monster on the list is affected
                    Monster_STEALING(monstersInRange[0], player);
                }
                return;
            }

            // If there are no valid NPCs for the player to steal from ...
            // ... play an emote as feedback
            player.performPlayerEmote("sad");
        }

        public static void Monster_STEALING(NPC npc, Farmer player)
        {
            // If the NPC is not a monster, leave this method
            if (npc is not Monster)
            {
                return;
            }
            // the NPC is a monster, so we now cast the NPC as a monster for the rest of the method
            Monster monsterNPC = (Monster)npc;

            // Get the random dice roll from 0 to 99
            decimal diceRoll = Game1.random.Next(100);
            decimal diceBonus = 1;
            Log.Trace($"Initial dice roll: {diceRoll}");
            Log.Trace($"Initial dice bonus: {diceBonus}");

            // Get how good the player is at thieving.
            // this is the player's thieving level * 2 + the dice roll.
            diceBonus = CalculateThievingPlayerLevel(diceBonus, player);
            Log.Trace($"dice bonus after level addition: {diceBonus}");

            // Add in the player luck to the roll
            diceBonus = CalculateThievingLuckLevel(diceBonus, player);
            Log.Trace($"dice bonus after luck addition: {diceBonus}");

            // Add 10 if the player has the profession bonus
            diceBonus = CalculateThievingProfessionBonus(diceBonus, player);
            Log.Trace($"dice bonus after profession addition: {diceBonus}");

            // Calculate light levels
            diceBonus = CalculateThievingNightTimeAdjustment(diceBonus, player);
            Log.Trace($"dice roll after night time addition: {diceRoll}");

            // Calculate weather bonus
            diceBonus = CalculateThievingWeatherAdjustment(diceBonus, player);
            Log.Trace($"dice bonus after rainy weather addition: {diceBonus}");

            // Set the theftLevel string to 0 for now...
            string theftLevel = "level_0";

            // Set the sound that will play when we steal from the monster
            string soundID = "wind";

            // If the player has the skill set to Thieving, then we...
            // ... Get the thieving level and calculate it here and
            // ... Set the sound ID to just a swish of the wind

            Log.Trace($"Final dice bonus is: {diceBonus}");
            decimal finalRoll = diceRoll * diceBonus;
            Log.Trace($"Final dice roll is: {finalRoll}");
            theftLevel = GetTheftLevel(finalRoll);

            Log.Trace($"Stealing vs monster, Dice roll is {diceRoll}, and theft level is {theftLevel}");

            // We play an emote and sound as feedback for the skill to the player that it is functioning
            player.performPlayerEmote("exclamation");
            player.currentLocation.playSound(soundID);

            // Create the loot the monster will drop
            Monster_CreateLoot(monsterNPC, player, theftLevel);

            // If the player has profession 10a2, then we heal the player 25% of their health and stamina.
            if (player.HasCustomProfession(Proffession10a2))
            {
                const double healingPercentage = 0.25;

                // Healing Health
                int healthIncrease = (int)(player.maxHealth * healingPercentage);
                player.health = Math.Min(player.health + healthIncrease, player.maxHealth);

                // Restoring Stamina
                int staminaIncrease = (int)(player.MaxStamina * healingPercentage);
                player.Stamina = Math.Min(player.Stamina + staminaIncrease, player.MaxStamina);
            }

            int exp = ((int)(CalculateExpTheftLevel(theftLevel) * 0.33));

            Utilities.AddEXP(player, exp);
        }

        public static void Villager_STEALING(NPC npc, Farmer player)
        {
            // Get the random dice roll from 0 to 99
            decimal diceRoll = Game1.random.Next(100);
            decimal diceBonus = 1;
            Log.Trace($"Initial dice roll: {diceRoll}");
            Log.Trace($"Initial dice bonus: {diceBonus}");

            // Get how good the player is at thieving.
            // this is the player's thieving level * 2 + the dice roll.
            diceBonus = CalculateThievingPlayerLevel(diceBonus, player);
            Log.Trace($"dice bonus after level addition: {diceBonus}");

            // Add in the player luck to the roll
            diceBonus = CalculateThievingLuckLevel(diceBonus, player);
            Log.Trace($"dice bonus after luck addition: {diceBonus}");

            // Get the direction the player is facing vs the direction the NPC is facing.
            // Like if the player and NPC are facing each other, is the player facing the side, or the NPC's back
            string facingSide = GetFacingSide(npc, player);
            // Get the theft level adjustment based on if the player is facing the NPC's back, sides, or front
            diceBonus = CalculateThievingDirectionChange(diceBonus, facingSide, player.HasCustomProfession(Proffession5a));
            Log.Trace($"dice bonus after direction addition: {diceBonus}");

            // Add 10 if the player has the profession bonus
            diceBonus = CalculateThievingProfessionBonus(diceBonus, player);
            Log.Trace($"dice bonus after profession addition: {diceBonus}");

            // Calculate light levels
            diceBonus = CalculateThievingNightTimeAdjustment(diceBonus, player);
            Log.Trace($"dice bonus after night time addition: {diceBonus}");

            // Calculate weather bonus
            diceBonus = CalculateThievingWeatherAdjustment(diceBonus, player);
            Log.Trace($"dice bonus after rainy weather addition: {diceBonus}");

            // Set the theft level string to the default value
            string theftLevel = "level_0";

            // Set the sound ID of what we will play
            string soundID = "wind";

            // If the player has the friendship protection profession, they have a 50% chance of ignoring friendship lost
            bool friendshipProtection = player.HasCustomProfession(Proffession5b) && Game1.random.NextDouble() < 0.5;

            // Calculate friendship lost based on facing direction
            int friendshipLost = CalculateFriendshipChangeDirectional(facingSide, friendshipProtection);

            Log.Trace($"Final dice bonus is: {diceBonus}");
            decimal finalRoll = diceRoll * diceBonus;
            Log.Trace($"Final dice roll is: {finalRoll}");
            theftLevel = GetTheftLevel(finalRoll);

            #region Player Skill Feedback

            // This section is to give feedback to the player

            // Make the NPC jump if they noticed enough
            bool jump = theftLevel == "level_0" || theftLevel == "level_1";
            if (jump) npc.jump();

            // Play an emote above the player's head
            player.performPlayerEmote("exclamation");

            // Play a sound
            player.currentLocation.playSound(soundID);

            // Set the string to the current NPC's name and the theft level. So each NPC can have a custom string
            string type = "Stolen";
            string theftString = ModEntry.Instance.I18N.Get($"moonslime.Thieving.{type}.{npc.Name}.{theftLevel}");
            if (theftString.Contains("no translation"))
            {
                // If no translation/custom string is found, set it to the default string for the theft level
                theftString = ModEntry.Instance.I18N.Get($"moonslime.Thieving.{type}.default.{theftLevel}");
            }

            // Show text above the NPC's head
            npc.showTextAboveHead(theftString);

            #endregion

            Log.Trace($"Stealing vs villager, final Dice roll is {diceRoll}, and theft level is {theftLevel}");

            // adjust friendship lost based on if it's day or night if the player is outside.
            // acting in broad daylight increases the loss of friendship
            // while acting at night lowers the friendship lost
            friendshipLost = CalculateFriendshipNightTimeAdjustment(friendshipLost, player);

            // Adjust friendship lost based on theft level
            friendshipLost = CalculateFriendshipTheftLevel(friendshipLost, theftLevel, friendshipProtection);

            // Now add the friendship data to the NPC and make this count as a gift for the day and week
            Friendship friendship = player.friendshipData[npc.Name];
            friendship.GiftsToday++;
            friendship.GiftsThisWeek++;
            friendship.LastGiftDate = new WorldDate(Game1.Date);
            friendship.Points += friendshipLost;

            // Calculate if the villager is going to drop loot or not
            Villager_CreateLoot(npc, player, theftLevel);

            // If the player has profession 10a2, then we heal the player 25% of their health and stamina.
            if (player.HasCustomProfession(Proffession10a2))
            {
                const double healingPercentage = 0.25;

                // Healing Health
                int healthIncrease = (int)(player.maxHealth * healingPercentage);
                player.health = Math.Min(player.health + healthIncrease, player.maxHealth);

                // Restoring Stamina
                int staminaIncrease = (int)(player.MaxStamina * healingPercentage);
                player.Stamina = Math.Min(player.Stamina + staminaIncrease, player.MaxStamina);
            }

            // Add exp to the player based on how well the villager theft attempt went
            Utilities.AddEXP(player, CalculateExpTheftLevel(theftLevel));
        }

        public static void Monster_Knockback(Monster monster, float knockBackModifier, Farmer who)
        {
            Microsoft.Xna.Framework.Rectangle boundingBox = monster.GetBoundingBox();
            who.currentLocation.damageMonster(boundingBox, 0, 0, false, knockBackModifier, 100, 0f, 1f, triggerMonsterInvincibleTimer: false, who);
            monster.stunTime.Value += 200;
        }

        public static void Monster_CreateLoot(Monster npc, Farmer player, string theftLevel)
        {
            var lootList = new List<string>();

            //Get the player level
            int playerLevel = Utilities.GetLevel(player);

            // Loot generation based on player level and profession
            if (playerLevel >= 3)
            {
                AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Neutral"]);
            }

            if (playerLevel >= 7)
            {
                AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Like"]);
            }

            if (player.HasCustomProfession(Proffession10b1))
            {
                AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Love"]);
                lootList.AddRange(new List<string> { "516", "517", "518", "519", "520", "521", "522", "523", "524", "525", "526", "528", "529", "530", "531", "532", "533", "534" });
            }

            lootList.AddRange(npc.objectsToDrop);

            // Filter out invalid items and select a random valid item
            // This also removes any item that is too expensive for the player's current Thieving level
            List<string> finalList = lootList.Where(item => IsValidLootItemForLevel(item, player)).ToList();
            lootList.Clear();
            string item = finalList.RandomChoose(Game1.random, "766");

            CreateLoot(npc, player, theftLevel, item);
        }

        public static void Villager_CreateLoot(NPC npc, Farmer player, string theftLevel)
        {
            // Generate a list of items based off the NPC's likes, neutrals, and loves
            List<string> lootList = GetItemList(npc, player);

            // Make a new list
            var finalList = new List<string>();

            // For each item in the loot list, we discard any that might be...
            // a null spot, a negative item (which is a category), if it has no Item Data (so an error item),
            // or if it is too expensive for the player's current Thieving level
            foreach (string lootItem in lootList)
            {
                if (IsValidLootItemForLevel(lootItem, player))
                {
                    finalList.Add(lootItem);
                }
            }
            // Clear the old list as it's not needed anymore
            lootList.Clear();

            // Add Mayor Lewis' shorts to the list if the NPC is Lewis or Marnie
            if ((npc.Name == "Lewis" || npc.Name == "Marnie") && IsValidLootItemForLevel("789", player))
            {
                finalList.Add("789");
            }

            // Shuffle the list to add in an additional layer of random to it after we finished adding all the items to it.
            finalList.Shuffle(Game1.random);

            // Select a random item from the final list
            // If there is somehow nothing on the final list, return bread
            string item = finalList.RandomChoose(Game1.random, "766");

            CreateLoot(npc, player, theftLevel, item);
        }

        public static void CreateLoot(NPC npc, Farmer player, string theftLevel, string item)
        {
            // Define thresholds for different theft levels
            Dictionary<string, decimal> theftThresholds = new()
            {
                { "level_0", 4.0m },
                { "level_1", 3.25m },
                { "level_2", 2.5m },
                { "level_3", 1.75m },
                { "level_4", 1.0m }
            };

            //Get the player level
            decimal diceBonus = 1;
            Log.Trace($"Stealing loot, Loot dice bonus is: {diceBonus}");
            diceBonus = CalculateThievingPlayerLevel(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after player level");
            diceBonus = CalculateThievingLuckLevel(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after player luck");
            diceBonus = CalculateThievingProfessionBonus(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after player profession");
            diceBonus = CalculateThievingNightTimeAdjustment(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after night time bonus");
            diceBonus = CalculateThievingWeatherAdjustment(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after weather bonus");
            diceBonus = CalculateBadLuckProtection(diceBonus, player);
            Log.Trace($"Loot dice bonus is: {diceBonus} after bad luck protection");

            decimal diceRoll = Game1.random.Next(100);
            Log.Trace($"Loot dice roll is {diceRoll}");

            decimal finalroll = diceRoll * diceBonus;

            decimal lootChance = 120 - ((20 - theftThresholds[theftLevel]) * (5 - theftThresholds[theftLevel]));

            Log.Trace($"Stealing loot, Loot Dice roll is {finalroll}, theft level is {theftLevel}, and loot chance is {lootChance}");

            // Determine loot drop based on theft level
            if (finalroll > lootChance)
            {
                if (theftLevel == "level_4")
                {
                    int howMany = (int)(Math.Floor(finalroll / 25));
                    Game1.createMultipleObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, howMany, player.UniqueMultiplayerID, npc.currentLocation);
                }
                else
                {
                    Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                }

                decimal ZeroBadLuckProtection = 0.0m;
                // We reset their badluck protection since they got loot
                if (player.modDataForSerialization.ContainsKey(BadLuckProtection))
                {
                    player.modDataForSerialization[BadLuckProtection] = ZeroBadLuckProtection.ToString();
                }
                else
                {
                    //We set the modData value to what we want it to be
                    player.modDataForSerialization.TryAdd(BadLuckProtection, ZeroBadLuckProtection.ToString());
                }
            }
            //If they don't get a drop, we add the bad luck protection to it.
            else
            {
                Dictionary<string, decimal> badLuckValues = new()
                {
                    { "level_0", 0.01m },
                    { "level_1", 0.02m },
                    { "level_2", 0.03m },
                    { "level_3", 0.04m },
                    { "level_4", 0.05m }
                };
                //Check to see if they have the mod Data already and get that value
                if (player.modDataForSerialization.TryGetValue(BadLuckProtection, out string storedBadLuckString))
                {
                    //Change the value into an int
                    decimal storedBadLuckValue = decimal.Parse(storedBadLuckString);

                    storedBadLuckValue += badLuckValues[theftLevel];

                    player.modDataForSerialization[BadLuckProtection] = storedBadLuckValue.ToString();
                }
                //If there is no modData value for our key...
                else
                {
                    //We set the modData value to what we want it to be
                    player.modDataForSerialization.TryAdd(BadLuckProtection, badLuckValues[theftLevel].ToString());
                }
            }

            if (ModEntry.Config.ShortCoolDown == true)
            {
                // Set cooldown to 1 so the player can't spam the skill
                SetCooldown(player, 1);
            }
            else
            {
                // Set cooldown based on theft level
                int cooldown = 5 - Array.IndexOf(theftThresholds.Keys.ToArray(), theftLevel);
                SetCooldown(player, cooldown + cooldown);
            }

            if (player.modDataForSerialization.TryGetValue(Cooldown, out string value))
            {
                Log.Trace("Thieving skill cooldown is now set to: " + value);
            }
        }

        // Method to generate a list of items from villager's likes, neutrals, and loves
        public static List<string> GetItemList(NPC npc, Farmer player)
        {
            List<string> lootList = [];

            if (!player.HasCustomProfession(Proffession10b1))
            {
                if (Utilities.GetLevel(player) >= 3)
                {
                    AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Neutral"]);
                }
                if (Utilities.GetLevel(player) >= 7)
                {
                    AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Like"]);
                }
            }
            if (player.HasCustomProfession(Proffession10b1))
            {
                AddLootFromList(lootList, Game1.NPCGiftTastes["Universal_Love"]);
            }

            Game1.NPCGiftTastes.TryGetValue(npc.Name, out string value2);
            string[] array5 = value2.Split('/');
            List<string[]> npcTastesList = new List<string[]>();
            for (int i = 0; i < 10; i += 2)
            {
                string[] array6 = ArgUtility.SplitBySpace(array5[i + 1]);
                string[] array7 = new string[array6.Length];
                for (int j = 0; j < array6.Length; j++)
                {
                    if (array6[j].Length > 0)
                    {
                        array7[j] = array6[j];
                    }
                }

                npcTastesList.Add(array7);
            }

            // Assume list2 is defined somewhere and populated properly as per earlier description.
            if (player.HasCustomProfession(Proffession10b1))
            {
                // Player has the loot profession, only add loved items
                lootList.AddRange(npcTastesList[0].Where(item => !lootList.Contains(item)));
            }
            else
            {
                // Player does not have the loot profession, add Loved, Like, and Neutral items
                for (int i = 0; i < 5; i++)
                {
                    if (i == 0 || i == 1 || i == 4) // Assuming indices 0, 1, 4 are Loved, Like, Neutral
                    {
                        foreach (string item in npcTastesList[i])
                        {
                            if (!lootList.Contains(item))
                            {
                                lootList.Add(item);
                            }
                        }
                    }
                    else
                    {
                        // Remove items not Loved, Liked, or Neutral (if they were mistakenly added)
                        foreach (string item in npcTastesList[i])
                        {
                            lootList.Remove(item);
                        }
                    }
                }
            }

            return lootList.Distinct().ToList();
        }

        // Method to add items to loot list from a given list of items
        private static void AddLootFromList(List<string> lootList, string itemList)
        {
            lootList.AddRange(ArgUtility.SplitBySpace(itemList));
            lootList.Shuffle(Game1.random);
        }

        // Method to check if an item is valid loot for the player's current Thieving level
        private static bool IsValidLootItemForLevel(string item, Farmer player)
        {
            if (string.IsNullOrEmpty(item) || item.StartsWith('-'))
                return false;

            var itemData = ItemRegistry.GetData(item);
            if (itemData == null)
                return false;

            if (!Game1.objectData.TryGetValue(itemData.ItemId, out var objectData))
                return false;

            int playerLevel = Utilities.GetLevel(player);
            int maxAllowedPrice = (playerLevel + 1) * 175;

            return objectData.Price <= maxAllowedPrice;
        }

        // Method to calculate if the player is facing the NPC's front, sides, or back when stealing
        private static string GetFacingSide(NPC npc, Farmer player)
        {
            int npcDirection = npc.FacingDirection;
            int playerDirection = player.FacingDirection;
            bool isSide = (playerDirection == 0 && npcDirection == 1) ||
                          (playerDirection == 0 && npcDirection == 3) ||
                          (playerDirection == 1 && npcDirection == 0) ||
                          (playerDirection == 1 && npcDirection == 2) ||
                          (playerDirection == 2 && npcDirection == 1) ||
                          (playerDirection == 2 && npcDirection == 3) ||
                          (playerDirection == 3 && npcDirection == 0) ||
                          (playerDirection == 3 && npcDirection == 2);
            bool isFront = (playerDirection == 0 && npcDirection == 2) ||
                          (playerDirection == 1 && npcDirection == 3) ||
                          (playerDirection == 2 && npcDirection == 0) ||
                          (playerDirection == 3 && npcDirection == 1);
            if (isSide) return "side";
            if (isFront) return "front";
            return "back";
        }

        // Method to calculate the change of friendship based on the direction the NPC is facing
        private static int CalculateFriendshipChangeDirectional(string facingSide, bool friendshipProtection)
        {
            int friendshipLost = 0;
            if (facingSide == "front") friendshipLost += friendshipProtection ? 0 : -12;
            if (facingSide == "side") friendshipLost += friendshipProtection ? 0 : -6;
            if (facingSide == "back") friendshipLost += friendshipProtection ? 0 : -3; ///the player feels guilty about it so they still lose friendship even if hidden from the NPC
            return friendshipLost;
        }

        // Method to calculate the change of friendship based the theft level
        private static int CalculateFriendshipTheftLevel(int friendshipLost, string theftLevel, bool friendshipProtection)
        {
            if (theftLevel == "level_0") friendshipLost += friendshipProtection ? 0 : -16;
            if (theftLevel == "level_1") friendshipLost += friendshipProtection ? 0 : -8;
            if (theftLevel == "level_2") friendshipLost += friendshipProtection ? 0 : -4;
            if (theftLevel == "level_3") friendshipLost += friendshipProtection ? 0 : -2;
            return friendshipLost;
        }

        public static int CalculateFriendshipNightTimeAdjustment(int value, Farmer who)
        {
            // Does the location count as an outdoor location?
            if (who.currentLocation.IsOutdoors)
            {
                // Is it dark outside?
                if (Game1.isDarkOut(who.currentLocation))
                {
                    // If yes, add 5 to the value
                    value += 5;
                }
                else
                {
                    // If not, subtract 5 to the value
                    value += -5;
                }
            }
            return value;
        }

        // Method to calculate addition or subtraction to the player's thieving roll based on what side of the NPC they are stealing from
        private static decimal CalculateThievingDirectionChange(decimal value, string facingSide, bool thieving5a)
        {
            decimal newValue = 0;
            if (facingSide == "side") newValue += thieving5a ? 0 : 0;
            if (facingSide == "front") newValue += thieving5a ? 0 : -0.1m;
            if (facingSide == "back") newValue += thieving5a ? 0 : 0.1m;
            return value += newValue;
        }

        public static decimal CalculateThievingNightTimeAdjustment(decimal value, Farmer who)
        {
            decimal newValue = 0;
            // Does the location count as an outdoor location?
            if (who.currentLocation.IsOutdoors)
            {
                // Is it dark outside?
                if (Game1.isDarkOut(who.currentLocation))
                {
                    // If yes, add 5 to the value
                    newValue += 0.05m;
                }
                else
                {
                    // If not, subtract 5 to the value
                    newValue -= 0.05m;
                }
            }
            return value += newValue;
        }

        public static decimal CalculateThievingWeatherAdjustment(decimal value, Farmer who)
        {
            decimal newValue = 0;
            // Does the location count as an outdoor location?
            if (who.currentLocation.IsRainingHere() || who.currentLocation.IsGreenRainingHere())
            {
                newValue += 0.05m;
            }
            return value += newValue;
        }

        private static decimal CalculateThievingPlayerLevel(decimal value, Farmer who)
        {
            // Higher Thieving level improves the player's stealing roll
            int playerLevel = Utilities.GetLevel(who);
            Log.Trace($"Player level is: {playerLevel}");
            decimal newValue = (decimal)((playerLevel + playerLevel) * 0.01);
            Log.Trace($"Player level bonus is: {newValue}");
            return value += newValue;
        }

        private static decimal CalculateThievingLuckLevel(decimal value, Farmer who)
        {
            // Player luck improves the player's stealing roll
            decimal newValue = (decimal)(who.DailyLuck + (who.LuckLevel * 0.01));
            return value += newValue;
        }

        private static decimal CalculateBadLuckProtection(decimal value, Farmer who)
        {
            decimal newValue = 0;
            if (who.modDataForSerialization.TryGetValue(BadLuckProtection, out string storedBadLuckString))
            {
                newValue = decimal.Parse(storedBadLuckString);
            }
            return value += newValue;
        }

        private static decimal CalculateThievingProfessionBonus(decimal value, Farmer who)
        {
            decimal newValue = 0;
            if (who.HasCustomProfession(Proffession10b2))
            {
                newValue += 0.1m;
            }
            return value += newValue;
        }

        // Method to calculate the theft level
        private static string GetTheftLevel(decimal theftLevel)
        {
            if (theftLevel <= 28) return "level_0";
            if (theftLevel <= 56) return "level_1";
            if (theftLevel <= 84) return "level_2";
            if (theftLevel <= 112) return "level_3";
            return "level_4";
        }

        // Method to figure out how much exp the player should get from stealing based on how "well" they did
        private static int CalculateExpTheftLevel(string theftLevel)
        {
            float exp = 0;
            if (theftLevel == "level_0") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpFromFail;
            if (theftLevel == "level_1") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel1;
            if (theftLevel == "level_2") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel2;
            if (theftLevel == "level_3") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel3;
            if (theftLevel == "level_4") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel4;
            return (int)Math.Floor(exp);
        }

        // Method to set the cooldown timer for Stealing
        private static void SetCooldown(Farmer who, int cooldown)
        {
            //Make sure the player is not null and they are the local player
            if (who != null && who.IsLocalPlayer)
            {
                //Set the variables up
                //Make sure to get the unique multiplayer to make sure we are setting the cooldown of the right player
                const string stealingMessageKey = "moonslime.Thieving.Cooldown.apply";
                string coolDownAsString = cooldown.ToString();
                Farmer player = Game1.GetPlayer(who.UniqueMultiplayerID);

                //Check to see if they have the mod Data already and get that value
                if (player.modDataForSerialization.TryGetValue(Cooldown, out string storedCooldownString))
                {
                    //Change the value into an int
                    int storedCooldown = int.Parse(storedCooldownString);
                    //If the value is 0, we are going to be setting the cooldown, so send the player a message about it
                    if (storedCooldown == 0)
                    {
                        string messageKey = stealingMessageKey;
                        Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(messageKey)));
                    }
                    //If the cooldown we want to set does not equal the stored cooldown...
                    if (cooldown != storedCooldown)
                    {
                        //.... we set the modData to the cooldown we want to set
                        player.modDataForSerialization[Cooldown] = coolDownAsString;
                    }
                }
                //If there is no modData value for our key...
                else
                {
                    //We set the modData value to what we want the cooldown to be
                    player.modDataForSerialization.TryAdd(Cooldown, coolDownAsString);
                    //And we send the player a message saying Thieving is now on cooldown
                    string messageKey = stealingMessageKey;
                    Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(messageKey)));
                }
            }
        }
    }
}
