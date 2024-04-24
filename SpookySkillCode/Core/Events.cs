using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using SpaceCore;
using SpaceCore.Interface;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using xTile.Dimensions;
using Log = BirbCore.Attributes.Log;

namespace SpookySkill.Core
{

    [SEvent]
    internal class Events
    {
        public static string Boo = "moonslime.Spooky.Cooldown";
        public static KeyedProfession Proffession5a => ModEntry.Config.DeScary ? Thief_Skill.Thief5a : Spooky_Skill.Spooky5a;
        public static KeyedProfession Proffession5b => ModEntry.Config.DeScary ? Thief_Skill.Thief5b : Spooky_Skill.Spooky5b;
        public static KeyedProfession Proffession10a1 => ModEntry.Config.DeScary ? Thief_Skill.Thief10a1 : Spooky_Skill.Spooky10a1;
        public static KeyedProfession Proffession10a2 => ModEntry.Config.DeScary ? Thief_Skill.Thief10a2 : Spooky_Skill.Spooky10a2;
        public static KeyedProfession Proffession10b1 => ModEntry.Config.DeScary ?  Thief_Skill.Thief10b1 : Spooky_Skill.Spooky10b1;
        public static KeyedProfession Proffession10b2 => ModEntry.Config.DeScary ? Thief_Skill.Thief10b2 : Spooky_Skill.Spooky10b2;

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            BirbCore.Attributes.Log.Trace("Spooky: Trying to Register skill.");
            if (!ModEntry.Config.DeScary)
            {
                SpaceCore.Skills.RegisterSkill(new Spooky_Skill());
            } else
            {
                SpaceCore.Skills.RegisterSkill(new Thief_Skill());
            }
        }

        [SEvent.SaveLoaded]
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {

            string Id = "moonslime.Spooky";
            int skillLevel = Game1.player.GetCustomSkillLevel(Id);
            if (skillLevel == 0)
            {
                return;
            }

            if (skillLevel >= 5 && !(Game1.player.HasCustomProfession(Proffession5a) ||
                                     Game1.player.HasCustomProfession(Proffession5b)))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(Id, 5));
            }

            if (skillLevel >= 10 && !(Game1.player.HasCustomProfession(Proffession10a1) ||
                                      Game1.player.HasCustomProfession(Proffession10a2) ||
                                      Game1.player.HasCustomProfession(Proffession10b1) ||
                                      Game1.player.HasCustomProfession(Proffession10b2)))
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

        [SEvent.TimeChanged]
        private static void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            ///Make sure this only runs on the player's client.
            if (!Game1.player.IsLocalPlayer)
                return;

            var farmer = Game1.getFarmer(Game1.player.UniqueMultiplayerID);


            farmer.modDataForSerialization.TryGetValue(Boo, out string value_1);
            int storedCooldown = 0;

            if (value_1 != null)
            {
                storedCooldown = int.Parse(value_1);
            }

            /// Check to see if the cooldown for scaring/stealing is not 0
            if (storedCooldown != 0)
            {
                SetCooldown(farmer, storedCooldown - 1);

                if (storedCooldown - 1 == 0)
                {
                    string line = ModEntry.Config.DeScary ? "moonslime.Spooky.Cooldown.Thieving.off_cooldown" : "moonslime.Spooky.Cooldown.Scaring.off_cooldown";
                    Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(line)));
                    return;
                }
                
            }
        }

        [SEvent.ButtonReleased]
        private static void ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button != ModEntry.Config.Key_Cast || !Game1.player.IsLocalPlayer || Game1.eventUp)
                return;

            Farmer player = Game1.player;
            player.modDataForSerialization.TryGetValue(Boo, out string value_1);
            int storedCooldown = 0;
            if (value_1 != null)
            {
                storedCooldown = int.Parse(value_1);
            }

            if (storedCooldown != 0)
            {
                player.performPlayerEmote("sad");
                string line = ModEntry.Config.DeScary ? "moonslime.Spooky.Cooldown.Thieving.on_cooldown" : "moonslime.Spooky.Cooldown.Scaring.on_cooldown";
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(line)));
                return;
            }

            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;
            List<NPC> npcsInRange = new List<NPC>();
            List<NPC> monstersInRange = new List<NPC>();


            foreach (var NPC in location.characters)
            {

                float Distance = Vector2.Distance(NPC.Tile, playerTile);
                float profession = (player.HasCustomProfession(Proffession10a1) ? 8 : 2);


                BirbCore.Attributes.Log.Trace("Scaring/Thieving: Button pressed, going to go through the list...");
                BirbCore.Attributes.Log.Trace("NPC name is: "+ NPC.Name);
                BirbCore.Attributes.Log.Trace("is NPC villager?: " + NPC.IsVillager.ToString());
                BirbCore.Attributes.Log.Trace("NPC Distance: "+ Distance.ToString());
                BirbCore.Attributes.Log.Trace("distance value: "+ profession.ToString());
                BirbCore.Attributes.Log.Trace("distance check: " + (Distance > profession).ToString());

                //Check to see if they are a villager
                if (NPC.IsVillager &&
                    //Check to see if they are in range of the player
                    //8 tiles if they have banshee, 2 if not
                    Distance <= profession &&
                    //Check to see if they are giftable
                    NPC.CanReceiveGifts() &&
                    //Check to make sure the player has not given them two gifts this week
                    player.friendshipData[NPC.Name].GiftsThisWeek < 2 &&
                    //Check to make sure the player has not given them a gift today
                    player.friendshipData[NPC.Name].GiftsToday < 1 &&
                    //And last, I don't want to give the elderly a heart attack, so leaving Evelyn and George out of this
                    //Sorry Cross-mod Elderfolk
                    NPC.Name != "Evelyn" && NPC.Name != "George")
                {
                    npcsInRange.Add(NPC);
                    continue;
                }

                
                if (NPC.IsMonster &&
                    NPC is Monster && //If the NPC is a monster
                    Distance <= profession*2 //and if the monster is in range
                    )
                {
                    monstersInRange.Add(NPC);
                }
            }

            if (npcsInRange.Count != 0)
            {
                if (player.HasCustomProfession(Proffession10a1))
                {
                    foreach (var npc in npcsInRange)
                    {
                        Villager_SPOOKY(npc, player);
                    }
                }
                else
                {
                    Villager_SPOOKY(npcsInRange[0], player);
                }
                return;
            }

            if (monstersInRange.Count != 0)
            {
                if (player.HasCustomProfession(Proffession10a1))
                {
                    foreach (var monster in monstersInRange)
                    {
                        Monster_SPOOKY(monster, player);
                    }
                }
                else
                {
                    Monster_SPOOKY(monstersInRange[0], player);
                }
                return;
            }



            player.performPlayerEmote("sad");


        }

        public static void Monster_SPOOKY(NPC npc, Farmer player)
        {
            if (npc is not Monster)
            {
                return;
            }

            Monster monsterNPC = (Monster)npc;


            ///Get the random dice roll from 0 to 99
            int diceRoll = Game1.random.Next(100);
            ///Get how spooky the player is.
            ///this is the player's spooky level * 2 + the dice roll.
            int spookyRoll = (Utilities.GetLevel(player) * 2) + diceRoll;
            spookyRoll = FallAdjustment(spookyRoll);
            ///Add 10 if the player has the ghoul profession
            if (player.HasCustomProfession(Proffession10b2))
            {
                spookyRoll += 10;
            }
            ///Get the spook level
            string spookLevel = GetSpookLevel(spookyRoll);

            player.performPlayerEmote("exclamation");
            string soundID = Game1.random.Choose("ghost", "explosion", "dog_bark", "thunder", "shadowpeep");
            if (ModEntry.Config.DeScary)
            {
                soundID = "wind";
            }
            spookLevel = "level_4";

            bool jump = spookLevel == "level_3" || spookLevel == "level_4";
            player.currentLocation.playSound(soundID);
            if (jump && !ModEntry.Config.DeScary)
            {
                Monster_Knockback(monsterNPC, 3f, player);
            }

            

            Monster_CreateLoot(monsterNPC, player, spookLevel);

            if (player.HasCustomProfession(Proffession10a2))
            {
                player.health += (int)(player.health * 1.25);
                player.stamina += (int)(player.stamina * 1.25);
            }

            int exp = ((int)(CalculateExpSpookLevel(spookLevel) * 0.33));

            Utilities.AddEXP(player, CalculateExpSpookLevel(spookLevel));
        }

        public static void Monster_Knockback(Monster monster, float knockBackModifier, Farmer who)
        {
            Microsoft.Xna.Framework.Rectangle boundingBox = monster.GetBoundingBox();
            who.currentLocation.damageMonster(boundingBox, 0, 0, false, knockBackModifier, 100, 0f, 1f, triggerMonsterInvincibleTimer: false,  who);
            monster.stunTime.Value += 200;

        }

        public static void Villager_SPOOKY(NPC npc, Farmer player)
        {
            ///Get the random dice roll from 0 to 99
            int diceRoll = Game1.random.Next(100);
            ///Get how spooky the player is.
            ///this is the player's spooky level * 2 + the dice roll.
            int spookyRoll = (Utilities.GetLevel(player) * 2) + diceRoll;
            ///Get the direction the player is facing vs the direction the NPC is facing.
            ///Like if the player and NPC are facing each other, is the player facing the side, or the NPC's back
            string facingSide = GetFacingSide(npc, player);
            ///Get the spook level adjustment based on if the player is facing the NPC's back, sides, or front
            spookyRoll = CalculateSpookyDirectionChange(spookyRoll, facingSide, player.HasCustomProfession(Proffession5a));
            ///Fall adjustment for the spooky roll
            spookyRoll = FallAdjustment(spookyRoll);

            ///Add 10 if the player has the ghoul profession
            if (player.HasCustomProfession(Proffession10b2))
            {
                spookyRoll += 10;
            }
            ///Get the spook level
            string spookLevel = GetSpookLevel(spookyRoll);

            

            ///If the player has the zombie profession, they have a 50% chance of ignoring friendship lost
            bool zombieCharm = player.HasCustomProfession(Proffession5b) && Game1.random.NextDouble() < 0.5;

            ///Calculate friendship lost based on facing direction
            int friendshipLost = CalculateFriendshipChangeDirectional(facingSide, zombieCharm);

            ///Adjust friendship lost based on spook level
            friendshipLost = CalculateFriendshipSpookLevel(friendshipLost, spookLevel, zombieCharm);

            ///People are moer in the mood to get scared during the fall
            friendshipLost = FallAdjustment(friendshipLost);

            ///Set the string to the current NPC's name and the spook level. So each NPC can have a custom string
            string type = ModEntry.Config.DeScary ? "Stolen" : "Scared";
            string spookString = ModEntry.Instance.I18N.Get($"moonslime.Spooky.{type}.{npc.Name}.{spookLevel}");
            if (spookString.Contains("no translation"))
            {
                ///If no translation/custom string is found, set it to the default string for the spook level
                spookString = ModEntry.Instance.I18N.Get($"moonslime.Spooky.{type}.default.{spookLevel}");
            }
            SpookyEffects(npc, player, friendshipLost, spookString, spookLevel, spookLevel == "level_3" || spookLevel == "level_4");
        }

        private static int FallAdjustment(int value)
        {
            ///Thieving doesn't get any bous if it's fall or spirit's eve.
            if (ModEntry.Config.DeScary)
            {
                return value;
            }
            ///People are more in the mood to get scared during the fall
            if (Game1.currentSeason == "fall")
            {
                value += 5;
                ///People are moer in the mood to get scared on Spirit's Eve
                if (Game1.dayOfMonth == 27)
                {
                    value += 5;
                }
            }
            return value;
        }

        private static string GetSpookLevel(int spookyLevel)
        {
            if (spookyLevel <= 24) return "level_0";
            if (spookyLevel <= 55) return "level_1";
            if (spookyLevel <= 90) return "level_2";
            if (spookyLevel <= 100) return "level_3";
            return "level_4";
        }

        private static int CalculateFriendshipChangeDirectional(string facingSide, bool zombieCharm)
        {
            int friendshipLost = 0;
            if (facingSide == "side") friendshipLost += zombieCharm ? 0 : -4;
            if (facingSide == "front") friendshipLost += zombieCharm ? 0 : -8;
            if (facingSide == "back") friendshipLost += zombieCharm ? 0 : -2; ///the player feels guilty about it so they still loose friendship even if hidden from the NPC
            return friendshipLost;
        }

        private static int CalculateFriendshipSpookLevel(int friendshipLost, string facingSide, bool zombieCharm)
        {
            if (facingSide == "level_0") friendshipLost += zombieCharm ? 0 : -8;
            if (facingSide == "level_1") friendshipLost += zombieCharm ? 0 : -4;
            if (facingSide == "level_2") friendshipLost += zombieCharm ? 0 : -2;
            return friendshipLost;
        }

        private static int CalculateSpookyDirectionChange(int spookyLevel, string facingSide, bool spooky5a)
        {

            if (facingSide == "side") spookyLevel += spooky5a ? 0 : 0;
            if (facingSide == "front") spookyLevel += spooky5a ? 0 : -10;
            if (facingSide == "back") spookyLevel += spooky5a ? 0 : +10;
            return spookyLevel;
        }

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
            bool isBack = (playerDirection == 0 && npcDirection == 2) ||
                          (playerDirection == 1 && npcDirection == 3) ||
                          (playerDirection == 2 && npcDirection == 0) ||
                          (playerDirection == 3 && npcDirection == 1);
            if (isSide) return "side";
            if (isBack) return "back";
            return "front";
        }

        public static void SpookyEffects(NPC npc, Farmer player, int friendshipLost, string spookString, string spookLevel, bool jump = false)
        {
            player.performPlayerEmote("exclamation");
            string soundID = Game1.random.Choose("ghost", "explosion", "dog_bark", "thunder", "shadowpeep");
            if (ModEntry.Config.DeScary)
            {
                soundID = "wind";
            }
            player.currentLocation.playSound(soundID);
            if (jump) npc.Halt();
            if (jump) npc.jump();

            npc.showTextAboveHead(spookString);
            Friendship friendship = player.friendshipData[npc.Name];
            friendship.GiftsToday++;
            friendship.GiftsThisWeek++;
            friendship.LastGiftDate = new WorldDate(Game1.Date);
            friendship.Points += friendshipLost;

            CreateLoot(npc, player, spookLevel);

            if (player.HasCustomProfession(Proffession10a2))
            {
                player.health += (int)(player.health * 1.25);
                player.stamina += (int)(player.stamina * 1.25);
            }

            Utilities.AddEXP(player, CalculateExpSpookLevel(spookLevel));
        }

        public static void Monster_CreateLoot(Monster npc, Farmer player, string spookLevel)
        {
            var lootList = new List<string>();

            // Define thresholds for different spook levels
            Dictionary<string, int> spookThresholds = new Dictionary<string, int>
            {
                { "level_0", 100 },
                { "level_1", 66 },
                { "level_2", 33 },
                { "level_3", 0 },
                { "level_4", 0 } 
            };

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
            var validItems = lootList.Where(item => !string.IsNullOrEmpty(item) && !item.StartsWith('-') && ItemRegistry.GetData(item) != null).ToList();
            lootList.Clear();
            string selectedItem = validItems.RandomChoose(Game1.random, "766");


            // Log the selected item
            Log.Warn("Attempting to steal: " + ItemRegistry.GetData(selectedItem).DisplayName);

            // Calculate spooky roll
            int spookyRoll = playerLevel + playerLevel + Game1.random.Next(100);

            // Apply profession bonus
            if (player.HasCustomProfession(Proffession10b2))
            {
                spookyRoll += 10;
            }

            // Determine loot drop based on spook level
            if (spookyRoll > spookThresholds[spookLevel])
            {
                if (spookLevel == "level_4")
                {
                    int howMany = spookyRoll / 10;
                    Game1.createMultipleObjectDebris(selectedItem, npc.TilePoint.X, npc.TilePoint.Y, howMany, player.UniqueMultiplayerID, npc.currentLocation);
                }
                else
                {
                    Game1.createObjectDebris(selectedItem, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                }

                // Set cooldown based on spook level
                SetCooldown(player, 5 - Array.IndexOf(spookThresholds.Keys.ToArray(), spookLevel));
            }

            // Display HUD message based on game configuration
            string line = ModEntry.Config.DeScary ? "moonslime.Spooky.Cooldown.Thieving.apply" : "moonslime.Spooky.Cooldown.Scaring.apply";
            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(line)));
        }

        // Method to add items to loot list from a given list of items
        private static void AddLootFromList(List<string> lootList, string itemList)
        {
            lootList.AddRange(ArgUtility.SplitBySpace(itemList));
            lootList.Shuffle(Game1.random);
        }

        public static void CreateLoot(NPC npc, Farmer player, string spookLevel)
        {
            var lootList = GetItemList(npc, player);
            var finalList = new List<string>();

            foreach (string lootItem in lootList)
            {
                if (lootItem != null && !lootItem.StartsWith('-') && ItemRegistry.GetData(lootItem) != null)
                {
                    finalList.Add(lootItem);
                }
            }
            lootList.Clear();

            finalList.Shuffle(Game1.random);

            // Add Mayor Lewis' shorts to the list if the NPC is Lewis or Marnie
            if (npc.Name == "Lewis" || npc.Name == "Marnie")
            {
                finalList.Add("789");
            }

            // Select a random item from the final list
            string item = finalList.Count > 0 ? finalList[Game1.random.Next(finalList.Count)] : null;

            int diceRoll = Game1.random.Next(100);
            int spookyRoll = (Utilities.GetLevel(player) * 2) + diceRoll;

            if (player.HasCustomProfession(Proffession10b2))
            {
                spookyRoll += 10;
            }

            // Determine the cooldown duration based on spook level
            int cooldownDuration = 5 - (spookLevel switch
            {
                "level_1" => 1,
                "level_2" => 2,
                "level_3" => 3,
                "level_4" => 4,
                _ => 0 // Default case for "level_0" or unrecognized spook levels
            });

            // Create object debris based on spooky roll
            if (spookyRoll > 100 - (33 * cooldownDuration))
            {
                if (spookLevel == "level_4")
                {
                    int howMany = spookyRoll / 10;
                    Game1.createMultipleObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, howMany, player.UniqueMultiplayerID, npc.currentLocation);
                }
                else
                {
                    Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                }
            }

            SetCooldown(player, cooldownDuration);

            string message = ModEntry.Config.DeScary ? "moonslime.Spooky.Cooldown.Thieving.apply" : "moonslime.Spooky.Cooldown.Scaring.apply";
            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(ModEntry.Instance.I18N.Get(message)));

            if (player.modDataForSerialization.TryGetValue(Boo, out string value))
            {
                Log.Warn("Spooky skill cooldown is now set to: " + value);
            }
        }

        public static List<string> GetItemList(NPC npc, Farmer player)
        {
            List<string> list = new List<string>();

            if (!player.HasCustomProfession(Proffession10b1))
            {
                if (Utilities.GetLevel(player) >= 3)
                {
                    list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Neutral"]));
                }
                if (Utilities.GetLevel(player) >= 7)
                {
                    list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Like"]));
                }
            }
            if (Game1.year != 1 || player.HasCustomProfession(Proffession10b1))
            {
                list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Love"]));
            }

            Game1.NPCGiftTastes.TryGetValue(npc.Name, out string value2);
            string[] array5 = value2.Split('/');
            List<string[]> list2 = new List<string[]>();
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

                list2.Add(array7);
            }


            // Assume list2 is defined somewhere and populated properly as per earlier description.
            if (player.HasCustomProfession(Proffession10b1))
            {
                // Player has vampire profession, only add loved items
                list.AddRange(list2[0].Where(item => !list.Contains(item)));
            }
            else
            {
                // Player does not have vampire profession, add Loved, Like, and Neutral items
                for (int i = 0; i < 5; i++)
                {
                    if (i == 0 || i == 1 || i == 4) // Assuming indices 0, 1, 4 are Loved, Like, Neutral
                    {
                        foreach (string item in list2[i])
                        {
                            if (!list.Contains(item))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else
                    {
                        // Remove items not Loved, Liked, or Neutral (if they were mistakenly added)
                        foreach (string item in list2[i])
                        {
                            list.Remove(item);
                        }
                    }
                }
            }

            return list.Distinct().ToList();
        }


        private static int CalculateExpSpookLevel(string spookLevel)
        {
            float exp = 0;
            if (spookLevel == "level_0") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpFromFail;
            if (spookLevel == "level_1") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel1;
            if (spookLevel == "level_2") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel2;
            if (spookLevel == "level_3") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel3;
            if (spookLevel == "level_4") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel4;
            return (int)Math.Floor(exp);
        }



        private static void SetCooldown(Farmer who, int cooldown)
        {
            Farmer farmer = Game1.getFarmer(who.UniqueMultiplayerID);
            if (farmer != null && farmer.IsLocalPlayer)
            {
                if (!farmer.modDataForSerialization.ContainsKey(Boo))
                {
                    farmer.modDataForSerialization.TryAdd(Boo, cooldown.ToString());
                }
                else
                {
                    farmer.modDataForSerialization.TryGetValue(Boo, out string value_1);
                    int storedCooldown = int.Parse(value_1);

                    int currentCooldown = cooldown;

                    if (currentCooldown != storedCooldown)
                    {
                        farmer.modDataForSerialization.Remove(Boo);
                        farmer.modDataForSerialization.TryAdd(Boo, currentCooldown.ToString());
                    }
                }
            }
        }
    }
}
