using HarmonyLib;
using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Locations;
using SpaceCore;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using MoonShared;
using System.Globalization;
using System.Linq;
using StardewModdingAPI;
using MoonShared.Patching;

namespace ArchaeologySkill
{
    internal class DigUpArtifactSpot_Patcher : BasePatcher
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Apply(Harmony harmony, IMonitor monitor)
        {
            harmony.Patch(
                original: this.RequireMethod<GameLocation>("digUpArtifactSpot"),
                postfix: this.GetHarmonyMethod(nameof(After_Gain_EXP))
            );

            harmony.Patch(
                original: this.RequireMethod<GameLocation>("digUpArtifactSpot"),
                postfix: this.GetHarmonyMethod(nameof(After_Profession_Extra_Loot))
            );
        }


        /*********
        ** Private methods
        *********/


        /// Post Fix to make it so that the player gets extra loot with the Antiquarian Profession
        [HarmonyLib.HarmonyPostfix]
        private static void After_Profession_Extra_Loot(
        GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            //Does The player have the Antiquarian Profession?
            if (Game1.player.HasCustomProfession(Archaeology_Skill.Archaeology10a1))
            {

                Log.Trace("Archaeology skill: Player has Antiquarian");
                Random random = Utility.CreateDaySaveRandom(xLocation * 2000, yLocation, Game1.netWorldState.Value.TreasureTotemsUsed * 777);
                Vector2 vector = new Vector2(xLocation * 64, yLocation * 64);
                int id = random.Next(ModEntry.ArtifactLootTable.Count - 1);
                int itemId = Math.Max(0, id);
                string item = ModEntry.ArtifactLootTable[itemId];
                Item finalItem = ItemRegistry.Create(item);
                Game1.createItemDebris(finalItem, vector, Game1.random.Next(4), __instance);
            }
        }


        /// Post Fix to make it so the player can get EXp. Also the extra loot chance when digging.
        [HarmonyLib.HarmonyPostfix]
        private static void After_Gain_EXP(GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            Utilities.AddEXP(Game1.getFarmer(who.UniqueMultiplayerID), ModEntry.Config.ExperienceFromArtifactSpots);
            Utilities.ApplySpeedBoost(Game1.getFarmer(who.UniqueMultiplayerID));

            double test = Utilities.GetLevel() * 0.05;
            bool bonusLoot = false;
            if (Game1.random.NextDouble() < test)
            {
                bonusLoot = true;
            }
            if (bonusLoot)
            {
                Log.Trace("Archaeology Skll, you won the extra loot chance!");
                string ObjectID = ModEntry.ArtifactLootTable[Math.Max(0, Game1.random.Next(ModEntry.ArtifactLootTable.Count - 1))];
                Game1.createMultipleObjectDebris(ObjectID, xLocation, yLocation, 1, who.UniqueMultiplayerID);
            }
        }
    }
}
