using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework.Schools
{
    public class MotionSchool : School
    {
        /*********
        ** Public methods
        *********/
        public MotionSchool()
            : base(SchoolId.Motion) { }

        public override Spell[] GetSpellsTier1()
        {
            return new[] { SpellManager.Get("motion:evac"), SpellManager.Get("motion:haste") };
        }

        public override Spell[] GetSpellsTier2()
        {

            return new[] { SpellManager.Get("motion:teleport"), SpellManager.Get("motion:descend") };
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("motion:blink") };
        }
    }
}
