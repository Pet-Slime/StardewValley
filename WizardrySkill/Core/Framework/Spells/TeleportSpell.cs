using System.Linq;
using WizardrySkill.Core.Framework.Game.Interface;
using StardewValley;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class TeleportSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public TeleportSpell()
            : base(SchoolId.Elemental, "teleport") { }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && player.currentLocation.IsOutdoors && player.mount == null && player.Items.ContainsId("moonslime.Wizardry.Travel_Core");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player.IsLocalPlayer)
                Game1.activeClickableMenu = new TeleportMenu(player);

            return null;
        }
    }
}
