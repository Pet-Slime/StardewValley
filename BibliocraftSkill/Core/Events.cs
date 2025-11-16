using System;
using System.Collections.Generic;
using System.Linq;
using BibliocraftSkill.Objects;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using MoonSharedSpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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

            foreach (var entry in Game1.objectData)
            {
                if (entry.Key.StartsWith("moonslime.Bibliocraft.letter_"))
                {
                    if (Game1.objectData.TryGetValue(entry.Key, out var data)
                            && data?.CustomFields != null
                            && data.CustomFields.TryGetValue("moonslime.Bibliocraft.mail", out string name))
                    {
                        ModEntry.MailingList.Add(name, entry.Key);
                    }
                }

            }

            // Get the asset dictionary reference (the asset's data)
            var dict = Game1.content.Load<Dictionary<string, string>>("Data/NPCGiftTastes");


            foreach (string npcName in dict.Keys.ToList()) // ToList() because we modify dictionary
            {
                string gifts = dict[npcName];
                string[] split = gifts.Split('/');

                // Safety: ensure split has at least 6 elements
                if (split.Length < 8)
                {
                    continue;
                }


                foreach (var kvp in ModEntry.MailingList)
                {
                    string mailNpcName = kvp.Key; // NPC name from mailing list
                    string itemKey = kvp.Value;   // Object ID

                    if (mailNpcName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Add to loved gifts only if not already present
                        var lovedItems = split[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (!lovedItems.Contains(itemKey))
                        {
                            lovedItems.Add(itemKey);
                            split[1] = string.Join(" ", lovedItems);
                        }
                    }
                    else
                    {
                        // Add to hated gifts only if not already present
                        var hatedItems = split[7].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (!hatedItems.Contains(itemKey))
                        {
                            hatedItems.Add(itemKey);
                            split[5] = string.Join(" ", hatedItems);
                        }
                    }
                }


                // Recombine and update the dictionary
                dict[npcName] = string.Join("/", split);
            }

        }

        [SEvent.AssetRequested]
        public static void AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Check for the target asset
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/NPCGiftTastes"))
                return;


            e.Edit(asset =>
            {
                // Get the asset dictionary reference (the asset's data)
                var dict = asset.AsDictionary<string, string>().Data;


                foreach (string npcName in dict.Keys.ToList()) // ToList() because we modify dictionary
                {
                    string gifts = dict[npcName];
                    string[] split = gifts.Split('/');

                    // Safety: ensure split has at least 6 elements
                    if (split.Length < 6)
                    {
                        continue;
                    }
                    foreach (var kvp in ModEntry.MailingList)
                    {
                        string mailNpcName = kvp.Key; // NPC name from mailing list
                        string itemKey = kvp.Value;   // Object ID

                        if (mailNpcName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Add to loved gifts only if not already present
                            var lovedItems = split[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (!lovedItems.Contains(itemKey))
                            {
                                lovedItems.Add(itemKey);
                                split[1] = string.Join(" ", lovedItems);
                            }
                        }
                        else
                        {
                            // Add to hated gifts only if not already present
                            var hatedItems = split[5].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (!hatedItems.Contains(itemKey))
                            {
                                hatedItems.Add(itemKey);
                                split[5] = string.Join(" ", hatedItems);
                            }
                        }
                    }
                    // Recombine and update the dictionary
                    dict[npcName] = string.Join("/", split);
                }

                asset.ReplaceWith(dict);
            });
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
