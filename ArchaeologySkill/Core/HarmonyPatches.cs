using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
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


    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkForBuriedItem))]
    class CheckForBuriedItem_Base_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        GameLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            BirbCore.Attributes.Log.Trace("Archaeology skill check for buried treasure, general");
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);

            Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation * 77, Game1.stats.DirtHoed);
            string text = ModEntry.Instance.Helper.Reflection.GetMethod(__instance, "HandleTreasureTileProperty").Invoke<string>(xLocation, yLocation, detectOnly);
            if (text != null)
            {
                __result = text;
                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation);
                return false;
            }

            bool flag = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
            float num = 0.5f;
            if (!__instance.IsFarm && (bool)__instance.IsOutdoors && __instance.GetSeason() == Season.Winter && random.NextDouble() < 0.08 && !explosion && !detectOnly && !(__instance is Desert))
            {
                string item = random.Choose("(O)412", "(O)416");
                Game1.createObjectDebris(item, xLocation, yLocation);
                if (flag && random.NextDouble() < (double)num)
                {
                    Game1.createObjectDebris(random.Choose("(O)412", "(O)416"), xLocation, yLocation);
                }

                __result = "";
                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
                return false;
            }

            LocationData data = __instance.GetData();
            if ((bool)__instance.IsOutdoors && random.NextBool(data?.ChanceForClay ?? 0.03) && !explosion)
            {
                if (detectOnly)
                {
                    __instance.map.RequireLayer("Back").Tiles[xLocation, yLocation].Properties.Add("Treasure", "Item (O)330");
                    __result = "Item";
                    return false;
                }

                Game1.createObjectDebris("(O)330", xLocation, yLocation);
                if (flag && random.NextDouble() < (double)num)
                {
                    Game1.createObjectDebris("(O)330", xLocation, yLocation);
                }

                __result = "";
                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: "(O)330");
                return false;
            }
            __result = "";
            return false;
        }

    }

    [HarmonyPatch(typeof(IslandLocation), nameof(IslandLocation.checkForBuriedItem))]
    class CheckForBuriedItem_IslandLocation_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static void Prefix(
        IslandLocation __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            BirbCore.Attributes.Log.Trace("Archaeology skill: check for buried treasure: Island");
            BirbCore.Attributes.Log.Trace(__instance.IsBuriedNutLocation(new Point(xLocation, yLocation)).ToString());
            if (who != null && __instance.IsBuriedNutLocation(new Point(xLocation, yLocation)))
            {
                BirbCore.Attributes.Log.Trace("Has the team collected said nut?");
                BirbCore.Attributes.Log.Trace(Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation).ToString());
                if (Game1.player.team.collectedNutTracker.Contains("Buried_" + __instance.Name + "_" + xLocation + "_" + yLocation) == false)
                {
                    BirbCore.Attributes.Log.Trace("The Team has not collected said not, award the player bonus exp!");
                    Utilities.AddEXP(Game1.GetPlayer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromArtifactSpots);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.checkForBuriedItem))]
    class CheckForBuriedItem_Mineshaft_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        MineShaft __instance, string __result, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
        {
            if (__instance.isQuarryArea)
            {
                __result = "";
                return false;
            }

            if (Game1.random.NextDouble() < 0.15)
            {
                string id = "(O)330";
                if (Game1.random.NextDouble() < 0.07)
                {
                    if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (Game1.random.Next(5))
                        {
                            case 0:
                                id = "(O)96";
                                break;
                            case 1:
                                id = ((!who.hasOrWillReceiveMail("lostBookFound")) ? "(O)770" : ((Game1.netWorldState.Value.LostBooksFound < 21) ? "(O)102" : "(O)770"));
                                break;
                            case 2:
                                id = "(O)110";
                                break;
                            case 3:
                                id = "(O)112";
                                break;
                            case 4:
                                id = "(O)585";
                                break;
                        }
                    }
                    else if (Game1.random.NextDouble() < 0.75)
                    {
                        switch (__instance.getMineArea())
                        {
                            case 0:
                            case 10:
                                id = Game1.random.Choose("(O)121", "(O)97");
                                break;
                            case 40:
                                id = Game1.random.Choose("(O)122", "(O)336");
                                break;
                            case 80:
                                id = "(O)99";
                                break;
                        }
                    }
                    else
                    {
                        id = Game1.random.Choose("(O)126", "(O)127");
                    }
                }
                else if (Game1.random.NextDouble() < 0.19)
                {
                    id = (Game1.random.NextBool() ? "(O)390" : __instance.getOreIdForLevel(__instance.mineLevel, Game1.random));
                }
                else if (Game1.random.NextDouble() < 0.45)
                {
                    id = "(O)330";
                }
                else if (Game1.random.NextDouble() < 0.12)
                {
                    if (Game1.random.NextDouble() < 0.25)
                    {
                        id = "(O)749";
                    }
                    else
                    {
                        switch (__instance.getMineArea())
                        {
                            case 0:
                            case 10:
                                id = "(O)535";
                                break;
                            case 40:
                                id = "(O)536";
                                break;
                            case 80:
                                id = "(O)537";
                                break;
                        }
                    }
                }
                else
                {
                    id = "(O)78";
                }

                Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                bool num = who?.CurrentTool is Hoe && who.CurrentTool.hasEnchantmentOfType<GenerousEnchantment>();
                float num2 = 0.25f;
                /// Custom code
                Utilities.ApplyArchaeologySkill(Game1.GetPlayer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromMinesDigging, false, xLocation, yLocation);
                ///
                if (num && Game1.random.NextDouble() < (double)num2)
                {
                    Game1.createObjectDebris(id, xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                }

                __result = "";
                return false;
            }

            __result = "";
            return false;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.digUpArtifactSpot))]
    class DigUpArtifactSpot_Patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void After_Profession_Extra_Loot(
        GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            if (who != null)
            {
                var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
                string item = ModEntry.ArtifactLootTable.RandomChoose(Game1.random, "390");
                xLocation = farmer.TilePoint.X;
                yLocation = farmer.TilePoint.Y;
                Utilities.ApplyArchaeologySkill(farmer, ModEntry.Config.ExperienceFromArtifactSpots, false, xLocation, yLocation, exactItem: item);
                //Does The player have the Antiquarian Profession?
                BirbCore.Attributes.Log.Trace("Archaeology skill: Checking to see if the player has Antiquarian");
                if (Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10a1))
                {

                    BirbCore.Attributes.Log.Trace("Archaeology skill: Player has Antiquarian");
                    Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);
                    Vector2 vector = new Vector2(xLocation * 64, yLocation * 64);
                    item = ModEntry.ArtifactLootTable.RandomChoose(Game1.random, "390");
                    Item finalItem = ItemRegistry.Create(item);
                    Game1.createItemDebris(finalItem, farmer.Tile, Game1.random.Next(4), __instance);
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
                BirbCore.Attributes.Log.Trace("Archaeology skill: Dowser skill");
                //Get a random Number
                Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);

                if (random.NextDouble() < Utilities.GetLevel(farmer))
                {
                    BirbCore.Attributes.Log.Trace("Archaeology skill: Dowser skill artifact roll won");
                    //Find a random artifact to add from the artifact loot table
                    string artifact = ModEntry.ArtifactLootTable.RandomChoose(random, "390");
                    __result.Add(new StardewValley.Object(artifact, 1));
                }

                BirbCore.Attributes.Log.Trace("Archaeology skill: Dowser skill adding additional loot to panning");
                random = new Random(xLocation * (int)who.DailyLuck * 2000 + yLocation + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
                string item = ModEntry.BonusLootTable.RandomChoose(random, "390");
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
                        extraPanningPointChance += 2;
                    }
                }
                if (Game1.MasterPlayer.mailReceived.Contains("ccFishTank") && !(__instance is Beach) && __instance.orePanPoint.Value.Equals(Point.Zero) && r.NextBool())
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
#pragma warning disable IDE0051 // Remove unused private members
        private static void IncereaseCosts(
#pragma warning restore IDE0051 // Remove unused private members
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
                BirbCore.Attributes.Log.Error($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            }
            //Take the result, and then multiply it by the sales multiplier, along with the config to control display pricing
            __result *= saleMultiplier;
        }
    }

    [HarmonyPatch(typeof(VolcanoDungeon), nameof(VolcanoDungeon.drawAboveAlwaysFrontLayer))]
    class VolcanoDungeonLevel_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(
        StardewValley.Locations.VolcanoDungeon __instance, SpriteBatch b)
        {
            if (__instance.level?.Get() > 10)
            {
                Color color_Red = SpriteText.color_Red;
                string s = (__instance.level?.Get() - 10).Value.ToString() ?? "";
                Microsoft.Xna.Framework.Rectangle titleSafeArea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
                SpriteText.drawString(b, s, titleSafeArea.Left + 16, titleSafeArea.Top + 16, 999999, -1, 999999, 1f, 1f, junimoText: false, 2, "", color_Red);
                return false; // don't run original code
            }

            return true; // run original code
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



    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction))]
    class VolcanoWarpTotem_patch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Moonslime_Volcano_Warp(
        StardewValley.Object __instance, ref bool __result, GameLocation location)
        {
            if (__instance.HasContextTag("moonslime_volcano_warp"))
            {
                var farmer = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
                if (farmer == null) { return; }

                farmer.jitterStrength = 1f;
                Color glowColor = Color.Red;
                location.playSound("warrior");
                farmer.faceDirection(2);
                farmer.CanMove = false;
                farmer.temporarilyInvincible = true;
                farmer.temporaryInvincibilityTimer = -4000;
                Game1.changeMusicTrack("silence");
                farmer.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
                {
                                new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
                                new FarmerSprite.AnimationFrame((short)farmer.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, Volcano_totemWarp, behaviorAtEndOfFrame: true)
                });

                TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
                {
                    motion = new Vector2(0f, -1f),
                    scaleChange = 0.01f,
                    alpha = 1f,
                    alphaFade = 0.0075f,
                    shakeIntensity = 1f,
                    initialPosition = Game1.player.Position + new Vector2(0f, -96f),
                    xPeriodic = true,
                    xPeriodicLoopTime = 1000f,
                    xPeriodicRange = 4f,
                    layerDepth = 1f
                };
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.ItemId);
                Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
                temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(-64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
                {
                    motion = new Vector2(0f, -0.5f),
                    scaleChange = 0.005f,
                    scale = 0.5f,
                    alpha = 1f,
                    alphaFade = 0.0075f,
                    shakeIntensity = 1f,
                    delayBeforeAnimationStart = 10,
                    initialPosition = Game1.player.Position + new Vector2(-64f, -96f),
                    xPeriodic = true,
                    xPeriodicLoopTime = 1000f,
                    xPeriodicRange = 4f,
                    layerDepth = 0.9999f
                };
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.ItemId);
                Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
                temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
                {
                    motion = new Vector2(0f, -0.5f),
                    scaleChange = 0.005f,
                    scale = 0.5f,
                    alpha = 1f,
                    alphaFade = 0.0075f,
                    delayBeforeAnimationStart = 20,
                    shakeIntensity = 1f,
                    initialPosition = Game1.player.Position + new Vector2(64f, -96f),
                    xPeriodic = true,
                    xPeriodicLoopTime = 1000f,
                    xPeriodicRange = 4f,
                    layerDepth = 0.9988f
                };
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.ItemId);
                Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
                Game1.screenGlowOnce(glowColor, hold: false);
                Utility.addSprinklesToLocation(location, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
                __result = true;
            }
        }

        public static void Volcano_totemWarp(Farmer who)
        {

            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);

            GameLocation currentLocation = farmer.currentLocation;
            for (int i = 0; i < 12; i++)
            {
                Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
            }

            who.playNearbySoundAll("wand");
            Game1.displayFarmer = false;
            farmer.temporarilyInvincible = true;
            farmer.temporaryInvincibilityTimer = -2000;
            farmer.freezePause = 1000;
            Game1.flashAlpha = 1f;
            DelayedAction.fadeAfterDelay(Volcano_totemWarpForReal, 1000);
            Microsoft.Xna.Framework.Rectangle rectangle = who.GetBoundingBox();
            new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, 64, 64).Inflate(192, 192);
            int num = 0;
            Point tilePoint = who.TilePoint;
            for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--)
            {
                Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
                {
                    layerDepth = 1f,
                    delayBeforeAnimationStart = num * 25,
                    motion = new Vector2(-0.25f, 0f)
                });
                num++;
            }
        }

        private static void Volcano_totemWarpForReal()
        {
            Game1.warpFarmer("VolcanoDungeon" + 11, 0, 1, false);


            Game1.changeMusicTrack("VolcanoMines");
            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }
    }
}
