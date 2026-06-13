using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using WizardrySkill.Core.Framework;
using xTile.Tiles;

namespace WizardrySkill.Core.Framework.Game.Interface
{
    public class TeleportMenu : IClickableMenu
    {
        /*********
        ** Fields
        *********/
        private const int WindowWidth = 640;
        private const int WindowHeight = 480;

        private const int ElemHeight = 50;
        private const int EdgePad = 16;

        private const int RandomTileAttempts = 256;
        private const int NearbySearchIterations = 12;

        private readonly Farmer Player;
        private readonly List<TeleportLocation> Locs = new();

        /// <summary>The outdoor locations already proven reachable when the menu opened.</summary>
        private readonly HashSet<string> ReachableLocations;

        /// <summary>Cached connected landing regions by destination location.</summary>
        private readonly Dictionary<string, HashSet<Point>> LandingPathCache = new(StringComparer.Ordinal);

        private TeleportLocation WarpTo;

        private int Scroll;
        private Rectangle ScrollbarBack;
        private Rectangle Scrollbar;

        private bool DragScroll;
        private bool JustClicked;


        /*********
        ** Private models
        *********/
        private sealed class TeleportLocation
        {
            /// <summary>The internal location name used by Stardew for lookups, visited-location checks, and warping.</summary>
            public string InternalName { get; }

            /// <summary>The display name shown to the player in the teleport menu.</summary>
            public string DisplayName { get; }

            public TeleportLocation(string internalName, string displayName)
            {
                this.InternalName = internalName;
                this.DisplayName = displayName;
            }
        }


        /*********
        ** Public methods
        *********/
        public TeleportMenu(Farmer player)
            : base((Game1.uiViewport.Width - WindowWidth) / 2, (Game1.uiViewport.Height - WindowHeight) / 2, WindowWidth, WindowHeight, true)
        {
            this.Player = player;
            this.ReachableLocations = PlayerRoutePathfinder.GetReachableLocations(player) ?? new HashSet<string>(StringComparer.Ordinal);

            foreach (GameLocation loc in Game1.locations)
            {
                if (loc == null)
                    continue;

                string internalName = loc.Name;
                if (string.IsNullOrWhiteSpace(internalName))
                    continue;

                if (!loc.IsOutdoors)
                    continue;

                if (!HasDiscoveredTeleportLocation(player, internalName))
                    continue;

                if (!this.ReachableLocations.Contains(internalName))
                    continue;

                this.Locs.Add(new TeleportLocation(internalName, GetDisplayName(loc)));
            }

            this.Locs.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            int x = this.xPositionOnScreen + 12;
            int y = this.yPositionOnScreen + 12;
            int w = WindowWidth - 24;
            int h = WindowHeight - 24;

            this.ScrollbarBack = new Rectangle(x + w - Game1.pixelZoom * 6, y, Game1.pixelZoom * 6, h);
            this.UpdateScrollbar();
        }

        /// <inheritdoc />
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        /// <inheritdoc />
        public override void update(GameTime time)
        {
            base.update(time);

            if (this.WarpTo != null)
            {
                TeleportLocation destination = this.WarpTo;
                this.WarpTo = null;
                this.TryWarpTo(destination);
                return;
            }

            if (this.DragScroll)
            {
                int mouseY = Game1.getMouseY();
                int relativeY = mouseY - (this.ScrollbarBack.Y + 2 + this.Scrollbar.Height / 2);
                relativeY = Math.Max(0, relativeY);
                relativeY = Math.Min(relativeY, Math.Max(0, this.ScrollbarBack.Height - 4 - this.Scrollbar.Height));

                float scrollSpace = this.ScrollbarBack.Height - 4f - this.Scrollbar.Height;
                float percentY = scrollSpace > 0 ? relativeY / scrollSpace : 0f;

                int totalY = this.GetScrollCap();
                this.Scroll = -(int)(totalY * percentY);
                this.ClampScroll();
            }
        }

        /// <inheritdoc />
        public override void draw(SpriteBatch b)
        {
            drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, WindowWidth, WindowHeight, Color.White);

            int x = this.xPositionOnScreen + 12;
            int y = this.yPositionOnScreen + 12;
            int w = WindowWidth - 24;
            int h = WindowHeight - 24;

            b.End();

            using RasterizerState state = new()
            {
                ScissorTestEnable = true
            };

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, state);
            b.GraphicsDevice.ScissorRectangle = new Rectangle(x, y, w, h);

            int iy = y + EdgePad + this.Scroll;

            if (this.Locs.Count == 0)
            {
                b.DrawString(Game1.dialogueFont, "No teleport locations unlocked.", new Vector2(x + EdgePad, iy), Color.Black);
            }
            else
            {
                foreach (TeleportLocation loc in this.Locs)
                {
                    Rectangle area = new(x, iy - 4, w - this.ScrollbarBack.Width, ElemHeight);
                    if (area.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        b.Draw(Game1.staminaRect, area, new Color(200, 32, 32, 64));

                        if (this.JustClicked)
                            this.WarpTo = loc;
                    }

                    b.DrawString(Game1.dialogueFont, loc.DisplayName, new Vector2(x + EdgePad, iy), Color.Black);
                    iy += ElemHeight;
                }
            }

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (this.ShouldShowScrollbar())
            {
                this.UpdateScrollbar();

                drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.ScrollbarBack.X, this.ScrollbarBack.Y, this.ScrollbarBack.Width, this.ScrollbarBack.Height, Color.DarkGoldenrod, Game1.pixelZoom, false);
                drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.Scrollbar.X, this.Scrollbar.Y, this.Scrollbar.Width, this.Scrollbar.Height, Color.Gold, Game1.pixelZoom, false);
            }

            this.JustClicked = false;

            base.draw(b);
            this.drawMouse(b);
        }

        /// <inheritdoc />
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (this.ShouldShowScrollbar() && this.ScrollbarBack.Contains(x, y))
                this.DragScroll = true;
            else
                this.JustClicked = true;
        }

        /// <inheritdoc />
        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);
            this.DragScroll = false;
        }

        /// <inheritdoc />
        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            this.Scroll += direction;
            this.ClampScroll();
        }


        /*********
        ** Private methods
        *********/
        private void TryWarpTo(TeleportLocation destinationInfo)
        {
            if (this.Player == null || !this.Player.IsLocalPlayer)
                return;

            if (destinationInfo == null || string.IsNullOrWhiteSpace(destinationInfo.InternalName))
                return;

            if (!this.Player.Items.ContainsId("moonslime.Wizardry.Travel_Core"))
            {
                Game1.playSound("cancel");
                return;
            }

            // Do not run route solving here.
            // The menu already read cached reachable outdoor maps when it opened.
            if (!this.ReachableLocations.Contains(destinationInfo.InternalName))
            {
                Game1.playSound("cancel");
                return;
            }

            GameLocation destination = Game1.getLocationFromName(destinationInfo.InternalName);
            if (destination == null)
            {
                Game1.playSound("cancel");
                return;
            }

            if (!this.TryFindRandomWarpTile(destination, destinationInfo.InternalName, out Vector2 warpTile))
            {
                Game1.playSound("cancel");
                return;
            }

            Game1.activeClickableMenu = null;

            this.Player.Items.ReduceId("moonslime.Wizardry.Travel_Core", 1);

            Game1.playSound("wand");
            Utilities.AddEXP(this.Player, 50);

            // Use the internal location name for the actual warp.
            Game1.warpFarmer(destinationInfo.InternalName, (int)warpTile.X, (int)warpTile.Y, false);
        }

        private bool TryFindRandomWarpTile(GameLocation location, string destinationName, out Vector2 warpTile)
        {
            warpTile = Vector2.Zero;

            if (location?.map?.Layers == null || location.map.Layers.Count == 0)
                return false;

            int mapWidth = location.map.Layers[0].LayerWidth;
            int mapHeight = location.map.Layers[0].LayerHeight;

            if (mapWidth <= 0 || mapHeight <= 0)
                return false;

            HashSet<Point> reachableLandingTiles = this.GetReachableLandingTiles(location, destinationName);
            if (reachableLandingTiles.Count == 0)
                return false;

            for (int attempt = 0; attempt < RandomTileAttempts; attempt++)
            {
                Vector2 candidate = new(Game1.random.Next(mapWidth), Game1.random.Next(mapHeight));

                if (this.IsReachableSoftValidWarpTile(location, candidate, reachableLandingTiles))
                {
                    warpTile = candidate;
                    return true;
                }
            }

            return this.TryFindFallbackWarpTile(location, mapWidth, mapHeight, reachableLandingTiles, out warpTile);
        }

        private bool TryFindFallbackWarpTile(GameLocation location, int mapWidth, int mapHeight, HashSet<Point> reachableLandingTiles, out Vector2 warpTile)
        {
            warpTile = Vector2.Zero;

            Vector2 center = new(mapWidth / 2f, mapHeight / 2f);
            Vector2 openTile = Utility.recursiveFindOpenTileForCharacter(this.Player, location, center, maxIterations: 80, allowOffMap: false);

            if (openTile != Vector2.Zero && this.IsReachableSoftValidWarpTile(location, openTile, reachableLandingTiles))
            {
                warpTile = openTile;
                return true;
            }

            // If the center fallback fails, scan in small steps instead of repeatedly
            // running recursiveFindOpenTileForCharacter on random candidates.
            for (int y = 0; y < mapHeight; y += 4)
            {
                for (int x = 0; x < mapWidth; x += 4)
                {
                    Vector2 candidate = new(x, y);
                    if (this.IsReachableSoftValidWarpTile(location, candidate, reachableLandingTiles))
                    {
                        warpTile = candidate;
                        return true;
                    }
                }
            }

            // Final fallback: scan the connected landing component directly.
            // This only runs if random + stepped scan both failed.
            foreach (Point point in reachableLandingTiles)
            {
                Vector2 candidate = new(point.X, point.Y);
                if (this.IsSoftValidWarpTile(location, candidate))
                {
                    warpTile = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool IsReachableSoftValidWarpTile(GameLocation location, Vector2 tile, HashSet<Point> reachableLandingTiles)
        {
            if (!this.IsSoftValidWarpTile(location, tile))
                return false;

            Point point = new((int)tile.X, (int)tile.Y);
            return reachableLandingTiles.Contains(point);
        }

        private HashSet<Point> GetReachableLandingTiles(GameLocation destination, string destinationName)
        {
            if (destination == null || string.IsNullOrWhiteSpace(destinationName))
                return new HashSet<Point>();

            if (this.LandingPathCache.TryGetValue(destinationName, out HashSet<Point> cached))
                return cached;

            HashSet<Point> reachable = new();
            Queue<Point> open = new();

            foreach (Point start in this.GetLandingStartTiles(destination, destinationName))
            {
                Vector2 startVector = new(start.X, start.Y);
                if (!destination.isTileOnMap(startVector))
                    continue;

                if (reachable.Add(start))
                    open.Enqueue(start);
            }

            while (open.Count > 0)
            {
                Point current = open.Dequeue();

                foreach (Point next in GetNeighbors(current))
                {
                    if (reachable.Contains(next))
                        continue;

                    if (!this.IsLandingPathTilePassable(destination, next))
                        continue;

                    reachable.Add(next);
                    open.Enqueue(next);
                }
            }

            this.LandingPathCache[destinationName] = reachable;
            return reachable;
        }

        private IEnumerable<Point> GetLandingStartTiles(GameLocation destination, string destinationName)
        {
            string[] route = PlayerRoutePathfinder.GetLocationRoute(this.Player, destinationName);

            if (route == null || route.Length <= 1)
            {
                if (this.Player?.currentLocation != null && this.Player.currentLocation.Name.Equals(destinationName, StringComparison.Ordinal))
                    yield return this.Player.TilePoint;

                foreach (Warp warp in destination.warps)
                    yield return new Point(warp.X, warp.Y);

                yield break;
            }

            string previousLocationName = route[route.Length - 2];
            GameLocation previousLocation = Game1.getLocationFromName(previousLocationName);

            if (previousLocation != null)
            {
                foreach (Warp warp in previousLocation.warps)
                {
                    if (warp == null || string.IsNullOrWhiteSpace(warp.TargetName))
                        continue;

                    string targetName = ResolveWarpTargetName(warp.TargetName);
                    GameLocation targetLocation = Game1.getLocationFromName(targetName);

                    if (targetLocation == null)
                        continue;

                    if (targetLocation.Name.Equals(destination.Name, StringComparison.Ordinal))
                        yield return new Point(warp.TargetX, warp.TargetY);
                }
            }

            // Safety fallback for weird/custom maps where the cached route exists but we couldn't
            // identify the exact entry warp. This still avoids accepting disconnected random tiles.
            foreach (Warp warp in destination.warps)
                yield return new Point(warp.X, warp.Y);
        }

        private bool IsLandingPathTilePassable(GameLocation location, Point tile)
        {
            Vector2 vector = new(tile.X, tile.Y);

            if (!location.isTileOnMap(vector))
                return false;

            if (location.isWaterTile(tile.X, tile.Y))
                return false;

            Tile buildingTile = location.Map.RequireLayer("Buildings").Tiles[tile.X, tile.Y];
            if (buildingTile != null && buildingTile.TileIndex != -1)
            {
                if (buildingTile.TileIndexProperties.TryGetValue("Action", out var action) || buildingTile.Properties.TryGetValue("Action", out action))
                {
return false;
                }
                else if (location.doesTileHaveProperty(tile.X, tile.Y, "Passable", "Buildings") == null)
                {
                    return false;
                }
            }

            if (location.objects.ContainsKey(vector) && location.objects[vector]?.isPassable() != true)
                return false;

            if (location.terrainFeatures.TryGetValue(vector, out var terrainFeature) && !terrainFeature.isPassable(this.Player))
                return false;

            if (!(location.getLargeTerrainFeatureAt(tile.X, tile.Y)?.isPassable(this.Player) ?? true))
                return false;

            return true;
        }

        private bool IsSoftValidWarpTile(GameLocation location, Vector2 tile)
        {
            if (location == null || this.Player == null)
                return false;

            // 1. Prevent warping out of bounds.
            if (!location.isTileOnMap(tile))
                return false;

            // 2. Block water tiles.
            if (location.isWaterTile((int)tile.X, (int)tile.Y))
                return false;

            // 3. Require a real Back-layer tile.
            var backLayer = location.map.RequireLayer("Back");
            Tile backTile = backLayer.Tiles[(int)tile.X, (int)tile.Y];
            if (backTile == null)
                return false;

            // 4. Block map properties we already treat as invalid for Blink.
            if (HasCollision(backTile))
                return false;

            // 5. Block placed objects.
            if (location.objects.ContainsKey(tile))
                return false;

            // 6. Block NPCs, monsters, pets, and other characters.
            if (location.isCharacterAtTile(tile) != null)
                return false;

            // 7. Block furniture on the tile.
            Rectangle landingBox = this.GetFarmerBoundingBoxAtTile(tile);
            if (location.furniture?.Any(furniture => furniture.boundingBox.Value.Intersects(landingBox)) == true)
                return false;

            // 8. Final character collision check using the exact box we expect after landing.
            if (location.isCollidingPosition(landingBox, Game1.viewport, isFarmer: true, 0, glider: false, this.Player))
                return false;

            return true;
        }

        private Rectangle GetFarmerBoundingBoxAtTile(Vector2 tile)
        {
            Vector2 oldPosition = this.Player.Position;
            int width = this.Player.GetBoundingBox().Width;

            this.Player.Position = new Vector2(tile.X * Game1.tileSize + Game1.tileSize / 2f - width / 2f, tile.Y * Game1.tileSize + 4f);
            Rectangle box = this.Player.GetBoundingBox();
            this.Player.Position = oldPosition;

            return box;
        }

        private static IEnumerable<Point> GetNeighbors(Point point)
        {
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X + 1, point.Y);
            yield return new Point(point.X, point.Y - 1);
            yield return new Point(point.X, point.Y + 1);
        }

        private static bool HasCollision(Tile tile)
        {
            // Same soft tile-property filter used by Blink.
            return tile.TileIndexProperties.ContainsKey("Passable")
                || tile.Properties.ContainsKey("Passable")
                || tile.TileIndexProperties.ContainsKey("Water")
                || tile.Properties.ContainsKey("Water")
                || tile.TileIndexProperties.ContainsKey("Buildings")
                || tile.Properties.ContainsKey("Buildings");
        }

        private static string GetDisplayName(GameLocation location)
        {
            if (location == null)
                return "";

            if (!string.IsNullOrWhiteSpace(location.DisplayName))
                return location.DisplayName;

            return location.Name;
        }

        private bool ShouldShowScrollbar()
        {
            int visibleRows = (WindowHeight - 24 - EdgePad * 2) / ElemHeight;
            return this.Locs.Count > visibleRows;
        }

        private int GetScrollCap()
        {
            int visibleHeight = WindowHeight - 24 - EdgePad;
            return Math.Max(0, this.Locs.Count * ElemHeight - visibleHeight);
        }

        private void ClampScroll()
        {
            int cap = this.GetScrollCap();

            if (this.Scroll > 0)
                this.Scroll = 0;

            if (this.Scroll < -cap)
                this.Scroll = -cap;
        }

        private void UpdateScrollbar()
        {
            int scrollbarWidth = 6 * Game1.pixelZoom - 4;

            if (!this.ShouldShowScrollbar())
            {
                this.Scrollbar = new Rectangle(this.ScrollbarBack.X + 2, this.ScrollbarBack.Y + 2, scrollbarWidth, this.ScrollbarBack.Height - 4);
                return;
            }

            int visibleRows = Math.Max(1, (WindowHeight - 24 - EdgePad * 2) / ElemHeight);
            float visiblePercent = Math.Min(1f, visibleRows / (float)Math.Max(1, this.Locs.Count));

            int height = Math.Max(24, (int)(this.ScrollbarBack.Height * visiblePercent) - 4);
            int scrollCap = this.GetScrollCap();

            int yOffset = scrollCap > 0
                ? (int)(-this.Scroll / (float)scrollCap * (this.ScrollbarBack.Height - 4 - height))
                : 0;

            this.Scrollbar = new Rectangle(this.ScrollbarBack.X + 2, this.ScrollbarBack.Y + 2 + yOffset, scrollbarWidth, height);
        }

        private static bool HasDiscoveredTeleportLocation(Farmer player, string locationName)
        {
            if (player == null || string.IsNullOrWhiteSpace(locationName))
                return false;

            if (player.modData.ContainsKey("moonslime.Wizardry.TeleportTo." + locationName))
                return true;

            // Fallback for older saves / safety, but don't trust this to always exist.
            return player.locationsVisited?.Contains(locationName) == true;
        }

        private static string ResolveWarpTargetName(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return targetName;

            if (targetName == "BoatTunnel")
                return "IslandSouth";

            if (targetName == "Trailer" && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
                return "Trailer_Big";

            foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
            {
                if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data)
                    && data.MapReplacements != null
                    && data.MapReplacements.TryGetValue(targetName, out string replacement))
                {
                    return replacement;
                }
            }

            return targetName;
        }
    }
}
