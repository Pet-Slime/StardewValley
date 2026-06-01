using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared;
using StardewValley;

namespace PoisonBarAPI.Core
{
    internal class Utilities
    {
        /*********
        ** Fields
        *********/
        /// <summary>The prefix added to mod data keys.</summary>
        private const string Prefix = "moonSlime.PoisonBar";

        /// <summary>The data key for the player's current Poison points.</summary>
        private const string CurrentPoisonKey = Prefix + "/CurrentPoison";


        /*********
        ** Public methods
        *********/
        /// <summary>Get a player's current Poison points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetCurrentPoison(Farmer player)
        {
            return player.modData.GetInt(CurrentPoisonKey, min: 0);
        }

        /// <summary>Set a player's current Poison points.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="Poison">The value to set.</param>
        public static void SetCurrentPoison(Farmer player, int Poison)
        {
            player.modData.SetInt(CurrentPoisonKey, Poison, min: 0);
        }



    }


    /// <summary>Provides extensions on <see cref="Farmer"/> for Poisonging Poison points.</summary>
    public static class Extensions
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the player's current Poison points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetCurrentPoison(this Farmer player)
        {
            return Utilities.GetCurrentPoison(player);
        }

        /// <summary>Add points to the player's Poison pool.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="amt">The number of Poison points to add.</param>
        public static void AddPoison(this Farmer player, int amt)
        {
            int Poison = player.GetCurrentPoison() + amt;
            int amount = Math.Clamp(Poison, 0, player.maxHealth);
            Utilities.SetCurrentPoison(player, amount);

        }

        /// <summary>Sets the player's Poison to # value.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="amt">The number of Poison points to go to.</param>
        public static void SetPoison(this Farmer player, int amt)
        {
            int amount = Math.Clamp(amt, 0, player.maxHealth);
            Utilities.SetCurrentPoison(player, amount);
        }

        /// <summary>Check to see if the player's current Poison is equal to or  greater than a value</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="valueToCheckAgainst">The value to check.</param>
        public static bool IsCurrentPoisonGreaterThanValue(this Farmer player, int valueToCheckAgainst)
        {
            return Utilities.GetCurrentPoison(player) >= valueToCheckAgainst;
        }


        /// <summary>Check to see if the player's current Poison is equal to or  greater than a value</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="valueToCheckAgainst">The value to check.</param>
        public static bool IsCurrentPoisonLessThanValue(this Farmer player, int valueToCheckAgainst)
        {
            return Utilities.GetCurrentPoison(player) <= valueToCheckAgainst;
        }


    }
}
