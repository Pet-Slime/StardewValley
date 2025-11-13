using AthleticSkill.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared;
using SpaceCore;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Tools;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace AthleticSkill.Core.Patches
{
    [HarmonyPatch(typeof(Axe), nameof(Axe.beginUsing))]
    public class AxeBeginUsing_patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Axe __instance, Farmer who)
        {
            // Fast exit — skip logic if profession or sprinting state not active
            if (ModEntry.UseAltProfession
                || !who.HasCustomProfession(Athletic_Skill.Athletic10a1)
                || __instance.UpgradeLevel <= 0
                || !who.modData.GetBool(Events.SprintingOn))
                return true;

            // Halt movement before triggering custom swing animation
            who.Halt();

            // Cache direction for multiple uses
            int facing = who.FacingDirection;

            // Apply correct animation frame and tool update
            int frame = facing switch
            {
                Game1.up => 176,
                Game1.right => 168,
                Game1.down => 160,
                Game1.left => 184,
                _ => 160 // safe default
            };

            // Set the sprite frame once
            who.FarmerSprite.setCurrentFrame(frame);

            // Update tool behavior (do once, after animation frame)
            __instance.Update(facing, 0, who);

            return false;
        }
    }

    [HarmonyPatch(typeof(Axe), nameof(Axe.DoFunction))]
    public class AxeFunction_patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Axe __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            if (!ModEntry.UseAltProfession && who.HasCustomProfession(Athletic_Skill.Athletic10a1) && __instance.UpgradeLevel > 0 && who.modData.GetBool(Events.SprintingOn))
            {
                LumberjackBuff(__instance, location, x, y, power, who);
                return false; // Skip original
            }
            return true;
        }

        // Instead of just copy pasting the original block of code
        // Break it up into multiple smaller methods to make it easier to read and mantaine
        private static void LumberjackBuff(Axe tool, GameLocation location, int originalX, int originalY, int power, Farmer who)
        {
            tool.lastUser = who;
            Game1.recentMultiplayerRandom = Utility.CreateRandom((short)Game1.random.Next(short.MinValue, short.MaxValue));

            // Apply stamina drain
            if (!tool.IsEfficient)
                who.Stamina -= (2 * power) - who.ForagingLevel * 0.1f;

            power = who.toolPower.Value;
            who.stopJittering();
            Vector2 originTile = new(originalX / 64, originalY / 64);
            foreach (Vector2 tile in Utilities.TilesAffected(originTile, power, who))
            {
                int tileX = (int)tile.X;
                int tileY = (int)tile.Y;
                Rectangle tileBox = new(tileX * 64, tileY * 64, 64, 64);

                if (IsTreeStump(location, tileX, tileY))
                {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Axe.cs.14023"));
                    continue;
                }

                // Temporarily buff tool power
                tool.UpgradeLevel += tool.additionalPower.Value;

                location.performToolAction(tool, tileX, tileY);

                HandleTerrainFeatures(location, tile, tool);
                HandleLargeTerrainFeatures(location, tileBox, tile, tool);
                HandleObjects(location, tile, who, tool);

                // Revert tool power
                tool.UpgradeLevel -= tool.additionalPower.Value;
            }
        }

        private static bool IsTreeStump(GameLocation location, int tileX, int tileY)
        {
            var tile = location.Map.RequireLayer("Buildings").Tiles[tileX, tileY];
            return tile?.TileIndexProperties.ContainsKey("TreeStump") == true;
        }

        private static void HandleTerrainFeatures(GameLocation location, Vector2 tile, Tool tool)
        {
            if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature.performToolAction(tool, 0, tile))
            {
                location.terrainFeatures.Remove(tile);
            }
        }

        private static void HandleLargeTerrainFeatures(GameLocation location, Rectangle tileBox, Vector2 pos, Tool tool)
        {
            if (location.largeTerrainFeatures is { Count: > 0 })
            {
                for (int i = location.largeTerrainFeatures.Count - 1; i >= 0; i--)
                {
                    var feature = location.largeTerrainFeatures[i];
                    if (feature.getBoundingBox().Intersects(tileBox) && feature.performToolAction(tool, 0, pos))
                    {
                        location.largeTerrainFeatures.RemoveAt(i);
                    }
                }
            }
        }

        private static void HandleObjects(GameLocation location, Vector2 pos, Farmer who, Tool tool)
        {
            if (location.Objects.TryGetValue(pos, out var obj) && obj.Type != null && obj.performToolAction(tool))
            {
                if (obj.Type == "Crafting" && obj.Fragility != 2)
                {
                    location.debris.Add(new Debris(
                        obj.QualifiedItemId,
                        who.GetToolLocation(),
                        Utility.PointToVector2(who.StandingPixel)
                    ));
                }

                obj.performRemoveAction();
                location.Objects.Remove(pos);
            }
        }
    }

}
