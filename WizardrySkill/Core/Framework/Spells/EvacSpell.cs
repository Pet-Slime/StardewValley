using SpaceCore;
using StardewValley;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class EvacSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private static float EnterX;
        private static float EnterY;


        /*********
        ** Public methods
        *********/
        public EvacSpell()
            : base(SchoolId.Life, "evac") { }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            player.position.X = EnterX;
            player.position.Y = EnterY;
            player.currentLocation.playSound("stairsdown", player.Tile);
            Utilities.AddEXP(player, 5);
            return null;
        }

        internal static void OnLocationChanged()
        {
            EnterX = Game1.player.position.X;
            EnterY = Game1.player.position.Y;
        }
    }
}
