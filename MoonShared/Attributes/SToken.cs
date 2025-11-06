#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MoonShared.APIs;
using StardewModdingAPI;

namespace MoonShared.Attributes;

/// <summary>
/// Specifies a method, field, or class as a Content Patcher token (simple or advanced).
/// Works with Pathoschild.ContentPatcher to provide dynamic tokens for content packs.
/// </summary>
public class SToken() : ClassHandler(2)
{
    /// <summary>
    /// Holds the reference to the Content Patcher API.
    /// </summary>
    private static IContentPatcherApi? _api;

    /// <summary>
    /// Handles registration of a token for a class or method.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        if (this.Priority < 1)
        {
            Log.Error("Tokens cannot be loaded with priority < 1");
            return;
        }

        // Attempt to get Content Patcher API
        _api = mod.Helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
        if (_api == null)
        {
            Log.Error("Content Patcher is not enabled, so token registration will be skipped.");
            return;
        }

        // Call base class handler to process nested attributes
        base.Handle(type, instance, mod);
    }

    // ──────────────────────────────────────────────
    // FIELD TOKEN
    // Treats a field as a Content Patcher token
    // ──────────────────────────────────────────────
    public class FieldToken : FieldHandler
    {
        private object? _instance;
        private Func<object?, object?>? _getter;

        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so field token registration will be skipped.");
                return;
            }

            this._instance = instance;
            this._getter = getter;

            // Register the field token with Content Patcher
            _api.RegisterToken(mod.ModManifest, name, this.GetValue);
        }

        /// <summary>
        /// Invoked by Content Patcher to get the current value(s) of this token.
        /// Converts the field's value into a collection of strings.
        /// </summary>
        private IEnumerable<string>? GetValue()
        {
            object? value = this._getter?.Invoke(this._instance);
            switch (value)
            {
                case null:
                    yield return "";
                    break;
                case IEnumerable items:
                    foreach (object item in items)
                        yield return (string)item; // assumes IEnumerable<string>
                    break;
                default:
                    yield return value.ToString() ?? "";
                    break;
            }
        }
    }

    // ──────────────────────────────────────────────
    // METHOD TOKEN
    // Treats a method as a Content Patcher token
    // ──────────────────────────────────────────────
    public class Token : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so method token registration will be skipped.");
                return;
            }

            // Register a method token, returning IEnumerable<string>
            _api.RegisterToken(mod.ModManifest, method.Name, () =>
                (IEnumerable<string>?)method.Invoke(instance, []));
        }
    }

    // ──────────────────────────────────────────────
    // ADVANCED TOKEN
    // Treats a class instance as a complex Content Patcher token
    // ──────────────────────────────────────────────
    public class AdvancedToken : ClassHandler
    {
        public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
        {
            // Create an instance of the class to provide to Content Patcher
            instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                Log.Error("Content Patcher advanced token requires an instance of token class. " +
                          "Provided class may be static.");
                return;
            }

            // Call base handler for any nested attributes
            base.Handle(type, instance, mod);

            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so advanced token registration will be skipped.");
                return;
            }

            // Register the token using the instance
            _api.RegisterToken(mod.ModManifest, type.Name, instance);
        }
    }
}
