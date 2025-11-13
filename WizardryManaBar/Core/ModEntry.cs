using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace WizardryManaBar.Core
{
    /// <summary>
    /// The main entry point for the Wizardry Mana Bar mod.
    /// Handles initialization, event registration, and API setup.
    /// </summary>
    [SMod]
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;
        internal static bool ArsVeneficiLoaded => Instance.Helper.ModRegistry.IsLoaded("HeyImAmethyst.ArsVenefici");
        internal static bool MagicStardewLoaded => Instance.Helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C");
        public ITranslationHelper I18N => Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // Initialize MoonShared attribute system
            Parser.InitEvents(helper);
            Parser.ParseAll(this);

            helper.Events.GameLoop.GameLaunched += Events.GameLaunched;

            // Register Trigger Actions
            RegisterTrigger("AddMana", AddMana);
            RegisterTrigger("SetMaxMana", SetMaxMana);
            RegisterTrigger("AddToMaxMana", AddToMaxMana);
            RegisterTrigger("SetManaToMax", SetManaToMax);

            // Register GameStateQueries
            RegisterManaQueries();
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
        /// Registers a trigger action under the ManaBar API namespace.
        /// </summary>
        /// <param name="name">The unique action name (without namespace prefix).</param>
        /// <param name="handler">The delegate to execute when triggered.</param>
        private static void RegisterTrigger(string name, TriggerActionDelegate handler)
        {
            TriggerActionManager.RegisterAction($"moonslime.ManaBarAPI.{name}", handler);
        }

        /// <summary>
        /// Registers all supported GameStateQueries for checking player mana conditions.
        /// </summary>
        private static void RegisterManaQueries()
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

            // PLAYER_CURRENT_MANA_GREATER_THAN_VALUE
            GameStateQuery.Register("PLAYER_CURRENT_MANA_GREATER_THAN_VALUE", (query, ctx) =>
            {
                if (!TryParseString(query[1], out string player, out string playerError, "player"))
                    return GameStateQuery.Helpers.ErrorResult(query, playerError);

                if (!TryParseInt(query[2], out int value, out string valueError, "value"))
                    return GameStateQuery.Helpers.ErrorResult(query, valueError);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.IsCurrentManaGreaterThanValue(value));
            });

            // PLAYER_CURRENT_MANA_LESS_THAN_VALUE
            GameStateQuery.Register("PLAYER_CURRENT_MANA_LESS_THAN_VALUE", (query, ctx) =>
            {
                if (!TryParseString(query[1], out string player, out string playerError, "player"))
                    return GameStateQuery.Helpers.ErrorResult(query, playerError);

                if (!TryParseInt(query[2], out int value, out string valueError, "value"))
                    return GameStateQuery.Helpers.ErrorResult(query, valueError);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.IsCurrentManaLessThanValue(value));
            });

            // PLAYER_MANA
            GameStateQuery.Register("PLAYER_MANA", (query, ctx) =>
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
                        int mana = target.GetCurrentMana();
                        return mana >= min && mana <= max;
                    });
            });

            // PLAYER_MAX_MANA
            GameStateQuery.Register("PLAYER_MAX_MANA", (query, ctx) =>
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
                        int mana = target.GetMaxMana();
                        return mana >= min && mana <= max;
                    });
            });
        }

        // Trigger Actions

        /// <summary>
        /// Adds a specified number of mana points to the current player.
        /// </summary>
        private static bool AddMana(string[] args, TriggerActionContext context, out string error) =>
            TryDoManaAction(args, 1, Game1.player.AddMana, out error);

        /// <summary>
        /// Sets the player's maximum mana to a specific value.
        /// </summary>
        private static bool SetMaxMana(string[] args, TriggerActionContext context, out string error) =>
            TryDoManaAction(args, 1, Game1.player.SetMaxMana, out error);

        /// <summary>
        /// Increases the player's maximum mana by the given number of points.
        /// </summary>
        private static bool AddToMaxMana(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
                return false;

            Game1.player.SetMaxMana(Game1.player.GetMaxMana() + points);
            return true;
        }

        /// <summary>
        /// Restores the player's mana to their maximum capacity.
        /// </summary>
        private static bool SetManaToMax(string[] args, TriggerActionContext context, out string error)
        {
            error = null;
            Game1.player.SetManaToMax();
            return true;
        }

        /// <summary>
        /// Shared helper for parsing integer arguments and applying a mana-related action.
        /// </summary>
        private static bool TryDoManaAction(string[] args, int index, Action<int> action, out string error)
        {
            if (!ArgUtility.TryGetInt(args, index, out int points, out error, "int points"))
                return false;

            action(points);
            return true;
        }
    }
}
