using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using MoonShared.Attributes;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    /// <summary>How a spell should be processed in multiplayer.</summary>
    public enum SpellSyncMode
    {
        /// <summary>
        /// The spell only matters on the caster's machine.
        /// No spell-cast observation packet is sent by the normal dispatch flow.
        /// Use this for self-only spells, menu/UI spells, inventory-only spells, or effects that have no useful remote visuals.
        /// </summary>
        LocalOnly,

        /// <summary>
        /// The caster's machine runs the real spell behavior and uses Stardew's normal systems to sync the results where possible.
        /// Other machines may receive an observation packet, but they should not replay gameplay mutation.
        /// Use this for spells where Stardew already syncs the important world changes, temporary sprites, sounds, debris, objects, or projectiles.
        /// </summary>
        LocalWorld,

        /// <summary>
        /// The caster's machine runs the real spell behavior, and remote machines may create custom Wizardry-only visuals or local active effects.
        /// Remote machines should not duplicate damage, EXP, costs, drops, buffs, inventory changes, or world mutation.
        /// Use this for effects that Stardew does not fully visualize/sync by itself, like custom tendrils, custom aura visuals, or remote-only presentation.
        /// </summary>
        NetworkedEffect,

        /// <summary>
        /// The caster's machine creates or updates durable summon state through SummonManager.
        /// SummonManager tracks owner ID, summon slot, summon def_id, duration, location, and custom data.
        /// Local visual instances are created from summon state instead of replaying the spell on every machine.
        /// Use this for companions/summons like lantern, spirit, bat_artifact, and bat_monster.
        /// </summary>
        Summon
    }

    public abstract class Spell
    {
        /*********
        ** Accessors
        *********/
        public string ParentSchoolId { get; }
        public School ParentSchool => School.GetSchool(this.ParentSchoolId);
        public string Id { get; }
        public string FullId => this.ParentSchoolId + ":" + this.Id;

        /// <summary>How this spell should be processed in multiplayer.</summary>
        public virtual SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

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

        /// <summary>Get the item costs needed to cast this spell.</summary>
        /// <param name="player">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        public virtual IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>();
        }

        /// <summary>Get whether the player has the item costs needed to cast this spell.</summary>
        /// <param name="player">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        public virtual bool HasItemCost(Farmer player, int level)
        {
            if (player == null)
                return false;

            IDictionary<string, int> itemCost = this.GetItemCost(player, level);
            if (itemCost == null || itemCost.Count == 0)
                return true;

            foreach (var pair in itemCost)
            {
                string itemId = pair.Key;
                int amount = pair.Value;

                if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                    continue;

                if (!player.Items.ContainsId(itemId, amount))
                    return false;
            }

            return true;
        }

        /// <summary>Consume the item costs needed to cast this spell.</summary>
        /// <param name="player">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        public virtual bool ConsumeItemCost(Farmer player, int level)
        {
            if (player == null)
                return false;

            if (!this.HasItemCost(player, level))
                return false;

            // Scroll casts should not consume reagent items.
            if (!this.ShouldConsumeItemCost(player))
                return true;

            IDictionary<string, int> itemCost = this.GetItemCost(player, level);
            if (itemCost == null || itemCost.Count == 0)
                return true;

            foreach (var pair in itemCost)
            {
                string itemId = pair.Key;
                int amount = pair.Value;

                if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                    continue;

                player.Items.ReduceId(itemId, amount);
            }

            return true;
        }

        public virtual bool CanCast(Farmer player, int level)
        {
            return player != null
                && player.GetSpellBook().KnowsSpell(this.FullId, level)
                && player.GetCurrentMana() >= this.GetManaCost(player, level)
                && this.HasItemCost(player, level);
        }

        public virtual bool CanContinueCast(Farmer player, int level)
        {
            return player != null && player.GetCurrentMana() >= this.GetManaCost(player, level);
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

        /// <summary>Get the spell's translated description for the spell menu.</summary>
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

        /// <summary>Build spell-specific packet data when the local player initiates a cast.</summary>
        /// <param name="caster">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="targetX">The target X position in pixels.</param>
        /// <param name="targetY">The target Y position in pixels.</param>
        public virtual Dictionary<string, string> BuildPacketData(Farmer caster, int level, int targetX, int targetY)
        {
            return new Dictionary<string, string>();
        }

        /// <summary>Run the real local spell behavior on the caster's own machine.</summary>
        /// <param name="player">The player casting the spell.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="targetX">The target X position in pixels.</param>
        /// <param name="targetY">The target Y position in pixels.</param>
        public abstract IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY);

        /// <summary>Handle an observed remote spell cast.</summary>
        /// <remarks>
        /// Remote default behavior intentionally does nothing.
        /// A spell must opt into remote visuals or local-player-only reactions by overriding this method.
        /// </remarks>
        /// <param name="caster">The player who originally cast the spell.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="targetX">The target X position in pixels.</param>
        /// <param name="targetY">The target Y position in pixels.</param>
        /// <param name="data">Spell-specific packet data.</param>
        public virtual IActiveEffect OnRemoteCast(Farmer caster, int level, int targetX, int targetY, IDictionary<string, string> data)
        {
            return null;
        }

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
                    this.SpellLevels[i - 1] = Content.LoadTexture($"interface/level_{i - 1}_spell");
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

        /// <summary>Get whether this cast should consume item costs.</summary>
        /// <param name="player">The player casting the spell.</param>
        protected virtual bool ShouldConsumeItemCost(Farmer player)
        {
            return player?.modData.GetBool("moonslime.Wizardry.scrollspell") != true;
        }
    }
}
