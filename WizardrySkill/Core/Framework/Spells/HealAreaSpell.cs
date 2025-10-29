using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class HealAreaSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public HealAreaSpell()
            : base(SchoolId.Life, "healarea") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return (player.GetMaxMana() / 4);
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            if (!player.IsLocalPlayer)
                return null;

            player.currentLocation.playSound("healSound", player.Tile);
            return new HealAreaEffect(player, level);
        }
    }
}
