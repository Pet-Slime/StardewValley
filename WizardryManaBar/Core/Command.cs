
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;

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
