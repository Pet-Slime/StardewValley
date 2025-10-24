using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    public class TillWaterSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public TillWaterSpell()
            : base(SchoolId.Toil, "tillwater") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            level += 1;
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;
            Vector2 target = new Vector2(targetX, targetY);

            Tool dummyHoe = new Hoe();
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(dummyHoe, "lastUser").SetValue(player);

            GameLocation loc = player.currentLocation;
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {

                    // skip if out of mana
                    if (!this.CanContinueCast(player, level))
                        return null;


                    Vector2 tile = new Vector2(tileX, tileY);



                    // skip if blocked
                    if (loc.terrainFeatures.ContainsKey(tile))
                        continue;

                    // handle artifact spot, else skip if blocked
                    if (loc.objects.TryGetValue(tile, out SObject obj))
                    {
                        if (obj.ParentSheetIndex == 590)
                        {
                            loc.digUpArtifactSpot(tileX, tileY, player);
                            loc.objects.Remove(tile);
                            player.AddMana(-2);
                        }
                        else
                            continue;
                    }

                    // till dirt
                    if (loc.doesTileHaveProperty(tileX, tileY, "Diggable", "Back") != null && !loc.IsTileOccupiedBy(tile))
                    {
                        loc.makeHoeDirt(tile);
                        loc.playSound("hoeHit", tile);
                        //Game1.removeSquareDebrisFromTile(tileX, tileY);
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(12, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f));
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(6, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, Vector2.Distance(tile, target) * 30f));
                        loc.checkForBuriedItem(tileX, tileY, false, false, player);
                        player.AddMana(-3);
                        Utilities.AddEXP(player, 2);
                    }
                }
            }

            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            int num = 0;

            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    // skip if out of mana
                    if (!this.CanContinueCast(player, level))
                        return null;

                    Vector2 tile = new Vector2(tileX, tileY);

                    if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) || feature is not HoeDirt dirt)
                        continue;

                    if (dirt.state.Value != HoeDirt.dry)
                        continue;

                    dirt.state.Value = HoeDirt.watered;
                    if (num != 0)
                    {
                        player.AddMana(-3);
                    }
                    Utilities.AddEXP(player, 1);
                    loc.playSound("wateringCan", tile);
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(13, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.White, 10, Game1.random.NextDouble() < 0.5, 70f, 0, Game1.tileSize, (float)((tileY * (double)Game1.tileSize + Game1.tileSize / 2) / 10000.0 - 0.00999999977648258))
                    {
                        delayBeforeAnimationStart = num * 10
                    });
                    num++;

                }
            }

            return null;
        }
    }
}
