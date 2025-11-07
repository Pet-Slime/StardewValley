using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Extensions;
using Log = MoonShared.Attributes.Log;

namespace MoonShared.Attributes
{
    /// <summary>
    /// Marks a class as a configuration definition for a mod.
    /// This attribute automates integration with Generic Mod Config Menu (GMCM),
    /// reading, saving, and wiring options to the in-game menu automatically.
    /// It supports typed config fields, GMCM layout controls (section titles, pages, etc.),
    /// and ensures correct order and title-screen-only behaviors.
    /// </summary>
    public class SConfig(bool titleScreenOnly = false) : ClassHandler(1)
    {
        // Holds a static reference to the GMCM API instance once loaded.
        // This allows nested handler classes (Option, SectionTitle, etc.)
        // to add options to the GMCM UI after registration.
        private static IGenericModConfigMenuApi? _api;

        /// <summary>
        /// Main entry point for handling a configuration class.
        /// Called when a mod declares [SConfig] on a config class.
        /// Reads config data, ensures a valid instance, and registers with GMCM.
        /// </summary>
        public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
        {
            // Safety check: config handlers must have at least priority 1
            // to ensure they run after lower-level handlers (like [SField]).
            if (this.Priority < 1)
            {
                Log.Error("Config cannot be loaded with priority < 1");
                return;
            }

            // Try to locate the config field or property on the mod that matches the target type.
            // This is how we know where the mod's configuration is stored.
            if (!mod.GetType().TryGetMemberOfType(type, out MemberInfo configField))
            {
                Log.Error("Mod must define a Config property");
                return;
            }

            // Extract the getter and setter for the config field.
            var getter = configField.GetGetter();
            var setter = configField.GetSetter();

            // Attempt to read the current config file via SMAPI’s helper API.
            // This uses reflection so that it’s generic-safe.
            // If no config file exists, it will use the instance passed in.
            instance = mod.Helper.GetType().GetMethod("ReadConfig")
                ?.MakeGenericMethod(type)
                .Invoke(mod.Helper, []) ?? instance;

            // Set the loaded config instance back onto the mod’s Config property.
            setter(mod, instance);

            // Attempt to acquire the Generic Mod Config Menu API.
            // If it’s missing, we skip all UI-related setup.
            _api = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (_api is null)
            {
                Log.Info("Generic Mod Config Menu is not enabled, so will skip parsing");
                return;
            }

            // Register the mod’s config root with GMCM.
            // Provides reset (restore defaults), save (write config), and titleScreenOnly behavior.
            _api.Register(
                mod: mod.ModManifest,
                reset: () =>
                {
                    // Reset copies default values from a fresh instance of the config class
                    // into the existing live config object.
                    object? copyFrom = Activator.CreateInstance(type);
                    object? copyTo = getter(mod);
                    foreach (PropertyInfo property in type.GetProperties(ReflectionExtensions.ALL_DECLARED))
                    {
                        property.SetValue(copyTo, property.GetValue(copyFrom));
                    }
                    foreach (FieldInfo field in type.GetFields(ReflectionExtensions.ALL_DECLARED))
                    {
                        field.SetValue(copyTo, field.GetValue(copyFrom));
                    }
                },
                // Save handler persists config to disk using SMAPI.
                save: () => mod.Helper.WriteConfig(getter(mod) ?? ""),
                titleScreenOnly: titleScreenOnly
            );

            // After registering the GMCM root, invoke base handling to parse child fields.
            base.Handle(type, instance, mod);
        }


        /// <summary>
        /// Represents a single configurable field within the mod config.
        /// Automatically determines its GMCM control type based on field type
        /// (bool, int, float, string, SButton, KeybindList) and adds it to the UI.
        /// Supports numeric constraints, allowed string values, and custom field IDs.
        /// </summary>
        public class Option : FieldHandler
        {
            private readonly string? _fieldId;
            private readonly float _min = float.MaxValue;
            private readonly float _max = float.MinValue;
            private readonly float _interval = float.MinValue;
            private readonly string[]? _allowedValues;

            // Constructors allow different styles of constraint specification.
            // The fieldId is used internally by GMCM to identify options uniquely.
            public Option(string? fieldId = null)
            {
                this._fieldId = fieldId;
            }

            public Option(int min, int max, int interval = 1, string? fieldId = null)
            {
                this._fieldId = fieldId;
                this._min = min;
                this._max = max;
                this._interval = interval;
            }

            public Option(float min, float max, float interval = 1.0f, string? fieldId = null)
            {
                this._fieldId = fieldId;
                this._min = min;
                this._max = max;
                this._interval = interval;
            }

            public Option(string[] allowedValues, string? fieldId = null)
            {
                this._fieldId = fieldId;
                this._allowedValues = allowedValues;
            }

            /// <summary>
            /// Adds a field to GMCM dynamically depending on its type.
            /// Uses reflection-provided getter/setter and auto-localizes names and tooltips.
            /// </summary>
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                // Validate that the GMCM API has been initialized first.
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }

                // Handle bool options → simple checkbox
                if (fieldType == typeof(bool))
                {
                    _api.AddBoolOption(
                        mod: mod.ModManifest,
                        getValue: () => (bool)(getter(instance) ?? false),
                        setValue: value => setter(instance, value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId
                    );
                }

                // Handle integer options → numeric input
                else if (fieldType == typeof(int))
                {
                    _api.AddNumberOption(
                        mod: mod.ModManifest,
                        getValue: () => (int)(getter(instance) ?? 0),
                        setValue: value => setter(instance, (int)value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId,
                        min: this._min == float.MaxValue ? null : this._min,
                        max: this._max == float.MinValue ? null : this._max,
                        interval: this._interval == float.MinValue ? null : this._interval,
                        formatValue: null
                    );
                }

                // Handle float options → numeric input with decimals
                else if (fieldType == typeof(float))
                {
                    _api.AddNumberOption(
                        mod: mod.ModManifest,
                        getValue: () => (float)(getter(instance) ?? 0f),
                        setValue: value => setter(instance, value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId,
                        min: this._min == float.MaxValue ? null : this._min,
                        max: this._max == float.MinValue ? null : this._max,
                        interval: this._interval == float.MinValue ? null : this._interval,
                        formatValue: null
                    );
                }

                // Handle text fields → text input or dropdown if allowedValues is provided
                else if (fieldType == typeof(string))
                {
                    _api.AddTextOption(
                        mod: mod.ModManifest,
                        getValue: () => (string)(getter(instance) ?? ""),
                        setValue: value => setter(instance, value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId,
                        allowedValues: this._allowedValues,
                        formatAllowedValue: null
                    );
                }

                // Handle single keybinding → SButton picker
                else if (fieldType == typeof(SButton))
                {
                    _api.AddKeybind(
                        mod: mod.ModManifest,
                        getValue: () => (SButton)(getter(instance) ?? SButton.None),
                        setValue: value => setter(instance, value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId
                    );
                }

                // Handle multi-keybinding → multiple allowed key combinations
                else if (fieldType == typeof(KeybindList))
                {
                    _api.AddKeybindList(
                        mod: mod.ModManifest,
                        getValue: () => (KeybindList)(getter(instance) ?? new KeybindList()),
                        setValue: value => setter(instance, value),
                        name: () => mod.Helper.Translation.Get($"config.{name}").Default(name),
                        tooltip: () => mod.Helper.Translation.Get($"config.{name}.tooltip").UsePlaceholder(false),
                        fieldId: this._fieldId
                    );
                }

                // If a type is unrecognized, the developer used an unsupported config type.
                else
                {
                    throw new Exception($"Config had invalid property type {name}");
                }
            }
        }


        // ───────────────────────────────────────────────────────────────
        // SECTION MANAGEMENT CLASSES
        // These allow advanced GMCM layout control (titles, pages, images, etc.)
        // ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a titled header section to the GMCM page.
        /// Displays localized title and optional tooltip.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class SectionTitle(string key) : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.AddSectionTitle(
                    mod: mod.ModManifest,
                    text: () => mod.Helper.Translation.Get($"config.{key}").Default(key),
                    tooltip: () => mod.Helper.Translation.Get($"config.{key}.tooltip").UsePlaceholder(false)
                );
            }
        }

        /// <summary>
        /// Adds a non-interactive paragraph of text to the config menu.
        /// Useful for descriptive explanations between options.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class Paragraph(string key) : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.AddParagraph(
                    mod: mod.ModManifest,
                    text: () => mod.Helper.Translation.Get($"config.{key}").Default(key)
                );
            }
        }

        /// <summary>
        /// Starts a new GMCM page block, allowing configuration to be split
        /// across multiple logical tabs.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class PageBlock(string pageId) : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.AddPage(
                    mod: mod.ModManifest,
                    pageId: pageId,
                    pageTitle: () => mod.Helper.Translation.Get($"config.{pageId}").Default(pageId)
                );
            }
        }

        /// <summary>
        /// Adds a clickable link that navigates to another GMCM page.
        /// This allows a table-of-contents or menu navigation pattern.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class PageLink(string pageId) : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.AddPageLink(
                    mod: mod.ModManifest,
                    pageId: pageId,
                    text: () => mod.Helper.Translation.Get($"config.{pageId}").Default(pageId),
                    tooltip: () => mod.Helper.Translation.Get($"config.{pageId}.tooltip").UsePlaceholder(false)
                );
            }
        }

        /// <summary>
        /// Displays an image in the config menu, either full texture or subsection of one.
        /// Useful for banners or diagrams within GMCM pages.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class Image(string texture, int x = 0, int y = 0, int width = 0, int height = 0) : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.AddImage(
                    mod: mod.ModManifest,
                    texture: () => mod.Helper.GameContent.Load<Texture2D>(texture),
                    texturePixelArea: width != 0 ? new Rectangle(x, y, width, height) : null
                );
            }
        }

        /// <summary>
        /// Begins a block of options that can only be modified from the title screen.
        /// These are hidden while the game world is loaded.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class StartTitleOnlyBlock : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.SetTitleScreenOnlyForNextOptions(
                    mod: mod.ModManifest,
                    titleScreenOnly: true
                );
            }
        }

        /// <summary>
        /// Ends a title-screen-only block, restoring normal config visibility.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
        public class EndTitleOnlyBlock : FieldHandler
        {
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter, Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                if (_api is null)
                {
                    Log.Error("Attempting to use GMCM API before it is initialized");
                    return;
                }
                _api.SetTitleScreenOnlyForNextOptions(
                    mod: mod.ModManifest,
                    titleScreenOnly: false
                );
            }
        }
    }
}
