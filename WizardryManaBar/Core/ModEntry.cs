using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Locations;
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



            GameStateQuery.Register("PLAYER_CURRENT_MANA_GREATER_THAN_VALUE", (string[] query, GameStateQueryContext ctx) =>
            {
                string player = query[1];
                string value = query[2];

                if (string.IsNullOrEmpty(player))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Player string is empty");
                }

                if (string.IsNullOrEmpty(value))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Value to check against is empty");
                }

                int intValue = int.Parse(value);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player, target => target.IsCurrentManaGreaterThanValue(intValue));
            });

            GameStateQuery.Register("PLAYER_CURRENT_MANA_LESS_THAN_VALUE", (string[] query, GameStateQueryContext ctx) =>
            {
                string player = query[1];
                string value = query[2];

                if (string.IsNullOrEmpty(player))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Player string is empty");
                }

                if (string.IsNullOrEmpty(value))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Value to check against is empty");
                }

                int intValue = int.Parse(value);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player, target => target.IsCurrentManaLessThanValue(intValue));
            });

            GameStateQuery.Register("PLAYER_MANA", (string[] query, GameStateQueryContext ctx) =>
            {
                string player = query[1];
                string minValue = query[2];
                string maxValue = query[2];

                if (string.IsNullOrEmpty(player))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Player string is empty");
                }

                if (string.IsNullOrEmpty(minValue))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Value to check against is empty");
                }

                int intMaxValue = int.MaxValue;

                if (!string.IsNullOrEmpty(maxValue))
                {
                    intMaxValue = int.Parse(maxValue);
                }

                int intMinValue = int.Parse(minValue);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.GetCurrentMana() >= intMinValue && target.GetCurrentMana() <= intMaxValue);
            });

            GameStateQuery.Register("PLAYER_MAX_MANA", (string[] query, GameStateQueryContext ctx) =>
            {
                string player = query[1];
                string minValue = query[2];
                string maxValue = query[2];

                if (string.IsNullOrEmpty(player))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Player string is empty");
                }

                if (string.IsNullOrEmpty(minValue))
                {
                    return GameStateQuery.Helpers.ErrorResult(query, "Value to check against is empty");
                }

                int intMaxValue = int.MaxValue;

                if (!string.IsNullOrEmpty(maxValue))
                {
                    intMaxValue = int.Parse(maxValue);
                }

                int intMinValue = int.Parse(minValue);

                return GameStateQuery.Helpers.WithPlayer(ctx.Player, player,
                    target => target.GetMaxMana() >= intMinValue && target.GetMaxMana() <= intMaxValue);
            });

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
