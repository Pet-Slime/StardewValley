using System.Collections.Generic;
using BirbCore.Attributes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoonShared.Command;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.GarbageCans;
using Log = BirbCore.Attributes.Log;

namespace WizardryManaBar.Core
{
    [SCommand("manaapi_addMana")]
    public class Command_playerAddMana
    {
        [SCommand.Command("Sets the player's Mana to the given amount")]
        public static void Set(int amount)
        {
            if (!Context.IsPlayerFree)
                return;
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            if (player != null || !player.IsLocalPlayer)
                return;
            player.AddMana(amount);
        }

    }

    [SCommand("manaapi_setMaxMana")]
    public class Command_playerSetMaxMana
    {
        [SCommand.Command("Sets the player's Max Mana to the given amount")]
        public static void Set(int amount)
        {
            if (!Context.IsPlayerFree)
                return;
            Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
            if (player != null || !player.IsLocalPlayer)
                return;

            player.SetMaxMana(amount);
        }

    }
}
