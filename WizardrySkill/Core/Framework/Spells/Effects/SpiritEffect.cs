using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class SpiritEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Summoner;
        private readonly Texture2D Tex;
        private Vector2 Pos;
        private int TimeLeft = 60 * 60;

        private GameLocation PrevSummonerLoc;
        private int AttackTimer;
        private int AnimTimer;
        private int AnimFrame;

        private TemporaryAnimatedSprite Sprite;
        private TemporaryAnimatedSprite Shadow;


        /*********
        ** Public methods
        *********/
        public SpiritEffect(Farmer theSummoner)
        {
            this.Summoner = theSummoner;
            this.Tex = ModEntry.Assets.Spirit;

            this.Pos = this.Summoner.Position;
            this.PrevSummonerLoc = this.Summoner.currentLocation;
            this.AddSprite();
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
                this.AddSprite();
            }

            float nearestDist = 10;
            Monster nearestMob = null;
            foreach (var character in this.Summoner.currentLocation.characters)
            {
                if (character is Monster mob)
                {

                    float dist = Vector2.Distance(mob.Tile, this.Summoner.Tile);
                    if (dist < nearestDist && !mob.IsInvisible)
                    {
                        nearestDist = dist;
                        nearestMob = mob;
                    }
                }
            }

            if (this.AttackTimer > 0)
                --this.AttackTimer;
            if (nearestMob != null)
            {
                Vector2 unit = nearestMob.Position - this.Pos;
                unit.Normalize();

                this.Pos += unit * 7;

                if (Utility.distance(nearestMob.Position.X, this.Pos.X, nearestMob.Position.Y, this.Pos.Y) < Game1.tileSize)
                {
                    if (this.AttackTimer <= 0)
                    {
                        this.AttackTimer = 45;
                        int baseDmg = 5 * (this.Summoner.CombatLevel + Skills.GetSkillLevel(this.Summoner, "moonslime.Wizard"));
                        var oldPos = this.Summoner.Position;
                        this.Summoner.Position = new Vector2(nearestMob.GetBoundingBox().Center.X, nearestMob.GetBoundingBox().Center.Y);
                        this.Summoner.currentLocation.damageMonster(nearestMob.GetBoundingBox(), (int)(baseDmg * 0.75f), (int)(baseDmg * 1.5f), false, 1, 0, 0.1f, 2, false, this.Summoner);
                        this.Summoner.Position = oldPos;
                    }
                }

                this.UpdateSprite(nearestMob.Position);
            }
            else
            {
                if (Utility.distance(this.Summoner.Position.X, this.Pos.X, this.Summoner.Position.Y, this.Pos.Y) > Game1.tileSize)
                {
                    Vector2 unit = this.Summoner.Position - this.Pos;
                    unit.Normalize();

                    this.Pos += unit * 7;
                }

                this.UpdateSprite(this.Summoner.Position);
            }

            if (--this.TimeLeft <= 0)
            {
                this.CleanUp();
                return false;
            }
            return true;
        }

        private void UpdateSprite(Vector2 target)
        {
            if (++this.AnimTimer >= 12)
            {
                this.AnimTimer = 0;
                if (++this.AnimFrame >= 4)
                    this.AnimFrame = 0;
            }
            int tx = this.AnimFrame % 4 * 16;
            int ty = GetSnappedDirection(this.Pos, target) * 24;
            Vector2 dynamicTargetPosition = this.GetDynamicTargetPosition(this.Pos + new Vector2(-12f, -80f));
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

        public void AddSprite()
        {
            this.Sprite = new TemporaryAnimatedSprite("", new Rectangle(0, 0, 16, 24), 200f, 1, 9999, this.Summoner.Position, false, false)
            {
                texture = this.Tex,
                scale = this.Summoner.Scale * 4f,
                layerDepth = (this.Pos.Y) / 10000f

            };
            this.Shadow = new TemporaryAnimatedSprite("", Game1.shadowTexture.Bounds, 200f, 1, 9999, this.Pos, false, false)
            {
                texture = Game1.shadowTexture,
                scale = this.Summoner.Scale * 4f,
                layerDepth = (this.Pos.Y - 1) / 10000f
            };
            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Sprite);
            Game1.Multiplayer.broadcastSprites(this.Summoner.currentLocation, this.Shadow);
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
        }

        public void Draw(SpriteBatch b)
        {

        }
    }
}
