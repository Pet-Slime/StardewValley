using System;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Game;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines spells that shoot projectiles, like fireballs or magic missiles.
    public class ProjectileSpell : Spell
    {
        /*********
        ** Fields / Properties
        *********/
        // Basic spell stats
        public int ManaBase { get; }           // Base mana cost
        public int DamageBase { get; }         // Base damage
        public int DamageIncr { get; }         // Damage increase per level
        public string Sound { get; }           // Sound played when cast
        public bool Seeking { get; }           // Does the projectile track a target?
        public int SpriteIndex { get; }        // Sprite used for the projectile
        public int Bounces { get; }            // How many times projectile can bounce
        public string Debuff { get; }          // Debuff applied on hit
        public int Tail { get; }                // Trail length
        public float RotationalVelocy { get; } // Rotational speed
        public bool Wavey { get; }             // Does it have wave motion?
        public int PiercesLeft { get; }        // How many targets it can pierce
        public bool IgnoreTerrain { get; }     // Can it pass through terrain?
        public bool Explosion { get; }         // Does it explode on impact?

        /*********
        ** Constructor
        *********/
        public ProjectileSpell(string school, string id, int manaBase, int dmgBase, int dmgIncr, string sound,
            int spriteindex, int bounces = 0, string debuff = "14", int tail = 0, float rotationVelocy = MathF.PI / 16f,
            bool seeking = false, bool wavey = true, int piercesLeft = 1, bool ignoreTerrain = false, bool explosion = false)
            : base(school, id)
        {
            // Initialize all properties with the provided parameters
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

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost scales with level
            return this.ManaBase * (level + 1);
        }

        /*********
        ** Casting the spell
        *********/
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer) // Only cast for the local player
                return null;

            // Calculate base damage, then add random variation and player buffs
            int ammoDamage = (this.DamageBase + this.DamageIncr * (level + 1)) * (player.CombatLevel + 1) / 2;
            int finalDamage = (int)((ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) *
                                    (1f + player.buffs.AttackMultiplier));

            // Determine where the projectile should spawn
            Vector2 shootOrigin = this.GetShootOrigin(player);

            // Determine projectile velocity toward the target
            Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(
                this.GetShootOrigin(player),
                AdjustForHeight(new Vector2(targetX, targetY)),
                (15 + Game1.random.Next(4, 6)) * (1f + player.buffs.WeaponSpeedMultiplier)
            );

            var location = player.currentLocation;

            // Create the projectile object
            var spellProjectile = new SpellProjectile(
                finalDamage, this.Debuff, this.SpriteIndex, this.Bounces,
                this.Tail, this.RotationalVelocy, velocityTowardPoint.X, velocityTowardPoint.Y,
                shootOrigin, location, player, true, sound: this.Sound, explosion: this.Explosion
            );

            // Apply motion and behavior properties
            spellProjectile.WavyMotion.Value = this.Wavey;
            spellProjectile.piercesLeft.Value = this.PiercesLeft;
            spellProjectile.rotationVelocity.Value = this.RotationalVelocy;
            spellProjectile.startingRotation.Value = this.GetRotation(player, targetX, targetY);

            // Optional projectile stats
            spellProjectile.DebuffIntensity.Value = 4000;
            spellProjectile.boundingBoxWidth.Value = 32;
            spellProjectile.maxTravelDistance.Value = 1000;

            // Optional terrain interaction
            if (this.IgnoreTerrain)
            {
                spellProjectile.ignoreLocationCollision.Value = true;
                spellProjectile.ignoreTravelGracePeriod.Value = true;
            }

            // Add projectile to the location so it can move and interact
            location.projectiles.Add(spellProjectile);

            return null; // No immediate effect on casting
        }

        /*********
        ** Helper methods
        *********/

        // Returns where the projectile should spawn (adjusted for player height)
        public Vector2 GetShootOrigin(Farmer who)
        {
            return AdjustForHeight(who.getStandingPosition());
        }

        // Adjusts a position for projectile height (so it doesn't spawn at the player's feet)
        public static Vector2 AdjustForHeight(Vector2 position)
        {
            return new Vector2(position.X - 8, position.Y - 32f - 8f);
        }

        // Calculates the rotation (angle) for the projectile to face the target
        public float GetRotation(Farmer player, int targetX, int targetY)
        {
            Point point = Utility.Vector2ToPoint(new Vector2(targetX, targetY));
            int mouseX = point.X;
            int mouseY = point.Y;

            Vector2 shootOrigin = this.GetShootOrigin(player);

            // Use Atan2 to calculate angle from origin to target
            float rotation = (float)Math.Atan2(mouseY - shootOrigin.Y, mouseX - shootOrigin.X) - (float)Math.PI / 2;

            // Normalize rotation to 0–360 degrees
            rotation -= (float)Math.PI;
            if (rotation < 0f)
                rotation += (float)Math.PI * 2f;

            return rotation;
        }
    }
}
