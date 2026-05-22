using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

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

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

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
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should remove grass, add hay, spend mana, award EXP, and broadcast harvest visuals.
            // Remote machines observe the cast packet but do not replay terrain/farm mutation.
            if (!player.IsLocalPlayer)
                return null;

            int radius = 5; // Spell range is fixed at 5 tiles around the target
            int actionCount = 0; // Tracks the number of tiles harvested
            int manaCost = this.GetManaCost(player, level);

            GameLocation location = player.currentLocation;

            // Convert pixel coordinates to tile coordinates
            int targetTileX = targetX / Game1.tileSize;
            int targetTileY = targetY / Game1.tileSize;

            // Loop through all tiles in a square around the target
            for (int tileX = targetTileX - radius; tileX <= targetTileX + radius; ++tileX)
            {
                for (int tileY = targetTileY - radius; tileY <= targetTileY + radius; ++tileY)
                {
                    // Stop if out of mana
                    if (!this.CanContinueCast(player, level))
                        return null;

                    Vector2 tile = new(tileX, tileY);

                    // Skip tiles that are not grass
                    if (!location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) || feature is not Grass)
                        continue;

                    // Remove the grass
                    location.terrainFeatures.Remove(tile);

                    // Attempt to add hay to the farm; show a HUD message if successful
                    if (Game1.getFarm().tryToAddHay(1) == 0)
                        Game1.addHUDMessage(new HUDMessage("Hay", HUDMessage.achievement_type, true));

                    // Deduct extra mana for additional tiles
                    if (actionCount != 0)
                        player.AddMana(-manaCost);

                    // Give experience points
                    Utilities.AddEXP(player, 2 * (level + 1));

                    // Play cutting sound
                    location.playSound("cut", tile);

                    // Show a small visual effect above the harvested tile
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(
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
                        delayBeforeAnimationStart = actionCount * 10
                    });

                    actionCount++;
                }
            }

            return actionCount == 0
                ? new SpellFizzle(player, this.GetManaCost(player, level))
                : null;
        }
    }
}
