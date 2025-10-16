using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class ToilSchool : School
    {
        /*********
        ** Public methods
        *********/
        public ToilSchool()
            : base(SchoolId.Toil) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("toil:cleardebris"), SpellManager.Get("toil:till") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("toil:water"), SpellManager.Get("toil:harvest") };
        }

        public override Spell[] GetSpellsTier3()
        {
            if (ModEntry.Config.VoidSchool)
            {
                return new[] { SpellManager.Get("toil:collect"), SpellManager.Get("toil:blink") };
            }
            else
            {
                return new[] { SpellManager.Get("toil:collect") };
            }
        }
    }
}
