using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizardrySkill.Core;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class CleanseSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CleanseSpell()
            : base(SchoolId.Life, "cleanse") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }



        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            player.ClearBuffs();
            player.currentLocation.playSound("debuffSpell", player.Tile);
            Utilities.AddEXP(player, 2);
            return null;
        }
    }
}
