using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
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
    /// A magical effect that rains crabs from the sky.
    /// Falling visuals are created locally on each client from synced tile data.
    /// World-changing impact effects are only created by the host.
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

        /// <summary>The item used for the falling visuals and debris.</summary>
        public Object Item;

        /// <summary>The pixel-based world positions of each falling object.</summary>
        private readonly List<Vector2> Positions;

        /// <summary>The current height above the ground for each falling object.</summary>
        private readonly List<float> Heights;

        /// <summary>The downward velocity per update tick, in pixels.</summary>
        private readonly float YVelocity = 32f;

        /// <summary>Random generator for deterministic variation for this specific effect.</summary>
        private readonly Random Rand;

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
            this.Rand = new Random(GetSeed(theSource, tiles));

            if (this.ItemData.IsErrorItem || tiles == null || tiles.Count == 0)
                return;

            foreach (Vector2 tile in tiles)
            {
                Vector2 position = tile * Game1.tileSize;
                this.Positions.Add(position);

                float randomHeight = this.Rand.Next(700, 1400);
                this.Heights.Add(randomHeight);

                Vector2 startPos = position;
                startPos.Y -= randomHeight;

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
                    rotation = (float)(this.Rand.NextDouble() * Math.PI * 2),
                    rotationChange = MathF.PI / 16f
                };

                // Local-only visual. Every client creates this from the synced tile list.
                this.Loc.temporarySprites.Add(sprite);
            }

            this.AddCasterBurstVisuals();
        }


        /*********
        ** Public methods
        *********/

        /// <summary>
        /// Called every tick to update falling motion and trigger impact effects.
        /// </summary>
        /// <returns><see langword="true"/> if at least one object is still falling; otherwise, <see langword="false"/>.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            if (this.ItemData.IsErrorItem)
                return false;

            bool anyActive = false;

            for (int i = 0; i < this.Positions.Count; i++)
            {
                if (this.Heights[i] <= 0)
                    continue;

                this.Heights[i] -= this.YVelocity;
                anyActive = true;

                if (this.Heights[i] <= 0)
                    this.OnImpact(this.Positions[i]);
            }

            return anyActive;
        }

        /// <summary>
        /// Draw logic is not needed here since falling visuals are handled via TemporaryAnimatedSprite.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Nothing to draw manually.
        }

        /// <summary>
        /// Clean up lingering effects.
        /// </summary>
        public void CleanUp()
        {
            // No persistent state to clean up.
        }


        /*********
        ** Private methods
        *********/

        /// <summary>Handle one crab impact.</summary>
        /// <param name="pos">The impact position in pixels.</param>
        private void OnImpact(Vector2 pos)
        {
            // Local sound is safe. It does not mutate synced world state.
            this.Loc.LocalSoundAtPixel("explosion", pos);

            // Only the host should create debris, explosions, monsters, or item drops.
            if (!Context.IsMainPlayer)
                return;

            this.CreateImpactDebris(pos);

            float roll = (float)this.Rand.NextDouble();

            if (roll < 0.6f)
            {
                Vector2 spot = new Vector2((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                this.Loc.explode(spot, 3, this.Source);
            }
            else if (roll < 0.95f)
            {
                var rocky = new RockCrab(pos)
                {
                    wildernessFarmMonster = true,
                    focusedOnFarmers = true
                };
                rocky.shellGone.Value = true;
                this.Loc.addCharacter(rocky);
            }
            else
            {
                Vector2 spot = new Vector2((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                Game1.createMultipleObjectDebris(this.ItemData.ItemId, (int)spot.X, (int)spot.Y, 1, this.Source.UniqueMultiplayerID);
            }
        }

        /// <summary>Create host-owned impact debris.</summary>
        /// <param name="pos">The impact position in pixels.</param>
        private void CreateImpactDebris(Vector2 pos)
        {
            for (int j = 0; j < 3; j++)
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
                            1,
                            (int)(pos.Y / Game1.tileSize) + 1,
                            Color.White,
                            4f
                        );
                    }
                }
            }
        }

        /// <summary>Add local-only caster burst visuals.</summary>
        private void AddCasterBurstVisuals()
        {
            var point = this.Source.StandingPixel;

            point.X -= this.Source.Sprite.SpriteWidth * 2;
            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 1.5);

            this.Source.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    this.Rand.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 2.5);

            this.Source.currentLocation.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    this.Rand.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));
        }



        /// <summary>Get a deterministic seed so every client creates matching local visuals from the same synced tile list.</summary>
        /// <param name="source">The player who cast the spell.</param>
        /// <param name="tiles">The synced target tiles.</param>
        private static int GetSeed(Farmer source, List<Vector2> tiles)
        {
            unchecked
            {
                int seed = 17;
                seed = seed * 31 + source.UniqueMultiplayerID.GetHashCode();

                if (tiles != null)
                {
                    foreach (Vector2 tile in tiles)
                    {
                        seed = seed * 31 + (int)tile.X;
                        seed = seed * 31 + (int)tile.Y;
                    }
                }

                return seed;
            }
        }
    }
}
