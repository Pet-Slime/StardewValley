#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using HarmonyLib;
using StardewModdingAPI;

namespace MoonShared.Attributes;

/// <summary>
/// Marks a class as a content container that automatically loads data from content packs.
/// Handles objects, lists, or dictionaries from JSON files in content packs.
/// </summary>
/// <param name="fileName">The JSON file name to read within each content pack (default: "content.json").</param>
/// <param name="isList">Whether the content type is a list.</param>
/// <param name="isDictionary">Whether the content type is a dictionary.</param>
public class SContent(string fileName = "content.json", bool isList = false, bool isDictionary = false) : ClassHandler
{
    /// <summary>
    /// Handles the content class, populates a dictionary with content from all owned content packs.
    /// </summary>
    public override void Handle(Type type, object? instance, IMod mod, object[]? args = null)
    {
        // Determine the underlying type to store in the dictionary.
        Type modEntryValueType = type;
        if (isList)
        {
            modEntryValueType = typeof(List<>).MakeGenericType(type);
        }
        else if (isDictionary)
        {
            modEntryValueType = typeof(Dictionary<,>).MakeGenericType(typeof(string), type);
        }

        // Create the top-level dictionary type to store all content packs
        Type modEntryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), modEntryValueType);

        // Find the property/field on the mod that will hold all content
        if (!mod.GetType().TryGetMemberOfType(modEntryType, out MemberInfo modContent))
        {
            Log.Error("Mod must define a Content dictionary property");
            return;
        }

        // Initialize the content dictionary
        IDictionary contentDictionary = (IDictionary)AccessTools.CreateInstance(modEntryType);
        if (contentDictionary is null)
        {
            Log.Error("contentDictionary was null. Underlying type might be static? Cannot initialize.");
            return;
        }
        modContent.GetSetter()(mod, contentDictionary);

        // Try to find a member that provides a unique ContentId
        if (type.TryGetMemberWithCustomAttribute(typeof(ContentId), out MemberInfo? idMember))
        {
            if (idMember.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                // Ignore ContentID if it is marked not to be serialized
                idMember = null;
            }
        }

        // Iterate through all content packs owned by this mod
        foreach (IContentPack contentPack in mod.Helper.ContentPacks.GetOwned())
        {
            // Read the content JSON file
            object? content = contentPack.GetType().GetMethod("ReadJsonFile")
                ?.MakeGenericMethod(modEntryValueType)
                .Invoke(contentPack, [fileName]);

            if (content is null)
            {
                Log.Error($"{fileName} in content pack {contentPack.Manifest.UniqueID} was null");
                continue;
            }

            // Assign unique IDs and handle each content object
            if (isList)
            {
                IList contentList = (IList)content;
                for (int i = 0; i < contentList.Count; i++)
                {
                    string id = (string)(idMember?.GetGetter()(contentList[i]) ?? i);
                    base.Handle(type, contentList[i], mod, [contentPack, id]);
                }
            }
            else if (isDictionary)
            {
                foreach (DictionaryEntry entry in (IDictionary)content)
                {
                    string key = (string)entry.Key;
                    object? value = entry.Value;
                    string id = (string)(idMember?.GetGetter()(value) ?? key);
                    base.Handle(type, value, mod, [contentPack, id]);
                }
            }
            else
            {
                string id = (string)(idMember?.GetGetter()(content) ?? "");
                base.Handle(type, content, mod, [contentPack, id]);
            }

            // Add the content to the dictionary using the content pack's unique ID
            string modId = contentPack.Manifest.UniqueID;
            contentDictionary.Add(modId, content);
        }
    }

    // ──────────────────────────────────────────────
    // NESTED FIELD HANDLERS
    // These provide automatic wiring of fields like ModId, UniqueId, ContentId, and the content pack itself.
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sets the ModId field to the unique ID of the content pack.
    /// </summary>
    public class ModId : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object, object?> getter, Action<object, object> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (instance is null)
            {
                Log.Error("Content instance might be static? Failing to add all content packs");
                return;
            }
            if (args?[0] == null)
            {
                Log.Error("Something went wrong in BirbCore Content Pack parsing");
                return;
            }

            setter(instance, ((IContentPack)args[0]).Manifest.UniqueID);
        }
    }

    /// <summary>
    /// Sets a unique ID combining the content pack ID and the content-specific ID.
    /// </summary>
    public class UniqueId : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object, object?> getter, Action<object, object> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (instance is null)
            {
                Log.Error("Content instance might be static? Failing to add all content packs");
                return;
            }
            if (args?[0] == null)
            {
                Log.Error("Something went wrong in BirbCore Content Pack parsing");
                return;
            }

            setter(instance, $"{((IContentPack)args[0]).Manifest.UniqueID}_{args[1]}");
        }
    }

    /// <summary>
    /// Sets the ContentId field for a content object.
    /// </summary>
    public class ContentId : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object, object?> getter, Action<object, object> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (instance is null)
            {
                Log.Error("Content instance might be static? Failing to add all content packs");
                return;
            }
            if (args?[1] == null)
            {
                Log.Error("Something went wrong in BirbCore Content Pack parsing");
                return;
            }

            setter(instance, args[1]);
        }
    }

    /// <summary>
    /// Injects the content pack itself into a field.
    /// </summary>
    public class ContentPack : FieldHandler
    {
        protected override void Handle(string name, Type fieldType, Func<object, object?> getter, Action<object, object> setter, object? instance, IMod mod, object[]? args = null)
        {
            if (instance is null)
            {
                Log.Error("Content instance might be static? Failing to add all content packs");
                return;
            }
            if (args?[0] == null)
            {
                Log.Error("Something went wrong in BirbCore Content Pack parsing");
                return;
            }
            if (fieldType != typeof(IContentPack))
            {
                Log.Error("ContentPack attribute can only set value to field or property of type IContentPack");
                return;
            }

            setter(instance, args[0]);
        }
    }
}
