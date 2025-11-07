using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class LifeSchool : School
    {
        /*********
        ** Public methods
        *********/
        public LifeSchool()
            : base(SchoolId.Life) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("life:cleanse"), SpellManager.Get("life:healarea") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("life:heal"), SpellManager.Get("life:magnetic_force") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("life:buff") };
        }
    }
}
