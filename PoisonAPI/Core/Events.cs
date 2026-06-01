using System;
using MoonShared.Attributes;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PoisonBarAPI.Core
{
    [SEvent]
    internal class Events
    {
        private const string PoisonFill = "moonslime.PoisonBarApi.PoisonFill";
        private const string PoisonRestore = "moonslime.PoisonBarApi.PoisonRestore";

        [SEvent.GameLaunchedLate]
        public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            SpaceEvents.OnItemEaten += OnItemEaten;
        }

        [SEvent.DayStarted]
        public static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player is null)
                return;

            // Poison is treated as a temporary condition, so clear it at the start of each day.
            Game1.player.SetPoison(0);
        }

        [SEvent.TimeChanged]
        public static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player is null)
                return;

            CheckPoisonFaint(Game1.player);
        }

        private static void OnItemEaten(object sender, EventArgs args)
        {
            if (sender is not Farmer player)
                return;

            var item = player.itemToEat;
            if (item is null)
            {
                Log.Warn("OnItemEaten called but no item was found.");
                return;
            }

            foreach (string tag in item.GetContextTags())
            {
                bool isFill = tag.StartsWith(PoisonFill);
                bool isRestore = !isFill && tag.StartsWith(PoisonRestore);

                if (!isFill && !isRestore)
                    continue;

                int separatorIndex = tag.IndexOf('/');
                if (separatorIndex <= 0 || separatorIndex >= tag.Length - 1)
                    continue;

                ReadOnlySpan<char> valueSpan = tag.AsSpan(separatorIndex + 1);

                if (isFill && int.TryParse(valueSpan, out int poisonValue))
                {
                    int qualityAdjustment = item.Quality;
                    poisonValue = (int)Math.Floor(poisonValue * (1 + qualityAdjustment * 0.4));
                    player.AddPoison(poisonValue);
                }
                else if (isRestore && float.TryParse(valueSpan, out float poisonPercent))
                {
                    int restoreAmount = (int)Math.Ceiling(player.maxHealth * (poisonPercent / 100f));
                    player.AddPoison(-restoreAmount);
                }
            }
        }

        private static void CheckPoisonFaint(Farmer player)
        {
            int poison = player.GetCurrentPoison();

            if (poison <= 0)
                return;

            if (poison <= player.health)
                return;

            KillPlayerFromPoison(player);
        }

        private static void KillPlayerFromPoison(Farmer player)
        {
            // Clear poison first so the player doesn't instantly retrigger the poison faint after the death/faint handling starts.
            player.SetPoison(0);

            // Use takeDamage so Stardew handles the health loss/faint flow more naturally than just setting health to 0.
            int lethalDamage = Math.Max(999, player.health + player.maxHealth);
            player.takeDamage(lethalDamage, overrideParry: true, damager: null);
        }
    }
}
