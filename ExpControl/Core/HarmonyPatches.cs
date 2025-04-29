using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley;
using System.Globalization;

namespace ExpControl.Core
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.gainExperience))]
    class GainExperience_patch
    {
        [HarmonyLib.HarmonyPrefix]
        private static void Prefix(ref int which, ref int howMuch)
        {
            if (!Game1.player.IsLocalPlayer && Game1.IsServer)
            {
                Game1.player.queueMessage(17, Game1.player, which, howMuch);
                return;
            }
            decimal totalExp = 0;
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            string stored = "";
            decimal prev = 0;
            int newExp = 0;
            decimal storedExp = 0;

            switch (which)
            {
                case 0:
                    //Calculate the new total exp
                    totalExp = (decimal)(ModEntry.Config.FarmingEXP * howMuch);
                    //Try to get out any stored exp and parse it. Add it to the total exp.
                    if (player.modData.TryGetValue("moonslime.ExpControl.FarmingEXP", out stored) && decimal.TryParse(stored, out prev))
                        totalExp += prev;
                    //Get the new exp. Do this by converting total exp here into an int which will floor it.
                    newExp = Math.Max(0, (int)totalExp);
                    //Get the new exp to store by taking the total exp and subtracting the new exp. New exp should always be less than total exp. 
                    storedExp = totalExp - newExp;
                    //Set the modData to the new stored exp value
                    player.modData["moonslime.ExpControl.FarmingEXP"] = storedExp.ToString();
                    //Change the value of howMuch into new value we calculated. 
                    howMuch = newExp;
                    break;
                case 3:
                    //mining
                    //Calculate the new total exp
                    totalExp = (decimal)(ModEntry.Config.MiningEXP * howMuch);
                    //Try to get out any stored exp and parse it. Add it to the total exp.
                    if (player.modData.TryGetValue("moonslime.ExpControl.MiningEXP", out stored) && decimal.TryParse(stored, out prev))
                        totalExp += prev;
                    //Get the new exp. Do this by converting total exp here into an int which will floor it.
                    newExp = Math.Max(0, (int)totalExp);
                    //Get the new exp to store by taking the total exp and subtracting the new exp. New exp should always be less than total exp. 
                     storedExp = totalExp - newExp;
                    //Set the modData to the new stored exp value
                    player.modData["moonslime.ExpControl.MiningEXP"] = storedExp.ToString();
                    //Change the value of howMuch into new value we calculated. 
                    howMuch = newExp;
                    break;
                case 1:
                    //fishing
                    //Calculate the new total exp
                    totalExp = (decimal)(ModEntry.Config.FishingEXP * howMuch);
                    //Try to get out any stored exp and parse it. Add it to the total exp.
                    if (player.modData.TryGetValue("moonslime.ExpControl.FishingEXP", out stored) && decimal.TryParse(stored, out prev))
                        totalExp += prev;
                    //Get the new exp. Do this by converting total exp here into an int which will floor it.
                    newExp = Math.Max(0, (int)totalExp);
                    //Get the new exp to store by taking the total exp and subtracting the new exp. New exp should always be less than total exp. 
                    storedExp = totalExp - newExp;
                    //Set the modData to the new stored exp value
                    player.modData["moonslime.ExpControl.FishingEXP"] = storedExp.ToString();
                    //Change the value of howMuch into new value we calculated. 
                    howMuch = newExp;
                break;
                case 2:
                    //forage
                    //Calculate the new total exp
                    totalExp = (decimal)(ModEntry.Config.ForagingEXP * howMuch);
                    //Try to get out any stored exp and parse it. Add it to the total exp.
                    if (player.modData.TryGetValue("moonslime.ExpControl.ForagingEXP", out stored) && decimal.TryParse(stored, out prev))
                        totalExp += prev;
                    //Get the new exp. Do this by converting total exp here into an int which will floor it.
                    newExp = Math.Max(0, (int)totalExp);
                    //Get the new exp to store by taking the total exp and subtracting the new exp. New exp should always be less than total exp. 
                    storedExp = totalExp - newExp;
                    //Set the modData to the new stored exp value
                    player.modData["moonslime.ExpControl.ForagingEXP"] = storedExp.ToString();
                    //Change the value of howMuch into new value we calculated. 
                    howMuch = newExp;
                    break;
                case 4:
                    //combat
                    //Calculate the new total exp
                    totalExp = (decimal)(ModEntry.Config.CombatEXP * howMuch);
                    //Try to get out any stored exp and parse it. Add it to the total exp.
                    if (player.modData.TryGetValue("moonslime.ExpControl.CombatEXP", out stored) && decimal.TryParse(stored, out prev))
                        totalExp += prev;
                    //Get the new exp. Do this by converting total exp here into an int which will floor it.
                    newExp = Math.Max(0, (int)totalExp);
                    //Get the new exp to store by taking the total exp and subtracting the new exp. New exp should always be less than total exp. 
                    storedExp = totalExp - newExp;
                    //Set the modData to the new stored exp value
                    player.modData["moonslime.ExpControl.CombatEXP"] = storedExp.ToString();
                    //Change the value of howMuch into new value we calculated. 
                    howMuch = newExp;
                break;
            }
        }
    }
}
