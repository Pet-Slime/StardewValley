using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    public class ClearDebrisSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public ClearDebrisSpell()
            : base(SchoolId.Toil, "cleardebris") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 3;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            // create fake tools
            Axe axe = new();
            Pickaxe pickaxe = new();
            foreach (var tool in new Tool[] { axe, pickaxe })
            {
                tool.UpgradeLevel = level;
                tool.IsEfficient = true; // don't drain stamina
                ModEntry.Instance.Helper.Reflection.GetField<Farmer>(tool, "lastUser").SetValue(player);
            }

            level += 1;
            int num = 0;
            // scan location
            GameLocation loc = player.currentLocation;
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);
            //get a list of the tiles affected
            List<Vector2> list = Utilities.TilesAffected(target, 3 * level, player);
            //for each tile in the list, do the spell's function
            foreach (var tile in list)
            {
                // skip if out of mana
                if (!this.CanContinueCast(player, level))
                    return null;

                bool didAction = false;

                Vector2 toolPixel = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2f); // center of tile

                if (loc.objects.TryGetValue(tile, out SObject obj))
                {
                    // select tool
                    Tool tool = null;
                    if (this.IsAxeDebris(loc, obj))
                        tool = axe;
                    else if (this.IsPickaxeDebris(loc, obj))
                        tool = pickaxe;

                    // apply
                    if (tool == null)
                        continue;
                    player.lastClick = toolPixel;
                    tool.DoFunction(loc, (int)toolPixel.X, (int)toolPixel.Y, 0, player);

                    if (!loc.objects.ContainsKey(tile))
                    {
                        didAction = true;
                    }
                }

                // Trees
                if (level >= 2)
                {
                    if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is not HoeDirt or Flooring or Grass)
                    {
                        if (feature is Tree)
                        {
                            player.AddMana(-3);
                        }
                        if (feature.performToolAction(axe, 0, tile) || feature is Grass || feature is Tree && feature.performToolAction(axe, 0, tile))
                        {
                            if (feature is Tree)
                                didAction = true;
                            loc.terrainFeatures.Remove(tile);
                        }

                    }
                }

                if (level >= 3)
                {
                    ICollection<ResourceClump> clumps = loc.resourceClumps;

                    if (clumps != null)
                    {
                        foreach (var rc in clumps)
                        {
                            if (new Rectangle((int)rc.Tile.X, (int)rc.Tile.Y, rc.width.Value, rc.height.Value).Contains(tileX, tileY))
                            {
                                player.AddMana(-3);
                                if (rc.performToolAction(axe, 1, tile) || rc.performToolAction(pickaxe, 1, tile))
                                {
                                    clumps.Remove(rc);
                                    didAction = true;
                                }
                                break;
                            }
                        }
                    }
                }

                if (didAction)
                {
                    if (num != 0)
                    {
                        player.AddMana(-3);
                    }
                    num++;
                    Utilities.AddEXP(player, 10);
                }
            }


            return null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a given object is debris which can be cleared with a pickaxe.</summary>
        /// <param name="location">The location containing the object.</param>
        /// <param name="obj">The world object.</param>
        private bool IsPickaxeDebris(GameLocation location, SObject obj)
        {
            if (obj is not Chest or null)
            {
                // stones
                if (obj.Name is "Weeds" or "Stone")
                    return true;

                // spawned mine objects
                if (location is MineShaft && obj.IsSpawnedObject)
                    return true;
            }

            return false;
        }

        /// <summary>Get whether a given object is debris which can be cleared with an axe.</summary>
        /// <param name="location">The location containing the object.</param>
        /// <param name="obj">The world object.</param>
        private bool IsAxeDebris(GameLocation location, SObject obj)
        {
            if (obj is not Chest or null)
            {
                // twig
                if (obj.ParentSheetIndex is 294 or 295)
                    return true;

                // weeds
                if (obj.Name is "Weeds")
                    return true;

                // spawned mine objects
                if (location is MineShaft && obj.IsSpawnedObject)
                    return true;
            }

            return false;
        }
    }
}
