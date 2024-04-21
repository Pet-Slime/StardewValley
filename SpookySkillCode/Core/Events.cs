using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using MoonShared.APIs;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;

namespace SpookySkill.Core
{
    [SEvent]
    internal class Events
    {

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Spooky: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Spooky_Skill());
        }
    }
}
