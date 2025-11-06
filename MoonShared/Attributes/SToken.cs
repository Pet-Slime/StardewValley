#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MoonShared.APIs;
using StardewModdingAPI;

namespace MoonShared.Attributes;

/// <summary>
/// Specifies a method or class as a content patcher simple or advanced token.
/// </summary>
public class SToken() : ClassHandler(2)
{
    private static IContentPatcherApi? _api;

    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        if (this.Priority < 1)
        {
            Log.Error("Tokens cannot be loaded with priority < 1");
            return;
        }

        _api = mod.Helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
        if (_api == null)
        {
            Log.Error("Content Patcher is not enabled, so will skip parsing");
            return;
        }

        base.Handle(type, instance, mod);
    }

    public class FieldToken : FieldHandler
    {
        private object? _instance;
        private Func<object?, object?>? _getter;

        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so will skip parsing");
                return;
            }

            this._instance = instance;
            this._getter = getter;

            _api.RegisterToken(mod.ModManifest, name, this.GetValue);
        }

        private IEnumerable<string>? GetValue()
        {
            object? value = this._getter?.Invoke(this._instance);
            switch (value)
            {
                case null:
                    yield return "";
                    break;
                case IEnumerable items:
                {
                    foreach (object item in items)
                    {
                        yield return (string)item;
                    }

                    break;
                }
                default:
                    yield return value.ToString() ?? "";
                    break;
            }
        }
    }

    public class Token : MethodHandler
    {
        public override void Handle(MethodInfo method, object? instance, IMod mod, object[]? args = null)
        {
            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so will skip parsing");
                return;
            }

            _api.RegisterToken(mod.ModManifest, method.Name, () => (IEnumerable<string>?)method.Invoke(instance, []));
        }
    }

    public class AdvancedToken : ClassHandler
    {
        public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
        {
            instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                Log.Error("Content Patcher advanced api requires an instance of token class. " +
                          "Provided token class may be static?");
                return;
            }

            base.Handle(type, instance, mod);
            if (_api == null)
            {
                Log.Error("Content Patcher is not enabled, so will skip parsing");
                return;
            }

            _api.RegisterToken(mod.ModManifest, type.Name, instance);
        }
    }
}
