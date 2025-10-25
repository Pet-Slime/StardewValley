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
        private Vector2 Pos;
        private int TimeLeft = 60 * 60;

        private GameLocation PrevSummonerLoc;
        private int AnimTimer;
        private int AnimFrame;
        private LightSource Light;
        private int Level;

        private TemporaryAnimatedSprite Sprite;
        private TemporaryAnimatedSprite Shadow;


        /*********
        ** Public methods
        *********/
        public LanternEffect(Farmer theSummoner, int level)
        {
            this.Summoner = theSummoner;
            this.Tex = ModEntry.Assets.Thunderbug;

            this.Pos = this.Summoner.Position;
            this.PrevSummonerLoc = this.Summoner.currentLocation;
            this.Level = level;
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

            if (this.PrevSummonerLoc != Game1.currentLocation)
            {
                this.CleanUp();
                this.PrevSummonerLoc = Game1.currentLocation;
                this.Pos = this.Summoner.Position;
                this.AddSpriteAndLight();
            }

            if (Utility.distance(this.Summoner.Position.X, this.Pos.X, this.Summoner.Position.Y, this.Pos.Y) > Game1.tileSize)
            {
                Vector2 unit = this.Summoner.Position - this.Pos;
                unit.Normalize();

                this.Pos += unit * 7;
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

        public void CleanUp()
        {
            if (this.PrevSummonerLoc.temporarySprites.Contains(this.Sprite))
            {
                this.PrevSummonerLoc.temporarySprites.Remove(this.Sprite);
            }
            if (this.PrevSummonerLoc.temporarySprites.Contains(this.Shadow))
            {
                this.PrevSummonerLoc.temporarySprites.Remove(this.Shadow);
            }
            if (Game1.currentLightSources.ContainsKey(this.Light.Id))
            {
                Game1.currentLightSources.Remove(this.Light.Id);
            }
        }

        private void UpdateSprite()
        {
            if (++this.AnimTimer >= 6)
            {
                this.AnimTimer = 0;
                if (++this.AnimFrame >= 4)
                    this.AnimFrame = 0;
            }
            int tx = this.AnimFrame % 4 * 16;
            int ty = GetSnappedDirection(this.Pos, this.Summoner.Position) * 16;
            Vector2 dynamicTargetPosition = this.GetDynamicTargetPosition(this.Pos + new Vector2(0f, -80f));
            this.Sprite.sourceRect.X = tx;
            this.Sprite.sourceRect.Y = ty;
            this.Sprite.position = Vector2.Lerp(this.Sprite.position, dynamicTargetPosition, 0.2f);
            this.Sprite.layerDepth = (this.Pos.Y) / 10000f;
            this.Shadow.position = Vector2.Lerp(this.Shadow.position, this.GetDynamicTargetPosition(this.Pos), 0.2f);
            this.Shadow.layerDepth = (this.Pos.Y - 1) / 10000f;
        }

        private Vector2 GetDynamicTargetPosition(Vector2 basePosition)
        {
            float y = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0) * 6f;
            float x = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500.0) * 10f;
            return basePosition + new Vector2(x, y);
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

        public void AddSpriteAndLight()
        {
            this.Sprite = new TemporaryAnimatedSprite("", new Rectangle(0, 0, 16, 16), 200f, 1, 9999, this.Summoner.Position, false, false)
            {
                texture = this.Tex,
                scale = this.Summoner.Scale * 2f,
                layerDepth = (this.Pos.Y) / 10000f

            };
            this.Sprite.color = Color.Yellow;
            this.Shadow = new TemporaryAnimatedSprite("", Game1.shadowTexture.Bounds, 200f, 1, 9999, this.Summoner.Position, false, false)
            {
                texture = Game1.shadowTexture,
                scale = this.Summoner.Scale * 2f,
                layerDepth = (this.Pos.Y - 1) / 10000f
            };
            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Sprite);
            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Shadow);
            string text = $"LanternSpell_{this.Summoner.UniqueMultiplayerID}_{this.Level}";
            this.Light = new LightSource(text, 1, new Vector2(this.Summoner.Position.X + 21f, this.Summoner.Position.Y + 64f), 8f * (this.Level + 1), new Color(0, 50, 170), LightSource.LightContext.None, this.Summoner.UniqueMultiplayerID, null);
            Game1.currentLightSources[text] = this.Light;
        }

        public void Draw(SpriteBatch b)
        {

        }
    }
}
