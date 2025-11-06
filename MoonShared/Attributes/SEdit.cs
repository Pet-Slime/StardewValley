#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Sickhead.Engine.Util;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MoonShared.Attributes;

/// <summary>
/// A collection of Edits made to the content pipeline.  
/// Similar functionality to Content Patcher, but in code, and with far fewer features.
/// </summary>
public class SEdit : ClassHandler
{
    /// <summary>
    /// Defines when an edit should be reapplied or invalidated.
    /// </summary>
    public enum Frequency
    {
        Never,              // Only apply once when asset is requested
        OnDayStart,         // Recheck condition at the start of a new day
        OnLocationChange,   // Recheck condition when the player warps to a new location
        OnTimeChange,       // Recheck condition when the in-game time changes
        OnTick              // Recheck condition every game update tick
    }

    /// <summary>
    /// Handles the main class-level setup for string translations.
    /// Automatically hooks into asset requested and locale changed events.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        base.Handle(type, instance, mod, args);

        // Hook into Content Patcher's asset requested event for translation strings
        mod.Helper.Events.Content.AssetRequested += (sender, e) =>
        {
            if (!e.Name.IsEquivalentTo($"Mods/{mod.ModManifest.UniqueID}/Strings"))
            {
                return; // Only intercept translation string assets
            }

            e.Edit(apply =>
            {
                Dictionary<string, string> dict = new();
                // Populate dictionary with all mod translations
                foreach (Translation translation in mod.Helper.Translation.GetTranslations())
                {
                    dict[translation.Key] = translation.ToString();
                }
                apply.ReplaceWith(dict);
            }, AssetEditPriority.Early); // Apply early to allow other edits to override
        };

        // Invalidate translation cache when locale changes
        mod.Helper.Events.Content.LocaleChanged += (sender, e) =>
        {
            mod.Helper.GameContent.InvalidateCache($"Mods/{mod.ModManifest.UniqueID}/Strings");
        };
    }

    /// <summary>
    /// Base class for content edits. Handles frequency-based invalidation and condition checking.
    /// </summary>
    public abstract class BaseEdit(
        string target,
        string? condition = null,
        Frequency frequency = Frequency.Never,
        AssetEditPriority priority = AssetEditPriority.Default)
        : FieldHandler
    {
        private readonly string _target = target;                  // Asset target path
        private readonly string? _condition = condition;          // Optional GameStateQuery condition
        private readonly AssetEditPriority _priority = priority;  // Priority of edit application
        protected IMod? Mod;                                      // Reference to the mod instance
        private bool _isApplied;                                   // Tracks whether the edit is currently applied

        /// <summary>
        /// Sets up event hooks for invalidation and asset edits.
        /// </summary>
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            // Skip if condition is known to always be false
            if (GameStateQuery.IsImmutablyFalse(this._condition))
            {
                Log.Error($"Condition {this._condition} will never be true, so edit {name} will never be applied.");
                return;
            }

            this.Mod = mod;

            // Attach frequency-based invalidation hooks
            switch (frequency)
            {
                case Frequency.OnDayStart: this.Mod.Helper.Events.GameLoop.DayStarted += this.InvalidateIfNeeded; break;
                case Frequency.OnLocationChange: this.Mod.Helper.Events.Player.Warped += this.InvalidateIfNeeded; break;
                case Frequency.OnTimeChange: this.Mod.Helper.Events.GameLoop.TimeChanged += this.InvalidateIfNeeded; break;
                case Frequency.OnTick: this.Mod.Helper.Events.GameLoop.UpdateTicked += this.InvalidateIfNeeded; break;
            }

            BaseEdit edit = this;

            // Attach asset edit callback
            this.Mod.Helper.Events.Content.AssetRequested += (sender, e) =>
            {
                if (!e.Name.IsEquivalentTo(edit._target))
                {
                    return; // Only edit the specified asset
                }
                if (!GameStateQuery.CheckConditions(edit._condition))
                {
                    return; // Skip if the condition is false
                }

                // Apply the edit
                e.Edit(asset =>
                {
                    edit.DoEdit(asset, getter(instance), name, fieldType, instance);
                }, edit._priority/*, edit.Mod.ModManifest.UniqueID // Can't use this because of SMAPI limitation */);
            };
        }

        /// <summary>
        /// Abstract method to perform the actual asset modification.
        /// </summary>
        protected abstract void DoEdit(IAssetData asset, object? edit, string name, Type fieldType, object? instance);

        /// <summary>
        /// Handles invalidation when the asset should be rechecked due to frequency events.
        /// </summary>
        private void InvalidateIfNeeded(object? sender, object e)
        {
            if (this.Mod is null || this._isApplied == GameStateQuery.CheckConditions(this._condition))
            {
                return; // No change needed
            }

            this._isApplied = !this._isApplied;
            this.Mod.Helper.GameContent.InvalidateCache(this._target);
        }
    }

    /// <summary>
    /// Change some data content in assets.
    /// Supports editing fields inside lists, dictionaries, or objects.
    /// </summary>
    public class Data(
        string target,
        string[]? field = null,
        string? condition = null,
        Frequency frequency = Frequency.Never,
        AssetEditPriority priority = AssetEditPriority.Default)
        : BaseEdit(target, condition, frequency, priority)
    {
        /// <summary>
        /// Main edit logic.
        /// Traverses field paths and applies the edit to the appropriate asset data.
        /// </summary>
        protected override void DoEdit(IAssetData asset, object? edit, string name, Type fieldType, object? instance)
        {
            if (this.Mod is null)
                return;

            // Start with root asset data
            List<object> toEdit = new() { asset.Data };

            if (field is { Length: >= 1 })
            {
                foreach (string t in field)
                {
                    List<object> nextToEdit = new();

                    foreach (object toEditValue in toEdit)
                    {
                        switch (toEditValue)
                        {
                            case IList toEditValueList:
                                nextToEdit.AddRange(GetListEdits(t, toEditValueList));
                                break;
                            case IDictionary toEditValueDictionary:
                                nextToEdit.AddRange(GetDictionaryEdits(t, toEditValueDictionary));
                                break;
                            default:
                                nextToEdit.AddRange(GetMemberEdits(t, toEditValue));
                                break;
                        }
                    }

                    toEdit = nextToEdit;
                }
            }

            // Apply the final edit to each target
            foreach (object toEditValue in toEdit)
            {
                switch (toEditValue)
                {
                    case IList toEditValueList:
                        ApplyListEdit(toEditValueList, edit);
                        break;
                    case IDictionary toEditValueDictionary:
                        this.ApplyDictionaryEdit(toEditValueDictionary, edit, name);
                        break;
                    default:
                        ApplyMemberEdit(toEditValue, edit);
                        break;
                }
            }
        }

        // ───── Utility methods for traversing and applying edits ─────
        private static IEnumerable<object> GetListEdits(string field, IList toEdit)
        {
            List<object> nextToEdit = new();
            if (toEdit.Count <= 0)
                return nextToEdit;

            if (field == "*")
            {
                foreach (object item in toEdit) nextToEdit.Add(item);
                return nextToEdit;
            }

            if (field.StartsWith("#"))
            {
                if (!int.TryParse(field[1..], out int index))
                {
                    Log.Error($"SEdit.Data could not parse field {field} because it expected a numeric index");
                    return nextToEdit;
                }
                if (index >= toEdit.Count)
                {
                    Log.Error($"SEdit.Data could not parse field {field} because the index was out of bounds");
                    return nextToEdit;
                }
                object? item = toEdit[index];
                if (item is null)
                {
                    Log.Error($"SEdit.Data could not parse field {field} because the value at the index was null");
                    return nextToEdit;
                }
                nextToEdit.Add(item);
            }
            else
            {
                Type? t = toEdit[0]?.GetType();
                if (t is null)
                {
                    Log.Error("SEdit.Data could not find type of index 0");
                    return nextToEdit;
                }
                if (!t.TryGetMemberOfName("Id", out MemberInfo id) && !t.TryGetMemberOfName("ID", out id))
                {
                    Log.Error("SEdit.Data could not find key field for list");
                    return nextToEdit;
                }

                foreach (object item in toEdit)
                {
                    if ((string)id.GetValue(item) != field) continue;
                    nextToEdit.Add(item);
                    return nextToEdit;
                }
            }

            return nextToEdit;
        }

        private static IEnumerable<object> GetDictionaryEdits(string field, IDictionary toEdit)
        {
            if (field == "*")
            {
                List<object> edits = new();
                foreach (object toEditItem in toEdit.Values) edits.Add(toEditItem);
                return edits;
            }

            if (!toEdit.Contains(field))
            {
                Log.Error($"SEdit.Data could not find dictionary key with value {field}");
                return Array.Empty<object>();
            }

            object? item = toEdit[field];
            if (item is not null)
                return new[] { item };

            Log.Error($"SEdit.Data dictionary contained null value for {field}");
            return Array.Empty<object>();
        }

        private static IEnumerable<object> GetMemberEdits(string field, object toEdit)
        {
            if (!toEdit.GetType().TryGetMemberOfName(field, out MemberInfo memberInfo))
            {
                Log.Error($"SEdit.Data could not find field or property of name {field}");
                return Array.Empty<object>();
            }

            object? nextToEdit = memberInfo.GetValue(toEdit);
            if (nextToEdit is not null) return new[] { nextToEdit };

            Log.Error($"SEdit.Data could not find field or property of name {field}");
            return Array.Empty<object>();
        }

        private static void ApplyListEdit(IList toEdit, object? edit)
        {
            Type? t = toEdit[0]?.GetType();
            if (t is null)
            {
                Log.Error("SEdit.Data could not find edits");
                return;
            }

            bool hasId = t.TryGetMemberOfName("Id", out MemberInfo id) || t.TryGetMemberOfName("ID", out id);

            if (edit is not IList editList)
            {
                if (!hasId)
                {
                    toEdit.Add(edit);
                    return;
                }
                for (int i = 0; i < toEdit.Count; i++)
                {
                    if (id.GetValue(toEdit[i]) != id.GetValue(edit)) continue;
                    toEdit[i] = edit;
                    return;
                }
                toEdit.Add(edit);
                return;
            }

            foreach (object editListItem in editList)
            {
                if (!hasId)
                {
                    toEdit.Add(editListItem);
                    return;
                }

                for (int i = 0; i < toEdit.Count; i++)
                {
                    if (id.GetValue(toEdit[i]) != id.GetValue(editListItem)) continue;
                    toEdit[i] = editListItem;
                    return;
                }
                toEdit.Add(editListItem);
            }
        }

        private void ApplyDictionaryEdit(IDictionary toEdit, object? edit, string name)
        {
            if (edit is not IDictionary editDictionary)
            {
                string key = $"{this.Mod?.ModManifest.UniqueID}_{name}";
                toEdit[key] = edit;
                return;
            }

            foreach (DictionaryEntry editDictionaryEntry in editDictionary)
            {
                toEdit[editDictionaryEntry.Key] = editDictionaryEntry.Value;
            }
        }

        private static void ApplyMemberEdit(object toEdit, object? edit)
        {
            edit.ShallowCloneTo(toEdit);
        }
    }

    /// <summary>
    /// Expects a relative path to a source image file as the string field value.
    /// </summary>
    public class Image(
        string target,
        PatchMode patchMode,
        string? condition = null,
        Frequency frequency = Frequency.Never,
        AssetEditPriority priority = AssetEditPriority.Default)
        : BaseEdit(target, condition, frequency, priority)
    {
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (fieldType != typeof(string))
            {
                Log.Error($"SEdit.Image only works with string fields or properties, but was {fieldType}");
                return;
            }

            base.Handle(name, fieldType, getter, setter, instance, mod, args);
        }

        protected override void DoEdit(IAssetData asset, object? edit, string name, Type fieldType, object? instance)
        {
            if (edit is null || this.Mod is null) return;

            string filePath = (string)edit;
            IAssetDataForImage image = asset.AsImage();

            IRawTextureData source = this.Mod.Helper.ModContent.Load<IRawTextureData>(filePath);
            Rectangle? sourceRect = null;
            Rectangle? targetRect = null;

            if (fieldType.TryGetGetterOfName(name + "SourceArea", out Func<object?, object?> rectGetter))
                sourceRect = (Rectangle?)rectGetter(instance);
            if (fieldType.TryGetGetterOfName(name + "TargetArea", out rectGetter))
                targetRect = (Rectangle?)rectGetter(instance);

            image.PatchImage(source, sourceRect, targetRect, patchMode);
        }
    }

    /// <summary>
    /// Edits map assets using a specified xTile map.
    /// </summary>
    public class Map(
        string target,
        PatchMapMode patchMode = PatchMapMode.Overlay,
        string? condition = null,
        Frequency frequency = Frequency.Never,
        AssetEditPriority priority = AssetEditPriority.Default)
        : BaseEdit(target, condition, frequency, priority)
    {
        protected override void Handle(string name, Type fieldType, Func<object?, object?> getter,
            Action<object?, object?> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (fieldType != typeof(string))
            {
                Log.Error($"SEdit.Map only works with string fields or properties, but was {fieldType}");
                return;
            }

            base.Handle(name, fieldType, getter, setter, instance, mod, args);
        }

        protected override void DoEdit(IAssetData asset, object? edit, string name, Type fieldType, object? instance)
        {
            if (edit is null || this.Mod is null) return;

            string filePath = (string)edit;
            IAssetDataForMap map = asset.AsMap();

            xTile.Map source = this.Mod.Helper.ModContent.Load<xTile.Map>(filePath);

            Rectangle? sourceRect = null;
            Rectangle? targetRect = null;
            if (fieldType.TryGetGetterOfName(name + "SourceArea", out Func<object?, object?> rectGetter))
                sourceRect = (Rectangle?)rectGetter(instance);
            if (fieldType.TryGetGetterOfName(name + "TargetArea", out rectGetter))
                targetRect = (Rectangle?)rectGetter(instance);

            map.PatchMap(source, sourceRect, targetRect, patchMode);
        }
    }
}
