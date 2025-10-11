using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace MoonShared.APIs
{
    public interface IJsonAssetsApi
    {
        /// <summary>Load a folder as a Json Assets content pack.</summary>
        /// <param name="path">The absolute path to the content pack folder.</param>
        void LoadAssets(string path);

        /// <summary>Load a folder as a Json Assets content pack.</summary>
        /// <param name="path">The absolute path to the content pack folder.</param>
        /// <param name="translations">The translations to use for <c>TranslationKey</c> fields, or <c>null</c> to load the content pack's <c>i18n</c> folder if present.</param>
        void LoadAssets(string path, ITranslationHelper translations);

        string GetObjectId(string name);
        string GetCropId(string name);
        string GetFruitTreeId(string name);
        string GetBigCraftableId(string name);
        string GetHatId(string name);
        string GetWeaponId(string name);
        string GetClothingId(string name);

        List<string> GetAllObjectsFromContentPack(string cp);
        List<string> GetAllCropsFromContentPack(string cp);
        List<string> GetAllFruitTreesFromContentPack(string cp);
        List<string> GetAllBigCraftablesFromContentPack(string cp);
        List<string> GetAllHatsFromContentPack(string cp);
        List<string> GetAllWeaponsFromContentPack(string cp);
        List<string> GetAllClothingFromContentPack(string cp);
        List<string> GetAllBootsFromContentPack(string cp);

        event EventHandler ItemsRegistered;
        event EventHandler AddedItemsToShop;
    }
}
