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
            if (ModEntry.Config.VoidSchool)
            {
                return new[] { SpellManager.Get("motion:descend"), SpellManager.Get("motion:blink") };
            }
            else
            {
                return new[] { SpellManager.Get("motion:descend") };
            }
        }

        public override Spell[] GetSpellsTier3()
        {
            return new[] { SpellManager.Get("motion:teleport") };
        }
    }
}
