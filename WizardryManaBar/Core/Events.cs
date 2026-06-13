using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace WizardryManaBar.Core
{
    public class Events
    {
        private const string ManaFill = "moonslime.ManaBarApi.ManaFill";
        private const string ManaRestore = "moonslime.ManaBarApi.ManaRestore";

        private const int ManaBarOriginalBaseMana = 100;
        private const int ManaBarBaseHeight = 168;
        private const int ManaBarVisualBase = 50;
        private const int ManaBarVisualCap = 250;
        private const int ManaBarMaxHeight = ManaBarBaseHeight + (ManaBarVisualCap - ManaBarOriginalBaseMana);

        private static int ManaBarManaPerChunk => Math.Max(1, ModEntry.Config.ManaBarGrowthLimit);

        private const int ManaPipSourceWidth = 3;
        private const int ManaPipSourceHeight = 6;
        private const int ManaPipDrawScale = 4;
        private const int ManaPipSourceOverlap = 1;

        private static readonly Rectangle ManaPipSourceRect = new Rectangle(398, 497, ManaPipSourceWidth, ManaPipSourceHeight);

        private const int ManaPipDrawWidth = ManaPipSourceWidth * ManaPipDrawScale;
        private const int ManaPipDrawHeight = ManaPipSourceHeight * ManaPipDrawScale;
        private const int ManaPipOverlap = ManaPipSourceOverlap * ManaPipDrawScale;
        private const int ManaPipStep = ManaPipDrawHeight - ManaPipOverlap;

        // --- Cached static bar data ---
        private static int CachedMaxMana = -1;
        private static int CachedVisualMana = -1;
        private static int CachedManaBarManaPerChunk = -1;
        private static int CachedXManaBarOffset;
        private static int CachedYManaBarOffset;
        private static Point CachedViewportSize;
        private static float CachedBarsPosition;
        private static int CachedBarFullHeight;
        private static Vector2 CachedTopOfBar;
        private static Vector2 CachedBottomOfBar;
        private static Rectangle CachedMiddleRect;
        private static Rectangle CachedBottomRect;

        // --- Cached dynamic filler ---
        private static int CachedCurrentMana = -1;
        private static Color CachedManaColor;
        private static Rectangle CachedFillerRect;
        private static Rectangle[] CachedManaPipRects = Array.Empty<Rectangle>();

        public static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
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
            if (!Game1.IsHudDrawn)
                return;

            if (Game1.eventUp || Game1.farmEvent != null)
            {
                return;
            }

            var player = Game1.player;
            if (player.GetMaxMana() <= 0 || !ModEntry.Config.RenderManaBar)
                return;

            DrawManaBar(e.SpriteBatch, player);
        }

        private static void DrawManaBar(SpriteBatch sb, Farmer player)
        {
            var viewportSize = new Point(Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height);
            float barsPosition = SetBarsPosition();

            // Update caches
            UpdateStaticBarCache(player, viewportSize, barsPosition);
            UpdateFillerCache(player);

            // Draw everything
            DrawStaticBar(sb);
            DrawFiller(sb);
            DrawManaPips(sb);
            DrawHoverText(sb);
        }

        private static void UpdateStaticBarCache(Farmer player, Point viewportSize, float barsPosition)
        {
            int maxMana = player.GetMaxMana();
            int xOffset = ModEntry.Config.XManaBarOffset;
            int yOffset = ModEntry.Config.YManaBarOffset;
            int manaPerChunk = ManaBarManaPerChunk;

            if (CachedMaxMana != maxMana || CachedViewportSize != viewportSize || CachedBarsPosition != barsPosition || CachedXManaBarOffset != xOffset || CachedYManaBarOffset != yOffset || CachedManaBarManaPerChunk != manaPerChunk)
            {
                CachedMaxMana = maxMana;
                CachedManaBarManaPerChunk = manaPerChunk;
                CachedVisualMana = GetVisualManaForMaxMana(maxMana, manaPerChunk);
                CachedBarsPosition = barsPosition;
                CachedViewportSize = viewportSize;
                CachedXManaBarOffset = xOffset;
                CachedYManaBarOffset = yOffset;

                // The bar's maximum physical size remains the old 250-mana size.
                // The player's max mana is scaled against the configured mana-per-chunk value to decide the current visual height.
                CachedBarFullHeight = GetBarHeightForVisualMana(CachedVisualMana);

                // Anchor the bar from the bottom so the bottom cap and mana pips stay stable while the top grows upward.
                CachedBottomOfBar = new Vector2(Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Right - 48 - 8, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 80);

                if (Game1.isOutdoorMapSmallerThanViewport())
                    CachedBottomOfBar.X = Math.Min(CachedBottomOfBar.X, Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 48 - Game1.viewport.X);

                if (Game1.staminaShakeTimer > 0)
                {
                    CachedBottomOfBar.X += Game1.random.Next(-3, 4);
                    CachedBottomOfBar.Y += Game1.random.Next(-3, 4);
                }

                CachedBottomOfBar.X -= barsPosition + xOffset;
                CachedBottomOfBar.Y += yOffset;

                CachedTopOfBar = new Vector2(CachedBottomOfBar.X, CachedBottomOfBar.Y - (CachedBarFullHeight - 8));

                // Precompute rectangles
                CachedMiddleRect = new Rectangle((int)CachedTopOfBar.X, (int)(CachedTopOfBar.Y + 64f), 48, Math.Max(1, CachedBarFullHeight - 64));
                CachedBottomRect = new Rectangle((int)CachedBottomOfBar.X, (int)CachedBottomOfBar.Y, 48, 16);

                // Update dynamic draw data whenever the static position/height changes.
                UpdateDynamicManaCache(player);
            }
        }

        private static void UpdateFillerCache(Farmer player)
        {
            if (CachedCurrentMana != player.GetCurrentMana())
                UpdateDynamicManaCache(player);
        }

        private static void UpdateDynamicManaCache(Farmer player)
        {
            CachedCurrentMana = player.GetCurrentMana();

            int displayMana = GetDisplayMana(CachedCurrentMana);
            int displayMaxMana = Math.Max(1, Math.Min(CachedMaxMana, ManaBarManaPerChunk));
            float manaPercent = MathHelper.Clamp((float)displayMana / displayMaxMana, 0f, 1f);

            CachedManaColor = GetBlueToWhiteLerpColor(manaPercent);

            int height = (int)(manaPercent * CachedBarFullHeight);
            int fillerBottom = CachedBottomRect.Y + 56;
            CachedFillerRect = new Rectangle((int)CachedBottomOfBar.X + 12, fillerBottom - height, 24, height);

            int pipCount = Math.Min(GetManaPipCount(CachedCurrentMana), GetMaxManaPipsThatFit(fillerBottom));
            CachedManaPipRects = BuildManaPipRects(pipCount, fillerBottom);
        }

        private static int GetVisualManaForMaxMana(int maxMana, int manaPerChunk)
        {
            if (maxMana <= 0)
                return ManaBarVisualBase;

            float ratio = MathHelper.Clamp((float)maxMana / manaPerChunk, 0f, 1f);
            int visualMana = (int)(ManaBarVisualCap * ratio);
            return Math.Clamp(visualMana, ManaBarVisualBase, ManaBarVisualCap);
        }

        private static int GetBarHeightForVisualMana(int visualMana)
        {
            int clampedVisualMana = Math.Clamp(visualMana, ManaBarVisualBase, ManaBarVisualCap);
            float ratio = (float)(clampedVisualMana - ManaBarVisualBase) / (ManaBarVisualCap - ManaBarVisualBase);
            return ManaBarBaseHeight + (int)Math.Round(ratio * (ManaBarMaxHeight - ManaBarBaseHeight));
        }

        private static int GetMaxManaPipsThatFit(int fillerBottom)
        {
            int availableHeight = fillerBottom - (int)CachedTopOfBar.Y;

            if (availableHeight < ManaPipDrawHeight)
                return 0;

            return 1 + (availableHeight - ManaPipDrawHeight) / ManaPipStep;
        }

        private static int GetDisplayMana(int currentMana)
        {
            if (currentMana <= 0)
                return 0;

            int manaPerChunk = ManaBarManaPerChunk;
            int remainder = currentMana % manaPerChunk;
            return remainder == 0 ? manaPerChunk : remainder;
        }

        private static int GetManaPipCount(int currentMana)
        {
            int manaPerChunk = ManaBarManaPerChunk;

            if (currentMana <= manaPerChunk)
                return 0;

            return (currentMana - 1) / manaPerChunk;
        }

        private static Rectangle[] BuildManaPipRects(int pipCount, int fillerBottom)
        {
            if (pipCount <= 0)
                return Array.Empty<Rectangle>();

            Rectangle[] rects = new Rectangle[pipCount];

            int x = (int)(CachedBottomOfBar.X + 24 - ManaPipDrawWidth / 2) + 5;

            for (int i = 0; i < pipCount; i++)
                rects[i] = new Rectangle(x, fillerBottom - ManaPipDrawHeight - i * ManaPipStep, ManaPipDrawWidth, ManaPipDrawHeight);

            return rects;
        }

        private static void DrawStaticBar(SpriteBatch sb)
        {
            sb.Draw(Game1.mouseCursors, CachedTopOfBar, new Rectangle(268, 408, 12, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            sb.Draw(Assets.ManaBarIcon, CachedTopOfBar, new Rectangle(0, 0, 12, 12), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            sb.Draw(Game1.mouseCursors, CachedMiddleRect, new Rectangle(256, 424, 12, 16), Color.White);
            sb.Draw(Game1.mouseCursors, new Vector2(CachedBottomRect.X, CachedBottomRect.Y), new Rectangle(256, 448, 12, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        }

        private static void DrawFiller(SpriteBatch sb)
        {
            if (CachedFillerRect.Height <= 0)
                return;

            sb.Draw(Game1.staminaRect, CachedFillerRect, CachedManaColor);

            Rectangle bottomShadeRect = CachedFillerRect;
            bottomShadeRect.Height = Math.Min(4, CachedFillerRect.Height);

            Color bottomShade = CachedManaColor;
            bottomShade.R = (byte)Math.Max(0, bottomShade.R - 50);
            bottomShade.G = (byte)Math.Max(0, bottomShade.G - 50);
            sb.Draw(Game1.staminaRect, bottomShadeRect, bottomShade);
        }

        private static void DrawManaPips(SpriteBatch sb)
        {
            for (int i = CachedManaPipRects.Length - 1; i >= 0; i--)
                sb.Draw(Game1.mouseCursors, CachedManaPipRects[i], ManaPipSourceRect, Color.White);
        }

        private static void DrawHoverText(SpriteBatch sb)
        {
            if (Game1.getOldMouseX() >= CachedTopOfBar.X && Game1.getOldMouseY() >= CachedTopOfBar.Y && Game1.getOldMouseX() < CachedTopOfBar.X + 32f)
            {
                string text = Math.Max(0, CachedCurrentMana) + "/" + CachedMaxMana;
                float textWidth = Game1.dialogueFont.MeasureString(text).X;
                Game1.drawWithBorder(text, Color.Black * 0f, Color.Blue, CachedTopOfBar + new Vector2(-textWidth - 32f, 64f));
            }
        }

        public static Color GetBlueToWhiteLerpColor(float power)
        {
            power = MathHelper.Clamp(power, 0f, 1f);
            int r = (int)((1f - power) * 255f);
            int g = (int)((1f - power) * 255f);
            int b = 255;
            return new Color(r, g, b);
        }

        public static float SetBarsPosition()
        {
            int extraCount = 1;
            if (Game1.showingHealth) extraCount++;
            if (ModEntry.MagicStardewLoaded) extraCount++;

            extraCount += ModEntry.Config.ManaBarExtraSnaps;

            if (ModEntry.Config.ManaBarExtraSnaps == -1)
            {
                return 56f;
            }
            else
            {
                return 56f * extraCount;
            }
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var player = Game1.player;

            float staminaPercent = player.Stamina / player.MaxStamina;  // ratio 0–1
            int maxMana = player.GetMaxMana();

            // Set mana to the same percent as stamina
            player.SetMana((int)(maxMana * staminaPercent));
        }
    }
}
