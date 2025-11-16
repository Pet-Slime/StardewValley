using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>
    /// Clears debris in an area: stones, weeds, stumps, boulders, and small terrain debris.
    /// Does not destroy full-grown trees.
    /// </summary>
    public class ClearDebrisSpell : Spell
    {
        public ClearDebrisSpell()
            : base(SchoolId.Toil, "cleardebris") { }

        public override int GetManaCost(Farmer player, int level) => 3;

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;


            level += 1; // Increment level for area scaling
            int actionCount = 0;


            // Setup fake tools for spell
            Axe axe = CreateTool<Axe>(player, level);
            Pickaxe pickaxe = CreateTool<Pickaxe>(player, level);

            GameLocation loc = player.currentLocation;
            Vector2 targetTile = new(targetX / Game1.tileSize, targetY / Game1.tileSize);
            List<Vector2> affectedTiles = Utilities.TilesAffected(targetTile, level, player);

            foreach (Vector2 tile in affectedTiles)
            {
                if (!this.CanContinueCast(player, level))
                    return null;

                bool didAction = false;

                // Handle world objects
                didAction |= HandleObjects(loc, tile, player, axe, pickaxe);

                // Handle terrain features (trees, branches, small debris)
                if (level >= 2)
                    didAction |= HandleTerrainFeatures(loc, tile, player, axe, pickaxe);

                // Handle resource clumps (stumps, boulders)
                if (level >= 3)
                    didAction |= HandleResourceClumps(loc, tile, player, axe, pickaxe);

                // Deduct mana and give EXP if any action was performed
                if (didAction)
                {
                    if (actionCount != 0)
                        player.AddMana(-this.GetManaCost(player, level));

                    actionCount++;
                    Utilities.AddEXP(player, 5);
                }
            }

            return actionCount == 0
                ? new SpellFizzle(player, this.GetManaCost(player, level))
                : null;
        }

        private static T CreateTool<T>(Farmer player, int level) where T : Tool, new()
        {
            T tool = new();
            tool.UpgradeLevel = level;
            tool.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(tool, "lastUser").SetValue(player);
            return tool;
        }

        private static bool HandleObjects(GameLocation loc, Vector2 tile, Farmer player, Axe axe, Pickaxe pickaxe)
        {
            if (!loc.objects.TryGetValue(tile, out SObject obj))
                return false;

            Tool tool = null;
            if (IsAxeDebris(loc, obj)) tool = axe;
            else if (IsPickaxeDebris(loc, obj)) tool = pickaxe;

            if (tool == null) return false;

            Vector2 toolPixel = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2f);
            player.lastClick = toolPixel;
            tool.DoFunction(loc, (int)toolPixel.X, (int)toolPixel.Y, 0, player);

            return !loc.objects.ContainsKey(tile);
        }

        private static bool HandleTerrainFeatures(GameLocation loc, Vector2 tile, Farmer player, Axe axe, Pickaxe pickaxe)
        {
            if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature))
                return false;

            if (feature is HoeDirt or Flooring or Grass)
                return false;

            if (feature is Tree tree)
            {
                player.AddMana(-3); // extra mana cost
                bool destroyed = tree.performToolAction(axe, 0, tile);

                if (destroyed)
                    loc.terrainFeatures.Remove(tile);

                return destroyed;
            }
            else
            {
                bool destroyed = feature.performToolAction(axe, 0, tile) || feature.performToolAction(pickaxe, 0, tile);

                if (destroyed)
                    loc.terrainFeatures.Remove(tile);

                return destroyed;
            }
        }

        private static bool HandleResourceClumps(GameLocation loc, Vector2 tile, Farmer player, Axe axe, Pickaxe pickaxe)
        {

            ICollection<ResourceClump> clumps = loc.resourceClumps;

            if (clumps == null)
                return false;

            bool didAction = false;
            var clumpsCopy = new List<ResourceClump>(loc.resourceClumps);

            foreach (var rc in clumpsCopy)
            {
                var rcRect = new Rectangle((int)rc.Tile.X, (int)rc.Tile.Y, rc.width.Value, rc.height.Value);
                if (!rcRect.Contains((int)tile.X, (int)tile.Y))
                    continue;

                player.AddMana(-3); // extra mana cost
                Vector2 topLeft = rc.Tile;
                bool destroyed = rc.performToolAction(axe, 1, topLeft) || rc.performToolAction(pickaxe, 1, topLeft);

                if (destroyed && loc.resourceClumps.Contains(rc))
                    loc.resourceClumps.Remove(rc);

                didAction = destroyed;
                break; // Only act on one clump per tile
            }

            return didAction;
        }

        private static bool IsPickaxeDebris(GameLocation location, SObject obj)
        {
            if (obj is not Chest or null)
            {
                if (obj.Name is "Weeds" or "Stone")
                    return true;
                if (location is MineShaft && obj.IsSpawnedObject)
                    return true;
            }
            return false;
        }

        private static bool IsAxeDebris(GameLocation location, SObject obj)
        {
            if (obj is not Chest or null)
            {
                if (obj.ParentSheetIndex is 294 or 295)
                    return true;
                if (obj.Name is "Weeds")
                    return true;
                if (location is MineShaft && obj.IsSpawnedObject)
                    return true;
            }
            return false;
        }
    }
}
