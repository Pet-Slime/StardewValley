using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Dimensions;

namespace WizardrySkill.Core.Framework.Spells
{
    public class TillSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: assigns spell school and spell ID
        public TillSpell()
            : base(SchoolId.Toil, "till") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        // The mana cost of casting the spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 1;
        }

        // What happens when the spell is cast by the local player
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should run the tilling logic.
            // Remote machines observe the cast packet but do not replay terrain, object, buried item, EXP, or stats mutation.
            if (!player.IsLocalPlayer)
                return null;

            // Create a dummy hoe tool to simulate hoe actions.
            Tool dummyHoe = new Hoe();
            dummyHoe.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(dummyHoe, "lastUser").SetValue(player);

            level += 1; // Increase level for spell radius
            int actionCount = 0; // Tracks how many tiles were affected
            int manaCost = this.GetManaCost(player, level);

            GameLocation loc = player.currentLocation;

            // Convert pixel coordinates to tile coordinates.
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            Vector2 target = new(tileX, tileY);

            // Get a list of all tiles affected by the spell.
            List<Vector2> affectedTiles = Utilities.TilesAffected(target, level, player);

            // Loop over each affected tile.
            foreach (Vector2 tile in affectedTiles)
            {
                if (!this.CanContinueCast(player, level))
                    break;

                bool didAction = false;

                // Handle terrain features, like grass or bushes.
                if (loc.terrainFeatures.TryGetValue(tile, out var terrainFeature))
                {
                    if (terrainFeature.performToolAction(dummyHoe, 0, tile))
                    {
                        loc.terrainFeatures.Remove(tile);
                        didAction = true;
                    }
                }
                else if (loc.objects.TryGetValue(tile, out var obj) && obj.performToolAction(dummyHoe))
                {
                    // Handle objects that react to hoe use.
                    if (obj.Type == "Crafting" && obj.Fragility != 2)
                        loc.debris.Add(new Debris(obj.QualifiedItemId, player.GetToolLocation(), Utility.PointToVector2(player.StandingPixel)));

                    obj.performRemoveAction();
                    loc.Objects.Remove(tile);
                    didAction = true;
                }

                // Handle creating hoe dirt if the tile can be dug.
                if (loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null)
                {
                    bool madeHoeDirt = false;

                    // Mine tilling.
                    if (loc is MineShaft && !loc.IsTileOccupiedBy(tile, CollisionMask.All, CollisionMask.None, useFarmerTile: true))
                    {
                        madeHoeDirt = loc.makeHoeDirt(tile);
                        if (madeHoeDirt)
                        {
                            loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, player);
                            this.BroadcastTillingSprites(loc, tile, target, affectedTiles.Count);
                        }
                    }
                    // Normal outdoors tilling.
                    else if (loc.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport))
                    {
                        madeHoeDirt = loc.makeHoeDirt(tile);
                        if (madeHoeDirt)
                        {
                            this.BroadcastTillingSprites(loc, tile, target, affectedTiles.Count);
                            loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, player);
                        }
                    }

                    didAction |= madeHoeDirt;
                }

                if (!didAction)
                    continue;

                // Reduce mana after the first tile.
                if (actionCount != 0)
                    player.AddMana(-manaCost);

                actionCount++;
                Utilities.AddEXP(player, 2);
                loc.playSound("hoeHit", tile);
                Game1.stats.DirtHoed++;
            }

            // If no tiles were affected, the spell fizzles.
            return actionCount == 0
                ? new SpellFizzle(player, manaCost)
                : null;
        }


        /*********
        ** Private methods
        *********/

        /// <summary>Broadcast tilling visuals through Stardew's native temporary sprite sync.</summary>
        /// <param name="location">The location where the tile was tilled.</param>
        /// <param name="tile">The tile that was affected.</param>
        /// <param name="target">The spell target tile.</param>
        /// <param name="affectedTileCount">The number of tiles in the affected area.</param>
        private void BroadcastTillingSprites(GameLocation location, Vector2 tile, Vector2 target, int affectedTileCount)
        {
            Vector2 pixelPosition = new(tile.X * 64f, tile.Y * 64f);

            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(12, pixelPosition, Color.White, 8, Game1.random.NextBool(), 50f));

            if (affectedTileCount > 2)
                Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(6, pixelPosition, Color.White, 8, Game1.random.NextBool(), Vector2.Distance(target, tile) * 30f));
        }
    }
}
