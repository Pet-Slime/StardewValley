using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace PoisonBarAPI.Core
{
    /// <summary>
    /// The main entry point for the Wizardry Poison Bar mod.
    /// Handles initialization, event registration, and API setup.
    /// </summary>
    [SMod]
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        public ITranslationHelper I18N => Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // Initialize MoonShared attribute system
            Parser.InitEvents(helper);
            Parser.ParseAll(this);


            // Register Trigger Actions
            RegisterTrigger("AddPoison", AddPoison);

            // Register GameStateQueries
            RegisterPoisonQueries();
            this.GetApi();
        }

        /// <summary>
        /// Returns this mod's API instance, exposed for other mods to integrate with.
        /// </summary>
        public override object GetApi()
        {
            return new Api();
        }

        /// <summary>
        /// Registers a trigger action under the PoisonBar API namespace.
        /// </summary>
        /// <param name="name">The unique action name (without namespace prefix).</param>
        /// <param name="handler">The delegate to execute when triggered.</param>
        private static void RegisterTrigger(string name, TriggerActionDelegate handler)
        {
            TriggerActionManager.RegisterAction($"moonslime.PoisonBarAPI.{name}", handler);
        }

        /// <summary>
        /// Registers all supported GameStateQueries for checking player Poison conditions.
        /// </summary>
        private static void RegisterPoisonQueries()
        {
            // --- Helper for safe int parsing ---
            static bool TryParseString(string input, out string value, out string error, string paramName)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    error = $"{paramName} cannot be empty or missing.";
                    value = null;
                    return false;
                }

                value = input;
                error = null;
                return true;
            }


            static bool TryParseInt(string input, out int value, out string error, string paramName)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    error = $"{paramName} cannot be empty";
                    value = 0;
                    return false;
                }

                if (!int.TryParse(input, out value))
                {
                    error = $"{paramName} must be an integer";
                    return false;
                }

                error = null;
                return true;
            }

            // PLAYER_CURRENT_Poison_GREATER_THAN_VALUE
            GameStateQuery.Register("PLAYER_CURRENT_POISON_GREATER_THAN_VALUE", (query, ctx) =>
            {
                if (!TryParseString(query[1], out string player, out string playerError, "player"))
                    return GameStateQuery.Helpers.ErrorResult(query, playerError);

                if (!TryParseInt(query[2], out int value, out string valueError, "value"))
                    return GameStateQuery.Helpers.ErrorResult(query, valueError);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.IsCurrentPoisonGreaterThanValue(value));
            });

            // PLAYER_CURRENT_Poison_LESS_THAN_VALUE
            GameStateQuery.Register("PLAYER_CURRENT_POISON_LESS_THAN_VALUE", (query, ctx) =>
            {
                if (!TryParseString(query[1], out string player, out string playerError, "player"))
                    return GameStateQuery.Helpers.ErrorResult(query, playerError);

                if (!TryParseInt(query[2], out int value, out string valueError, "value"))
                    return GameStateQuery.Helpers.ErrorResult(query, valueError);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.IsCurrentPoisonLessThanValue(value));
            });

            // PLAYER_Poison
            GameStateQuery.Register("PLAYER_POISON", (query, ctx) =>
            {
                if (!TryParseString(query[1], out string player, out string playerError, "player"))
                    return GameStateQuery.Helpers.ErrorResult(query, playerError);

                if (!TryParseInt(query[2], out int min, out string minError, "minValue"))
                    return GameStateQuery.Helpers.ErrorResult(query, minError);

                int max = int.MaxValue;
                if (int.TryParse(query[3], out int parsedMax))
                    max = parsedMax;

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target =>
                    {
                        int Poison = target.GetCurrentPoison();
                        return Poison >= min && Poison <= max;
                    });
            });
        }

        // Trigger Actions

        /// <summary>
        /// Adds a specified number of Poison points to the current player.
        /// </summary>
        private static bool AddPoison(string[] args, TriggerActionContext context, out string error) =>
            TryDoPoisonAction(args, 1, Game1.player.AddPoison, out error);

        /// <summary>
        /// Shared helper for parsing integer arguments and applying a Poison-related action.
        /// </summary>
        private static bool TryDoPoisonAction(string[] args, int index, Action<int> action, out string error)
        {
            if (!ArgUtility.TryGetInt(args, index, out int points, out error, "int points"))
                return false;

            action(points);
            return true;
        }
    }
}
