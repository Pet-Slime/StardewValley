using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;

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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

        // The mana cost of casting the spell (1 per cast)
        public override int GetManaCost(Farmer player, int level)
        {
            return 1;
        }

        // Main effect of the spell
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        // Called when the spell is received through the spell sync system
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            // Only the host should mutate shared terrain/object/location state
            if (!Context.IsMainPlayer)
                return null;

            if (caster == null || caster.currentLocation == null)
                return null;

            // Create a fake efficient watering can to simulate watering
            WateringCan water = new();
            water.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(water, "lastUser").SetValue(caster);

            level += 1; // Increase radius
            int actionCount = 0; // Track number of tiles watered

            GameLocation loc = caster.currentLocation;

            // Convert pixel coordinates to tile coordinates
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);

            // Get all tiles affected by the spell (radius = level)
            List<Vector2> list = Utilities.TilesAffected(target, level, caster);

            // Loop over each tile in the affected area
            foreach (Vector2 tile in list)
            {
                // Skip if the player is out of mana
                if (!this.CanContinueCast(caster, level))
                    continue;

                bool didAction = false;

                // Water crops, grass, etc.
                if (loc.terrainFeatures.TryGetValue(tile, out var value))
                {
                    value.performToolAction(water, 0, tile);
                    didAction = true;
                }

                // Water objects if applicable (e.g., crops in pots)
                if (loc.objects.TryGetValue(tile, out var value2))
                {
                    value2.performToolAction(water);
                    didAction = true;
                }

                // Special case for Volcano Dungeon tiles
                if (loc is VolcanoDungeon && loc.isWaterTile((int)tile.X, (int)tile.Y))
                {
                    loc.performToolAction(water, (int)tile.X, (int)tile.Y);
                    didAction = true;
                }

                // Apply effects only if the tile was actually affected
                if (didAction)
                {
                    // Visual effects for watering
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

                    // Reduce mana after the first tile
                    if (actionCount != 0)
                        caster.AddMana(-this.GetManaCost(caster, level));

                    actionCount++;
                    Utilities.AddEXP(caster, 2); // Give experience
                    loc.playSound("wateringCan", tile); // Play watering sound
                }
            }

            // If no tiles were watered, the spell fizzles
            return actionCount == 0 && caster.IsLocalPlayer
                ? new SpellFizzle(caster, this.GetManaCost(caster, level))
                : null;
        }
    }
}
