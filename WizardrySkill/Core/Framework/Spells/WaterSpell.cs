using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class WaterSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the school (Toil) and spell ID ("water")
        public WaterSpell()
            : base(SchoolId.Toil, "water") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        // The mana cost of casting the spell (1 per cast)
        public override int GetManaCost(Farmer player, int level)
        {
            return 1;
        }

        // Main effect of the spell
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should run the watering logic.
            // Remote machines observe the cast packet but do not replay terrain/object mutation.
            if (!player.IsLocalPlayer)
                return null;

            // Create a fake efficient watering can to simulate watering.
            WateringCan water = new();
            water.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(water, "lastUser").SetValue(player);

            level += 1; // Increase radius
            int actionCount = 0; // Track number of tiles watered
            int manaCost = this.GetManaCost(player, level);

            GameLocation loc = player.currentLocation;

            // Convert pixel coordinates to tile coordinates.
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            Vector2 target = new(tileX, tileY);

            // Get all tiles affected by the spell (radius = level).
            List<Vector2> affectedTiles = Utilities.TilesAffected(target, level, player);

            // Loop over each tile in the affected area.
            foreach (Vector2 tile in affectedTiles)
            {
                // Stop if the player is out of mana for additional tiles.
                if (!this.CanContinueCast(player, level))
                    continue;

                bool didAction = false;

                // Water crops, grass, etc.
                if (loc.terrainFeatures.TryGetValue(tile, out var terrainFeature))
                {
                    terrainFeature.performToolAction(water, 0, tile);
                    didAction = true;
                }

                // Water objects if applicable, like crops in pots.
                if (loc.objects.TryGetValue(tile, out var obj))
                {
                    obj.performToolAction(water);
                    didAction = true;
                }

                // Special case for Volcano Dungeon lava/water tiles.
                if (loc is VolcanoDungeon && loc.isWaterTile((int)tile.X, (int)tile.Y))
                {
                    loc.performToolAction(water, (int)tile.X, (int)tile.Y);
                    didAction = true;
                }

                // Apply effects only if the tile was actually affected.
                if (!didAction)
                    continue;

                // Visual effects for watering.
                Game1.Multiplayer.broadcastSprites(
                    loc,
                    new TemporaryAnimatedSprite(
                        13,
                        new Vector2(tile.X * 64f, tile.Y * 64f),
                        Color.White,
                        10,
                        Game1.random.NextBool(),
                        70f,
                        0,
                        64,
                        (tile.Y * 64f + 32f) / 10000f - 0.01f)
                    {
                        delayBeforeAnimationStart = actionCount * 10
                    });

                // Reduce mana after the first tile.
                if (actionCount != 0)
                    player.AddMana(-manaCost);

                actionCount++;
                Utilities.AddEXP(player, 2);
                loc.playSound("wateringCan", tile);
            }

            // If no tiles were watered, the spell fizzles.
            return actionCount == 0
                ? new SpellFizzle(player, manaCost)
                : null;
        }
    }
}
