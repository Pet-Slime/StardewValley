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
            level += 1;
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            int num = 0;

            GameLocation loc = player.currentLocation;

            WateringCan water = new();

            List<Vector2> list = new List<Vector2>();

            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {

                    Vector2 tile = new Vector2(tileX, tileY);
                    list.Add(tile);

                }
            }
            foreach (Vector2 item in list)
            {

                // skip if out of mana
                if (!this.CanContinueCast(player, level))
                    continue;

                bool didAction = false;

                if (loc.terrainFeatures.TryGetValue(item, out var value))
                {
                    value.performToolAction(water, 0, item);
                        didAction= true;
                }

                if (loc.objects.TryGetValue(item, out var value2))
                {
                    value2.performToolAction(water);
                        didAction = true;
                }

                if (loc is VolcanoDungeon && loc.isWaterTile((int)item.X, (int)item.Y))
                {
                    loc.performToolAction(water, (int)item.X, (int)item.Y);
                    didAction = true;
                }

                BirbCore.Attributes.Log.Alert($"{didAction}");
                if (didAction)
                {
                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(13, new Vector2(item.X * 64f, item.Y * 64f), Color.White, 10, Game1.random.NextBool(), 70f, 0, 64, (item.Y * 64f + 32f) / 10000f - 0.01f)
                    {
                        delayBeforeAnimationStart =  num * 10
                    });
                    if (num != 0)
                    {
                        player.AddMana(-3);
                    }
                    Utilities.AddEXP(player, 1);
                    loc.playSound("wateringCan", item);
                }
                
                num++;
            }

            return null;
        }
    }
}
