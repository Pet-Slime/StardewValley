using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class SpiritSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public SpiritSpell()
            : base(SchoolId.Eldritch, "spirit") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 50;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.health > (player.maxHealth / 5);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            if (!player.IsLocalPlayer)
                return null;

            player.currentLocation.playSound("ghost", player.Tile);
            player.takeDamage((player.maxHealth / 5), false, null);
            Utilities.AddEXP(player, 50);
            return new SpiritEffect(player);
        }
    }
}
