using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.APIs;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;

namespace WizardryManaBar.Core
{
    [SEvent]
    internal class Events
    {
        public static Vector2 barPosition;

        private static Vector2 sizeUI;

        private static Texture2D manaFg;

        private static Texture2D ManaFg
        {
            get
            {
                Color manaCol;
                if (Context.IsWorldReady)
                {
                    double offset = GetManaRatio();
                    manaCol = ApplyColorOffset(new Color(0, 48, 255), offset);

                    manaFg.SetData(new[] { manaCol });
                }
                else
                {
                    manaCol = new Color(0, 48, 255);
                    manaFg.SetData(new[] { manaCol });
                }

                return manaFg;
            }

            set => manaFg = value;
        }

        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Color manaCol = new(0, 48, 255);
            WizardryManaBar.Core.Events.ManaFg = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            ManaFg.SetData(new[] { manaCol });
        }


        [SEvent.RenderingHud]
        [EventPriority(EventPriority.Low)]
        public static void OnRenderedHud(object sender, RenderingHudEventArgs e)
        {
            // Skip if not applicable.
            if (Game1.activeClickableMenu != null || Game1.eventUp || !Context.IsPlayerFree)
                return;

            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            // Begin rendering, if mana is available and rendering is enabled.
            if (player.GetMaxMana() > 0 && ModEntry.Config.RenderManaBar)
                BeginDrawManaBar(e.SpriteBatch);
            SetBarsPosition();
        }

        #region Mana Bar Render Functions.

        private static void BeginDrawManaBar(SpriteBatch e)
        {
            #region Used Variables.

            int safeXCoordinate = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;
            int safeYCoordinate = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom;

            int barWidth = 12;
            int barHeaderHeight = 16;
            int barBottomPosition = ModEntry.Assets.ManaBG.Height - barHeaderHeight;
            int drawedBarsHeight = default;

            int overchargeHeight = Convert.ToInt32(Math.Ceiling(GetManaOvercharge() * ModEntry.Config.SizeMultiplier));

            Rectangle srcRect;
            Vector2 topOfBar = new(safeXCoordinate + 3 + ModEntry.Config.XManaBarOffset + barPosition.X,
                                   safeYCoordinate - CalculateYOffsetToManaBar(barHeaderHeight, overchargeHeight, barBottomPosition) +
                                                     ModEntry.Config.YManaBarOffset);
            #endregion

            // Drawing Bar Layout.
            srcRect = DrawManaBarTop(e, barWidth, barHeaderHeight, ref drawedBarsHeight, topOfBar);
            srcRect = DrawManaBarBody(e, barWidth, barHeaderHeight, ref drawedBarsHeight, overchargeHeight, out srcRect, out Rectangle destRect, topOfBar);
            srcRect = DrawManaBarBottom(e, barWidth, barBottomPosition, ref drawedBarsHeight, topOfBar);

            // Filling Layout with Content.
            DrawManaBarFiller(e, barHeaderHeight, barBottomPosition, drawedBarsHeight, out srcRect, out destRect, topOfBar);
            DrawManaBarShade(e, destRect);
            DrawManaBarHoverText(e, drawedBarsHeight, topOfBar);
        }

        private static Rectangle DrawManaBarTop(SpriteBatch e, int barWidth, int barHeaderHeight, ref int drawedBarsHeight, Vector2 topOfBar)
        {
            Rectangle srcRect = new Rectangle(0, 0, barWidth, barHeaderHeight);
            e.Draw(
                ModEntry.Assets.ManaBG,
                topOfBar,
                srcRect,
                Color.White,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f
            );
            drawedBarsHeight += srcRect.Height;
            return srcRect;
        }

        private static Rectangle DrawManaBarBody(SpriteBatch e, int barWidth, int barHeaderHeight, ref int drawedBarsHeight, int overchargeHeight, out Rectangle srcRect, out Rectangle destRect, Vector2 topOfBar)
        {
            srcRect = new Rectangle(0, barHeaderHeight, barWidth, 20);
            destRect = new Rectangle(Convert.ToInt32(topOfBar.X),
                                     Convert.ToInt32(topOfBar.Y + drawedBarsHeight * Game1.pixelZoom),
                                     barWidth * 4,
                                     barHeaderHeight + (ModEntry.Assets.ManaBG.Height - barHeaderHeight * 2) +
                                                        Convert.ToInt32(overchargeHeight * Game1.pixelZoom));

            e.Draw(
                ModEntry.Assets.ManaBG,
                destRect,
                srcRect,
                Color.White
            );

            drawedBarsHeight += destRect.Height;
            return srcRect;
        }

        private static Rectangle DrawManaBarBottom(SpriteBatch e, int barWidth, int barBottomPosition, ref int drawedBarsHeight, Vector2 topOfBar)
        {
            Rectangle srcRect = new Rectangle(0, barBottomPosition, barWidth, 16);
            e.Draw(
                ModEntry.Assets.ManaBG,
                new Vector2(topOfBar.X, topOfBar.Y + drawedBarsHeight + barBottomPosition),
                srcRect,
                Color.White,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f
            );
            drawedBarsHeight += srcRect.Height + barBottomPosition;
            return srcRect;
        }

        private static void DrawManaBarFiller(SpriteBatch e, int barHeaderHeight, int barBottomPosition, int drawedBarsHeight, out Rectangle srcRect, out Rectangle destRect, Vector2 topOfBar)
        {
            double currentManaPercent = GetManaRatio();
            int srcHeight = Convert.ToInt32(barBottomPosition);
            int fillerWidth = 6;

            /** Magical Numbers:
             * Yes, here we are using MagicSkillCodeal numbers. There are two of them.
             * 40 — Additional offset, to prevent render magic filler above bottom of bar;
             * 12 — Additional negative offset, to prevent magic filler overflow bar.
             * 
             * Also, we using check to current mana percent, to prevent magic overflow too.
             **/
            srcRect = new Rectangle(barHeaderHeight, barBottomPosition, fillerWidth, srcHeight);
            destRect = new Rectangle(Convert.ToInt32(topOfBar.X + ModEntry.Assets.ManaBG.Width * (int)Math.PI),
                                     Convert.ToInt32(topOfBar.Y + drawedBarsHeight + 40),
                                     fillerWidth * Game1.pixelZoom,
                                     Convert.ToInt32((drawedBarsHeight - 12) *
                                                     (currentManaPercent > 1.0 ? 1.0 : currentManaPercent)));

            e.Draw(
                ManaFg,
                destRect,
                srcRect,
                Color.White,
                (float)Math.PI,
                Vector2.Zero,
                SpriteEffects.None,
                1f
            );
        }

        private static void DrawManaBarShade(SpriteBatch e, Rectangle destRect)
        {
            destRect.Height = 4;
            e.Draw(
                Game1.staminaRect,
                destRect,
                Game1.staminaRect.Bounds,
                Color.Black * 0.3f,
                (float)Math.PI,
                Vector2.Zero,
                SpriteEffects.None,
                1f
            );
        }

        private static void DrawManaBarHoverText(SpriteBatch e, int drawedBarsHeight, Vector2 topOfBar)
        {
            var vector = topOfBar;

            if (Game1.getOldMouseX() >= vector.X && Game1.getOldMouseY() >= vector.Y && Game1.getOldMouseX() < vector.X + 32f)
            {
                e.DrawString(Game1.dialogueFont,
                    $"{Game1.player.GetCurrentMana()}/{Game1.player.GetMaxMana()}",
                    vector + new Vector2(0f - Game1.dialogueFont.MeasureString("999/9999").X - 16f, 64f),
                    new Color(0, 48, 255));
            }
        }
        #endregion

        private static int CalculateYOffsetToManaBar(int barHeaderHeight, double oversize, int barBottomPosition)
        {
            /** Variable: 'bottomMargin'.
             *
             * After base calculations, we get value, that lies right on game screen border.
             * But we need to make small margin. So we need this variable to this needs.
             * Value set to 24, cause with this value mana bar will have same margin as other bars.
             **/
            const int bottomMargin = 24;
            int height = ModEntry.Assets.ManaBG.Height;

            height += barHeaderHeight * 2;
            height += ModEntry.Assets.ManaBG.Height - barHeaderHeight * 2 + Convert.ToInt32(oversize * Game1.pixelZoom);
            height += barBottomPosition;
            height += bottomMargin;

            return height;
        }

        private static bool CheckXAxisToMouseIntersection(float xTopPosition, out int xPosition)
        {
            xPosition = Game1.getOldMouseX();

            return xPosition >= xTopPosition && xPosition < xTopPosition + 36f;
        }

        private static bool CheckYAxisToMouseIntersection(int drawedBarsHeight, float yTopPosition, out int yPosition)
        {
            yPosition = Game1.getOldMouseY();

            return yPosition >= yTopPosition && yPosition < yTopPosition + drawedBarsHeight + 46f;
        }

        /*********
        ** Private methods
        *********/
        private static Color ApplyColorOffset(Color color, double offset)
        {
            byte redMaxOffset = 255;
            byte greenMaxOffset = 207;

            byte currentRedOffset = (byte)(Math.Abs(offset - 1) * redMaxOffset);
            byte currentGreenOffset = (byte)(Math.Abs(offset - 1) * greenMaxOffset);

            return new Color(color.R + currentRedOffset, color.G + currentGreenOffset, color.B);
        }

        private static double GetManaRatio()
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            double currentMana = player.GetCurrentMana() * 1.0;
            double maxMana = player.GetMaxMana() * 1.0;

            return currentMana / maxMana;
        }

        private static double GetManaOvercharge()
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            double maxMana = player.GetMaxMana();
            double overchargeValue = maxMana / WizardryManaBar.Core.Api.BaseMaxMana;

            // This will prevent bar to grow limitless and exceed monitor area.
            return overchargeValue <= ModEntry.Config.MaxOverchargeValue ? overchargeValue : ModEntry.Config.MaxOverchargeValue;
        }

        private static void HandleAddManaCommand(string[] args)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            player.AddMana(int.Parse(args[0]));
        }

        private static void HandleSetMaxManaCommand(string[] args)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            player.SetMaxMana(int.Parse(args[0]));
        }


        [SEvent.DayStarted]
        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            ///Give the player only half mana if they passed out from the night before
            if ((int)player.Stamina != player.MaxStamina)
            {
                int manaToRestore = (int)(player.GetMaxMana() * 0.5);
                player.AddMana(manaToRestore);
            }
            else
            {
                player.SetManaToMax();
            }
        }


        public static void SetBarsPosition()
        {
            if (!Context.IsWorldReady) return;

            sizeUI = new Vector2(Game1.uiViewport.Width, Game1.uiViewport.Height);
            if (ModEntry.Config.BarsPosition)
            {
                barPosition.X = GetPositionInRightBottomCorner();
            }
            else
            {
                barPosition.X = 0;
            }
        }


        private static float GetPositionInRightBottomCorner()
        {
            float basePosition = 116f;
            float offset = 55f;

            bool[] conditions = {
                CheckToDangerous(),
                false, // ultimateIsVisible
                ModEntry.MagicStardewLoaded
            };

            basePosition += conditions.Count(c => c) * offset;

            return sizeUI.X - basePosition;
        }

        private static bool CheckToDangerous() =>
                    Game1.showingHealth;
    }
}
