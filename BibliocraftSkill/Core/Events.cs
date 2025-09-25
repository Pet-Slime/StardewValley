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
using StardewValley.Constants;
using static SpaceCore.Skills;

namespace BibliocraftSkill.Core
{
    [SEvent]
    internal class Events
    {
        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Bibliocraft: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Book_Skill());


//            foreach (string SkillID in Skills.GetSkillList()) {
//
//                Skill test = GetSkill(SkillID);
//                foreach (Skills.Skill.Profession prof in test.Professions)
//                {
//                    Log.Alert($"Profession name is: {prof.Id}");
//                    Log.Alert($"Profession number is: {prof.GetVanillaId()}");
//                }
//
//            }

        }

        [SEvent.StatChanged("moonslime.BibliocraftSkill.Machines")]
        private void StatChanged_BookMachinesCheck(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromBookMachines);
        }
    }
}
