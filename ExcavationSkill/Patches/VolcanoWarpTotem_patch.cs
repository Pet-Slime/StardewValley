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

namespace ExcavationSkill.Patches
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
            if (__instance.Name == "moonslime.excavation.totem_volcano_warp")
            {
                Game1.player.jitterStrength = 1f;
                Color glowColor = Color.Red;
                location.playSound("warrior");
                Game1.player.faceDirection(2);
                Game1.player.CanMove = false;
                Game1.player.temporarilyInvincible = true;
                Game1.player.temporaryInvincibilityTimer = -4000;
                Game1.changeMusicTrack("none");
                Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
                {
                            new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
                            new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, Volcano_totemWarp, behaviorAtEndOfFrame: true)
                });

                BroadcastSprites(location, new TemporaryAnimatedSprite(textureName: ModEntry.Assets.Totem_volcano_warpPath,sourceRect: IconSourceRectangle(), animationInterval: 9999f,animationLength: 1,numberOfLoops: 99,position: Game1.player.Position + new Vector2(0f, -96f),flicker: false, flipped: false)
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
                });
                BroadcastSprites(location, new TemporaryAnimatedSprite(textureName: ModEntry.Assets.Totem_volcano_warpPath, sourceRect: IconSourceRectangle(), animationInterval: 9999f, animationLength: 1, numberOfLoops: 99, position: Game1.player.Position + new Vector2(-64f, -96f), flicker: false, flipped: false)
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
                });
                BroadcastSprites(location, new TemporaryAnimatedSprite(textureName: ModEntry.Assets.Totem_volcano_warpPath, sourceRect: IconSourceRectangle(), animationInterval: 9999f, animationLength: 1, numberOfLoops: 99, position: Game1.player.Position + new Vector2(64f, -96f), flicker: false, flipped: false)
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
                });
                Game1.screenGlowOnce(glowColor, hold: false);
                Utility.addSprinklesToLocation(location, Game1.player.getTileX(), Game1.player.getTileY(), 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
                __result = true;
            }
        }
        public static Rectangle IconSourceRectangle()
        {
            Rectangle source = new(0, 0, 48, 48);
            return source;
        }

        public static void Volcano_totemWarp(Farmer who)
        {
            for (int i = 0; i < 12; i++)
            {
                BroadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, (Game1.random.NextDouble() < 0.5) ? true : false));
            }

            who.currentLocation.playSound("wand");
            Game1.displayFarmer = false;
            Game1.player.temporarilyInvincible = true;
            Game1.player.temporaryInvincibilityTimer = -2000;
            Game1.player.freezePause = 1000;
            Game1.flashAlpha = 1f;
            DelayedAction.fadeAfterDelay(Volcano_totemWarpForReal, 1000);
            new Microsoft.Xna.Framework.Rectangle(who.GetBoundingBox().X, who.GetBoundingBox().Y, 64, 64).Inflate(192, 192);
            int num = 0;
            for (int num2 = who.getTileX() + 8; num2 >= who.getTileX() - 8; num2--)
            {
                BroadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, who.getTileY()) * 64f, Color.White, 8, flipped: false, 50f)
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

            Game1.warpFarmer("VolcanoDungeon" + 31, 0, 1, 2);

            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }

        public static void BroadcastSprites(GameLocation location, params TemporaryAnimatedSprite[] sprites)
        {
            location.temporarySprites.AddRange(sprites);
            if (sprites.Length == 0 || !Game1.IsMultiplayer)
            {
                return;
            }

            using MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = CreateWriter(memoryStream))
            {
                binaryWriter.Push("TemporaryAnimatedSprites");
                binaryWriter.Write(sprites.Length);
                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i].Write(binaryWriter, location);
                }

                binaryWriter.Pop();
            }

            BroadcastLocationBytes(location, 7, memoryStream.ToArray());
        }

        protected static BinaryWriter CreateWriter(Stream stream)
        {
            var logging = ModEntry.Instance.Helper.Reflection.GetField<NetLogger>(typeof(StardewValley.Multiplayer), "logging").GetValue();
            BinaryWriter binaryWriter = new BinaryWriter(stream);
            if (logging.IsLogging)
            {
                binaryWriter = new LoggingBinaryWriter(binaryWriter);
            }

            return binaryWriter;
        }

        protected static void BroadcastLocationBytes(GameLocation loc, byte messageType, byte[] bytes)
        {
            OutgoingMessage message = new OutgoingMessage(messageType, Game1.player, loc.isStructure.Value, loc.isStructure.Value ? loc.uniqueName.Value : loc.Name, bytes);
            BroadcastLocationMessage(loc, message);
        }

        protected static void BroadcastLocationMessage(GameLocation loc, OutgoingMessage message)
        {
            if (Game1.IsClient)
            {
                Game1.client.sendMessage(message);
                return;
            }

            Action<Farmer> action = delegate (Farmer f)
            {
                if (f != Game1.player)
                {
                    Game1.server.sendMessage(f.UniqueMultiplayerID, message);
                }
            };
            if (IsAlwaysActiveLocation(loc))
            {
                foreach (Farmer value in Game1.otherFarmers.Values)
                {
                    action(value);
                }

                return;
            }

            foreach (Farmer farmer in loc.farmers)
            {
                action(farmer);
            }

            if (!(loc is BuildableGameLocation))
            {
                return;
            }

            foreach (Building building in (loc as BuildableGameLocation).buildings)
            {
                if (building.indoors.Value == null)
                {
                    continue;
                }

                foreach (Farmer farmer2 in building.indoors.Value.farmers)
                {
                    action(farmer2);
                }
            }
        }

        public static bool IsAlwaysActiveLocation(GameLocation location)
        {
            if (!(location.Name == "Farm") && !(location.Name == "FarmHouse") && !(location.Name == "Greenhouse"))
            {
                if (location.Root.Value != null)
                {
                    return location.Root.Value.Equals(Game1.getFarm());
                }

                return false;
            }

            return true;
        }
    }
}
