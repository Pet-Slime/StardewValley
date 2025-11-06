using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewModdingAPI;

namespace MoonShared.Attributes
{
    /// <summary>
    /// This static class provides a centralized logging system that can be used
    /// across multiple mods or assemblies that share this code.
    ///
    /// It works by storing each mod's `IMonitor` (the SMAPI-provided logging tool)
    /// in a dictionary, allowing any mod that calls `Log.Init()` to later log
    /// messages without directly holding onto its own monitor reference.
    ///
    /// Essentially, this lets you call `Log.Info("message")` or `Log.Error("message")`
    /// anywhere in your code, even in static helper classes, without needing to pass
    /// your mod’s monitor around.
    /// </summary>
    public class Log
    {
        // Stores a mapping of each assembly's name (unique per mod)
        // to the mod's IMonitor instance.
        // This allows each mod using this shared code to log separately.
        private static readonly Dictionary<string, IMonitor> Monitors = new();

        /// <summary>
        /// Initializes the logging system for a mod by linking its assembly to its monitor.
        /// 
        /// This method should be called once per mod (usually during mod setup)
        /// to register that mod’s `IMonitor` instance. Afterward, any code running
        /// in that mod’s assembly can use Log.Debug(), Log.Info(), etc.
        /// </summary>
        /// <param name="monitor">The mod’s IMonitor instance from SMAPI.</param>
        /// <param name="caller">The mod’s assembly, used as the unique key.</param>
        internal static void Init(IMonitor monitor, Assembly caller)
        {
            string assembly = caller.FullName;

            // Try to add the monitor to our list of known assemblies.
            // If this assembly hasn’t been registered before, store it.
            if (Monitors.TryAdd(assembly, monitor))
            {
                return;
            }

            // If this assembly name already exists, that’s unusual.
            // It may mean that two DLLs share the same assembly name,
            // which can cause logging conflicts.
            monitor.Log(
                $"Assembly {assembly} has already initialized Log... Are there two dlls with the same assembly name?",
                LogLevel.Error);
        }

        // -------------------------------------------------------------------
        // These are convenience wrappers that log messages using the SMAPI monitor.
        // Each one automatically detects which assembly is calling it,
        // then uses the correct IMonitor for that mod.
        // -------------------------------------------------------------------

        /// <summary>
        /// Writes a detailed debug-level message.
        /// Use this for development information that players don’t usually need to see.
        /// </summary>
        public static void Debug(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Debug);
        }

        /// <summary>
        /// Writes a trace-level message (even lower-level than Debug).
        /// Useful for tracking internal flow, like “entered function X”.
        /// </summary>
        public static void Trace(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str);
        }

        /// <summary>
        /// Writes a normal informational message.
        /// This level is safe for everyday events players may want to see.
        /// </summary>
        public static void Info(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Info);
        }

        /// <summary>
        /// Writes a warning message, usually for recoverable problems or misconfigurations.
        /// </summary>
        public static void Warn(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Warn);
        }

        /// <summary>
        /// Writes an error message.
        /// Use this for exceptions or serious issues that stop part of your code from working.
        /// </summary>
        public static void Error(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Error);
        }

        /// <summary>
        /// Writes a high-priority “alert” message.
        /// This is rarely used — it’s for major or critical problems that need immediate attention.
        /// </summary>
        public static void Alert(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Alert);
        }

        /// <summary>
        /// Internal helper that determines which mod (assembly) is making the current log call.
        /// 
        /// It checks if the calling assembly is already registered in the Monitors dictionary.
        /// If not, it falls back to the executing assembly (usually this shared library).
        /// </summary>
        private static string GetKey(Assembly assembly)
        {
            // If we have a valid match for this assembly, use it.
            if (assembly.FullName != null && Monitors.ContainsKey(assembly.FullName))
            {
                return assembly.FullName;
            }

            // Otherwise, fallback to this shared library’s assembly.
            // This ensures logging still works even if the mod forgot to register itself.
            return Assembly.GetExecutingAssembly().FullName;
        }
    }
}
