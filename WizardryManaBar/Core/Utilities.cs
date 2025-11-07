using System;
using MoonShared;
using StardewValley;

namespace WizardryManaBar.Core
{
    internal class Utilities
    {
        /*********
        ** Fields
        *********/
        /// <summary>The prefix added to mod data keys.</summary>
        private const string Prefix = "moonSlime.ManaBar";

        /// <summary>The data key for the player's current mana points.</summary>
        private const string CurrentManaKey = Prefix + "/CurrentMana";

        /// <summary>The data key for the player's max mana points.</summary>
        private const string MaxManaKey = Prefix + "/MaxMana";

        /*********
        ** Public methods
        *********/
        /// <summary>Get a player's current mana points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetCurrentMana(Farmer player)
        {
            return player.modData.GetInt(CurrentManaKey, min: 0);
        }

        /// <summary>Set a player's current mana points.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="mana">The value to set.</param>
        public static void SetCurrentMana(Farmer player, int mana)
        {
            player.modData.SetInt(CurrentManaKey, mana, min: 0);
        }

        /// <summary>Get a player's max mana points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetMaxMana(Farmer player)
        {
            return player.modData.GetInt(MaxManaKey, min: 0);
        }

        /// <summary>Set a player's max mana points.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="mana">The value to set.</param>
        public static void SetMaxMana(Farmer player, int mana)
        {
            player.modData.SetInt(MaxManaKey, mana, min: 0);
        }



    }

    
    /// <summary>Provides extensions on <see cref="Farmer"/> for managing mana points.</summary>
    public static class Extensions
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the player's current mana points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetCurrentMana(this Farmer player)
        {
            return Utilities.GetCurrentMana(player);
        }

        /// <summary>Add points to the player's mana pool.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="amt">The number of mana points to add.</param>
        public static void AddMana(this Farmer player, int amt)
        {
            int mana = player.GetCurrentMana() + amt;
            int amount = Math.Clamp(mana, 0, player.GetMaxMana());
            Utilities.SetCurrentMana(player, amount);
            
        }

        /// <summary>Get the player's max mana points.</summary>
        /// <param name="player">The player to check.</param>
        public static int GetMaxMana(this Farmer player)
        {
            return Utilities.GetMaxMana(player);
        }

        /// <summary>Set the player's max mana points.</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="newCap">The value to set.</param>
        public static void SetMaxMana(this Farmer player, int newCap)
        {
            Utilities.SetMaxMana(player, Math.Max(0, newCap));
        }

        /// <summary>Set's the player's mana to their current max.</summary>
        /// <param name="player">The player to check.</param>
        public static void SetManaToMax(this Farmer player)
        {
            Utilities.SetCurrentMana(player, player.GetMaxMana());
        }

        /// <summary>Check to see if the player's current mana is equal to or  greater than a value</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="valueToCheckAgainst">The value to check.</param>
        public static bool IsCurrentManaGreaterThanValue(this Farmer player, int valueToCheckAgainst)
        {
            return Utilities.GetCurrentMana(player) >= valueToCheckAgainst;
        }


        /// <summary>Check to see if the player's current mana is equal to or  greater than a value</summary>
        /// <param name="player">The player to check.</param>
        /// <param name="valueToCheckAgainst">The value to check.</param>
        public static bool IsCurrentManaLessThanValue(this Farmer player, int valueToCheckAgainst)
        {
            return Utilities.GetCurrentMana(player) <= valueToCheckAgainst;
        }


    }
}
