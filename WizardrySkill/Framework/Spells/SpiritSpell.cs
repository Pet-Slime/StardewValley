using WizardrySkill.Framework.Schools;
using WizardrySkill.Framework.Spells.Effects;
using SpaceCore;
using StardewValley;
using WizardrySkill.Core;

namespace WizardrySkill.Framework.Spells
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

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            Utilities.AddEXP(player, 25);
            return new SpiritEffect(player);
        }
    }
}
