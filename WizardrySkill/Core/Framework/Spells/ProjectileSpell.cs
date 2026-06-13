using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Game;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines spells that shoot projectiles, like fireballs or magic missiles.
    public class ProjectileSpell : Spell
    {
        /*********
        ** Fields / Properties
        *********/
        public int ManaBase { get; }
        public int DamageBase { get; }
        public int DamageIncr { get; }
        public string Sound { get; }
        public bool Seeking { get; }
        public int SpriteIndex { get; }
        public int Bounces { get; }
        public string Debuff { get; }
        public int Tail { get; }
        public float RotationalVelocy { get; }
        public bool Wavey { get; }
        public int PiercesLeft { get; }
        public bool IgnoreTerrain { get; }
        public bool Explosion { get; }


        /*********
        ** Constructor
        *********/
        public ProjectileSpell(string school, string def_id, int manaBase, int dmgBase, int dmgIncr, string sound,
            int spriteindex, int bounces = 0, string debuff = "14", int tail = 0, float rotationVelocy = MathF.PI / 16f,
            bool seeking = false, bool wavey = true, int piercesLeft = 1, bool ignoreTerrain = false, bool explosion = false)
            : base(school, def_id)
        {
            this.ManaBase = manaBase;
            this.DamageBase = dmgBase;
            this.DamageIncr = dmgIncr;
            this.Sound = sound;
            this.Seeking = seeking;
            this.SpriteIndex = spriteindex;
            this.Bounces = bounces;
            this.Debuff = debuff;
            this.Tail = tail;
            this.RotationalVelocy = rotationVelocy;
            this.Wavey = wavey;
            this.PiercesLeft = piercesLeft;
            this.IgnoreTerrain = ignoreTerrain;
            this.Explosion = explosion;
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return this.ManaBase * (level + 1);
        }

        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        /// <summary>Build spell-specific packet data when the local player initiates a cast.</summary>
        /// <param name="caster">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="targetX">The target X position in pixels.</param>
        /// <param name="targetY">The target Y position in pixels.</param>
        public override Dictionary<string, string> BuildPacketData(Farmer caster, int level, int targetX, int targetY)
        {
            Vector2 shootOrigin = this.GetShootOrigin(caster);

            return new Dictionary<string, string>
            {
                ["origin_x"] = shootOrigin.X.ToString(CultureInfo.InvariantCulture),
                ["origin_y"] = shootOrigin.Y.ToString(CultureInfo.InvariantCulture)
            };
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            if (!player.IsLocalPlayer)
                return null;

            Vector2 shootOrigin = this.GetShootOrigin(player);
            this.SpawnProjectile(player, level, targetX, targetY, shootOrigin);
            Utilities.AddEXP(player, this.GetManaCost(player, level));

            return null;
        }


        /*********
        ** Private methods
        *********/
        private void SpawnProjectile(Farmer player, int level, int targetX, int targetY, Vector2 shootOrigin)
        {
            int newLevel = level + 1;

            int ammoDamage = this.DamageIncr * newLevel;
            int finalDamage = (int)(this.DamageBase * (ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) * (1f + player.buffs.AttackMultiplier));

            Vector2 targetPosition = this.AdjustForHeight(new Vector2(targetX, targetY));
            Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(shootOrigin, targetPosition, (15 + Game1.random.Next(4, 6)) * (1f + player.buffs.WeaponSpeedMultiplier));

            GameLocation location = player.currentLocation;

            SpellProjectile spellProjectile = new(
                damage: finalDamage,
                debuff: this.Debuff,
                spriteIndex: this.SpriteIndex,
                bouncesTillDestruct: this.Bounces,
                tailLength: this.Tail,
                rotationVelocity: this.RotationalVelocy,
                xVelocity: velocityTowardPoint.X,
                yVelocity: velocityTowardPoint.Y,
                startingPosition: shootOrigin,
                location: location,
                owner: player,
                hitsMonsters: true,
                playDefaultSoundOnFire: true,
                sound: this.Sound,
                explosion: this.Explosion);

            spellProjectile.WavyMotion.Value = this.Wavey;
            spellProjectile.piercesLeft.Value = this.PiercesLeft;
            spellProjectile.rotationVelocity.Value = this.RotationalVelocy;
            spellProjectile.startingRotation.Value = this.GetRotationFromOrigin(shootOrigin, targetX, targetY);

            spellProjectile.DebuffIntensity.Value = 2000 * newLevel;
            spellProjectile.boundingBoxWidth.Value = 32;
            spellProjectile.maxTravelDistance.Value = -1;

            if (this.IgnoreTerrain)
            {
                spellProjectile.IgnoreLocationCollision = true;
                spellProjectile.ignoreTravelGracePeriod.Value = true;
            }

            location.projectiles.Add(spellProjectile);
        }



        /*********
        ** Helper methods
        *********/
        public Vector2 GetShootOrigin(Farmer who)
        {
            return this.AdjustForHeight(who.getStandingPosition(), for_cursor: false);
        }

        public Vector2 AdjustForHeight(Vector2 position, bool for_cursor = true)
        {
            if (!Game1.options.useLegacySlingshotFiring && for_cursor)
                return new Vector2(position.X, position.Y);

            return new Vector2(position.X, position.Y - 32f - 8f);
        }

        public float GetRotation(Farmer player, int targetX, int targetY)
        {
            return this.GetRotationFromOrigin(this.GetShootOrigin(player), targetX, targetY);
        }

        private float GetRotationFromOrigin(Vector2 shootOrigin, int targetX, int targetY)
        {
            float rotation = (float)Math.Atan2(targetY - shootOrigin.Y, targetX - shootOrigin.X) - MathF.PI / 2f;

            rotation -= MathF.PI;
            if (rotation < 0f)
                rotation += MathF.PI * 2f;

            return rotation;
        }
    }
}
