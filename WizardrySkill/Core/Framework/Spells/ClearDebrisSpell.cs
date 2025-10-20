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
            level += 1;
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            // create fake tools
            Axe axe = new();
            Pickaxe pickaxe = new();
            foreach (var tool in new Tool[] { axe, pickaxe })
            {
                tool.UpgradeLevel = level;
                tool.IsEfficient = true; // don't drain stamina
                ModEntry.Instance.Helper.Reflection.GetField<Farmer>(tool, "lastUser").SetValue(player);
            }

            // scan location
            GameLocation loc = player.currentLocation;
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    if (!this.CanCast(player, level))
                        return null;

                    Vector2 tile = new(tileX, tileY);
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
                            player.AddMana(-3);
                            Utilities.AddEXP(player, 10);
                        }
                    }

                    // Trees
                    if (level >= 2)
                    {
                        if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is not HoeDirt or Flooring)
                        {
                            if (feature is Tree)
                            {
                                player.AddMana(-3);
                            }
                            if (feature.performToolAction(axe, 0, tile) || feature is Grass || feature is Tree && feature.performToolAction(axe, 0, tile))
                            {
                                if (feature is Tree)
                                    Utilities.AddEXP(player, 5);
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
                                        Utilities.AddEXP(player, 10);
                                    }
                                    break;
                                }
                            }
                        }
                    }
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
