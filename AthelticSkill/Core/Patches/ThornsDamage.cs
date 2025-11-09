using System;
using AthleticSkill.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;
using StardewValley.Monsters;

namespace AthleticSkill.Core.Patches
{
    /// <summary>
    /// This patch modifies Farmer.takeDamage to implement the "Thorns" effect
    /// for players with the Linebacker profession (Athletic10a2). When the player
    /// takes damage, this reflects some damage back to the monster.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public class ThornsDamage
    {
        // Using a Postfix so we can apply reflected damage *after* the player takes damage
        [HarmonyPostfix]
        private static void PostFix(Farmer __instance, ref int damage, ref bool overrideParry, ref Monster damager)
        {
            // --- Check if the player has the Linebacker profession ---
            if (__instance.HasCustomProfession(Athletic_Skill.Athletic10a2))
            {
                // --- Validate the monster and damage ---
                // Only reflect damage if:
                // 1. There is a monster (damager != null)
                // 2. The monster is not invincible
                // 3. The damage was not parried (overrideParry == false)
                // 4. Damage is not zero
                if (damager != null && !damager.isInvincible() && !overrideParry && damage != 0)
                {
                    // --- Calculate effective defense ---
                    // Copied from vanilla Thorn's Ring logic
                    int playerDefense = __instance.buffs.Defense;

                    // Some additional defense from "Book_Defense" stat
                    if (__instance.stats.Get("Book_Defense") != 0)
                    {
                        playerDefense++;
                    }

                    // Reduce effective defense slightly with random factor if defense >= 50% of damage
                    if ((float)playerDefense >= (float)damage * 0.5f)
                    {
                        playerDefense -= (int)((float)playerDefense * (float)Game1.random.Next(3) / 10f);
                    }

                    // --- Determine monster bounding box for reflected damage ---
                    Rectangle monsterBoundingBox = damager.GetBoundingBox();

                    // Calculate vector away from player (used for knockback) - not stored, just called
                    _ = Utility.getAwayFromPlayerTrajectory(monsterBoundingBox, __instance) / 2f;

                    // --- Calculate reflected damage ---
                    int reflectedDamage = damage;                           // Original damage taken
                    int damageAfterDefense = Math.Max(1, damage - playerDefense); // Damage minus defense, minimum 1

                    // If damage after defense is small (<10), average it with original damage
                    if (damageAfterDefense < 10)
                    {
                        reflectedDamage = (int)Math.Ceiling((double)(reflectedDamage + damageAfterDefense) / 2.0);
                    }

                    // Scale reflected damage by player’s athletic level
                    // Each level adds +2% of reflected damage (0.02 multiplier per level)
                    reflectedDamage = Math.Max(1, (int)(reflectedDamage * (Utilities.GetLevel(__instance) * 0.02f)));

                    // --- Apply reflected damage to the monster ---
                    // Damage range: reflectedDamage to reflectedDamage + 1
                    // Not using bombs
                    __instance.currentLocation?.damageMonster(monsterBoundingBox, reflectedDamage, reflectedDamage + 1, isBomb: false, __instance);
                }
            }
        }
    }
}
