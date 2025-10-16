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
            return 0;
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
                    if (player.GetCurrentMana() <= 3)
                        return null;

                    Vector2 tile = new Vector2(tileX, tileY);

                    #region Harvest Machines
                    loc.objects.TryGetValue(tile, out StardewValley.Object machine);

                    if (machine != null && machine.readyForHarvest.Value && machine.heldObject.Value != null)
                    {
                        machine.checkForAction(Game1.player);

                    }
                    #endregion

                    loc.temporarySprites.Add(new TemporaryAnimatedSprite(13, new Vector2(tileX * (float)Game1.tileSize, tileY * (float)Game1.tileSize), Color.Brown, 10, Game1.random.NextDouble() < 0.5, 70f, 0, Game1.tileSize, (float)((tileY * (double)Game1.tileSize + Game1.tileSize / 2) / 10000.0 - 0.00999999977648258))
                    {
                        delayBeforeAnimationStart = num * 10
                    });
                    num++;

                    player.AddMana(-4);
                    Utilities.AddEXP(player, 5);
                    loc.playSound("grunt", tile);
                }
            }
            return null;
        }
    }
}
