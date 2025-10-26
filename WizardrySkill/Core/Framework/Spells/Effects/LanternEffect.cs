using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

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
            this.Level = level;
            this.Tex = ModEntry.Assets.Thunderbug;

            this.Pos = summoner.Position;
            this.PrevSummonerLoc = summoner.currentLocation;

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

            // Location changed
            if (this.PrevSummonerLoc != Game1.currentLocation)
            {
                this.CleanUp();
                this.PrevSummonerLoc = Game1.currentLocation;
                this.Pos = this.Summoner.Position;
                this.AddSpriteAndLight();
            }

            // Smoothly follow summoner
            if (Vector2.Distance(this.Pos, this.Summoner.Position) > Game1.tileSize)
            {
                Vector2 direction = this.Summoner.Position - this.Pos;
                direction.Normalize();
                this.Pos += direction * 7f;

                if (this.Light != null)
                    this.Light.position.Value = this.Pos;
            }

            this.UpdateSprite();

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
        public static void UpdateSharedOscillation()
        {
            double t = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            SharedOscillation.X = (float)Math.Sin(t * 0.002) * 10f;
            SharedOscillation.Y = (float)Math.Sin(t * 0.004) * 6f;
        }

        private void UpdateSprite()
        {
            UpdateSharedOscillation();

            // Animation
            if (++this.AnimTimer >= 6)
            {
                this.AnimTimer = 0;
                this.AnimFrame = (this.AnimFrame + 1) & 3; // faster modulo 4
            }

            int direction = GetSnappedDirection(this.Pos, this.Summoner.Position);
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
            this.Light = new LightSource(lightId, 1, new Vector2(this.Summoner.Position.X + 21f, this.Summoner.Position.Y + 64f), 8f * (this.Level + 1), new Color(0, 50, 170), LightSource.LightContext.None, this.Summoner.UniqueMultiplayerID, null);

            Game1.currentLightSources[lightId] = this.Light;
        }
    }
}
