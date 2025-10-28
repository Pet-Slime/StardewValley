using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using SpaceCore;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class Shockwave : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly Farmer Player;
        private readonly int Level;

        private bool Jumping = true;
        private float PrevJumpVel;
        private float LandX;
        private float LandY;
        private float Timer;
        private int CurrRad;


        /*********
        ** Public methods
        *********/
        public Shockwave(Farmer player, int level)
        {
            this.Player = player;
            this.Level = level;
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Jumping)
            {
                if (this.Player.yJumpVelocity == 0 && this.PrevJumpVel < 0)
                {
                    this.LandX = this.Player.position.X;
                    this.LandY = this.Player.position.Y;
                    this.Jumping = false;
                }
                this.PrevJumpVel = this.Player.yJumpVelocity;
            }
            if (!this.Jumping)
            {
                if (--this.Timer > 0)
                {
                    return true;
                }
                this.Timer = 10;

                int spotsForCurrRadius = 1 + this.CurrRad * 7;
                for (int i = 0; i < spotsForCurrRadius; ++i)
                {
                    Vector2 pixelPos = new(
                        x: this.LandX + (float)Math.Cos(Math.PI * 2 / spotsForCurrRadius * i) * this.CurrRad * Game1.tileSize,
                        y: this.LandY + (float)Math.Sin(Math.PI * 2 / spotsForCurrRadius * i) * this.CurrRad * Game1.tileSize
                    );

                    var loc = this.Player.currentLocation;
                    loc.playSound("hoeHit", pixelPos);
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 30));
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, pixelPos, Color.White, 8, Game1.random.NextDouble() < 0.5, 50f));
                }
                ++this.CurrRad;

                foreach (var character in this.Player.currentLocation.characters)
                {
                    if (character is Monster mob)
                    {
                        if (Vector2.Distance(new Vector2(this.LandX, this.LandY), mob.position.Value) < this.CurrRad * Game1.tileSize)
                        {
                            int baseDMG = (this.Level + 1) * 5 * (this.Player.CombatLevel + 1 + this.Player.GetCustomBuffedSkillLevel(MagicConstants.SkillName));
                            int minDMG = (int)(baseDMG * 0.75);
                            int maxDMG = (int)(baseDMG * 1.5);
                            this.Player.currentLocation.damageMonster(mob.GetBoundingBox(), minDMG, maxDMG, false, this.Player);
                            Utilities.AddEXP(this.Player, 3);
                        }
                    }
                }

                if (this.CurrRad >= 1 + (this.Level + 1) * 2)
                    return false;
            }

            return true;
        }

        public void CleanUp()
        {

        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch) { }
    }
}
