using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using Color = Microsoft.Xna.Framework.Color;
using Object = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells.Effects
{
    /// <summary>
    /// A magical effect that rains crabs (or fish-like items) from the sky, 
    /// creating explosions or summoning Rock Crabs on impact.
    /// </summary>
    public class CrabRave : IActiveEffect
    {
        /*********
        ** Fields
        *********/

        /// <summary>The location where the effect occurs.</summary>
        private readonly GameLocation Loc;

        /// <summary>The player who cast the spell.</summary>
        private readonly Farmer Source;

        /// <summary>The item (fish or crab) used for the falling visuals and debris.</summary>
        public Object Item;

        /// <summary>Random generator for variation between casts.</summary>
        private static readonly Random Rand = new();

        /// <summary>The pixel-based world positions of each falling object.</summary>
        private readonly List<Vector2> Positions;

        /// <summary>The current height above the ground for each falling object.</summary>
        private readonly List<float> Heights;

        /// <summary>The downward velocity per update tick (in pixels).</summary>
        private readonly float YVelocity = 32f;

        /// <summary>Metadata about the item texture and source rect for rendering.</summary>
        public ParsedItemData ItemData { get; }

        /*********
        ** Constructor
        *********/
        /// <summary>
        /// Create a new instance of the CrabRave spell effect.
        /// </summary>
        /// <param name="theSource">The player casting the spell.</param>
        /// <param name="fish">The item used to represent the falling object.</param>
        /// <param name="tiles">A list of tile positions where crabs will fall.</param>
        public CrabRave(Farmer theSource, StardewValley.Item fish, List<Vector2> tiles)
        {
            this.Loc = theSource.currentLocation;
            this.Source = theSource;
            this.Item = (Object)fish;
            this.ItemData = ItemRegistry.GetDataOrErrorItem(fish.QualifiedItemId);

            this.Positions = new List<Vector2>();
            this.Heights = new List<float>();

            // Create a falling object for each target tile
            foreach (var tile in tiles)
            {
                // Convert tile coordinate to pixel coordinate
                this.Positions.Add(tile * Game1.tileSize);

                // Assign a random initial height between 700–1400 pixels
                float randomHeight = Rand.Next(700, 1400);
                this.Heights.Add(randomHeight);

                // Determine the starting position above the target tile
                var startPos = tile * Game1.tileSize;
                startPos.Y -= randomHeight;

                // Create a falling temporary sprite for visual effect
                // `numberOfLoops` is equal to how many ticks it will take to reach ground level
                var sprite = new TemporaryAnimatedSprite(
                    textureName: this.ItemData.TextureName,
                    sourceRect: this.ItemData.GetSourceRect(),
                    animationInterval: 1f,
                    animationLength: 1,
                    numberOfLoops: (int)(randomHeight / this.YVelocity),
                    position: startPos,
                    flicker: false,
                    flipped: false)
                {
                    scale = 4f,
                    color = Color.White,
                    layerDepth = 1f,
                    motion = new Vector2(0, this.YVelocity),
                    rotation = (float)(Rand.NextDouble() * Math.PI * 2),
                    rotationChange = MathF.PI / 16f,
                };

                // Broadcast ensures all clients see the sprite fall in multiplayer
                Game1.Multiplayer.broadcastSprites(this.Loc, sprite);
            }

            // Create a pair of particle bursts around the player’s position for visual flair
            var point = this.Source.StandingPixel;

            // Offset to appear above and slightly to the side of the player
            point.X -= this.Source.Sprite.SpriteWidth * 2;
            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 1.5);

            // First orange burst
            Game1.Multiplayer.broadcastSprites(this.Source.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

            // Second burst higher up for a layered effect
            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 2.5);
            Game1.Multiplayer.broadcastSprites(this.Source.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));
        }

        /*********
        ** Update
        *********/
        /// <summary>
        /// Called every tick to update falling motion and trigger impact effects.
        /// </summary>
        /// <returns><see langword="true"/> if at least one object is still falling; otherwise, <see langword="false"/>.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            // Prevent running with invalid data
            if (this.ItemData.IsErrorItem)
                return false;

            bool anyActive = false;

            // Process each falling object
            for (int i = 0; i < this.Positions.Count; i++)
            {
                if (this.Heights[i] > 0)
                {
                    // Move downward
                    this.Heights[i] -= this.YVelocity;
                    anyActive = true;

                    // When the object hits the ground
                    if (this.Heights[i] <= 0 && Game1.player == Source)
                    {
                        Vector2 pos = this.Positions[i];

                        // Play explosion sound at impact
                        this.Loc.LocalSoundAtPixel("explosion", pos);

                        // Create debris using the item texture for visual fragments
                        for (int j = 0; j < 5; j++)
                        {
                            for (int x = -j; x <= j; x++)
                            {
                                for (int y = -j; y <= j; y++)
                                {
                                    Game1.createRadialDebris(
                                        this.Loc,
                                        this.ItemData.TextureName,
                                        this.ItemData.GetSourceRect(),
                                        4,
                                        (int)pos.X + x * 20,
                                        (int)pos.Y + y * 20,
                                        1 + Rand.Next(1),
                                        (int)(pos.Y / Game1.tileSize) + 1,
                                        Color.White,
                                        4f
                                    );
                                }
                            }
                        }

                        // Randomly determine what happens on impact
                        float roll = (float)Rand.NextDouble(); // 0.0–1.0

                        if (roll < 0.6f)
                        {
                            // 60%: small explosion at the tile
                            var spot = new Vector2((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                            this.Loc.explode(spot, 3, this.Source);
                        }
                        else if (roll < 0.95f)
                        {
                            // 30%: summon a Rock Crab (host sync only)
                            var rocky = new RockCrab(pos);
                            rocky.shellGone.Value = true;
                            this.Loc.addCharacter(rocky);
                        }
                        else
                        {
                            // 10%: spawn a crab item as loot
                            var spot = new Vector2((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                            Game1.createMultipleObjectDebris(this.ItemData.ItemId, (int)spot.X, (int)spot.Y, 1, this.Source.UniqueMultiplayerID);
                        }
                    }
                }
            }

            // Return true if any are still falling
            return anyActive;
        }

        /*********
        ** Draw
        *********/
        /// <summary>
        /// Draw logic is not needed here since all visuals are handled via TemporaryAnimatedSprite.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Nothing to draw manually
        }

        /*********
        ** Cleanup
        *********/
        /// <summary>
        /// Clean up lingering effects (currently unused, placeholder for future improvements).
        /// </summary>
        public void CleanUp()
        {
            // No persistent state to clean up
        }
    }
}
