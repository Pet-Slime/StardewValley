using System;
using AthleticSkill.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Tools;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace AthleticSkill.Core.Patches
{
    [HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.beginUsing))]
    public static class PickAxeBeginUsing_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Pickaxe __instance, GameLocation location, int x, int y, Farmer who)
        {
            // Skip if conditions are not met early to minimize nesting
            if (ModEntry.UseAltProfession
                || !who.HasCustomProfession(Athletic_Skill.Athletic10a1)
                || __instance.UpgradeLevel <= 0
                || !who.modData.GetBool(Events.SprintingOn))
            {
                return true;
            }

            Log.Trace($"Enhanced pickaxe use: {__instance.DisplayName}, Power={who.toolPower.Value}");

            // Halt player and prepare animation
            who.Halt();
            __instance.Update(who.FacingDirection, 0, who);

            // Precompute facing direction data
            int frame;
            int facingIndex;

            switch (who.FacingDirection)
            {
                case Game1.up:
                    frame = 176; facingIndex = 0; break;
                case Game1.right:
                    frame = 168; facingIndex = 1; break;
                case Game1.down:
                    frame = 160; facingIndex = 2; break;
                case Game1.left:
                    frame = 184; facingIndex = 3; break;
                default:
                    return true; // invalid direction; let vanilla handle it
            }

            // Apply animation and update
            who.FarmerSprite.setCurrentFrame(frame);
            __instance.Update(facingIndex, 0, who);

            return false; // prevent original method
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.toolPowerIncrease))]
    public static class FarmerToolPower_Patch
    {
        private static int ToolPitchAccumulator;
        private static readonly Random ToolRng = new Random();

        private static readonly (Color Color, float Jitter)[] PowerStages =
        {
        (Color.White, 0f),           // 0 — unused
        (Color.Orange, 0.25f),       // 1
        (Color.LightSteelBlue, 0.5f),// 2
        (Color.Gold, 1f),            // 3
        (Color.Violet, 2f),          // 4
        (Color.BlueViolet, 3f)       // 5
    };

        private const int SpriteOffsetY = 192;
        private const int SpriteSheetY = 1152;

        [HarmonyPrefix]
        private static bool Prefix(Farmer __instance)
        {
            var who = __instance;

            // Early exit if any required condition fails 
            if (ModEntry.UseAltProfession
                || !who.HasCustomProfession(Athletic_Skill.Athletic10a1)
                || who.CurrentTool is not Pickaxe
                || !who.modData.GetBool(Events.SprintingOn))
            {
                return true; // run vanilla
            }

            // Reset accumulator if starting from 0
            if (who.toolPower.Value == 0)
                ToolPitchAccumulator = 0;

            // Increase tool charge level 
            who.toolPower.Value++;
            int powerLevel = who.toolPower.Value;

            // Clamp power level to valid range (1–5)
            if (powerLevel >= PowerStages.Length)
                powerLevel = PowerStages.Length - 1;

            // Color and jitter from precomputed lookup
            var (color, jitter) = PowerStages[powerLevel];
            who.jitterStrength = jitter;

            // Direction-based offsets
            int facing = who.FacingDirection;
            int offsetX = facing switch
            {
                Game1.right => 40,
                Game1.left => -40,
                Game1.down => 32,
                _ => 0
            };

            int offsetIndex = facing switch
            {
                Game1.up => 4,
                Game1.down => 2,
                _ => 0
            };

            int y = who.StandingPixel.Y;

            // Add sparkle/charge sprites
            var pos = who.Position;
            var sprites = Game1.currentLocation.temporarySprites;
            sprites.Add(new TemporaryAnimatedSprite(
                21,
                pos - new Vector2(offsetX, SpriteOffsetY),
                color,
                8,
                flipped: false,
                70f,
                0,
                64,
                y / 10000f + 0.005f,
                128));

            sprites.Add(new TemporaryAnimatedSprite(
                "TileSheets\\animations",
                new Rectangle(192, SpriteSheetY, 64, 64),
                50f,
                4,
                0,
                pos - new Vector2(facing != Game1.right ? -64 : 0, 128f),
                flicker: false,
                facing == Game1.right,
                y / 10000f,
                0.01f,
                Color.White,
                1f,
                0f,
                0f,
                0f));

            // Play charge-up sound ---
            int soundPitch = ToolRng.Next(12, 16) * 100 + powerLevel * 100;
            Game1.playSound("toolCharge", soundPitch);

            return false; // skip vanilla method
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
            if (!ModEntry.UseAltProfession && who.HasCustomProfession(Athletic_Skill.Athletic10a1) && __instance.UpgradeLevel > 0 && who.modData.GetBool(Events.SprintingOn))
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
