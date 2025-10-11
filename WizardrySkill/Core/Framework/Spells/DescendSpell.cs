using SpaceCore;
using StardewValley;
using StardewValley.Locations;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class DescendSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public DescendSpell()
            : base(SchoolId.Elemental, "descend") { }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.currentLocation is MineShaft ms && ms.mineLevel != MineShaft.quarryMineShaft;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 15;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            var ms = player.currentLocation as MineShaft;
            if (ms == null)
                return null;

            int target = ms.mineLevel + 1 + 2 * level;
            if (ms.mineLevel <= 120 && target >= 120)
            {
                // We don't want the player to go through the bottom of the
                // original mine and into the skull cavern.
                target = 120;
            }

            Game1.enterMine(target);
            player.currentLocation.playSound("stairsdown", player.Tile);
            Utilities.AddEXP(player, 5);
            return null;
        }
    }
}
