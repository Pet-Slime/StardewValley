using AthleticSkill.Objects;
using HarmonyLib;
using SpaceCore;
using StardewValley;

namespace AthleticSkill.Core.Patches
{
    /// <summary>
    /// This patch modifies Farmer.takeDamage to implement the "Avoid Damage" effect
    /// for players with the Acrobat profession (Athletic10b1). When the player
    /// takes damage, they have a chance to completely dodge the attack.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.takeDamage))]
    public class AvoidDamage
    {
        // Using a Prefix so we can set the player temporarily invincible
        // before the damage is applied, effectively negating it.
        [HarmonyPrefix]
        private static void Prefix(Farmer __instance)
        {
            // --- Check if the player has the Acrobat profession ---
            if (__instance.HasCustomProfession(Athletic_Skill.Athletic10b1))
            {
                // --- Calculate dodge chance ---
                // Formula: 1% per athletic level + 0.5% per luck level
                double dodgeChance = Utilities.GetLevel(__instance) * 0.01 + __instance.LuckLevel * 0.005;

                // --- Roll the dice ---
                double diceRoll = Game1.random.NextDouble(); // Generates a value between 0.0 and 1.0

                // --- Determine if dodge succeeds ---
                bool didTheyWin = (diceRoll < dodgeChance);

                if (didTheyWin)
                {
                    // --- Apply temporary invincibility ---
                    __instance.temporarilyInvincible = true;                       // Prevents damage
                    __instance.flashDuringThisTemporaryInvincibility = true;        // Visual flash effect
                    __instance.temporaryInvincibilityTimer = 0;                     // Reset timer
                    __instance.currentTemporaryInvincibilityDuration = 1200         // Base duration (ms)
                        + __instance.GetEffectsOfRingMultiplier("861") * 400;     // Bonus from ring (if any)

                    // --- Play sound to indicate successful dodge ---
                    __instance.playNearbySoundAll("coldSpell");
                }
            }
        }
    }
}
