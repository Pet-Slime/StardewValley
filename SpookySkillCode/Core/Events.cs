using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

namespace SpookySkill.Core
{
    [SEvent]
    internal class Events
    {
        public static string Boo = "moonslime.Spooky.Boo";

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            BirbCore.Attributes.Log.Trace("Spooky: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Spooky_Skill());
        }

        [SEvent.ButtonReleased]
        private static void ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button != ModEntry.Config.Key_Cast || !Game1.player.IsLocalPlayer)
                return;

            Farmer player = Game1.player;
            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;
            List<NPC> npcsInRange = new List<NPC>();

            foreach(var NPC in location.characters)
            {
                //me being parinoid, make sure the NPC is a character
                if (NPC is Character &&
                    //Check to see if they are in range of the player
                    //8 tiles if they have banshee, 2 if not
                    Vector2.Distance(NPC.Tile, playerTile) <= (player.HasCustomProfession(Spooky_Skill.Spooky10a1) ? 8 : 2) &&
                    //Check to see if they are a villager
                    NPC.IsVillager &&
                    //Check to see if the player has talked to them
                    player.hasPlayerTalkedToNPC(NPC.Name) &&
                    //Check to see if they are giftable
                    NPC.CanReceiveGifts() &&
                    //Check to make sure the player has not given them two gifts this week
                    player.friendshipData[NPC.Name].GiftsThisWeek < 2 &&
                    //Check to make sure the player has not given them a gift today
                    player.friendshipData[NPC.Name].GiftsToday < 1 &&
                    //Make sure the player can emote
                    player.CanEmote() &&
                    //And last, I don't want to give the elderly a heart attack, so leaving Evelyn and George out of this
                    //Sorry Cross-mod Elderfolk
                    NPC.Name != "Evelyn" && NPC.Name != "George")
                {
                    npcsInRange.Add(NPC);
                }
            }

            if (npcsInRange.Count == 0)
            {
                player.performPlayerEmote("sad");
                return;
            }

            if (player.HasCustomProfession(Spooky_Skill.Spooky10a1))
            {
                foreach (var npc in npcsInRange)
                {
                    SPOOKY(npc, player);
                }
            }
            else
            {
                SPOOKY(npcsInRange[0], player);
            }
        }

        public static void SPOOKY(NPC npc, Farmer player)
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
            spookyRoll = CalculateSpookyDirectionChange(spookyRoll, facingSide, player.HasCustomProfession(Spooky_Skill.Spooky5a));
            ///Fall adjustment for the spooky roll
            spookyRoll = FallAdjustment(spookyRoll);
            ///Add 10 if the player has the ghoul profession
            if (player.HasCustomProfession(Spooky_Skill.Spooky10b2))
            {
                spookyRoll += 10;
            }
            ///Get the spook level
            string spookLevel = GetSpookLevel(spookyRoll);
            ///If the player has the zombie profession, they have a 50% chance of ignoring friendship lost
            bool zombieCharm = player.HasCustomProfession(Spooky_Skill.Spooky5b) && Game1.random.NextDouble() < 0.5;
            ///Calculate friendship lost based on facing direction
            int friendshipLost = CalculateFriendshipChangeDirectional(facingSide, zombieCharm);
            ///Adjust friendship lost based on spook level
            friendshipLost = CalculateFriendshipSpookLevel(friendshipLost, spookLevel, zombieCharm);
            ///People are moer in the mood to get scared during the fall
            friendshipLost = FallAdjustment(friendshipLost);
            ///Set the string to the current NPC's name and the spook level. So each NPC can have a custom string
            string spookString = ModEntry.Instance.I18N.Get($"moonslime.Spooky.Scared.{npc.Name}.{spookLevel}");
            if (spookString.Contains("no translation"))
            {
                ///If no translation/custom string is found, set it to the default string for the spook level
                spookString = ModEntry.Instance.I18N.Get("moonslime.Spooky.Scared.default." + spookLevel);
            }
            SpookyEffects(npc, player, friendshipLost, spookString, spookLevel, spookLevel == "level_3" || spookLevel == "level_4");
        }

        private static int FallAdjustment(int value)
        {
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
            player.currentLocation.playSound(soundID);
            if (jump) npc.jump();
            if (jump) npc.Halt();

            npc.showTextAboveHead(spookString);
            Friendship friendship = player.friendshipData[npc.Name];
            friendship.GiftsToday++;
            friendship.GiftsThisWeek++;
            friendship.LastGiftDate = new WorldDate(Game1.Date);
            friendship.Points += friendshipLost;

            CreateLoot(npc, player, spookLevel);

            if (player.HasCustomProfession(Spooky_Skill.Spooky10a2))
            {
                player.health += (int)(player.health * 1.25);
                player.stamina += (int)(player.stamina * 1.25);
            }

            Utilities.AddEXP(player, CalculateExpSpookLevel(spookLevel));
        }

        public static void CreateLoot(NPC npc, Farmer player, string spookLevel)
        {
            var list = GetItemList(npc, player);

            var finalList = new List<string>();

            foreach (string thing in list)
            {
                if (thing != null && !thing.StartsWith('-'))
                {
                    finalList.Add(thing);
                    finalList.Shuffle(Game1.random);
                }
            }

            string item = finalList[Math.Max(1, Game1.random.Next(finalList.Count))];


            int diceRoll = Game1.random.Next(100);
            int spookyRoll = (Utilities.GetLevel(player) * 2) + diceRoll;

            if (player.HasCustomProfession(Spooky_Skill.Spooky10b2))
            {
                spookyRoll += 10;
            }

            switch (spookLevel)
            {
                case "level_0":
                    if (spookyRoll > 100)
                    {
                        Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                    }
                    break;
                case "level_1":
                    if (spookyRoll > 66)
                    {
                        Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                    }
                    break;
                case "level_2":
                    if (spookyRoll > 33)
                    {
                        Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                    }
                    break;
                case "level_3":
                    if (spookyRoll > 0)
                    {
                        Game1.createObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, npc.currentLocation);
                    }
                    break;
                case "level_4":
                    int howMany = spookyRoll / 10;
                    Game1.createMultipleObjectDebris(item, npc.TilePoint.X, npc.TilePoint.Y, howMany, player.UniqueMultiplayerID, npc.currentLocation);
                    break;
            }
        }

        public static List<string> GetItemList(NPC npc, Farmer player)
        {
            List<string> list = new List<string>();

            if (!player.HasCustomProfession(Spooky_Skill.Spooky10b1))
            {
                list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Like"]));
                list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Neutral"]));
            }
            list.AddRange(ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Love"]));

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
            if (player.HasCustomProfession(Spooky_Skill.Spooky10b1))
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
            int exp = 0;
            if (spookLevel == "level_0") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpFromFail;
            if (spookLevel == "level_1") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel1;
            if (spookLevel == "level_2") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel2;
            if (spookLevel == "level_3") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel3;
            if (spookLevel == "level_4") exp += ModEntry.Config.ExpMod * ModEntry.Config.ExpLevel4;
            return (int)exp;
        }
    }
}
