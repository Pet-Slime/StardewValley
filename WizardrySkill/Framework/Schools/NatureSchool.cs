using WizardrySkill.Framework.Spells;

namespace WizardrySkill.Framework.Schools
{
    public class NatureSchool : School
    {
        /*********
        ** Public methods
        *********/
        public NatureSchool()
            : base(SchoolId.Nature) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("nature:lantern"), SpellManager.Get("nature:tendrils") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("nature:shockwave"), SpellManager.Get("nature:magnetic_force") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("nature:photosynthesis") };
        }
    }
}
