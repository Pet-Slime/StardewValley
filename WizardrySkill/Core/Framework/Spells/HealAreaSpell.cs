using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "HealAreaSpell" that heals all nearby friendly characters
    public class HealAreaSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public HealAreaSpell()
            : base(SchoolId.Life, "healarea")
        {
            // SchoolId.Life identifies the spell's magical school
            // "healarea" is the internal name for this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost is 25% of the player's maximum mana
            return (player.GetMaxMana() / 4);
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can always cast if base spell conditions are met
            return base.CanCast(player, level);
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Play healing sound at the player's position
            player.currentLocation.playSound("healSound", player.Tile);

            // Apply the area healing effect
            return new HealAreaEffect(player, level);
        }
    }
}
