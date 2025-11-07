using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace WizardryManaBar.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;
        internal static Assets Assets;

        internal static bool ArsVeneficiLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("HeyImAmethyst.ArsVenefici");
        internal static bool MagicStardewLoaded => ModEntry.Instance.Helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C");

        public ITranslationHelper I18N => this.Helper.Translation;


        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Assembly assembly = this.GetType().Assembly;
            MoonShared.Attributes.Parser.InitEvents(helper);
            MoonShared.Attributes.Parser.ParseAll(this);
            ModEntry.Instance.Helper.Events.GameLoop.GameLaunched += Events.GameLaunched;

            TriggerActionManager.RegisterAction(
                $"moonslime.ManaBarAPI.AddMana",
                AddMana);

            TriggerActionManager.RegisterAction(
                $"moonslime.ManaBarAPI.SetMaxMana",
                SetMaxMana);

            TriggerActionManager.RegisterAction(
                $"moonslime.ManaBarAPI.AddToMaxMana",
                AddToMaxMana);

            TriggerActionManager.RegisterAction(
                $"moonslime.ManaBarAPI.SetManaToMax",
                SetManaToMax);
        }

        public override object GetApi()
        {
            try
            {
                return new WizardryManaBar.Core.Api();
            }
            catch
            {
                return null;
            }

        }

        static bool AddMana(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
            {
                return false;
            }
            Game1.player.AddMana(points);
            return true;
        }

        static bool SetMaxMana(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
            {
                return false;
            }
            Game1.player.SetMaxMana(points);
            return true;
        }

        static bool AddToMaxMana(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
            {
                return false;
            }
            Game1.player.SetMaxMana(Game1.player.GetMaxMana() + points);
            return true;
        }

        static bool SetManaToMax(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGetInt(args, 1, out int points, out error, "int points"))
            {
                return false;
            }
            Game1.player.SetManaToMax();
            return true;
        }


    }
}
