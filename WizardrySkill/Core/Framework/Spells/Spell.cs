using BirbCore.Attributes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public abstract class Spell
    {
        /*********
        ** Accessors
        *********/
        public string ParentSchoolId { get; }
        public School ParentSchool => School.GetSchool(this.ParentSchoolId);
        public string Id { get; }
        public string FullId => this.ParentSchoolId + ":" + this.Id;

        /// <summary>Whether the spell can be cast while a menu is open.</summary>
        public bool CanCastInMenus { get; protected set; }

        public Texture2D Icon { get; protected set; }
        public Texture2D[] SpellLevels { get; protected set; }


        /*********
        ** Public methods
        *********/
        public virtual int GetMaxCastingLevel()
        {
            return 3;
        }

        public abstract int GetManaCost(Farmer player, int level);

        public virtual bool CanCast(Farmer player, int level)
        {
            return
                Game1.player.GetSpellBook().KnowsSpell(this.FullId, level)
                && player.GetCurrentMana() >= this.GetManaCost(player, level);
        }

        public virtual bool CanContinueCast(Farmer player, int level)
        {
            return player.GetCurrentMana() >= this.GetManaCost(player, level);
        }

        /// <summary>Get the spell's translated name.</summary>
        public virtual string GetTranslatedName()
        {

            return ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{this.FullId}.name");
        }

        /// <summary>Get the spell's translated description.</summary>
        public virtual string GetTranslatedDescription()
        {
            string costBase = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.cost");
            string costNumbers = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{this.FullId}.cost");
            string effect = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.effect");
            string description = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{this.FullId}.desc");
            return $"{costBase}{costNumbers}\n{effect}{description}";
        }

        /// <summary>Get the spell's translated description.</summary>
        public virtual string GetTranslatedDescriptionForSpellMenu()
        {
            string costBase = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.cost");
            string costNumbers = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{this.FullId}.cost");
            string effect = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.effect");
            string description = ModEntry.Instance.I18N.Get($"moonslime.Wizardry.spell.{this.FullId}.desc");
            return $"{costBase}{costNumbers}\n\n{effect}{description}";
        }

        /// <summary>Get a translated tooltip to show for the spell.</summary>
        /// <param name="level">The spell level, if applicable.</param>
        public string GetTooltip(int? level = null)
        {
            string name = level != null && this.GetMaxCastingLevel() > 1
                ? I18n.Tooltip_Spell_NameAndLevel(spellName: this.GetTranslatedName(), level + 1)
                : this.GetTranslatedName();

            return string.Concat(name, "\n", this.GetTranslatedDescription());
        }

        public abstract IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY);

        public virtual void LoadIcon()
        {
            try
            {
                this.Icon = Content.LoadTexture("magic/" + this.ParentSchool.Id + "/" + this.Id + "/" + 1);
            }
            catch (ContentLoadException e)
            {
                Log.Warn("Failed to load icon for spell " + this.FullId + ": " + e);
            }
        }

        public virtual void LoadLevel()
        {
            try
            {
                this.SpellLevels = new Texture2D[this.GetMaxCastingLevel()];
                for (int i = 1; i <= this.GetMaxCastingLevel(); ++i)
                {
                    this.SpellLevels[i - 1] = Content.LoadTexture($"interface/level_{i-1}_spell");
                }
            }
            catch (ContentLoadException e)
            {
                Log.Warn("Failed to load icon for spell " + this.FullId + ": " + e);
            }
        }


        /*********
        ** Protected methods
        *********/
        protected Spell(string school, string id)
        {
            this.ParentSchoolId = school;
            this.Id = id;
        }

    }
}
