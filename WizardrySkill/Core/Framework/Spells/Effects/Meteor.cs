using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private readonly float YVelocity = 64f;
        private float Height = 1000f;


        /*********
        ** Public methods
        *********/
        public Meteor(Farmer theSource, int tx, int ty)
        {
            this.Loc = theSource.currentLocation;
            this.Source = theSource;

            this.Position.X = tx;
            this.Position.Y = ty;

            // Only the caster's own machine should create and broadcast the falling visual.
            // Remote clients see this through Stardew's normal TemporaryAnimatedSprite sync.
            if (this.Source.IsLocalPlayer)
                this.BroadcastMeteorSprite();
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.Loc == null || this.Source == null)
                return false;

            // Decrease height until the meteor reaches the ground.
            this.Height -= this.YVelocity;
            if (this.Height > 0)
                return true;

            this.OnImpact();
            return false;
        }

        public void CleanUp()
        {
            // No persistent sprite/light resources to remove.
        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Nothing to draw manually.
            // The falling meteor visual is handled by Game1.Multiplayer.broadcastSprites.
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Broadcast the falling meteor visual through Stardew's native temporary sprite sync.</summary>
        private void BroadcastMeteorSprite()
        {
            if (this.Loc == null)
                return;

            Vector2 startPos = this.Position;
            startPos.Y -= this.Height;

            // numberOfLoops is equal to how many ticks it will take to reach ground level,
            // matching the old CrabRave falling-object pattern.
            TemporaryAnimatedSprite sprite = new(
                textureName: Game1.objectSpriteSheetName,
                sourceRect: new Rectangle(352, 400, 32, 32),
                animationInterval: 1f,
                animationLength: 1,
                numberOfLoops: (int)(this.Height / this.YVelocity),
                position: startPos,
                flicker: false,
                flipped: false)
            {
                scale = 10f,
                color = Color.White,
                layerDepth = 1f,
                motion = new Vector2(0f, this.YVelocity),
                rotation = (float)(Rand.NextDouble() * Math.PI * 2),
                rotationChange = MathF.PI / 16f
            };

            Game1.Multiplayer.broadcastSprites(this.Loc, sprite);
        }

        private void OnImpact()
        {


            // Only the caster-owned gameplay meteor should mutate the world.
            // Remote machines see the broadcast temporary sprite but must not run damage, debris, EXP, or explosion logic.
            if (!this.Source.IsLocalPlayer)
                return;


            // Play explosion sound at impact.
            this.Source.currentLocation.playSound("explosion", this.Position / Game1.tileSize);

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
