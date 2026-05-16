using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

        // The mana cost of casting the spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 1;
        }

        // What happens when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        // What happens when the spell is received through the spell sync system
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            // Only the host should mutate shared terrain/object/location state
            if (!Context.IsMainPlayer)
                return null;

            if (caster == null || caster.currentLocation == null)
                return null;

            // Create a dummy hoe tool to simulate hoe actions
            Tool dummyHoe = new Hoe();
            dummyHoe.IsEfficient = true; // Makes the tool work instantly
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(dummyHoe, "lastUser").SetValue(caster);

            level += 1; // Increase level for spell radius
            int actionCount = 0; // Tracks how many tiles were affected

            GameLocation loc = caster.currentLocation;

            // Convert pixel coordinates to tile coordinates
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);

            // Cache the mana cost in a variable for use later to reduce calls
            int manaCost = this.GetManaCost(caster, level);

            // Get a list of all tiles affected by the spell (radius = level)
            List<Vector2> list = Utilities.TilesAffected(target, level, caster);

            // Loop over each affected tile
            foreach (Vector2 tile in list)
            {
                if (!this.CanContinueCast(caster, level))
                    return null;

                // Handle terrain features (e.g., grass, bushes)
                if (loc.terrainFeatures.TryGetValue(tile, out var value))
                {
                    if (value.performToolAction(dummyHoe, 0, tile))
                    {
                        loc.terrainFeatures.Remove(tile); // Remove feature if tilleds

                        // Reduce mana after the first tile
                        if (actionCount != 0)
                            caster.AddMana(-manaCost);

                        actionCount++; // Count how many tiles were affected
                        Utilities.AddEXP(caster, 2); // Give experience
                        loc.playSound("hoeHit", tile); // Sound effect
                        Game1.stats.DirtHoed++; // Update game stats
                    }

                    continue; // Skip to next tile
                }

                // Handle objects (e.g., stones, small logs)
                if (loc.objects.TryGetValue(tile, out var value2) && value2.performToolAction(dummyHoe))
                {
                    if (value2.Type == "Crafting" && value2.Fragility != 2)
                    {
                        // Drop debris if the object is breakable
                        loc.debris.Add(new Debris(value2.QualifiedItemId, caster.GetToolLocation(), Utility.PointToVector2(caster.StandingPixel)));
                    }

                    value2.performRemoveAction(); // Remove object
                    loc.Objects.Remove(tile);
                }

                // Skip tiles that cannot be dug
                if (loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") == null)
                    continue;

                // If in the mine and tile is free
                if (loc is MineShaft && !loc.IsTileOccupiedBy(tile, CollisionMask.All, CollisionMask.None, useFarmerTile: true))
                {
                    if (loc.makeHoeDirt(tile))
                    {
                        loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, caster);

                        // Visual effects for tilled dirt
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
                        if (list.Count > 2)
                        {
                            Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(target, tile) * 30f));
                        }
                    }
                }
                // Normal outdoors tiling
                else if (loc.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport) && loc.makeHoeDirt(tile))
                {
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
                    if (list.Count > 2)
                    {
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(target, tile) * 30f));
                    }

                    loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, caster);
                }

                // Reduce mana after the first tile
                if (actionCount != 0)
                    caster.AddMana(-manaCost);

                actionCount++; // Count how many tiles were affected
                Utilities.AddEXP(caster, 2); // Give experience
                loc.playSound("hoeHit", tile); // Sound effect
                Game1.stats.DirtHoed++; // Update game stats
            }

            // If no tiles were affected, the spell fizzles
            return actionCount == 0 && caster.IsLocalPlayer
                ? new SpellFizzle(caster, manaCost)
                : null;
        }
    }
}
