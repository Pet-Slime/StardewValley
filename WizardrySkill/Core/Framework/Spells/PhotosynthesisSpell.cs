using System;
using System.Collections.Generic;
using System.Linq;
using MoonShared.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Minigames;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines the "PhotosynthesisSpell", which accelerates the growth of crops, trees, and fruit trees.
    public class PhotosynthesisSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public PhotosynthesisSpell()
            : base(SchoolId.Nature, "photosynthesis")
        {
            // SchoolId.Nature indicates the spell belongs to the Nature school
            // "photosynthesis" is the internal name for this spell
        }

        public override int GetMaxCastingLevel()
        {
            return 3;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 80;
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player has a Prismatic Shard in their inventory
            return base.CanCast(player, level) && player.Items.ContainsId("872", 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only apply effects for the local player
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;

            // Convert pixel coordinates to tile coordinates
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);

            // Get the tiles affected by the spell based on the spell's level
            List<Vector2> list = Utilities.TilesAffected(target, 3 * (level + 1), player);

            int num = 0; // Count of affected tiles

            // Loop through all terrain features (crops, trees, etc.) in the location
            foreach (var entry in location.terrainFeatures.Pairs.ToList())
            {
                bool didAction = false;
                var tf = entry.Value;
                var tile = tf.Tile;

                // If the terrain feature is HoeDirt with a crop
                if (tf is HoeDirt dirt && list.Contains(entry.Key))
                {
                    if (dirt.crop == null || dirt.crop.fullyGrown.Value)
                        continue; // Skip if empty or fully grown
                    dirt.crop.newDay(1); // Advance crop growth by one day
                    didAction = true;
                }

                // If the terrain feature is a Fruit Tree
                if (tf is FruitTree fruitTree && list.Contains(entry.Key))
                {
                    if (fruitTree.daysUntilMature.Value > 0)
                    {
                        didAction = true;
                        // Reduce days until mature by 7 (accelerate growth)
                        fruitTree.daysUntilMature.Value = Math.Max(0, fruitTree.daysUntilMature.Value - 7);

                        // Update growth stage based on remaining days
                        fruitTree.growthStage.Value = fruitTree.daysUntilMature.Value > 21 ? 0 :
                                                      fruitTree.daysUntilMature.Value > 14 ? 1 :
                                                      fruitTree.daysUntilMature.Value > 7 ? 2 :
                                                      fruitTree.daysUntilMature.Value > 0 ? 3 : 4;
                    }
                    else if (!fruitTree.stump.Value && fruitTree.growthStage.Value == 4 &&
                             (fruitTree.IsInSeasonHere() || location.Name == "Greenhouse"))
                    {
                        // If mature, try to spawn up to 3 fruits
                        int fruitCount = fruitTree.fruit.Count;
                        for (int i = fruitCount; i < 3; i++)
                        {
                            fruitTree.TryAddFruit();
                            didAction = true;
                        }
                    }
                }

                // If the terrain feature is a normal tree
                if (tf is Tree tree && list.Contains(entry.Key))
                {
                    if (tree.growthStage.Value < 5)
                    {
                        tree.growthStage.Value++; // Advance tree growth by 1 stage
                        didAction = true;
                    }
                }

                // If any action was done on this tile, add visuals and XP
                if (didAction)
                {
                    Game1.Multiplayer.broadcastSprites(location,
                        new TemporaryAnimatedSprite(10, new Vector2(tile.X * 64f, tile.Y * 64f),
                        Color.Purple, 10, Game1.random.NextBool(), 70f, 0, 64, (tile.Y * 64f + 32f) / 10000f - 0.01f));

                    Utilities.AddEXP(player, 2); // Give experience
                    location.playSound("grassyStep", tile); // Play sound
                    num++;
                    location.updateMap();
                }
            }

            // If no terrain features were affected, the spell fails
            if (num == 0)
            {
                return new SpellFizzle(player, this.GetManaCost(player, level));
            }

            // Consume one fairydust after casting
            player.Items.ReduceId("872", 1);

            return null; // Spell succeeded
        }
    }
}
