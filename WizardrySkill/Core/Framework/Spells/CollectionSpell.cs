using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using Log = MoonShared.Attributes.Log;

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
            if (player == null)
            {
                Log.Trace("[CollectionSpell] Cast aborted: player is null.");
                return null;
            }

            if (player.currentLocation == null)
            {
                Log.Trace($"[CollectionSpell] Cast aborted: player '{player.Name}' has no current location.");
                return null;
            }

            // Only the caster's own machine should run machine collection.
            // Remote machines observe the cast packet but do not replay harvest, inventory, item, or EXP mutation.
            if (!player.IsLocalPlayer)
            {
                Log.Trace($"[CollectionSpell] Ignoring non-local cast for player '{player.Name}' ({player.UniqueMultiplayerID}).");
                return null;
            }

            int effectiveRange = (level + 1) * 3; // Spell range scales with level.
            int collectedCount = 0; // Tracks how many machines were collected.
            int manaCost = this.GetManaCost(player, level);
            const int expPerCollect = 5;
            const string collectSound = "coin";

            int emptyTiles = 0;
            int notBigCraftable = 0;
            int notReady = 0;
            int noHeldObject = 0;
            int inventoryFull = 0;
            int checkedMachines = 0;

            GameLocation location = player.currentLocation;
            int tileSize = Game1.tileSize;

            // Determine the target tile for the spell.
            Vector2 target = new(targetX / tileSize, targetY / tileSize);

            // Get all tiles affected by the spell.
            List<Vector2> tiles = Utilities.TilesAffected(target, effectiveRange, player);

            Log.Trace($"[CollectionSpell] Cast started. Player='{player.Name}' ({player.UniqueMultiplayerID}), Location='{location.NameOrUniqueName}', Level={level}, Range={effectiveRange}, TargetPixel=({targetX},{targetY}), TargetTile={target}, TilesChecked={tiles.Count}, ManaCost={manaCost}, CurrentMana={player.GetCurrentMana()}.");

            // Loop through each affected tile.
            foreach (Vector2 tile in tiles)
            {
                // Stop if the player runs out of mana.
                if (!this.CanContinueCast(player, level))
                {
                    Log.Trace($"[CollectionSpell] Stopped early: not enough mana to continue. Collected={collectedCount}, CurrentMana={player.GetCurrentMana()}, ManaCost={manaCost}, Tile={tile}.");
                    break;
                }

                // Check if there's a machine at this tile.
                if (!location.objects.TryGetValue(tile, out StardewValley.Object machine))
                {
                    emptyTiles++;
                    continue;
                }

                checkedMachines++;

                if (!machine.bigCraftable.Value)
                {
                    notBigCraftable++;
                    Log.Trace($"[CollectionSpell] Skipped object at {tile}: not a big craftable. QualifiedItemId='{machine.QualifiedItemId}', Name='{machine.Name}'.");
                    continue;
                }

                if (!machine.readyForHarvest.Value)
                {
                    notReady++;
                    Log.Trace($"[CollectionSpell] Skipped machine at {tile}: not ready for harvest. QualifiedItemId='{machine.QualifiedItemId}', Name='{machine.Name}', MinutesUntilReady={machine.MinutesUntilReady}.");
                    continue;
                }

                if (machine.heldObject.Value is null)
                {
                    noHeldObject++;
                    Log.Trace($"[CollectionSpell] Skipped machine at {tile}: readyForHarvest=true but heldObject is null. QualifiedItemId='{machine.QualifiedItemId}', Name='{machine.Name}'.");
                    continue;
                }

                StardewValley.Object heldObject = machine.heldObject.Value;
                string heldObjectId = heldObject.QualifiedItemId;
                string heldObjectName = heldObject.Name;
                int heldObjectStack = heldObject.Stack;
                int heldObjectQuality = heldObject.Quality;

                if (!player.couldInventoryAcceptThisItem(heldObject))
                {
                    inventoryFull++;
                    Log.Trace($"[CollectionSpell] Skipped machine at {tile}: inventory cannot accept held item. Machine='{machine.Name}' ({machine.QualifiedItemId}), Held='{heldObjectName}' ({heldObjectId}), Stack={heldObjectStack}, Quality={heldObjectQuality}.");
                    continue;
                }

                Log.Trace($"[CollectionSpell] Collecting machine at {tile}. Machine='{machine.Name}' ({machine.QualifiedItemId}), Held='{heldObjectName}' ({heldObjectId}), Stack={heldObjectStack}, Quality={heldObjectQuality}, CollectedSoFar={collectedCount}.");

                // Perform the machine's harvest action.
                bool actionResult = machine.checkForAction(player);

                Log.Trace($"[CollectionSpell] checkForAction result at {tile}: Result={actionResult}, HeldObjectAfter={(machine.heldObject.Value == null ? "null" : machine.heldObject.Value.QualifiedItemId)}, ReadyAfter={machine.readyForHarvest.Value}.");

                // Deduct extra mana for additional machines.
                if (collectedCount > 0)
                {
                    player.AddMana(-manaCost);
                    Log.Trace($"[CollectionSpell] Deducted extra mana for additional collection. Amount={manaCost}, CurrentMana={player.GetCurrentMana()}, CollectedSoFar={collectedCount}.");
                }

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

            Log.Trace($"[CollectionSpell] Cast finished. Player='{player.Name}', Location='{location.NameOrUniqueName}', Collected={collectedCount}, CheckedMachines={checkedMachines}, EmptyTiles={emptyTiles}, NotBigCraftable={notBigCraftable}, NotReady={notReady}, NoHeldObject={noHeldObject}, InventoryFull={inventoryFull}, CurrentMana={player.GetCurrentMana()}.");

            if (collectedCount == 0)
            {
                Log.Trace("[CollectionSpell] Result: fizzle because no machines were collected.");
                return new SpellFizzle(player, manaCost);
            }

            int exp = expPerCollect * collectedCount;
            Log.Trace($"[CollectionSpell] Result: success. EXP={exp}, Sound='coin'.");

            return new SpellSuccess(player, "coin", exp);
        }
    }
}
