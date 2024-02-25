using HarmonyLib;
using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Locations;
using Netcode;
using SpaceCore;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using MoonShared;
using StardewValley.Objects;
using System.Reflection;
using xTile.Dimensions;
using System.Reflection.Emit;
using System.Globalization;
using System.Linq;
using xTile.ObjectModel;
using xTile;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using System.Diagnostics.CodeAnalysis;
using MoonShared.Patching;

namespace ExcavationSkill
{
    internal class GetPanItems_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<Pan>("getPanItems"),
                postfix: this.GetHarmonyMethod(nameof(After_getPanItems))
            );
        }


        /*********
        ** Private methods
        *********/

        [HarmonyLib.HarmonyPostfix]
        private static void After_getPanItems(
        Pan __instance, List<Item> __result, GameLocation location, Farmer who)
        {
            //Add EXP for the player Panning and check for the gold rush profession
            Utilities.ApplyExcavationSkill(Game1.getFarmer(who.UniqueMultiplayerID));

            //Add Artifacts to the drop list chance if they have the Trowler Profession
            if (Game1.player.HasCustomProfession(Excavation_Skill.Excavation5b))
            {

                Random random = new Random(who.getTileX() * 2000 + who.getTileY() + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);

                if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(Excavation_Skill.Excavation5b))
                {
                    
                    int item_1 = ModEntry.ArtifactLootTable[Math.Max(0, random.Next(ModEntry.ArtifactLootTable.Count - 1))];
                    int item_2 = ModEntry.ArtifactLootTable[Math.Max(0, random.Next(ModEntry.ArtifactLootTable.Count - 1))];
                    if (random.NextDouble() < 0.25)
                    {
                        __result.Clear();
                        __result.Add(new StardewValley.Object(item_1, 1));
                        __result.Add(new StardewValley.Object(item_2, 1));
                    }
                }
                else
                {
                    int item_1 = ModEntry.ArtifactLootTable[Math.Max(0, random.Next(ModEntry.ArtifactLootTable.Count - 1))];
                    if (random.NextDouble() < 0.25)
                    {
                        __result.Clear();
                        __result.Add(new StardewValley.Object(item_1, 1));
                    }
                }
            }

            //Add extra loot to the list if they have the Dowser profession
            if (Game1.player.HasCustomProfession(Excavation_Skill.Excavation10b1))
            {
                Random random = new Random(who.getTileX() * (int)who.DailyLuck * 2000 + who.getTileY() + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
                int xLocation = who.getTileX();
                int yLocation = who.getTileY();
                int lootTableValue = random.Next(ModEntry.BonusLootTable.Count);
                int itemId = Math.Max(0, lootTableValue);
                int item = ModEntry.BonusLootTable[itemId];
                if (ModEntry.MargoLoaded && Game1.player.HasCustomPrestigeProfession(Excavation_Skill.Excavation10b1))
                {
                    for (int i = 0; i < 1; i++)
                    {
                        Game1.createDebris(item, xLocation, yLocation, random.Next(3));
                    }

                }
                else
                {
                    Game1.createDebris(item, xLocation, yLocation, random.Next(3));
                }
            }
        }
    }
}
