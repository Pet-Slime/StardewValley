using WizardrySkill.Core.Framework.Spells;

namespace WizardrySkill.Core.Framework
{
    public class PreparedSpell
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The learned spell ID.</summary>
        public string SpellId { get; set; }

        /// <summary>The spell level.</summary>
        public int Level { get; set; }


        /*********
        ** Public methods
        *********/
        public PreparedSpell() { }

        public PreparedSpell(string spellId, int level)
        {
            this.SpellId = spellId;
            this.Level = level;
        }

        public PreparedSpell(Spell spell, int level)
        {
            this.SpellId = spell.ParentSchoolId + ":" + spell.Id;
            this.Level = level;
        }
    }
}
