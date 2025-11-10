using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;

namespace MoonShared.Attributes
{

    /// CREDITS: Dr Birb from birbcore. Code is under MIT liscense

    /// <summary>
    /// The Parser class scans a mod's assembly (its compiled .dll)
    /// for custom attributes that extend BirbCore-style automation.
    /// 
    /// It helps automatically register configuration, commands,
    /// events, and content pipeline assets by inspecting the mod’s
    /// code and invoking logic defined by special attributes.
    /// 
    /// This allows mod developers to just "mark" classes or fields
    /// with attributes instead of writing lots of setup code manually.
    /// </summary>
    public class Parser
    {
        // A reference to SMAPI's IModHelper, used for event hookups and general mod utilities.
        internal static IModHelper Help;

        // These events represent a priority-based system.
        // Handlers with higher priority numbers run later in the loading sequence.
        // Each priority level (1–9) corresponds to a stage in the SMAPI load cycle.
        internal static event EventHandler? Priority1Event;
        internal static event EventHandler? Priority2Event;
        internal static event EventHandler? Priority3Event;
        internal static event EventHandler? Priority4Event;
        internal static event EventHandler? Priority5Event;
        internal static event EventHandler? Priority6Event;
        internal static event EventHandler? Priority7Event;
        internal static event EventHandler? Priority8Event;
        internal static event EventHandler? Priority9Event;

        /// <summary>
        /// Scans the entire mod assembly for custom attributes and
        /// automatically processes them. This includes registering assets,
        /// commands, events, APIs, and more.
        /// </summary>
        /// <param name="mod">The SMAPI mod instance being parsed.</param>
        public static void ParseAll(IMod mod)
        {
            // Get the assembly (the compiled DLL) that contains the mod's code.
            Assembly assembly = mod.GetType().Assembly;

            // Initialize the custom logging system for this mod.
            Log.Init(mod.Monitor, assembly);

            // Go through every type (class/struct) in the mod's assembly.
            foreach (Type type in assembly.GetTypes())
            {
                // Find any attributes attached to the class that derive from ClassHandler.
                ClassHandler[] classHandlers = (ClassHandler[])Attribute.GetCustomAttributes(type, typeof(ClassHandler));

                // If this class doesn't have any ClassHandler attributes, skip it.
                if (classHandlers.Length == 0)
                    continue;

                // Create an instance of the class so its attributes can be accessed at runtime.
                object? instance = Activator.CreateInstance(type);

                // Process each ClassHandler attribute found on this class.
                foreach (ClassHandler handler in classHandlers)
                {
                    // The priority value determines when the handler should run.
                    switch (handler.Priority)
                    {
                        case 0:
                            // Priority 0 runs immediately during parsing.
                            handler.Handle(type, instance, mod);
                            break;
                        // The rest are queued up to run later through SMAPI events.
                        case 1:
                            Priority1Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 2:
                            Priority2Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 3:
                            Priority3Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 4:
                            Priority4Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 5:
                            Priority5Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 6:
                            Priority6Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 7:
                            Priority7Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 8:
                            Priority8Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                        case 9:
                            Priority9Event += (sender, e) => WrapHandler(handler, type, instance, mod);
                            break;
                    }
                }
            }

            // Apply all Harmony patches defined in this assembly.
            // Harmony is a library for modifying (patching) Stardew Valley code at runtime.
            new Harmony(mod.ModManifest.UniqueID).PatchAll(assembly);
        }

        /// <summary>
        /// Safely runs a ClassHandler's Handle() method, catching any errors that occur.
        /// This ensures that one broken handler doesn't stop others from running.
        /// </summary>
        private static void WrapHandler(ClassHandler handler, Type type, object? instance, IMod mod)
        {
            try
            {
                handler.Handle(type, instance, mod);
            }
            catch (Exception e)
            {
                mod.Monitor.Log($"BirbCore failed to parse {handler.GetType().Name} class {type}: {e}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Sets up event listeners so that the Parser can trigger its
        /// priority events at the correct points in the game's lifecycle.
        /// </summary>
        internal static void InitEvents(IModHelper helper)
        {
            Help = helper;
            Help.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Help.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            Help.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
        }

        /// <summary>
        /// Runs once when the game finishes launching (before save loaded).
        /// Used to trigger early initialization events (Priority 1).
        /// </summary>
        private static void GameLoop_GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            Log.Trace("=== Running Priority 1 events ===");
            Priority1Event?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Runs every game tick *before* the game updates.
        /// Triggers Priority 2, 4, 6, and 8 events on specific ticks.
        /// Then unsubscribes when done.
        /// </summary>
        private static void GameLoop_UpdateTicking(object? sender, StardewModdingAPI.Events.UpdateTickingEventArgs e)
        {
            switch (e.Ticks)
            {
                case 0:
                    Log.Trace("=== Running Priority 2 events ===");
                    Priority2Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 1:
                    Log.Trace("=== Running Priority 4 events ===");
                    Priority4Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 2:
                    Log.Trace("=== Running Priority 6 events ===");
                    Priority6Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 3:
                    Log.Trace("=== Running Priority 8 events ===");
                    Priority8Event?.Invoke(sender, EventArgs.Empty);
                    break;
                default:
                    // After a few ticks, stop listening to this event to save performance.
                    Help.Events.GameLoop.UpdateTicking -= GameLoop_UpdateTicking;
                    break;
            }
        }

        /// <summary>
        /// Runs every game tick *after* the game updates.
        /// Triggers Priority 3, 5, 7, and 9 events on specific ticks.
        /// Then unsubscribes when done.
        /// </summary>
        private static void GameLoop_UpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            switch (e.Ticks)
            {
                case 0:
                    Log.Trace("=== Running Priority 3 events ===");
                    Priority3Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 1:
                    Log.Trace("=== Running Priority 5 events ===");
                    Priority5Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 2:
                    Log.Trace("=== Running Priority 7 events ===");
                    Priority7Event?.Invoke(sender, EventArgs.Empty);
                    break;
                case 3:
                    Log.Trace("=== Running Priority 9 events ===");
                    Priority9Event?.Invoke(sender, EventArgs.Empty);
                    break;
                default:
                    // Stop listening once all priority levels have been handled.
                    Help.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked;
                    break;
            }
        }
    }

    // ==========================================================
    // ========== ATTRIBUTE HANDLER BASE CLASSES BELOW ==========
    // ==========================================================

    /// <summary>
    /// A base attribute that marks a class to be handled automatically.
    /// 
    /// Each ClassHandler subclass defines what to do with classes that
    /// have that attribute (for example, to register config, assets, etc.).
    /// 
    /// The "Priority" value determines the order that these handlers run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class ClassHandler(int priority = 0) : Attribute
    {
        public int Priority = priority;

        /// <summary>
        /// Called when the Parser finds a class with this handler.
        /// It searches through all fields, properties, and methods in that class
        /// to find any child attributes (FieldHandler or MethodHandler) to process.
        /// </summary>
        public virtual void Handle(Type type, object? instance, IMod mod, object[]? args = null)
        {
            string className = this.ToString() ?? "";

            // --- Process all FIELDS in the class ---
            foreach (FieldInfo fieldInfo in type.GetFields(ReflectionExtensions.ALL_DECLARED))
            {
                foreach (Attribute attribute in fieldInfo.GetCustomAttributes())
                {
                    string attributeName = attribute.ToString() ?? "";
                    if (attribute is FieldHandler handler && attributeName.StartsWith(className))
                    {
                        try
                        {
                            handler.Handle(fieldInfo, instance, mod, args);
                        }
                        catch (Exception e)
                        {
                            mod.Monitor.Log($"BirbCore failed to parse {handler.GetType().Name} field {fieldInfo.Name}: {e}", LogLevel.Error);
                        }
                    }
                }
            }

            // --- Process all PROPERTIES in the class ---
            foreach (PropertyInfo propertyInfo in type.GetProperties(ReflectionExtensions.ALL_DECLARED))
            {
                foreach (Attribute attribute in propertyInfo.GetCustomAttributes())
                {
                    string attributeName = attribute.ToString() ?? "";
                    if (attribute is FieldHandler handler && attributeName.StartsWith(className))
                    {
                        try
                        {
                            handler.Handle(propertyInfo, instance, mod, args);
                        }
                        catch (Exception e)
                        {
                            mod.Monitor.Log($"BirbCore failed to parse {handler.GetType().Name} property {propertyInfo.Name}: {e}", LogLevel.Error);
                        }
                    }
                }
            }

            // --- Process all METHODS in the class ---
            foreach (MethodInfo method in type.GetMethods(ReflectionExtensions.ALL_DECLARED))
            {
                foreach (Attribute attribute in method.GetCustomAttributes())
                {
                    string attributeName = attribute.ToString() ?? "";
                    if (attribute is MethodHandler handler && attributeName.StartsWith(className))
                    {
                        try
                        {
                            handler.Handle(method, instance, mod, args);
                        }
                        catch (Exception e)
                        {
                            mod.Monitor.Log($"BirbCore failed to parse {handler.GetType().Name} method {method.Name}: {e}", LogLevel.Error);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Base class for any attribute that handles methods (functions).
    /// Derived classes define what to do when such methods are found.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class MethodHandler : Attribute
    {
        // Each MethodHandler subclass must define how to handle the method it decorates.
        public abstract void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null);
    }

    /// <summary>
    /// Base class for any attribute that handles fields or properties.
    /// 
    /// Derived classes implement the abstract Handle() method to define
    /// what to do when such a field/property is found on a class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public abstract class FieldHandler : Attribute
    {
        // Helper overload: handles a FIELD and passes accessors (get/set).
        public void Handle(FieldInfo fieldInfo, object? instance, IMod mod, object[]? args = null)
        {
            this.Handle(fieldInfo.Name, fieldInfo.FieldType, fieldInfo.GetValue, fieldInfo.SetValue, instance, mod, args);
        }

        // Helper overload: handles a PROPERTY and passes accessors (get/set).
        public void Handle(PropertyInfo propertyInfo, object? instance, IMod mod, object[]? args = null)
        {
            this.Handle(propertyInfo.Name, propertyInfo.PropertyType, propertyInfo.GetValue, propertyInfo.SetValue, instance, mod, args);
        }

        /// <summary>
        /// The main method that subclasses must override to define behavior.
        /// Gives you direct access to get/set the field/property’s value.
        /// </summary>
        protected abstract void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null);
    }
}
