using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using HarmonyLib;
using SpaceCore;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace AthleticSkill.Core.Patches
{

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public class AvoidDamage
    {
        //Patch farmer take damage
        //This is a prefix since we can set the farmer invincible if they pass the check right before they take the damage, negating the damage.
        [HarmonyPrefix]
        private static void Prefix(Farmer __instance)
        {
            //Check to see if they have the acrobat profession
            if (__instance.HasCustomProfession(Athletic_Skill.Athletic10b1))
            {
                //Get the dodge chance. The formula is atheltics level + luck level
                double dodgeChance = Utilities.GetLevel(__instance) * 0.01 + __instance.LuckLevel * 0.005;
                //Get the dice roll they have to roll againce
                double diceRoll = Game1.random.NextDouble();
                //See if they win the dice roll
                bool didTheyWin = (diceRoll < dodgeChance);
                //If they won, set invincibility.
                if (didTheyWin)
                {
                    __instance.temporarilyInvincible = true;
                    __instance.flashDuringThisTemporaryInvincibility = true;
                    __instance.temporaryInvincibilityTimer = 0;
                    __instance.currentTemporaryInvincibilityDuration = 1200 + __instance.GetEffectsOfRingMultiplier("861") * 400;
                    //Play a sound to indicate they just dodged an attack
                    __instance.playNearbySoundAll("coldSpell");
                }
            }
        }
    }
}
