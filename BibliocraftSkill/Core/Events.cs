using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using BibliocraftSkill;
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
using BibliocraftSkill.Objects.Book_Restoration_Table;

namespace BibliocraftSkill.Core
{
    [SEvent]
    internal class Events
    {
        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var sc = ModEntry.Instance.Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(BookRestorationTable));
            BibliocraftSkill.Objects.Book_Restoration_Table.Patches.Patch(ModEntry.Instance.Helper);

            Log.Trace("Bibliocraft: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Book_Skill());


        }
    }
}
