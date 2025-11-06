using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewModdingAPI;

namespace MoonShared.Attributes
{
    public class Log
    {
        private static readonly Dictionary<string, IMonitor> Monitors = new();

        internal static void Init(IMonitor monitor, Assembly caller)
        {
            string assembly = caller.FullName;
            if (Monitors.TryAdd(assembly, monitor))
            {
                return;
            }

            monitor.Log(
                $"Assembly {assembly} has already initialized Log...Are there two dlls with the same assembly name?",
                LogLevel.Error);
        }
        public static void Debug(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Debug);
        }
        public static void Trace(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str);
        }
        public static void Info(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Info);
        }
        public static void Warn(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Warn);
        }
        public static void Error(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Error);
        }
        public static void Alert(string str)
        {
            Monitors[GetKey(Assembly.GetCallingAssembly())].Log(str, LogLevel.Alert);
        }

        private static string GetKey(Assembly assembly)
        {
            if (assembly.FullName != null && Monitors.ContainsKey(assembly.FullName))
            {
                return assembly.FullName;
            }
            return Assembly.GetExecutingAssembly().FullName;
        }
    }
}
