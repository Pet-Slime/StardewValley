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

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            // Make the player jump slightly as part of the spell animation
            player.jump();


            // Only execute for the local player (prevents duplicates in multiplayer)
            if (!player.IsLocalPlayer)
                return null;

            // Create the shockwave effect around the player
            return new Shockwave(player, level);
        }
    }
}
