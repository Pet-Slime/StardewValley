using HarmonyLib;
using MoonShared.Patching;
using StardewModdingAPI;
using StardewValley;

using Microsoft.Xna.Framework;
using MoonShared;
using Force.DeepCloner;
using System.IO;
using StardewValley.Network;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Locations;
using System;
using StardewValley.Extensions;

namespace ArchaeologySkill.Patches
{
    internal class VolcanoWarpTotem_patch : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<StardewValley.Object>("performUseAction"),
                postfix: this.GetHarmonyMethod(nameof(After_GetPriceAfterMultipliers))
            );
        }


        /*********
        ** Private methods
        *********/
        /// Post Fix to make it so the player gets more money with the Antiquary profession

        [HarmonyLib.HarmonyPostfix]
        private static void After_GetPriceAfterMultipliers(
        StardewValley.Object __instance, ref bool __result, GameLocation location)
        {
            if (__instance.HasContextTag("moonslime_volcano_warp"))
            {
                Game1.player.jitterStrength = 1f;
                Color glowColor = Color.Red;
                location.playSound("warrior");
                Game1.player.faceDirection(2);
                Game1.player.CanMove = false;
                Game1.player.temporarilyInvincible = true;
                Game1.player.temporaryInvincibilityTimer = -4000;
                Game1.changeMusicTrack("silence");
                Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
                {
                                new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
                                new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, Volcano_totemWarp, behaviorAtEndOfFrame: true)
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
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.Name);
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
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.Name);
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
                temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.Name);
                Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
                Game1.screenGlowOnce(glowColor, hold: false);
                Utility.addSprinklesToLocation(location, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
                __result = true;
            }
        }

        public static void Volcano_totemWarp(Farmer who)
        {
            GameLocation currentLocation = who.currentLocation;
            for (int i = 0; i < 12; i++)
            {
                Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
            }

            who.playNearbySoundAll("wand");
            Game1.displayFarmer = false;
            Game1.player.temporarilyInvincible = true;
            Game1.player.temporaryInvincibilityTimer = -2000;
            Game1.player.freezePause = 1000;
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
            Game1.changeMusicTrack("VolcanoMines");
        }

        private static void Volcano_totemWarpForReal()
        {

            Game1.warpFarmer("VolcanoDungeon" + 31, 0, 1, false);

            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }
    }
}
