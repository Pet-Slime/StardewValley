using MoonShared;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "MeteorSpell" that allows the player to summon a meteor at a targeted location
    public class MeteorSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public MeteorSpell()
            : base(SchoolId.Elemental, "meteor")
        {
            // SchoolId.Elemental indicates this spell belongs to the Elemental school
            // "meteor" is the internal name for this spell
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player has at least 1 Iridium Ore in their inventory
            return base.CanCast(player, level) && player.Items.ContainsId(SObject.iridium.ToString(), 1);
        }

        public override int GetMaxCastingLevel()
        {
            // Only has one level of casting
            return 1;
        }

        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster.IsLocalPlayer && caster.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
            {
                // Consume 1 Iridium Ore from inventory
                caster.Items.ReduceId(SObject.iridium.ToString(), 1);
            }

            // Create a meteor effect at the targeted pixel position
            return new Meteor(caster, targetX, targetY);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }
    }
}
