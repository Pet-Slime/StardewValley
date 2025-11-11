using BirbCore.Attributes;
using StardewModdingAPI.Events;
using StardewValley;
using BibliocraftSkill.Objects;
using MoonSharedSpaceCore;

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

        [SEvent.SaveLoaded]
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            foreach (Farmer player in Game1.getAllFarmers())
            {
                SpaceUtilities.LearnRecipesOnLoad(Game1.GetPlayer(player.UniqueMultiplayerID), ModEntry.SkillID);
            }
        }

        [SEvent.StatChanged($"moonslime.Bibliocraft.Machines")]
        private void StatChanged_BookMachinesCheck(object sender, SEvent.StatChanged.EventArgs e)
        {
            Utilities.AddEXP(Game1.player, ModEntry.Config.ExperienceFromBookMachines);
        }
    }
}
