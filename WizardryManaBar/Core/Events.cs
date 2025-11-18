using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace WizardryManaBar.Core
{
    internal class Events
    {
        private const string ManaFill = "moonslime.ManaBarApi.ManaFill";
        private const string ManaRestore = "moonslime.ManaBarApi.ManaRestore";

        // --- Cached static bar data ---
        private static int CachedMaxMana = -1;
        private static Point CachedViewportSize;
        private static float CachedBarsPosition;
        private static int CachedBarFullHeight;
        private static Vector2 CachedTopOfBar;
        private static Rectangle CachedTopRect;
        private static Rectangle CachedMiddleRect;
        private static Rectangle CachedBottomRect;
        private static Rectangle CachedMIconRect;

        // --- Cached dynamic filler ---
        private static int CachedCurrentMana = -1;
        private static Color CachedManaColor;
        private static Rectangle CachedFillerRect;

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
            if (!Context.IsPlayerFree || Game1.farmEvent != null || !Game1.displayHUD)
                return;

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
            DrawHoverText(sb);
        }

        private static void UpdateStaticBarCache(Farmer player, Point viewportSize, float barsPosition)
        {
            if (CachedMaxMana != player.GetMaxMana() || CachedViewportSize != viewportSize || CachedBarsPosition != barsPosition)
            {
                CachedMaxMana = player.GetMaxMana();
                CachedBarsPosition = barsPosition;
                CachedViewportSize = viewportSize;

                // Compute bar height and cap it
                int calculatedHeight = 168 + (CachedMaxMana - 100);
                CachedBarFullHeight = Math.Min(calculatedHeight, 500); // 500 is the max bar height

                CachedTopOfBar = new Vector2(
                    Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Right - 48 - 8,
                    Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 224 - 16 - (CachedBarFullHeight - 168)
                );

                if (Game1.isOutdoorMapSmallerThanViewport())
                    CachedTopOfBar.X = Math.Min(CachedTopOfBar.X, Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 48 - Game1.viewport.X);

                if (Game1.staminaShakeTimer > 0)
                {
                    CachedTopOfBar.X += Game1.random.Next(-3, 4);
                    CachedTopOfBar.Y += Game1.random.Next(-3, 4);
                }

                CachedTopOfBar.X -= barsPosition;

                // Precompute rectangles
                CachedTopRect = new Rectangle((int)CachedTopOfBar.X, (int)CachedTopOfBar.Y, 48, 16); // top
                CachedMiddleRect = new Rectangle((int)CachedTopOfBar.X, (int)(CachedTopOfBar.Y + 64f), 48, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 64 - 16 - (int)(CachedTopOfBar.Y + 64f - 8f)); // middle
                CachedBottomRect = new Rectangle((int)CachedTopOfBar.X, (int)(CachedTopOfBar.Y + 224f + (CachedMaxMana - 100) - 64f), 48, 16); // bottom
                CachedMIconRect = new Rectangle((int)CachedTopOfBar.X, (int)CachedTopOfBar.Y, 48, 12); // M icon (scaled in draw call)

                // Update filler when ever the rest updates
                CachedCurrentMana = player.GetCurrentMana();
                float manaPercent = (float)CachedCurrentMana / CachedMaxMana;
                CachedManaColor = GetBlueToWhiteLerpColor(manaPercent);

                int height = (int)(manaPercent * CachedBarFullHeight);
                CachedFillerRect = new Rectangle((int)CachedTopOfBar.X + 12, (int)CachedTopOfBar.Y + 16 + 32 + CachedBarFullHeight - height, 24, height);
            }
        }

        private static void UpdateFillerCache(Farmer player)
        {
            if (CachedCurrentMana != player.GetCurrentMana())
            {
                CachedCurrentMana = player.GetCurrentMana();
                float manaPercent = (float)CachedCurrentMana / CachedMaxMana;
                CachedManaColor = GetBlueToWhiteLerpColor(manaPercent);

                int height = (int)(manaPercent * CachedBarFullHeight);
                CachedFillerRect = new Rectangle((int)CachedTopOfBar.X + 12, (int)CachedTopOfBar.Y + 16 + 32 + CachedBarFullHeight - height, 24, height);
            }
        }

        private static void DrawStaticBar(SpriteBatch sb)
        {
            sb.Draw(Game1.mouseCursors, CachedTopOfBar, new Rectangle?(new Rectangle(268, 408, 12, 16)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            sb.Draw(Assets.ManaBarIcon, CachedTopOfBar, new Rectangle?(new Rectangle(0, 0, 12, 12)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            sb.Draw(Game1.mouseCursors, CachedMiddleRect, new Rectangle?(new Rectangle(256, 424, 12, 16)), Color.White);
            sb.Draw(Game1.mouseCursors, new Vector2(CachedBottomRect.X, CachedBottomRect.Y), new Rectangle?(new Rectangle(256, 448, 12, 16)), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        }

        private static void DrawFiller(SpriteBatch sb)
        {
            sb.Draw(Game1.staminaRect, CachedFillerRect, CachedManaColor);

            Rectangle bottomShadeRect = CachedFillerRect;
            bottomShadeRect.Height = 4;
            Color bottomShade = CachedManaColor;
            bottomShade.R = (byte)Math.Max(0, bottomShade.R - 50);
            bottomShade.G = (byte)Math.Max(0, bottomShade.G - 50);
            sb.Draw(Game1.staminaRect, bottomShadeRect, bottomShade);
        }

        private static void DrawHoverText(SpriteBatch sb)
        {
            if (Game1.getOldMouseX() >= CachedTopOfBar.X && Game1.getOldMouseY() >= CachedTopOfBar.Y && Game1.getOldMouseX() < CachedTopOfBar.X + 32f)
            {
                Game1.drawWithBorder(Math.Max(0, CachedCurrentMana) + "/" + CachedMaxMana, Color.Black * 0f, Color.Blue,
                    CachedTopOfBar + new Vector2(-Game1.dialogueFont.MeasureString("999/999").X - 32f, 64f));
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
            } else
            {
                return 56f * extraCount;
            }

        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var player = Game1.player;
            if ((int)player.Stamina != player.MaxStamina)
                player.AddMana((int)(player.GetMaxMana() * 0.5));
            else
                player.SetManaToMax();
        }
    }
}
