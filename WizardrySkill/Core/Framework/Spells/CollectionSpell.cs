using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class CollectionSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public CollectionSpell()
            : base(SchoolId.Toil, "collect") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 3;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int effectiveRange = (level + 1) * 3;
            int collectedCount = 0;
            const int baseManaCost = 3;
            const int expPerCollect = 5;
            const string collectSound = "coin";

            GameLocation location = player.currentLocation;
            int tileSize = Game1.tileSize;

            // Determine the target tile
            Vector2 target = new Vector2(targetX / tileSize, targetY / tileSize);

            // Get all affected tiles
            List<Vector2> tiles = Utilities.TilesAffected(target, effectiveRange, player);

            foreach (Vector2 tile in tiles)
            {
                // Stop if the player runs out of mana
                if (!this.CanContinueCast(player, level))
                    break;

                // Skip if no machine at this tile
                if (!location.objects.TryGetValue(tile, out StardewValley.Object machine) ||
                    machine is not { bigCraftable.Value: true, readyForHarvest.Value: true } ||
                    machine.heldObject.Value is null ||
                    !player.couldInventoryAcceptThisItem(machine.heldObject.Value))
                    continue;

                // Perform harvest
                machine.checkForAction(player);
                Utilities.AddEXP(player, expPerCollect);

                if (collectedCount > 0)
                    player.AddMana(-baseManaCost);

                // Visual and audio feedback
                location.playSound(collectSound, tile);

                var point = machine.TileLocation * tileSize;

                Game1.Multiplayer.broadcastSprites(player.currentLocation,
                    new TemporaryAnimatedSprite(10,
                    point,
                    Color.Cyan,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

                point.Y -= (int)(player.Sprite.SpriteHeight * 2);

                Game1.Multiplayer.broadcastSprites(player.currentLocation,
                    new TemporaryAnimatedSprite(10,
                    point,
                    Color.Cyan,
                    10,
                    Game1.random.NextDouble() < 0.5,
                    70f,
                    0,
                    Game1.tileSize,
                    100f));

                collectedCount++;
            }

            return collectedCount == 0
                ? new SpellFizzle(player, this.GetManaCost(player, level))
                : null;
        }
    }
}
