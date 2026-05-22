using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "CollectionSpell" that automatically collects ready-for-harvest machines (like kegs, preserves jars, etc.)
    public class CollectionSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CollectionSpell()
            : base(SchoolId.Toil, "collect")
        {
            // SchoolId.Toil identifies the spell's magical school.
            // "collect" is the internal name used to reference this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return 3; // Base mana cost per machine collected.
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should run machine collection.
            // Remote machines observe the cast packet but do not replay harvest, inventory, item, or EXP mutation.
            if (!player.IsLocalPlayer)
                return null;

            int effectiveRange = (level + 1) * 3; // Spell range scales with level.
            int collectedCount = 0; // Tracks how many machines were collected.
            int manaCost = this.GetManaCost(player, level);
            const int expPerCollect = 5;
            const string collectSound = "coin";

            GameLocation location = player.currentLocation;
            int tileSize = Game1.tileSize;

            // Determine the target tile for the spell.
            Vector2 target = new(targetX / tileSize, targetY / tileSize);

            // Get all tiles affected by the spell.
            List<Vector2> tiles = Utilities.TilesAffected(target, effectiveRange, player);

            // Loop through each affected tile.
            foreach (Vector2 tile in tiles)
            {
                // Stop if the player runs out of mana.
                if (!this.CanContinueCast(player, level))
                    break;

                // Check if there's a machine at this tile that is big, ready for harvest, and contains an item.
                if (!location.objects.TryGetValue(tile, out StardewValley.Object machine) ||
                    machine is not { bigCraftable.Value: true, readyForHarvest.Value: true } ||
                    machine.heldObject.Value is null ||
                    !player.couldInventoryAcceptThisItem(machine.heldObject.Value))
                    continue;

                // Perform the machine's harvest action.
                machine.checkForAction(player);

                // Give the player experience points.
                Utilities.AddEXP(player, expPerCollect);

                // Deduct extra mana for additional machines.
                if (collectedCount > 0)
                    player.AddMana(-manaCost);

                // Visual and audio feedback for the collection.
                location.playSound(collectSound, tile);

                Vector2 point = machine.TileLocation * tileSize;

                // Show first layer of cyan particles above the machine.
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(
                        10,
                        point,
                        Color.Cyan,
                        10,
                        Game1.random.NextDouble() < 0.5,
                        70f,
                        0,
                        Game1.tileSize,
                        100f));

                // Show second layer of particles slightly higher.
                point.Y -= player.Sprite.SpriteHeight * 2;
                Game1.Multiplayer.broadcastSprites(location,
                    new TemporaryAnimatedSprite(
                        10,
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
                ? new SpellFizzle(player, manaCost)
                : null;
        }
    }
}
