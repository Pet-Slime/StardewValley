using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExpControl.Core
{
    [SEvent]
    public class Events
    {

        [SEvent.SaveLoaded]
        private void OnSaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            WriteHostConfigToFarm();
        }


        [SEvent.DayStarted]
        private void OnDayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            WriteHostConfigToFarm();
        }

        private void WriteHostConfigToFarm()
        {
            if (!Context.IsWorldReady)
                return;

            if (!Context.IsMainPlayer)
                return;

            Game1.getFarm().modData["moonslime.ExpControl.HostConfig.FarmingEXP"] = ModEntry.Config.FarmingEXP.ToString(CultureInfo.InvariantCulture);
            Game1.getFarm().modData["moonslime.ExpControl.HostConfig.MiningEXP"] = ModEntry.Config.MiningEXP.ToString(CultureInfo.InvariantCulture);
            Game1.getFarm().modData["moonslime.ExpControl.HostConfig.FishingEXP"] = ModEntry.Config.FishingEXP.ToString(CultureInfo.InvariantCulture);
            Game1.getFarm().modData["moonslime.ExpControl.HostConfig.ForagingEXP"] = ModEntry.Config.ForagingEXP.ToString(CultureInfo.InvariantCulture);
            Game1.getFarm().modData["moonslime.ExpControl.HostConfig.CombatEXP"] = ModEntry.Config.CombatEXP.ToString(CultureInfo.InvariantCulture);
        }
    }

    
}
