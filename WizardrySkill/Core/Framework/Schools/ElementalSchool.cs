using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class ElementalSchool : School
    {
        /*********
        ** Public methods
        *********/
        public ElementalSchool()
            : base(SchoolId.Elemental) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("elemental:magicmissle"), SpellManager.Get("elemental:frostbolt") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("elemental:fireball"), SpellManager.Get("elemental:shockwave") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("elemental:meteor") };
        }
    }
}
