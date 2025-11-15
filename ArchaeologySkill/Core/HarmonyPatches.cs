using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Tools;

namespace ArchaeologySkill.Core
{

    [HarmonyPatch(typeof(StardewValley.Object), "_PopulateContextTags")]
    class PopulateContextTags_patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix(StardewValley.Object __instance, ref HashSet<string> tags)
        {
            if (__instance.Type == "Arch")
            {
                tags.Add("type_Arch");
            }
        }
    }

    //Legacy code, kept for future refrence in case I need to swtich back

    //    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkForBuriedItem))]
    //    class CheckForBuriedItem_Base_patch
    //    {
    //        [HarmonyLib.HarmonyPrefix]
    //        private static bool Prefix(
    //        GameLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
    //        {
    //            BirbCore.Attributes.Log.Trace("Archaeology skill check for buried treasure, general");
    //            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
    //
    //            Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation * 77, Game1.stats.DirtHoed);
    //            string text = ModEntry.Instance.Helper.Reflection.GetMethod(__instance, "HandleTreasureTileProperty").Invoke<string>(xLocation, yLocation, detectOnly);
    //            if (text != null)
    //            {
    //                __result = text;
    //                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation);
    //                return false;
    //            }
    //
    //            bool flag = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
    //            float num = 0.5f;
    //            if (!__instance.IsFarm && (bool)__instance.IsOutdoors && __instance.GetSeason() == Season.Winter && random.NextDouble() < 0.08 && !explosion && !detectOnly && !(__instance is Desert))
    //            {
    //                string item = random.Choose("(O)412", "(O)416");
    //                Game1.createObjectDebris(item, xLocation, yLocation);
    //                if (flag && random.NextDouble() < (double)num)
    //                {
    //                    Game1.createObjectDebris(random.Choose("(O)412", "(O)416"), xLocation, yLocation);
    //                }
    //
    //                __result = "";
    //                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
    //                return false;
    //            }
    //
    //            LocationData data = __instance.GetData();
    //            if ((bool)__instance.IsOutdoors && random.NextBool(data?.ChanceForClay ?? 0.03) && !explosion)
    //            {
    //                __result = "";
    //                return false;
    //            }
    //
    //            if (detectOnly)
    //            {
    //                __instance.map.RequireLayer("Back").Tiles[xLocation, yLocation].Properties.Add("Treasure", "Item (O)330");
    //                __result = "Item";
    //                return false;
    //            }
    //
    //            Game1.createObjectDebris("(O)330", xLocation, yLocation);
    //            if (flag && random.NextDouble() < (double)num)
    //            {
    //                Game1.createObjectDebris("(O)330", xLocation, yLocation);
    //            }
    //
    //            __result = "";
    //            Log.Alert("testers 4");
    //            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: "(O)330");
    //            return false;
    //        }
    //
    //    }

    [HarmonyPatch(typeof(IslandLocation), nameof(IslandLocation.checkForBuriedItem))]
    class CheckForBuriedItem_IslandLocation_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static void Prefix(
        IslandLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            Log.Trace("Archaeology skill: check for buried treasure: Island");
            Log.Trace(__instance.IsBuriedNutLocation(new Point(xLocation, yLocation)).ToString());
            if (who != null && __instance.IsBuriedNutLocation(new Point(xLocation, yLocation)))
            {
                Log.Trace("Has the team collected said nut?");
                Log.Trace(Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation).ToString());
                if (Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation) == false)
                {
                    Log.Trace("The Team has not collected said not, award the player bonus exp!");
                    Utilities.AddEXP(Game1.GetPlayer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromArtifactSpots);
                }
            }
        }
    }

    //Legacy code, kept for future refrence just in case

    //    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.checkForBuriedItem))]
    //    class CheckForBuriedItem_Mineshaft_patch
    //    {
    //        [HarmonyLib.HarmonyPrefix]
    //        private static bool Prefix(
    //        MineShaft __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
    //        {
    //            if (__instance.isQuarryArea)
    //            {
    //                __result = "";
    //                return false;
    //            }
    //
    //            if (Game1.random.NextDouble() < 0.15)
    //            {
    //                string id = "(O)330";
    //                if (Game1.random.NextDouble() < 0.07)
    //                {
    //                    if (Game1.random.NextDouble() < 0.75)
    //                    {
    //                        switch (Game1.random.Next(5))
    //                        {
    //                            case 0:
    //                                id = "(O)96";
    //                                break;
    //                            case 1:
    //                                id = ((!who.hasOrWillReceiveMail("lostBookFound")) ? "(O)770" : ((Game1.netWorldState.Value.LostBooksFound < 21) ? "(O)102" : "(O)770"));
    //                                break;
    //                            case 2:
    //                                id = "(O)110";
    //                                break;
    //                            case 3:
    //                                id = "(O)112";
    //                                break;
    //                            case 4:
    //                                id = "(O)585";
    //                                break;
    //                        }
    //                    }
    //                    else if (Game1.random.NextDouble() < 0.75)
    //                    {
    //                        switch (__instance.getMineArea())
    //                        {
    //                            case 0:
    //                            case 10:
    //                                id = Game1.random.Choose("(O)121", "(O)97");
    //                                break;
    //                            case 40:
    //                                id = Game1.random.Choose("(O)122", "(O)336");
    //                                break;
    //                            case 80:
    //                                id = "(O)99";
    //                                break;
    //                        }
    //                    }
    //                    else
    //                    {
    //                        id = Game1.random.Choose("(O)126", "(O)127");
    //                    }
    //                }
    //                else if (Game1.random.NextDouble() < 0.19)
    //                {
    //                    id = (Game1.random.NextBool() ? "(O)390" : __instance.getOreIdForLevel(__instance.mineLevel, Game1.random));
    //                }
    //                else if (Game1.random.NextDouble() < 0.45)
    //                {
    //                    id = "(O)330";
    //                }
    //                else if (Game1.random.NextDouble() < 0.12)
    //                {
    //                    if (Game1.random.NextDouble() < 0.25)
    //                    {
    //                        id = "(O)749";
    //                    }
    //                    else
    //                    {
    //                        switch (__instance.getMineArea())
    //                        {
    //                            case 0:
    //                            case 10:
    //                                id = "(O)535";
    //                                break;
    //                            case 40:
    //                                id = "(O)536";
    //                                break;
    //                            case 80:
    //                                id = "(O)537";
    //                                break;
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    id = "(O)78";
    //                }
    //
    //                Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
    //                bool num = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
    //                float num2 = 0.25f;
    //                /// Custom code
    //                Utilities.ApplyArchaeologySkill(Game1.GetPlayer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromMinesDigging, false, xLocation, yLocation);
    //                ///
    //                if (num && Game1.random.NextDouble() < (double)num2)
    //                {
    //                    Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
    //                }
    //
    //                __result = "";
    //                return false;
    //            }
    //
    //            __result = "";
    //            return false;
    //        }
    //    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.digUpArtifactSpot))]
    class DigUpArtifactSpot_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void After_Profession_Extra_Loot(
        GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            if (who != null)
            {
                //Get the unique player ~~Yes I am parinoid about it screwing up in multiplayer~~
                Farmer farmer = Game1.GetPlayer(who.UniqueMultiplayerID);

                //Get the player's current location
                string location = farmer.currentLocation.Name;

                //Make a new list
                var list = new List<string>();

                //Get the artifact loot table
                foreach (string thing in ModEntry.ArtifactLootTable)
                {
                    //Get the game data for each object in the artifact loot table
                    if (Game1.objectData.TryGetValue(thing, out var value))
                    {
                        //Check the keys of the artifact spot chances to see if they match the machine's location
                        if (value.ArtifactSpotChances != null && value.ArtifactSpotChances.ContainsKey(location))
                        {
                            //Finally add it to the list of possible artifacts
                            list.Add(thing);
                        }
                    }
                }
                //Choose a random item from that list. Else  default to "stone"
                string item = list.RandomChoose(Game1.random, "390");

                //Get the tile location of the player
                xLocation = farmer.TilePoint.X;
                yLocation = farmer.TilePoint.Y;

                //Send it to the generic Archaeology skill function
                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);

                //Does The player have the Antiquarian Profession?
                Log.Trace("Archaeology skill: Checking to see if the player has Antiquarian");
                if (Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10a1))
                {

                    Log.Trace("Archaeology skill: Player has Antiquarian");
                    //Make a random seed
                    Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);
                    //Choose a new item from the list
                    item = list.RandomChoose(random, "390");
                    //Change the string item into an Item item
                    Item finalItem = ItemRegistry.Create(item);
                    //Spawn the item
                    Game1.createItemDebris(finalItem, farmer.Tile, random.Next(4), __instance);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Pan), nameof(Pan.getPanItems))]
    class GetPanItems_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void After_getPanItems(
        Pan __instance, List<Item> __result, GameLocation location, Farmer who)
        {


            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            if (farmer == null) { return; };
            //Add EXP for the player Panning and check for the gold rush profession
            Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromPanSpots, panning: true);

            int xLocation = who.TilePoint.X;
            int yLocation = who.TilePoint.Y;

            //Add Artifacts to the drop list chance if they have the Trowler Profession
            if (farmer.HasCustomProfession(Archaeology_Skill.Archaeology10b1))
            {
                Log.Trace("Archaeology skill: Dowser skill");
                //Get a random Number
                Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);

                if (random.NextDouble() < Utilities.GetLevel(farmer))
                {
                    Log.Trace("Archaeology skill: Dowser skill artifact roll won");
                    var list = new List<string>();
                    //Find a random artifact to add from the artifact loot table
                    //Get the artifact loot table
                    foreach (string thing in ModEntry.ArtifactLootTable)
                    {
                        //Get the game data for each object in the artifact loot table
                        if (Game1.objectData.TryGetValue(thing, out var value))
                        {
                            //Check the keys of the artifact spot chances to see if they match the machine's location
                            if (value.ArtifactSpotChances != null && value.ArtifactSpotChances.ContainsKey(location.Name))
                            {
                                //Finally add it to the list of possible artifacts
                                list.Add(thing);
                            }
                        }
                    }
                    string artifact = list.RandomChoose(random, "390");
                    __result.Add(new StardewValley.Object(artifact, 1));
                }

                Log.Trace("Archaeology skill: Dowser skill adding additional loot to panning");
                random = new Random(xLocation * (int)who.DailyLuck * 2000 + yLocation + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
                List<string> newBonusLootTable = new List<string>(ModEntry.BonusLootTable);
                if (farmer.mailReceived.Contains("willyBoatFixed"))
                {
                    newBonusLootTable.AddRange(ModEntry.BonusLootTable_GI);
                    newBonusLootTable.Shuffle(random);
                }
                string item = newBonusLootTable.RandomChoose(random, "390");
                __result.Add(new StardewValley.Object(item, 1));
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performOrePanTenMinuteUpdate))]
    class PerformOrePanTenMinuteUpdate_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void TryToSpawnMorePanPoints(
        GameLocation __instance, bool __result, ref Random r)
        {
            if (Game1.IsMasterGame && __instance.orePanPoint.Value.Equals(Point.Zero))
            {
                int extraPanningPointChance = 0;

                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    var player = Game1.GetPlayer(farmer.UniqueMultiplayerID);
                    if (player.isActive() && player.HasCustomProfession(Archaeology_Skill.Archaeology5b))
                    {
                        extraPanningPointChance += 4;
                    }
                }
                if (Game1.MasterPlayer.mailReceived.Contains("ccFishTank") && __instance is not Beach && __instance.orePanPoint.Value.Equals(Point.Zero) && r.NextBool())
                {
                    for (int i = 0; i < extraPanningPointChance; i++)
                    {
                        Point point = new Point(r.Next(0, __instance.Map.RequireLayer("Back").LayerWidth), r.Next(0, __instance.Map.RequireLayer("Back").LayerHeight));
                        if (__instance.isOpenWater(point.X, point.Y) && FishingRod.distanceToLand(point.X, point.Y, __instance, landMustBeAdjacentToWalkableTile: true) <= 1 && __instance.getTileIndexAt(point, "Buildings") == -1)
                        {
                            if (Game1.player.currentLocation.Equals(__instance))
                            {
                                __instance.playSound("slosh");
                            }

                            __instance.orePanPoint.Value = point;
                            __result = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StardewValley.Object), "getPriceAfterMultipliers")]
    class GetPriceAfterMultipliers_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void IncereaseCosts(
        StardewValley.Object __instance, ref float __result, float startPrice, long specificPlayerID)
        {
            //Set the sale multiplier to 1
            float saleMultiplier = 1f;
            try
            {
                //For each farmer....
                foreach (var farmer in Game1.getAllFarmers())
                {
                    // If they use seperate wallets, get the seperate wallet
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
                    // Look to see if the item has the context tag
                    if (__instance.HasContextTag("moonslime_artifact"))
                    {
                        // If they have the right profession, increase the selling multipler by 1
                        if (farmer.HasCustomProfession(Archaeology_Skill.Archaeology10a2))
                        {
                            saleMultiplier += 1f;
                        }
                        // If they have the read the treasure book, increase the sale multiplier by 3
                        if (farmer.stats.Get("Book_Artifact") != 0)
                        {
                            saleMultiplier += 2f;
                        }

                        saleMultiplier *= ModEntry.Config.DisplaySellPrice;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            }
            //Take the result, and then multiply it by the sales multiplier, along with the config to control display pricing
            __result *= saleMultiplier;
        }
    }

    [HarmonyPatch(typeof(VolcanoDungeon), nameof(VolcanoDungeon.drawAboveAlwaysFrontLayer))]
    class VolcanoDungeonLevel_patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(
        StardewValley.Locations.VolcanoDungeon __instance, SpriteBatch b)
        {
            if (!Game1.game1.takingMapScreenshot && __instance.level?.Get() > 10)
            {
                Color color_Red = SpriteText.color_Red;
                string s = "∞" + (__instance.level?.Get() - 10).Value.ToString() ?? "";
                Microsoft.Xna.Framework.Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
                SpriteText.drawString(b, s, titleSafeArea.Left + 16, titleSafeArea.Top + 16, 999999, -1, 999999, 1f, 1f, junimoText: false, 2, "", color_Red);               
            }
        }
    }




    [HarmonyPatch(typeof(VolcanoDungeon), nameof(VolcanoDungeon.CreateEntrance))]
    class VolcanoDungeonCreateEntrence_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        StardewValley.Locations.VolcanoDungeon __instance)
        {
            if (__instance.level?.Get() == 11)
            {
                //Clear the warps of the current floor so they don't exist anymore.
                __instance.warps.Clear();
                __instance.ApplyToColor(new Color(255, 0, 0), delegate (int x, int y)
                {
                    if (!__instance.endPosition.HasValue)
                    {
                        __instance.endPosition = new Point(x, y);
                    }

                    if (__instance.level.Value == 9)
                    {
                        __instance.warps.Add(new Warp(x, y - 2, "Caldera", 21, 39, flipFarmer: false));
                    }
                    else
                    {
                        __instance.warps.Add(new Warp(x, y - 2, GetLevelName(__instance.level.Value + 1), x - __instance.endPosition.Value.X, 1, flipFarmer: false));
                    }
                });
                return false; // don't run original code
            }

            return true; // run original code
        }
        public static string GetLevelName(int level)
        {
            return "VolcanoDungeon" + level;
        }
    }

}
