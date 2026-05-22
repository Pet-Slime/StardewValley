using System.Collections.Generic;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
        }

        // Returns the item cost for casting this spell.
        public override IDictionary<string, int> GetItemCost(Farmer player, int level)
        {
            return new Dictionary<string, int>
            {
                ["386"] = 1
            };
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if the player has at least 1 Iridium Ore in their inventory
            return base.CanCast(player, level);
        }

        public override int GetMaxCastingLevel()
        {
            // Only has one level of casting
            return 1;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;


            // Consume the item unless this cast came from a scroll.
            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Create the caster-owned meteor effect.
            // The falling meteor visual is broadcast through Stardew's native TemporaryAnimatedSprite sync.
            // Only the caster-owned active effect handles impact damage, debris, EXP, and explosion mutation.
            return new Meteor(player, targetX, targetY);
        }
    }
}
