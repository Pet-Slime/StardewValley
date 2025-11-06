using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.Companions;
using StardewValley.Monsters;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;

namespace WizardrySkill.Objects
{
    public class SpiritCompanion : FlyingCompanion
    {
        private readonly Farmer Summoner;
        private readonly Texture2D Tex;
        private GameLocation PrevSummonerLoc;

        private int TimeLeft = 60 * 60;
        private int AttackTimer;
        private int AnimTimer;
        private int AnimFrame;

        private static readonly Vector2 SpriteOffset = new(-11f, -80f);
        private static Vector2 SharedOscillation;


        private Vector2 AnimationPosition;

        public Vector2 SpriteDrawPosition;
        public float SpriteDrawLayer;

        public Vector2 ShadowPosition;

        public float ShadowLayerDepth { get; private set; }

        public SpiritCompanion()
        {
            this.Summoner = this.Owner;
            this.Tex = ModEntry.Assets.Spirit;
            this.AnimationPosition = new Vector2(0,0);
            this.PrevSummonerLoc = this.Owner.currentLocation;
            this.ShadowPosition = this.OwnerPosition;
            this.SpriteDrawPosition = this.OwnerPosition;

        }

        public override void Update(GameTime time, GameLocation location)
        {
            if (this.Summoner == null)
            {
                this.CleanUp();
            }

            // Handle location changes
            if (this.PrevSummonerLoc != this.Summoner.currentLocation)
            {
                this.CleanUp();
                this.PrevSummonerLoc = this.Summoner.currentLocation;
                this.Position = this.Summoner.Position;
            }

            // Handle attack or movement
            Monster target = this.AttackTimer > 0 ? null : this.FindNearestMonster(10f);
            if (this.AttackTimer > 0)
                this.AttackTimer--;

            if (target != null)
            {
                this.MoveTowards(target.Position);

                if (Vector2.Distance(this.Position, target.Position) < Game1.tileSize)
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
            }
        }

        public override void Draw(SpriteBatch b)
        {
            float scale = this.Summoner.Scale * 4f;
            SpriteEffects effect = SpriteEffects.None;

            b.Draw(this.Tex,
                this.SpriteDrawPosition,
                new Rectangle((int)this.AnimationPosition.X, (int)this.AnimationPosition.Y, 16, 24),
                Color.White,
                0f,
                new Vector2(8f, 8f),
                scale,
                effect,
                this.SpriteDrawLayer / 10000f);


            b.Draw(Game1.shadowTexture,
                this.ShadowPosition,
                Game1.shadowTexture.Bounds,
                Color.White,
                0f,
                new Vector2(8f, 8f),
                scale,
                effect,
                this.ShadowLayerDepth / 10000f);
        }


        /*********
        ** Private helpers
        *********/
        private void FollowSummoner()
        {
            if (Vector2.Distance(this.Position, this.Summoner.Position) <= Game1.tileSize)
                return;

            Vector2 dir = this.Summoner.Position - this.Position;
            dir.Normalize();
            this.Position += dir * 7f;
        }

        private void MoveTowards(Vector2 target)
        {
            Vector2 dir = target - this.Position;
            if (dir.LengthSquared() > 0.001f)
            {
                dir.Normalize();
                this.Position += dir * 7f;
            }
        }

        private void AttemptAttack(Monster mob)
        {
            if (this.AttackTimer > 0)
                return;

            this.AttackTimer = 60;
            int baseDmg = 5 * (this.Summoner.CombatLevel + this.Summoner.GetCustomBuffedSkillLevel(MagicConstants.SkillName));

            // Temporarily move summoner to apply hitbox-based damage
            Vector2 oldPos = this.Summoner.Position;
            this.Summoner.Position = new Vector2(mob.GetBoundingBox().Center.X, mob.GetBoundingBox().Center.Y);
            this.Summoner.currentLocation.damageMonster(mob.GetBoundingBox(), (int)(baseDmg * 0.75f), (int)(baseDmg * 1.5f), false, 1, 0, 0.1f, 2, false, this.Summoner);
            this.Summoner.Position = oldPos;
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

            int direction = GetSnappedDirection(this.Position, target);
            this.AnimationPosition.X = this.AnimFrame * 16;
            this.AnimationPosition.Y = direction * 24;

            // Visual offset + smooth movement
            Vector2 dynamicPos = this.Position + SharedOscillation + SpriteOffset;
            Vector2 shadowPos = this.Position + SharedOscillation;
            this.SpriteDrawPosition = Vector2.Lerp(this.SpriteDrawPosition, dynamicPos, 0.2f);
            this.SpriteDrawLayer = shadowPos.Y / 10000f;

            this.ShadowPosition = Vector2.Lerp(this.ShadowPosition, shadowPos, 0.2f);
            this.ShadowLayerDepth = (shadowPos.Y - 1) / 10000f;
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



        public void CleanUp()
        {

        }

        public override void Hop(float amount)
        {
        }
    }
}
