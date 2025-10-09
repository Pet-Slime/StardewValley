using System.Linq;
using WizardrySkill.Framework.Game.Interface;
using WizardrySkill.Framework.Schools;
using StardewValley;
using WizardrySkill.Core;

namespace WizardrySkill.Framework.Spells
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
                Game1.activeClickableMenu = new TeleportMenu();

            return null;
        }
    }
}
