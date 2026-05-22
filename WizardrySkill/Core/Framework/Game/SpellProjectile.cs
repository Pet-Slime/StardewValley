using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace WizardrySkill.Core.Framework.Game
{
    // Copied and modified from vanilla's debuff projectile to make our own projectiles!
    public class SpellProjectile : Projectile
    {
        /*********
        ** Net fields
        *********/

        /// <summary>The debuff ID to apply on hit.</summary>
        public readonly NetString Debuff = new();

        /// <summary>Whether the projectile should use wavy motion.</summary>
        public readonly NetBool WavyMotion = new(value: true);

        /// <summary>The debuff intensity or duration used by special projectile effects.</summary>
        public readonly NetInt DebuffIntensity = new(-1);

        /// <summary>The amount of damage caused when this projectile hits a monster or player.</summary>
        public readonly NetInt DamageToFarmer = new();

        /// <summary>Whether this projectile should create an explosion on monster impact.</summary>
        public readonly NetBool ExplodesOnImpact = new();


        /*********
        ** Fields
        *********/
        private float PeriodicEffectTimer;


        /*********
        ** Constructors
        *********/

        /// <summary>Construct an empty instance.</summary>
        public SpellProjectile()
        {
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="damage">The projectile damage.</param>
        /// <param name="debuff">The debuff ID to apply on hit.</param>
        /// <param name="spriteIndex">The index of the sprite to draw in StardewValley.Projectiles.Projectile.projectileSheetName.</param>
        /// <param name="bouncesTillDestruct">The number of times the projectile can bounce off walls before being destroyed.</param>
        /// <param name="tailLength">The length of the tail which trails behind the main projectile.</param>
        /// <param name="rotationVelocity">The rotation velocity.</param>
        /// <param name="xVelocity">The speed at which the projectile moves along the X axis.</param>
        /// <param name="yVelocity">The speed at which the projectile moves along the Y axis.</param>
        /// <param name="startingPosition">The pixel world position at which the projectile will start moving.</param>
        /// <param name="location">The location containing the projectile.</param>
        /// <param name="owner">The character who fired the projectile.</param>
        /// <param name="hitsMonsters">Whether this projectile should hit monsters.</param>
        /// <param name="playDefaultSoundOnFire">Whether to play the fire sound immediately.</param>
        /// <param name="sound">The sound to play when the projectile is fired.</param>
        /// <param name="explosion">Whether this projectile should explode on monster impact.</param>
        public SpellProjectile(int damage, string debuff, int spriteIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float xVelocity, float yVelocity, Vector2 startingPosition, GameLocation location = null, Character owner = null, bool hitsMonsters = false, bool playDefaultSoundOnFire = true, string sound = "", bool explosion = false)
            : this()
        {
            this.theOneWhoFiredMe.Set(location, owner);
            this.Debuff.Value = debuff;
            this.currentTileSheetIndex.Value = spriteIndex;
            this.bouncesLeft.Value = bouncesTillDestruct;
            this.tailLength.Value = tailLength;
            this.rotationVelocity.Value = rotationVelocity;
            this.xVelocity.Value = xVelocity;
            this.yVelocity.Value = yVelocity;
            this.position.Value = startingPosition;
            this.damagesMonsters.Value = hitsMonsters;
            this.DamageToFarmer.Value = damage;
            this.ExplodesOnImpact.Value = explosion;

            if (playDefaultSoundOnFire)
            {
                string sfx = string.IsNullOrEmpty(sound) ? "debuffSpell" : sound;

                if (location == null)
                    Game1.playSound(sfx);
                else
                    location.playSound(sfx);
            }
        }


        /*********
        ** Public methods
        *********/
        protected override void InitNetFields()
        {
            base.InitNetFields();
            this.NetFields
                .AddField(this.Debuff, "debuff")
                .AddField(this.WavyMotion, "wavyMotion")
                .AddField(this.DebuffIntensity, "debuffIntensity")
                .AddField(this.DamageToFarmer, "damageToFarmer")
                .AddField(this.ExplodesOnImpact, "explodesOnImpact");
        }

        public override void updatePosition(GameTime time)
        {
            this.xVelocity.Value += this.acceleration.X;
            this.yVelocity.Value += this.acceleration.Y;
            this.position.X += this.xVelocity.Value;
            this.position.Y += this.yVelocity.Value;

            if (this.WavyMotion.Value)
            {
                this.position.X += (float)Math.Sin(time.TotalGameTime.Milliseconds * Math.PI / 128.0) * 8f;
                this.position.Y += (float)Math.Cos(time.TotalGameTime.Milliseconds * Math.PI / 128.0) * 8f;
            }
        }

        public override bool update(GameTime time, GameLocation location)
        {
            // Custom periodic trail visuals should only be broadcast by the caster-owned projectile.
            // Remote clients receive those sprites through Stardew's normal temporary sprite sync.
            if (this.IsOwnedByLocalPlayer(location) && this.Debuff.Value == "frozen")
            {
                this.PeriodicEffectTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
                if (this.PeriodicEffectTimer > 50f)
                {
                    this.PeriodicEffectTimer = 0f;
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("TileSheets\\Projectiles", new Rectangle(32, 32, 16, 16), 9999f, 1, 1, this.position.Value, flicker: false, flipped: false, 1f, 0.01f, Color.White, 4f, 0f, 0f, 0f)
                    {
                        motion = Utility.getRandom360degreeVector(1f) + new Vector2(this.xVelocity.Value, this.yVelocity.Value),
                        drawAboveAlwaysFront = true
                    });
                }
            }

            return base.update(time, location);
        }

        public override void behaviorOnCollisionWithPlayer(GameLocation location, Farmer player)
        {
            if (this.damagesMonsters.Value)
                return;

            // Player damage is personal state, so only the local target should apply it.
            if (player != Game1.player)
                return;

            if (this.Debuff.Value != null && player.CanBeDamaged() && Game1.random.Next(11) >= player.Immunity && !player.hasBuff("28") && !player.hasTrinketWithID("BasiliskPaw"))
                player.applyBuff(this.Debuff.Value);

            if (player.CanBeDamaged())
                this.piercesLeft.Value--;

            player.takeDamage(this.DamageToFarmer.Value, overrideParry: false, null);
            this.BroadcastImpactAnimation(location);
        }

        public override void behaviorOnCollisionWithTerrainFeature(TerrainFeature terrainFeature, Vector2 tileLocation, GameLocation location)
        {
            if (!this.IsOwnedByLocalPlayer(location))
                return;

            this.BroadcastImpactAnimation(location);
            this.piercesLeft.Value--;
        }

        public override void behaviorOnCollisionWithOther(GameLocation location)
        {
            if (!this.IsOwnedByLocalPlayer(location))
                return;

            this.BroadcastImpactAnimation(location);
            this.piercesLeft.Value--;
        }

        public override void behaviorOnCollisionWithMonster(NPC npc, GameLocation location)
        {
            if (!this.damagesMonsters.Value)
                return;

            // Only the caster-owned projectile should apply monster damage, EXP, debuffs, explosions, or impact sprites.
            if (!this.IsOwnedByLocalPlayer(location))
                return;

            Farmer playerWhoFiredMe = this.GetPlayerWhoFiredMe(location);
            this.BroadcastImpactAnimation(location);

            if (npc is not Monster mob)
                return;

            location.damageMonster(npc.GetBoundingBox(), this.DamageToFarmer.Value, this.DamageToFarmer.Value + 1, isBomb: false, playerWhoFiredMe, isProjectile: true);
            Utilities.AddEXP(playerWhoFiredMe, this.DamageToFarmer.Value >> 2);

            if (this.currentTileSheetIndex.Value == 15)
                Utility.addRainbowStarExplosion(location, this.position.Value, 11);

            if (!mob.IsInvisible)
            {
                this.piercesLeft.Value--;

                if (this.ExplodesOnImpact.Value)
                    location.explode(new Vector2(npc.Tile.X, npc.Tile.Y), 3, playerWhoFiredMe, damage_amount: this.DamageToFarmer.Value);
            }

            if (this.Debuff.Value == "frozen" && (!(npc is Leaper leaper) || !leaper.leaping.Value))
            {
                if (mob.stunTime.Value < 51)
                    this.piercesLeft.Value--;

                if (mob.stunTime.Value < this.DebuffIntensity.Value - 1000)
                {
                    location.playSound("frozen");
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(118, 227, 16, 13), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
                    {
                        layerDepth = (npc.StandingPixel.Y + 2) / 10000f,
                        animationLength = 1,
                        interval = this.DebuffIntensity.Value,
                        scale = 4f,
                        id = (int)(npc.position.X * 777f + npc.position.Y * 77777f),
                        positionFollowsAttachedCharacter = true,
                        attachedCharacter = npc
                    });
                }

                mob.stunTime.Value = this.DebuffIntensity.Value;
            }
        }

        /// <summary>Get the player who fired this projectile.</summary>
        /// <param name="location">The location containing the player.</param>
        public virtual Farmer GetPlayerWhoFiredMe(GameLocation location)
        {
            return this.theOneWhoFiredMe.Get(location) as Farmer ?? Game1.player;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Broadcast the projectile's impact animation through Stardew's native temporary sprite sync.</summary>
        /// <param name="location">The projectile location.</param>
        private void BroadcastImpactAnimation(GameLocation location)
        {
            if (this.Debuff.Value != "frozen")
                Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(352, Game1.random.Next(100, 150), 2, 1, this.position.Value, flicker: false, flipped: false));
        }

        /// <summary>Get whether this machine owns gameplay for this projectile.</summary>
        /// <param name="location">The projectile location.</param>
        private bool IsOwnedByLocalPlayer(GameLocation location)
        {
            return this.GetPlayerWhoFiredMe(location)?.IsLocalPlayer == true;
        }
    }
}
