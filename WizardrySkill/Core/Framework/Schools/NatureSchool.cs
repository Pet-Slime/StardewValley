using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
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
            return new[] { SpellManager.Get("nature:lantern"), SpellManager.Get("nature:fish_frenzy") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("nature:tendrils") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("nature:photosynthesis"), SpellManager.Get("nature:crabrain") };
        }
    }
}
