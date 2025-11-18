using System;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace WizardryManaBar.Core
{
    internal class Events
    {
        private const string ManaFill = "moonslime.ManaBarApi.ManaFill";
        private const string ManaRestore = "moonslime.ManaBarApi.ManaRestore";

        public static Vector2 BarPosition;
        private static Vector2 SizeUI;
        private static Vector2 LastViewportSize;
        private static Texture2D ManaFg;

        private static double LastManaRatio = double.NaN;
        private static Color LastManaColor = Color.Transparent;

        // Temporary reusable objects
        private static Rectangle SharedRect = new();
        private static readonly StringBuilder HoverTextBuilder = new();
        private static readonly Color[] SingleColorBuffer = new Color[1];


        private static int LastExtraCount = -1; // track previous bar offsets

        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Initialize mana texture
            SingleColorBuffer[0] = new Color(0, 48, 255);
            ManaFg = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            ManaFg.SetData(SingleColorBuffer);

            // Hook events
            SpaceEvents.OnItemEaten += OnItemEaten;
            ModEntry.Instance.Helper.Events.Display.RenderingHud += OnRenderedHud;
            ModEntry.Instance.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private static void OnItemEaten(object sender, EventArgs args)
        {
            if (sender is not Farmer player) return;

            var item = player.itemToEat;
            if (item == null)
            {
                Log.Warn("OnItemEaten called but no item was found.");
                return;
            }

            foreach (string tag in item.GetContextTags())
            {
                bool isFill = tag.StartsWith(ManaFill);
                bool isRestore = !isFill && tag.StartsWith(ManaRestore);
                if (!isFill && !isRestore) continue;

                int sep = tag.IndexOf('/');
                if (sep <= 0 || sep >= tag.Length - 1) continue;

                ReadOnlySpan<char> valueSpan = tag.AsSpan(sep + 1);

                if (isFill && int.TryParse(valueSpan, out int manaValue))
                {
                    int qualityAdjustment = item.Quality;
                    manaValue = (int)Math.Floor(manaValue * (1 + qualityAdjustment * 0.4));
                    player.AddMana(manaValue);
                }
                else if (isRestore && float.TryParse(valueSpan, out float manaPercent))
                {
                    player.AddMana((int)(player.GetMaxMana() * (manaPercent / 100)));
                }
            }
        }

        [EventPriority(EventPriority.Low)]
        public static void OnRenderedHud(object sender, RenderingHudEventArgs e)
        {
            // Skip drawing if menus are open, events are active, or the player can't act
            if  (!Context.IsPlayerFree || Game1.farmEvent != null || Game1.displayHUD == false)
                return;

            var player = Game1.player;


            if (player.GetMaxMana() > 0 && ModEntry.Config.RenderManaBar)
            {
                UpdateManaTexture(player);
                SetBarsPosition();
                BeginDrawManaBar(e.SpriteBatch);
            }
        }

        private static void UpdateManaTexture(Farmer player)
        {
            double ratio = GetManaRatio(player);
            Color manaColor = ApplyColorOffset(new Color(0, 48, 255), ratio);

            if (Math.Abs(ratio - LastManaRatio) > 0.001 || manaColor != LastManaColor)
            {
                SingleColorBuffer[0] = manaColor;
                ManaFg.SetData(SingleColorBuffer);
                LastManaRatio = ratio;
                LastManaColor = manaColor;
            }
        }

        private static void BeginDrawManaBar(SpriteBatch e)
        {
            #region Used Variables.

            int safeXCoordinate = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;
            int safeYCoordinate = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom;

            int barWidth = 12;
            int barHeaderHeight = 16;
            int barBottomPosition = Assets.ManaBG.Height - barHeaderHeight;
            int drawedBarsHeight = default;

            int overchargeHeight = Convert.ToInt32(Math.Ceiling(GetManaOvercharge() * ModEntry.Config.SizeMultiplier));

            Vector2 topOfBar = new(safeXCoordinate + 3 + ModEntry.Config.XManaBarOffset + BarPosition.X,
                                   safeYCoordinate - CalculateYOffsetToManaBar(barHeaderHeight, overchargeHeight, barBottomPosition) +
                                                     ModEntry.Config.YManaBarOffset);
            #endregion

            // Drawing Bar Layout.
            SharedRect = DrawManaBarTop(e, barWidth, barHeaderHeight, ref drawedBarsHeight, topOfBar);
            SharedRect = DrawManaBarBody(e, barWidth, barHeaderHeight, ref drawedBarsHeight, overchargeHeight, out SharedRect, out Rectangle destRect, topOfBar);
            SharedRect = DrawManaBarBottom(e, barWidth, barBottomPosition, ref drawedBarsHeight, topOfBar);

            // Filling Layout with Content.
            DrawManaBarFiller(e, barHeaderHeight, barBottomPosition, drawedBarsHeight, out SharedRect, out destRect, topOfBar);
            DrawManaBarShade(e, destRect);
            DrawManaBarHoverText(e, drawedBarsHeight, topOfBar);
        }


        private static double GetManaOvercharge()
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            double maxMana = player.GetMaxMana();
            double overchargeValue = maxMana / WizardryManaBar.Core.Api.BaseMaxMana;

            // This will prevent bar to grow limitless and exceed monitor area.
            return overchargeValue <= ModEntry.Config.MaxOverchargeValue ? overchargeValue : ModEntry.Config.MaxOverchargeValue;
        }

        private static Rectangle DrawManaBarTop(SpriteBatch e, int barWidth, int barHeaderHeight, ref int drawedBarsHeight, Vector2 topOfBar)
        {
            Rectangle srcRect = new Rectangle(268, 408, barWidth, barHeaderHeight);
            Rectangle manaIconRect = new Rectangle(0, 0, 12, 12);
            e.Draw(
                Game1.mouseCursors,
                topOfBar,
                srcRect,
                Color.White,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f
            );

            e.Draw(
                Assets.ManaBarIcon,
                topOfBar,
                manaIconRect,
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
            srcRect = new Rectangle(268, 408+barHeaderHeight, barWidth, 20);
            destRect = new Rectangle(Convert.ToInt32(topOfBar.X),
                                     Convert.ToInt32(topOfBar.Y + drawedBarsHeight * Game1.pixelZoom),
                                     barWidth * 4,
                                     barHeaderHeight + (Assets.ManaBG.Height - barHeaderHeight * 2) +
                                                        Convert.ToInt32(overchargeHeight * Game1.pixelZoom));

            e.Draw(
                Game1.mouseCursors,
                destRect,
                srcRect,
                Color.White
            );

            drawedBarsHeight += destRect.Height;
            return srcRect;
        }

        private static Rectangle DrawManaBarBottom(SpriteBatch e, int barWidth, int barBottomPosition, ref int drawedBarsHeight, Vector2 topOfBar)
        {
            Rectangle srcRect = new Rectangle(268, 408+barBottomPosition, barWidth, 16);
            e.Draw(
                Game1.mouseCursors,
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
            destRect = new Rectangle(Convert.ToInt32(topOfBar.X + Assets.ManaBG.Width * (int)Math.PI),
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


        private static double GetManaRatio()
        {
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            double currentMana = player.GetCurrentMana() * 1.0;
            double maxMana = player.GetMaxMana() * 1.0;

            return currentMana / maxMana;
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



        private static int CalculateYOffsetToManaBar(int headerHeight, double oversize, int bottomPos)
        {
            const int bottomMargin = 24;
            int height = Assets.ManaBG.Height + headerHeight * 2;
            height += Assets.ManaBG.Height - headerHeight * 2 + (int)(oversize * Game1.pixelZoom);
            height += bottomPos + bottomMargin;
            return height;
        }

        private static Color ApplyColorOffset(Color color, double offset)
        {
            const byte redMaxOffset = 255;
            const byte greenMaxOffset = 207;

            byte r = (byte)(Math.Abs(offset - 1) * redMaxOffset);
            byte g = (byte)(Math.Abs(offset - 1) * greenMaxOffset);

            return new Color(color.R + r, color.G + g, color.B);
        }

        private static double GetManaRatio(Farmer player)
            => player.GetCurrentMana() / (double)player.GetMaxMana();

        private static double GetManaOvercharge(Farmer player)
        {
            double overchargeValue = player.GetMaxMana() / WizardryManaBar.Core.Api.BaseMaxMana;
            return Math.Min(overchargeValue, ModEntry.Config.MaxOverchargeValue);
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var player = Game1.player;
            if ((int)player.Stamina != player.MaxStamina)
                player.AddMana((int)(player.GetMaxMana() * 0.5));
            else
                player.SetManaToMax();
        }


        public static void SetBarsPosition()
        {
            if (!Context.IsWorldReady) return;

            Vector2 currentViewport = new(Game1.uiViewport.Width, Game1.uiViewport.Height);

            // Compute how many extra bars shift the mana bar
            int extraCount = 0;
            if (Game1.showingHealth) extraCount++;
            if (ModEntry.MagicStardewLoaded) extraCount++;

            // Only update if viewport size or extra count changed
            if (currentViewport == LastViewportSize && extraCount == LastExtraCount)
                return;

            LastViewportSize = currentViewport;
            LastExtraCount = extraCount;
            SizeUI = currentViewport;

            BarPosition.X = ModEntry.Config.BarsPosition
                ? SizeUI.X - (116f + 55f * extraCount)
                : 0;
        }
    }
}
