using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared;
using MoonShared.Command;
using StardewValley;
using StardewValley.Tools;

namespace RanchingToolUpgrades
{
    [CommandClass]
    public class Command
    {
        [CommandMethod("Add a pail to the player inventory")]
        public static void GivePail(int level = 0)
        {
            Game1.player.addItemToInventory(new UpgradeablePail(level));
        }

        [CommandMethod("Add shears to the player inventory")]
        public static void GiveShears(int level = 0)
        {
            Game1.player.addItemToInventory(new UpgradeableShears(level));
        }

        [CommandMethod("Remove a pail from the player inventory")]
        public static void RemovePail()
        {
            Item pail = Game1.player.getToolFromName("Pail");
            if (pail is not null)
            {
                Game1.player.removeItemFromInventory(pail);
            }
        }

        [CommandMethod("Remove shears from the player inventory")]
        public static void RemoveShears()
        {
            Item shears = Game1.player.getToolFromName("Shears");
            if (shears is not null)
            {
                Game1.player.removeItemFromInventory(shears);
            }
        }

        [CommandMethod("Add the original Milk Pail item to the player inventory")]
        public static void GiveOriginalPail()
        {
            Game1.player.addItemToInventory(new StardewValley.Tools.MilkPail());
        }

        [CommandMethod("Add the original Shears item to the player inventory")]
        public static void GiveOriginalShears()
        {
            Game1.player.addItemToInventory(new StardewValley.Tools.Shears());
        }

        [CommandMethod("Add a pan to the player inventory")]
        public static void GivePan(int level = 0)
        {
            Game1.player.addItemToInventory(new UpgradeablePan(level));
        }

        [CommandMethod("Remove a pan from the player inventory")]
        public static void RemovePan()
        {
            Item pan = Game1.player.getToolFromName("Pan");
            if (pan is not null)
            {
                Log.Info("Found pan to remove");
                Game1.player.removeItemFromInventory(pan);
            }
            else
            {
                Log.Info("Found no pan to remove");
            }
        }

        [CommandMethod("Add the original Copper Pan item to the player inventory")]
        public static void GiveOriginalPan()
        {
            Game1.player.addItemToInventory(new Pan());
        }
    }
}
