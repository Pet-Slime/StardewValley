using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    public class Meteor : IActiveEffect
    {
        /*********
        ** Fields
        *********/
        private readonly GameLocation Loc;
        private readonly Farmer Source;
        private static readonly Random Rand = new();
        private readonly Vector2 Position;
        private readonly float YVelocity;
        private float Height = 1000;


        /*********
        ** Public methods
        *********/
        public Meteor(Farmer theSource, int tx, int ty)
        {
            this.Loc = theSource.currentLocation;
            this.Source = theSource;

            this.Position.X = tx;
            this.Position.Y = ty;
            this.YVelocity = 64;
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Loc == null || this.Source == null)
                return false;

            // decrease height until zero
            this.Height -= this.YVelocity;
            if (this.Height > 0)
                return true;

            this.OnImpact();
            return false;
        }

        public void CleanUp()
        {
        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 drawPos = Game1.GlobalToLocal(new Vector2(this.Position.X, this.Position.Y - this.Height));
            spriteBatch.Draw(Game1.objectSpriteSheet, drawPos, new Rectangle(352, 400, 32, 32), Color.White, 0, new Vector2(16, 16), 10f, SpriteEffects.None, (float)((this.Position.Y - this.Height + Game1.tileSize * 3 / 2) / 10000.0));
        }


        /*********
        ** Private methods
        *********/
        private void OnImpact()
        {
            // Local sound is safe. It does not mutate synced world state.
            this.Loc.LocalSoundAtPixel("explosion", this.Position);

            // Only the host should create debris, damage monsters, grant EXP, or explode the location.
            if (!Context.IsMainPlayer)
                return;

            this.CreateImpactDebris();
            this.DamageNearbyMonsters();

            this.Loc.explode(new Vector2((int)this.Position.X / Game1.tileSize, (int)this.Position.Y / Game1.tileSize), 6, this.Source);
        }

        private void CreateImpactDebris()
        {
            for (int i = 0; i < 5; ++i)
            {
                for (int x = -i; x <= i; ++x)
                {
                    for (int y = -i; y <= i; ++y)
                    {
                        Game1.createRadialDebris(this.Loc, Game1.objectSpriteSheetName, new Rectangle(352, 400, 32, 32), 4, (int)this.Position.X + x * 20, (int)this.Position.Y + y * 20, 1 + Rand.Next(1), (int)(this.Position.Y / Game1.tileSize) + 1, Color.White, 4f);
                    }
                }
            }
        }

        private void DamageNearbyMonsters()
        {
            foreach (var npc in this.Loc.characters)
            {
                if (npc is not Monster mob)
                    continue;

                float rad = 8 * Game1.tileSize;
                if (Vector2.Distance(mob.position.Value, this.Position) > rad)
                    continue;

                // TODO: Use location damage method for xp and quest progress
                mob.takeDamage(300, 0, 0, false, 0, this.Source);
                Utilities.AddEXP(this.Source, 5);
            }
        }
    }
}
