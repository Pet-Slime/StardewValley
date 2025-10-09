using MagicSkillCode.Framework.Schools;
using SpaceCore;
using StardewValley;
using MagicSkillCode.Core;

namespace MagicSkillCode.Framework.Spells
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
            player.position.X = EvacSpell.EnterX;
            player.position.Y = EvacSpell.EnterY;
            Utilities.AddEXP(player, 5);
            return null;
        }

        internal static void OnLocationChanged()
        {
            EvacSpell.EnterX = Game1.player.position.X;
            EvacSpell.EnterY = Game1.player.position.Y;
        }
    }
}
