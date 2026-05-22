using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MoonShared;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

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
            // SchoolId.Nature indicates the spell belongs to the Nature school.
            // "photosynthesis" is the internal name for this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetMaxCastingLevel()
        {
            return 3;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 80;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["872"] = 1
            };
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player has Fairy Dust in their inventory.
            return base.CanCast(player, level);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should accelerate growth, consume Fairy Dust, award EXP, and broadcast visuals.
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;

            // Convert pixel coordinates to tile coordinates.
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            Vector2 target = new(tileX, tileY);

            // Get the tiles affected by the spell based on the spell's level.
            List<Vector2> affectedTiles = Utilities.TilesAffected(target, 3 * (level + 1), player);

            int actionCount = 0;

            // Loop through all terrain features in the location.
            foreach (var entry in location.terrainFeatures.Pairs.ToList())
            {
                if (!affectedTiles.Contains(entry.Key))
                    continue;

                bool didAction = false;
                TerrainFeature terrainFeature = entry.Value;
                Vector2 tile = terrainFeature.Tile;

                // If the terrain feature is HoeDirt with a crop.
                if (terrainFeature is HoeDirt dirt)
                {
                    if (dirt.crop != null && !dirt.crop.fullyGrown.Value)
                    {
                        dirt.crop.newDay(1);
                        didAction = true;
                    }
                }

                // If the terrain feature is a Fruit Tree.
                if (terrainFeature is FruitTree fruitTree)
                {
                    if (fruitTree.daysUntilMature.Value > 0)
                    {
                        didAction = true;

                        // Reduce days until mature by 7.
                        fruitTree.daysUntilMature.Value = Math.Max(0, fruitTree.daysUntilMature.Value - 7);

                        // Update growth stage based on remaining days.
                        fruitTree.growthStage.Value = fruitTree.daysUntilMature.Value > 21 ? 0 :
                                                      fruitTree.daysUntilMature.Value > 14 ? 1 :
                                                      fruitTree.daysUntilMature.Value > 7 ? 2 :
                                                      fruitTree.daysUntilMature.Value > 0 ? 3 : 4;
                    }
                    else if (!fruitTree.stump.Value && fruitTree.growthStage.Value == 4 && (fruitTree.IsInSeasonHere() || location.Name == "Greenhouse"))
                    {
                        int fruitCount = fruitTree.fruit.Count;
                        for (int i = fruitCount; i < 3; i++)
                        {
                            fruitTree.TryAddFruit();
                            didAction = true;
                        }
                    }
                }

                // If the terrain feature is a normal tree.
                if (terrainFeature is Tree tree && tree.growthStage.Value < 5)
                {
                    tree.growthStage.Value++;
                    didAction = true;
                }

                if (!didAction)
                    continue;

                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(
                        10,
                        new Vector2(tile.X * 64f, tile.Y * 64f),
                        Color.Purple,
                        10,
                        Game1.random.NextBool(),
                        70f,
                        0,
                        64,
                        (tile.Y * 64f + 32f) / 10000f - 0.01f));

                Utilities.AddEXP(player, 2);
                location.playSound("grassyStep", tile / Game1.tileSize);
                location.updateMap();
                actionCount++;
            }

            if (actionCount == 0)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Consume the item unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            return null;
        }
    }
}
