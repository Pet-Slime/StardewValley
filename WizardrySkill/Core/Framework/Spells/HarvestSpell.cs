using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class HarvestSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HarvestSpell()
            : base(SchoolId.Toil, "harvest") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            level = 5;
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            int num = 0;

            GameLocation loc = player.currentLocation;
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    if (player.GetCurrentMana() <= 3)
                        return null;

                    Vector2 tile = new Vector2(tileX, tileY);

                    if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) || feature is not Grass)
                        continue;

                    loc.terrainFeatures.Remove(tile);
                    // collect hay
                    Random random = Game1.IsMultiplayer
                    ? Game1.recentMultiplayerRandom
                        : new Random((int)(Game1.uniqueIDForThisGame + tile.X * 1000.0 + tile.Y * 11.0));
                    if (random.NextDouble() < 0.5)
                    {
                        if (Game1.getFarm().tryToAddHay(1) == 0) // returns number left
                            Game1.addHUDMessage(new HUDMessage("Hay", HUDMessage.achievement_type, true));
                    }

                    loc.temporarySprites.Add(new TemporaryAnimatedSprite(13, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.Brown, 10, Game1.random.NextDouble() < 0.5, 70f, 0, Game1.tileSize, (float)((tileY * (double)Game1.tileSize + Game1.tileSize / 2) / 10000.0 - 0.00999999977648258))
                    {
                        delayBeforeAnimationStart = num * 10
                    });
                    num++;

                    player.AddMana(-4);
                    Utilities.AddEXP(player, 2 * (level + 1));
                    player.currentLocation.playSound("cut", tile);
                }
            }
            return null;
        }
    }
}
