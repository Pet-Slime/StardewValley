using System.Collections.Generic;

namespace WizardrySkill.Core.Framework
{
    /// <summary>A hotbar of prepared spells.</summary>
    public class PreparedSpellBar
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The prepared spells on the bar.</summary>
        public List<PreparedSpell> Spells { get; set; } = new();


        /*********
        ** Public methods
        *********/
        /// <summary>Get the spell in the given slot, if any.</summary>
        /// <param name="index">The slot index.</param>
        public PreparedSpell GetSlot(int index)
        {
            return index < this.Spells.Count
                ? this.Spells[index]
                : null;
        }

        /// <summary>
        /// Sets or clears a spell slot. Automatically expands or trims the list as needed.
        /// </summary>
        /// <param name="index">The slot index.</param>
        /// <param name="spell">The spell to assign, or <c>null</c> to clear.</param>
        public void SetSlot(int index, PreparedSpell spell)
        {
            // ensure list is large enough
            while (this.Spells.Count <= index)
                this.Spells.Add(null);

            // set or clear slot
            this.Spells[index] = spell;

            // trim trailing nulls to keep list compact
            for (int i = this.Spells.Count - 1; i >= 0; i--)
            {
                if (this.Spells[i] != null)
                    break;

                this.Spells.RemoveAt(i);
            }
        }
    }
}
