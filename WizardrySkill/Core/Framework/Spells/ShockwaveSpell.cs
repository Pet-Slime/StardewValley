using StardewModdingAPI;
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

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

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
            return this.OnReceiveCast(player, level, targetX, targetY, "");
        }

        // Called when the spell is received through the spell sync system
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            // Make the player jump slightly as part of the spell animation.
            // This is safe to run locally on every client so everyone sees the cast animation.
            if (caster.yJumpVelocity == 0)
                caster.jump();

            // Only the host should create the actual shockwave effect.
            // Shockwave damages monsters and grants EXP, so this prevents duplicate damage in multiplayer.
            if (!Context.IsMainPlayer)
                return null;

            // Create the shockwave effect around the player
            return new Shockwave(caster, level);
        }
    }
}
