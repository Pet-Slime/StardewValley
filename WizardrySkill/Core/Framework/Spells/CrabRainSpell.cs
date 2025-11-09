using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Monsters;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;
using static StardewValley.Minigames.TargetGame;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>
    /// A Nature spell that summons crabs raining from the sky.
    /// </summary>
    public class CrabRainSpell : Spell
    {
        public CrabRainSpell()
            : base(SchoolId.Nature, "crabrain")
        {
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Must have at least one crab item
            return base.CanCast(player, level) && player.Items.ContainsId("(O)717", 1);
        }

        private static readonly Random Rng = new Random();

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;

            if (player.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
            {
                player.Items.ReduceId("(O)717", 1);
            }

            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);

            GameLocation loc = player.currentLocation;

            // Get all candidate tiles (radius 10)
            List<Vector2> areaTiles = Utilities.TilesAffected(target, 10, player);

            // Efficiently pick up to 30 random unique tiles from areaTiles (no OrderBy/shuffle of entire list)
            List<Vector2> sample30 = PickRandomSubset(areaTiles, 30);

            // Filter to valid ground tiles and stop once we have up to 8 valid tiles
            List<Vector2> selectedTiles = GetWalkableTiles(loc, sample30);

            return new CrabRave(player, ItemRegistry.Create("(O)717"), selectedTiles);
        }

        /// <summary>
        /// Picks up to 'count' unique random items from 'source' without a heavy sort/shuffle.
        /// Preserves O(count) memory and avoids sorting the whole list.
        /// </summary>
        private static List<Vector2> PickRandomSubset(List<Vector2> source, int count)
        {
            var result = new List<Vector2>(Math.Min(count, source.Count));
            if (source.Count <= count)
            {
                // small list -> return shallow copy (we'll shuffle for randomness)
                result.AddRange(source);
                // Fisher-Yates shuffle in-place for variety
                for (int i = result.Count - 1; i > 0; i--)
                {
                    int j = Rng.Next(i + 1);
                    (result[i], result[j]) = (result[j], result[i]);
                }
                return result;
            }

            // Reservoir sampling style / unique random indices
            var selectedIndices = new HashSet<int>();
            while (selectedIndices.Count < count)
            {
                int idx = Rng.Next(source.Count);
                selectedIndices.Add(idx);
            }

            foreach (int i in selectedIndices)
                result.Add(source[i]);

            return result;
        }

        /// <summary>
        /// Returns only walkable, non-water, non-blocked tiles.
        /// </summary>
        public List<Vector2> GetWalkableTiles(GameLocation location, List<Vector2> tiles)
        {
            var walkable = new List<Vector2>();
            int walkableAmount = 0;
            var crab = new RockCrab(tiles[0]);
            var backLayer = location.map.RequireLayer("Back");

            foreach (Vector2 tile in tiles)
            {
                float targetX = tile.X * Game1.tileSize;
                float targetY = tile.Y * Game1.tileSize;

                crab.Position = tile * Game1.tileSize;

                if (!location.isTileOnMap(tile))
                    continue;

                Rectangle boundingBox = BlinkSpot(crab, (int)(targetX - crab.GetBoundingBox().Width / 2), (int)(targetY - crab.GetBoundingBox().Height / 2));

                if (location.isCollidingPosition(boundingBox, Game1.viewport, isFarmer: true, 0, glider: false, crab))
                    continue;

                Tile backTile = backLayer.Tiles[(int)tile.X, (int)tile.Y];
                if (backTile == null)
                    continue;

                if (HasCollision(backTile))
                    continue;

                if (location.objects.ContainsKey(tile))
                    continue;

                if (location.isCharacterAtTile(tile) != null)
                    continue;

                if (location.furniture?.Any(f => f.boundingBox.Value.Contains(targetX, targetY)) == true)
                    continue;

                walkable.Add(tile);
                walkableAmount++;
                if (walkableAmount == 8)
                {
                    return walkable;
                }
            }

            return walkable;
        }

        private static Rectangle BlinkSpot(Monster who, int targetX, int targetY)
        {
            // Roughly aligns with player's hitbox
            return new Rectangle(targetX + 8, targetY + who.Sprite.getHeight() - 32, 48, 32);
        }

        private static bool HasCollision(Tile tile)
        {
            // Checks for any terrain property that should block blinking
            return tile.TileIndexProperties.ContainsKey("Passable")
                || tile.Properties.ContainsKey("Passable")
                || tile.TileIndexProperties.ContainsKey("Water")
                || tile.Properties.ContainsKey("Water")
                || tile.TileIndexProperties.ContainsKey("Buildings")
                || tile.Properties.ContainsKey("Buildings");
        }
    }
}
