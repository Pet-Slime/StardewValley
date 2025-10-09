using WizardrySkill.Framework.Spells;

namespace WizardrySkill.Framework.Schools
{
    public class ArcaneSchool : School
    {
        /*********
        ** Public methods
        *********/
        public ArcaneSchool()
            : base(SchoolId.Arcane) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("arcane:analyze"), SpellManager.Get("arcane:magicmissle") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("arcane:disenchant"), SpellManager.Get("arcane:enchant") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("arcane:rewind") };
        }
    }
}
