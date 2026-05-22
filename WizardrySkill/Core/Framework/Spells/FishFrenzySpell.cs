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
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>
    /// A Nature spell that summons a legitimate fish frenzy near the player's cursor position
    /// if the targeted area contains valid fishing water.
    /// </summary>
    public class FishFrenzySpell : Spell
    {
        /*********
        ** Fields
        *********/

        // How far from the clicked tile the spell can search for valid fishing water.
        private const int TargetSearchRadius = 4;

        // How many times the spell should try to find a valid non-legendary frenzy fish.
        private const int FishSelectionAttempts = 12;

        // Private vanilla field used to control how long the splash/frenzy point lasts.
        private static readonly FieldInfo FishSplashPointTimeField = AccessTools.Field(typeof(GameLocation), "fishSplashPointTime");


        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school and ID.
        public FishFrenzySpell()
            : base(SchoolId.Nature, "fish_frenzy") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        // Returns the mana cost for casting this spell.
        public override int GetManaCost(Farmer player, int level)
        {
            return 10;
        }

        // Defines the maximum level this spell can be cast at.
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            SObject fish = GetFishReagent(player);
            if (fish == null)
                return new Dictionary<string, int>();

            return new Dictionary<string, int>
            {
                [fish.QualifiedItemId] = 1
            };
        }

        // Checks whether the player can currently cast this spell.
        public override bool CanCast(Farmer player, int level)
        {
            if (!base.CanCast(player, level))
                return false;

            GameLocation location = player.currentLocation;
            if (location == null)
                return false;

            Point cursorTile = new((int)Game1.currentCursorTile.X, (int)Game1.currentCursorTile.Y);

            // The spell needs a valid water tile near the cursor.
            // The fish reagent is checked by base.CanCast through GetItemCost.
            return TryFindValidFrenzyTile(location, cursorTile, out _, out _);
        }

        // Checks whether the player has any valid non-legendary fish to use as a reagent.
        public static bool PlayerHasFish(Farmer player)
        {
            return GetFishReagent(player) != null;
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should consume fish and create the fish frenzy state.
            // Remote machines observe the cast packet but do not replay item or location mutation.
            if (!player.IsLocalPlayer)
                return null;

            GameLocation location = player.currentLocation;
            Point targetTile = new(targetX / Game1.tileSize, targetY / Game1.tileSize);

            // Check the target area before removing any existing frenzy.
            if (!TryFindValidFrenzyTile(location, targetTile, out Point frenzyTile, out int distanceToLand))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Pick a valid fish for this location before clearing the old frenzy or consuming the item cost.
            if (!TryChooseFrenzyFish(location, frenzyTile, distanceToLand, player, out Item frenzyFish))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Consume one non-legendary fish unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Only now clear the old fish frenzy/splash point.
            ClearFishFrenzy(location);

            // Spawn the new vanilla-style fish frenzy.
            SpawnFishFrenzy(location, frenzyTile, frenzyFish, player);

            return new SpellSuccess(player, "slosh", 10);
        }


        /*********
        ** Private methods
        *********/

        // Finds a valid frenzy tile near the target tile.
        private static bool TryFindValidFrenzyTile(GameLocation location, Point targetTile, out Point frenzyTile, out int distanceToLand)
        {
            frenzyTile = Point.Zero;
            distanceToLand = 0;

            if (location == null)
                return false;

            // Match vanilla farm restriction.
            if (location is Farm && Game1.whichFarm != 1)
                return false;

            // Try the exact target first.
            if (IsValidFrenzyTile(location, targetTile, out distanceToLand))
            {
                frenzyTile = targetTile;
                return true;
            }

            // Then search nearby tiles so the spell works when the cursor is close to valid water.
            List<Point> candidates = new();

            for (int x = targetTile.X - TargetSearchRadius; x <= targetTile.X + TargetSearchRadius; x++)
            {
                for (int y = targetTile.Y - TargetSearchRadius; y <= targetTile.Y + TargetSearchRadius; y++)
                {
                    Point candidate = new(x, y);
                    int dx = candidate.X - targetTile.X;
                    int dy = candidate.Y - targetTile.Y;

                    if (dx * dx + dy * dy <= TargetSearchRadius * TargetSearchRadius)
                        candidates.Add(candidate);
                }
            }

            // Randomize nearby candidates so repeated casts do not always choose the same nearby tile.
            foreach (Point candidate in candidates.OrderBy(_ => Game1.random.Next()))
            {
                if (!IsValidFrenzyTile(location, candidate, out distanceToLand))
                    continue;

                frenzyTile = candidate;
                return true;
            }

            return false;
        }

        // Checks whether a tile follows vanilla fish frenzy water rules.
        private static bool IsValidFrenzyTile(GameLocation location, Point tile, out int distanceToLand)
        {
            distanceToLand = 0;

            if (location == null)
                return false;

            Vector2 tileVector = new(tile.X, tile.Y);

            // Make sure the tile is on the map.
            if (!location.isTileOnMap(tileVector))
                return false;

            // Vanilla requires open water and no NoFishing property.
            if (!location.isOpenWater(tile.X, tile.Y))
                return false;

            if (location.doesTileHaveProperty(tile.X, tile.Y, "NoFishing", "Back") != null)
                return false;

            // Vanilla requires the fishing spot to be near land, but not too close.
            distanceToLand = FishingRod.distanceToLand(tile.X, tile.Y, location);
            return distanceToLand > 1 && distanceToLand < 5;
        }

        // Chooses a valid non-legendary fish for the frenzy.
        private static bool TryChooseFrenzyFish(GameLocation location, Point tile, int distanceToLand, Farmer player, out Item frenzyFish)
        {
            frenzyFish = null;

            if (location == null || player == null)
                return false;

            Vector2 tileVector = Utility.PointToVector2(tile);

            // Try multiple times because location.getFish can return non-fish or invalid results.
            for (int i = 0; i < FishSelectionAttempts; i++)
            {
                Item fish = location.getFish(Game1.random.Next(500), "", distanceToLand, player, 0.0, tileVector);

                if (fish == null)
                    continue;

                if (fish.Category != SObject.FishCategory)
                    continue;

                if (fish.HasContextTag("fish_legendary"))
                    continue;

                frenzyFish = fish;
                return true;
            }

            return false;
        }

        // Spawns the fish frenzy using Stardew's built-in location fields.
        private static void SpawnFishFrenzy(GameLocation location, Point tile, Item frenzyFish, Farmer player)
        {
            if (location == null || frenzyFish == null)
                return;

            // Set the vanilla fish frenzy fish and splash point.
            location.fishFrenzyFish.Value = frenzyFish.QualifiedItemId;
            FishSplashPointTimeField?.SetValue(location, Game1.timeOfDay);
            location.fishSplashPoint.Value = tile;

            // Play local water feedback if the caster is in this location.
            if (player?.currentLocation?.Equals(location) == true)
                location.playSound("waterSlosh");

            // Show the vanilla fish frenzy message when the location supports it.
            BroadcastFishFrenzyMessage(location, frenzyFish);
        }

        // Clears any existing fish frenzy or splash point in the location.
        private static void ClearFishFrenzy(GameLocation location)
        {
            if (location == null)
                return;

            location.fishFrenzyFish.Value = "";
            location.fishSplashPoint.Value = Point.Zero;
            FishSplashPointTimeField?.SetValue(location, 0);
        }

        // Broadcasts the vanilla fish frenzy global message.
        private static void BroadcastFishFrenzyMessage(GameLocation location, Item fish)
        {
            if (location == null || fish == null)
                return;

            if (!TryGetVanillaFrenzyLocationKey(location, out string locationKey))
                return;

            string fishName = TokenStringBuilder.ItemNameFor(fish);
            string displayName = TokenStringBuilder.CapitalizeFirstLetter(TokenStringBuilder.ArticleFor(fishName));

            Game1.Multiplayer.broadcastGlobalMessage(
                "Strings\\1_6_Strings:FishFrenzy_" + locationKey,
                false,
                null,
                fishName,
                displayName);
        }

        // Gets the vanilla message key for locations that support fish frenzy messages.
        private static bool TryGetVanillaFrenzyLocationKey(GameLocation location, out string locationKey)
        {
            locationKey = "";

            switch (location)
            {
                case Mountain:
                    locationKey = "mountain";
                    return true;

                case Forest:
                    locationKey = "forest";
                    return true;

                case Town:
                    locationKey = "town";
                    return true;

                case Beach:
                    locationKey = "beach";
                    return true;

                default:
                    return false;
            }
        }

        // Gets the first non-legendary fish in the player's inventory.
        private static SObject GetFishReagent(Farmer player)
        {
            return player?.Items?.FirstOrDefault(item =>
                item is SObject { Category: SObject.FishCategory } fish
                && !fish.HasContextTag("fish_legendary")) as SObject;
        }
    }
}
