using System;
using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Minigames;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    public class PhotosynthesisSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public PhotosynthesisSpell()
            : base(SchoolId.Nature, "photosynthesis") { }

        public override int GetMaxCastingLevel()
        {
            return 3;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 100;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.Items.ContainsId(SObject.prismaticShardIndex.ToString(), 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            GameLocation location = player.currentLocation;
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var tile = new Vector2(tileX, tileY);
            //get a list of the tiles affected
            List<Vector2> list = Utilities.TilesAffected(tile, 10 * (level +1), player);

            // check each tile for the crops
            foreach (var entry in location.terrainFeatures.Pairs.ToList())
            {
                var tf = entry.Value;
                // If the object in hoedirt and is on the list
                if (tf is HoeDirt dirt && list.Contains(entry.Key))
                {
                    // continue if there is no crop or if the crop is fully grown
                    if (dirt.crop == null || dirt.crop.fullyGrown.Value)
                        continue;
                    // If it does contain a a crop, advance the crop for one day
                    dirt.crop.newDay(1);
                    location.playSound("grassyStep");
                }
                if (tf is FruitTree fruitTree && list.Contains(entry.Key)) {
                    if (fruitTree.daysUntilMature.Value > 0)
                    {
                        location.playSound("grassyStep");
                        fruitTree.daysUntilMature.Value = Math.Max(0, fruitTree.daysUntilMature.Value - 7);
                        fruitTree.growthStage.Value = fruitTree.daysUntilMature.Value > 0 ? fruitTree.daysUntilMature.Value > 7 ? fruitTree.daysUntilMature.Value > 14 ? fruitTree.daysUntilMature.Value > 21 ? 0 : 1 : 2 : 3 : 4;
                    }
                    else if (!fruitTree.stump.Value && fruitTree.growthStage.Value == 4 && (fruitTree.IsInSeasonHere() || location.Name == "Greenhouse"))
                    {
                        int fruitCount = fruitTree.fruit.Count;
                        for (int i = fruitCount; i < 3; i++)
                        {
                            fruitTree.TryAddFruit();
                            location.playSound("grassyStep");
                        }
                    }
                }
                if (tf is FruitTree tree && list.Contains(entry.Key))
                {
                    if (tree.growthStage.Value < 5)
                    {

                        location.playSound("grassyStep");
                        tree.growthStage.Value++;
                    }
                }
                location.updateMap();

            }
            player.Items.ReduceId(SObject.prismaticShardIndex.ToString(), 1);
            return null;
        }
    }
}
