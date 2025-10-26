using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using WizardrySkill.Core.Framework.Schools;
using xTile.Tiles;

namespace WizardrySkill.Core.Framework.Spells
{
    public class WaterSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public WaterSpell()
            : base(SchoolId.Toil, "water") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 2;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            WateringCan water = new();
            water.IsEfficient = true;
            ModEntry.Instance.Helper.Reflection.GetField<Farmer>(water, "lastUser").SetValue(player);

            level += 1;
            int num = 0;

            GameLocation loc = player.currentLocation;
            int tileX = targetX / Game1.tileSize;
            int tileY = targetY / Game1.tileSize;
            var target = new Vector2(tileX, tileY);
            //get a list of the tiles affected
            List<Vector2> list = Utilities.TilesAffected(target, 3 * level, player);
            //for each tile in the list, do the spell's function
            foreach (Vector2 tile in list)
            {

                // skip if out of mana
                if (!this.CanContinueCast(player, level))
                    continue;

                bool didAction = false;

                if (loc.terrainFeatures.TryGetValue(tile, out var value))
                {
                    value.performToolAction(water, 0, tile);
                        didAction= true;
                }

                if (loc.objects.TryGetValue(tile, out var value2))
                {
                    value2.performToolAction(water);
                        didAction = true;
                }

                if (loc is VolcanoDungeon && loc.isWaterTile((int)tile.X, (int)tile.Y))
                {
                    loc.performToolAction(water, (int)tile.X, (int)tile.Y);
                    didAction = true;
                }

                BirbCore.Attributes.Log.Alert($"{didAction}");
                if (didAction)
                {
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(13, new Vector2(tile.X * 64f, tile.Y * 64f), Color.White, 10, Game1.random.NextBool(), 70f, 0, 64, (tile.Y * 64f + 32f) / 10000f - 0.01f)
                    {
                        delayBeforeAnimationStart =  num * 10
                    });
                    if (num != 0)
                    {
                        player.AddMana(-3);
                    }

                    num++;
                    Utilities.AddEXP(player, 2);
                    loc.playSound("wateringCan", tile);
                }
            }

            return null;
        }
    }
}
