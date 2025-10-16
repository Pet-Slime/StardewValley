using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace WizardrySkill.Core.Framework.Game
{
    public class SpellProjectile : Projectile
    {
        //
        // Summary:
        //     The buff ID to apply to players hit by this projectile.
        public readonly NetString debuff = new NetString();

        public NetBool wavyMotion = new NetBool(value: true);

        public NetInt debuffIntensity = new NetInt(-1);


        //
        // Summary:
        //     The amount of damage caused when this projectile hits a monster or player.
        public readonly NetInt damageToFarmer = new NetInt();

        private float periodicEffectTimer;
        private bool Explosion;

        //
        // Summary:
        //     Construct an empty instance.
        public SpellProjectile()
        {
        }

        //
        // Summary:
        //     Construct an instance.
        //
        // Parameters:
        //   debuff:
        //     The debuff ID to apply to players hit by this projectile.
        //
        //   spriteIndex:
        //     The index of the sprite to draw in StardewValley.Projectiles.Projectile.projectileSheetName.
        //
        //
        //   bouncesTillDestruct:
        //     The number of times the projectile can bounce off walls before being destroyed.
        //
        //
        //   tailLength:
        //     The length of the tail which trails behind the main projectile.
        //
        //   rotationVelocity:
        //     The rotation velocity.
        //
        //   xVelocity:
        //     The speed at which the projectile moves along the X axis.
        //
        //   yVelocity:
        //     The speed at which the projectile moves along the Y axis.
        //
        //   startingPosition:
        //     The pixel world position at which the projectile will start moving.
        //
        //   location:
        //     The location containing the projectile.
        //
        //   owner:
        //     The character who fired the projectile.
        public SpellProjectile(int damage, string debuff, int spriteIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float xVelocity, float yVelocity, Vector2 startingPosition, GameLocation location = null, Character owner = null, bool hitsMonsters = false, bool playDefaultSoundOnFire = true, string sound = "", bool explosion = false)
            : this()
        {
            this.theOneWhoFiredMe.Set(location, owner);
            this.debuff.Value = debuff;
            this.currentTileSheetIndex.Value = spriteIndex;
            this.bouncesLeft.Value = bouncesTillDestruct;
            base.tailLength.Value = tailLength;
            base.rotationVelocity.Value = rotationVelocity;
            base.xVelocity.Value = xVelocity;
            base.yVelocity.Value = yVelocity;
            this.position.Value = startingPosition;
            this.damagesMonsters.Value = hitsMonsters;
            this.damageToFarmer.Value = damage;
            this.Explosion = explosion;

            if (playDefaultSoundOnFire)
            {
                string sfx = string.IsNullOrEmpty(sound) ? "debuffSpell" : sound;

                if (location == null)
                    Game1.playSound(sfx);
                else
                    location.playSound(sfx);
            }




        }

        protected override void InitNetFields()
        {
            base.InitNetFields();
            NetFields.AddField(debuff, "debuff").AddField(wavyMotion, "wavyMotion").AddField(debuffIntensity, "debuffIntensity").AddField(damageToFarmer, "damageToFarmer");
        }

        public override void updatePosition(GameTime time)
        {
            this.xVelocity.Value += this.acceleration.X;
            this.yVelocity.Value += this.acceleration.Y;
            this.position.X += this.xVelocity.Value;
            this.position.Y += this.yVelocity.Value;
            if (this.wavyMotion.Value)
            {
                this.position.X += (float)Math.Sin(time.TotalGameTime.Milliseconds * Math.PI / 128.0) * 8f;
                this.position.Y += (float)Math.Cos(time.TotalGameTime.Milliseconds * Math.PI / 128.0) * 8f;
            }
        }

        public override bool update(GameTime time, GameLocation location)
        {

            if (debuff.Value == "frozen")
            {
                periodicEffectTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
                if (periodicEffectTimer > 50f)
                {
                    periodicEffectTimer = 0f;
                    location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\Projectiles", new Rectangle(32, 32, 16, 16), 9999f, 1, 1, position.Value, flicker: false, flipped: false, 1f, 0.01f, Color.White, 4f, 0f, 0f, 0f)
                    {
                        motion = Utility.getRandom360degreeVector(1f) + new Vector2(xVelocity.Value, yVelocity.Value),
                        drawAboveAlwaysFront = true
                    });
                }
            }

            return base.update(time, location);
        }

        public override void behaviorOnCollisionWithPlayer(GameLocation location, Farmer player)
        {
            if (!damagesMonsters.Value && Game1.random.Next(11) >= player.Immunity && !player.hasBuff("28") && !player.hasTrinketWithID("BasiliskPaw"))
            {
                piercesLeft.Value--;
                if (Game1.player == player)
                {
                    player.applyBuff(debuff.Value);
                }

                explosionAnimation(location);
                if (debuff.Value == "19")
                {
                    location.playSound("frozen");
                }
                else
                {
                    location.playSound("debuffHit");
                }
            }
        }

        public override void behaviorOnCollisionWithTerrainFeature(TerrainFeature t, Vector2 tileLocation, GameLocation location)
        {
            explosionAnimation(location);

            this.piercesLeft.Value--;

        }

        public override void behaviorOnCollisionWithOther(GameLocation location)
        {
            explosionAnimation(location);
            this.piercesLeft.Value--;

        }

        protected virtual void explosionAnimation(GameLocation location)
        {
            if (!(debuff.Value == "frozen"))
            {
                Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(352, Game1.random.Next(100, 150), 2, 1, position.Value, flicker: false, flipped: false));
            }
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {

            if (n is Monster)
            {
                Farmer playerWhoFiredMe = GetPlayerWhoFiredMe(location);
                location.damageMonster(n.GetBoundingBox(), this.damageToFarmer.Value, this.damageToFarmer.Value + 1, isBomb: false, playerWhoFiredMe, isProjectile: true);
                Utilities.AddEXP(GetPlayerWhoFiredMe(location), this.damageToFarmer.Value / ((GetPlayerWhoFiredMe(location) as Farmer).CombatLevel + 1));
                if (this.currentTileSheetIndex.Value == 15)
                {
                    Utility.addRainbowStarExplosion(location, this.position.Value, 11);
                }

                if (!(n as Monster).IsInvisible)
                {
                    this.piercesLeft.Value--;
                    if (this.Explosion)
                    {
                        location.explode(new Vector2(n.Tile.X, n.Tile.Y), 3, GetPlayerWhoFiredMe(location), damage_amount: this.damageToFarmer.Value);
                    }
                }
            }


            if (damagesMonsters.Value && n is Monster && debuff.Value == "frozen" && (!(n is Leaper leaper) || !leaper.leaping.Value))
            {
                if ((n as Monster).stunTime.Value < 51)
                {
                    this.piercesLeft.Value--;
                }

                if ((n as Monster).stunTime.Value < debuffIntensity.Value - 1000)
                {
                    location.playSound("frozen");
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(118, 227, 16, 13), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
                    {
                        layerDepth = (n.StandingPixel.Y + 2) / 10000f,
                        animationLength = 1,
                        interval = debuffIntensity.Value,
                        scale = 4f,
                        id = (int)(n.position.X * 777f + n.position.Y * 77777f),
                        positionFollowsAttachedCharacter = true,
                        attachedCharacter = n
                    });
                }

                (n as Monster).stunTime.Value = debuffIntensity.Value;
            }
        }

        //
        // Summary:
        //     Get the player who fired this projectile.
        //
        // Parameters:
        //   location:
        //     The location containing the player.
        public virtual Farmer GetPlayerWhoFiredMe(GameLocation location)
        {
            return theOneWhoFiredMe.Get(location) as Farmer ?? Game1.player;
        }
    }
}
