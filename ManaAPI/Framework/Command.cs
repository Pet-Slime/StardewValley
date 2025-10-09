using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using MoonShared;

namespace ManaBar.Framework
{
    internal static class Command
    {
        /*********
        ** Fields
        *********/
        private static readonly Dictionary<string, Action<string[]>> Commands = new();


        /*********
        ** Public methods
        *********/
        public static void Register(string name, Action<string[]> callback)
        {
            // TODO: Load documentation from a file.
            ModEntry.Instance.Helper.ConsoleCommands.Add(name, "TO BE IMPLEMENTED", Command.HandleCommand);
            Command.Commands.Add(name, callback);
        }


        /*********
        ** Private methods
        *********/
        private static void HandleCommand(string cmd, string[] args)
        {
            try
            {
                Command.Commands[cmd].Invoke(args);
            }
            catch (Exception e)
            {
                Log.Error("Error running command.");
                Log.Debug("Exception: " + e);
            }
        }
    }
}
