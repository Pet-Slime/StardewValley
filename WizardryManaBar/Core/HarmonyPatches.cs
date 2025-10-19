using System;
using System.Collections.Generic;
using System.Linq;
using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Objects;
using static BirbCore.Attributes.SEvent;
using static SpaceCore.Skills;

namespace WizardryManaBar.Core
{
    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "GetHeightAdjustment")]
    class GetHeightAdjustment_patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix( string[] buffIconsToDisplay, Item hoveredItem, int height,ref int __result)
        {
            __result = ManaToolTip_patch.GetHeightAdjustment(buffIconsToDisplay, hoveredItem, height);
        }
    }

    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "GetWidthAdjustment")]
    class GetWidthAdjustment_patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix( SpriteFont font,  Item hoveredItem,  int width, ref int __result)
        {
            __result = ManaToolTip_patch.GetWidthAdjustment(font, hoveredItem, width);
        }
    }

    [HarmonyPatch("SpaceCore.Patches.SkillBuffPatcher", "DrawAdditionalBuffEffects")]
    class DrawAdditionalBuffEffects_patch
    {
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix( SpriteBatch b,  SpriteFont font, Item hoveredItem, int x, int y, ref int __result)
        {
            __result = ManaToolTip_patch.DrawAdditionalBuffEffects(b, font, hoveredItem, x, y);
        }
    }



    class ManaToolTip_patch
    {
        private const string ManaFill = "moonslime.ManaBarApi.ManaFill";



        public static int GetHeightAdjustment(string[] buffIconsToDisplay, Item hoveredItem, int height)
        {
            if (hoveredItem is null)
            {
                return height;
            }

            bool addedAny = false;
            if (hoveredItem.GetContextTags().Any(tag => tag.StartsWith(ManaFill)))
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
            if (hoveredItem == null)
                return width;

            if (hoveredItem.GetContextTags().Any(tag => tag.StartsWith(ManaFill)))
            {
                string manaTitle = ModEntry.Instance.I18N.Get("moonslime.ManaBarApi.Mana");
                width = Math.Max(width, (int)font.MeasureString("+999 " + manaTitle).X) + 92;
            }
            return width;
        }

        public static int DrawAdditionalBuffEffects(SpriteBatch b, SpriteFont font, Item hoveredItem, int x, int y)
        {
            if (hoveredItem == null)
                return y;

            var tags = hoveredItem.GetContextTags();

            foreach (string tag in tags)
            {
                if (!tag.StartsWith(ManaFill))
                    continue;

                // Expected format: "ManaFill/value"
                int separatorIndex = tag.IndexOf('/');
                if (separatorIndex <= 0 || separatorIndex >= tag.Length - 1)
                    break; // malformed tag, skip

                if (!float.TryParse(tag.AsSpan(separatorIndex + 1), out float manaValue))
                    break;

                Vector2 offset = new Vector2(16 + 4, 16);
                Point spacing = new Point(34, 34);
                DrawManaBuffEffect(b, new Vector2(x, y) + offset, manaValue, font: font, spacing: spacing.X);

                y += spacing.Y;
                break;
            }

            return y;
        }

        /// <summary>
        /// Draws a buff icon and formatted buff effect value label in the style of a Mana buff.
        /// </summary>
        /// <param name="position">Local display pixel draw position.</param>
        /// <param name="value">Stamina regeneration value.</param>
        /// <param name="drawText">Whether to draw label in addition to buff effect value.</param>
        /// <param name="font">Font used to draw label. Defaults to <see cref="Game1.smallFont"/>.</param>
        /// <param name="alpha">Opacity of icon and label when drawn.</param>
        /// <param name="spacing">Display pixel spacing between icon and text.</param>
        /// <param name="shadowAlpha">Relative opacity of shadow when drawn.</param>
        public static void DrawManaBuffEffect(SpriteBatch b, Vector2 position, float value, bool drawText = true, SpriteFont font = null, float alpha = 1, int spacing = 8 * Game1.pixelZoom, float shadowAlpha = 1)
        {
            string manaTitle = ModEntry.Instance.I18N.Get("moonslime.ManaBarApi.Mana");
            DrawBuffEffect(b, position, value, drawText ? manaTitle : null, font, ModEntry.Assets.ManaSymbol, new Rectangle(0, 0, 10, 10), alpha: alpha, spacing: spacing, shadowAlpha: shadowAlpha);
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
        public static void DrawBuffEffect(SpriteBatch b, Vector2 position, float value, string label = null, SpriteFont font = null, Texture2D icon = null, Rectangle? iconSource = null, float alpha = 1, int spacing = 8 * Game1.pixelZoom, float shadowAlpha = 1)
        {
            string text = SkillBuff.FormattedBuffEffect(value, label);
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
