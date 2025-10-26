using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using static StardewValley.Minigames.TargetGame;
using SObject = StardewValley.Object;

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
            int num = 0;

            GameLocation loc = player.currentLocation;
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);
            //get a list of the tiles affected
            List<Vector2> list = Utilities.TilesAffected(target, level, player);
            //for each tile in the list, do the spell's function
            foreach (Vector2 tile in list)
            {
                // skip if out of mana
                if (!this.CanContinueCast(player, level))
                    return null;

                // skip if blocked
                if (loc.terrainFeatures.ContainsKey(tile))
                    continue;

                int targetTileX = (int)tile.X;
                int targetTileY = (int)tile.X;

                // handle artifact spot, else skip if blocked
                if (loc.objects.TryGetValue(tile, out SObject obj))
                {
                    if (obj.ParentSheetIndex == 590)
                    {
                        loc.digUpArtifactSpot(targetTileX, targetTileY, player);
                        loc.objects.Remove(tile);
                        if (num != 0)
                        {
                            player.AddMana(this.GetManaCost(player, level) * -1);
                        }
                        num++;
                        Utilities.AddEXP(player, 2);
                        continue;
                    }
                    else
                        continue;
                }

                // till dirt
                if (loc.doesTileHaveProperty(targetTileX, targetTileY, "Diggable", "Back") != null && !loc.IsTileOccupiedBy(tile))
                {
                    loc.makeHoeDirt(tile);
                    loc.playSound("hoeHit", tile);
                    //Game1.removeSquareDebrisFromTile(tileX, tileY);
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(targetTileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f));
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(targetTileX * (float)Game1.tileSize, targetTileY * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, Vector2.Distance(tile, target) * 30f));
                    loc.checkForBuriedItem(targetTileX, targetTileY, false, false, player);
                    if (num != 0)
                    {
                        player.AddMana(this.GetManaCost(player, level) * -1);
                    }
                    num++;
                    Utilities.AddEXP(player, 2);
                }
            }

            return null;
        }
    }
}
