using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace MoonShared.Attributes
{
    /// <summary>
    /// The SAsset class handles automatic linking of mod asset fields or properties to Stardew's content system.
    /// This lets you easily define assets (like textures or data files) in your mod class and have them automatically
    /// loaded and updated when SMAPI reloads content.
    /// </summary>
    public class SAsset : ClassHandler
    {
        // Holds reference info for the mod's asset-related member (field or property).
        private MemberInfo? _modAssets;

        /// <summary>
        /// Called when SMAPI (or this shared framework) initializes the class.
        /// It finds the asset-related field/property in the mod and sets up automatic syncing with SMAPI’s content system.
        /// </summary>
        /// <param name="type">The field or property type to handle (e.g., Texture2D, Data file).</param>
        /// <param name="instance">The instance of the object that owns this field, or null if static.</param>
        /// <param name="mod">The mod instance using this attribute system.</param>
        /// <param name="args">Optional parameters (usually unused).</param>
        public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
        {
            // Try to find a member (property or field) in the mod that matches this type.
            if (!mod.GetType().TryGetMemberOfType(type, out MemberInfo memberInfo))
            {
                // If no member matches, log an error and stop. This helps catch setup mistakes in the mod’s class.
                Log.Error("Mod must define an asset property");
                return;
            }

            // Store reference to the asset member for later use.
            this._modAssets = memberInfo;

            // Get a setter delegate for the member (a fast way to set its value using reflection).
            Action<object?, object?> setter = this._modAssets.GetSetter();

            // Assign the instance’s asset handler to the mod.
            setter(mod, instance);

            // Call the base handler to continue any other initialization logic.
            base.Handle(type, instance, mod, args);
        }

        /// <summary>
        /// Represents an attribute that defines a single asset field inside a mod.
        /// When used, this automatically manages loading, refreshing, and reassigning the asset
        /// when the game reloads or other mods override the asset.
        /// </summary>
        /// <example>
        /// Example usage inside a mod class:
        /// <code>
        /// [SAsset.Asset("assets/my_texture.png")]
        /// public static Texture2D MyTexture;
        /// public static string MyTextureAssetName;
        /// </code>
        /// This would load "assets/my_texture.png" and make it available at
        /// Mods/&lt;ModID&gt;/MyTexture, updating it automatically if replaced.
        /// </example>
        public class Asset(string path, AssetLoadPriority priority = AssetLoadPriority.Medium) : FieldHandler
        {
            // The relative file path to the mod asset, normalized to be consistent across OSes.
            private readonly string _path = PathUtilities.NormalizePath(path);

            /// <summary>
            /// Called to set up the linkage between the field and the content pipeline.
            /// Hooks into SMAPI’s content events so that this field always reflects the correct asset value.
            /// </summary>
            protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
                Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
            {
                // Create an internal SMAPI-friendly name for the asset (e.g., "Mods/Author.ModID/MyTexture")
                IAssetName assetName = mod.Helper.ModContent.GetInternalAssetName(this._path);

                // If the instance is null, we’re likely handling a static field.
                // In either case, we try to find a related property ending in "AssetName" and set it to the full asset path.
                if (instance is null)
                {
                    if (fieldType.DeclaringType != null &&
                        fieldType.DeclaringType.TryGetSetterOfName(name + "AssetName", out Action<object?, object?> assetNameSetter))
                    {
                        assetNameSetter(instance, assetName);
                    }
                }
                else
                {
                    if (instance.GetType().TryGetSetterOfName(name + "AssetName", out Action<object?, object?> assetNameSetter))
                    {
                        assetNameSetter(instance, assetName);
                    }
                }

                // === Hook #1: When SMAPI requests this asset (before it’s loaded) ===
                mod.Helper.Events.Content.AssetRequested += (sender, e) =>
                {
                    // Only handle the event if the requested asset name matches our tracked asset.
                    if (!e.Name.IsEquivalentTo(assetName))
                        return;

                    // Dynamically call e.LoadFromModFile<T>(path, priority) where T = fieldType
                    // This tells SMAPI to load the file from the mod folder into the game content pipeline.
                    e.GetType().GetMethod("LoadFromModFile")
                        ?.MakeGenericMethod(fieldType)
                        .Invoke(e, [this._path, priority]);

                    // After requesting the asset, load and assign it to the field immediately.
                    setter(instance, LoadValue(fieldType, this._path, mod));
                };

                // === Hook #2: When SMAPI finishes loading the asset ===
                mod.Helper.Events.Content.AssetReady += (sender, e) =>
                {
                    // Again, check that the ready asset matches our target asset.
                    if (!e.Name.IsEquivalentTo(assetName))
                        return;

                    // Re-load and assign the freshly loaded asset.
                    setter(instance, LoadValue(fieldType, this._path, mod));
                };

                // === Hook #3: When SMAPI invalidates assets (e.g., content packs or updates replace them) ===
                mod.Helper.Events.Content.AssetsInvalidated += (sender, e) =>
                {
                    foreach (IAssetName asset in e.Names)
                    {
                        // If the invalidated asset matches ours, re-load it.
                        if (asset.IsEquivalentTo(assetName))
                        {
                            setter(instance, LoadValue(fieldType, this._path, mod));
                        }
                    }
                };

                // Initial load: ensures that the field starts with the correct value on mod load.
                setter(instance, LoadValue(fieldType, this._path, mod));
            }

            /// <summary>
            /// Loads the asset file from the mod folder using SMAPI’s reflection-based ModContent API.
            /// </summary>
            private static object? LoadValue(Type fieldType, string assetPath, IMod mod)
            {
                // Dynamically calls mod.Helper.ModContent.Load<T>(assetPath)
                return mod.Helper.ModContent.GetType().GetMethod("Load", [typeof(string)])
                    ?.MakeGenericMethod(fieldType)
                    .Invoke(mod.Helper.ModContent, [assetPath]);
            }
        }
    }
}
