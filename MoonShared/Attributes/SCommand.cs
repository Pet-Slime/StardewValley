#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewModdingAPI;
using StardewValley;

namespace MoonShared.Attributes;

/// <summary>
/// Defines a collection of SMAPI console commands for a mod.
/// 
/// Mods can use this by decorating a class with `[SCommand("commandname")]`,
/// and decorating its methods with `[SCommand.Command("description")]`.
/// Each method becomes a subcommand (like `commandname subcommand arg1 arg2`).
/// 
/// This system automatically registers those commands with SMAPI’s console
/// and handles argument parsing, help text, and type conversion.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SCommand(string name, string help = "") : ClassHandler(2)
{
    // Stores all subcommands: the key is the subcommand name, and the value is an Action that executes it.
    private readonly Dictionary<string, Action<string[]>> _commands = new();

    // Stores help text for each subcommand, used when printing help info in the console.
    private readonly Dictionary<string, string> _helps = new();

    /// <summary>
    /// Sets up this command group when the mod is loaded.
    /// Registers the main command name with SMAPI, and wires up subcommand handling.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        // Call the base handler — this finds all [Command] methods inside this class.
        // We pass the dictionaries (_commands, _helps) and the command name down so
        // subcommands can register themselves into these collections.
        base.Handle(type, instance, mod, [this._commands, this._helps, name]);

        // Register the main command with SMAPI’s console system.
        // Example: if "name" is "wizard", this adds "wizard" as a console command.
        mod.Helper.ConsoleCommands.Add(
            name: name,
            documentation: this.GetHelp(),   // Shown when player types "help wizard"
            callback: (s, commandArgs) => this.CallCommand(commandArgs) // Runs our command handler.
        );
    }

    /// <summary>
    /// Returns a formatted help message for this command or for a specific subcommand.
    /// </summary>
    private string GetHelp(string? subCommand = null)
    {
        // If a specific subcommand is requested, return only its help text.
        if (subCommand is not null)
        {
            return this._helps[subCommand];
        }

        // Otherwise, build a combined help overview of all subcommands.
        StringBuilder sb = new();
        sb.Append($"{name}: {help}\n");

        foreach (string sub in this._helps.Keys)
        {
            string helpText = this._helps[sub];
            sb.Append($"\t{helpText}\n\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Called when the player executes this command in the SMAPI console.
    /// Parses arguments, handles help, and runs the right subcommand if found.
    /// </summary>
    private void CallCommand(string[] args)
    {
        // If no arguments are given, just print general help text.
        if (args.Length == 0)
        {
            Log.Info(this.GetHelp());
            return;
        }

        // Handle built-in help flags like "help" or "-h".
        if (args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase)
            || args[0].Equals("-h", StringComparison.InvariantCultureIgnoreCase))
        {
            // If a subcommand is specified (e.g., "help teleport"), show that subcommand’s help.
            if (args.Length > 1 && this._helps.ContainsKey(args[1]))
            {
                Log.Info(this.GetHelp(args[1]));
            }
            else
            {
                // Otherwise, show general help for all subcommands.
                Log.Info(this.GetHelp());
            }

            return;
        }

        // Try to find and execute the requested subcommand.
        try
        {
            // args[0] is the subcommand name, args[1..] are parameters to it.
            this._commands[args[0]].Invoke(args[1..]);
        }
        catch (Exception e)
        {
            // If something fails (unknown command, bad args, etc.), show help and trace output.
            Log.Info(this.GetHelp(args[0]));
            Log.Trace($"Args are:{string.Join(" ", args)}");
            Log.Trace(e.ToString());
        }
    }

    /// <summary>
    /// Represents a single subcommand within the main SCommand group.
    /// Used on individual methods to register them as subcommands.
    /// 
    /// Example:
    /// <code>
    /// [SCommand("birb")]
    /// public class BirbCommand {
    ///     [SCommand.Command("Makes the birb sing")]
    ///     public void Sing(string songName) { ... }
    /// }
    /// </code>
    /// 
    /// This creates a console command: `birb sing songName`
    /// </summary>
    public class Command(string help = "") : MethodHandler
    {
        /// <summary>
        /// Called for each method decorated with [SCommand.Command].
        /// It registers the method as a callable subcommand in the command group.
        /// </summary>
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            // The class instance must exist — static-only command classes aren’t allowed.
            if (instance is null)
            {
                Log.Error("SCommand class may be static? Cannot parse subcommands.");
                return;
            }

            // The outer SCommand class must have passed the shared dictionaries and name.
            if (args is null)
            {
                Log.Error("SCommand class didn't pass args");
                return;
            }

            // Extract the shared command dictionary, help dictionary, and main command name.
            Dictionary<string, Action<string[]>> commands = (Dictionary<string, Action<string[]>>)args[0];
            Dictionary<string, string> helps = (Dictionary<string, string>)args[1];
            string command = (string)args[2];

            // Convert method name to snake_case to use as the subcommand name.
            // Example: "TeleportPlayer" becomes "teleport_player"
            string subCommand = method.Name.ToSnakeCase();

            // Add the subcommand to the dictionary, defining what happens when it’s run.
            commands.Add(subCommand, commandArgs =>
            {
                List<object> commandArgsList = [];

                // Convert each string argument from the console into the correct parameter type.
                for (int i = 0; i < method.GetParameters().Length; i++)
                {
                    ParameterInfo parameter = method.GetParameters()[i];
                    string? arg = commandArgs?.Length > i ? commandArgs[i] : null;

                    // Parse and add this argument to the list.
                    commandArgsList.Add(ParseArg(arg, parameter));

                    // If the parameter is marked with [params], consume all remaining arguments.
                    if (parameter.GetCustomAttribute(typeof(ParamArrayAttribute), false) is null)
                        continue;

                    for (int j = i + 1; j < (commandArgs?.Length ?? 0); j++)
                    {
                        commandArgsList.Add(ParseArg(commandArgs?[j], parameter));
                    }
                }

                // Invoke the method with the parsed arguments.
                method.Invoke(instance, commandArgsList.ToArray());
            });

            // === Build the help text for this subcommand ===
            StringBuilder help1 = new();
            help1.Append($"{command} {subCommand}");

            // Show the command’s parameters in <angle brackets> or [square brackets] if optional.
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                string dotDotDot = parameter.GetCustomAttribute<ParamArrayAttribute>() is not null ? "..." : "";

                if (parameter.IsOptional)
                {
                    help1.Append($" [{parameter.Name}{dotDotDot}]");
                }
                else
                {
                    help1.Append($" <{parameter.Name}{dotDotDot}>");
                }
            }

            help1.Append($"\n\t\t{help}");

            // Register this subcommand’s help text for use in console help output.
            helps.Add(subCommand, help1.ToString());
        }

        /// <summary>
        /// Converts a string argument from the console into the expected parameter type.
        /// Supports primitive types, in-game objects, and fuzzy search helpers from Stardew.
        /// </summary>
        private static object ParseArg(string? arg, ParameterInfo parameter)
        {
            // If argument missing and parameter is optional, return default (Type.Missing).
            if (arg == null)
            {
                return Type.Missing;
            }

            // === BASIC TYPES ===
            if (parameter.ParameterType == typeof(string))
                return arg;

            if (parameter.ParameterType == typeof(int))
                return int.Parse(arg);

            if (parameter.ParameterType == typeof(double) || parameter.ParameterType == typeof(float))
                return float.Parse(arg);

            if (parameter.ParameterType == typeof(bool))
                return bool.Parse(arg);

            // === STARDEW TYPES ===
            if (parameter.ParameterType == typeof(GameLocation))
                return Utility.fuzzyLocationSearch(arg); // Match by partial name (e.g. “farm”)

            if (parameter.ParameterType == typeof(NPC))
                return Utility.fuzzyCharacterSearch(arg); // Match by character name

            if (parameter.ParameterType == typeof(FarmAnimal))
                return Utility.fuzzyAnimalSearch(arg); // Match by animal name

            if (parameter.ParameterType == typeof(Farmer))
            {
                // Allow player ID number, or special keywords "host" and "player"
                if (long.TryParse(arg, out long playerId))
                    return Game1.GetPlayer(playerId);

                return arg.Equals("host", StringComparison.InvariantCultureIgnoreCase)
                    ? Game1.MasterPlayer
                    : Game1.player;
            }

            // For item lookup commands.
            if (parameter.ParameterType == typeof(Item))
                return Utility.fuzzyItemSearch(arg);

            // Default fallback for unsupported or missing types.
            return Type.Missing;
        }
    }
}
