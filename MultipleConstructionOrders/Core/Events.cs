using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonShared.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MultipleConstructionOrders.Core
{
    [SEvent]
    public class Events
    {
        internal static bool RobinCanAcceptConstructionOrdersToday { get; private set; }

        [SEvent.DayStarted]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {

            RobinCanAcceptConstructionOrdersToday = !AnyBuildingUnderConstruction();
            Log.Trace($"Can robin accept multiple orders today: {RobinCanAcceptConstructionOrdersToday}");
        }

        [SEvent.DayEnding]
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            // Important:
            // Do not let yesterday's "Robin can take orders" value leak into tomorrow morning.
            RobinCanAcceptConstructionOrdersToday = false;
        }

        [SEvent.ReturnedToTitle]
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            RobinCanAcceptConstructionOrdersToday = false;
        }

        private static bool AnyBuildingUnderConstruction()
        {
            foreach (GameLocation location in Game1.locations.ToArray())
            {
                if (location == null)
                    continue;

                if (location.isThereABuildingUnderConstruction())
                    return true;
            }

            return false;
        }
    }
}
