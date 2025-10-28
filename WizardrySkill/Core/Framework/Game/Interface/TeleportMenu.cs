using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

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
            : base((Game1.viewport.Width - WindowWidth) / 2, (Game1.viewport.Height - WindowHeight) / 2, WindowWidth, WindowHeight)
        {
            foreach (var loc in Game1.locations)
            {
                if (player.modData.ContainsKey("moonslime.Wizardry.TeleportTo." + loc.Name))
                    this.Locs.Add(loc.Name);

            }
            this.Locs.Remove("Summit");
            this.Locs.Remove("BeachNightMarket");
            this.Locs.Remove("DesertFestival");
            int x = this.xPositionOnScreen + 12, y = this.yPositionOnScreen + 12, w = WindowWidth - 24, h = WindowHeight - 24;
            this.ScrollbarBack = new Rectangle(x + w - Game1.pixelZoom * 6, y, Game1.pixelZoom * 6, h);
            this.Scrollbar = new Rectangle(this.ScrollbarBack.X + 2, this.ScrollbarBack.Y + 2, 6 * Game1.pixelZoom - 4, (int)(5.0 / this.Locs.Count * this.ScrollbarBack.Height) - 4);
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
                var locObj = Game1.getLocationFromName(this.WarpTo);
                int mapW = locObj.Map.Layers[0].LayerWidth;
                int mapH = locObj.map.Layers[0].LayerHeight;
                int tileSize = Game1.tileSize;

                var cloud = new CloudMount
                {
                    currentLocation = locObj,
                    Position = new Vector2(mapW * tileSize / 4, mapH * tileSize / 2)
                };
                Vector2 tileForCharacter = Utility.recursiveFindOpenTileForCharacter(cloud, locObj, cloud.Tile, 5, false);
                cloud.Position = new Vector2(tileForCharacter.X * tileSize, tileForCharacter.Y * tileSize);
                locObj.addCharacter(cloud);
                Game1.player.mount = cloud;
                cloud.rider = Game1.player;

                Game1.activeClickableMenu = null;
                Game1.warpFarmer(this.WarpTo, (int)cloud.Tile.X, (int)cloud.Tile.Y, false);
                Game1.player.mount.dismount();
                Game1.player.Items.ReduceId("moonslime.Wizardry.Travel_Core", 1);

            }

            if (this.DragScroll)
            {
                int my = Game1.getMouseY();
                int relY = my - (this.ScrollbarBack.Y + 2 + this.Scrollbar.Height / 2);
                relY = Math.Max(0, relY);
                relY = Math.Min(relY, this.ScrollbarBack.Height - 4 - this.Scrollbar.Height);
                float percY = relY / (this.ScrollbarBack.Height - 4f - this.Scrollbar.Height);
                int totalY = this.Locs.Count * ElemHeight - (WindowHeight - 24) + 16;
                this.Scroll = -(int)(totalY * percY);
            }
        }

        /// <inheritdoc />
        public override void draw(SpriteBatch b)
        {
            drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, WindowWidth, WindowHeight, Color.White);

            int x = this.xPositionOnScreen + 12, y = this.yPositionOnScreen + 12, w = WindowWidth - 24, h = WindowHeight - 24;

            b.End();
            using RasterizerState state = new RasterizerState
            {
                ScissorTestEnable = true
            };
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, state);
            b.GraphicsDevice.ScissorRectangle = new Rectangle(x, y, w, h);
            {
                int iy = y + EdgePad;
                iy += this.Scroll;
                foreach (string loc in this.Locs)
                {
                    Rectangle area = new Rectangle(x, iy - 4, w - this.ScrollbarBack.Width, ElemHeight);
                    if (area.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        b.Draw(Game1.staminaRect, area, new Color(200, 32, 32, 64));
                        if (this.JustClicked)
                        {
                            this.WarpTo = loc;
                        }
                    }

                    b.DrawString(Game1.dialogueFont, loc, new Vector2(x + EdgePad, iy), Color.Black);

                    iy += ElemHeight;
                }
            }
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (this.Locs.Count > h / ElemHeight)
            {
                this.Scrollbar.Y = this.ScrollbarBack.Y + 2 + (int)(this.Scroll / (float)-ElemHeight / (this.Locs.Count - (h - 20) / (float)ElemHeight) * (this.ScrollbarBack.Height - this.Scrollbar.Height));

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

            if (this.ScrollbarBack.Contains(x, y))
            {
                this.DragScroll = true;
            }
            else
            {
                this.JustClicked = true;
            }
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
            if (this.Scroll > 0)
                this.Scroll = 0;

            int cap = this.Locs.Count * 50 - (WindowHeight - 24) + 16;
            if (this.Scroll < -cap)
                this.Scroll = -cap;
        }
    }
}
