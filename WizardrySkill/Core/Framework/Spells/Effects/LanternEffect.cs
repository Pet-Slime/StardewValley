using System;
using System.Collections.Generic;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Monsters;
using xTile.Tiles;
using static StardewValley.Minigames.TargetGame;
using Object = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class LanternEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Summoner;
        private readonly Texture2D Tex;
        private readonly int Level;

        private Vector2 Pos;
        private GameLocation PrevSummonerLoc;
        private LightSource Light;

        private TemporaryAnimatedSprite Sprite;
        private TemporaryAnimatedSprite Shadow;

        private int TimeLeft = 60 * 60;
        private int BaseAttackTimer;
        private int AttackTimer;
        private int AnimTimer;
        private int AnimFrame;

        private static readonly Vector2 SpriteOffset = new(0f, -80f);
        private static Vector2 SharedOscillation;


        /*********
        ** Public methods
        *********/
        public LanternEffect(Farmer summoner, int level)
        {
            this.Summoner = summoner;
            this.Level = level+1;
            this.TimeLeft = 30 * 3 * 60;
            this.Tex = ModEntry.Assets.Thunderbug;

            this.Pos = summoner.Position;
            this.PrevSummonerLoc = summoner.currentLocation;
            this.BaseAttackTimer = this.TimeLeft / (this.Level + 1);
            this.AttackTimer = this.BaseAttackTimer;
            this.AddSpriteAndLight();
        }

        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Summoner == null)
            {
                this.CleanUp();
                this.TimeLeft = 0;
                return false;
            }

            // Handle Location changed
            if (this.PrevSummonerLoc != this.Summoner.currentLocation)
            {
                this.CleanUp();
                this.PrevSummonerLoc = this.Summoner.currentLocation;
                this.Pos = this.Summoner.Position;
                this.AddSpriteAndLight();
            }

            // Handle attack or movement
            Vector2 target = this.AttackTimer > 0 ? Vector2.Zero : this.FindNearestLightningRod(10f * (this.Level));
            if (this.AttackTimer > 0)
                this.AttackTimer--;

            if (target != Vector2.Zero)
            {
                var targetPosition = target * Game1.tileSize;
                this.MoveTowards(targetPosition);

                if (Vector2.Distance(this.Pos, targetPosition) < Game1.tileSize)
                    this.AttemptAttack(target);

                this.UpdateSprite(targetPosition);
            }
            else
            {
                // No enemies found — follow the summoner
                this.FollowSummoner();
                this.UpdateSprite(this.Summoner.Position);
            }

            if (--this.TimeLeft <= 0)
            {
                this.CleanUp();
                return false;
            }

            return true;
        }

        public void Draw(SpriteBatch b)
        {
            // nothing; drawn Manually via TemporaryAnimatedSprite
        }

        public void CleanUp()
        {
            if (this.PrevSummonerLoc != null)
            {
                this.PrevSummonerLoc.temporarySprites.Remove(this.Sprite);
                this.PrevSummonerLoc.temporarySprites.Remove(this.Shadow);
            }

            if (this.Light != null)
                Game1.currentLightSources.Remove(this.Light.Id);
        }


        /*********
        ** Private helpers
        *********/
        private void FollowSummoner()
        {
            if (Vector2.Distance(this.Pos, this.Summoner.Position) <= Game1.tileSize)
                return;

            Vector2 direction = this.Summoner.Position - this.Pos;
            direction.Normalize();
            this.Pos += direction * 7f;

            if (this.Light != null)
                this.Light.position.Value = this.Pos;
        }

        private void MoveTowards(Vector2 target)
        {
            Vector2 dir = target - this.Pos;
            if (dir.LengthSquared() > 0.001f)
            {
                dir.Normalize();
                this.Pos += dir * 7f;
            }
        }

        private void AttemptAttack(Vector2 target)
        {
            var location = this.Summoner.currentLocation;
            List<Vector2> lightningRods = new List<Vector2>();
            foreach (KeyValuePair<Vector2, StardewValley.Object> thing in location.objects.Pairs)
            {
                if (thing.Key == target)
                {
                    lightningRods.Add(target);
                }
            }
            if (lightningRods.Count > 0)
            {
                if (location.objects[target].heldObject.Value == null)
                {
                    location.objects[target].heldObject.Value = ItemRegistry.Create<Object>("(O)787", 1, 0, false);
                    location.objects[target].MinutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
                    location.objects[target].shakeTimer = 1000;
                    Game1.flashAlpha = (float)(0.5 + Game1.random.NextDouble());
                    location.playSound("thunder", target);

                    var boltPosition = target * 64f + new Vector2(32f, 0f);
                    Utility.drawLightningBolt(boltPosition, location);
                }
            }
            this.AttackTimer = this.BaseAttackTimer;
        }

        private Vector2 FindNearestLightningRod(float maxDistance)
        {
            Vector2 nearest = Vector2.Zero;
            float nearestDist = maxDistance;

            Farm.LightningStrikeEvent lightningEvent = new Farm.LightningStrikeEvent();
            lightningEvent.bigFlash = true;
            List<Vector2> lightningRods = new List<Vector2>();
            GameLocation location = this.Summoner.currentLocation;
            foreach (KeyValuePair<Vector2, StardewValley.Object> thing in location.objects.Pairs)
            {
                if (thing.Value.QualifiedItemId == "(BC)9" && thing.Value.heldObject.Value == null)
                {
                    float dist = Vector2.Distance(thing.Key, this.Summoner.Tile);
                    if (dist < nearestDist)
                    {
                        nearest = thing.Value.TileLocation;
                        nearestDist = dist;
                    }
                }
            }

            return nearest;
        }

        public static void UpdateSharedOscillation()
        {
            double t = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            SharedOscillation.X = (float)Math.Sin(t * 0.002) * 10f;
            SharedOscillation.Y = (float)Math.Sin(t * 0.004) * 6f;
        }

        private void UpdateSprite(Vector2 target)
        {
            UpdateSharedOscillation();

            // Animation
            if (++this.AnimTimer >= 6)
            {
                this.AnimTimer = 0;
                this.AnimFrame = (this.AnimFrame + 1) & 3; // faster modulo 4
            }

            int direction = GetSnappedDirection(this.Pos, target);
            this.Sprite.sourceRect.X = this.AnimFrame * 16;
            this.Sprite.sourceRect.Y = direction * 16;

            // Visual offset + smooth movement
            Vector2 dynamicPos = this.Pos + SharedOscillation + SpriteOffset;
            Vector2 shadowPos = this.Pos + SharedOscillation;
            this.Sprite.position = Vector2.Lerp(this.Sprite.position, dynamicPos, 0.2f);
            this.Sprite.layerDepth = shadowPos.Y / 10000f;

            this.Shadow.position = Vector2.Lerp(this.Shadow.position, shadowPos, 0.2f);
            this.Shadow.layerDepth = (shadowPos.Y - 1) / 10000f;
        }

        public static int GetSnappedDirection(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;

            float angle = MathF.Atan2(direction.Y, direction.X);

            float degrees = MathHelper.ToDegrees(angle);

            if (degrees < 0)
                degrees += 360f;

            if (degrees >= 45 && degrees < 135)
                return 0; // Up
            if (degrees >= 135 && degrees < 225)
                return 3; // Left
            if (degrees >= 225 && degrees < 315)
                return 2; // Down
            return 1; // Right
        }

        private void AddSpriteAndLight()
        {
            float scale = this.Summoner.Scale * 2f;
            Vector2 startPos = this.Summoner.Position;

            this.Sprite = new TemporaryAnimatedSprite(
                textureName: "",
                sourceRect: new Rectangle(0, 0, 16, 16),
                animationInterval: 200f,
                animationLength: 1,
                numberOfLoops: 9999,
                position: startPos,
                flicker: false,
                flipped: false)
            {
                texture = this.Tex,
                scale = scale,
                color = Color.White,
                layerDepth = startPos.Y / 10000f
            };

            this.Shadow = new TemporaryAnimatedSprite(
                textureName: "",
                sourceRect: Game1.shadowTexture.Bounds,
                animationInterval: 200f,
                animationLength: 1,
                numberOfLoops: 9999,
                position: startPos,
                flicker: false,
                flipped: false)
            {
                texture = Game1.shadowTexture,
                scale = scale,
                layerDepth = (startPos.Y - 1) / 10000f
            };

            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Sprite);
            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Shadow);

            string lightId = $"LanternSpell_{this.Summoner.UniqueMultiplayerID}";
            this.Light = new LightSource(lightId, 1, new Vector2(this.Summoner.Position.X + 21f, this.Summoner.Position.Y + 64f), 8f * (this.Level), new Color(0, 50, 170), LightSource.LightContext.None, this.Summoner.UniqueMultiplayerID, null);

            Game1.currentLightSources[lightId] = this.Light;
        }
    }
}
