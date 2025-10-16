using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class EldritchSchool : School
    {
        /*********
        ** Public methods
        *********/
        public EldritchSchool()
            : base(SchoolId.Eldritch) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("eldritch:meteor"), SpellManager.Get("eldritch:bloodmana") };
        }

        public override Spell[] GetSpellsTier2()
        {
            return new[] { SpellManager.Get("eldritch:lucksteal"), SpellManager.Get("eldritch:charm") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("eldritch:spirit") };
        }
    }
}
