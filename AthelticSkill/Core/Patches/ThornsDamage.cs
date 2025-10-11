using System;
using HarmonyLib;
using SpaceCore;
using StardewValley;
using StardewValley.Monsters;

namespace AthleticSkill.Core.Patches
{

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public class ThornsDamage
    {
        //Patch farmer take damage
        //This is a prefix since we can set the farmer invincible if they pass the check right before they take the damage, negating the damage.
        [HarmonyPostfix]
        private static void PostFix(Farmer __instance, ref int damage, ref bool overrideParry, ref Monster damager)
        {
            //Check to see if they have the acrobat profession
            if (__instance.HasCustomProfession(Athletic_Skill.Athletic10a2))
            {
                if (damager != null && !damager.isInvincible() && !overrideParry && damage != 0)
                {

                    //Copied from vanilla thorn's ring
                    int num2 = __instance.buffs.Defense;
                    if (__instance.stats.Get("Book_Defense") != 0)
                    {
                        num2++;
                    }

                    if ((float)num2 >= (float)damage * 0.5f)
                    {
                        num2 -= (int)((float)num2 * (float)Game1.random.Next(3) / 10f);
                    }

                    Microsoft.Xna.Framework.Rectangle boundingBox = damager.GetBoundingBox();
                    _ = Utility.getAwayFromPlayerTrajectory(boundingBox, __instance) / 2f;
                    int num3 = damage;
                    int num4 = Math.Max(1, damage - num2);
                    if (num4 < 10)
                    {
                        num3 = (int)Math.Ceiling((double)(num3 + num4) / 2.0);
                    }

                    num3 *= (int)Math.Floor((Utilities.GetLevel(__instance) * 0.02));

                    __instance.currentLocation?.damageMonster(boundingBox, num3, num3 + 1, isBomb: false, __instance);
                }
            }
        }
    }
}
