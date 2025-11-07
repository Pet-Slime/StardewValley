using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HarvestSpell" that automatically harvests grass within a target area
    public class HarvestSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HarvestSpell()
            : base(SchoolId.Toil, "harvest")
        {
            // SchoolId.Toil identifies the spell's magical school
            // "harvest" is the internal name for this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 3; // Base mana cost per harvested tile
        }

        public override int GetMaxCastingLevel()
        {
            return 1; // Max spell level is 1
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Spell range is fixed at 5 tiles around the target
            level = 5;

            // Convert pixel coordinates to tile coordinates
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            int num = 0; // Tracks the number of tiles harvested

            GameLocation loc = player.currentLocation;

            // Loop through all tiles in a square around the target
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    // Stop if out of mana
                    if (!this.CanContinueCast(player, level))
                        return null;

                    Vector2 tile = new Vector2(tileX, tileY);

                    // Skip tiles that are not grass
                    if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) || feature is not Grass)
                        continue;

                    // Remove the grass (harvest)
                    loc.terrainFeatures.Remove(tile);

                    // Attempt to add hay to the farm; show a HUD message if successful
                    if (Game1.getFarm().tryToAddHay(1) == 0) // returns number left
                        Game1.addHUDMessage(new HUDMessage("Hay", HUDMessage.achievement_type, true));

                    // Deduct extra mana for additional tiles
                    if (num != 0)
                        player.AddMana(-3);

                    // Give experience points
                    Utilities.AddEXP(player, 2 * (level + 1));

                    // Play cutting sound
                    player.currentLocation.playSound("cut", tile);

                    // Show a small visual effect above the harvested tile
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(
                        13,
                        new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize),
                        Color.Brown,
                        10,
                        Game1.random.NextDouble() < 0.5,
                        70f,
                        0,
                        Game1.tileSize,
                        (float)((tileY * (double)Game1.tileSize + Game1.tileSize / 2) / 10000.0 - 0.01))
                    {
                        delayBeforeAnimationStart = num * 10 // stagger animations for each tile
                    });

                    num++; // Increment harvested tile count
                }
            }

            return null; // Spell does not return a specific success effect
        }
    }
}
