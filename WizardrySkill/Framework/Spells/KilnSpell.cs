using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizardrySkill.Core;
using WizardrySkill.Framework.Schools;
using StardewValley;

namespace WizardrySkill.Framework.Spells
{
    public class KilnSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public KilnSpell()
            : base(SchoolId.Elemental, "kiln") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
        }

        public override bool CanCast(Farmer player, int level)
        {
            //Player needs wood or drift wood in the inventory
            int woodAmount = 9 - (level + 1) * 2;
            return base.CanCast(player, level) && (
                player.Items.ContainsId("388", woodAmount) ||
                player.Items.ContainsId("169", woodAmount) ||
                player.Items.ContainsId("709", woodAmount)
                );
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            int woodAmount = 9 - (level + 1) * 2;
            if (player.Items.ContainsId("169", woodAmount))
            {
                player.Items.ReduceId("169", woodAmount);
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                player.currentLocation.playSound("furnace", player.Tile);
                Utilities.AddEXP(player, 2 * (level + 1));
                return null;

            }

            if (player.Items.ContainsId("388", woodAmount))
            {
                player.Items.ReduceId("388", woodAmount);
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                player.currentLocation.playSound("furnace", player.Tile);
                Utilities.AddEXP(player, 2 * (level + 1));
                return null;

            }

            if (player.Items.ContainsId("709", woodAmount))
            {
                player.Items.ReduceId("709", woodAmount);
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                player.currentLocation.playSound("furnace", player.Tile);
                Utilities.AddEXP(player, 2 * (level + 1));
                return null;

            }
            return null;
        }
    }
}
