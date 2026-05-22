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
        private readonly List<string> Locs = new();

        private string WarpTo;

        private int Scroll;
        private Rectangle ScrollbarBack;
        private Rectangle Scrollbar;

        private bool DragScroll;
        private bool JustClicked;


        /*********
        ** Public methods
        *********/
        public TeleportMenu(Farmer player)
            : base((Game1.uiViewport.Width - WindowWidth) / 2, (Game1.uiViewport.Height - WindowHeight) / 2, WindowWidth, WindowHeight, true)
        {
            this.Player = player;

            foreach (GameLocation loc in Game1.locations)
            {
                if (player.modData.ContainsKey("moonslime.Wizardry.TeleportTo." + loc.Name))
                    this.Locs.Add(loc.Name);
            }

            this.Locs.Remove("Summit");
            this.Locs.Remove("BeachNightMarket");
            this.Locs.Remove("DesertFestival");

            this.Locs.Sort(StringComparer.OrdinalIgnoreCase);

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
                string destination = this.WarpTo;
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
                foreach (string loc in this.Locs)
                {
                    Rectangle area = new(x, iy - 4, w - this.ScrollbarBack.Width, ElemHeight);
                    if (area.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        b.Draw(Game1.staminaRect, area, new Color(200, 32, 32, 64));

                        if (this.JustClicked)
                            this.WarpTo = loc;
                    }

                    b.DrawString(Game1.dialogueFont, loc, new Vector2(x + EdgePad, iy), Color.Black);
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
        private void TryWarpTo(string locationName)
        {
            if (this.Player == null || !this.Player.IsLocalPlayer)
                return;

            if (string.IsNullOrWhiteSpace(locationName))
                return;

            if (!this.Player.Items.ContainsId("moonslime.Wizardry.Travel_Core"))
            {
                Game1.playSound("cancel");
                return;
            }

            GameLocation destination = Game1.getLocationFromName(locationName);
            if (destination == null)
            {
                Game1.playSound("cancel");
                return;
            }

            if (!this.TryFindRandomWarpTile(destination, out Vector2 warpTile))
            {
                Game1.playSound("cancel");
                return;
            }

            Game1.activeClickableMenu = null;

            this.Player.Items.ReduceId("moonslime.Wizardry.Travel_Core", 1);

            Game1.playSound("wand");
            Utilities.AddEXP(this.Player, 50);

            Game1.warpFarmer(locationName, (int)warpTile.X, (int)warpTile.Y, false);
        }

        private bool TryFindRandomWarpTile(GameLocation location, out Vector2 warpTile)
        {
            warpTile = Vector2.Zero;

            if (location?.map?.Layers == null || location.map.Layers.Count == 0)
                return false;

            int mapWidth = location.map.Layers[0].LayerWidth;
            int mapHeight = location.map.Layers[0].LayerHeight;

            if (mapWidth <= 0 || mapHeight <= 0)
                return false;

            for (int attempt = 0; attempt < RandomTileAttempts; attempt++)
            {
                Vector2 candidate = new(Game1.random.Next(mapWidth), Game1.random.Next(mapHeight));

                if (!this.IsSoftValidWarpTile(location, candidate))
                    continue;

                Vector2 openTile = Utility.recursiveFindOpenTileForCharacter(this.Player, location, candidate, NearbySearchIterations, allowOffMap: false);

                if (openTile == Vector2.Zero)
                    continue;

                if (!this.IsSoftValidWarpTile(location, openTile))
                    continue;

                warpTile = openTile;
                return true;
            }

            return this.TryFindFallbackWarpTile(location, mapWidth, mapHeight, out warpTile);
        }

        private bool TryFindFallbackWarpTile(GameLocation location, int mapWidth, int mapHeight, out Vector2 warpTile)
        {
            warpTile = Vector2.Zero;

            Vector2 center = new(mapWidth / 2f, mapHeight / 2f);
            Vector2 openTile = Utility.recursiveFindOpenTileForCharacter(this.Player, location, center, maxIterations: 80, allowOffMap: false);

            if (openTile == Vector2.Zero)
                return false;

            if (!this.IsSoftValidWarpTile(location, openTile))
                return false;

            warpTile = openTile;
            return true;
        }

        private bool IsSoftValidWarpTile(GameLocation location, Vector2 tile)
        {
            if (location == null)
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
    }
}
