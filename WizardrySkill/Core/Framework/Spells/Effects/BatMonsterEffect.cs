using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class BatMonsterEffect : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Summoner;
        private readonly Texture2D Tex;

        private Vector2 Pos;
        private GameLocation PrevSummonerLoc;

        private TemporaryAnimatedSprite Sprite;
        private TemporaryAnimatedSprite Shadow;

        private int TimeLeft = 60 * 60;
        private int AttackTimer;
        private int AnimTimer;
        private int AnimFrame;
        private readonly float AttackRange;

        private static readonly Vector2 SpriteOffset = new(-11f, -80f);
        private static Vector2 SharedOscillation;


        /*********
        ** Constructor
        *********/
        public BatMonsterEffect(Farmer summoner, float attackrange)
        {
            this.Summoner = summoner;
            this.Tex = ModEntry.Assets.Bat;
            this.Pos = summoner.Position;
            this.PrevSummonerLoc = summoner.currentLocation;
            this.AttackRange = attackrange;

            this.AddSprite();
        }


        /*********
        ** Update / Draw
        *********/
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Summoner == null)
            {
                this.CleanUp();
                return false;
            }

            // Handle location changes
            if (this.PrevSummonerLoc != this.Summoner.currentLocation)
            {
                this.CleanUp();
                this.PrevSummonerLoc = this.Summoner.currentLocation;
                this.Pos = this.Summoner.Position;
                this.AddSprite();
            }

            // Handle attack or movement
            Monster target = this.AttackTimer > 0 ? null : this.FindNearestMonster(this.AttackRange);
            if (this.AttackTimer > 0)
                this.AttackTimer--;

            if (target != null)
            {
                this.MoveTowards(target.Position);

                if (Vector2.Distance(this.Pos, target.Position) < Game1.tileSize)
                    this.AttemptAttack(target);

                this.UpdateSprite(target.Position);
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


        /*********
        ** Private helpers
        *********/
        private void FollowSummoner()
        {
            if (Vector2.Distance(this.Pos, this.Summoner.Position) <= Game1.tileSize)
                return;

            Vector2 dir = this.Summoner.Position - this.Pos;
            dir.Normalize();
            this.Pos += dir * 7f;
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

        private void AttemptAttack(Monster mob)
        {
            if (this.AttackTimer > 0)
                return;

            this.AttackTimer = 60;
        }

        private Monster FindNearestMonster(float maxDistance)
        {
            Monster nearest = null;
            float nearestDist = maxDistance;

            foreach (var character in this.Summoner.currentLocation.characters)
            {
                if (character is not Monster mob || mob.IsInvisible || mob.isInvincible())
                    continue;

                if (mob is LavaLurk lurk &&
                    (lurk.currentState.Value == LavaLurk.State.Submerged || lurk.currentState.Value == LavaLurk.State.Diving))
                    continue;

                float dist = Vector2.Distance(mob.Tile, this.Summoner.Tile);
                if (dist < nearestDist)
                {
                    nearest = mob;
                    nearestDist = dist;
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

            // Animate frames
            if (++this.AnimTimer >= 12)
            {
                this.AnimTimer = 0;
                this.AnimFrame = (this.AnimFrame + 1) & 3; // faster modulo 4
            }

            int direction = GetSnappedDirection(this.Pos, target);
            this.Sprite.sourceRect.X = this.AnimFrame * 16;
            this.Sprite.sourceRect.Y = direction * 24;

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

        private void AddSprite()
        {
            float scale = this.Summoner.Scale * 4f;
            Vector2 startPos = this.Summoner.Position;

            this.Sprite = new TemporaryAnimatedSprite(
                textureName: "",
                sourceRect: new Rectangle(0, 0, 16, 24),
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
            this.PrevSummonerLoc.TemporarySprites.Add(this.Sprite);
            this.PrevSummonerLoc.TemporarySprites.Add(this.Shadow);
        }

        public void CleanUp()
        {
            if (this.PrevSummonerLoc == null)
                return;

            this.PrevSummonerLoc.temporarySprites.Remove(this.Sprite);
            this.PrevSummonerLoc.temporarySprites.Remove(this.Shadow);
        }
    }
}
