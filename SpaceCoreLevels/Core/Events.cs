using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using SpaceShared.APIs;
using StardewModdingAPI.Events;

namespace SpaceCoreLevels.Core
{
    [SEvent]
    public class Events
    {
        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ModEntry.Instance.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");


        }
    }
}
