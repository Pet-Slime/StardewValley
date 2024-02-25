using StardewModdingAPI;
using System;

namespace RadiationTierTools
{
    internal static class I18n
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's translation helper.</summary>
        private static ITranslationHelper? Translations;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="translations">The mod's translation helper.</param>
        public static void Init(ITranslationHelper translations)
        {
            I18n.Translations = translations;
        }

        /// <summary>Get a translation equivalent to "Radioactive Axe".</summary>
        public static string Tool_Axe_Radioactive()
        {
            return I18n.GetByKey("tool.axe.radioactive");
        }

        /// <summary>Get a translation equivalent to "Radioactive Watering Can".</summary>
        public static string Tool_Wcan_Radioactive()
        {
            return I18n.GetByKey("tool.wcan.radioactive");
        }

        /// <summary>Get a translation equivalent to "Radioactive Pickaxe".</summary>
        public static string Tool_Pick_Radioactive()
        {
            return I18n.GetByKey("tool.pick.radioactive");
        }

        /// <summary>Get a translation equivalent to "Radioactive Hoe".</summary>
        public static string Tool_Hoe_Radioactive()
        {
            return I18n.GetByKey("tool.hoe.radioactive");
        }

        /// <summary>Get a translation by its key.</summary>
        /// <param name="key">The translation key.</param>
        /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
        public static Translation GetByKey(string key, object? tokens = null)
        {
            if (I18n.Translations == null)
                throw new InvalidOperationException($"You must call {nameof(I18n)}.{nameof(I18n.Init)} from the mod's entry method before reading translations.");
            return I18n.Translations.Get(key, tokens);
        }
    }
}
