using System;
using StardewModdingAPI.Events;

namespace MoonShared.Asset
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AssetClass : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AssetProperty : Attribute
    {
        public string LocalPath;
        public AssetLoadPriority Priority;

        public AssetProperty(string localPath, AssetLoadPriority priority = AssetLoadPriority.Medium)
        {
            this.LocalPath = localPath;
            this.Priority = priority;
        }
    }
}
