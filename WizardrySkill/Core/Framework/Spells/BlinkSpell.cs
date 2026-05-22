using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Pathfinding;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;

namespace WizardrySkill.Core.Framework.Spells
{
    public class BlinkSpell : Spell
    {
        /*********
        ** Fields
        *********/

        // How far Stardew should search from the clicked tile to find a nearby valid character-standing tile.
        private const int NearbySearchIterations = 8;

        // How many pathfinding nodes Blink can search before rejecting the destination.
        private const int PathfindLimit = 1000;

        // Blink path sparkle settings.
        private const int SparkleAnimationLength = 6;
        private const float SparkleAnimationInterval = 100f;
        private const int SparkleDelayPerTile = 5;


        /*********
        ** Public methods
        *********/
        public BlinkSpell()
            : base(SchoolId.Motion, "blink")
        {
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        // Checks whether the player can cast this spell
        public override bool CanCast(Farmer player, int level)
        {
            // Requirements:
            // - Base spell requirements.
            // - Must not be riding a mount.
            // - Must have a Travel Core item in inventory.
            return base.CanCast(player, level)
                && player.mount == null;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            // Base cost is handled dynamically in OnCast based on path distance.
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["moonslime.Wizardry.Travel_Core"] = 1
            };
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should move the player, spend blink mana, and show the blink path.
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;
            Vector2 requestedTile = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            // 1. Prevent blinking from an out-of-map click.
            if (!location.isTileOnMap(requestedTile))
                return new SpellFizzle(player);

            // 2. Ask Stardew to resolve the clicked tile into a nearby valid character-standing tile.
            // allowOffMap: false prevents the resolver from returning an off-map tile.
            Vector2 landingTile = Utility.recursiveFindOpenTileForCharacter(player, location, requestedTile, NearbySearchIterations, allowOffMap: false);
            if (landingTile == Vector2.Zero)
                return new SpellFizzle(player);

            // 3. Run the same soft validation rules we use for Blink.
            if (!IsValidBlinkTile(player, location, landingTile))
                return new SpellFizzle(player);

            // 4. Require a valid walking path to the resolved landing tile and use that path length for cost.
            // This prevents blinking across cliffs, walls, water gaps, map partitions, or other disconnected terrain.
            if (!TryGetPathToTile(player, location, landingTile, out List<Point> pathTiles, out int pathDistance))
                return new SpellFizzle(player);

            // 5. Prevent blinking to the same tile.
            if (pathDistance <= 0)
                return new SpellFizzle(player);

            // 6. Mana cost based on path distance instead of straight-line distance.
            int manaCost = pathDistance * 0;
            if (player.GetCurrentMana() < manaCost)
                return new SpellFizzle(player);

            // Consume the travel core
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // 7. Show a short local blue sparkle trail along the resolved path.
            ShowBlinkPathVisuals(location, pathTiles);

            // 8. Move the player to the resolved tile.
            player.Position = GetFarmerLandingPositionAtTile(player, landingTile);
            player.AddMana(-manaCost);



            // 9. Success effect.
            return new SpellSuccess(player, "powerup", pathDistance);
        }


        /*********
        ** Private helpers
        *********/
        private static bool IsValidBlinkTile(Farmer player, GameLocation location, Vector2 tile)
        {
            // 1. Prevent teleporting out of bounds.
            if (!location.isTileOnMap(tile))
                return false;

            // 2. Block water tiles.
            if (location.isWaterTile((int)tile.X, (int)tile.Y))
                return false;

            // 3. Build collision rectangle for where the farmer would actually land.
            Rectangle landingBox = GetFarmerBoundingBoxAtTile(player, tile);

            // 4. Block solid walls or obstacles.
            if (location.isCollidingPosition(landingBox, Game1.viewport, isFarmer: true, 0, glider: false, player))
                return false;

            // 5. Terrain validation on the Back layer.
            var backLayer = location.map.RequireLayer("Back");
            Tile backTile = backLayer.Tiles[(int)tile.X, (int)tile.Y];
            if (backTile == null)
                return false;

            if (HasCollision(backTile))
                return false;

            // 6. Block if there's an object on the target tile.
            if (location.objects.ContainsKey(tile))
                return false;

            // 7. Block if an NPC, monster, pet, or other character occupies the target tile.
            if (location.isCharacterAtTile(tile) != null)
                return false;

            // 8. Block if furniture occupies the landing area.
            if (location.furniture?.Any(furniture => furniture.boundingBox.Value.Intersects(landingBox)) == true)
                return false;

            return true;
        }

        private static bool TryGetPathToTile(Farmer player, GameLocation location, Vector2 landingTile, out List<Point> pathTiles, out int pathDistance)
        {
            pathTiles = new List<Point>();
            pathDistance = 0;

            if (player == null || location == null)
                return false;

            Point start = player.TilePoint;
            Point end = new((int)landingTile.X, (int)landingTile.Y);

            if (start == end)
                return false;

            Stack<Point> path = PathFindController.findPath(
                startPoint: start,
                endPoint: end,
                endPointFunction: PathFindController.isAtEndPoint,
                location: location,
                character: player,
                limit: PathfindLimit
            );

            if (path == null || path.Count == 0)
                return false;

            // PathFindController.reconstructPath includes both start and end nodes.
            pathTiles = path.ToList();
            pathDistance = Math.Max(0, pathTiles.Count - 1);
            return true;
        }

        private static void ShowBlinkPathVisuals(GameLocation location, List<Point> pathTiles)
        {
            if (location == null || pathTiles == null || pathTiles.Count == 0)
                return;

            for (int i = 0; i < pathTiles.Count; i++)
            {
                Point tile = pathTiles[i];
                Vector2 tileVector = new(tile.X, tile.Y);

                if (!location.isTileOnMap(tileVector))
                    continue;

                Vector2 pixelPosition = tileVector * Game1.tileSize;
                float layerDepth = (tile.Y + 1) * Game1.tileSize / 10000f;

                Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(
                    10,
                    pixelPosition,
                    Color.DeepSkyBlue,
                    SparkleAnimationLength,
                    Game1.random.NextDouble() < 0.5,
                    SparkleAnimationInterval,
                    0,
                    Game1.tileSize,
                    layerDepth)
                {
                    delayBeforeAnimationStart = i * SparkleDelayPerTile
                });
            }
        }

        private static Vector2 GetFarmerLandingPositionAtTile(Farmer player, Vector2 tile)
        {
            int width = player.GetBoundingBox().Width;
            return new Vector2(tile.X * Game1.tileSize + Game1.tileSize / 2f - width / 2f, tile.Y * Game1.tileSize + 4f);
        }

        private static Rectangle GetFarmerBoundingBoxAtTile(Farmer player, Vector2 tile)
        {
            Vector2 oldPosition = player.Position;

            player.Position = GetFarmerLandingPositionAtTile(player, tile);
            Rectangle box = player.GetBoundingBox();

            player.Position = oldPosition;
            return box;
        }

        private static bool HasCollision(Tile tile)
        {
            // Checks for terrain properties that should block blinking.
            return tile.TileIndexProperties.ContainsKey("Passable")
                || tile.Properties.ContainsKey("Passable")
                || tile.TileIndexProperties.ContainsKey("Water")
                || tile.Properties.ContainsKey("Water")
                || tile.TileIndexProperties.ContainsKey("Buildings")
                || tile.Properties.ContainsKey("Buildings");
        }
    }
}
