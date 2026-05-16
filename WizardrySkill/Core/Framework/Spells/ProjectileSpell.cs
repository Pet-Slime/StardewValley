using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
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
        public ProjectileSpell(string school, string id, int manaBase, int dmgBase, int dmgIncr, string sound,
            int spriteindex, int bounces = 0, string debuff = "14", int tail = 0, float rotationVelocy = MathF.PI / 16f,
            bool seeking = false, bool wavey = true, int piercesLeft = 1, bool ignoreTerrain = false, bool explosion = false)
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return this.ManaBase * (level + 1);
        }

        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        public override string BuildExtraData(Farmer caster, int level, int targetX, int targetY)
        {
            Vector2 shootOrigin = this.GetShootOrigin(caster);
            return SerializeShootOrigin(shootOrigin);
        }

        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (!Context.IsMainPlayer)
                return null;

            Vector2 shootOrigin = this.GetShootOrigin(caster);
            if (TryParseShootOrigin(extraData, out Vector2 syncedShootOrigin))
                shootOrigin = syncedShootOrigin;

            this.SpawnProjectile(caster, level, targetX, targetY, shootOrigin);
            return null;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            Vector2 shootOrigin = this.GetShootOrigin(player);
            this.SpawnProjectile(player, level, targetX, targetY, shootOrigin);
            return null;
        }


        /*********
        ** Private methods
        *********/
        private void SpawnProjectile(Farmer player, int level, int targetX, int targetY, Vector2 shootOrigin)
        {
            int newLevel = level + 1;

            float num = this.DamageBase;
            int ammoDamage = this.DamageIncr * newLevel;
            int finalDamage = (int)(num * (ammoDamage + Game1.random.Next(-(ammoDamage / 2), ammoDamage + 2)) * (1f + player.buffs.AttackMultiplier));

            Vector2 velocityTowardPoint = Utility.getVelocityTowardPoint(shootOrigin, this.AdjustForHeight(new Vector2(targetX, targetY)), (15 + Game1.random.Next(4, 6)) * (1f + player.buffs.WeaponSpeedMultiplier));

            GameLocation location = player.currentLocation;

            var spellProjectile = new SpellProjectile(finalDamage, this.Debuff, this.SpriteIndex, this.Bounces, this.Tail, this.RotationalVelocy, velocityTowardPoint.X, velocityTowardPoint.Y, shootOrigin, location, player, true, sound: this.Sound, explosion: this.Explosion);

            spellProjectile.WavyMotion.Value = this.Wavey;
            spellProjectile.piercesLeft.Value = this.PiercesLeft;
            spellProjectile.rotationVelocity.Value = this.RotationalVelocy;
            spellProjectile.startingRotation.Value = this.GetRotationFromOrigin(shootOrigin, targetX, targetY);

            spellProjectile.DebuffIntensity.Value = 2000 * newLevel;
            spellProjectile.boundingBoxWidth.Value = 32;
            spellProjectile.maxTravelDistance.Value = 1000 * newLevel;

            if (this.IgnoreTerrain)
            {
                spellProjectile.ignoreLocationCollision.Value = true;
                spellProjectile.ignoreTravelGracePeriod.Value = true;
            }

            location.projectiles.Add(spellProjectile);
        }

        private static string SerializeShootOrigin(Vector2 shootOrigin)
        {
            return string.Join("|",
                shootOrigin.X.ToString(CultureInfo.InvariantCulture),
                shootOrigin.Y.ToString(CultureInfo.InvariantCulture)
            );
        }

        private static bool TryParseShootOrigin(string raw, out Vector2 shootOrigin)
        {
            shootOrigin = Vector2.Zero;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string[] parts = raw.Split('|', 2);
            if (parts.Length < 2)
                return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                return false;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                return false;

            shootOrigin = new Vector2(x, y);
            return true;
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
