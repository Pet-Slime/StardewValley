using StardewValley;
using StardewValley.Locations;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "DescendSpell" that lets the player skip down multiple levels in the mine
    public class DescendSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public DescendSpell()
            : base(SchoolId.Motion, "descend")
        {
            // SchoolId.Elemental identifies the spell's magical school
            // "descend" is the internal name used to reference this spell
        }

        // Determines whether the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Can only cast if base conditions are met AND the player is in a MineShaft (not the Quarry)
            return base.CanCast(player, level) && player.currentLocation is MineShaft ms && ms.mineLevel != MineShaft.quarryMineShaft;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 15; // Mana cost for casting
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run this for the local player
            if (!player.IsLocalPlayer)
                return null;

            var ms = player.currentLocation as MineShaft;

            // If the player is not in a mine, the spell fizzles
            if (ms == null)
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Calculate target mine level to descend to
            int target = ms.mineLevel + 1 + level;

            // Prevent descending past level 120 to avoid going into the Skull Cavern accidentally
            if (ms.mineLevel <= 120 && target >= 120)
            {
                target = 120;
            }

            // Enter the target mine level
            Game1.enterMine(target);

            // Spell success: plays "stairsdown" sound and gives 5 XP
            return new SpellSuccess(player, "stairsdown", 5);
        }
    }
}
