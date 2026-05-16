using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

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
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            var location = caster.currentLocation;
            Point tile = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            // Only the actual casting player should spend the fish reagent.
            if (caster.IsLocalPlayer)
            {
                if (caster.Items.FirstOrDefault(i => i is StardewValley.Object { Category: -4 } obj) is not StardewValley.Object fishItem)
                    return new SpellFizzle(caster, this.GetManaCost(caster, level));

                if (!location.isWaterTile(tile.X, tile.Y))
                    return new SpellFizzle(caster, this.GetManaCost(caster, level));

                caster.Items.ReduceId(fishItem.QualifiedItemId, 1);
            }

            // Only the host should mutate the shared location's fish frenzy state.
            if (Context.IsMainPlayer)
            {
                if (!location.isWaterTile(tile.X, tile.Y))
                    return caster.IsLocalPlayer ? new SpellFizzle(caster, this.GetManaCost(caster, level)) : null;

                this.TrySpawnFishFrenzy(location, tile, caster);
            }

            return caster.IsLocalPlayer
                ? new SpellSuccess(caster, "slosh", 10)
                : null;
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
