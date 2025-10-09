using WizardrySkill.Framework.Spells;

namespace WizardrySkill.Framework.Schools
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
            return new[] { SpellManager.Get("toil:collect") };
        }
    }
}
