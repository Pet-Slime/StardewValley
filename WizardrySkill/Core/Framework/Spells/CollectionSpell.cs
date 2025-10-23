using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class CollectionSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public CollectionSpell()
            : base(SchoolId.Toil, "collect") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 3;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            level += 1 + level * 2;
            targetX /= Game1.tileSize;
            targetY /= Game1.tileSize;

            int num = 0;

            GameLocation loc = player.currentLocation;
            for (int tileX = targetX - level; tileX <= targetX + level; ++tileX)
            {
                for (int tileY = targetY - level; tileY <= targetY + level; ++tileY)
                {
                    // skip if out of mana
                    if (!this.CanContinueCast(player, level))
                        return null;

                    Vector2 tile = new Vector2(tileX, tileY);

                    #region Harvest Machines
                    loc.objects.TryGetValue(tile, out StardewValley.Object machine);

                    if (machine != null && machine.readyForHarvest.Value && machine.heldObject.Value != null)
                    {
                        machine.checkForAction(player);

                    }
                    #endregion

                    if (num != 0)
                    {
                        player.AddMana(-3);
                    }
                    Utilities.AddEXP(player, 5);
                    loc.playSound("grunt", tile);

                    Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite(13, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.Brown, 10, Game1.random.NextDouble() < 0.5, 70f, 0, Game1.tileSize, (float)((tileY * (double)Game1.tileSize + Game1.tileSize / 2) / 10000.0 - 0.00999999977648258))
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
