using System.Collections.Generic;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This spell generates a shockwave effect around the player.
    // The player must be standing on the ground (not mid-jump) to cast it.
    public class ShockwaveSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school and ID
        public ShockwaveSpell()
            : base(SchoolId.Elemental, "shockwave") { }

        public override SpellSyncMode SyncMode => SpellSyncMode.NetworkedEffect;

        // Determines if the spell can currently be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Can cast only if:
            // 1. Base spell conditions are met (enough mana, cooldown, etc.)
            // 2. Player is not already jumping (yJumpVelocity == 0)
            return base.CanCast(player, level) && player.yJumpVelocity == 0;
        }

        // Returns the mana cost for casting this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 10; // Fixed mana cost
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Make the caster jump slightly as part of the spell animation.
            if (player.yJumpVelocity == 0)
                player.jump();

            // The caster-owned effect handles landing timing, broadcast visuals, monster damage, and EXP.
            return new Shockwave(player, level);
        }

        // Called when another machine observes this spell cast.
        public override IActiveEffect OnRemoteCast(Farmer caster, int level, int targetX, int targetY, IDictionary<string, string> data)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Remote machines may show the caster's jump animation, but must not create the shockwave effect.
            // The ring visuals are broadcast by the caster-owned Shockwave effect through Stardew's sprite sync.
            if (caster.yJumpVelocity == 0)
                caster.jump();

            return null;
        }
    }
}
