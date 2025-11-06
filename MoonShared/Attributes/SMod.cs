#nullable enable
using System;
using StardewModdingAPI;

namespace MoonShared.Attributes;

/// <summary>
/// Marks a class as a SMAPI mod handler container.
/// Provides nested attributes to automatically handle API injection or mod instance wiring.
/// </summary>
public class SMod() : ClassHandler(1)
{
    /// <summary>
    /// Handles a mod entry class.
    /// Ensures the class is loaded with sufficient priority and invokes the base handler.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        // Ensure mod entry priority is valid
        if (this.Priority < 1)
        {
            Log.Error("ModEntry cannot be loaded with priority < 1");
            return;
        }

        // Call base to handle nested fields or methods
        base.Handle(type, mod, mod, args);
    }

    // ───────────────────────────────────────────────────────────────
    // NESTED FIELD HANDLERS
    // These handle automatic injection of APIs or the mod instance itself.
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Automatically injects another mod's API into a field.
    /// </summary>
    /// <param name="uniqueId">The unique ID of the target mod.</param>
    /// <param name="isRequired">If true, logs an error if the API is not found.</param>
    public class Api(string uniqueId, bool isRequired = true) : FieldHandler
    {
        /// <summary>
        /// Whether this API is required for the mod to function.
        /// </summary>
        public bool IsRequired = isRequired;

        /// <summary>
        /// Resolves the API from the ModRegistry and sets it on the annotated field.
        /// </summary>
        protected override void Handle(
            string name,
            Type fieldType,
            Func<object?, object?> getter,
            Action<object?, object?> setter,
            object? instance,
            IMod mod,
            object[]? args = null
        )
        {
            // Use reflection to call GetApi<T>(string) on the ModRegistry
            object? api = mod.Helper.ModRegistry.GetType()
                .GetMethod("GetApi", 1, [typeof(string)])
                ?.MakeGenericMethod(fieldType)
                .Invoke(mod.Helper.ModRegistry, [uniqueId]);

            // Log error if the API is required but not found
            if (api is null && this.IsRequired)
            {
                Log.Error($"[{name}] Can't access required API");
            }

            // Set the API reference on the target field
            setter(instance, api);
        }
    }

    /// <summary>
    /// Automatically injects the current mod instance into a field.
    /// Useful for giving other classes access to the mod itself without manually passing it.
    /// </summary>
    public class Instance : FieldHandler
    {
        protected override void Handle(
            string name,
            Type fieldType,
            Func<object?, object?> getter,
            Action<object?, object?> setter,
            object? instance,
            IMod mod,
            object[]? args = null
        )
        {
            // Simply set the field to the current mod instance
            setter(instance, mod);
        }
    }
}
