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
    /// A magical effect that rains crabs from the sky.
    /// The caster-owned effect broadcasts falling visuals through Stardew's temporary sprite sync,
    /// then applies impact gameplay only on the caster's machine.
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

        /// <summary>Random generator for variation between casts.</summary>
        private static readonly Random Rand = new();

        /// <summary>The pixel-based world positions of each falling object.</summary>
        private readonly List<Vector2> Positions;

        /// <summary>The current height above the ground for each falling object.</summary>
        private readonly List<float> Heights;

        /// <summary>The downward velocity per update tick, in pixels.</summary>
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

            if (this.ItemData.IsErrorItem || tiles == null || tiles.Count == 0)
                return;

            // Create a falling object for each target tile.
            foreach (Vector2 tile in tiles)
            {
                // Convert tile coordinate to pixel coordinate.
                Vector2 position = tile * Game1.tileSize;
                this.Positions.Add(position);

                // Assign a random initial height between 700–1400 pixels.
                float randomHeight = Rand.Next(700, 1400);
                this.Heights.Add(randomHeight);

                // Only the caster's own machine should create and broadcast the falling visual.
                // Remote clients receive these through Stardew's normal TemporaryAnimatedSprite sync.
                if (this.Source.IsLocalPlayer)
                    this.BroadcastFallingCrabSprite(position, randomHeight);
            }

            if (this.Source.IsLocalPlayer)
                this.BroadcastCasterBurstVisuals();
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

            // Process each falling object.
            for (int i = 0; i < this.Positions.Count; i++)
            {
                if (this.Heights[i] <= 0)
                    continue;

                // Move downward.
                this.Heights[i] -= this.YVelocity;
                anyActive = true;

                // When the object hits the ground.
                if (this.Heights[i] <= 0)
                    this.HandleImpact(this.Positions[i]);
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

        /// <summary>Broadcast one falling crab visual through Stardew's native temporary sprite sync.</summary>
        /// <param name="position">The impact position in pixels.</param>
        /// <param name="height">The initial height above the impact position.</param>
        private void BroadcastFallingCrabSprite(Vector2 position, float height)
        {
            Vector2 startPos = position;
            startPos.Y -= height;

            // numberOfLoops is equal to how many ticks it will take to reach ground level.
            TemporaryAnimatedSprite sprite = new(
                textureName: this.ItemData.TextureName,
                sourceRect: this.ItemData.GetSourceRect(),
                animationInterval: 1f,
                animationLength: 1,
                numberOfLoops: (int)(height / this.YVelocity),
                position: startPos,
                flicker: false,
                flipped: false)
            {
                scale = 4f,
                color = Color.White,
                layerDepth = 1f,
                motion = new Vector2(0f, this.YVelocity),
                rotation = (float)(Rand.NextDouble() * Math.PI * 2),
                rotationChange = MathF.PI / 16f
            };

            Game1.Multiplayer.broadcastSprites(this.Loc, sprite);
        }

        /// <summary>Add caster burst visuals through Stardew's native temporary sprite sync.</summary>
        private void BroadcastCasterBurstVisuals()
        {
            Point point = this.Source.StandingPixel;

            // Offset to appear above and slightly to the side of the player.
            point.X -= this.Source.Sprite.SpriteWidth * 2;
            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 1.5);

            Game1.Multiplayer.broadcastSprites(this.Source.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    Rand.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

            point.Y -= (int)(this.Source.Sprite.SpriteHeight * 2.5);

            Game1.Multiplayer.broadcastSprites(this.Source.currentLocation,
                new TemporaryAnimatedSprite(
                    10,
                    point.ToVector2(),
                    Color.Orange,
                    10,
                    Rand.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));
        }

        /// <summary>Handle one crab impact.</summary>
        /// <param name="pos">The impact position in pixels.</param>
        private void HandleImpact(Vector2 pos)
        {

            // Only the caster-owned CrabRave should mutate gameplay state.
            // Remote clients receive falling sprites through Stardew, but should not run this effect.
            if (!this.Source.IsLocalPlayer)
                return;


            // Play explosion sound at impact.
            this.Source.currentLocation.playSound("explosion", pos / Game1.tileSize);

            // Create debris using the item texture for visual fragments.
            this.CreateImpactDebris(pos);

            // Randomly determine what happens on impact.
            float roll = (float)Rand.NextDouble();

            if (roll < 0.6f)
            {
                // 60%: small explosion at the tile.
                Vector2 spot = new((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                this.Loc.explode(spot, 3, this.Source);
            }
            else if (roll < 0.95f)
            {
                // 35%: summon a Rock Crab.
                RockCrab rocky = new(pos)
                {
                    wildernessFarmMonster = true,
                    focusedOnFarmers = true
                };
                rocky.shellGone.Value = true;
                this.Loc.addCharacter(rocky);
            }
            else
            {
                // 5%: spawn a crab item as loot.
                Vector2 spot = new((int)pos.X / Game1.tileSize, (int)pos.Y / Game1.tileSize);
                Game1.createMultipleObjectDebris(this.ItemData.ItemId, (int)spot.X, (int)spot.Y, 1, this.Source.UniqueMultiplayerID);
            }
        }

        /// <summary>Create caster-owned impact debris.</summary>
        /// <param name="pos">The impact position in pixels.</param>
        private void CreateImpactDebris(Vector2 pos)
        {
            for (int j = 0; j < 2; j++)
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
        }
    }
}
