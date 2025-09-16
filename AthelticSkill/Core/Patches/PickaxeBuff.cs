using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static BirbCore.Attributes.SMod;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SpaceCore;
using BirbCore.Attributes;

namespace AthleticSkill.Core.Patches
{
    [HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.beginUsing))]
    public class PickAxeBeginUsing_patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Pickaxe __instance, GameLocation location, int x, int y, Farmer who)
        {
            // Copied from Stardewvalley.Tool

            if (who.HasCustomProfession(Athletic_Skill.Athletic10a1) && __instance.UpgradeLevel > 0)
            {
                BirbCore.Attributes.Log.Warn($"The power of the {__instance.DisplayName} is {who.toolPower.Value}");
                who.Halt();
                __instance.Update(who.FacingDirection, 0, who);
                switch (who.FacingDirection)
                {
                    case Game1.up:
                        who.FarmerSprite.setCurrentFrame(176);
                        __instance.Update(0, 0, who);
                        break;

                    case Game1.right:
                        who.FarmerSprite.setCurrentFrame(168);
                        __instance.Update(1, 0, who);
                        break;

                    case Game1.down:
                        who.FarmerSprite.setCurrentFrame(160);
                        __instance.Update(2, 0, who);
                        break;

                    case Game1.left:
                        who.FarmerSprite.setCurrentFrame(184);
                        __instance.Update(3, 0, who);
                        break;
                }

                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.toolPowerIncrease))]
    public class FarmerToolPower_patch
    {

        private static int ToolPitchAccumulator;

        [HarmonyPrefix]
        private static bool Prefix(Farmer __instance)
        {
            // Copied from Stardewvalley.Tool
            var who = __instance;
            if (who.HasCustomProfession(Athletic_Skill.Athletic10a1) && who.CurrentTool is Pickaxe)
            {

                if (who.toolPower.Value == 0)
                {
                    ToolPitchAccumulator = 0;
                }

                who.toolPower.Value++;

                Color color = Color.White;
                int num = who.FacingDirection == 0 ? 4 : who.FacingDirection == 2 ? 2 : 0;
                switch (who.toolPower.Value)
                {
                    case 1:
                        color = Color.Orange;

                        who.jitterStrength = 0.25f;
                        break;
                    case 2:
                        color = Color.LightSteelBlue;

                        who.jitterStrength = 0.5f;
                        break;
                    case 3:
                        color = Color.Gold;
                        who.jitterStrength = 1f;
                        break;
                    case 4:
                        color = Color.Violet;
                        who.jitterStrength = 2f;
                        break;
                    case 5:
                        color = Color.BlueViolet;
                        who.jitterStrength = 3f;
                        break;
                }

                int num2 = who.FacingDirection == 1 ? 40 : who.FacingDirection == 3 ? -40 : who.FacingDirection == 2 ? 32 : 0;
                int num3 = 192;

                int y = who.StandingPixel.Y;
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(21, who.Position - new Vector2(num2, num3), color, 8, flipped: false, 70f, 0, 64, y / 10000f + 0.005f, 128));
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(192, 1152, 64, 64), 50f, 4, 0, who.Position - new Vector2(who.FacingDirection != 1 ? -64 : 0, 128f), flicker: false, who.FacingDirection == 1, y / 10000f, 0.01f, Color.White, 1f, 0f, 0f, 0f));
                int value = Utility.CreateRandom(Game1.dayOfMonth, who.Position.X * 1000.0, who.Position.Y).Next(12, 16) * 100 + who.toolPower.Value * 100;
                Game1.playSound("toolCharge", value);

                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction))]
    public class PickAxeFunction_patch
    {
        private static int BoulderTileX;
        private static int BoulderTileY;
        private static int HitsToBoulder;

        [HarmonyPrefix]
        private static bool Prefix(Pickaxe __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            if (who.HasCustomProfession(Athletic_Skill.Athletic10a1) && __instance.UpgradeLevel > 0)
            {
                ProspectorBuff(__instance, location, x, y, power, who);
                return false; // skip original logic
            }
            return true;
        }


        //Instead of just copy pasting the original block of code
        //Break it up into multiple smaller methods to make it easier to read and mantaine
        public static void ProspectorBuff(Pickaxe tool, GameLocation location, int originalX, int originalY, int power, Farmer who)
        {
            tool.lastUser = who;
            Game1.recentMultiplayerRandom = Utility.CreateRandom((short)Game1.random.Next(short.MinValue, short.MaxValue));

            // Apply stamina drain
            if (!tool.IsEfficient)
                who.Stamina -= (float)(2 * power) - (float)who.MiningLevel * 0.1f;

            power = who.toolPower.Value;
            who.stopJittering();
            Vector2 originTile = new Vector2(originalX / 64, originalY / 64);
            foreach (Vector2 tile in Utilities.TilesAffected(originTile, power, who))
            {
                //do the original pickaxe fuctions with each tile in the selected area
                ProcessTile(tool, location, tile, originalX, originalY, power, who);
            }
        }

        private static void ProcessTile(Pickaxe tool, GameLocation location, Vector2 tile, int originalX, int originalY, int power, Farmer who)
        {
            int tileX = (int)tile.X;
            int tileY = (int)tile.Y;

            if (location.performToolAction(tool, tileX, tileY))
                return;

            if (!location.Objects.TryGetValue(tile, out var obj))
            {
                HandleEmptyTile(tool, location, tile, who, originalX, originalY);
                return;
            }

            if (obj.IsBreakableStone())
                HandleStone(tool, location, tile, obj, who);
            else if (obj.Name.Contains("Boulder"))
                HandleBoulder(tool, location, tile, obj, power, who);
            else if (obj.performToolAction(tool))
                HandleGenericObject(location, tile, obj, who);
            else
                HandleWoodHit(location, tile);
        }

        private static void HandleEmptyTile(Pickaxe tool, GameLocation location, Vector2 tile, Farmer who, int originalX, int originalY)
        {
            location.playSound("woodyHit", tile);

            if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null)
            {
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(12, tile * 64f, Color.White, 8, false, 80f)
                    { alphaFade = 0.015f });
            }

            // Terrain features
            if (location.terrainFeatures.TryGetValue(tile, out var tf) && tf.performToolAction(tool, 0, tile))
                location.terrainFeatures.Remove(tile);
        }

        private static void HandleStone(Pickaxe tool, GameLocation location, Vector2 tile, StardewValley.Object obj, Farmer who)
        {
            location.playSound("hammer", tile);

            if (obj.MinutesUntilReady > 0)
            {
                int damage = Math.Max(1, tool.UpgradeLevel + 1) + tool.additionalPower.Value;
                obj.MinutesUntilReady -= damage;
                obj.shakeTimer = 200;

                if (obj.MinutesUntilReady > 0)
                {
                    Game1.createRadialDebris(Game1.currentLocation, 14, (int)tile.X, (int)tile.Y, Game1.random.Next(2, 5), false);
                    return;
                }
            }

            // Handle break effects
            SpawnStoneBreakSprites(location, tile, obj);
            location.OnStoneDestroyed(obj.ItemId, (int)tile.X, (int)tile.Y, who);

            if (who?.stats.Get("Book_Diamonds") != 0 && Game1.random.NextDouble() < 0.0066)
            {
                Game1.createObjectDebris("(O)72", (int)tile.X, (int)tile.Y, who.UniqueMultiplayerID, location);
                if (who.professions.Contains(19) && Game1.random.NextBool())
                    Game1.createObjectDebris("(O)72", (int)tile.X, (int)tile.Y, who.UniqueMultiplayerID, location);
            }

            if (obj.MinutesUntilReady <= 0)
            {
                obj.performRemoveAction();
                location.Objects.Remove(tile);
                location.playSound("stoneCrack", tile);
                Game1.stats.RocksCrushed++;
            }
        }

        //Need to rework this logic eventually so the player can instantly destroy a boulder if it's with in like 4 tiles of the charge attack
        private static void HandleBoulder(Pickaxe tool, GameLocation location, Vector2 tile, StardewValley.Object obj, int power, Farmer who)
        {
            location.playSound("hammer", tile);

            if (tool.UpgradeLevel < 2)
            {
                Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:Pickaxe.cs.14194")));
                return;
            }


            if ((int)tile.X == BoulderTileX && (int)tile.Y == BoulderTileY)
            {
                HitsToBoulder += power + 1;
                obj.shakeTimer = 190;
            }
            else
            {
                HitsToBoulder = 0;
                BoulderTileX = (int)tile.X;
                BoulderTileY = (int)tile.Y;
            }

            if (HitsToBoulder >= 4)
            {
                location.removeObject(tile, false);
                SpawnBoulderBreakSprites(location, tile);
                location.playSound("boulderBreak", tile);
            }
        }

        private static void HandleGenericObject(GameLocation location, Vector2 tile, StardewValley.Object obj, Farmer who)
        {
            obj.performRemoveAction();

            if (obj.Type == "Crafting" && obj.Fragility != 2)
            {
                Game1.currentLocation.debris.Add(new Debris(obj.QualifiedItemId, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel)));
            }

            Game1.currentLocation.Objects.Remove(tile);
        }

        private static void HandleWoodHit(GameLocation location, Vector2 tile)
        {
            location.playSound("woodyHit", tile);
        }

        private static void SpawnStoneBreakSprites(GameLocation location, Vector2 tile, StardewValley.Object obj)
        {
            // Extracted sprite/debris code here for readability
            TemporaryAnimatedSprite sprite =
                ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).TextureName == "Maps\\springobjects"
                && obj.ParentSheetIndex < 200
                && !Game1.objectData.ContainsKey((obj.ParentSheetIndex + 1).ToString())
                && obj.QualifiedItemId != "(O)25"
                ? new TemporaryAnimatedSprite(obj.ParentSheetIndex + 1, 300f, 1, 2,
                    new Vector2((int)tile.X * 64, (int)tile.Y * 64), true, obj.Flipped)
                { alphaFade = 0.01f }
                : new TemporaryAnimatedSprite(47, tile * 64f, Color.Gray, 10, false, 80f);

            Game1.Multiplayer.broadcastSprites(location, sprite);
            Game1.createRadialDebris(location, 14, (int)tile.X, (int)tile.Y, Game1.random.Next(2, 5), false);
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(46, tile * 64f, Color.White, 10, false, 80f)
            {
                motion = new Vector2(0f, -0.6f),
                acceleration = new Vector2(0f, 0.002f),
                alphaFade = 0.015f
            });
        }

        private static void SpawnBoulderBreakSprites(GameLocation location, Vector2 tile)
        {
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * tile.X - 32f, 64f * (tile.Y - 1f)), Color.Gray, 8, Game1.random.NextBool(), 50f));
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * tile.X + 32f, 64f * (tile.Y - 1f)), Color.Gray, 8, Game1.random.NextBool(), 50f) { delayBeforeAnimationStart = 200 });
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * tile.X, 64f * (tile.Y - 1f) - 32f), Color.Gray, 8, Game1.random.NextBool(), 50f) { delayBeforeAnimationStart = 400 });
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(5, new Vector2(64f * tile.X, 64f * tile.Y - 32f), Color.Gray, 8, Game1.random.NextBool(), 50f) { delayBeforeAnimationStart = 600 });

            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, tile * 64f, Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128));
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, new Vector2(64f * tile.X + 32f, 64f * tile.Y), Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128) { delayBeforeAnimationStart = 250 });
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(25, new Vector2(64f * tile.X - 32f, 64f * tile.Y), Color.White, 8, Game1.random.NextBool(), 50f, 0, -1, -1f, 128) { delayBeforeAnimationStart = 500 });
        }

    }

}
