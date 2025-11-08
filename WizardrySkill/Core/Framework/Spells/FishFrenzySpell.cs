using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile;
using xTile.Layers;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>
    /// A Nature spell that summons a legitimate fish frenzy (bubbling fishing node)
    /// at the player's cursor position if it's cast over a water tile.
    /// </summary>
    public class FishFrenzySpell : Spell
    {
        private static readonly FieldInfo FishSplashPointTimeField =
            AccessTools.Field(typeof(GameLocation), "fishSplashPointTime");

        public FishFrenzySpell() : base(SchoolId.Nature, "fish_frenzy") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 10;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            if (!base.CanCast(player, level))
                return false;

            var location = player.currentLocation;
            Vector2 cursorTile = Game1.currentCursorTile;
            Point currentSplash = location.fishSplashPoint.Value;

            return location.isWaterTile((int)cursorTile.X, (int)cursorTile.Y)
                   && currentSplash.Equals(Point.Zero)
                   && PlayerHasFish(player);
        }

        public static bool PlayerHasFish(Farmer player)
        {
            return player.Items.Any(i => i is StardewValley.Object { Category: StardewValley.Object.FishCategory });
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;

            if (player.Items.FirstOrDefault(i => i is StardewValley.Object { Category: -4 } obj) is not StardewValley.Object fishItem)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            var location = player.currentLocation;
            Point tile = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            if (!location.isWaterTile(tile.X, tile.Y))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            player.Items.ReduceId(fishItem.QualifiedItemId, 1);
            this.TrySpawnFishFrenzy(location, tile, player);

            return new SpellSuccess(player, "slosh", 10);
        }

        private void TrySpawnFishFrenzy(GameLocation location, Point tile, Farmer player)
        {
            Layer backLayer = RequireLayer(location.map, "Back");
            int fishSplashTime = FishSplashPointTimeField?.GetValue(location) as int? ?? 0;

            Random random = Utility.CreateDaySaveRandom(Game1.timeOfDay, backLayer.LayerWidth);
            Point currentSplash = location.fishSplashPoint.Value;
            var tileVector = Utility.PointToVector2(tile);

            if (!currentSplash.Equals(Point.Zero) || (location is Farm && Game1.whichFarm != 1))
                return;

            for (int k = 0; k < 3; k++)
            {
                if (!location.isOpenWater(tile.X, tile.Y) || location.doesTileHaveProperty(tile.X, tile.Y, "NoFishing", "Back") != null)
                    continue;

                int distanceToLand = FishingRod.distanceToLand(tile.X, tile.Y, location);
                if (distanceToLand <= 1 || distanceToLand >= 5)
                    continue;

                if (player.currentLocation.Equals(location))
                    location.playSound("waterSlosh");

                if (random.NextDouble() < ((location is Beach) ? 0.008 : 0.01) &&
                    Game1.Date.TotalDays > 3 &&
                    (player.fishCaught.Count() > 2 || Game1.Date.TotalDays > 14) &&
                    !Utility.isFestivalDay())
                {
                    Item fish = location.getFish(random.Next(500), "", distanceToLand, player, 0.0, tileVector);

                    if (fish.Category == -4 && !fish.HasContextTag("fish_legendary"))
                    {
                        location.fishFrenzyFish.Value = fish.QualifiedItemId;
                        string locationName = location.DisplayName;
                        string fishName = TokenStringBuilder.ItemNameFor(fish);
                        string displayName = TokenStringBuilder.CapitalizeFirstLetter(TokenStringBuilder.ArticleFor(fishName));
                        Game1.Multiplayer.broadcastGlobalMessage(
                            $"Strings\\1_6_Strings:FishFrenzy_{locationName}", false, null, fishName, displayName);
                    }
                }

                FishSplashPointTimeField?.SetValue(location, Game1.timeOfDay);
                location.fishSplashPoint.Value = tile;
                break;
            }
        }

        public static Layer RequireLayer(Map map, string layerId)
        {
            return map.GetLayer(layerId) ?? throw new KeyNotFoundException($"The '{map.assetPath}' map doesn't have required layer '{layerId}'.");
        }
    }
}
