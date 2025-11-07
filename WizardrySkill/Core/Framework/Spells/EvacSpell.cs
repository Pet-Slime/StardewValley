using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines an "EvacSpell" that teleports the player back to their last saved location
    public class EvacSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private static float EnterX; // X-coordinate of last saved location
        private static float EnterY; // Y-coordinate of last saved location

        /*********
        ** Public methods
        *********/

        public EvacSpell()
            : base(SchoolId.Life, "evac")
        {
            // SchoolId.Life identifies the spell's magical school
            // "evac" is the internal name for this spell
        }

        public override int GetMaxCastingLevel()
        {
            return 1; // Max spell level is 1
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25; // Mana cost for casting
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // Teleport the player to the last saved position
            player.position.X = EnterX;
            player.position.Y = EnterY;

            // Spell success: plays "stairsdown" sound and grants 5 XP
            return new SpellSuccess(player, "stairsdown", 5);
        }

        /*********
        ** Internal helpers
        *********/
        // This should be called when the player changes location to record their entry point
        internal static void OnLocationChanged()
        {
            EnterX = Game1.player.position.X; // Save current X position
            EnterY = Game1.player.position.Y; // Save current Y position
        }
    }
}
