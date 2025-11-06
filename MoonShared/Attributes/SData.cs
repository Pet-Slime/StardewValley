#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;

namespace MoonShared.Attributes;

/// <summary>
/// Marks a class as a data container that automatically handles save, local, or global data.
/// </summary>
public class SData : ClassHandler
{
    /// <summary>
    /// Handles the data class itself by wiring it to a mod-level data property.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        // Ensure the mod has a property to hold this data class
        if (!mod.GetType().TryGetMemberOfType(type, out MemberInfo modData))
        {
            Log.Error("Mod must define a data property");
            return;
        }

        // Set the data instance into the mod's property
        modData.GetSetter()(mod, instance);

        // Continue with any additional processing from the base handler
        base.Handle(type, instance, mod);
    }

    // ──────────────────────────────────────────────
    // NESTED FIELD HANDLERS
    // These provide automatic wiring of fields for save, local, or global data.
    // ──────────────────────────────────────────────

    /// <summary>
    /// Loads and saves persistent save data under a specific key.
    /// </summary>
    /// <param name="key">The key under which the data will be saved and loaded.</param>
    public class SaveData(string key) : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            // Load save data when a save is loaded
            mod.Helper.Events.GameLoop.SaveLoaded += (sender, e) =>
            {
                object? saveData = mod.Helper.Data.GetType().GetMethod("ReadSaveData")
                    ?.MakeGenericMethod(fieldType)
                    .Invoke(mod.Helper.Data, [key]);

                setter(instance, saveData);
            };

            // Initialize default data when a save is created
            mod.Helper.Events.GameLoop.SaveCreated += (sender, e) =>
            {
                object? saveData = AccessTools.CreateInstance(fieldType);
                setter(instance, saveData);
            };

            // Write save data at the end of the day
            mod.Helper.Events.GameLoop.DayEnding += (sender, e) =>
            {
                object? saveData = getter(instance);
                mod.Helper.Data.WriteSaveData(key, saveData);
            };
        }
    }

    /// <summary>
    /// Loads and saves local (JSON) data in the mod folder.
    /// </summary>
    /// <param name="jsonFile">The JSON file name for storing this data.</param>
    public class LocalData(string jsonFile) : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            // Load local JSON file, or create default if not present
            object? localData = mod.Helper.Data.GetType().GetMethod("ReadJsonFile")
                ?.MakeGenericMethod(fieldType)
                .Invoke(mod.Helper.Data, [jsonFile]);

            localData ??= AccessTools.CreateInstance(fieldType);
            setter(instance, localData);

            // Save JSON file at the end of the day
            mod.Helper.Events.GameLoop.DayEnding += (sender, e) =>
            {
                object? data = getter(instance);
                mod.Helper.Data.WriteJsonFile(jsonFile, data);
            };
        }
    }

    /// <summary>
    /// Loads and saves global data shared across all save files.
    /// </summary>
    /// <param name="key">The key under which the global data is stored.</param>
    public class GlobalData(string key) : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            // Load global data, or create default if missing
            object? globalData = mod.Helper.Data.GetType().GetMethod("ReadGlobalData")
                ?.MakeGenericMethod(fieldType)
                .Invoke(mod.Helper.Data, [key]);

            globalData ??= AccessTools.CreateInstance(fieldType);
            setter(instance, globalData);

            // Save global data at the end of the day
            mod.Helper.Events.GameLoop.DayEnding += (sender, e) =>
            {
                object? data = getter(instance);
                mod.Helper.Data.WriteGlobalData(key, data);
            };
        }
    }
}
