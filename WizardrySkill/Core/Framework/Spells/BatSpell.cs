using MoonShared;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // The Batspell allows the player to summon a bat that helps tracks various things based on level
    public class BatSpell : Spell
    {
        /*********
        ** Public methods
        *********/


        public BatSpell()
            : base(SchoolId.Nature, "bat") { }

        // Defines how much mana it costs to cast this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
        }

        // Defines the maximum level at which this spell can be cast
        public override int GetMaxCastingLevel()
        {
            return 2;
        }

        // Checks if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Player must meet the base spell requirements (mana, cooldown, etc.)
            return base.CanCast(player, level) && player.Items.ContainsId("767", 1);
        }

        // Called when the spell is actually cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Play a ghost sound at the player's current tile
            player.currentLocation.playSound("batScreech", player.Tile);

            if (player.modData.GetBool("moonslime.Wizardry.scrollspell") == false)
            {
                player.Items.ReduceId("767", 1);
            }

            // Give player some experience points for casting
            Utilities.AddEXP(player, 10);
            if (level == 0)
            {
                // Spawn the visual/functional effect of the spirit spell
                return new BatArtifactEffect(player, 100);
            } else
            {
                // Spawn the visual/functional effect of the spirit spell
                return new BatArtifactEffect(player, 100);
            }
        }
    }
}
