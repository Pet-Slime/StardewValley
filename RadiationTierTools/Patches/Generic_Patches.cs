using System;
using System.Collections.Generic;
using System.Linq;
using MoonShared;
using HarmonyLib;
using StardewValley.Tools;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;

namespace RadiationTierTools.Patches
{
    internal class Generic_Patches
    {

        [HarmonyPatch(typeof(ResourceClump), nameof(ResourceClump.performToolAction))]
        public static class ResourceClumpToolActionPatch
        {
            public static void Prefix(ResourceClump __instance, Tool t)
            {
                if (t.UpgradeLevel != 6) // Mythicite
                    return;

                int[] axeIds = new int[] { 600, 602 };
                int[] pickaxeIds = new int[] { 622, 672, 752, 754, 756, 758 };

                if (t is Axe && axeIds.Contains(__instance.parentSheetIndex.Value) ||
                     t is Pickaxe && pickaxeIds.Contains(__instance.parentSheetIndex.Value))
                    __instance.health.Value = 0;
            }
        }

        [HarmonyPatch(typeof(Tool), "tilesAffected")]
        public static class ToolTilesAffectedPatch
        {
            public static bool Prefix(Vector2 tileLocation, ref int power, Farmer who, ref List<Vector2> __result)
            {

                Log.Error("Patch Fired");
                if (power >= 5) // Radioactive or above
                {
                    int rad = 2;
                    int len = 5;
                    if (power >= 6) // Mythicite
                    {
                        ++rad;
                        len += 2;
                    }

                    __result = new();

                    Vector2 dir = Vector2.Zero;
                    switch (who.FacingDirection)
                    {
                        case Game1.up: dir = new Vector2(0, -1); break;
                        case Game1.right: dir = new Vector2(1, 0); break;
                        case Game1.down: dir = new Vector2(0, 1); break;
                        case Game1.left: dir = new Vector2(-1, 0); break;
                    }
                    Vector2 perp = new Vector2(dir.Y, dir.X);

                    for (int il = 0; il < len; ++il)
                    {
                        for (int ir = -rad; ir <= rad; ++ir)
                        {
                            __result.Add(tileLocation + dir * il + perp * ir);
                        }
                    }

                    // Goldenrevolver asked for this for their WIP mod
                    ++power;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Tool), "get_" + nameof(Tool.Name))]
        public static class ToolNamePatch
        {
            public static bool Prefix(Tool __instance, ref string __result)
            {
                if (__instance.UpgradeLevel >= 5)
                {
                    string tier = __instance.UpgradeLevel == 5 ? "radioactive" : "mythicite";
                    string tool = "";
                    switch (__instance.BaseName)
                    {
                        case "Axe": tool = "axe"; break;
                        case "Watering Can": tool = "wcan"; break;
                        case "Pickaxe": tool = "pick"; break;
                        case "Hoe": tool = "hoe"; break;
                        case "Pail": tool = "pail"; break;
                        case "Milk Pail": tool = "milk_pail"; break;
                        case "Shears": tool = "shears"; break;
                        case "Pan": tool = "pan"; break;
                        case "Copper Pan": tool = "copper_pan"; break;
                    }
                    __result = I18n.GetByKey($"tool.{tool}.{tier}");
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Tool), "get_" + nameof(Tool.DisplayName))]
        public static class ToolDisplayNamePatch
        {
            public static bool Prefix(Tool __instance, ref string __result)
            {
                if (__instance.UpgradeLevel >= 5)
                {
                    __result = __instance.Name;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Tool), nameof(Tool.setNewTileIndexForUpgradeLevel))]
        public static class ToolSetUpgradeTileIndexPatch
        {
            public static void Prefix(Tool __instance, ref object __state)
            {
                if (__instance.UpgradeLevel >= 5)
                {
                    __state = __instance.UpgradeLevel;
                    __instance.UpgradeLevel = 4;
                }
            }

            public static void Postfix(Tool __instance, ref object __state)
            {
                if (__state != null)
                {
                    __instance.upgradeLevel.Value = (int)__state;
                }
            }
        }


        internal class ToolTextureState
        {
            public int upgrade;
            public Texture2D oldSpritesheet;
        }

        [HarmonyPatch(typeof(Tool), nameof(Tool.drawInMenu))]
        public static class ToolDrawMenuPatch
        {
            public static void Prefix(Tool __instance, ref object __state)
            {
                if (__instance.UpgradeLevel == 5)
                {
                    __state = new ToolTextureState() { upgrade = __instance.UpgradeLevel, oldSpritesheet = Game1.toolSpriteSheet };
                    ModEntry.Instance.Helper.Reflection.GetField<Texture2D>(typeof(Game1), "_toolSpriteSheet").SetValue(__instance.UpgradeLevel == 5 ? Assets.RadioactiveTools : Assets.MythiciteTools);
                    __instance.upgradeLevel.Value = 4;
                }
            }

            public static void Postfix(Tool __instance, ref object __state)
            {
                if (__state != null)
                {
                    ModEntry.Instance.Helper.Reflection.GetField<Texture2D>(typeof(Game1), "_toolSpriteSheet").SetValue((__state as ToolTextureState).oldSpritesheet);
                    __instance.upgradeLevel.Value = (__state as ToolTextureState).upgrade;
                }
            }
        }


        [HarmonyPatch(typeof(Game1), nameof(Game1.drawTool), new Type[] { typeof(Farmer), typeof(int) })]
        public static class Game1DrawToolPatch
        {
            public static void Prefix(Farmer f, ref object __state)
            {
                var tool = f.CurrentTool;
                if (tool.UpgradeLevel == 5)
                {
                    __state = new ToolTextureState() { upgrade = tool.UpgradeLevel, oldSpritesheet = Game1.toolSpriteSheet };
                    ModEntry.Instance.Helper.Reflection.GetField<Texture2D>(typeof(Game1), "_toolSpriteSheet").SetValue(tool.UpgradeLevel == 5 ? Assets.RadioactiveTools : Assets.MythiciteTools);
                    tool.upgradeLevel.Value = 4;
                }
            }

            public static void Postfix(Farmer f, ref object __state)
            {
                var tool = f.CurrentTool;
                if (__state != null)
                {
                    ModEntry.Instance.Helper.Reflection.GetField<Texture2D>(typeof(Game1), "_toolSpriteSheet").SetValue((__state as ToolTextureState).oldSpritesheet);
                    tool.upgradeLevel.Value = (__state as ToolTextureState).upgrade;
                }
            }
        }

        [HarmonyPatch(typeof(ReachingToolEnchantment), nameof(ReachingToolEnchantment.CanApplyTo))]
        public static class ReachingEnchantCanApplyPatch
        {
            public static bool Prefix(Item item, ref bool __result)
            {
                if (item is Tool t && (item is WateringCan || item is Hoe) && t.UpgradeLevel >= 4)
                {
                    __result = true;
                    return false;
                }
                __result = false;
                return false;
            }
        }
    }
}
