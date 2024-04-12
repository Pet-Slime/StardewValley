using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace MoonShared.APIs
{
    public interface IDynamicGameAssetsApi
    {

        /// <inheritdoc/>
        public string GetDGAItemId(object item_);


        /// <inheritdoc/>
        public object SpawnDGAItem(string fullId, Color? color);


        /// <inheritdoc/>
        public object SpawnDGAItem(string fullId);


        public string[] ListContentPacks();


        public string[]? GetItemsByPack(string packname);

        public string[] GetAllItems();

        /// <inheritdoc/>
        public void AddEmbeddedPack(IManifest manifest, string dir);
    }
}
