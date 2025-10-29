using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

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
            if (!player.IsLocalPlayer)
                return null;
            player.ClearBuffs();
            return new SpellSuccess(player, "debuffSpell", 2);
        }
    }
}
