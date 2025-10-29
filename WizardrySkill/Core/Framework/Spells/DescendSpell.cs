using StardewValley;
using StardewValley.Locations;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

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
            if (!player.IsLocalPlayer)
                return null;

            var ms = player.currentLocation as MineShaft;
            if (ms == null)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            int target = ms.mineLevel + 1 + level;
            if (ms.mineLevel <= 120 && target >= 120)
            {
                // We don't want the player to go through the bottom of the
                // original mine and into the skull cavern.
                target = 120;
            }

            Game1.enterMine(target);
            return new SpellSuccess(player, "stairsdown", 5);
        }
    }
}
