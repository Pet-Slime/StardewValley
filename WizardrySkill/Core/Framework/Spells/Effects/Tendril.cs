using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class Tendril : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly GameLocation Location;
        private readonly Monster Mob;
        private readonly Vector2 Pos;
        private readonly float Radius;
        private readonly Texture2D Tex;
        private readonly bool AllowMonsterPositionMutation;
        private int Duration;


        /*********
        ** Public methods
        *********/
        public Tendril(GameLocation location, Monster theMob, Vector2 pos, float rad, int dur, bool allowMonsterPositionMutation)
        {
            this.Location = location;
            this.Mob = theMob;
            this.Pos = pos;
            this.Radius = rad;
            this.Duration = dur;
            this.AllowMonsterPositionMutation = allowMonsterPositionMutation;
            this.Tex = Content.LoadTexture("magic/nature/tendrils/tendril");
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Mob == null || this.Location == null)
                return false;

            // Only the chosen mutation authority should constrain monster position.
            // Other machines still keep the tendril alive locally so it can draw the visual effect.
            if (this.AllowMonsterPositionMutation)
            {
                Vector2 mobPos = new(this.Mob.GetBoundingBox().Center.X, this.Mob.GetBoundingBox().Center.Y);
                if (Vector2.Distance(mobPos, this.Pos) >= this.Radius)
                {
                    Vector2 offset = this.Mob.position.Value - this.Pos;

                    if (offset.LengthSquared() > 0.001f)
                    {
                        offset.Normalize();
                        offset *= this.Radius;
                        this.Mob.position.Value = this.Pos + offset;
                    }
                }
            }

            return --this.Duration > 0;
        }

        public void CleanUp()
        {
            // No persistent sprite/light resources to remove.
        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (this.Mob == null || this.Location == null || !this.IsCurrentLocation())
                return;

            Vector2 mobPos = new(this.Mob.GetBoundingBox().Center.X, this.Mob.GetBoundingBox().Center.Y);
            float dist = Vector2.Distance(mobPos, this.Pos);
            Rectangle r = new((int)this.Pos.X, (int)this.Pos.Y, 10, (int)dist);
            r = Game1.GlobalToLocal(Game1.viewport, r);
            float rot = (float)-Math.Atan2(this.Pos.Y - mobPos.Y, mobPos.X - this.Pos.X);
            spriteBatch.Draw(this.Tex, r, new Rectangle(0, 0, 10, 12), Color.White, rot - 3.14f / 2, new Vector2(5, 0), SpriteEffects.None, 1);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether this tendril's location is currently being viewed locally.</summary>
        private bool IsCurrentLocation()
        {
            return ReferenceEquals(Game1.currentLocation, this.Location)
                || Game1.currentLocation?.Name == this.Location.Name;
        }
    }
}
