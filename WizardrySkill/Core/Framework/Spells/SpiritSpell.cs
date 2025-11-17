using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // The SpiritSpell allows the player to summon a spirit effect at the cost of some health.
    public class SpiritSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school (Eldritch) and its ID ("spirit")
        public SpiritSpell()
            : base(SchoolId.Eldritch, "spirit") { }

        // Defines how much mana it costs to cast this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 50; // Fixed mana cost
        }

        // Defines the maximum level at which this spell can be cast
        public override int GetMaxCastingLevel()
        {
            return 1; // Only one level for this spell
        }

        // Checks if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Player must meet the base spell requirements (mana, cooldown, etc.)
            // AND player must have more than 1/5 of their max health
            return base.CanCast(player, level) && player.health > (player.maxHealth / 5);
        }

        // Called when the spell is actually cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Play a ghost sound at the player's current tile
            player.currentLocation.playSound("ghost", player.Tile);

            // Cost to cast: remove 1/5 of the player's max health
            player.takeDamage((player.maxHealth / 5), false, null);

            // Give player some experience points for casting
            Utilities.AddEXP(player, 50);

            // Spawn the visual/functional effect of the spirit spell
            return new SpiritEffect(player, ModEntry.Config.Spirit_attack_range);
        }
    }
}
