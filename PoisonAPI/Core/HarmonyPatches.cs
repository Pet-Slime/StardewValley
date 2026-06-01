using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;
using StardewValley.Extensions;
using static SpaceCore.Skills;

namespace PoisonBarAPI.Core
{

    [HarmonyPatch(typeof(Game1), "drawHUD")]
    internal static class Game1DrawHUD_PoisonPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Game1.eventUp || Game1.farmEvent is not null)
                return;

            Farmer player = Game1.player;
            if (player is null)
                return;

            int poison = player.GetCurrentPoison();
            if (poison <= 0)
                return;

            // Poison forces the health bar to be considered visible.
            // This keeps other HUD spacing/snap logic aware that the health bar is being shown.
            Game1.showingHealthBar = true;
            Game1.showingHealth = true;

            DrawPoisonHealthOverlay(player, poison);
        }

        /// <summary>
        /// Draws the player's current poison as an overlay filling upward over the vanilla health bar.
        /// Poison uses max health as its visual cap, so poison and health are directly comparable.
        /// </summary>
        private static void DrawPoisonHealthOverlay(Farmer player, int poison)
        {
            int maxHealth = Math.Max(1, player.maxHealth);
            int health = Math.Clamp(player.health, 0, maxHealth);
            poison = Math.Clamp(poison, 0, maxHealth);

            int barHeight = 168 + (maxHealth - 100);
            int healthHeight = (int)(health / (float)maxHealth * barHeight);
            int poisonHeight = (int)(poison / (float)maxHealth * barHeight);

            if (poisonHeight <= 0)
                return;

            Rectangle safeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();

            Vector2 position = new Vector2(
                safeArea.Right - 48 - 8 - 56,
                safeArea.Bottom - 224 - 16 - (maxHealth - 100)
            );

            DrawHealthBarFrame(player, position, barHeight, safeArea);
            DrawHealthFill(player, position, barHeight, healthHeight);
            DrawPoisonFill(position, barHeight, poisonHeight);
            DrawHoverText(player, position, poison);
        }

        /// <summary>
        /// Draws the vanilla health bar frame so poison is visible even when vanilla would normally hide the health bar.
        /// </summary>
        private static void DrawHealthBarFrame(Farmer player, Vector2 position, int barHeight, Rectangle safeArea)
        {
            Color frameColor = GetHealthFrameColor(player);

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                position,
                new Rectangle(268, 408, 12, 16),
                frameColor,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                1f
            );

            int middleHeight = safeArea.Bottom - 64 - 16 - (int)(position.Y + 64f);
            if (middleHeight > 0)
            {
                Game1.spriteBatch.Draw(
                    Game1.mouseCursors,
                    new Rectangle((int)position.X, (int)(position.Y + 64f), 48, middleHeight),
                    new Rectangle(268, 424, 12, 16),
                    frameColor
                );
            }

            Game1.spriteBatch.Draw(
                Game1.mouseCursors,
                new Vector2(position.X, position.Y + 224f + barHeight - 168 - 64f),
                new Rectangle(268, 448, 12, 16),
                frameColor,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                1f
            );
        }

        /// <summary>
        /// Redraws the vanilla health fill underneath poison.
        /// </summary>
        private static void DrawHealthFill(Farmer player, Vector2 position, int barHeight, int healthHeight)
        {
            if (healthHeight <= 0)
                return;

            Rectangle healthFill = new Rectangle(
                (int)position.X + 12,
                (int)position.Y + 16 + 32 + barHeight - healthHeight,
                24,
                healthHeight
            );

            Color healthColor = Utility.getRedToGreenLerpColor(player.health / (float)Math.Max(1, player.maxHealth));
            Game1.spriteBatch.Draw(Game1.staminaRect, healthFill, Game1.staminaRect.Bounds, healthColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);

            healthFill.Height = 4;

            healthColor.R = (byte)Math.Max(0, healthColor.R - 50);
            healthColor.G = (byte)Math.Max(0, healthColor.G - 50);

            Game1.spriteBatch.Draw(Game1.staminaRect, healthFill, Game1.staminaRect.Bounds, healthColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);
        }

        /// <summary>
        /// Draws poison over the health fill, filling upward from the bottom of the health bar.
        /// </summary>
        private static void DrawPoisonFill(Vector2 position, int barHeight, int poisonHeight)
        {
            Rectangle poisonFill = new Rectangle(
                (int)position.X + 12,
                (int)position.Y + 16 + 32 + barHeight - poisonHeight,
                24,
                poisonHeight
            );

            Color poisonColor = new Color(ModEntry.Config.PoisonBarRed, ModEntry.Config.PoisonBarGreen, ModEntry.Config.PoisonBarBlue);
            Game1.spriteBatch.Draw(Game1.staminaRect, poisonFill, Game1.staminaRect.Bounds, poisonColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);

            poisonFill.Height = 4;

            Color poisonTopColor = new Color(ModEntry.Config.PoisonBarTopRed, ModEntry.Config.PoisonBarTopGreen, ModEntry.Config.PoisonBarTopBlue);
            Game1.spriteBatch.Draw(Game1.staminaRect, poisonFill, Game1.staminaRect.Bounds, poisonTopColor, 0f, Vector2.Zero, SpriteEffects.None, 1f);
        }

        /// <summary>
        /// Draws health and poison values when hovering over the health/poison bar.
        /// </summary>
        private static void DrawHoverText(Farmer player, Vector2 position, int poison)
        {
            if ((float)Game1.getOldMouseX() < position.X || (float)Game1.getOldMouseY() < position.Y || (float)Game1.getOldMouseX() >= position.X + 32f)
                return;

            string healthText = Math.Max(0, player.health) + "/" + player.maxHealth;
            string poisonText = Math.Max(0, poison) + "/" + player.health;

            float textWidth = Game1.dialogueFont.MeasureString("999/999").X;

            Game1.drawWithBorder(healthText, Color.Black * 0f, Color.Red, position + new Vector2(0f - textWidth - 32f, 64f));

            Color poisonTextColor = new Color(ModEntry.Config.PoisonBarTopRed, ModEntry.Config.PoisonBarTopGreen, ModEntry.Config.PoisonBarTopBlue);
            Game1.drawWithBorder(poisonText, Color.Black * 0f, poisonTextColor, position + new Vector2(0f - textWidth - 32f, 96f));
        }

        /// <summary>
        /// Matches vanilla's low-health frame pulse as closely as possible.
        /// </summary>
        private static Color GetHealthFrameColor(Farmer player)
        {
            if (player.health <= 0 || player.health >= 20)
                return Color.White;

            float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (player.health * 50f)) / 4f + 0.9f;
            return Color.Pink * pulse;
        }
    }



    [HarmonyPatch(typeof(Farmer), "updateCommon")]
    public static class FarmerUpdateCommon_SwimRegenPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var hook = AccessTools.Method(typeof(FarmerUpdateCommon_SwimRegenPatch), nameof(OnSwimRegen));

            for (int i = 2; i < list.Count; i++)
            {
                // look for the exact sequence:
                // ldarg.0
                // ldc.i4.s 100
                // stfld swimTimer
                if (list[i].opcode == OpCodes.Stfld &&
                    list[i].operand is FieldInfo fld &&
                    fld.Name == "swimTimer" &&
                    list[i - 1].opcode == OpCodes.Ldc_I4_S &&
                    (sbyte)list[i - 1].operand == 100 &&
                    list[i - 2].opcode == OpCodes.Ldarg_0)
                {
                    // Insert after stfld swimTimer
                    list.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(i + 2, new CodeInstruction(OpCodes.Call, hook));
                    break; // ensure NOT injecting again
                }
            }

            return list;
        }

        public static void OnSwimRegen(Farmer who)
        {
            if (who.GetCurrentPoison() > 0)
                who.AddPoison(-1);
        }
    }


    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "GetHeightAdjustment")]
    class GetHeightAdjustment_patch
    {
        [HarmonyPostfix]
        public static void Postfix(string[] buffIconsToDisplay, Item hoveredItem, int height, ref int __result)
        {
            __result = PoisonToolTip_patch.GetHeightAdjustment(buffIconsToDisplay, hoveredItem, __result);
        }
    }

    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "GetWidthAdjustment")]
    class GetWidthAdjustment_patch
    {
        [HarmonyPostfix]
        public static void Postfix(SpriteFont font, Item hoveredItem, int width, ref int __result)
        {
            __result = PoisonToolTip_patch.GetWidthAdjustment(font, hoveredItem, __result);
        }
    }

    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "DrawAdditionalBuffEffects")]
    class DrawAdditionalBuffEffects_patch
    {
        [HarmonyPostfix]
        public static void Postfix(SpriteBatch b, SpriteFont font, Item hoveredItem, int x, int y, ref int __result)
        {
            __result = PoisonToolTip_patch.DrawAdditionalBuffEffects(b, font, hoveredItem, x, __result);
        }
    }



    class PoisonToolTip_patch
    {
        private const string PoisonFill = "moonslime.PoisonBarApi.PoisonFill";
        private const string PoisonRestore = "moonslime.PoisonBarApi.PoisonRestore";



        public static int GetHeightAdjustment(string[] buffIconsToDisplay, Item hoveredItem, int height)
        {
            if (hoveredItem is null)
            {
                return height;
            }

            bool addedAny = false;
            if (hoveredItem.GetContextTags().Any(tag => tag.StartsWith(PoisonFill)))
            {
                addedAny = true;
                height += 34;
            }
            if (hoveredItem.GetContextTags().Any(tag => tag.StartsWith(PoisonRestore)))
            {
                addedAny = true;
                height += 34;
            }

            if (buffIconsToDisplay is null && addedAny)
            {
                height += 4;
            }

            return height;
        }

        public static int GetWidthAdjustment(SpriteFont font, Item hoveredItem, int width)
        {

            return width;
        }

        public static int DrawAdditionalBuffEffects(SpriteBatch b, SpriteFont font, Item hoveredItem, int x, int y)
        {
            if (hoveredItem == null)
                return y;

            const int offsetX = 20;   // 16 + 4
            const int offsetY = 16;
            const int spacing = 34;

            var tags = hoveredItem.GetContextTags();

            foreach (string tag in tags)
            {
                bool isFill = tag.StartsWith(PoisonFill);
                bool isRestore = !isFill && tag.StartsWith(PoisonRestore);

                if (!isFill && !isRestore)
                    continue;

                int separatorIndex = tag.IndexOf('/');
                if (separatorIndex <= 0 || separatorIndex >= tag.Length - 1)
                    continue; // skip malformed

                if (!float.TryParse(tag.AsSpan(separatorIndex + 1), out float PoisonValue))
                    continue;


                var position = new Vector2(x + offsetX, y + offsetY);

                if (isFill)
                {
                    int qualityAdjustment = hoveredItem.Quality;
                    PoisonValue = (float)Math.Floor(PoisonValue * (1 + (qualityAdjustment * 0.4)));
                    DrawPoisonFillEffect(b, position, PoisonValue, font: font, spacing: spacing);
                }
                else
                    DrawPoisonPercentEffect(b, position, PoisonValue, font: font, spacing: spacing);

                y += spacing;
            }

            return y;
        }

        /// <summary>
        /// Draws a buff icon and formatted buff effect value label in the style of a Poison buff.
        /// </summary>
        /// <param name="position">Local display pixel draw position.</param>
        /// <param name="value">Stamina regeneration value.</param>
        /// <param name="drawText">Whether to draw label in addition to buff effect value.</param>
        /// <param name="font">Font used to draw label. Defaults to <see cref="Game1.smallFont"/>.</param>
        /// <param name="alpha">Opacity of icon and label when drawn.</param>
        /// <param name="spacing">Display pixel spacing between icon and text.</param>
        /// <param name="shadowAlpha">Relative opacity of shadow when drawn.</param>
        public static void DrawPoisonFillEffect(SpriteBatch b, Vector2 position, float value, bool drawText = true, SpriteFont font = null, float alpha = 1, int spacing = 8 * Game1.pixelZoom, float shadowAlpha = 1)
        {
            string PoisonTitle = ModEntry.Instance.I18N.Get("moonslime.PoisonBarApi.Poison");
            SkillBuff.DrawBuffEffect(b, position, value, drawText ? PoisonTitle : null, font, Assets.PoisonSymbol, new Rectangle(0, 0, 10, 10), alpha: alpha, spacing: spacing, shadowAlpha: shadowAlpha);
        }

        /// <summary>
        /// Draws a buff icon and formatted buff effect value label in the style of a Poison buff.
        /// </summary>
        /// <param name="position">Local display pixel draw position.</param>
        /// <param name="value">Stamina regeneration value.</param>
        /// <param name="drawText">Whether to draw label in addition to buff effect value.</param>
        /// <param name="font">Font used to draw label. Defaults to <see cref="Game1.smallFont"/>.</param>
        /// <param name="alpha">Opacity of icon and label when drawn.</param>
        /// <param name="spacing">Display pixel spacing between icon and text.</param>
        /// <param name="shadowAlpha">Relative opacity of shadow when drawn.</param>
        public static void DrawPoisonPercentEffect(SpriteBatch b, Vector2 position, float value, bool drawText = true, SpriteFont font = null, float alpha = 1, int spacing = 8 * Game1.pixelZoom, float shadowAlpha = 1)
        {
            DrawPercentfEffect(b, position, value, null, font, Assets.PoisonSymbol, new Rectangle(0, 0, 10, 10), alpha: alpha, spacing: spacing, shadowAlpha: shadowAlpha);
        }

        /// <summary>
        /// Draws a buff icon and formatted buff effect value label.
        /// </summary>
        /// <param name="position">Local display pixel draw position.</param>
        /// <param name="value">Buff effect value.</param>
        /// <param name="label">Translated label. Will be formatted into a standardised style when drawn.</param>
        /// <param name="font">Font used to draw label. Defaults to <see cref="Game1.smallFont"/>.</param>
        /// <param name="icon">Texture used for buff icon.</param>
        /// <param name="iconSource">Area in icon texture used for buff icon when drawn. Defaults to entire texture.</param>
        /// <param name="alpha">Opacity of icon and label when drawn.</param>
        /// <param name="spacing">Display pixel spacing between icon and text.</param>
        /// <param name="shadowAlpha">Relative opacity of shadow when drawn.</param>
        public static void DrawPercentfEffect(SpriteBatch b, Vector2 position, float value, string label = null, SpriteFont font = null, Texture2D icon = null, Rectangle? iconSource = null, float alpha = 1, int spacing = 8 * Game1.pixelZoom, float shadowAlpha = 1)
        {
            string text = $"{value}%";
            int xOffset = 0;

            if (icon is not null)
            {
                Utility.drawWithShadow(b, icon, position, iconSource ?? icon.Bounds, Color.White * alpha, 0f, Vector2.Zero, 3f, flipped: false, layerDepth: 0.95f, shadowIntensity: 0.35f * shadowAlpha * alpha);
                xOffset += spacing;
            }
            Utility.drawTextWithShadow(b, text, font ?? Game1.smallFont, position + new Vector2(xOffset, 0), Game1.textColor * alpha);
        }
    }
}
