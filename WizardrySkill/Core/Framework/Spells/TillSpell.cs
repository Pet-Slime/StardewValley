using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;
using WizardrySkill.Core.Framework.Spells.Effects;
using static StardewValley.Minigames.TargetGame;
using SObject = StardewValley.Object;
using xTile.Dimensions;

namespace WizardrySkill.Core.Framework.Spells
{
    public class TillSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public TillSpell()
            : base(SchoolId.Toil, "till") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 2;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            // create fake tools
            Tool dummyHoe = new Hoe();
            dummyHoe.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(dummyHoe, "lastUser").SetValue(player);

            level += 1;
            int actionCount = 0;

            GameLocation loc = player.currentLocation;
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);
            //get a list of the tiles affected
            List<Vector2> list = Utilities.TilesAffected(target, level, player);
            //for each tile in the list, do the spell's function
            foreach (Vector2 tile in list)
            {
                if (loc.terrainFeatures.TryGetValue(tile, out var value))
                {
                    if (value.performToolAction(dummyHoe, 0, tile))
                    {
                        loc.terrainFeatures.Remove(tile);
                    }

                    continue;
                }

                if (loc.objects.TryGetValue(tile, out var value2) && value2.performToolAction(dummyHoe))
                {
                    if (value2.Type == "Crafting" && value2.Fragility != 2)
                    {
                        loc.debris.Add(new Debris(value2.QualifiedItemId, player.GetToolLocation(), Utility.PointToVector2(player.StandingPixel)));
                    }

                    value2.performRemoveAction();
                    loc.Objects.Remove(tile);
                }

                if (loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") == null)
                {
                    continue;
                }

                if (loc is MineShaft && !loc.IsTileOccupiedBy(tile, CollisionMask.All, CollisionMask.None, useFarmerTile: true))
                {
                    if (loc.makeHoeDirt(tile))
                    {

                        loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, player);
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
                        if (list.Count > 2)
                        {
                            Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(target, tile) * 30f));
                        }
                    }
                }
                else if (loc.isTilePassable(new Location((int)tile.X, (int)tile.Y), Game1.viewport) && loc.makeHoeDirt(tile))
                {

                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), 50f));
                    if (list.Count > 2)
                    {
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 8, Game1.random.NextBool(), Vector2.Distance(target, tile) * 30f));
                    }

                    loc.checkForBuriedItem((int)tile.X, (int)tile.Y, explosion: false, detectOnly: false, player);
                }

                actionCount++;
                Utilities.AddEXP(player, 3);
                loc.playSound("hoeHit", tile);
                Game1.stats.DirtHoed++;
            }


            return actionCount == 0
                ? new SpellFizzle(player, this.GetManaCost(player, level))
                : null;
        }
    }
}
