using System;
using WizardrySkill.Core.Framework.Game;
using StardewValley;
using WizardrySkill.Core;
using Microsoft.Xna.Framework;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using StardewValley.Tools;
using StardewValley.Projectiles;
using StardewValley.Monsters;
using WizardrySkill.Core.Framework;

namespace WizardrySkill.Core.Framework.Spells
{
    public class ProjectileSpell : Spell
    {
        /*********
        ** Accessors
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
        ** Public methods
        *********/

        public ProjectileSpell(string school, string id, int manaBase, int dmgBase, int dmgIncr, string sound,
            int spriteindex, int bounces = 0, string debuff = "14", int tail = 0, float rotationVelocy = MathF.PI / 16f, bool seeking = false,
            bool wavey = true, int piercesLeft = 1, bool ignoreTerrain = false, bool explosion = false)
            : base(school, id)
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

        public override int GetManaCost(Farmer player, int level)
        {
            return this.ManaBase * (level + 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int ammoDamage = (this.DamageBase + this.DamageIncr * (level + 1)) * (player.CombatLevel + 1) / 2;
            int finalDamage = (int)((ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) * (1f + player.buffs.AttackMultiplier));
            Vector2 shootOrigin = this.GetShootOrigin(player);
            Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(this.GetShootOrigin(player), this.AdjustForHeight(new Vector2(targetX, targetY)), (15 + Game1.random.Next(4, 6)) * (1f + player.buffs.WeaponSpeedMultiplier));
            var location = player.currentLocation;
            var spellProjectile = new SpellProjectile(finalDamage, this.Debuff, this.SpriteIndex, this.Bounces,
                 this.Tail, this.RotationalVelocy, velocityTowardPoint.X, velocityTowardPoint.Y, shootOrigin, location, player, true, sound: this.Sound, explosion: this.Explosion);
            spellProjectile.wavyMotion.Value = this.Wavey;
            spellProjectile.piercesLeft.Value = this.PiercesLeft;
            spellProjectile.rotationVelocity.Value = this.RotationalVelocy;
            spellProjectile.startingRotation.Value = this.GetRotation(player, targetX, targetY);

            spellProjectile.debuffIntensity.Value = 4000;
            spellProjectile.boundingBoxWidth.Value = 32;
            spellProjectile.maxTravelDistance.Value = 1000;
            if (this.IgnoreTerrain)
            {
                spellProjectile.ignoreLocationCollision.Value = true;
                spellProjectile.ignoreTravelGracePeriod.Value = true;
            }

            location.projectiles.Add(spellProjectile);



            return null;
        }

        public Vector2 GetShootOrigin(Farmer who)
        {
            return this.AdjustForHeight(who.getStandingPosition());
        }

        public Vector2 AdjustForHeight(Vector2 position)
        {
            return new Vector2(position.X - 8, position.Y - 32f - 8f);

        }

        public float GetRotation(Farmer player, int targetX, int targetY)
        {
            Point point = Utility.Vector2ToPoint(new Vector2(targetX, targetY));
            int mouseX = point.X;
            int mouseY = point.Y;

            Vector2 shootOrigin = this.GetShootOrigin(player);
            float rotation = (float)Math.Atan2(mouseY - shootOrigin.Y, mouseX - shootOrigin.X) + (float)-Math.PI / 2;

            rotation -= (float)Math.PI;
            if (rotation < 0f)
            {
                rotation += (float)Math.PI * 2f;
            }

            return rotation;
        }
    }
}
